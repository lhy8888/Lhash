#pragma once

#include <msclr\auto_gcroot.h>

#include "Adapters/UiBridge/HashEngineBridge.h"
#include "Common/Global.h"
#include "Common/ManagedBridgeDispatch.h"

#include "UIBridgeDelegates.h"
#include "HashResultNet.h"

namespace FilesHashWUI
{
	class UIBridgeWUI : public HashEngineBridge
	{
	public:
		UIBridgeWUI(UIBridgeDelegates^ uiBridgeDelegates);
		virtual ~UIBridgeWUI();

		virtual void lockData();
		virtual void unlockData();

		virtual void onJobPreparing();
		virtual void onJobPreparationFinished();
		virtual void onJobCancelled();
		virtual void onJobCompleted();

		virtual void onFileResultEvent(const HashResult& result,
										ProgressEventType eventType,
										bool uppercaseDigest);

		virtual int queryProgressMax();
		virtual void onFileProgressValue(int value);
		virtual void onTotalProgressValue(int value);

		virtual void onFileCalculated();
		virtual void onFileFinished();

	private:
		static System::String^ ConvertManagedResultText(const TCHAR* resultText);
		void DispatchProjectedResultToDelegate(const HashResult& result, ManagedResultDispatchType dispatchType, bool uppercase = false);
		void DispatchDelegateActionByType(ManagedDelegateActionType actionType, int value = 0);
		int DispatchDelegateQueryByType(ManagedDelegateQueryType queryType);

		msclr::auto_gcroot<UIBridgeDelegates^> m_uiBridgeDelegates;
	};
}
