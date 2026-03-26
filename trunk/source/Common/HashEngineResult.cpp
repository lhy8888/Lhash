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
	uint64_t PrepareFileMetaResult(ThreadData *thrdData, HashEngineObserver *observer, ResultData& result,
		OsFile& osFile, const TCHAR *path, bool isSizeCaled, ULLongVector& fSizes, uint32_t fileIndex, tstring& tstrFileVersion)
	{
		SetResultModifiedDate(result, osFile.getModifiedTimeFormat());

		uint64_t fsize = osFile.getLength();
		SetResultSize(result, fsize);

		if (!isSizeCaled)
		{
			AddThreadDataTotalSize(*thrdData, fsize);
		}
		else
		{
			ReplaceThreadDataCountedFileSize(*thrdData, fSizes[fileIndex], fsize);
			fSizes[fileIndex] = fsize;
		}

#if defined (_WIN32)
#if (defined (FHASH_UWP_LIB) || defined(FHASH_WUI_LIB))
		WindowsComm::FileVersionHelper fvHelper(osFile);
		tstrFileVersion = fvHelper.Find();
		SetResultVersion(result, tstrFileVersion);
		osFile.seek(0, OsFile::OsFileSeekFrom::OF_SEEK_BEGIN);
#else
		tstrFileVersion = WindowsComm::GetExeFileVersion((TCHAR *)path);
		SetResultVersion(result, tstrFileVersion);
#endif
#endif

		EmitMetaResult(observer, result);
		return fsize;
	}

	void InitializeFileHashing(const ThreadData& threadData, HashEngineObserver *observer, FileHashContexts *hashContexts)
	{
		VisitEnabledThreadDataHashAlgorithms(threadData, [&](ResultDigestType digestType)
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

		observer->onFileProgress(0);
	}

	void UpdateWholeProgressAfterFile(HashEngineObserver *observer, ThreadData *thrdData, bool isSizeCaled, uint32_t fileIndex)
	{
		if (!isSizeCaled)
		{
			if (GetThreadDataFileCount(*thrdData) == 0)
			{
				observer->onTotalProgress(0);
			}
			else
			{
				int progressMax = observer->progressMax();
				observer->onTotalProgress((fileIndex + 1) * progressMax / (GetThreadDataFileCount(*thrdData)));
			}
		}
	}

	const tstring& GetFinalizedDigestValue(const FinalizedDigestBundle& digestBundle, ResultDigestType digestType)
	{
		return GetDigestStorageValue(digestBundle, digestType);
	}

	void SetFinalizedDigestValue(FinalizedDigestBundle& digestBundle, ResultDigestType digestType, const tstring& digestValue)
	{
		SetDigestStorageValue(digestBundle, digestType, digestValue);
	}

	void PopulateDigestResult(const ThreadData& threadData, ResultData& result, const FinalizedDigestBundle& digestBundle)
	{
		VisitEnabledThreadDataHashAlgorithms(threadData, [&](ResultDigestType digestType)
		{
			SetResultDigest(result, digestType, GetFinalizedDigestValue(digestBundle, digestType));
			return true;
		});
	}

	void FinalizeDigestStrings(const ThreadData& threadData, FileHashContexts& hashContexts, FinalizedDigestBundle& digestBundle)
	{
		char chHashBuff[1024] = { 0 };
		char strSHA1[256] = { 0 };
		string strSHA256;
		string strSHA512;

		VisitEnabledThreadDataHashAlgorithms(threadData, [&](ResultDigestType digestType)
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

	void CompleteSuccessfulFileHashing(HashEngineObserver *observer, ThreadData *thrdData, ResultData& result, uint32_t fileIndex, bool isSizeCaled, bool uppercase,
		FileExecutionState& executionState)
	{
		observer->onFileCalculated();

		FinalizeDigestStrings(*thrdData, executionState.hashContexts, executionState.digestBundle);
		UpdateWholeProgressAfterFile(observer, thrdData, isSizeCaled, fileIndex);

		executionState.fileAttemptState.osFile->close();

		PopulateDigestResult(*thrdData, result, executionState.digestBundle);
		if (HasAnyResultDigests(result))
		{
			EmitHashResult(observer, result, uppercase);
		}
	}

	void CompleteOpenedFileAttempt(HashEngineObserver *observer, ThreadData *thrdData, ResultData& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState)
	{
		if (executionState.fileAttemptState.readFailed)
		{
			executionState.fileAttemptState.osFile->close();
			EmitReadFileError(observer, result);
		}
		else
		{
			CompleteSuccessfulFileHashing(observer, thrdData, result, fileIndex, isSizeCaled, GetThreadDataUppercase(*thrdData), executionState);
		}

		FinishFileProcessing(observer);
	}

	void EmitMetaResult(HashEngineObserver *observer, ResultData& result)
	{
		SetResultState(result, RESULT_META);
		observer->onFileMetaReady(result);
	}

	void EmitHashResult(HashEngineObserver *observer, ResultData& result, bool uppercase)
	{
		SetResultState(result, RESULT_ALL);
		observer->onFileHashReady(result, uppercase);
	}

	void EmitErrorResult(HashEngineObserver *observer, ResultData& result)
	{
		SetResultState(result, RESULT_ERROR);
		observer->onFileFailed(result);
	}

	void EmitErrorMessageResult(HashEngineObserver *observer, ResultData& result, const tstring& errorText)
	{
		SetResultError(result, errorText);
		EmitErrorResult(observer, result);
	}

	void EmitOpenFileError(HashEngineObserver *observer, ResultData& result, const TCHAR *errorText)
	{
		EmitErrorMessageResult(observer, result, tstring(errorText));
	}

	void EmitReadFileError(HashEngineObserver *observer, ResultData& result)
	{
		EmitErrorMessageResult(observer, result, strtotstr(string("Failed to read file while hashing.")));
	}

	void FinishFileProcessing(HashEngineObserver *observer)
	{
		observer->onFileFinished();
	}

	void CompleteFileAttempt(HashEngineObserver *observer, ThreadData *thrdData, ResultData& result, uint32_t fileIndex, bool isSizeCaled,
		FileExecutionState& executionState)
	{
		if (executionState.fileAttemptState.isFileOpened)
		{
			CompleteOpenedFileAttempt(observer, thrdData, result, fileIndex, isSizeCaled, executionState);
		}
		else
		{
			EmitOpenFileError(observer, result, executionState.fileAttemptState.openErrorText);
			FinishFileProcessing(observer);
		}
	}
}
