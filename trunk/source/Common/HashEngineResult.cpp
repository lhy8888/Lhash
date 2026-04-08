#include "stdafx.h"

#include "Common/HashEngineInternal.h"

using namespace sunjwbase;

namespace HashEngineInternal
{
	uint64_t PrepareFileMetaResult(HashExecutionContext *executionContext, HashResult& result,
		OsFile& osFile, const TCHAR *path, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, tstring& tstrFileVersion)
	{
		uint64_t fsize = 0;
		result.meta.modifiedDate = osFile.getModifiedTimeFormat();
		fsize = ResolveHashFileSizeAndTrack(executionContext, osFile, isSizeCaled, fSizes, fileIndex, result);

		tstrFileVersion = ResolveHashFileVersion(osFile, path);
		result.meta.version = tstrFileVersion;

		EmitMetaResult(executionContext, result);
		return fsize;
	}
}
