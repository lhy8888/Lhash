#include "stdafx.h"

#include "Runtime/Hash/OpenSslEvpHashProvider.h"

namespace HashRuntime
{
	bool InitializeOpenSslEvpHashContext(
		OpenSslEvpHashContext *hashContext,
		const char *const *algorithmNames,
		size_t algorithmNameCount,
		bool xofMode,
		size_t digestOutputBytes)
	{
		(void)hashContext;
		(void)algorithmNames;
		(void)algorithmNameCount;
		(void)xofMode;
		(void)digestOutputBytes;
		return false;
	}

	void UpdateOpenSslEvpHashContext(OpenSslEvpHashContext& hashContext, const unsigned char *data, size_t dataLen)
	{
		(void)hashContext;
		(void)data;
		(void)dataLen;
	}

	OpenSslEvpHashFinalizeResult FinalizeOpenSslEvpHashContextHex(OpenSslEvpHashContext& hashContext, size_t outputBytes)
	{
		(void)hashContext;
		(void)outputBytes;
		return OpenSslEvpHashFinalizeResult();
	}

	void CleanupOpenSslEvpHashContext(OpenSslEvpHashContext *hashContext)
	{
		(void)hashContext;
	}

	void ConfigureOpenSslEvpFailureInjection(size_t failDigestUpdateCall, bool failFinalize)
	{
		(void)failDigestUpdateCall;
		(void)failFinalize;
	}

	void ResetOpenSslEvpFailureInjection()
	{
		ConfigureOpenSslEvpFailureInjection(0, false);
	}
}
