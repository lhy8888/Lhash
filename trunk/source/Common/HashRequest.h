#ifndef _HASH_REQUEST_H_
#define _HASH_REQUEST_H_

#include <algorithm>
#include <vector>

#include "Common/Global.h"
#include "Common/HashAlgorithmRegistry.h"


struct HashRequest
{
	HashRequest()
		: uppercaseDigest(false)
	{
	}

	TStrVector files;
	std::vector<ResultDigestType> algorithms;
	bool uppercaseDigest;
};

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

template<typename THashRequestAlgorithmVisitor>
static inline bool VisitHashRequestAlgorithms(const HashRequest& request, THashRequestAlgorithmVisitor visitor)
{
	std::vector<ResultDigestType> visitedAlgorithms;
	visitedAlgorithms.reserve(request.algorithms.size());

	for (size_t algorithmIndex = 0; algorithmIndex < request.algorithms.size(); ++algorithmIndex)
	{
		ResultDigestType digestType = request.algorithms[algorithmIndex];
		if (!IsRegisteredHashAlgorithmType(digestType))
		{
			continue;
		}

		if (std::find(visitedAlgorithms.begin(), visitedAlgorithms.end(), digestType) != visitedAlgorithms.end())
		{
			continue;
		}

		visitedAlgorithms.push_back(digestType);
		if (!visitor(digestType))
		{
			return false;
		}
	}

	return true;
}

static inline bool HasHashRequestAlgorithm(const HashRequest& request, ResultDigestType digestType)
{
	bool hasAlgorithm = false;

	VisitHashRequestAlgorithms(request, [&](ResultDigestType algorithmType)
	{
		if (algorithmType == digestType)
		{
			hasAlgorithm = true;
			return false;
		}

		return true;
	});

	return hasAlgorithm;
}

static inline bool GetHashRequestUppercaseDigest(const HashRequest& request)
{
	return request.uppercaseDigest;
}

#endif
