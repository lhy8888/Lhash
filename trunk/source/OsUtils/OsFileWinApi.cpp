/*
 * OsFile Windows API implementation file
 * Author: Sun Junwen
 * Version: 0.5
 * Provider basic open/close, read, write and
 * attributes functions of file.
 */
#include "stdafx.h"

#include "OsFile.h"

#include <vector>
#include <Windows.h>
#include <strsafe.h>

#include "WinCommon/WinHandleGuard.h"

using namespace sunjwbase;

#define WINBOOL_2_CBOOL(winbool_var) bool((winbool_var) == TRUE)

// HANDLE == void *

static tstring LongPathFix(const tstring& tstrPath)
{
	tstring tstrFixPath;
	size_t pathLen = tstrPath.size();
	if (pathLen < MAX_PATH - 12)
		return tstrPath;

	if (tstrPath[0] == TEXT('\\') && tstrPath[1] == TEXT('\\'))
	{
		if (tstrPath[2] == TEXT('?')) // Already formatted
			return tstrPath;
		tstrFixPath = TEXT("\\\\?\\UNC\\");
		tstrFixPath += (tstrPath.c_str() + 2);
	}
	else
	{
		tstrFixPath = TEXT("\\\\?\\");
		tstrFixPath += tstrPath;
	}
	return tstrFixPath;
}

struct CreateFileFlag
{
	DWORD dwDesiredAccess;
	DWORD dwShareMode;
	DWORD dwCreationDisposition;
	DWORD dwFlagsAndAttributes;
};

static void CopyOpenErrorText(TCHAR *errorBuffer, const tstring& errorText)
{
	if (errorBuffer == NULL)
	{
		return;
	}

#if defined(UNICODE) || defined(_UNICODE)
	wcscpy_s(errorBuffer, OsFile::ERR_MSG_BUFFER_LEN, errorText.c_str());
#else
	strcpy_s(errorBuffer, OsFile::ERR_MSG_BUFFER_LEN, errorText.c_str());
#endif
}

static bool IsPathSeparator(TCHAR character)
{
	return character == TEXT('\\') || character == TEXT('/');
}

static size_t GetPathRootLength(const tstring& filePath)
{
	if (filePath.length() >= 8 &&
		filePath[0] == TEXT('\\') &&
		filePath[1] == TEXT('\\') &&
		filePath[2] == TEXT('?') &&
		filePath[3] == TEXT('\\') &&
		(filePath[4] == TEXT('U') || filePath[4] == TEXT('u')) &&
		(filePath[5] == TEXT('N') || filePath[5] == TEXT('n')) &&
		(filePath[6] == TEXT('C') || filePath[6] == TEXT('c')) &&
		filePath[7] == TEXT('\\'))
	{
		size_t serverSeparator = filePath.find_first_of(TEXT("\\/"), 8);
		if (serverSeparator == tstring::npos)
		{
			return filePath.length();
		}

		size_t shareSeparator = filePath.find_first_of(TEXT("\\/"), serverSeparator + 1);
		return shareSeparator == tstring::npos ? filePath.length() : shareSeparator;
	}

	if (filePath.length() >= 7 &&
		filePath[0] == TEXT('\\') &&
		filePath[1] == TEXT('\\') &&
		filePath[2] == TEXT('?') &&
		filePath[3] == TEXT('\\') &&
		filePath[5] == TEXT(':') &&
		IsPathSeparator(filePath[6]))
	{
		return 7;
	}

	if (filePath.length() >= 3 &&
		filePath[1] == TEXT(':') &&
		IsPathSeparator(filePath[2]))
	{
		return 3;
	}

	if (filePath.length() >= 2 &&
		filePath[0] == TEXT('\\') &&
		filePath[1] == TEXT('\\'))
	{
		size_t serverSeparator = filePath.find_first_of(TEXT("\\/"), 2);
		if (serverSeparator == tstring::npos)
		{
			return filePath.length();
		}

		size_t shareSeparator = filePath.find_first_of(TEXT("\\/"), serverSeparator + 1);
		return shareSeparator == tstring::npos ? filePath.length() : shareSeparator;
	}

	return 0;
}

