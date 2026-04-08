#include "stdafx.h"

#include "HashMgmtClr.h"
#include "ClrHelper.h"
#include "LegacyCompat/ManagedHashMgmtAccess.h"
#include "LegacyCompat/HashThreadLaunch.h"
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

static sunjwbase::tstring ConvertManagedFilePathToTstr(String^ filePath)
{
	return tstring(ConvertSystemStringToTstr(filePath));
}

static sunjwbase::tstring ConvertManagedAlgorithmIdToTstr(String^ algorithmId)
{
	if (algorithmId == nullptr)
	{
		return tstring();
	}

	return tstring(ConvertSystemStringToTstr(algorithmId));
}

static cli::array<HashAlgorithmDescriptorNet^>^ CreateSupportedHashAlgorithmDescriptors()
{
	return CreateSupportedManagedHashAlgorithmDescriptors<HashAlgorithmDescriptorNet^, cli::array<HashAlgorithmDescriptorNet^>^>(
		CreateHashAlgorithmDescriptorNetArray,
		CreateHashAlgorithmDescriptorNetInstance,
		ConvertTstrToSystemString);
}

HashMgmtClr::HashMgmtClr(UIBridgeDelegates^ uiBridgeDelegates)
	:m_pUiBridgeWUI(NULL), m_pThreadData(NULL), m_pWorkThread(NULL)
{
	m_pUiBridgeWUI = new UIBridgeWUI(uiBridgeDelegates);
	m_pThreadData = new ThreadData();
	m_pWorkThread = new WinHandleGuard::UniqueWinHandle();
	SetThreadDataObserver(*m_pThreadData, m_pUiBridgeWUI);
}

HashMgmtClr::!HashMgmtClr()
{
	if (m_pWorkThread)
	{
		CloseHashWorkerThreadHandle(m_pWorkThread);
		delete m_pWorkThread;
		m_pWorkThread = NULL;
	}
	if (m_pUiBridgeWUI)
	{
		delete m_pUiBridgeWUI;
		m_pUiBridgeWUI = NULL;
	}
	if (m_pThreadData)
	{
		delete m_pThreadData;
		m_pThreadData = NULL;
	}
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

void HashMgmtClr::SetHashAlgorithmEnabledById(String^ algorithmId, bool val)
{
	::SetManagedHashAlgorithmEnabledById(*m_pThreadData, ConvertManagedAlgorithmIdToTstr(algorithmId), val);
}

bool HashMgmtClr::GetHashAlgorithmEnabledById(String^ algorithmId)
{
	return ::GetManagedHashAlgorithmEnabledById(*m_pThreadData, ConvertManagedAlgorithmIdToTstr(algorithmId));
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

	unsigned int thredID = 0;
	RestartHashWorkerThread(m_pWorkThread, m_pThreadData, &thredID);
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

UInt64 HashMgmtClr::GetResultCount()
{
	return GetThreadDataResultCount(*m_pThreadData);
}
