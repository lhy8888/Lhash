#include "stdafx.h"
#include "UIBridgeDelegate.h"

using namespace Platform;
using namespace FilesHashUwp;

UIBridgeDelegate::UIBridgeDelegate()
{
}

int32 UIBridgeDelegate::GetProgressValueMax()
{
	return 100;
}

void UIBridgeDelegate::NotifyJobPreparing()
{
	JobPreparingHandler();
}

void UIBridgeDelegate::NotifyJobPreparationFinished()
{
	JobPreparationFinishedHandler();
}

void UIBridgeDelegate::NotifyJobCancelled()
{
	JobCancelledHandler();
}

void UIBridgeDelegate::NotifyJobCompleted()
{
	JobCompletedHandler();
}

void UIBridgeDelegate::PublishFileStarted(HashResultNet hashResultNet)
{
	FileStartedHandler(hashResultNet);
}

void UIBridgeDelegate::PublishFileMetadata(HashResultNet hashResultNet)
{
	FileMetadataHandler(hashResultNet);
}

void UIBridgeDelegate::PublishFileHash(HashResultNet hashResultNet, Boolean uppercase)
{
	FileHashHandler(hashResultNet, uppercase);
}

void UIBridgeDelegate::PublishFileError(HashResultNet hashResultNet)
{
	FileErrorHandler(hashResultNet);
}

void UIBridgeDelegate::PublishTotalProgress(int32 value)
{
	TotalProgressHandler(value);
}
