#pragma once

#include "HashResultNet.h"

namespace FilesHashUwp
{
	public delegate void CalcEventHandler();
	public delegate void HashResultEventHandler(HashResultNet);
	public delegate void HashResultHashEventHandler(HashResultNet, Platform::Boolean);
	public delegate void CalcProgEventHandler(int32);

	public ref class UIBridgeDelegate sealed
	{
	public:
		UIBridgeDelegate();

		int32 GetProgMax();

		void PreparingCalc();
		void RemovePreparingCalc();
		void CalcStop();
		void CalcFinish();

		void ShowFileName(HashResultNet hashResultNet);
		void ShowFileMeta(HashResultNet hashResultNet);
		void ShowFileHash(HashResultNet hashResultNet, Platform::Boolean uppercase);
		void ShowFileErr(HashResultNet hashResultNet);

		void UpdateProgWhole(int32 value);

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
