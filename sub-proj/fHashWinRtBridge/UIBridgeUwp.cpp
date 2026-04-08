#include "stdafx.h"

#include "UIBridgeUwp.h"
#include "Common/Global.h"
#include "CxHelper.h"
using namespace Platform;
using namespace FilesHashUwp;
using namespace sunjwbase;

UIBridgeUwp::UIBridgeUwp(UIBridgeDelegate^ uiBridgeDelegate)
	:m_uiBridgeDelegate(uiBridgeDelegate)
{
}

UIBridgeUwp::~UIBridgeUwp()
{
}

void UIBridgeUwp::lockBridgeData()
{
	// No need here.
}

void UIBridgeUwp::unlockBridgeData()
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

String^ UIBridgeUwp::ConvertManagedResultText(const TCHAR* resultText)
{
	return ConvertToPlatStr(resultText);
}

void UIBridgeUwp::DispatchProjectedResultToDelegate(const HashResult& result, ManagedResultDispatchType dispatchType, bool uppercase)
{
	DispatchManagedBridgeResultByType<HashResultNet, HashResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)
	{
		return ConvertManagedResultText(resultText);
	}, [&](HashResultNet hashResultNet)
	{
		m_uiBridgeDelegate->ShowFileName(hashResultNet);
	}, [&](HashResultNet hashResultNet)
	{
		m_uiBridgeDelegate->ShowFileMeta(hashResultNet);
	}, [&](HashResultNet hashResultNet, bool hashUppercase)
	{
		m_uiBridgeDelegate->ShowFileHash(hashResultNet, hashUppercase);
	}, [&](HashResultNet hashResultNet)
	{
		m_uiBridgeDelegate->ShowFileErr(hashResultNet);
	});
}

void UIBridgeUwp::DispatchDelegateActionByType(ManagedDelegateActionType actionType, int value)
{
	DispatchManagedBridgeDelegateActionByType(actionType, value, [&]()
	{
		m_uiBridgeDelegate->PreparingCalc();
	}, [&]()
	{
		m_uiBridgeDelegate->RemovePreparingCalc();
	}, [&]()
	{
		m_uiBridgeDelegate->CalcStop();
	}, [&]()
	{
		m_uiBridgeDelegate->CalcFinish();
	}, [&](int progressValue)
	{
		m_uiBridgeDelegate->UpdateProgWhole(progressValue);
	});
}

int UIBridgeUwp::DispatchDelegateQueryByType(ManagedDelegateQueryType queryType)
{
	return DispatchManagedBridgeDelegateQueryByType<int>(queryType, [&]()
	{
		return m_uiBridgeDelegate->GetProgMax();
	});
}

void UIBridgeUwp::handleJobPreparingEvent()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_PREPARING_CALC);
}

void UIBridgeUwp::handleJobPreparationFinishedEvent()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_REMOVE_PREPARING_CALC);
}

void UIBridgeUwp::handleJobCancelledEvent()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_CALC_STOP);
}

void UIBridgeUwp::handleJobCompletedEvent()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_CALC_FINISH);
}

void UIBridgeUwp::handleFileResultProgressEvent(const HashResult& result,
													ProgressEventType eventType,
													bool uppercaseDigest)
{
	DispatchProjectedResultToDelegate(result,
										GetManagedResultDispatchType(eventType),
										uppercaseDigest);
}

int UIBridgeUwp::getProgressValueMax()
{
	return DispatchDelegateQueryByType(MANAGED_DELEGATE_QUERY_PROG_MAX);
}

void UIBridgeUwp::handleFileProgressEvent(int value)
{
}

void UIBridgeUwp::handleTotalProgressEvent(int value)
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_UPDATE_PROG_WHOLE, value);
}

void UIBridgeUwp::handleFileCalculatedEvent()
{
}

void UIBridgeUwp::handleFileFinishedEvent()
{
}
