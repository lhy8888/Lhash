#include "stdafx.h"

#include "Common/HashEngineInternal.h"
#include "Common/strhelper.h"

#if defined (_WIN32)
#include "WinCommon/WindowsComm.h"
#if (defined (FHASH_UWP_LIB) || defined(FHASH_WUI_LIB))
#include "WinCommon/FileVersionHelper.h"
#endif
#endif

using namespace std;
using namespace sunjwbase;

namespace HashEngineInternal
{
	uint64_t PrepareFileMetaResult(HashExecutionContext *executionContext, HashResult& result,
		OsFile& osFile, const TCHAR *path, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, tstring& tstrFileVersion)
	{
		HashProgressSink *observer = GetHashExecutionProgressSink(*executionContext);
		result.meta.modifiedDate = osFile.getModifiedTimeFormat();

		uint64_t fsize = osFile.getLength();
		result.meta.size = fsize;

		if (!isSizeCaled)
		{
			AddHashExecutionTotalSize(*executionContext, fsize);
		}
		else
		{
			ReplaceHashExecutionCountedFileSize(*executionContext, fSizes[fileIndex], fsize);
			fSizes[fileIndex] = fsize;
		}

#if defined (_WIN32)
#if (defined (FHASH_UWP_LIB) || defined(FHASH_WUI_LIB))
		WindowsComm::FileVersionHelper fvHelper(osFile);
		tstrFileVersion = fvHelper.Find();
		result.meta.version = tstrFileVersion;
		osFile.seek(0, OsFile::OsFileSeekFrom::OF_SEEK_BEGIN);
#else
		tstrFileVersion = WindowsComm::GetExeFileVersion((TCHAR *)path);
		result.meta.version = tstrFileVersion;
#endif
#endif

		EmitMetaResult(executionContext, result);
		return fsize;
	}

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

	const tstring& GetFinalizedDigestValue(const FinalizedDigestBundle& digestBundle, ResultDigestType digestType)
	{
		return GetDigestStorageValue(digestBundle, digestType);
	}

	void SetFinalizedDigestValue(FinalizedDigestBundle& digestBundle, ResultDigestType digestType, const tstring& digestValue)
	{
		SetDigestStorageValue(digestBundle, digestType, digestValue);
	}

	void PopulateDigestResult(const HashRequest& request, HashResult& result, const FinalizedDigestBundle& digestBundle)
	{
		result.digests.clear();
		VisitHashRequestAlgorithms(request, [&](ResultDigestType digestType)
		{
			const tstring& digestValue = GetFinalizedDigestValue(digestBundle, digestType);
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

	void FinalizeDigestStrings(const HashRequest& request, FileHashContexts& hashContexts, FinalizedDigestBundle& digestBundle)
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
				SetFinalizedDigestValue(digestBundle, RESULT_DIGEST_MD5, strtotstr(string(chHashBuff)));
				break;
			case RESULT_DIGEST_SHA1:
				hashContexts.sha1.Final();
				hashContexts.sha1.ReportHash(strSHA1, CSHA1::REPORT_HEX);
				SetFinalizedDigestValue(digestBundle, RESULT_DIGEST_SHA1, strtotstr(string(strSHA1)));
				break;
			case RESULT_DIGEST_SHA256:
				sha256_final(&hashContexts.sha256Ctx);
				sha256_digest(&hashContexts.sha256Ctx, &strSHA256);
				SetFinalizedDigestValue(digestBundle, RESULT_DIGEST_SHA256, strtotstr(strSHA256));
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
				SetFinalizedDigestValue(digestBundle, RESULT_DIGEST_SHA512, strtotstr(strSHA512));
				break;
			}

			return true;
		});
	}

}
