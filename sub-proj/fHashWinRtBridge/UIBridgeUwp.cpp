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

void UIBridgeUwp::lockData()
{
	// No need here.
}

void UIBridgeUwp::unlockData()
{
	// No need here.
}

String^ UIBridgeUwp::ConvertManagedResultText(const TCHAR* resultText)
{
	return ConvertToPlatStr(resultText);
}

void UIBridgeUwp::DispatchProjectedResultToDelegate(const ResultData& result, ManagedResultDispatchType dispatchType, bool uppercase)
{
	DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)
	{
		return ConvertManagedResultText(resultText);
	}, [&](ResultDataNet resultDataNet)
	{
		m_uiBridgeDelegate->ShowFileName(resultDataNet);
	}, [&](ResultDataNet resultDataNet)
	{
		m_uiBridgeDelegate->ShowFileMeta(resultDataNet);
	}, [&](ResultDataNet resultDataNet, bool hashUppercase)
	{
		m_uiBridgeDelegate->ShowFileHash(resultDataNet, hashUppercase);
	}, [&](ResultDataNet resultDataNet)
	{
		m_uiBridgeDelegate->ShowFileErr(resultDataNet);
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

void UIBridgeUwp::preparingCalc()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_PREPARING_CALC);
}

void UIBridgeUwp::removePreparingCalc()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_REMOVE_PREPARING_CALC);
}

void UIBridgeUwp::calcStop()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_CALC_STOP);
}

void UIBridgeUwp::calcFinish()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_CALC_FINISH);
}

void UIBridgeUwp::showFileName(const ResultData& result)
{
	DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_NAME);
}

void UIBridgeUwp::showFileMeta(const ResultData& result)
{
	DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_META);
}

void UIBridgeUwp::showFileHash(const ResultData& result, bool uppercase)
{
	DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_HASH, uppercase);
}

void UIBridgeUwp::showFileErr(const ResultData& result)
{
	DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_ERROR);
}

int UIBridgeUwp::getProgMax()
{
	return DispatchDelegateQueryByType(MANAGED_DELEGATE_QUERY_PROG_MAX);
}

void UIBridgeUwp::updateProg(int value)
{
}

void UIBridgeUwp::updateProgWhole(int value)
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_UPDATE_PROG_WHOLE, value);
}

void UIBridgeUwp::fileCalcFinish()
{
}

void UIBridgeUwp::fileFinish()
{
}
