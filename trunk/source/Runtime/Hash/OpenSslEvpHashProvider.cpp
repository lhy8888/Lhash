#include "stdafx.h"

#include <atomic>
#include <string>
#include <vector>

#include "Runtime/Hash/OpenSslEvpHashProvider.h"

#if defined(FHASH_WITH_OPENSSL3_VENDOR)
#include <openssl/core_names.h>
#include <openssl/params.h>
#endif

namespace
{
	struct OpenSslEvpFailureInjectionState
	{
		OpenSslEvpFailureInjectionState()
			: updateCallCount(0),
			failDigestUpdateCall(0),
			failFinalize(false)
		{
		}

		std::atomic<size_t> updateCallCount;
		std::atomic<size_t> failDigestUpdateCall;
		std::atomic<bool> failFinalize;
	};

	static OpenSslEvpFailureInjectionState& GetOpenSslEvpFailureInjectionState()
	{
		static OpenSslEvpFailureInjectionState failureInjectionState;
		return failureInjectionState;
	}

	static bool ShouldInjectDigestUpdateFailure()
	{
		OpenSslEvpFailureInjectionState& failureInjectionState = GetOpenSslEvpFailureInjectionState();
		size_t failDigestUpdateCall = failureInjectionState.failDigestUpdateCall.load();
		if (failDigestUpdateCall == 0)
		{
			return false;
		}

		size_t updateCallIndex = failureInjectionState.updateCallCount.fetch_add(1) + 1;
		return updateCallIndex == failDigestUpdateCall;
	}

	static char RenderUppercaseHexNibble(unsigned char nibble)
	{
		static const char kHexDigits[] = "0123456789ABCDEF";
		return kHexDigits[nibble & 0x0F];
	}

	static std::string RenderUppercaseHexString(const std::vector<unsigned char>& bytes)
	{
		std::string renderedHex;
		renderedHex.reserve(bytes.size() * 2);

		for (size_t index = 0; index < bytes.size(); ++index)
		{
			unsigned char byteValue = bytes[index];
			renderedHex.push_back(RenderUppercaseHexNibble(static_cast<unsigned char>(byteValue >> 4)));
			renderedHex.push_back(RenderUppercaseHexNibble(byteValue));
		}

		return renderedHex;
	}
}

namespace HashRuntime
{
	bool InitializeOpenSslEvpHashContext(
		OpenSslEvpHashContext *hashContext,
		const char *const *algorithmNames,
		size_t algorithmNameCount,
		bool xofMode,
		size_t digestOutputBytes)
	{
#if !defined(FHASH_WITH_OPENSSL3_VENDOR)
		(void)hashContext;
		(void)algorithmNames;
		(void)algorithmNameCount;
		(void)xofMode;
		(void)digestOutputBytes;
		return false;
#else
		if (hashContext == NULL || algorithmNames == NULL || algorithmNameCount == 0)
		{
			return false;
		}

		CleanupOpenSslEvpHashContext(hashContext);
		for (size_t algorithmIndex = 0; algorithmIndex < algorithmNameCount; ++algorithmIndex)
		{
			const char *algorithmName = algorithmNames[algorithmIndex];
			if (algorithmName == NULL || algorithmName[0] == '\0')
			{
				continue;
			}

			EVP_MD *digestImplementation = EVP_MD_fetch(NULL, algorithmName, NULL);
			if (digestImplementation == NULL)
			{
				continue;
			}

			EVP_MD_CTX *mdContext = EVP_MD_CTX_new();
			if (mdContext == NULL)
			{
				EVP_MD_free(digestImplementation);
				continue;
			}

			OSSL_PARAM digestParams[2];
			OSSL_PARAM *resolvedDigestParams = NULL;
			size_t resolvedDigestOutputBytes = digestOutputBytes;
			if (!xofMode && resolvedDigestOutputBytes > 0)
			{
				digestParams[0] = OSSL_PARAM_construct_size_t(OSSL_DIGEST_PARAM_SIZE, &resolvedDigestOutputBytes);
				digestParams[1] = OSSL_PARAM_construct_end();
				resolvedDigestParams = digestParams;
			}

			if (EVP_DigestInit_ex2(mdContext, digestImplementation, resolvedDigestParams) != 1)
			{
				EVP_MD_CTX_free(mdContext);
				EVP_MD_free(digestImplementation);
				continue;
			}

			hashContext->mdContext = mdContext;
			hashContext->mdImplementation = digestImplementation;
			hashContext->xofMode = xofMode;
			hashContext->digestOutputBytes = resolvedDigestOutputBytes;
			return true;
		}

		return false;
#endif
	}

