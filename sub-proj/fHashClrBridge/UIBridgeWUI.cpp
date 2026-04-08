#include "stdafx.h"

#include "UIBridgeWUI.h"

#include "Common/Global.h"
#include "ClrHelper.h"
using namespace System;
using namespace FilesHashWUI;
using namespace sunjwbase;

UIBridgeWUI::UIBridgeWUI(UIBridgeDelegates^ hashUiEvents)
	:m_hashUiEvents(hashUiEvents)
{
}

UIBridgeWUI::~UIBridgeWUI()
{
}

void UIBridgeWUI::lockBridgeData()
{
	// No need here.
}

void UIBridgeWUI::unlockBridgeData()
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

String^ UIBridgeWUI::ConvertManagedResultText(const TCHAR* resultText)
{
	return ConvertTstrToSystemString(resultText);
}

void UIBridgeWUI::DispatchProjectedResultEvent(const HashResult& result, ManagedResultEventType eventType, bool uppercase)
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

void UIBridgeWUI::DispatchBridgeLifecycleEvent(ManagedBridgeLifecycleEventType eventType, int value)
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

int UIBridgeWUI::DispatchBridgeQuery(ManagedBridgeQueryType queryType)
{
	return DispatchManagedBridgeQueryByType<int>(queryType, [&]()
	{
		return m_hashUiEvents->GetProgressValueMax();
	});
}

void UIBridgeWUI::handleJobPreparingEvent()
{
	DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_PREPARING);
}

void UIBridgeWUI::handleJobPreparationFinishedEvent()
{
	DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_PREPARATION_FINISHED);
}

void UIBridgeWUI::handleJobCancelledEvent()
{
	DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_CANCELLED);
}

void UIBridgeWUI::handleJobCompletedEvent()
{
	DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_JOB_COMPLETED);
}

void UIBridgeWUI::handleFileResultProgressEvent(const HashResult& result,
													ProgressEventType eventType,
													bool uppercaseDigest)
{
	DispatchProjectedResultEvent(result,
									GetManagedResultEventType(eventType),
									uppercaseDigest);
}

int UIBridgeWUI::getProgressValueMax()
{
	return DispatchBridgeQuery(MANAGED_BRIDGE_QUERY_PROGRESS_VALUE_MAX);
}

void UIBridgeWUI::handleFileProgressEvent(int value)
{
}

void UIBridgeWUI::handleTotalProgressEvent(int value)
{
	DispatchBridgeLifecycleEvent(MANAGED_BRIDGE_LIFECYCLE_TOTAL_PROGRESS, value);
}

void UIBridgeWUI::handleFileCalculatedEvent()
{
}

void UIBridgeWUI::handleFileFinishedEvent()
{
}
