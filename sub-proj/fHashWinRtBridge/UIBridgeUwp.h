#pragma once

#include "Common/HashEngineBridge.h"
#include "Common/Global.h"
#include "Common/ManagedBridgeDispatch.h"

#include "UIBridgeDelegate.h"
#include "ResultDataNet.h"

namespace FilesHashUwp
{
	class UIBridgeUwp : public HashEngineBridge
	{
	public:
		UIBridgeUwp(UIBridgeDelegate^ uiBridgeDelegate);
		virtual ~UIBridgeUwp();

		virtual void lockData();
		virtual void unlockData();

		virtual void preparingCalc();
		virtual void removePreparingCalc();
		virtual void calcStop();
		virtual void calcFinish();

		virtual void showFileName(const ResultData& result);
		virtual void showFileMeta(const ResultData& result);
		virtual void showFileHash(const ResultData& result, bool uppercase);
		virtual void showFileErr(const ResultData& result);

		virtual int getProgMax();
		virtual void updateProg(int value);
	virtual void updateProgWhole(int value);

	virtual void fileCalcFinish();
	virtual void fileFinish();

	private:
		static Platform::String^ ConvertManagedResultText(const TCHAR* resultText);
		void DispatchProjectedResultToDelegate(const ResultData& result, ManagedResultDispatchType dispatchType, bool uppercase = false);
		void DispatchDelegateActionByType(ManagedDelegateActionType actionType, int value = 0);
		int DispatchDelegateQueryByType(ManagedDelegateQueryType queryType);

		UIBridgeDelegate^ m_uiBridgeDelegate;
	};
}
