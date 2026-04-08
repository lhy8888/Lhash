#include "stdafx.h"
#include "UIBridgeDelegates.h"

using namespace System;
using namespace FilesHashWUI;

UIBridgeDelegates::UIBridgeDelegates()
{
}

Int32 UIBridgeDelegates::GetProgressValueMax()
{
	return 100;
}

void UIBridgeDelegates::NotifyJobPreparing()
{
	JobPreparingHandler();
}

void UIBridgeDelegates::NotifyJobPreparationFinished()
{
	JobPreparationFinishedHandler();
}

void UIBridgeDelegates::NotifyJobCancelled()
{
	JobCancelledHandler();
}

void UIBridgeDelegates::NotifyJobCompleted()
{
	JobCompletedHandler();
}

void UIBridgeDelegates::PublishFileStarted(HashResultNet hashResultNet)
{
	FileStartedHandler(hashResultNet);
}

void UIBridgeDelegates::PublishFileMetadata(HashResultNet hashResultNet)
{
	FileMetadataHandler(hashResultNet);
}

void UIBridgeDelegates::PublishFileHash(HashResultNet hashResultNet, bool uppercase)
{
	FileHashHandler(hashResultNet, uppercase);
}

void UIBridgeDelegates::PublishFileError(HashResultNet hashResultNet)
{
	FileErrorHandler(hashResultNet);
}

void UIBridgeDelegates::PublishTotalProgress(Int32 value)
{
	TotalProgressHandler(value);
}
