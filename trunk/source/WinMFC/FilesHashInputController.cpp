#include "stdafx.h"

#include <vector>
#include <shlobj.h>
#include <atlbase.h>
#include <utility>

#include "Common/HashInputPolicy.h"
#include "WinCommon/HashFolderScanner.h"
#include "WinCommon/WindowsStrings.h"

#include "FilesHashInputController.h"

#include "Adapters/MfcBridge/ThreadDataInputAccess.h"
#include "Common/strhelper.h"

using namespace sunjwbase;

namespace
{
	const size_t COPYDATA_COMMAND_CHAR_LIMIT = 32768;

	class ScopedComApartment
	{
	public:
		ScopedComApartment() : m_result(CoInitializeEx(NULL, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE)) {}
		~ScopedComApartment() { if (SUCCEEDED(m_result)) CoUninitialize(); }
		bool initialized() const { return SUCCEEDED(m_result); }
	private:
		HRESULT m_result;
	};

	FileLoadOutcome MakeFileLoadOutcome(
		FileLoadResult result,
		size_t loadedCount,
		size_t limit,
		bool hasRequestedCount = false,
		size_t requestedCount = 0)
	{
		FileLoadOutcome outcome;
		outcome.result = result;
		outcome.loadedCount = loadedCount;
		outcome.limit = limit;
		outcome.hasRequestedCount = hasRequestedCount;
		outcome.requestedCount = requestedCount;
		return outcome;
	}
}

FilesHashInputController::FilesHashInputController()
	: m_threadData(NULL),
	m_parentWnd(NULL),
	m_loadingFiles(false),
	m_cancelLoad(false),
	m_closeAfterLoading(false)
{
}

FilesHashInputController::~FilesHashInputController()
{
	if (m_folderScan) m_folderScan->Cancel();
}

FilesHashInputController::PendingLoadGuard::PendingLoadGuard(FilesHashInputController& owner)
	: m_owner(owner)
{
	m_owner.m_loadingFiles = true;
	m_owner.m_cancelLoad = false;
	m_owner.m_closeAfterLoading = false;
}

FilesHashInputController::PendingLoadGuard::~PendingLoadGuard()
{
	m_owner.m_loadingFiles = false;
	if (m_owner.m_closeAfterLoading && m_owner.m_parentWnd != NULL)
		m_owner.m_parentWnd->PostMessage(WM_CLOSE);
}

void FilesHashInputController::CancelPendingLoad(bool closeAfterLoading)
{
	if (!m_loadingFiles) return;
	m_cancelLoad = true;
	if (m_folderScan) m_folderScan->Cancel();
	m_closeAfterLoading = m_closeAfterLoading || closeAfterLoading;
}

void FilesHashInputController::Initialize(ThreadData* threadData, CWnd* parentWnd)
{
	m_threadData = threadData;
	m_parentWnd = parentWnd;
}

void FilesHashInputController::LoadCommandLineFiles(LPTSTR filesCmdLine)
{
	if (m_threadData == NULL)
	{
		return;
	}

	TStrVector parameters = ParseFilesCmdLine(filesCmdLine);
	if (parameters.size() > kMaxHashFilesPerSession)
	{
		AfxMessageBox(WindowsStrings::GetCurrentUILang() == 2052
			? _T("选择的文件过多。每次最多选择 8192 个文件。")
			: _T("Too many files. Select at most 8192 files per task."), MB_OK | MB_ICONWARNING);
		return;
	}
	ClearFilePaths();
	ReplaceThreadDataInputFiles(*m_threadData, parameters);
}

