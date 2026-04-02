#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void InitializeFileHashing(const HashRequest& request, HashExecutionContext *executionContext, FileHashContexts *hashContexts)
	{
		VisitHashRequestAlgorithms(request, [&](ResultDigestType digestType)
		{
			InitializeHashDigestContext(hashContexts, digestType);
			return true;
		});

		GetHashExecutionProgressSink(*executionContext)->onProgressEvent(CreateFileProgressEvent(0));
	}

	const sunjwbase::tstring& GetFinalizedDigestValue(const ResultDigestStorage& digestBundle, ResultDigestType digestType)
	{
		return GetDigestStorageValue(digestBundle, digestType);
	}

	void SetFinalizedDigestValue(ResultDigestStorage& digestBundle, ResultDigestType digestType, const sunjwbase::tstring& digestValue)
	{
		SetDigestStorageValue(digestBundle, digestType, digestValue);
	}

	void PopulateDigestResult(const HashRequest& request, HashResult& result, const ResultDigestStorage& digestBundle)
	{
		result.digests.clear();
		VisitHashRequestAlgorithms(request, [&](ResultDigestType digestType)
		{
			const sunjwbase::tstring& digestValue = GetFinalizedDigestValue(digestBundle, digestType);
			if (digestValue.empty())
			{
				return true;
			}

			const ResultDigestMetadata& digestMetadata = GetResultDigestMetadata(digestType);
			HashDigestResult digestResult;
			digestResult.type = digestType;
			digestResult.stableName = GetResultDigestMetadataStableName(digestMetadata);
			digestResult.displayLabel = GetResultDigestMetadataDisplayLabel(digestMetadata);
			digestResult.value = digestValue;
			result.digests.push_back(digestResult);
			return true;
		});
	}

	void FinalizeDigestStrings(const HashRequest& request, FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		VisitHashRequestAlgorithms(request, [&](ResultDigestType digestType)
		{
			FinalizeHashDigestContext(hashContexts, digestType, digestBundle);
			return true;
		});
	}
}
