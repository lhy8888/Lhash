#ifndef _HASH_REQUEST_H_
#define _HASH_REQUEST_H_

#include <algorithm>
#include <vector>

#include "Common/Global.h"
#include "Common/HashAlgorithmRegistry.h"

enum HashRequestDigestExecutionPolicy
{
	HASH_REQUEST_DIGEST_EXECUTION_POLICY_AUTO = 0,
	HASH_REQUEST_DIGEST_EXECUTION_POLICY_SINGLE_THREADED,
	HASH_REQUEST_DIGEST_EXECUTION_POLICY_PARALLEL
};

struct HashRequest
{
	HashRequest()
		: uppercaseDigest(false),
		digestExecutionPolicy(HASH_REQUEST_DIGEST_EXECUTION_POLICY_AUTO)
	{
	}

	TStrVector files;
	std::vector<HashAlgorithmId> algorithmIds;
	std::vector<ResultDigestType> algorithms;
	bool uppercaseDigest;
	HashRequestDigestExecutionPolicy digestExecutionPolicy;
};

static inline void AppendHashRequestAlgorithmId(HashRequest& request, const HashAlgorithmId& algorithmId)
{
	HashAlgorithmId normalizedAlgorithmId = NormalizeHashAlgorithmId(algorithmId);
	if (normalizedAlgorithmId.empty())
	{
		return;
	}

	request.algorithmIds.push_back(normalizedAlgorithmId);
}

static inline void AppendHashRequestAlgorithm(HashRequest& request, ResultDigestType digestType)
{
	request.algorithms.push_back(digestType);
	AppendHashRequestAlgorithmId(request, GetHashAlgorithmId(digestType));
}

static inline std::vector<HashAlgorithmId> GetHashRequestNormalizedAlgorithmIds(const HashRequest& request)
{
	std::vector<HashAlgorithmId> normalizedAlgorithmIds;
	normalizedAlgorithmIds.reserve(request.algorithmIds.size() + request.algorithms.size());

	for (size_t algorithmIndex = 0; algorithmIndex < request.algorithmIds.size(); ++algorithmIndex)
	{
		HashAlgorithmId normalizedAlgorithmId = NormalizeHashAlgorithmId(request.algorithmIds[algorithmIndex]);
		if (normalizedAlgorithmId.empty())
		{
			continue;
		}

		if (!IsRegisteredHashAlgorithmId(normalizedAlgorithmId))
		{
			continue;
		}

		if (std::find(normalizedAlgorithmIds.begin(), normalizedAlgorithmIds.end(), normalizedAlgorithmId) != normalizedAlgorithmIds.end())
		{
			continue;
		}

		normalizedAlgorithmIds.push_back(normalizedAlgorithmId);
	}

	for (size_t algorithmIndex = 0; algorithmIndex < request.algorithms.size(); ++algorithmIndex)
	{
		ResultDigestType digestType = request.algorithms[algorithmIndex];
		if (!IsRegisteredHashAlgorithmType(digestType))
		{
			continue;
		}

		HashAlgorithmId normalizedAlgorithmId = GetHashAlgorithmId(digestType);
		if (std::find(normalizedAlgorithmIds.begin(), normalizedAlgorithmIds.end(), normalizedAlgorithmId) != normalizedAlgorithmIds.end())
		{
			continue;
		}

		normalizedAlgorithmIds.push_back(normalizedAlgorithmId);
	}

	return normalizedAlgorithmIds;
}

