#ifndef _HASH_DIGEST_OPERATION_REGISTRY_RUNTIME_H_
#define _HASH_DIGEST_OPERATION_REGISTRY_RUNTIME_H_

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
		HashAlgorithmId algorithmId;
		HashDigestInitializeAction initializeAction;
		HashDigestUpdateAction updateAction;
		HashDigestFinalizeAction finalizeAction;
	};

	bool RegisterHashDigestOperationDescriptor(const HashDigestOperationDescriptor& operationDescriptor);
	bool IsHashDigestOperationDescriptorComplete(const HashDigestOperationDescriptor& operationDescriptor);
	bool IsHashDigestOperationDescriptorSupportedById(const HashAlgorithmId& algorithmId);
	bool IsHashDigestOperationRegistryConsistent();

	const HashDigestOperationDescriptor *GetHashDigestOperationDescriptors(int *descriptorCount);
	bool TryGetHashDigestOperationDescriptorById(const HashAlgorithmId& algorithmId, HashDigestOperationDescriptor *operationDescriptor);
}

#endif
