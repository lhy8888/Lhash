#ifndef _MANAGED_BRIDGE_DISPATCH_H_
#define _MANAGED_BRIDGE_DISPATCH_H_

#include "Common/ResultDataAccess.h"

enum ManagedResultDispatchType
{
	MANAGED_RESULT_DISPATCH_FILE_NAME,
	MANAGED_RESULT_DISPATCH_FILE_META,
	MANAGED_RESULT_DISPATCH_FILE_HASH,
	MANAGED_RESULT_DISPATCH_FILE_ERROR
};

template<typename TResultDataNet, typename TFileNameAction, typename TFileMetaAction, typename TFileHashAction, typename TFileErrorAction>
static inline void DispatchManagedResultByType(ManagedResultDispatchType dispatchType, TResultDataNet resultDataNet, bool uppercase, TFileNameAction onFileName, TFileMetaAction onFileMeta, TFileHashAction onFileHash, TFileErrorAction onFileError)
{
	switch (dispatchType)
	{
	case MANAGED_RESULT_DISPATCH_FILE_NAME:
		onFileName(resultDataNet);
		break;
	case MANAGED_RESULT_DISPATCH_FILE_META:
		onFileMeta(resultDataNet);
		break;
	case MANAGED_RESULT_DISPATCH_FILE_HASH:
		onFileHash(resultDataNet, uppercase);
		break;
	case MANAGED_RESULT_DISPATCH_FILE_ERROR:
		onFileError(resultDataNet);
		break;
	}
}

enum ManagedDelegateActionType
{
	MANAGED_DELEGATE_ACTION_PREPARING_CALC,
	MANAGED_DELEGATE_ACTION_REMOVE_PREPARING_CALC,
	MANAGED_DELEGATE_ACTION_CALC_STOP,
	MANAGED_DELEGATE_ACTION_CALC_FINISH,
	MANAGED_DELEGATE_ACTION_UPDATE_PROG_WHOLE
};

template<typename TPreparingCalcAction, typename TRemovePreparingCalcAction, typename TCalcStopAction, typename TCalcFinishAction, typename TUpdateProgWholeAction>
static inline void DispatchManagedDelegateActionByType(ManagedDelegateActionType actionType, int value, TPreparingCalcAction onPreparingCalc, TRemovePreparingCalcAction onRemovePreparingCalc, TCalcStopAction onCalcStop, TCalcFinishAction onCalcFinish, TUpdateProgWholeAction onUpdateProgWhole)
{
	switch (actionType)
	{
	case MANAGED_DELEGATE_ACTION_PREPARING_CALC:
		onPreparingCalc();
		break;
	case MANAGED_DELEGATE_ACTION_REMOVE_PREPARING_CALC:
		onRemovePreparingCalc();
		break;
	case MANAGED_DELEGATE_ACTION_CALC_STOP:
		onCalcStop();
		break;
	case MANAGED_DELEGATE_ACTION_CALC_FINISH:
		onCalcFinish();
		break;
	case MANAGED_DELEGATE_ACTION_UPDATE_PROG_WHOLE:
		onUpdateProgWhole(value);
		break;
	}
}

enum ManagedDelegateQueryType
{
	MANAGED_DELEGATE_QUERY_PROG_MAX
};

template<typename TProgMaxQuery>
static inline int DispatchManagedDelegateQueryByType(ManagedDelegateQueryType queryType, TProgMaxQuery queryProgMax)
{
	switch (queryType)
	{
	case MANAGED_DELEGATE_QUERY_PROG_MAX:
		return queryProgMax();
	}

	return 0;
}

template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter, typename TFileNameAction, typename TFileMetaAction, typename TFileHashAction, typename TFileErrorAction>
static inline void DispatchManagedBridgeResultByType(const ResultData& result, ManagedResultDispatchType dispatchType, bool uppercase, TStringConverter convertString, TFileNameAction onFileName, TFileMetaAction onFileMeta, TFileHashAction onFileHash, TFileErrorAction onFileError)
{
	TResultDataNet resultDataNet = ProjectResultDataToNet<TResultDataNet, TResultStateNet>(result, convertString);
	DispatchManagedResultByType(dispatchType, resultDataNet, uppercase, onFileName, onFileMeta, onFileHash, onFileError);
}

template<typename TPreparingCalcAction, typename TRemovePreparingCalcAction, typename TCalcStopAction, typename TCalcFinishAction, typename TUpdateProgWholeAction>
static inline void DispatchManagedBridgeDelegateActionByType(ManagedDelegateActionType actionType, int value, TPreparingCalcAction onPreparingCalc, TRemovePreparingCalcAction onRemovePreparingCalc, TCalcStopAction onCalcStop, TCalcFinishAction onCalcFinish, TUpdateProgWholeAction onUpdateProgWhole)
{
	DispatchManagedDelegateActionByType(actionType, value, [&]()
	{
		onPreparingCalc();
	}, [&]()
	{
		onRemovePreparingCalc();
	}, [&]()
	{
		onCalcStop();
	}, [&]()
	{
		onCalcFinish();
	}, [&](int progressValue)
	{
		onUpdateProgWhole(progressValue);
	});
}

template<typename TResult, typename TProgMaxQuery>
static inline TResult DispatchManagedBridgeDelegateQueryByType(ManagedDelegateQueryType queryType, TProgMaxQuery queryProgMax)
{
	return DispatchManagedDelegateQueryByType(queryType, [&]()
	{
		return queryProgMax();
	});
}

#endif
