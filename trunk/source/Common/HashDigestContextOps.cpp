#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void InitializeHashDigestContextById(FileHashContexts *hashContexts, const HashAlgorithmId& algorithmId)
	{
		HashDigestOperationDescriptor operationDescriptor = {};
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

	bool FinalizeHashDigestContextById(FileHashContexts& hashContexts, const HashAlgorithmId& algorithmId, ResultDigestStorage& digestBundle, sunjwbase::tstring *errorText)
	{
		HashAlgorithmDescriptor algorithmDescriptor = {};
		if (!TryGetHashAlgorithmDescriptorById(algorithmId, &algorithmDescriptor))
		{
			return false;
		}

		if (!DoesHashAlgorithmDescriptorRequireDigestOperations(algorithmDescriptor))
		{
			return true;
		}

		HashDigestOperationDescriptor operationDescriptor = {};
		if (!TryGetHashDigestOperationDescriptorById(algorithmId, &operationDescriptor))
		{
			return false;
		}

		if (operationDescriptor.finalizeAction == NULL)
		{
			return false;
		}

		return operationDescriptor.finalizeAction(hashContexts, digestBundle, errorText);
	}
}
