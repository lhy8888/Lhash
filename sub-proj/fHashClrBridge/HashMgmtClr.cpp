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

UInt64 HashMgmtClr::GetTotalSize()
{
	return GetThreadDataTotalSize(*m_pThreadData);
}

void HashMgmtClr::AddFiles(cli::array<String^>^ filePaths)
{
	ResetThreadDataInputFilesAndAppend(*m_pThreadData, filePaths->Length, [&](uint32_t fileIndex)
	{
		return tstring(ConvertSystemStringToTstr(filePaths[fileIndex]));
	});
}

void HashMgmtClr::StartHashThread()
{
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

	return CreateProjectedDigestMatchingResults<ResultDataNet, ResultStateNet, cli::array<ResultDataNet>^>(GetThreadDataResults(*m_pThreadData), tstrHashToFind, [&](size_t resultCount)
	{
		return gcnew cli::array<ResultDataNet>(resultCount);
	}, [&](const TCHAR* resultText)
	{
		return ConvertTstrToSystemString(resultText);
	}, [&](cli::array<ResultDataNet>^ projectedResults, size_t index, ResultDataNet resultDataNet)
	{
		projectedResults[index] = resultDataNet;
	});
}

UInt64 HashMgmtClr::GetResultCount()
{
	return GetThreadDataResultCount(*m_pThreadData);
}
