#ifndef _HASH_DIGEST_OPERATION_REGISTRY_H_
#define _HASH_DIGEST_OPERATION_REGISTRY_H_

#include "Common/Global.h"
#include "Common/HashAlgorithmRegistry.h"

namespace HashEngineInternal
{
	struct FileHashContexts;

	typedef void (*HashDigestInitializeAction)(FileHashContexts *hashContexts);
	typedef void (*HashDigestUpdateAction)(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen);
	typedef void (*HashDigestFinalizeAction)(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle);

	struct HashDigestOperationDescriptor
	{
		ResultDigestType digestType;
		HashDigestInitializeAction initializeAction;
		HashDigestUpdateAction updateAction;
		HashDigestFinalizeAction finalizeAction;
		HashAlgorithmId algorithmId;
	};

	bool RegisterHashDigestOperationDescriptor(const HashDigestOperationDescriptor& operationDescriptor);
	bool IsHashDigestOperationDescriptorComplete(const HashDigestOperationDescriptor& operationDescriptor);
	bool IsHashDigestOperationDescriptorSupported(ResultDigestType digestType);
	bool IsHashDigestOperationDescriptorSupportedById(const HashAlgorithmId& algorithmId);
	bool IsHashDigestOperationRegistryConsistent();

	const HashDigestOperationDescriptor *GetHashDigestOperationDescriptors(int *descriptorCount);
	bool TryGetHashDigestOperationDescriptor(ResultDigestType digestType, HashDigestOperationDescriptor *operationDescriptor);
	bool TryGetHashDigestOperationDescriptorById(const HashAlgorithmId& algorithmId, HashDigestOperationDescriptor *operationDescriptor);
}

#endif
