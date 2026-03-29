#include "stdafx.h"
#include "UIBridgeDelegates.h"

using namespace System;
using namespace FilesHashWUI;

UIBridgeDelegates::UIBridgeDelegates()
{
}

Int32 UIBridgeDelegates::GetProgMax()
{
	return 100;
}

void UIBridgeDelegates::PreparingCalc()
{
	PreparingCalcHandler();
}

void UIBridgeDelegates::RemovePreparingCalc()
{
	RemovePreparingCalcHandler();
}

void UIBridgeDelegates::CalcStop()
{
	CalcStopHandler();
}

void UIBridgeDelegates::CalcFinish()
{
	CalcFinishHandler();
}

void UIBridgeDelegates::ShowFileName(HashResultNet hashResultNet)
{
	ShowFileNameHandler(hashResultNet);
}

void UIBridgeDelegates::ShowFileMeta(HashResultNet hashResultNet)
{
	ShowFileMetaHandler(hashResultNet);
}

void UIBridgeDelegates::ShowFileHash(HashResultNet hashResultNet, bool uppercase)
{
	ShowFileHashHandler(hashResultNet, uppercase);
}

void UIBridgeDelegates::ShowFileErr(HashResultNet hashResultNet)
{
	ShowFileErrHandler(hashResultNet);
}

void UIBridgeDelegates::UpdateProgWhole(Int32 value)
{
	UpdateProgWholeHandler(value);
}
