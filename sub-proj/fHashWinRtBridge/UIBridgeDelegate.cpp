#include "stdafx.h"
#include "UIBridgeDelegate.h"

using namespace Platform;
using namespace FilesHashUwp;

UIBridgeDelegate::UIBridgeDelegate()
{
}

int32 UIBridgeDelegate::GetProgMax()
{
	return 100;
}

void UIBridgeDelegate::PreparingCalc()
{
	PreparingCalcHandler();
}

void UIBridgeDelegate::RemovePreparingCalc()
{
	RemovePreparingCalcHandler();
}

void UIBridgeDelegate::CalcStop()
{
	CalcStopHandler();
}

void UIBridgeDelegate::CalcFinish()
{
	CalcFinishHandler();
}

void UIBridgeDelegate::ShowFileName(HashResultNet hashResultNet)
{
	ShowFileNameHandler(hashResultNet);
}

void UIBridgeDelegate::ShowFileMeta(HashResultNet hashResultNet)
{
	ShowFileMetaHandler(hashResultNet);
}

void UIBridgeDelegate::ShowFileHash(HashResultNet hashResultNet, Boolean uppercase)
{
	ShowFileHashHandler(hashResultNet, uppercase);
}

void UIBridgeDelegate::ShowFileErr(HashResultNet hashResultNet)
{
	ShowFileErrHandler(hashResultNet);
}

void UIBridgeDelegate::UpdateProgWhole(int32 value)
{
	UpdateProgWholeHandler(value);
}
