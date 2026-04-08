#include "stdafx.h"

#include "UIBridgeMFC.h"

#include <stdlib.h>
#include <stdio.h>
#include <string>
#include <Windows.h>

#include "Common/strhelper.h"
#include "Common/Global.h"
#include "Common/ResultDataRender.h"
#include "Common/ResultDigestRender.h"
#include "Common/Utils.h"
#include "WindowsUtils.h"
#include "WinCommon/WindowsStrings.h"

using namespace std;
using namespace sunjwbase;

UIBridgeMFC::UIBridgeMFC(HWND hWnd,
						 OsMutex *mainMtx,
						 CHyperEditHash *hyperEdit)
:m_hWnd(hWnd), m_mainMtx(mainMtx), m_mainHyperEdit(hyperEdit)
{
}

UIBridgeMFC::~UIBridgeMFC()
{
}

void UIBridgeMFC::lockBridgeData()
{
	m_mainMtx->lock();
}

void UIBridgeMFC::unlockBridgeData()
{
	m_mainMtx->unlock();
}

void UIBridgeMFC::handleJobPreparingEvent()
{
	PostThreadInfoMessage(WP_WORKING);

	UpdateMainHyperEdit([&](CHyperEditHash *hyperEdit)
	{
		m_preparingSnapshot = CaptureHyperEditSnapshot(hyperEdit);
		if (m_preparingSnapshot.text == tstring(GetStringByKey(MAINDLG_INITINFO)))
		{
			// Initial state
			m_preparingSnapshot.text = _T("");
			m_preparingSnapshot.linkOffsets.clear();
			hyperEdit->ClearTextBuffer();
		}

		AppendTextLineToHyperEdit(GetStringByKey(MAINDLG_WAITING_START), hyperEdit);
	}, true);
}

void UIBridgeMFC::handleJobPreparationFinishedEvent()
{
	UpdateMainHyperEdit([&](CHyperEditHash *hyperEdit)
	{
		// Restore and remove MAINDLG_WAITING_START
		RestoreHyperEditSnapshot(m_preparingSnapshot, hyperEdit);
	});
}

void UIBridgeMFC::handleJobCancelledEvent()
{
	PostThreadInfoMessage(WP_STOPPED);
}

void UIBridgeMFC::handleJobCompletedEvent()
{
	PostThreadInfoMessage(WP_FINISHED);
}

void UIBridgeMFC::handleFileResultProgressEvent(const HashResult& result,
													ProgressEventType eventType,
													bool uppercaseDigest)
{
	switch (eventType)
	{
	case PROGRESS_EVENT_FILE_STARTED:
		AppendResultSectionAndRefresh(result, RESULT_RENDER_SECTION_FILE_NAME, false);
		break;
	case PROGRESS_EVENT_FILE_META_READY:
		AppendResultSectionAndRefresh(result, RESULT_RENDER_SECTION_META, false);
		break;
	case PROGRESS_EVENT_FILE_HASH_READY:
		AppendResultSectionAndRefresh(result, RESULT_RENDER_SECTION_HASH, uppercaseDigest);
		break;
	case PROGRESS_EVENT_FILE_FAILED:
		AppendResultSectionAndRefresh(result, RESULT_RENDER_SECTION_ERROR, false);
		break;
	default:
		break;
	}
}

int UIBridgeMFC::getProgressValueMax()
{
	return 100;
}

void UIBridgeMFC::handleFileProgressEvent(int value)
{
	//::PostMessage(m_hWnd, WM_THREAD_INFO, WP_PROG, value);
}

void UIBridgeMFC::handleTotalProgressEvent(int value)
{
	PostThreadInfoMessage(WP_PROG_WHOLE, value);
}

void UIBridgeMFC::handleFileCalculatedEvent()
{
}

void UIBridgeMFC::handleFileFinishedEvent()
{
}

void UIBridgeMFC::AppendLineBreakToHyperEdit(CHyperEditHash *hyerEdit)
{
	hyerEdit->AppendTextToBuffer(_T("\r\n"));
}