	void UpdateOpenSslEvpHashContext(OpenSslEvpHashContext& hashContext, const unsigned char *data, size_t dataLen)
	{
#if defined(FHASH_WITH_OPENSSL3_VENDOR)
		if (hashContext.updateFailed)
		{
			return;
		}

		if (hashContext.mdContext == NULL || data == NULL || dataLen == 0)
		{
			return;
		}

		if (ShouldInjectDigestUpdateFailure() ||
			EVP_DigestUpdate(hashContext.mdContext, data, dataLen) != 1)
		{
			hashContext.updateFailed = true;
		}
#else
		(void)hashContext;
		(void)data;
		(void)dataLen;
#endif
	}

	OpenSslEvpHashFinalizeResult FinalizeOpenSslEvpHashContextHex(OpenSslEvpHashContext& hashContext, size_t outputBytes)
	{
		OpenSslEvpHashFinalizeResult finalizeResult;
#if !defined(FHASH_WITH_OPENSSL3_VENDOR)
		(void)hashContext;
		(void)outputBytes;
		return finalizeResult;
#else
		if (hashContext.updateFailed || hashContext.mdContext == NULL || hashContext.mdImplementation == NULL || outputBytes == 0)
		{
			CleanupOpenSslEvpHashContext(&hashContext);
			return finalizeResult;
		}

		std::vector<unsigned char> outputBuffer(outputBytes);
		bool finalizeSucceeded = false;

		if (GetOpenSslEvpFailureInjectionState().failFinalize.load())
		{
			finalizeSucceeded = false;
		}
		else if (hashContext.xofMode)
		{
			finalizeSucceeded = EVP_DigestFinalXOF(hashContext.mdContext, &outputBuffer[0], outputBuffer.size()) == 1;
		}
		else
		{
			unsigned int finalizedBytes = 0;
			finalizeSucceeded = EVP_DigestFinal_ex(hashContext.mdContext, &outputBuffer[0], &finalizedBytes) == 1 &&
				finalizedBytes == outputBytes;
		}

		CleanupOpenSslEvpHashContext(&hashContext);
		if (!finalizeSucceeded)
		{
			return finalizeResult;
		}

		finalizeResult.success = true;
		finalizeResult.digest = sunjwbase::strtotstr(RenderUppercaseHexString(outputBuffer));
		return finalizeResult;
#endif
	}

	void CleanupOpenSslEvpHashContext(OpenSslEvpHashContext *hashContext)
	{
#if defined(FHASH_WITH_OPENSSL3_VENDOR)
		if (hashContext == NULL)
		{
			return;
		}

		if (hashContext->mdContext != NULL)
		{
			EVP_MD_CTX_free(hashContext->mdContext);
			hashContext->mdContext = NULL;
		}

		if (hashContext->mdImplementation != NULL)
		{
			EVP_MD_free(hashContext->mdImplementation);
			hashContext->mdImplementation = NULL;
		}

		hashContext->xofMode = false;
		hashContext->digestOutputBytes = 0;
		hashContext->updateFailed = false;
#else
		(void)hashContext;
#endif
	}

	void ConfigureOpenSslEvpFailureInjection(size_t failDigestUpdateCall, bool failFinalize)
	{
		OpenSslEvpFailureInjectionState& failureInjectionState = GetOpenSslEvpFailureInjectionState();
		failureInjectionState.updateCallCount.store(0);
		failureInjectionState.failDigestUpdateCall.store(failDigestUpdateCall);
		failureInjectionState.failFinalize.store(failFinalize);
	}

	void ResetOpenSslEvpFailureInjection()
	{
		ConfigureOpenSslEvpFailureInjection(0, false);
	}
}
