#include "stdafx.h"

#include "HashMgmt.h"
#include "CxHelper.h"
#include "Common/strhelper.h"
#include "Common/ResultDataSearch.h"
#include "Common/ResultDataProjection.h"
#include "Common/ThreadDataAccess.h"
#include "Common/HashEngine.h"
using namespace std;
using namespace Platform;
using namespace FilesHashUwp;
using namespace sunjwbase;

static bool TryConvertHashAlgorithmDigestType(int digestTypeValue, ResultDigestType *digestType)
{
	return TryGetHashAlgorithmType(digestTypeValue, digestType);
}

static bool TryConvertHashAlgorithmType(HashAlgorithmTypeNet hashAlgorithm, ResultDigestType *digestType)
{
	return TryConvertHashAlgorithmDigestType(static_cast<int>(hashAlgorithm), digestType);
}

static HashAlgorithmDescriptorNet^ CreateHashAlgorithmDescriptorNet(const HashAlgorithmDescriptor& algorithmDescriptor)
{
	HashAlgorithmDescriptorNet^ descriptorNet = ref new HashAlgorithmDescriptorNet();
	descriptorNet->DigestType = static_cast<int>(GetHashAlgorithmDescriptorType(algorithmDescriptor));
	sunjwbase::tstring stableName = GetHashAlgorithmDescriptorStableName(algorithmDescriptor);
	sunjwbase::tstring displayLabel = GetHashAlgorithmDescriptorDisplayLabel(algorithmDescriptor);
	descriptorNet->StableName = ConvertToPlatStr(stableName.c_str());
	descriptorNet->DisplayLabel = ConvertToPlatStr(displayLabel.c_str());
	return descriptorNet;
}

static Array<HashAlgorithmDescriptorNet^>^ CreateSupportedHashAlgorithmDescriptors()
{
	Array<HashAlgorithmDescriptorNet^>^ algorithmDescriptors = ref new Array<HashAlgorithmDescriptorNet^>(GetRegisteredHashAlgorithmCount());

	for (int algorithmIndex = 0; algorithmIndex < GetRegisteredHashAlgorithmCount(); ++algorithmIndex)
	{
		algorithmDescriptors[algorithmIndex] = CreateHashAlgorithmDescriptorNet(GetHashAlgorithmDescriptorAt(algorithmIndex));
	}

	return algorithmDescriptors;
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
	ResultDigestType digestType;
	if (!TryConvertHashAlgorithmDigestType(digestTypeValue, &digestType))
	{
		return;
	}

	SetThreadDataHashAlgorithmEnabled(m_threadData, digestType, val);
}

Boolean HashMgmt::GetHashAlgorithmEnabledByDigestType(int digestTypeValue)
{
	ResultDigestType digestType;
	if (!TryConvertHashAlgorithmDigestType(digestTypeValue, &digestType))
	{
		return false;
	}

	return IsThreadDataHashAlgorithmEnabled(m_threadData, digestType);
}

uint64 HashMgmt::GetTotalSize()
{
	return GetThreadDataTotalSize(m_threadData);
}

void HashMgmt::AddFiles(const Array<String^>^ filePaths)
{
	ResetThreadDataInputFilesAndAppend(m_threadData, filePaths->Length, [&](uint32_t fileIndex)
	{
		return tstring(filePaths[fileIndex]->Data());
	});
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
	tstring tstrHashToFind(pstrHashToFind->Data());
	tstrHashToFind = NormalizeDigestSearchText(tstrHashToFind);

	return CreateProjectedDigestMatchingResults<ResultDataNet, ResultStateNet, Array<ResultDataNet>^>(GetThreadDataResults(m_threadData), tstrHashToFind, [&](size_t resultCount)
	{
		return ref new Array<ResultDataNet>(resultCount);
	}, [&](const TCHAR* resultText)
	{
		return ConvertToPlatStr(resultText);
	}, [&](Array<ResultDataNet>^ projectedResults, size_t index, ResultDataNet resultDataNet)
	{
		projectedResults[index] = resultDataNet;
	});
}