void UIBridgeMFC::AppendTextLineToHyperEdit(const tstring& text,
											CHyperEditHash *hyerEdit)
{
	hyerEdit->AppendTextToBuffer(text.c_str());
	AppendLineBreakToHyperEdit(hyerEdit);
}

void UIBridgeMFC::AppendLabelValueToHyperEdit(const TCHAR *label,
												const tstring& value,
												CHyperEditHash *hyerEdit)
{
	hyerEdit->AppendTextToBuffer(label);
	hyerEdit->AppendTextToBuffer(_T(" "));
	hyerEdit->AppendTextToBuffer(value.c_str());
}

void UIBridgeMFC::AppendLabelValueLineToHyperEdit(const TCHAR *label,
													const tstring& value,
													CHyperEditHash *hyerEdit)
{
	AppendLabelValueToHyperEdit(label, value, hyerEdit);
	AppendLineBreakToHyperEdit(hyerEdit);
}

void UIBridgeMFC::AppendLabelLinkLineToHyperEdit(const TCHAR *label,
												const tstring& value,
												CHyperEditHash *hyerEdit)
{
	hyerEdit->AppendTextToBuffer(label);
	hyerEdit->AppendTextToBuffer(_T(": "));
	hyerEdit->AppendLinkToBuffer(value.c_str());
	AppendLineBreakToHyperEdit(hyerEdit);
}

UIBridgeMFC::MainHyperEditSnapshot UIBridgeMFC::CaptureHyperEditSnapshot(CHyperEditHash *hyperEdit)
{
	MainHyperEditSnapshot snapshot;
	snapshot.text = hyperEdit->GetTextBuffer().GetBuffer();
	hyperEdit->CopyLinkOffsets(snapshot.linkOffsets);
	return snapshot;
}

void UIBridgeMFC::RestoreHyperEditSnapshot(const MainHyperEditSnapshot& snapshot,
											CHyperEditHash *hyperEdit)
{
	hyperEdit->ClearTextBuffer();
	hyperEdit->AppendTextToBuffer(snapshot.text.c_str());
	hyperEdit->SetLinkOffsets(snapshot.linkOffsets);
}

UIBridgeMFC::ResultMetaLineDisplayInfo UIBridgeMFC::GetResultMetaLineDisplayInfo(const ResultData& result,
																				ResultMetaLineType metaLine)
{
	ResultMetaLineDisplayInfo metaLineDisplayInfo;

	DispatchResultMetaLineByType(metaLine, [&]()
	{
		ResultSizeDisplayInfo resultSizeDisplayInfo = GetResultSizeDisplayInfo(result);

		metaLineDisplayInfo.label = GetStringByKey(FILESIZE_STRING);
		metaLineDisplayInfo.value = resultSizeDisplayInfo.sizeText;
		metaLineDisplayInfo.suffix = _T(" ");
		metaLineDisplayInfo.suffix += GetStringByKey(BYTE_STRING);
		if (resultSizeDisplayInfo.shortSizeText.length() > 0)
		{
			metaLineDisplayInfo.suffix += _T(" (");
			metaLineDisplayInfo.suffix += resultSizeDisplayInfo.shortSizeText;
			metaLineDisplayInfo.suffix += _T(")");
		}
	}, [&]()
	{
		metaLineDisplayInfo.label = GetStringByKey(MODIFYTIME_STRING);
		metaLineDisplayInfo.value = GetResultModifiedDate(result);
	}, [&]()
	{
		metaLineDisplayInfo.label = GetStringByKey(VERSION_STRING);
		metaLineDisplayInfo.value = GetResultVersion(result);
	});

	return metaLineDisplayInfo;
}