FileLoadOutcome FilesHashInputController::LoadOpenFileDialogSelection(LPCTSTR fileFilter)
{
	if (m_threadData == NULL || m_parentWnd == NULL || m_loadingFiles)
	{
		return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	}

	PendingLoadGuard loading(*this);
	std::vector<TCHAR> nameBuffer((kMaxHashFilesPerSession * MAX_PATH) + 1, 0);
	CFileDialog dlgOpen(TRUE, NULL, NULL, OFN_HIDEREADONLY | OFN_ALLOWMULTISELECT, fileFilter, m_parentWnd, 0);
	dlgOpen.GetOFN().lpstrFile = nameBuffer.data();
	dlgOpen.GetOFN().nMaxFile = static_cast<DWORD>(nameBuffer.size());
	if (IDOK != dlgOpen.DoModal())
	{
		return MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession);
	}

	if (m_cancelLoad) return MakeFileLoadOutcome(FileLoadResult::Cancelled, 0, kMaxHashFilesPerSession);
	TStrVector selected;
	for (POSITION pos = dlgOpen.GetStartPosition(); pos != NULL;)
	{
		if (selected.size() == kMaxHashFilesPerSession)
		{
			// Stop before accumulating an unbounded list. Keep the previous task.
			return MakeFileLoadOutcome(FileLoadResult::RejectedOverLimit, 0, kMaxHashFilesPerSession);
		}
		selected.push_back(dlgOpen.GetNextPathName(pos).GetString());
	}
	if (selected.empty()) return MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession);
	ReplaceThreadDataInputFiles(*m_threadData, selected);
	return MakeFileLoadOutcome(FileLoadResult::Success, selected.size(), kMaxHashFilesPerSession);
}

FileLoadOutcome FilesHashInputController::LoadFolderDialogSelection(LPCTSTR folderDialogTitle, LPCTSTR emptyFolderMessage)
{
	if (m_threadData == NULL || m_parentWnd == NULL || m_loadingFiles)
	{
		return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	}

	// A cancelled scan may still be inside a filesystem driver. Keep at most
	// one such worker; other input modes and application close remain available.
	if (m_folderScan && !m_folderScan->HasExited())
	{
		AfxMessageBox(WindowsStrings::GetCurrentUILang() == 2052
			? _T("上一次文件夹扫描仍在停止，请稍后重试，或直接选择文件。")
			: _T("The previous folder scan is still stopping. Retry later or select files directly."), MB_OK | MB_ICONINFORMATION);
		return MakeFileLoadOutcome(FileLoadResult::Cancelled, 0, kMaxHashFilesPerSession);
	}
	m_folderScan.reset();
	PendingLoadGuard loading(*this);
	ScopedComApartment apartment;
	if (!apartment.initialized()) return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	BROWSEINFO browseInfo = {};
	browseInfo.hwndOwner = m_parentWnd->GetSafeHwnd();
	browseInfo.lpszTitle = folderDialogTitle;
	browseInfo.ulFlags = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE | BIF_USENEWUI;

	LPITEMIDLIST itemIdList = SHBrowseForFolder(&browseInfo);
	if (itemIdList == NULL)
	{
		return MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession);
	}

	TCHAR selectedPath[MAX_PATH] = { 0 };
	BOOL hasPath = SHGetPathFromIDList(itemIdList, selectedPath);
	CoTaskMemFree(itemIdList);
	if (!hasPath)
	{
		return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	}

	if (m_cancelLoad) return MakeFileLoadOutcome(FileLoadResult::Cancelled, 0, kMaxHashFilesPerSession);
	CComPtr<IProgressDialog> progress;
	HRESULT progressResult = progress.CoCreateInstance(CLSID_ProgressDialog);
	if (FAILED(progressResult)) return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	progress->SetTitle(folderDialogTitle);
	progress->SetLine(1, WindowsStrings::GetCurrentUILang() == 2052
		? L"正在查找文件，可以取消。" : L"Finding files. You can cancel this operation.", FALSE, NULL);
	if (FAILED(progress->StartProgressDialog(m_parentWnd->GetSafeHwnd(), NULL, PROGDLG_MODAL | PROGDLG_NOTIME | PROGDLG_NOMINIMIZE, NULL)))
		return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	FolderScanOutcome scanOutcome;
	bool quitRequested = false;
	int quitCode = 0;
	try
	{
		m_folderScan = HashFolderScanJob::Start(selectedPath);
		if (!m_folderScan) scanOutcome.failed = true;
		while (m_folderScan && !m_folderScan->IsFinished())
		{
			if (m_cancelLoad || progress->HasUserCancelled())
			{
				CancelPendingLoad();
				break;
			}
			HANDLE worker = m_folderScan->ThreadHandle();
			const DWORD waited = MsgWaitForMultipleObjectsEx(1, &worker, 50, QS_ALLINPUT, MWMO_INPUTAVAILABLE);
			if (waited == WAIT_FAILED)
			{
				scanOutcome.failed = true;
				m_folderScan->Cancel();
				break;
			}
			// A bounded pump keeps the owner responsive without letting a message
			// flood prevent cancellation checks. All UI and COM calls stay here.
			MSG message;
			for (unsigned int count = 0; count < 64 && PeekMessage(&message, NULL, 0, 0, PM_REMOVE); ++count)
			{
				if (message.message == WM_QUIT)
				{
					quitRequested = true;
					quitCode = static_cast<int>(message.wParam);
					CancelPendingLoad();
					break;
				}
				if (!AfxGetThread()->PreTranslateMessage(&message))
				{
					TranslateMessage(&message);
					DispatchMessage(&message);
				}
				if (m_cancelLoad) break;
			}
			if (m_cancelLoad) break;
		}
		if (progress->HasUserCancelled()) CancelPendingLoad();
		if (m_folderScan && !m_cancelLoad && !scanOutcome.failed && !m_folderScan->TryTake(scanOutcome))
			scanOutcome.failed = true;
	}
	catch (...)
	{
		if (m_folderScan) m_folderScan->Cancel();
		scanOutcome.failed = true;
	}
	progress->StopProgressDialog();
	if (quitRequested) PostQuitMessage(quitCode);
	if (scanOutcome.cancelled || m_cancelLoad) return MakeFileLoadOutcome(FileLoadResult::Cancelled, 0, kMaxHashFilesPerSession);
	if (scanOutcome.failed) return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	if (scanOutcome.overFileLimit) return MakeFileLoadOutcome(FileLoadResult::RejectedOverLimit, 0, kMaxHashFilesPerSession);
	if (scanOutcome.budgetExceeded) return MakeFileLoadOutcome(FileLoadResult::EnumerationLimit, 0, kMaxHashFilesPerSession);
	if (scanOutcome.files.empty())
	{
		AfxMessageBox(emptyFolderMessage, MB_OK | MB_ICONINFORMATION);
		return MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession);
	}
	ReplaceThreadDataInputFiles(*m_threadData, scanOutcome.files);
	return MakeFileLoadOutcome(FileLoadResult::Success, scanOutcome.files.size(), kMaxHashFilesPerSession);
}

