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

	void InitializeHashDigestContext(FileHashContexts *hashContexts, ResultDigestType digestType)
	{
		HashDigestOperationDescriptor operationDescriptor = { 0 };
		if (!TryGetHashDigestOperationDescriptor(digestType, &operationDescriptor))
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

	void FinalizeHashDigestContext(FileHashContexts& hashContexts, ResultDigestType digestType, ResultDigestStorage& digestBundle)
	{
		HashDigestOperationDescriptor operationDescriptor = { 0 };
		if (!TryGetHashDigestOperationDescriptor(digestType, &operationDescriptor))
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
