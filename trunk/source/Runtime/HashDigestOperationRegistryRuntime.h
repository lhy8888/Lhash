#ifndef _HASH_DIGEST_OPERATION_REGISTRY_RUNTIME_H_
#define _HASH_DIGEST_OPERATION_REGISTRY_RUNTIME_H_

#include <vector>

#include "Common/HashTypes.h"
#include "Domain/HashAlgorithmRegistryCore.h"

namespace HashEngineInternal
{
	struct FileHashContexts;

	typedef void (*HashDigestInitializeAction)(FileHashContexts *hashContexts);
	typedef void (*HashDigestUpdateAction)(FileHashContexts& hashContexts, unsigned char *data, unsigned int dataLen);
	typedef bool (*HashDigestFinalizeAction)(FileHashContexts& hashContexts, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText);

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

	std::vector<HashDigestOperationDescriptor> GetHashDigestOperationDescriptorSnapshot();
	const HashDigestOperationDescriptor *GetHashDigestOperationDescriptors(int *descriptorCount);
	bool TryGetHashDigestOperationDescriptorById(const HashAlgorithmId& algorithmId, HashDigestOperationDescriptor *operationDescriptor);
}

#endif