FileLoadOutcome FilesHashInputController::LoadDroppedFiles(HDROP hDropInfo)
{
	if (m_threadData == NULL)
	{
		DragFinish(hDropInfo);
		return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	}

	UINT droppedFileCount = DragQueryFile(hDropInfo, 0xFFFFFFFF, NULL, 0);
	if (droppedFileCount == 0)
	{
		DragFinish(hDropInfo);
		return MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession);
	}

	if (static_cast<size_t>(droppedFileCount) > kMaxHashFilesPerSession)
	{
		DragFinish(hDropInfo);
		return MakeFileLoadOutcome(FileLoadResult::RejectedOverLimit, 0, kMaxHashFilesPerSession, true, droppedFileCount);
	}

	TStrVector selected;
	for (UINT index = 0; index < droppedFileCount; ++index)
	{
		tstring path;
		if (!CopyDraggedPath(hDropInfo, index, path))
		{
			DragFinish(hDropInfo);
			return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
		}
		selected.push_back(path);
	}
	DragFinish(hDropInfo);
	ReplaceThreadDataInputFiles(*m_threadData, selected);
	return MakeFileLoadOutcome(FileLoadResult::Success, selected.size(), kMaxHashFilesPerSession, true, droppedFileCount);
}

FileLoadOutcome FilesHashInputController::LoadCopyDataFiles(const COPYDATASTRUCT* pCopyDataStruct)
{
	if (m_threadData == NULL || !IsValidCopyDataString(pCopyDataStruct))
	{
		return MakeFileLoadOutcome(FileLoadResult::Error, 0, kMaxHashFilesPerSession);
	}

	const TCHAR* szFiles = static_cast<const TCHAR*>(pCopyDataStruct->lpData);
	TStrVector parameters = ParseFilesCmdLine(const_cast<TCHAR*>(szFiles));
	if (parameters.empty())
	{
		return MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession, true, parameters.size());
	}

	if (parameters.size() > kMaxHashFilesPerSession)
	{
		return MakeFileLoadOutcome(FileLoadResult::RejectedOverLimit, 0, kMaxHashFilesPerSession, true, parameters.size());
	}

	ClearFilePaths();
	ReplaceTrimmedThreadDataInputFiles(*m_threadData, parameters);

	return HasThreadDataInputFiles(*m_threadData)
		? MakeFileLoadOutcome(FileLoadResult::Success, GetThreadDataFileCount(*m_threadData), kMaxHashFilesPerSession, true, parameters.size())
		: MakeFileLoadOutcome(FileLoadResult::Empty, 0, kMaxHashFilesPerSession, true, parameters.size());
}