UIBridgeMFC::ResultMetaLineDisplayInfo UIBridgeMFC::GetHashResultMetaLineDisplayInfo(const HashResult& result,
																					ResultMetaLineType metaLine)
{
	ResultMetaLineDisplayInfo metaLineDisplayInfo;

	DispatchResultMetaLineByType(metaLine, [&]()
	{
		ResultSizeDisplayInfo resultSizeDisplayInfo = GetHashResultSizeDisplayInfo(result);

		metaLineDisplayInfo.label = GetStringByKey(FILESIZE_STRING);
		metaLineDisplayInfo.value = resultSizeDisplayInfo.sizeText;
		metaLineDisplayInfo.suffix = _T(" ");
		metaLineDisplayInfo.suffix += GetStringByKey(BYTE_STRING);
		if (resultSizeDisplayInfo.shortSizeText.length() > 0)
		{
			metaLineDisplayInfo.suffix += _T(" (");
			metaLineDisplayInfo.suffix += resultSizeDisplayInfo.shortSizeText;
			metaLineDisplayInfo.suffix += _T(")");
		}
	}, [&]()
	{
		metaLineDisplayInfo.label = GetStringByKey(MODIFYTIME_STRING);
		metaLineDisplayInfo.value = result.meta.modifiedDate;
	}, [&]()
	{
		metaLineDisplayInfo.label = GetStringByKey(VERSION_STRING);
		metaLineDisplayInfo.value = result.meta.version;
	});

	return metaLineDisplayInfo;
}

void UIBridgeMFC::AppendResultMetaLineDisplayInfoToHyperEdit(const ResultMetaLineDisplayInfo& metaLineDisplayInfo,
															CHyperEditHash *hyerEdit)
{
	AppendLabelValueToHyperEdit(metaLineDisplayInfo.label.c_str(),
								metaLineDisplayInfo.value,
								hyerEdit);
	hyerEdit->AppendTextToBuffer(metaLineDisplayInfo.suffix.c_str());
}

void UIBridgeMFC::AppendResultDigestDisplayInfoToHyperEdit(const ResultDigestDisplayInfo& digestDisplayInfo,
															CHyperEditHash *hyerEdit)
{
	AppendLabelLinkLineToHyperEdit(digestDisplayInfo.label.c_str(),
									digestDisplayInfo.value,
									hyerEdit);
}

void UIBridgeMFC::AppendResultMetaLineToHyperEdit(const ResultData& result,
												ResultMetaLineType metaLine,
												CHyperEditHash *hyerEdit)
{
	AppendResultMetaLineDisplayInfoToHyperEdit(GetResultMetaLineDisplayInfo(result, metaLine), hyerEdit);
}

void UIBridgeMFC::AppendFileNameToHyperEdit(const ResultData& result,
											CHyperEditHash *hyerEdit)
{
	AppendLabelValueLineToHyperEdit(GetStringByKey(FILENAME_STRING),
									GetResultPath(result),
									hyerEdit);
}

void UIBridgeMFC::AppendFileNameToHyperEdit(const HashResult& result,
											CHyperEditHash *hyerEdit)
{
	AppendLabelValueLineToHyperEdit(GetStringByKey(FILENAME_STRING),
									result.path,
									hyerEdit);
}

void UIBridgeMFC::AppendFileMetaToHyperEdit(const ResultData& result,
											CHyperEditHash *hyerEdit)
{
	bool appendLineBreak = false;
	VisitRenderableResultMetaLineDisplayInfos(result, [&](ResultMetaLineType metaLine, const ResultMetaLineDisplayInfo& metaLineDisplayInfo)
	{
		(void)metaLine;
		if (appendLineBreak)
		{
			AppendLineBreakToHyperEdit(hyerEdit);
		}

		AppendResultMetaLineDisplayInfoToHyperEdit(metaLineDisplayInfo, hyerEdit);
		appendLineBreak = true;
		return true;
	});
	AppendLineBreakToHyperEdit(hyerEdit);
}

void UIBridgeMFC::AppendFileMetaToHyperEdit(const HashResult& result,
											CHyperEditHash *hyerEdit)
{
	bool appendLineBreak = false;
	VisitRenderableHashResultMetaLines(result, [&](ResultMetaLineType metaLine)
	{
		if (appendLineBreak)
		{
			AppendLineBreakToHyperEdit(hyerEdit);
		}

		AppendResultMetaLineDisplayInfoToHyperEdit(GetHashResultMetaLineDisplayInfo(result, metaLine), hyerEdit);
		appendLineBreak = true;
		return true;
	});
	AppendLineBreakToHyperEdit(hyerEdit);
}