static bool PathSegmentHasReparsePoint(const tstring& pathSegment)
{
	DWORD attributes = GetFileAttributes(pathSegment.c_str());
	return attributes != INVALID_FILE_ATTRIBUTES &&
		(attributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0;
}

static bool HasReparsePointInPathHierarchy(const tstring& filePath)
{
	size_t rootLength = GetPathRootLength(filePath);
	size_t segmentStart = rootLength;

	while (segmentStart < filePath.length())
	{
		size_t segmentEnd = filePath.find_first_of(TEXT("\\/"), segmentStart);
		tstring candidatePath = segmentEnd == tstring::npos ? filePath : filePath.substr(0, segmentEnd);
		if (candidatePath.length() > rootLength &&
			PathSegmentHasReparsePoint(candidatePath))
		{
			return true;
		}

		if (segmentEnd == tstring::npos)
		{
			break;
		}

		segmentStart = segmentEnd + 1;
	}

	return false;
}

static bool TryRejectReparsePointPath(const tstring& filePath, TCHAR *errorBuffer)
{
	if (!HasReparsePointInPathHierarchy(filePath))
	{
		return false;
	}

	CopyOpenErrorText(errorBuffer, TEXT("Refusing to hash a symbolic link, junction, mount point, or other reparse point."));
	return true;
}

static tstring NormalizePathForHandleComparison(tstring filePath)
{
	for (size_t index = 0; index < filePath.length(); ++index)
	{
		if (filePath[index] == TEXT('/'))
		{
			filePath[index] = TEXT('\\');
		}
	}

	if (filePath.length() >= 8 &&
		filePath.compare(0, 8, TEXT("\\\\?\\UNC\\")) == 0)
	{
		filePath = TEXT("\\\\") + filePath.substr(8);
	}
	else if (filePath.length() >= 4 &&
		filePath.compare(0, 4, TEXT("\\\\?\\")) == 0)
	{
		filePath = filePath.substr(4);
	}

	for (size_t index = 0; index < filePath.length(); ++index)
	{
		filePath[index] = static_cast<TCHAR>(_totlower(filePath[index]));
	}

	return filePath;
}

static bool TryGetNormalizedFinalPathFromHandle(HANDLE fileHandle, tstring *finalPath)
{
	if (fileHandle == NULL || fileHandle == INVALID_HANDLE_VALUE || finalPath == NULL)
	{
		return false;
	}

	DWORD requiredLength = GetFinalPathNameByHandle(
		fileHandle,
		NULL,
		0,
		FILE_NAME_NORMALIZED | VOLUME_NAME_DOS);
	if (requiredLength == 0)
	{
		return false;
	}

	std::vector<TCHAR> finalPathBuffer(requiredLength + 1, TEXT('\0'));
	DWORD actualLength = GetFinalPathNameByHandle(
		fileHandle,
		finalPathBuffer.data(),
		static_cast<DWORD>(finalPathBuffer.size()),
		FILE_NAME_NORMALIZED | VOLUME_NAME_DOS);
	if (actualLength == 0 || actualLength >= finalPathBuffer.size())
	{
		return false;
	}

	*finalPath = NormalizePathForHandleComparison(tstring(finalPathBuffer.data(), actualLength));
	return true;
}

static bool IsOpenedHandleReparsePoint(HANDLE fileHandle)
{
	FILE_ATTRIBUTE_TAG_INFO attributeTagInfo = { 0 };
	if (!GetFileInformationByHandleEx(
		fileHandle,
		FileAttributeTagInfo,
		&attributeTagInfo,
		static_cast<DWORD>(sizeof(attributeTagInfo))))
	{
		return false;
	}

	return (attributeTagInfo.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0;
}

static bool ValidateOpenedHandleAgainstPathPolicy(HANDLE fileHandle, const tstring& expectedPath, TCHAR *errorBuffer)
{
	if (IsOpenedHandleReparsePoint(fileHandle))
	{
		CopyOpenErrorText(errorBuffer, TEXT("Refusing to hash a symbolic link, junction, mount point, or other reparse point."));
		return false;
	}

	tstring normalizedFinalPath;
	if (!TryGetNormalizedFinalPathFromHandle(fileHandle, &normalizedFinalPath))
	{
		return true;
	}

	tstring normalizedExpectedPath = NormalizePathForHandleComparison(expectedPath);
	if (normalizedFinalPath != normalizedExpectedPath)
	{
		CopyOpenErrorText(errorBuffer, TEXT("Refusing to hash a path whose resolved handle no longer matches the validated path."));
		return false;
	}

	return true;
}

OsFile::OsFile(tstring filePath):
	_filePath(LongPathFix(filePath)),
	_osfileData(NULL),
	_fileStatus(CLOSED)
{

}

OsFile::~OsFile()
{
	if (_osfileData != NULL)
	{
		if (_fileStatus != CLOSED)
		{
			close();
		}
	}
}

bool OsFile::isHashTargetAllowed(void *exception)
{
	return !TryRejectReparsePointPath(_filePath, (TCHAR *)exception);
}

bool OsFile::open(void *flag, void *exception)
{
	CreateFileFlag* fileFlag = (CreateFileFlag*)flag;
	TCHAR *pFileExc = (TCHAR *)exception;
	_osfileData = NULL;
	if (!isHashTargetAllowed(exception))
	{
		return false;
	}

	HANDLE openedHandle = INVALID_HANDLE_VALUE;
#if defined (FHASH_UWP_LIB)
	openedHandle = CreateFileFromAppW(_filePath.c_str(), // file to open
		fileFlag->dwDesiredAccess, // open for reading
		fileFlag->dwShareMode, // share for reading
		NULL, // default security
		fileFlag->dwCreationDisposition, // existing file only
		fileFlag->dwFlagsAndAttributes, // normal file
		NULL); // no attr. template
#else
	openedHandle = CreateFile(_filePath.c_str(), // file to open
		fileFlag->dwDesiredAccess, // open for reading
		fileFlag->dwShareMode, // share for reading
		NULL, // default security
		fileFlag->dwCreationDisposition, // existing file only
		fileFlag->dwFlagsAndAttributes, // normal file
		NULL); // no attr. template
#endif

	if (openedHandle == INVALID_HANDLE_VALUE)
	{
		if (pFileExc != NULL)
		{
			DWORD dw = GetLastError();
			LPVOID lpMsgBuf = NULL;
			FormatMessage(
				FORMAT_MESSAGE_ALLOCATE_BUFFER |
				FORMAT_MESSAGE_FROM_SYSTEM |
				FORMAT_MESSAGE_IGNORE_INSERTS,
				NULL,
				dw,
				0, //MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_US), // MAKELANGID(LANG_FRENCH, SUBLANG_FRENCH),
				(LPTSTR)&lpMsgBuf,
				0, NULL);

			tstring tstrErrMsg(TEXT("Cannot open this file."));
			if (lpMsgBuf != NULL)
			{
				tstrErrMsg = (LPCTSTR)lpMsgBuf;
				LocalFree(lpMsgBuf);
			}
			tstrErrMsg = strtrim(tstrErrMsg);

#if defined(UNICODE) || defined(_UNICODE)
			wcscpy_s(pFileExc, OsFile::ERR_MSG_BUFFER_LEN, tstrErrMsg.c_str());
#else
			strcpy_s(pFileExc, OsFile::ERR_MSG_BUFFER_LEN, tstrErrMsg.c_str());
#endif
		}
	}
	else
	{
		WinHandleGuard::UniqueWinHandle validatedHandle(openedHandle);
		if (!ValidateOpenedHandleAgainstPathPolicy(validatedHandle.get(), _filePath, pFileExc))
		{
			return false;
		}

		_osfileData = validatedHandle.release();
	}

	return (_osfileData != NULL);
}

bool OsFile::openRead(void *exception/* = NULL*/)
{
	bool ret = false;

	CreateFileFlag fileFlag;
	fileFlag.dwDesiredAccess = GENERIC_READ;
	fileFlag.dwShareMode = FILE_SHARE_READ;
	fileFlag.dwCreationDisposition = OPEN_EXISTING;
	fileFlag.dwFlagsAndAttributes = FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OPEN_REPARSE_POINT;
	ret = this->open((void *)&fileFlag, exception);

	if (ret == true)
	{
		_fileStatus = OPEN_READ;
	}

	return ret;
}

bool OsFile::openReadScan(void *exception/* = NULL*/)
{
	bool ret = false;

	CreateFileFlag fileFlag;
	fileFlag.dwDesiredAccess = GENERIC_READ;
	fileFlag.dwShareMode = FILE_SHARE_READ;
	fileFlag.dwCreationDisposition = OPEN_EXISTING;
	fileFlag.dwFlagsAndAttributes = FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN | FILE_FLAG_OPEN_REPARSE_POINT;
	ret = this->open((void*)&fileFlag, exception);

	if (ret == true)
	{
		_fileStatus = OPEN_READ;
	}

	return ret;
}

bool OsFile::openWrite(void *exception/* = NULL*/)
{
	bool ret = false;

	CreateFileFlag fileFlag;
	fileFlag.dwDesiredAccess = GENERIC_WRITE;
	fileFlag.dwShareMode = 0;
	fileFlag.dwCreationDisposition = CREATE_NEW;
	fileFlag.dwFlagsAndAttributes = FILE_ATTRIBUTE_NORMAL;
	ret = this->open((void*)&fileFlag, exception);

	if (ret == true)
	{
		_fileStatus = OPEN_WRITE;
	}

	return ret;
}

bool OsFile::openReadWrite(void *exception/* = NULL*/)
{
	bool ret = false;

	CreateFileFlag fileFlag;
	fileFlag.dwDesiredAccess = GENERIC_WRITE | GENERIC_READ;
	fileFlag.dwShareMode = 0;
	fileFlag.dwCreationDisposition = CREATE_NEW;
	fileFlag.dwFlagsAndAttributes = FILE_ATTRIBUTE_NORMAL;
	ret = this->open((void*)&fileFlag, exception);

	if (ret == true)
	{
		_fileStatus = OPEN_READWRITE;
	}

	return ret;
}

int64_t OsFile::getLength()
{
	int64_t retLength = 0;

	bool needClose = false;

	// Not opened, let's open it.
	if (_fileStatus == CLOSED && openRead())
	{
		needClose = true;
	}

	if (_fileStatus != CLOSED)
	{
		LARGE_INTEGER liSize = { 0 };
		if (GetFileSizeEx(_osfileData, &liSize))
		{
			retLength = liSize.QuadPart;
		}
	}

	if (needClose)
	{
		close();
	}

	return retLength;
}

bool OsFile::getModifiedTime(void *modifiedTime)
{
	FILETIME *lpFtWrite = (FILETIME *)modifiedTime;

	bool needClose = false;

	// Not opened, let's open it.
	if (_fileStatus == CLOSED && openRead())
	{
		needClose = true;
	}

	BOOL ret = FALSE;
	if (_fileStatus != CLOSED)
	{
		ret = GetFileTime(_osfileData, NULL, NULL, lpFtWrite);
	}

	if (needClose)
	{
		close();
	}

	return WINBOOL_2_CBOOL(ret);
}

tstring OsFile::getModifiedTimeFormat()
{
	tstring tstrLastModifiedTime;

	FILETIME ftWrite;
	if (this->getModifiedTime((void*)&ftWrite))
	{
		// Convert the last-write time to local time.
		SYSTEMTIME stUTC, stLocal;
		FileTimeToSystemTime(&ftWrite, &stUTC);
		SystemTimeToTzSpecificLocalTime(NULL, &stUTC, &stLocal);

		TCHAR tzTmBuf[1024] = { 0 };
		StringCchPrintf(tzTmBuf, 1024,
			TEXT("%d-%02d-%02d %02d:%02d"),
			stLocal.wYear, stLocal.wMonth, stLocal.wDay,
			stLocal.wHour, stLocal.wMinute);
		tstrLastModifiedTime = tzTmBuf;
	}

	return tstrLastModifiedTime;
}

uint64_t OsFile::seek(uint64_t offset, OsFileSeekFrom from)
{
	DWORD dwMoveMethod = FILE_BEGIN;
	switch (from)
	{
	case OF_SEEK_BEGIN:
		dwMoveMethod = FILE_BEGIN;
		break;
	case OF_SEEK_CUR:
		dwMoveMethod = FILE_CURRENT;
		break;
	case OF_SEEK_END:
		dwMoveMethod = FILE_END;
		break;
	}

	// Open first, we don't check here.
	LARGE_INTEGER liDistance = { 0 };
	liDistance.QuadPart = offset;
	LARGE_INTEGER liNewPos = { 0 };
	SetFilePointerEx(_osfileData, liDistance, &liNewPos, dwMoveMethod);

	return liNewPos.QuadPart;
}

int64_t OsFile::read(void *readBuffer, uint32_t bytes)
{
	// Open first, we don't check here.
	DWORD dwNumberOfBytesRead = 0;
	if (!ReadFile(_osfileData, readBuffer, bytes, &dwNumberOfBytesRead, NULL))
	{
		return -1;
	}

	return dwNumberOfBytesRead;
}

int64_t OsFile::write(void *writeBuffer, uint32_t bytes)
{
	// Open first, we don't check here.
	DWORD lpNumberOfBytesWritten = 0;
	if (!WriteFile(_osfileData, writeBuffer, bytes, &lpNumberOfBytesWritten, NULL))
	{
		return -1;
	}

	return lpNumberOfBytesWritten;
}

void OsFile::close()
{
	if (_fileStatus != CLOSED)
	{
		WinHandleGuard::UniqueWinHandle fileHandle(reinterpret_cast<HANDLE>(_osfileData));
		_osfileData = NULL;
		_fileStatus = CLOSED;
	}
}
