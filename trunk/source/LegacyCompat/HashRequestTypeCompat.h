#ifndef _LEGACY_HASH_REQUEST_TYPE_COMPAT_H_
#define _LEGACY_HASH_REQUEST_TYPE_COMPAT_H_

#include "Domain/HashRequest.h"
#include "LegacyCompat/HashAlgorithmTypeCompat.h"

static inline void AppendHashRequestAlgorithm(HashRequest& request, ResultDigestType digestType)
{
	AppendHashRequestAlgorithmId(request, GetHashAlgorithmId(digestType));
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

#endif
