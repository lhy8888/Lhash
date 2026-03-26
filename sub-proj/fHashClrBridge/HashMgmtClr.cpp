#include "stdafx.h"

#include "HashMgmtClr.h"
#include "ClrHelper.h"
#include "Common/strhelper.h"
#include "Common/ResultDataSearch.h"
#include "Common/ResultDataProjection.h"
#include "Common/ThreadDataAccess.h"
#include "Common/HashEngine.h"
using namespace std;
using namespace System;
using namespace FilesHashWUI;
using namespace sunjwbase;

static bool TryConvertHashAlgorithmDigestType(int digestTypeValue, ResultDigestType *digestType)
{
	return TryGetHashAlgorithmType(digestTypeValue, digestType);
}

static bool TryConvertHashAlgorithmType(HashAlgorithmTypeNet hashAlgorithm, ResultDigestType *digestType)
{
	return TryConvertHashAlgorithmDigestType(static_cast<int>(hashAlgorithm), digestType);
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

static cli::array<ResultDataNet>^ CreateProjectedResultDataNetArray(size_t resultCount)
{
	return gcnew cli::array<ResultDataNet>(static_cast<int>(resultCount));
}

static void SetProjectedResultDataNet(cli::array<ResultDataNet>^ projectedResults, size_t index, ResultDataNet resultDataNet)
{
	projectedResults[static_cast<int>(index)] = resultDataNet;
}

static HashAlgorithmDescriptorNet^ CreateHashAlgorithmDescriptorNet(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	HashAlgorithmDescriptorNet^ descriptorNet = gcnew HashAlgorithmDescriptorNet();
	descriptorNet->DigestType = static_cast<int>(GetHashAlgorithmDescriptorType(algorithmDescriptor));
	sunjwbase::tstring stableName = GetHashAlgorithmDescriptorStableName(algorithmDescriptor);
	sunjwbase::tstring displayLabel = GetHashAlgorithmDescriptorDisplayLabel(algorithmDescriptor);
	descriptorNet->StableName = ConvertTstrToSystemString(stableName.c_str());
	descriptorNet->DisplayLabel = ConvertTstrToSystemString(displayLabel.c_str());
	return descriptorNet;
}

static cli::array<HashAlgorithmDescriptorNet^>^ CreateSupportedHashAlgorithmDescriptors()
{
	cli::array<HashAlgorithmDescriptorNet^>^ algorithmDescriptors = gcnew cli::array<HashAlgorithmDescriptorNet^>(GetRegisteredHashAlgorithmCount());

	for (int algorithmIndex = 0; algorithmIndex < GetRegisteredHashAlgorithmCount(); ++algorithmIndex)
	{
		algorithmDescriptors[algorithmIndex] = CreateHashAlgorithmDescriptorNet(GetHashAlgorithmDescriptorAt(algorithmIndex));
	}

	return algorithmDescriptors;
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

cli::array<HashAlgorithmDescriptorNet^>^ HashMgmtClr::GetSupportedHashAlgorithms()
{
	return CreateSupportedHashAlgorithmDescriptors();
}

void HashMgmtClr::SetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm, bool val)
{
	SetHashAlgorithmEnabledByDigestType(static_cast<int>(hashAlgorithm), val);
}

bool HashMgmtClr::GetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm)
{
	return GetHashAlgorithmEnabledByDigestType(static_cast<int>(hashAlgorithm));
}

void HashMgmtClr::SetHashAlgorithmEnabledByDigestType(int digestTypeValue, bool val)
{
	ResultDigestType digestType;
	if (!TryConvertHashAlgorithmDigestType(digestTypeValue, &digestType))
	{
		return;
	}

	SetThreadDataHashAlgorithmEnabled(*m_pThreadData, digestType, val);
}

bool HashMgmtClr::GetHashAlgorithmEnabledByDigestType(int digestTypeValue)
{
	ResultDigestType digestType;
	if (!TryConvertHashAlgorithmDigestType(digestTypeValue, &digestType))
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

	return CreateProjectedDigestMatchingResults<ResultDataNet, ResultStateNet, cli::array<ResultDataNet>^>(GetThreadDataResults(*m_pThreadData), tstrHashToFind, CreateProjectedResultDataNetArray, ConvertTstrToSystemString, SetProjectedResultDataNet);
}

UInt64 HashMgmtClr::GetResultCount()
{
	return GetThreadDataResultCount(*m_pThreadData);
}
