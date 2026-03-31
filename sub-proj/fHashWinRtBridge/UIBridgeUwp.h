#pragma once

#include "Adapters/UiBridge/HashEngineBridge.h"
#include "Common/Global.h"
#include "Common/ManagedBridgeDispatch.h"

#include "UIBridgeDelegate.h"
#include "HashResultNet.h"

namespace FilesHashUwp
{
	class UIBridgeUwp : public HashEngineBridge
	{
	public:
		UIBridgeUwp(UIBridgeDelegate^ uiBridgeDelegate);
		virtual ~UIBridgeUwp();

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
		static Platform::String^ ConvertManagedResultText(const TCHAR* resultText);
		void DispatchProjectedResultToDelegate(const HashResult& result, ManagedResultDispatchType dispatchType, bool uppercase = false);
		void DispatchDelegateActionByType(ManagedDelegateActionType actionType, int value = 0);
		int DispatchDelegateQueryByType(ManagedDelegateQueryType queryType);

		UIBridgeDelegate^ m_uiBridgeDelegate;
	};
}
