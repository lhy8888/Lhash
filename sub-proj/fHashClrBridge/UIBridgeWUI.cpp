#include "stdafx.h"

#include "UIBridgeWUI.h"

#include "Common/Global.h"
#include "ClrHelper.h"
using namespace System;
using namespace FilesHashWUI;
using namespace sunjwbase;

UIBridgeWUI::UIBridgeWUI(UIBridgeDelegates^ uiBridgeDelegates)
	:m_uiBridgeDelegates(uiBridgeDelegates)
{
}

UIBridgeWUI::~UIBridgeWUI()
{
}

void UIBridgeWUI::lockData()
{
	// No need here.
}

void UIBridgeWUI::unlockData()
{
	// No need here.
}

static ManagedResultDispatchType GetManagedResultDispatchType(ProgressEventType eventType)
{
	switch (eventType)
	{
	case PROGRESS_EVENT_FILE_STARTED:
		return MANAGED_RESULT_DISPATCH_FILE_NAME;
	case PROGRESS_EVENT_FILE_META_READY:
		return MANAGED_RESULT_DISPATCH_FILE_META;
	case PROGRESS_EVENT_FILE_HASH_READY:
		return MANAGED_RESULT_DISPATCH_FILE_HASH;
	case PROGRESS_EVENT_FILE_FAILED:
	default:
		return MANAGED_RESULT_DISPATCH_FILE_ERROR;
	}
}

String^ UIBridgeWUI::ConvertManagedResultText(const TCHAR* resultText)
{
	return ConvertTstrToSystemString(resultText);
}

void UIBridgeWUI::DispatchProjectedResultToDelegate(const HashResult& result, ManagedResultDispatchType dispatchType, bool uppercase)
{
	DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)
	{
		return ConvertManagedResultText(resultText);
	}, [&](HashResultNet hashResultNet)
	{
		m_uiBridgeDelegates->ShowFileName(hashResultNet);
	}, [&](HashResultNet hashResultNet)
	{
		m_uiBridgeDelegates->ShowFileMeta(hashResultNet);
	}, [&](HashResultNet hashResultNet, bool hashUppercase)
	{
		m_uiBridgeDelegates->ShowFileHash(hashResultNet, hashUppercase);
	}, [&](HashResultNet hashResultNet)
	{
		m_uiBridgeDelegates->ShowFileErr(hashResultNet);
	});
}

void UIBridgeWUI::DispatchDelegateActionByType(ManagedDelegateActionType actionType, int value)
{
	DispatchManagedBridgeDelegateActionByType(actionType, value, [&]()
	{
		m_uiBridgeDelegates->PreparingCalc();
	}, [&]()
	{
		m_uiBridgeDelegates->RemovePreparingCalc();
	}, [&]()
	{
		m_uiBridgeDelegates->CalcStop();
	}, [&]()
	{
		m_uiBridgeDelegates->CalcFinish();
	}, [&](int progressValue)
	{
		m_uiBridgeDelegates->UpdateProgWhole(progressValue);
	});
}

int UIBridgeWUI::DispatchDelegateQueryByType(ManagedDelegateQueryType queryType)
{
	return DispatchManagedBridgeDelegateQueryByType<int>(queryType, [&]()
	{
		return m_uiBridgeDelegates->GetProgMax();
	});
}

void UIBridgeWUI::onJobPreparing()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_PREPARING_CALC);
}

void UIBridgeWUI::onJobPreparationFinished()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_REMOVE_PREPARING_CALC);
}

void UIBridgeWUI::onJobCancelled()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_CALC_STOP);
}

void UIBridgeWUI::onJobCompleted()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_CALC_FINISH);
}

void UIBridgeWUI::onFileResultEvent(const HashResult& result,
										ProgressEventType eventType,
										bool uppercaseDigest)
{
	DispatchProjectedResultToDelegate(result,
										GetManagedResultDispatchType(eventType),
										uppercaseDigest);
}

int UIBridgeWUI::queryProgressMax()
{
	return DispatchDelegateQueryByType(MANAGED_DELEGATE_QUERY_PROG_MAX);
}

void UIBridgeWUI::onFileProgressValue(int value)
{
}

void UIBridgeWUI::onTotalProgressValue(int value)
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_UPDATE_PROG_WHOLE, value);
}

void UIBridgeWUI::onFileCalculated()
{
}

void UIBridgeWUI::onFileFinished()
{
}
