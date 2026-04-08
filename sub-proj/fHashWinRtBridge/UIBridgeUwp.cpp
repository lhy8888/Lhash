#include "stdafx.h"

#include "UIBridgeUwp.h"
#include "Common/Global.h"
#include "CxHelper.h"
using namespace Platform;
using namespace FilesHashUwp;
using namespace sunjwbase;

UIBridgeUwp::UIBridgeUwp(UIBridgeDelegate^ hashUiEvents)
	:m_hashUiEvents(hashUiEvents)
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

static ManagedResultEventType GetManagedResultEventType(ProgressEventType eventType)
{
	switch (eventType)
	{
	case PROGRESS_EVENT_FILE_STARTED:
		return MANAGED_RESULT_EVENT_FILE_STARTED;
	case PROGRESS_EVENT_FILE_META_READY:
		return MANAGED_RESULT_EVENT_FILE_META_READY;
	case PROGRESS_EVENT_FILE_HASH_READY:
		return MANAGED_RESULT_EVENT_FILE_HASH_READY;
	case PROGRESS_EVENT_FILE_FAILED:
	default:
		return MANAGED_RESULT_EVENT_FILE_FAILED;
	}
}

String^ UIBridgeUwp::ConvertManagedResultText(const TCHAR* resultText)
{
	return ConvertToPlatStr(resultText);
}

void UIBridgeUwp::DispatchProjectedResultEvent(const HashResult& result, ManagedResultEventType eventType, bool uppercase)
{
	DispatchManagedBridgeResultEventByType<HashResultNet, HashResultStateNet>(result, eventType, uppercase, [&](const TCHAR* resultText)
	{
		return ConvertManagedResultText(resultText);
	}, [&](HashResultNet hashResultNet)
	{
		m_hashUiEvents->PublishFileStarted(hashResultNet);
	}, [&](HashResultNet hashResultNet)
	{
		m_hashUiEvents->PublishFileMetadata(hashResultNet);
	}, [&](HashResultNet hashResultNet, bool hashUppercase)
	{
		m_hashUiEvents->PublishFileHash(hashResultNet, hashUppercase);
	}, [&](HashResultNet hashResultNet)
	{
		m_hashUiEvents->PublishFileError(hashResultNet);
	});
}

void UIBridgeUwp::DispatchBridgeLifecycleEvent(ManagedBridgeLifecycleEventType eventType, int value)
{
	DispatchManagedBridgeLifecycleEventByType(eventType, value, [&]()
	{
		m_hashUiEvents->NotifyJobPreparing();
	}, [&]()
	{
		m_hashUiEvents->NotifyJobPreparationFinished();
	}, [&]()
	{
		m_hashUiEvents->NotifyJobCancelled();
	}, [&]()
	{
		m_hashUiEvents->NotifyJobCompleted();
	}, [&](int progressValue)
	{
		m_hashUiEvents->PublishTotalProgress(progressValue);
	});
}

int UIBridgeUwp::DispatchBridgeQuery(ManagedBridgeQueryType queryType)
{
	return DispatchManagedBridgeQueryByType<int>(queryType, [&]()
	{
		return m_hashUiEvents->GetProgressValueMax();
	});
}

void UIBridgeUwp::handleJobPreparingEvent()
{
	DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_PREPARING);
}

void UIBridgeUwp::handleJobPreparationFinishedEvent()
{
	DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_PREPARATION_FINISHED);
}

void UIBridgeUwp::handleJobCancelledEvent()
{
	DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_CANCELLED);
}

void UIBridgeUwp::handleJobCompletedEvent()
{
	DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_COMPLETED);
}

void UIBridgeUwp::handleFileResultProgressEvent(const HashResult& result,
													ProgressEventType eventType,
													bool uppercaseDigest)
{
	DispatchProjectedResultEvent(result,
									GetManagedResultEventType(eventType),
									uppercaseDigest);
}

int UIBridgeUwp::getProgressValueMax()
{
	return DispatchBridgeQuery(MANAGED_BRIDGE_QUERY_PROGRESS_VALUE_MAX);
}

void UIBridgeUwp::handleFileProgressEvent(int value)
{
}

void UIBridgeUwp::handleTotalProgressEvent(int value)
{
	DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_TOTAL_PROGRESS, value);
}

void UIBridgeUwp::handleFileCalculatedEvent()
{
}

void UIBridgeUwp::handleFileFinishedEvent()
{
}
