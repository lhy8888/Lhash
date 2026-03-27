#include "stdafx.h"

#include "HashMgmtClr.h"
#include "ClrHelper.h"
#include "Common/ManagedHashMgmtAccess.h"
#include "Common/HashEngine.h"
using namespace std;
using namespace System;
using namespace FilesHashWUI;
using namespace sunjwbase;

static HashAlgorithmDescriptorNet^ CreateHashAlgorithmDescriptorNetInstance()
{
	return gcnew HashAlgorithmDescriptorNet();
}

static cli::array<HashAlgorithmDescriptorNet^>^ CreateHashAlgorithmDescriptorNetArray(size_t algorithmCount)
{
	return gcnew cli::array<HashAlgorithmDescriptorNet^>(static_cast<int>(algorithmCount));
}

static cli::array<ResultDataNet>^ CreateProjectedResultDataNetArray(size_t resultCount)
{
	return gcnew cli::array<ResultDataNet>(static_cast<int>(resultCount));
}

static void SetProjectedResultDataNet(cli::array<ResultDataNet>^ projectedResults, size_t index, ResultDataNet resultDataNet)
{
	projectedResults[static_cast<int>(index)] = resultDataNet;
}

static sunjwbase::tstring ConvertManagedFilePathToTstr(String^ filePath)
{
	return tstring(ConvertSystemStringToTstr(filePath));
}

static cli::array<HashAlgorithmDescriptorNet^>^ CreateSupportedHashAlgorithmDescriptors()
{
	return CreateSupportedManagedHashAlgorithmDescriptors<HashAlgorithmDescriptorNet^, cli::array<HashAlgorithmDescriptorNet^>^>(
		CreateHashAlgorithmDescriptorNetArray,
		CreateHashAlgorithmDescriptorNetInstance,
		ConvertTstrToSystemString);
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
	::SetManagedHashAlgorithmEnabledByDigestType(*m_pThreadData, digestTypeValue, val);
}

bool HashMgmtClr::GetHashAlgorithmEnabledByDigestType(int digestTypeValue)
{
	return ::GetManagedHashAlgorithmEnabledByDigestType(*m_pThreadData, digestTypeValue);
}

UInt64 HashMgmtClr::GetTotalSize()
{
	return GetThreadDataTotalSize(*m_pThreadData);
}

void HashMgmtClr::AddFiles(cli::array<String^>^ filePaths)
{
	ReplaceThreadDataInputFilesFromManagedArray(*m_pThreadData, filePaths, ConvertManagedFilePathToTstr);
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
	return CreateProjectedManagedDigestMatchingResults<ResultDataNet, ResultStateNet, cli::array<ResultDataNet>^>(
		*m_pThreadData,
		tstring(ConvertSystemStringToTstr(sstrHashToFind)),
		CreateProjectedResultDataNetArray,
		ConvertTstrToSystemString,
		SetProjectedResultDataNet);
}

UInt64 HashMgmtClr::GetResultCount()
{
	return GetThreadDataResultCount(*m_pThreadData);
}
