#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
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
