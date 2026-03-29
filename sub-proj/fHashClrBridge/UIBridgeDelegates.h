#pragma once

#include "HashResultNet.h"

namespace FilesHashWUI
{
	public delegate void CalcEventHandler();
	public delegate void HashResultEventHandler(HashResultNet);
	public delegate void HashResultHashEventHandler(HashResultNet, bool);
	public delegate void CalcProgEventHandler(System::Int32);

	public ref class UIBridgeDelegates sealed
	{
	public:
		UIBridgeDelegates();

		System::Int32 GetProgMax();

		void PreparingCalc();
		void RemovePreparingCalc();
		void CalcStop();
		void CalcFinish();

		void ShowFileName(HashResultNet hashResultNet);
		void ShowFileMeta(HashResultNet hashResultNet);
		void ShowFileHash(HashResultNet hashResultNet, bool uppercase);
		void ShowFileErr(HashResultNet hashResultNet);

		void UpdateProgWhole(System::Int32 value);

		event CalcEventHandler^ PreparingCalcHandler;
		event CalcEventHandler^ RemovePreparingCalcHandler;
		event CalcEventHandler^ CalcStopHandler;
		event CalcEventHandler^ CalcFinishHandler;

		event HashResultEventHandler^ ShowFileNameHandler;
		event HashResultEventHandler^ ShowFileMetaHandler;
		event HashResultHashEventHandler^ ShowFileHashHandler;
		event HashResultEventHandler^ ShowFileErrHandler;

		event CalcProgEventHandler^ UpdateProgWholeHandler;
	};
}
