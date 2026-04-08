#pragma once

#include "Adapters/UiBridge/HashEngineBridge.h"
#include "Common/Global.h"
#include "Common/ManagedBridgeDispatch.h"

#include "UIBridgeDelegate.h"
#include "HashResultNet.h"

namespace FilesHashUwp
{
	class UIBridgeUwp : public HashUiBridgeAdapter
	{
	public:
		UIBridgeUwp(UIBridgeDelegate^ hashUiEvents);
		virtual ~UIBridgeUwp();

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
		static Platform::String^ ConvertManagedResultText(const TCHAR* resultText);
		void DispatchProjectedResultEvent(const HashResult& result, ManagedResultEventType eventType, bool uppercase = false);
		void DispatchBridgeLifecycleEvent(ManagedBridgeLifecycleEventType eventType, int value = 0);
		int DispatchBridgeQuery(ManagedBridgeQueryType queryType);

		UIBridgeDelegate^ m_hashUiEvents;
	};
}
