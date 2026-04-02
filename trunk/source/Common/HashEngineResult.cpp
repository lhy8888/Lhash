#include "stdafx.h"

#include "Common/HashEngineInternal.h"

using namespace sunjwbase;

namespace HashEngineInternal
{
	uint64_t PrepareFileMetaResult(HashExecutionContext *executionContext, HashResult& result,
		OsFile& osFile, const TCHAR *path, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, tstring& tstrFileVersion)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		result.meta.modifiedDate = osFile.getModifiedTimeFormat();

		uint64_t fsize = osFile.getLength();
		result.meta.size = fsize;

		if (!isSizeCaled)
		{
			AddHashExecutionTotalSize(*executionContext, fsize);
		}
		else
		{
			ReplaceHashExecutionCountedFileSize(*executionContext, fSizes[fileIndex], fsize);
			fSizes[fileIndex] = fsize;
		}

		tstrFileVersion = ResolveHashFileVersion(osFile, path);
		result.meta.version = tstrFileVersion;

		EmitMetaResult(executionContext, result);
		return fsize;
	}
}
