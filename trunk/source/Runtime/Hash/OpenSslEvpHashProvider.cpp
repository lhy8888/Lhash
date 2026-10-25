#include "stdafx.h"

#if !defined(LHASH_WITH_OPENSSL3_VENDOR)
#error "OpenSslEvpHashProvider.cpp is the vendor-backed implementation. Use OpenSslEvpHashProviderStub.cpp in default builds."
#endif

#include <atomic>
#include <memory>
#include <mutex>
#include <string>
#include <unordered_map>
#include <vector>

#include "Runtime/Hash/OpenSslEvpHashProvider.h"

#if defined(LHASH_WITH_OPENSSL3_VENDOR)
#include <openssl/core_names.h>
#include <openssl/params.h>
#include <openssl/provider.h>
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

#if defined(LHASH_WITH_OPENSSL3_VENDOR)
	struct OpenSslDigestImplementationCache
	{
		OpenSslDigestImplementationCache()
			: libraryContext(OSSL_LIB_CTX_new()),
			defaultProvider(libraryContext == NULL ? NULL : OSSL_PROVIDER_load(libraryContext, "default"))
		{
		}

		std::mutex mutex;
		std::unordered_map<std::string, EVP_MD*> digestImplementations;
		// A private context does not load the process's openssl.cnf. The builtin
		// default provider supplies every digest exposed by Lhash.
		OSSL_LIB_CTX *libraryContext;
		OSSL_PROVIDER *defaultProvider;

		~OpenSslDigestImplementationCache()
		{
			for (std::unordered_map<std::string, EVP_MD*>::iterator itr = digestImplementations.begin();
				itr != digestImplementations.end();
				++itr)
			{
				EVP_MD_free(itr->second);
			}
			if (defaultProvider != NULL)
				OSSL_PROVIDER_unload(defaultProvider);
			OSSL_LIB_CTX_free(libraryContext);
		}
	};

	static OpenSslDigestImplementationCache& GetOpenSslDigestImplementationCache()
	{
		static OpenSslDigestImplementationCache digestImplementationCache;
		return digestImplementationCache;
	}

	static EVP_MD* GetCachedDigestImplementation(const char *algorithmName)
	{
		if (algorithmName == NULL || algorithmName[0] == '\0')
		{
			return NULL;
		}

		OpenSslDigestImplementationCache& digestImplementationCache = GetOpenSslDigestImplementationCache();
		std::lock_guard<std::mutex> lock(digestImplementationCache.mutex);
		std::unordered_map<std::string, EVP_MD*>::iterator cachedDigest =
			digestImplementationCache.digestImplementations.find(algorithmName);
		if (cachedDigest != digestImplementationCache.digestImplementations.end())
		{
			return cachedDigest->second;
		}

		if (digestImplementationCache.defaultProvider == NULL)
		{
			return NULL;
		}

		std::unique_ptr<EVP_MD, decltype(&EVP_MD_free)> digestImplementation(
			EVP_MD_fetch(digestImplementationCache.libraryContext, algorithmName, "provider=default"),
			&EVP_MD_free);
		if (!digestImplementation)
		{
			return NULL;
		}
		digestImplementationCache.digestImplementations.emplace(algorithmName, digestImplementation.get());
		return digestImplementation.release();
	}
#endif
}

namespace HashRuntime
{
	OpenSslEvpHashContext::~OpenSslEvpHashContext()
	{
		CleanupOpenSslEvpHashContext(this);
	}

	bool InitializeOpenSslEvpHashContext(
		OpenSslEvpHashContext *hashContext,
		const char *const *algorithmNames,
		size_t algorithmNameCount,
		bool xofMode,
		size_t digestOutputBytes)
	{
#if !defined(LHASH_WITH_OPENSSL3_VENDOR)
		(void)hashContext;
		(void)algorithmNames;
		(void)algorithmNameCount;
		(void)xofMode;
		(void)digestOutputBytes;
		return false;
#else
		if (hashContext == NULL)
		{
			return false;
		}

		CleanupOpenSslEvpHashContext(hashContext);
		if (algorithmNames == NULL || algorithmNameCount == 0 || digestOutputBytes == 0 ||
			digestOutputBytes > std::string().max_size() / 2)
		{
			return false;
		}
		for (size_t algorithmIndex = 0; algorithmIndex < algorithmNameCount; ++algorithmIndex)
		{
			const char *algorithmName = algorithmNames[algorithmIndex];
			if (algorithmName == NULL || algorithmName[0] == '\0')
			{
				continue;
			}

			EVP_MD *digestImplementation = GetCachedDigestImplementation(algorithmName);
			if (digestImplementation == NULL)
			{
				continue;
			}
			if (((EVP_MD_get_flags(digestImplementation) & EVP_MD_FLAG_XOF) != 0) != xofMode)
			{
				continue;
			}
			if (!xofMode && (EVP_MD_get_size(digestImplementation) <= 0 ||
				static_cast<size_t>(EVP_MD_get_size(digestImplementation)) != digestOutputBytes))
			{
				continue;
			}

			EVP_MD_CTX *mdContext = EVP_MD_CTX_new();
			if (mdContext == NULL)
			{
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
				continue;
			}
			if (!xofMode && (EVP_MD_CTX_get_size(mdContext) <= 0 ||
				static_cast<size_t>(EVP_MD_CTX_get_size(mdContext)) != resolvedDigestOutputBytes))
			{
				EVP_MD_CTX_free(mdContext);
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
#if defined(LHASH_WITH_OPENSSL3_VENDOR)
		if (hashContext.updateFailed)
		{
			return;
		}

		if (dataLen == 0)
		{
			return;
		}
		if (hashContext.mdContext == NULL || data == NULL)
		{
			hashContext.updateFailed = true;
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
#if !defined(LHASH_WITH_OPENSSL3_VENDOR)
		(void)hashContext;
		(void)outputBytes;
		return finalizeResult;
#else
		if (hashContext.updateFailed || hashContext.mdContext == NULL || hashContext.mdImplementation == NULL ||
			outputBytes == 0 || outputBytes != hashContext.digestOutputBytes ||
			outputBytes > std::string().max_size() / 2)
		{
			CleanupOpenSslEvpHashContext(&hashContext);
			return finalizeResult;
		}
		// EVP_DigestFinal_ex has no capacity argument: validate before it writes.
		// Checking finalizedBytes afterwards cannot prevent an undersized buffer.
		if (!hashContext.xofMode && (EVP_MD_CTX_get_size(hashContext.mdContext) <= 0 ||
			static_cast<size_t>(EVP_MD_CTX_get_size(hashContext.mdContext)) != outputBytes))
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
#if defined(LHASH_WITH_OPENSSL3_VENDOR)
		if (hashContext == NULL)
		{
			return;
		}

		if (hashContext->mdContext != NULL)
		{
			EVP_MD_CTX_free(hashContext->mdContext);
			hashContext->mdContext = NULL;
		}

		hashContext->mdImplementation = NULL;

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