static inline HashAlgorithmSelectionState CreateHashRequestAlgorithmSelectionState(const HashRequest& request)
{
	HashAlgorithmSelectionState selectionState;
	selectionState.enabled.assign(static_cast<size_t>(GetRegisteredHashAlgorithmCount()), false);

	std::vector<HashAlgorithmId> normalizedAlgorithmIds = GetHashRequestNormalizedAlgorithmIds(request);
	for (size_t algorithmIndex = 0; algorithmIndex < normalizedAlgorithmIds.size(); ++algorithmIndex)
	{
		const HashAlgorithmDescriptor *algorithmDescriptor = NULL;
		if (!TryGetHashAlgorithmDescriptorById(normalizedAlgorithmIds[algorithmIndex], &algorithmDescriptor) ||
			algorithmDescriptor == NULL)
		{
			continue;
		}

		ResultDigestType digestType = GetHashAlgorithmDescriptorType(*algorithmDescriptor);
		int registeredIndex = -1;
		if (!TryGetHashAlgorithmIndex(digestType, &registeredIndex))
		{
			continue;
		}
		size_t normalizedIndex = static_cast<size_t>(registeredIndex);
		if (normalizedIndex >= selectionState.enabled.size())
		{
			continue;
		}

		selectionState.enabled[normalizedIndex] = true;
	}

	return selectionState;
}

static inline bool IsHashRequestAlgorithmSelected(const HashAlgorithmSelectionState& selectionState, ResultDigestType digestType)
{
	int algorithmIndex = -1;
	if (!TryGetHashAlgorithmIndex(digestType, &algorithmIndex))
	{
		return false;
	}

	size_t normalizedIndex = static_cast<size_t>(algorithmIndex);
	if (normalizedIndex >= selectionState.enabled.size())
	{
		return false;
	}

	return selectionState.enabled[normalizedIndex];
}

static inline size_t GetHashRequestFileCount(const HashRequest& request)
{
	return request.files.size();
}

static inline const sunjwbase::tstring& GetHashRequestFileAt(const HashRequest& request, size_t fileIndex)
{
	return request.files[fileIndex];
}

template<typename THashRequestFileVisitor>
static inline bool VisitHashRequestFiles(const HashRequest& request, THashRequestFileVisitor visitor)
{
	for (size_t fileIndex = 0; fileIndex < GetHashRequestFileCount(request); ++fileIndex)
	{
		if (!visitor(static_cast<uint32_t>(fileIndex), GetHashRequestFileAt(request, fileIndex)))
		{
			return false;
		}
	}

	return true;
}

template<typename THashRequestAlgorithmIdVisitor>
static inline bool VisitHashRequestAlgorithmIds(const HashRequest& request, THashRequestAlgorithmIdVisitor visitor)
{
	std::vector<HashAlgorithmId> normalizedAlgorithmIds = GetHashRequestNormalizedAlgorithmIds(request);
	for (size_t algorithmIndex = 0; algorithmIndex < normalizedAlgorithmIds.size(); ++algorithmIndex)
	{
		if (!visitor(normalizedAlgorithmIds[algorithmIndex]))
		{
			return false;
		}
	}

	return true;
}

template<typename THashRequestAlgorithmVisitor>
static inline bool VisitHashRequestAlgorithms(const HashRequest& request, THashRequestAlgorithmVisitor visitor)
{
	return VisitHashRequestAlgorithmIds(request, [&](const HashAlgorithmId& algorithmId)
	{
		ResultDigestType digestType = RESULT_DIGEST_UNKNOWN;
		if (!TryGetHashAlgorithmTypeById(algorithmId, &digestType))
		{
			return true;
		}

		return visitor(digestType);
	});
}

static inline bool HasHashRequestAlgorithm(const HashRequest& request, ResultDigestType digestType)
{
	return IsHashRequestAlgorithmSelected(CreateHashRequestAlgorithmSelectionState(request), digestType);
}

static inline bool HasHashRequestAlgorithmId(const HashRequest& request, const HashAlgorithmId& algorithmId)
{
	std::vector<HashAlgorithmId> normalizedAlgorithmIds = GetHashRequestNormalizedAlgorithmIds(request);
	HashAlgorithmId normalizedAlgorithmId = NormalizeHashAlgorithmId(algorithmId);
	return std::find(normalizedAlgorithmIds.begin(), normalizedAlgorithmIds.end(), normalizedAlgorithmId) != normalizedAlgorithmIds.end();
}

static inline bool GetHashRequestUppercaseDigest(const HashRequest& request)
{
	return request.uppercaseDigest;
}

static inline HashRequestDigestExecutionPolicy GetHashRequestDigestExecutionPolicy(const HashRequest& request)
{
	return request.digestExecutionPolicy;
}

#endif
