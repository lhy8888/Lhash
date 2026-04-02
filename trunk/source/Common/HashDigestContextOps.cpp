#include "stdafx.h"

#include "Common/HashEngineInternal.h"
#include "Common/strhelper.h"

using namespace std;

namespace HashEngineInternal
{
	void InitializeHashDigestContext(FileHashContexts *hashContexts, ResultDigestType digestType)
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
	}

	void FinalizeHashDigestContext(FileHashContexts& hashContexts, ResultDigestType digestType, ResultDigestStorage& digestBundle)
	{
		char chHashBuff[1024] = { 0 };
		char strSHA1[256] = { 0 };
		string strSHA256;
		string strSHA512;

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
			SetDigestStorageValue(digestBundle, RESULT_DIGEST_MD5, sunjwbase::strtotstr(string(chHashBuff)));
			break;
		case RESULT_DIGEST_SHA1:
			hashContexts.sha1.Final();
			hashContexts.sha1.ReportHash(strSHA1, CSHA1::REPORT_HEX);
			SetDigestStorageValue(digestBundle, RESULT_DIGEST_SHA1, sunjwbase::strtotstr(string(strSHA1)));
			break;
		case RESULT_DIGEST_SHA256:
			sha256_final(&hashContexts.sha256Ctx);
			sha256_digest(&hashContexts.sha256Ctx, &strSHA256);
			SetDigestStorageValue(digestBundle, RESULT_DIGEST_SHA256, sunjwbase::strtotstr(strSHA256));
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
			SetDigestStorageValue(digestBundle, RESULT_DIGEST_SHA512, sunjwbase::strtotstr(strSHA512));
			break;
		}
	}
}
