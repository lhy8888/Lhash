#include "..\..\trunk\source\stdafx.h"

#include <cstdlib>
#include <sstream>
#include <vector>

#include "NativeTestHarness.h"

#include "OsUtils/OsFile.h"
#include "WinCommon/WinHandleGuard.h"

namespace
{
	class ScopedSecurityTempDirectory
	{
	public:
		ScopedSecurityTempDirectory()
		{
			TCHAR tempPath[MAX_PATH] = { 0 };
			DWORD copied = ::GetTempPath(MAX_PATH, tempPath);
			NativeAssertTrue(copied > 0 && copied < MAX_PATH, "Unable to locate the Windows temp directory.");

			sunjwbase::tstring rootPath(tempPath);
			if (!rootPath.empty() && rootPath[rootPath.length() - 1] == _T('\\'))
			{
				rootPath.erase(rootPath.length() - 1);
			}

			rootPath += _T("\\lhash-native-security-tests");
			::CreateDirectory(rootPath.c_str(), NULL);

			std::basic_ostringstream<TCHAR> pathBuilder;
			pathBuilder << rootPath << _T("\\run-") << ::GetCurrentProcessId() << _T('-') << ::GetTickCount64();
			rootPath_ = pathBuilder.str();

			NativeAssertTrue(::CreateDirectory(rootPath_.c_str(), NULL) == TRUE, "Unable to create the native security test root directory.");
		}

		~ScopedSecurityTempDirectory()
		{
			for (size_t fileIndex = 0; fileIndex < files_.size(); ++fileIndex)
			{
				::DeleteFile(files_[fileIndex].c_str());
			}

			for (size_t junctionIndex = 0; junctionIndex < junctions_.size(); ++junctionIndex)
			{
				::RemoveDirectory(junctions_[junctionIndex].c_str());
			}

			for (size_t directoryIndex = 0; directoryIndex < directories_.size(); ++directoryIndex)
			{
				::RemoveDirectory(directories_[directoryIndex].c_str());
			}

			if (!rootPath_.empty())
			{
				::RemoveDirectory(rootPath_.c_str());
			}
		}

		sunjwbase::tstring CreateDirectoryPath(const sunjwbase::tstring& directoryName)
		{
			sunjwbase::tstring directoryPath = BuildPath(directoryName);
			NativeAssertTrue(::CreateDirectory(directoryPath.c_str(), NULL) == TRUE, "Unable to create the native security test subdirectory.");
			directories_.push_back(directoryPath);
			return directoryPath;
		}

		sunjwbase::tstring WriteTextFile(const sunjwbase::tstring& relativeFilePath, const std::string& contents)
		{
			sunjwbase::tstring filePath = BuildPath(relativeFilePath);
			WinHandleGuard::UniqueWinHandle fileHandle(::CreateFile(filePath.c_str(), GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL));
			NativeAssertTrue(fileHandle.isValid(), "Unable to create the native security test file.");

			DWORD bytesWritten = 0;
			BOOL writeSucceeded = ::WriteFile(fileHandle.get(), contents.data(), static_cast<DWORD>(contents.size()), &bytesWritten, NULL);
			NativeAssertTrue(writeSucceeded == TRUE, "Unable to write the native security test file.");
			NativeAssertEqual(static_cast<DWORD>(contents.size()), bytesWritten, "The native security test file was only partially written.");

			files_.push_back(filePath);
			return filePath;
		}

		void TrackJunction(const sunjwbase::tstring& junctionPath)
		{
			junctions_.push_back(junctionPath);
		}

		sunjwbase::tstring BuildPath(const sunjwbase::tstring& relativePath) const
		{
			return rootPath_ + _T("\\") + relativePath;
		}

	private:
		sunjwbase::tstring rootPath_;
		std::vector<sunjwbase::tstring> files_;
		std::vector<sunjwbase::tstring> junctions_;
		std::vector<sunjwbase::tstring> directories_;
	};

