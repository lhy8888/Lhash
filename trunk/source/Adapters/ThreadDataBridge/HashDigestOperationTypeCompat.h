#ifndef _LEGACY_HASH_DIGEST_OPERATION_TYPE_COMPAT_H_
#define _LEGACY_HASH_DIGEST_OPERATION_TYPE_COMPAT_H_

#include "Common/HashDigestOperationRegistry.h"
#include "Adapters/ThreadDataBridge/HashAlgorithmTypeCompat.h"

namespace HashEngineInternal
{
	static inline bool TryGetHashDigestOperationDescriptor(ResultDigestType digestType, HashDigestOperationDescriptor *operationDescriptor)
	{
		if (!IsRegisteredHashAlgorithmType(digestType))
		{
			return false;
		}

		return TryGetHashDigestOperationDescriptorById(GetHashAlgorithmId(digestType), operationDescriptor);
	}

	static inline bool IsHashDigestOperationDescriptorSupported(ResultDigestType digestType)
	{
		if (!IsRegisteredHashAlgorithmType(digestType))
		{
			return false;
		}

		return IsHashDigestOperationDescriptorSupportedById(GetHashAlgorithmId(digestType));
	}
}

#endif
