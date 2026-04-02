#include "stdafx.h"

#include "Common/HashEngineInternal.h"
#include "Common/strhelper.h"

using namespace std;

namespace HashEngineInternal
{
	void InitializeFileHashing(const HashRequest& request, HashExecutionContext *executionContext, FileHashContexts *hashContexts)
	{
		VisitHashRequestAlgorithms(request, [&](ResultDigestType digestType)
		{
			switch (digestType)
			{
			case RESULT_DIGEST_MD5:
				MD5Init(&hashContexts->mdContext, 0);
				break;
			case RESULT_DIGEST_SHA1:
				hashContexts->sha1.Reset();
				break;
			case RESULT_DIGEST_SHA256:
				sha256_init(&hashContexts->sha256Ctx);
				break;
			case RESULT_DIGEST_SHA512:
				SHA512_Init(&hashContexts->sha512Ctx);
				break;
			}

			return true;
		});

		GetHashExecutionProgressSink(*executionContext)->onProgressEvent(CreateFileProgressEvent(0));
	}

	const sunjwbase::tstring& GetFinalizedDigestValue(const ResultDigestStorage& digestBundle, ResultDigestType digestType)
	{
		return GetDigestStorageValue(digestBundle, digestType);
	}

	void SetFinalizedDigestValue(ResultDigestStorage& digestBundle, ResultDigestType digestType, const sunjwbase::tstring& digestValue)
	{
		SetDigestStorageValue(digestBundle, digestType, digestValue);
	}

	void PopulateDigestResult(const HashRequest& request, HashResult& result, const ResultDigestStorage& digestBundle)
	{
		result.digests.clear();
		VisitHashRequestAlgorithms(request, [&](ResultDigestType digestType)
		{
			const sunjwbase::tstring& digestValue = GetFinalizedDigestValue(digestBundle, digestType);
			if (digestValue.empty())
			{
				return true;
			}

			const ResultDigestMetadata& digestMetadata = GetResultDigestMetadata(digestType);
			HashDigestResult digestResult;
			digestResult.type = digestType;
			digestResult.stableName = GetResultDigestMetadataStableName(digestMetadata);
			digestResult.displayLabel = GetResultDigestMetadataDisplayLabel(digestMetadata);
			digestResult.value = digestValue;
			result.digests.push_back(digestResult);
			return true;
		});
	}

	void FinalizeDigestStrings(const HashRequest& request, FileHashContexts& hashContexts, ResultDigestStorage& digestBundle)
	{
		char chHashBuff[1024] = { 0 };
		char strSHA1[256] = { 0 };
		string strSHA256;
		string strSHA512;

		VisitHashRequestAlgorithms(request, [&](ResultDigestType digestType)
		{
			switch (digestType)
			{
			case RESULT_DIGEST_MD5:
				MD5Final(&hashContexts.mdContext);
#if defined (_WIN32)
				sprintf_s(chHashBuff, 1024,
					"%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X",
					hashContexts.mdContext.digest[0],
					hashContexts.mdContext.digest[1],
					hashContexts.mdContext.digest[2],
					hashContexts.mdContext.digest[3],
					hashContexts.mdContext.digest[4],
					hashContexts.mdContext.digest[5],
					hashContexts.mdContext.digest[6],
					hashContexts.mdContext.digest[7],
					hashContexts.mdContext.digest[8],
					hashContexts.mdContext.digest[9],
					hashContexts.mdContext.digest[10],
					hashContexts.mdContext.digest[11],
					hashContexts.mdContext.digest[12],
					hashContexts.mdContext.digest[13],
					hashContexts.mdContext.digest[14],
					hashContexts.mdContext.digest[15]);
#else
				snprintf(chHashBuff, 1024,
					"%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X%02X",
					hashContexts.mdContext.digest[0],
					hashContexts.mdContext.digest[1],
					hashContexts.mdContext.digest[2],
					hashContexts.mdContext.digest[3],
					hashContexts.mdContext.digest[4],
					hashContexts.mdContext.digest[5],
					hashContexts.mdContext.digest[6],
					hashContexts.mdContext.digest[7],
					hashContexts.mdContext.digest[8],
					hashContexts.mdContext.digest[9],
					hashContexts.mdContext.digest[10],
					hashContexts.mdContext.digest[11],
					hashContexts.mdContext.digest[12],
					hashContexts.mdContext.digest[13],
					hashContexts.mdContext.digest[14],
					hashContexts.mdContext.digest[15]);
#endif
				SetFinalizedDigestValue(digestBundle, RESULT_DIGEST_MD5, sunjwbase::strtotstr(string(chHashBuff)));
				break;
			case RESULT_DIGEST_SHA1:
				hashContexts.sha1.Final();
				hashContexts.sha1.ReportHash(strSHA1, CSHA1::REPORT_HEX);
				SetFinalizedDigestValue(digestBundle, RESULT_DIGEST_SHA1, sunjwbase::strtotstr(string(strSHA1)));
				break;
			case RESULT_DIGEST_SHA256:
				sha256_final(&hashContexts.sha256Ctx);
				sha256_digest(&hashContexts.sha256Ctx, &strSHA256);
				SetFinalizedDigestValue(digestBundle, RESULT_DIGEST_SHA256, sunjwbase::strtotstr(strSHA256));
				break;
			case RESULT_DIGEST_SHA512:
				SHA512_Final(hashContexts.digestSHA512, &hashContexts.sha512Ctx);
				strSHA512.clear();
				for (int p = 0; p < SHA512_DIGEST_LENGTH; p++)
				{
					char buf[8] = { 0 };
#if defined (_WIN32)
					sprintf_s(buf, 8, "%02X", hashContexts.digestSHA512[p]);
#else
					snprintf(buf, 8, "%02X", hashContexts.digestSHA512[p]);
#endif
					strSHA512.append(std::string(buf));
				}
				SetFinalizedDigestValue(digestBundle, RESULT_DIGEST_SHA512, sunjwbase::strtotstr(strSHA512));
				break;
			}

			return true;
		});
	}
}
