#ifndef _HASH_DIGEST_OPERATION_REGISTRY_H_
#define _HASH_DIGEST_OPERATION_REGISTRY_H_

#include "Common/Global.h"

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
	};

	bool IsHashDigestOperationDescriptorComplete(const HashDigestOperationDescriptor& operationDescriptor);
	bool IsHashDigestOperationDescriptorSupported(ResultDigestType digestType);
	bool IsHashDigestOperationRegistryConsistent();

	const HashDigestOperationDescriptor *GetHashDigestOperationDescriptors(int *descriptorCount);
	bool TryGetHashDigestOperationDescriptor(ResultDigestType digestType, HashDigestOperationDescriptor *operationDescriptor);
}

#endif
