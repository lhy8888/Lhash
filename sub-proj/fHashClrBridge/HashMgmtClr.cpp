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

static cli::array<HashResultNet>^ CreateProjectedHashResultNetArray(size_t resultCount)
{
	return gcnew cli::array<HashResultNet>(static_cast<int>(resultCount));
}

static void SetProjectedHashResultNet(cli::array<HashResultNet>^ projectedResults, size_t index, HashResultNet hashResultNet)
{
	projectedResults[static_cast<int>(index)] = hashResultNet;
}

static ResultDataNet CreateCompatibilityResultDataNet(HashResultNet hashResultNet)
{
	ResultDataNet resultDataNet;
	resultDataNet.EnumState = static_cast<ResultStateNet>(hashResultNet.EnumState);
	resultDataNet.Path = hashResultNet.Path;
	resultDataNet.Size = hashResultNet.Size;
	resultDataNet.ModifiedDate = hashResultNet.ModifiedDate;
	resultDataNet.Version = hashResultNet.Version;
	resultDataNet.MD5 = hashResultNet.MD5;
	resultDataNet.SHA1 = hashResultNet.SHA1;
	resultDataNet.SHA256 = hashResultNet.SHA256;
	resultDataNet.SHA512 = hashResultNet.SHA512;
	resultDataNet.Error = hashResultNet.Error;
	return resultDataNet;
}

static cli::array<ResultDataNet>^ CreateCompatibilityResultDataNetArray(cli::array<HashResultNet>^ hashResultNetArray)
{
	if (hashResultNetArray == nullptr)
	{
		return nullptr;
	}

	cli::array<ResultDataNet>^ compatibilityResults = gcnew cli::array<ResultDataNet>(hashResultNetArray->Length);
	for (int resultIndex = 0; resultIndex < hashResultNetArray->Length; ++resultIndex)
	{
		compatibilityResults[resultIndex] = CreateCompatibilityResultDataNet(hashResultNetArray[resultIndex]);
	}

	return compatibilityResults;
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

cli::array<HashResultNet>^ HashMgmtClr::FindHashResults(String^ sstrHashToFind)
{
	return CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, cli::array<HashResultNet>^>(
		*m_pThreadData,
		tstring(ConvertSystemStringToTstr(sstrHashToFind)),
		CreateProjectedHashResultNetArray,
		ConvertTstrToSystemString,
		SetProjectedHashResultNet);
}

cli::array<ResultDataNet>^ HashMgmtClr::FindResult(String^ sstrHashToFind)
{
	return CreateCompatibilityResultDataNetArray(FindHashResults(sstrHashToFind));
}

UInt64 HashMgmtClr::GetResultCount()
{
	return GetThreadDataResultCount(*m_pThreadData);
}