void UIBridgeMFC::AppendFileHashToHyperEdit(const ResultData& result,
											bool uppercase,
											CHyperEditHash *hyerEdit)
{
	VisitResultDigestDisplayValues(result, uppercase, [&](int index, const ResultDigestMetadata& digestMetadata, const ResultDigestDisplayInfo& digestDisplayInfo)
	{
		(void)index;
		(void)digestMetadata;
		AppendResultDigestDisplayInfoToHyperEdit(digestDisplayInfo, hyerEdit);
		return true;
	});
	AppendLineBreakToHyperEdit(hyerEdit);
}

void UIBridgeMFC::AppendFileHashToHyperEdit(const HashResult& result,
											bool uppercase,
											CHyperEditHash *hyerEdit)
{
	VisitHashResultDigestDisplayValues(result, uppercase, [&](int index, const HashDigestResult& digestResult, const ResultDigestDisplayInfo& digestDisplayInfo)
	{
		(void)index;
		(void)digestResult;
		AppendResultDigestDisplayInfoToHyperEdit(digestDisplayInfo, hyerEdit);
		return true;
	});
	AppendLineBreakToHyperEdit(hyerEdit);
}

void UIBridgeMFC::AppendFileErrToHyperEdit(const ResultData& result,
											CHyperEditHash *hyerEdit)
{
	AppendTextLineToHyperEdit(GetResultError(result), hyerEdit);
	AppendLineBreakToHyperEdit(hyerEdit);
}

void UIBridgeMFC::AppendFileErrToHyperEdit(const HashResult& result,
											CHyperEditHash *hyerEdit)
{
	AppendTextLineToHyperEdit(result.error, hyerEdit);
	AppendLineBreakToHyperEdit(hyerEdit);
}

void UIBridgeMFC::AppendResultRenderSectionToHyperEdit(const ResultData& result,
													ResultRenderSectionType renderSection,
													bool uppercase,
													CHyperEditHash *hyerEdit)
{
	DispatchResultRenderSectionByType(renderSection, [&]()
	{
		AppendFileNameToHyperEdit(result, hyerEdit);
	}, [&]()
	{
		AppendFileMetaToHyperEdit(result, hyerEdit);
	}, [&]()
	{
		AppendFileHashToHyperEdit(result, uppercase, hyerEdit);
	}, [&]()
	{
		AppendFileErrToHyperEdit(result, hyerEdit);
	});
}

void UIBridgeMFC::AppendResultRenderSectionToHyperEdit(const HashResult& result,
													ResultRenderSectionType renderSection,
													bool uppercase,
													CHyperEditHash *hyerEdit)
{
	DispatchResultRenderSectionByType(renderSection, [&]()
	{
		AppendFileNameToHyperEdit(result, hyerEdit);
	}, [&]()
	{
		AppendFileMetaToHyperEdit(result, hyerEdit);
	}, [&]()
	{
		AppendFileHashToHyperEdit(result, uppercase, hyerEdit);
	}, [&]()
	{
		AppendFileErrToHyperEdit(result, hyerEdit);
	});
}

void UIBridgeMFC::AppendResultToHyperEdit(const ResultData& result,
											bool uppercase,
											CHyperEditHash *hyerEdit)
{
	ResultState resultState = GetResultState(result);
	if (IsResultStateNone(resultState))
		return;

	VisitRenderableResultSections(resultState, [&](ResultRenderSectionType renderSection)
	{
		AppendResultRenderSectionToHyperEdit(result, renderSection, uppercase, hyerEdit);
		return true;
	});

	if (ShouldAppendResultTrailingLineBreak(resultState))
	{
		AppendLineBreakToHyperEdit(hyerEdit);
	}
}

void UIBridgeMFC::AppendResultToHyperEdit(const HashResult& result,
											bool uppercase,
											CHyperEditHash *hyerEdit)
{
	if (IsResultStateNone(result.state))
		return;

	VisitRenderableResultSections(result.state, [&](ResultRenderSectionType renderSection)
	{
		AppendResultRenderSectionToHyperEdit(result, renderSection, uppercase, hyerEdit);
		return true;
	});

	if (ShouldAppendResultTrailingLineBreak(result.state))
	{
		AppendLineBreakToHyperEdit(hyerEdit);
	}
}
