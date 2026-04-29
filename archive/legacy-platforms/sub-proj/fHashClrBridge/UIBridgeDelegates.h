#pragma once

#include "HashResultNet.h"

namespace FilesHashWUI
{
	public delegate void HashJobEventHandler();
	public delegate void HashResultEventHandler(HashResultNet);
	public delegate void HashResultHashEventHandler(HashResultNet, bool);
	public delegate void HashProgressEventHandler(System::Int32);

	public ref class UIBridgeDelegates sealed
	{
	public:
		UIBridgeDelegates();

		System::Int32 GetProgressValueMax();

		void NotifyJobPreparing();
		void NotifyJobPreparationFinished();
		void NotifyJobCancelled();
		void NotifyJobCompleted();

		void PublishFileStarted(HashResultNet hashResultNet);
		void PublishFileMetadata(HashResultNet hashResultNet);
		void PublishFileHash(HashResultNet hashResultNet, bool uppercase);
		void PublishFileError(HashResultNet hashResultNet);

		void PublishTotalProgress(System::Int32 value);

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
