#include "stdafx.h"

#include "HashMgmt.h"
#include "CxHelper.h"
#include "Common/strhelper.h"
#include "Common/ResultDataAccess.h"
#include "Common/ResultDigestAccess.h"
#include "Common/ThreadDataAccess.h"
#include "Common/HashEngine.h"
using namespace std;
using namespace Platform;
using namespace FilesHashUwp;
using namespace sunjwbase;

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
