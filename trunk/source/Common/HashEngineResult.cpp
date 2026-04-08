#include "stdafx.h"

#include "Common/HashEngineInternal.h"

#if defined (_WIN32)
#include <strsafe.h>
#endif

using namespace sunjwbase;

namespace HashEngineInternal
{
#if defined (_WIN32)
	static bool TryResolveWindowsPathFileMeta(const TCHAR *path, HashFileMeta *meta)
	{
		if (path == NULL || meta == NULL || path[0] == _T('\0'))
		{
			return false;
		}

		WIN32_FILE_ATTRIBUTE_DATA attributeData = { 0 };
		if (!GetFileAttributesEx(path, GetFileExInfoStandard, &attributeData))
		{
			return false;
		}

		ULARGE_INTEGER fileSize = { 0 };
		fileSize.HighPart = attributeData.nFileSizeHigh;
		fileSize.LowPart = attributeData.nFileSizeLow;
		meta->size = fileSize.QuadPart;

		SYSTEMTIME utcTime = { 0 };
		SYSTEMTIME localTime = { 0 };
		if (!FileTimeToSystemTime(&attributeData.ftLastWriteTime, &utcTime))
		{
			return true;
		}

		if (!SystemTimeToTzSpecificLocalTime(NULL, &utcTime, &localTime))
		{
			return true;
		}

		TCHAR formattedTime[1024] = { 0 };
		if (SUCCEEDED(StringCchPrintf(
			formattedTime,
			ARRAYSIZE(formattedTime),
			TEXT("%d-%02d-%02d %02d:%02d"),
			localTime.wYear,
			localTime.wMonth,
			localTime.wDay,
			localTime.wHour,
			localTime.wMinute)))
		{
			meta->modifiedDate = formattedTime;
		}

		return true;
	}
#endif

	uint64_t PrepareFileMetaResult(HashExecutionContext *executionContext, HashResult& result,
		OsFile& osFile, const TCHAR *path, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, tstring& tstrFileVersion)
	{
		uint64_t fsize = 0;
#if defined (_WIN32)
		HashFileMeta resolvedMeta;
		if (TryResolveWindowsPathFileMeta(path, &resolvedMeta))
		{
			result.meta.modifiedDate = resolvedMeta.modifiedDate;
			fsize = TrackHashResolvedFileSize(executionContext, isSizeCaled, fSizes, fileIndex, result, resolvedMeta.size);
		}
		else
#endif
		{
			result.meta.modifiedDate = osFile.getModifiedTimeFormat();
			fsize = ResolveHashFileSizeAndTrack(executionContext, osFile, isSizeCaled, fSizes, fileIndex, result);
		}

		tstrFileVersion = ResolveHashFileVersion(osFile, path);
		result.meta.version = tstrFileVersion;

		EmitMetaResult(executionContext, result);
		return fsize;
	}
}