	static void CreateDirectoryJunction(const sunjwbase::tstring& junctionPath, const sunjwbase::tstring& targetPath)
	{
		sunjwbase::tstring commandLine = _T("cmd.exe /c mklink /J \"") + junctionPath + _T("\" \"") + targetPath + _T("\" >nul");
#if defined(UNICODE) || defined(_UNICODE)
		int exitCode = _wsystem(commandLine.c_str());
#else
		int exitCode = system(commandLine.c_str());
#endif
		NativeAssertEqual(0, exitCode, "Unable to create the directory junction used by the native security test.");
	}

	static void OsFile_RejectsLeafPathsNestedUnderDirectoryJunctions()
	{
		ScopedSecurityTempDirectory tempDirectory;
		sunjwbase::tstring realDirectoryPath = tempDirectory.CreateDirectoryPath(_T("real"));
		tempDirectory.WriteTextFile(_T("real\\secret.txt"), "secret");

		sunjwbase::tstring junctionPath = tempDirectory.BuildPath(_T("linked"));
		CreateDirectoryJunction(junctionPath, realDirectoryPath);
		tempDirectory.TrackJunction(junctionPath);

		sunjwbase::tstring linkedFilePath = junctionPath + _T("\\secret.txt");
		DWORD junctionAttributes = ::GetFileAttributes(junctionPath.c_str());
		NativeAssertTrue(junctionAttributes != INVALID_FILE_ATTRIBUTES &&
			(junctionAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0,
			"The directory junction should expose the reparse-point attribute.");

		DWORD leafAttributes = ::GetFileAttributes(linkedFilePath.c_str());
		NativeAssertTrue(leafAttributes != INVALID_FILE_ATTRIBUTES &&
			(leafAttributes & FILE_ATTRIBUTE_REPARSE_POINT) == 0,
			"The nested linked file should demonstrate that a leaf-only attribute check misses ancestor reparse points.");

		TCHAR openError[sunjwbase::OsFile::ERR_MSG_BUFFER_LEN] = { 0 };
		sunjwbase::OsFile osFile(linkedFilePath);
		NativeAssertTrue(!osFile.isHashTargetAllowed(openError), "OsFile should reject file paths that traverse a directory junction.");
		NativeAssertTrue(sunjwbase::tstring(openError).find(_T("reparse point")) != sunjwbase::tstring::npos,
			"OsFile should surface the reparse-point refusal message for nested junction paths.");
		NativeAssertTrue(!osFile.openReadScan(openError), "OsFile should refuse to open file paths that traverse a directory junction.");
	}

	static void OsFile_ReportsSharingViolationsForLockedFiles()
	{
		ScopedSecurityTempDirectory tempDirectory;
		sunjwbase::tstring filePath = tempDirectory.WriteTextFile(_T("locked.txt"), "locked");

		WinHandleGuard::UniqueWinHandle lockedHandle(::CreateFile(filePath.c_str(), GENERIC_READ, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL));
		NativeAssertTrue(lockedHandle.isValid(), "Unable to acquire the exclusive test lock on the security test file.");

		TCHAR openError[sunjwbase::OsFile::ERR_MSG_BUFFER_LEN] = { 0 };
		sunjwbase::OsFile osFile(filePath);
		NativeAssertTrue(osFile.isHashTargetAllowed(openError), "A regular file should remain eligible for hashing before the share-violation probe.");
		NativeAssertTrue(!osFile.openReadScan(openError), "OsFile should fail when another process holds an exclusive lock on the file.");
		NativeAssertNotEmpty(sunjwbase::tstring(openError), "OsFile should surface a non-empty system error for a sharing violation.");
	}
}

void RegisterHashEngineSecurityRuntimeTests(std::vector<NativeTestCase>& tests)
{
	tests.push_back({ "OsFile_RejectsLeafPathsNestedUnderDirectoryJunctions", &OsFile_RejectsLeafPathsNestedUnderDirectoryJunctions });
	tests.push_back({ "OsFile_ReportsSharingViolationsForLockedFiles", &OsFile_ReportsSharingViolationsForLockedFiles });
}

