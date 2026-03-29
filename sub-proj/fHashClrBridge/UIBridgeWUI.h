#pragma once

#include <msclr\auto_gcroot.h>

#include "Common/HashEngineBridge.h"
#include "Common/Global.h"
#include "Common/ManagedBridgeDispatch.h"

#include "UIBridgeDelegates.h"
#include "ResultDataNet.h"

namespace FilesHashWUI
{
	class UIBridgeWUI : public HashEngineBridge
	{
	public:
		UIBridgeWUI(UIBridgeDelegates^ uiBridgeDelegates);
		virtual ~UIBridgeWUI();

		virtual void lockData();
		virtual void unlockData();

		virtual void preparingCalc();
		virtual void removePreparingCalc();
		virtual void calcStop();
		virtual void calcFinish();

		virtual void showFileName(const HashResult& result);
		virtual void showFileMeta(const HashResult& result);
		virtual void showFileHash(const HashResult& result, bool uppercase);
		virtual void showFileErr(const HashResult& result);

		virtual int getProgMax();
		virtual void updateProg(int value);
	virtual void updateProgWhole(int value);

	virtual void fileCalcFinish();
	virtual void fileFinish();

	private:
		static System::String^ ConvertManagedResultText(const TCHAR* resultText);
		void DispatchProjectedResultToDelegate(const HashResult& result, ManagedResultDispatchType dispatchType, bool uppercase = false);
		void DispatchDelegateActionByType(ManagedDelegateActionType actionType, int value = 0);
		int DispatchDelegateQueryByType(ManagedDelegateQueryType queryType);

		msclr::auto_gcroot<UIBridgeDelegates^> m_uiBridgeDelegates;
	};
}
