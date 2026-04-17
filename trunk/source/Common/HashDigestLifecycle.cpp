#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void InitializeFileHashing(const HashRequest& request, HashExecutionContext *executionContext, FileHashContexts *hashContexts)
	{
		VisitHashRequestAlgorithmIds(request, [&](const HashAlgorithmId& algorithmId)
		{
			InitializeHashDigestContextById(hashContexts, algorithmId);
			return true;
		});

		GetHashExecutionProgressSink(*executionContext)->onProgressEvent(CreateFileProgressEvent(0));
	}

	const sunjwbase::tstring& GetFinalizedDigestValueById(const ResultDigestStorage& digestBundle, const HashAlgorithmId& algorithmId)
	{
		return GetDigestStorageValueById(digestBundle, algorithmId);
	}

	void SetFinalizedDigestValueById(ResultDigestStorage& digestBundle, const HashAlgorithmId& algorithmId, const sunjwbase::tstring& digestValue)
	{
		SetDigestStorageValueById(digestBundle, algorithmId, digestValue);
	}

	void PopulateDigestResult(const HashRequest& request, HashResult& result, const ResultDigestStorage& digestBundle)
	{
		result.digests.clear();
		VisitHashRequestAlgorithmIds(request, [&](const HashAlgorithmId& algorithmId)
		{
			const sunjwbase::tstring& digestValue = GetFinalizedDigestValueById(digestBundle, algorithmId);
			if (digestValue.empty())
			{
				return true;
			}

			ResultDigestMetadata digestMetadata;
			if (!TryGetResultDigestMetadataById(algorithmId, &digestMetadata))
			{
				return true;
			}

			HashDigestResult digestResult;
			digestResult.algorithmId = NormalizeHashAlgorithmId(algorithmId);
			digestResult.stableName = GetResultDigestMetadataStableName(digestMetadata);
			digestResult.displayLabel = GetResultDigestMetadataDisplayLabel(digestMetadata);
			digestResult.value = digestValue;
			result.digests.push_back(digestResult);
			return true;
		});
	}

	bool FinalizeDigestStrings(const HashRequest& request, FileHashContexts& hashContexts, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText)
	{
		bool finalizedAllDigests = true;
		VisitHashRequestAlgorithmIds(request, [&](const HashAlgorithmId& algorithmId)
		{
			if (FinalizeHashDigestContextById(hashContexts, algorithmId, digestBundle, errorText))
			{
				return true;
			}

			finalizedAllDigests = false;
			return false;
		});

		return finalizedAllDigests;
	}
}