size_t FilesHashInputController::GetCopyDataCommandCharLimit()
{
	return COPYDATA_COMMAND_CHAR_LIMIT;
}

bool FilesHashInputController::CopyDraggedPath(HDROP hDropInfo, UINT index, sunjwbase::tstring& tstrPath)
{
	UINT cchPath = DragQueryFile(hDropInfo, index, NULL, 0);
	if (cchPath == 0)
	{
		return false;
	}

	std::vector<TCHAR> pathBuffer(cchPath + 1, 0);
	if (DragQueryFile(hDropInfo, index, pathBuffer.data(), cchPath + 1) == 0)
	{
		return false;
	}

	tstrPath.assign(pathBuffer.data());
	return true;
}

bool FilesHashInputController::IsValidCopyDataString(const COPYDATASTRUCT* pCopyDataStruct)
{
	if (pCopyDataStruct == NULL || pCopyDataStruct->lpData == NULL)
	{
		return false;
	}

	if (pCopyDataStruct->cbData < sizeof(TCHAR) ||
		(pCopyDataStruct->cbData % sizeof(TCHAR)) != 0)
	{
		return false;
	}

	size_t charCount = pCopyDataStruct->cbData / sizeof(TCHAR);
	if (charCount == 0 || charCount > GetCopyDataCommandCharLimit())
	{
		return false;
	}

	const TCHAR* szData = static_cast<const TCHAR*>(pCopyDataStruct->lpData);
	if (szData[charCount - 1] != _T('\0'))
	{
		return false;
	}

	bool sawTerminator = false;
	for (size_t i = 0; i < charCount; ++i)
	{
		if (szData[i] == _T('\0'))
		{
			sawTerminator = true;
			continue;
		}

		if (sawTerminator)
		{
			return false;
		}
	}

	return sawTerminator;
}

TStrVector FilesHashInputController::ParseFilesCmdLine(LPTSTR filesCmdLine)
{
	TStrVector parameters;
	if (filesCmdLine == NULL || filesCmdLine[0] == _T('\0'))
	{
		return parameters;
	}

#if defined(UNICODE) || defined(_UNICODE)
	int argc = 0;
	LPWSTR* argv = CommandLineToArgvW(filesCmdLine, &argc);
	if (argv == NULL)
	{
		return parameters;
	}

	for (int i = 0; i < argc; ++i)
	{
		if (argv[i] != NULL && argv[i][0] != L'\0')
		{
			parameters.push_back(argv[i]);
		}
	}
	LocalFree(argv);
#else
	std::wstring wstrCmdLine = strtowstr(std::string(filesCmdLine));
	int argc = 0;
	LPWSTR* argv = CommandLineToArgvW(wstrCmdLine.c_str(), &argc);
	if (argv == NULL)
	{
		return parameters;
	}

	for (int i = 0; i < argc; ++i)
	{
		if (argv[i] != NULL && argv[i][0] != L'\0')
		{
			parameters.push_back(wstrtostr(argv[i]));
		}
	}
	LocalFree(argv);
#endif

	return parameters;
}


void FilesHashInputController::ClearFilePaths()
{
	if (m_threadData == NULL)
	{
		return;
	}

	ResetThreadDataInputFiles(*m_threadData);
}
