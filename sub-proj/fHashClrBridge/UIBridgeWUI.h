#pragma once

#include <msclr\auto_gcroot.h>

#include "Adapters/UiBridge/HashEngineBridge.h"
#include "Common/Global.h"
#include "Common/ManagedBridgeDispatch.h"

#include "UIBridgeDelegates.h"
#include "HashResultNet.h"

namespace FilesHashWUI
{
	class UIBridgeWUI : public HashUiBridgeAdapter
	{
	public:
		UIBridgeWUI(UIBridgeDelegates^ uiBridgeDelegates);
		virtual ~UIBridgeWUI();

		virtual void lockBridgeData();
		virtual void unlockBridgeData();

		virtual void handleJobPreparingEvent();
		virtual void handleJobPreparationFinishedEvent();
		virtual void handleJobCancelledEvent();
		virtual void handleJobCompletedEvent();

		virtual void handleFileResultProgressEvent(const HashResult& result,
													ProgressEventType eventType,
													bool uppercaseDigest);

		virtual int getProgressValueMax();
		virtual void handleFileProgressEvent(int value);
		virtual void handleTotalProgressEvent(int value);

		virtual void handleFileCalculatedEvent();
		virtual void handleFileFinishedEvent();

	private:
		static System::String^ ConvertManagedResultText(const TCHAR* resultText);
		void DispatchProjectedResultToDelegate(const HashResult& result, ManagedResultDispatchType dispatchType, bool uppercase = false);
		void DispatchDelegateActionByType(ManagedDelegateActionType actionType, int value = 0);
		int DispatchDelegateQueryByType(ManagedDelegateQueryType queryType);

		msclr::auto_gcroot<UIBridgeDelegates^> m_uiBridgeDelegates;
	};
}
