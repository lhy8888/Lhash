#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void InitializeHashDigestContextById(FileHashContexts *hashContexts, const HashAlgorithmId& algorithmId)
	{
		HashDigestOperationDescriptor operationDescriptor = { 0 };
		if (!TryGetHashDigestOperationDescriptorById(algorithmId, &operationDescriptor))
		{
			return;
		}

		if (operationDescriptor.initializeAction == NULL)
		{
			return;
		}

		operationDescriptor.initializeAction(hashContexts);
	}

	void FinalizeHashDigestContextById(FileHashContexts& hashContexts, const HashAlgorithmId& algorithmId, ResultDigestStorage& digestBundle)
	{
		HashDigestOperationDescriptor operationDescriptor = { 0 };
		if (!TryGetHashDigestOperationDescriptorById(algorithmId, &operationDescriptor))
		{
			return;
		}

		if (operationDescriptor.finalizeAction == NULL)
		{
			return;
		}

		operationDescriptor.finalizeAction(hashContexts, digestBundle);
	}
}
