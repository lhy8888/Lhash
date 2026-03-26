#include "stdafx.h"

#include "HashMgmtClr.h"
#include "ClrHelper.h"
#include "Common/strhelper.h"
#include "Common/ResultDataAccess.h"
#include "Common/ResultDigestAccess.h"
#include "Common/ThreadDataAccess.h"
#include "Common/HashEngine.h"
using namespace std;
using namespace System;
using namespace FilesHashWUI;
using namespace sunjwbase;

static bool TryConvertHashAlgorithmType(HashAlgorithmTypeNet hashAlgorithm, ResultDigestType *digestType)
{
	switch (hashAlgorithm)
	{
	case HashAlgorithmTypeNet::MD5:
		*digestType = RESULT_DIGEST_MD5;
		return true;
	case HashAlgorithmTypeNet::SHA1:
		*digestType = RESULT_DIGEST_SHA1;
		return true;
	case HashAlgorithmTypeNet::SHA256:
		*digestType = RESULT_DIGEST_SHA256;
		return true;
	case HashAlgorithmTypeNet::SHA512:
		*digestType = RESULT_DIGEST_SHA512;
		return true;
	default:
		return false;
	}
}

static TStrVector ConvertSystemStringArrayToTStrVector(cli::array<String^>^ filePaths)
{
	TStrVector fullPaths;
	for (int fileIndex = 0; fileIndex < filePaths->Length; ++fileIndex)
	{
		fullPaths.push_back(tstring(ConvertSystemStringToTstr(filePaths[fileIndex])));
	}
	return fullPaths;
}

HashMgmtClr::HashMgmtClr(UIBridgeDelegates^ uiBridgeDelegates)
	:m_pUiBridgeWUI(NULL), m_pThreadData(NULL), m_hWorkThread(NULL)
{
	m_pUiBridgeWUI = new UIBridgeWUI(uiBridgeDelegates);
	m_pThreadData = new ThreadData();
	SetThreadDataObserver(*m_pThreadData, m_pUiBridgeWUI);
}

HashMgmtClr::!HashMgmtClr()
{
	if (m_pUiBridgeWUI)
		delete m_pUiBridgeWUI;
	if (m_pThreadData)
		delete m_pThreadData;
	if (m_hWorkThread)
		CloseHandle(m_hWorkThread);
}

void HashMgmtClr::Init()
{
}

void HashMgmtClr::Clear()
{
	ResetThreadDataForNewSession(*m_pThreadData);
}

void HashMgmtClr::SetStop(bool val)
{
	SetThreadDataStop(*m_pThreadData, val);
}

void HashMgmtClr::SetUppercase(bool val)
{
	SetThreadDataUppercase(*m_pThreadData, val);
}

void HashMgmtClr::ResetHashAlgorithms()
{
	ResetThreadDataHashAlgorithms(*m_pThreadData);
}

void HashMgmtClr::SetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm, bool val)
{
	ResultDigestType digestType;
	if (!TryConvertHashAlgorithmType(hashAlgorithm, &digestType))
	{
		return;
	}

	SetThreadDataHashAlgorithmEnabled(*m_pThreadData, digestType, val);
}

bool HashMgmtClr::GetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm)
{
	ResultDigestType digestType;
	if (!TryConvertHashAlgorithmType(hashAlgorithm, &digestType))
	{
		return false;
	}

	return IsThreadDataHashAlgorithmEnabled(*m_pThreadData, digestType);
}

UInt64 HashMgmtClr::GetTotalSize()
{
	return GetThreadDataTotalSize(*m_pThreadData);
}

void HashMgmtClr::AddFiles(cli::array<String^>^ filePaths)
{
	ReplaceThreadDataInputFiles(*m_pThreadData, ConvertSystemStringArrayToTStrVector(filePaths));
}

void HashMgmtClr::StartHashThread()
{
	if (!HasEnabledThreadDataHashAlgorithms(*m_pThreadData))
	{
		throw gcnew InvalidOperationException("At least one hash algorithm must be enabled.");
	}

	if (m_hWorkThread)
	{
		CloseHandle(m_hWorkThread);
		m_hWorkThread = NULL;
	}
	DWORD thredID;
	m_hWorkThread = (HANDLE)_beginthreadex(NULL,
										0,
										(unsigned int (WINAPI*)(void*))HashThreadFunc,
										m_pThreadData,
										0,
										(unsigned int*)&thredID);
}

cli::array<ResultDataNet>^ HashMgmtClr::FindResult(String^ sstrHashToFind)
{
	tstring tstrHashToFind(ConvertSystemStringToTstr(sstrHashToFind));
	tstrHashToFind = NormalizeDigestSearchText(tstrHashToFind);

	const ResultList& resultList = GetThreadDataResults(*m_pThreadData);
	cli::array<ResultDataNet>^ projectedResults = gcnew cli::array<ResultDataNet>(static_cast<int>(CountDigestMatchingResults(resultList, tstrHashToFind)));
	int projectedIndex = 0;
	ResultList::const_iterator itr = resultList.begin();
	for (; itr != resultList.end(); ++itr)
	{
		if (ResultMatchesDigestText(*itr, tstrHashToFind))
		{
			projectedResults[projectedIndex] = ProjectResultDataToNet<ResultDataNet, ResultStateNet>(*itr, ConvertTstrToSystemString);
			++projectedIndex;
		}
	}
	return projectedResults;
}

UInt64 HashMgmtClr::GetResultCount()
{
	return GetThreadDataResultCount(*m_pThreadData);
}
