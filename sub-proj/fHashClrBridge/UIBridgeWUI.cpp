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

String^ UIBridgeWUI::ConvertManagedResultText(const TCHAR* resultText)
{
	return ConvertTstrToSystemString(resultText);
}

void UIBridgeWUI::DispatchProjectedResultToDelegate(const ResultData& result, ManagedResultDispatchType dispatchType, bool uppercase)
{
	DispatchManagedBridgeResultByType<ResultDataNet, ResultStateNet>(result, dispatchType, uppercase, [&](const TCHAR* resultText)
	{
		return ConvertManagedResultText(resultText);
	}, [&](ResultDataNet resultDataNet)
	{
		m_uiBridgeDelegates->ShowFileName(resultDataNet);
	}, [&](ResultDataNet resultDataNet)
	{
		m_uiBridgeDelegates->ShowFileMeta(resultDataNet);
	}, [&](ResultDataNet resultDataNet, bool hashUppercase)
	{
		m_uiBridgeDelegates->ShowFileHash(resultDataNet, hashUppercase);
	}, [&](ResultDataNet resultDataNet)
	{
		m_uiBridgeDelegates->ShowFileErr(resultDataNet);
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

void UIBridgeWUI::preparingCalc()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_PREPARING_CALC);
}

void UIBridgeWUI::removePreparingCalc()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_REMOVE_PREPARING_CALC);
}

void UIBridgeWUI::calcStop()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_CALC_STOP);
}

void UIBridgeWUI::calcFinish()
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_CALC_FINISH);
}

void UIBridgeWUI::showFileName(const ResultData& result)
{
	DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_NAME);
}

void UIBridgeWUI::showFileMeta(const ResultData& result)
{
	DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_META);
}

void UIBridgeWUI::showFileHash(const ResultData& result, bool uppercase)
{
	DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_HASH, uppercase);
}

void UIBridgeWUI::showFileErr(const ResultData& result)
{
	DispatchProjectedResultToDelegate(result, MANAGED_RESULT_DISPATCH_FILE_ERROR);
}

int UIBridgeWUI::getProgMax()
{
	return DispatchDelegateQueryByType(MANAGED_DELEGATE_QUERY_PROG_MAX);
}

void UIBridgeWUI::updateProg(int value)
{
}

void UIBridgeWUI::updateProgWhole(int value)
{
	DispatchDelegateActionByType(MANAGED_DELEGATE_ACTION_UPDATE_PROG_WHOLE, value);
}

void UIBridgeWUI::fileCalcFinish()
{
}

void UIBridgeWUI::fileFinish()
{
}
