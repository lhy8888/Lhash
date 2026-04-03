#include "stdafx.h"

#include "HashMgmt.h"
#include "CxHelper.h"
#include "Common/ManagedHashMgmtAccess.h"
#include "Common/HashThreadLaunch.h"
using namespace std;
using namespace Platform;
using namespace FilesHashUwp;
using namespace sunjwbase;

static HashAlgorithmDescriptorNet^ CreateHashAlgorithmDescriptorNetInstance()
{
	return ref new HashAlgorithmDescriptorNet();
}

static Array<HashAlgorithmDescriptorNet^>^ CreateHashAlgorithmDescriptorNetArray(size_t algorithmCount)
{
	return ref new Array<HashAlgorithmDescriptorNet^>(static_cast<unsigned int>(algorithmCount));
}

static sunjwbase::tstring ConvertManagedFilePathToTstr(String^ filePath)
{
	return tstring(filePath->Data());
}

static Array<HashAlgorithmDescriptorNet^>^ CreateSupportedHashAlgorithmDescriptors()
{
	return CreateSupportedManagedHashAlgorithmDescriptors<HashAlgorithmDescriptorNet^, Array<HashAlgorithmDescriptorNet^>^>(
		CreateHashAlgorithmDescriptorNetArray,
		CreateHashAlgorithmDescriptorNetInstance,
		ConvertToPlatStr);
}

static Array<HashResultNet>^ CreateProjectedHashResultNetArray(size_t resultCount)
{
	return ref new Array<HashResultNet>(static_cast<unsigned int>(resultCount));
}

static void SetProjectedHashResultNet(Array<HashResultNet>^ projectedResults, size_t index, HashResultNet hashResultNet)
{
	projectedResults[static_cast<unsigned int>(index)] = hashResultNet;
}

HashMgmt::HashMgmt(UIBridgeDelegate^ uiBridgeDelegate)
	:m_hWorkThread(NULL)
{
	m_spUiBridgeUwp = make_shared<UIBridgeUwp>(uiBridgeDelegate);
}

void HashMgmt::Init()
{
	SetThreadDataObserver(m_threadData, m_spUiBridgeUwp.get());
}

void HashMgmt::Clear()
{
	ResetThreadDataForNewSession(m_threadData);
}

void HashMgmt::SetStop(Boolean val)
{
	SetThreadDataStop(m_threadData, val);
}

void HashMgmt::SetUppercase(Boolean val)
{
	SetThreadDataUppercase(m_threadData, val);
}

void HashMgmt::ResetHashAlgorithms()
{
	ResetThreadDataHashAlgorithms(m_threadData);
}

Array<HashAlgorithmDescriptorNet^>^ HashMgmt::GetSupportedHashAlgorithms()
{
	return CreateSupportedHashAlgorithmDescriptors();
}

void HashMgmt::SetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm, Boolean val)
{
	SetHashAlgorithmEnabledByDigestType(static_cast<int>(hashAlgorithm), val);
}

Boolean HashMgmt::GetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm)
{
	return GetHashAlgorithmEnabledByDigestType(static_cast<int>(hashAlgorithm));
}

void HashMgmt::SetHashAlgorithmEnabledByDigestType(int digestTypeValue, Boolean val)
{
	::SetManagedHashAlgorithmEnabledByDigestType(m_threadData, digestTypeValue, val);
}

Boolean HashMgmt::GetHashAlgorithmEnabledByDigestType(int digestTypeValue)
{
	return ::GetManagedHashAlgorithmEnabledByDigestType(m_threadData, digestTypeValue);
}

uint64 HashMgmt::GetTotalSize()
{
	return GetThreadDataTotalSize(m_threadData);
}

void HashMgmt::AddFiles(const Array<String^>^ filePaths)
{
	ReplaceThreadDataInputFilesFromManagedArray(m_threadData, filePaths, ConvertManagedFilePathToTstr);
}

void HashMgmt::StartHashThread()
{
	if (!HasEnabledThreadDataHashAlgorithms(m_threadData))
	{
		throw ref new FailureException(L"At least one hash algorithm must be enabled.");
	}

	unsigned int thredID = 0;
	RestartHashWorkerThread(&m_hWorkThread, &m_threadData, &thredID);
}

Array<HashResultNet>^ HashMgmt::FindHashResults(String^ pstrHashToFind)
{
	return CreateProjectedManagedDigestMatchingHashResults<HashResultNet, HashResultStateNet, Array<HashResultNet>^>(
		m_threadData,
		tstring(pstrHashToFind->Data()),
		CreateProjectedHashResultNetArray,
		ConvertToPlatStr,
		SetProjectedHashResultNet);
}
