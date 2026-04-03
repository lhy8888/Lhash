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

	const sunjwbase::tstring& GetFinalizedDigestValue(const ResultDigestStorage& digestBundle, ResultDigestType digestType)
	{
		if (!IsRegisteredHashAlgorithmType(digestType))
		{
			static const sunjwbase::tstring emptyDigestValue;
			return emptyDigestValue;
		}

		return GetFinalizedDigestValueById(digestBundle, GetHashAlgorithmId(digestType));
	}

	void SetFinalizedDigestValueById(ResultDigestStorage& digestBundle, const HashAlgorithmId& algorithmId, const sunjwbase::tstring& digestValue)
	{
		SetDigestStorageValueById(digestBundle, algorithmId, digestValue);
	}

	void SetFinalizedDigestValue(ResultDigestStorage& digestBundle, ResultDigestType digestType, const sunjwbase::tstring& digestValue)
	{
		if (!IsRegisteredHashAlgorithmType(digestType))
		{
			return;
		}

		SetFinalizedDigestValueById(digestBundle, GetHashAlgorithmId(digestType), digestValue);
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

			const ResultDigestMetadata *digestMetadata = NULL;
			if (!TryGetResultDigestMetadataById(algorithmId, &digestMetadata) || digestMetadata == NULL)
			{
				return true;
			}

			HashDigestResult digestResult;
			digestResult.type = GetResultDigestMetadataType(*digestMetadata);
			digestResult.stableName = GetResultDigestMetadataStableName(*digestMetadata);
			digestResult.displayLabel = GetResultDigestMetadataDisplayLabel(*digestMetadata);
			digestResult.value = digestValue;
			result.digests.push_back(digestResult);
			return true;
		});
	}

	void FinalizeDigestStrings(const HashRequest& request, FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		VisitHashRequestAlgorithmIds(request, [&](const HashAlgorithmId& algorithmId)
		{
			FinalizeHashDigestContextById(hashContexts, algorithmId, digestBundle);
			return true;
		});
	}
}
