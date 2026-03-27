#include "stdafx.h"

#include "HashMgmt.h"
#include "CxHelper.h"
#include "Common/ManagedHashMgmtAccess.h"
#include "Common/HashEngine.h"
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

static Array<ResultDataNet>^ CreateProjectedResultDataNetArray(size_t resultCount)
{
	return ref new Array<ResultDataNet>(static_cast<unsigned int>(resultCount));
}

static void SetProjectedResultDataNet(Array<ResultDataNet>^ projectedResults, size_t index, ResultDataNet resultDataNet)
{
	projectedResults[static_cast<unsigned int>(index)] = resultDataNet;
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

	if (m_hWorkThread)
	{
		CloseHandle(m_hWorkThread);
	}
	DWORD thredID;
	m_hWorkThread = (HANDLE)_beginthreadex(NULL,
										0,
										(unsigned int (WINAPI*)(void*))HashThreadFunc,
										&m_threadData,
										0,
										(unsigned int*)&thredID);
}

Array<ResultDataNet>^ HashMgmt::FindResult(String^ pstrHashToFind)
{
	return CreateProjectedManagedDigestMatchingResults<ResultDataNet, ResultStateNet, Array<ResultDataNet>^>(
		m_threadData,
		tstring(pstrHashToFind->Data()),
		CreateProjectedResultDataNetArray,
		ConvertToPlatStr,
		SetProjectedResultDataNet);
}
