#pragma once

#include "HashResultNet.h"

namespace FilesHashUwp
{
	public delegate void HashJobEventHandler();
	public delegate void HashResultEventHandler(HashResultNet);
	public delegate void HashResultHashEventHandler(HashResultNet, Platform::Boolean);
	public delegate void HashProgressEventHandler(int32);

	public ref class UIBridgeDelegate sealed
	{
	public:
		UIBridgeDelegate();

		int32 GetProgressValueMax();

		void NotifyJobPreparing();
		void NotifyJobPreparationFinished();
		void NotifyJobCancelled();
		void NotifyJobCompleted();

		void PublishFileStarted(HashResultNet hashResultNet);
		void PublishFileMetadata(HashResultNet hashResultNet);
		void PublishFileHash(HashResultNet hashResultNet, Platform::Boolean uppercase);
		void PublishFileError(HashResultNet hashResultNet);

		void PublishTotalProgress(int32 value);

		event HashJobEventHandler^ JobPreparingHandler;
		event HashJobEventHandler^ JobPreparationFinishedHandler;
		event HashJobEventHandler^ JobCancelledHandler;
		event HashJobEventHandler^ JobCompletedHandler;

		event HashResultEventHandler^ FileStartedHandler;
		event HashResultEventHandler^ FileMetadataHandler;
		event HashResultHashEventHandler^ FileHashHandler;
		event HashResultEventHandler^ FileErrorHandler;

		event HashProgressEventHandler^ TotalProgressHandler;
	};
}
