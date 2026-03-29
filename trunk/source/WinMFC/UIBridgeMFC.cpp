#include "stdafx.h"

#include "UIBridgeMFC.h"

#include <stdlib.h>
#include <stdio.h>
#include <string>
#include <Windows.h>

#include "Common/strhelper.h"
#include "Common/Global.h"
#include "Common/ResultDataRender.h"
#include "Common/ResultDigestRender.h"
#include "Common/Utils.h"
#include "WindowsUtils.h"
#include "WinCommon/WindowsStrings.h"

using namespace std;
using namespace sunjwbase;

UIBridgeMFC::UIBridgeMFC(HWND hWnd,
						 OsMutex *mainMtx,
						 CHyperEditHash *hyperEdit)
:m_hWnd(hWnd), m_mainMtx(mainMtx), m_mainHyperEdit(hyperEdit)
{
}

UIBridgeMFC::~UIBridgeMFC()
{
}

void UIBridgeMFC::lockData()
{
	m_mainMtx->lock();
}

void UIBridgeMFC::unlockData()
{
	m_mainMtx->unlock();
}

void UIBridgeMFC::preparingCalc()
{
	PostThreadInfoMessage(WP_WORKING);

	UpdateMainHyperEdit([&](CHyperEditHash *hyperEdit)
	{
		m_preparingSnapshot = CaptureHyperEditSnapshot(hyperEdit);
		if (m_preparingSnapshot.text == tstring(GetStringByKey(MAINDLG_INITINFO)))
		{
			// Initial state
			m_preparingSnapshot.text = _T("");
			m_preparingSnapshot.linkOffsets.clear();
			hyperEdit->ClearTextBuffer();
		}

		AppendTextLineToHyperEdit(GetStringByKey(MAINDLG_WAITING_START), hyperEdit);
	}, true);
}

void UIBridgeMFC::removePreparingCalc()
{
	UpdateMainHyperEdit([&](CHyperEditHash *hyperEdit)
	{
		// Restore and remove MAINDLG_WAITING_START
		RestoreHyperEditSnapshot(m_preparingSnapshot, hyperEdit);
	});
}

void UIBridgeMFC::calcStop()
{
	PostThreadInfoMessage(WP_STOPPED);
}

void UIBridgeMFC::calcFinish()
{
	PostThreadInfoMessage(WP_FINISHED);
}

void UIBridgeMFC::showFileName(const HashResult& result)
{
	ResultData compatibilityResult = CreateCompatibilityResultData(result);
	AppendResultSectionAndRefresh(compatibilityResult, RESULT_RENDER_SECTION_FILE_NAME, false);
}

void UIBridgeMFC::showFileMeta(const HashResult& result)
{
	ResultData compatibilityResult = CreateCompatibilityResultData(result);
	AppendResultSectionAndRefresh(compatibilityResult, RESULT_RENDER_SECTION_META, false);
}

void UIBridgeMFC::showFileHash(const HashResult& result, bool uppercase)
{
	ResultData compatibilityResult = CreateCompatibilityResultData(result);
	AppendResultSectionAndRefresh(compatibilityResult, RESULT_RENDER_SECTION_HASH, uppercase);
}

void UIBridgeMFC::showFileErr(const HashResult& result)
{
	ResultData compatibilityResult = CreateCompatibilityResultData(result);
	AppendResultSectionAndRefresh(compatibilityResult, RESULT_RENDER_SECTION_ERROR, false);
}

int UIBridgeMFC::getProgMax()
{
	return 100;
}

void UIBridgeMFC::updateProg(int value)
{
	//::PostMessage(m_hWnd, WM_THREAD_INFO, WP_PROG, value);
}

void UIBridgeMFC::updateProgWhole(int value)
{
	PostThreadInfoMessage(WP_PROG_WHOLE, value);
}

void UIBridgeMFC::fileCalcFinish()
{
}

void UIBridgeMFC::fileFinish()
{
}

void UIBridgeMFC::AppendLineBreakToHyperEdit(CHyperEditHash *hyerEdit)
{
	hyerEdit->AppendTextToBuffer(_T("\r\n"));
}

void UIBridgeMFC::AppendTextLineToHyperEdit(const tstring& text,
											CHyperEditHash *hyerEdit)
{
	hyerEdit->AppendTextToBuffer(text.c_str());
	AppendLineBreakToHyperEdit(hyerEdit);
}

void UIBridgeMFC::AppendLabelValueToHyperEdit(const TCHAR *label,
												const tstring& value,
												CHyperEditHash *hyerEdit)
{
	hyerEdit->AppendTextToBuffer(label);
	hyerEdit->AppendTextToBuffer(_T(" "));
	hyerEdit->AppendTextToBuffer(value.c_str());
}

void UIBridgeMFC::AppendLabelValueLineToHyperEdit(const TCHAR *label,
													const tstring& value,
													CHyperEditHash *hyerEdit)
{
	AppendLabelValueToHyperEdit(label, value, hyerEdit);
	AppendLineBreakToHyperEdit(hyerEdit);
}

void UIBridgeMFC::AppendLabelLinkLineToHyperEdit(const TCHAR *label,
												const tstring& value,
												CHyperEditHash *hyerEdit)
{
	hyerEdit->AppendTextToBuffer(label);
	hyerEdit->AppendTextToBuffer(_T(": "));
	hyerEdit->AppendLinkToBuffer(value.c_str());
	AppendLineBreakToHyperEdit(hyerEdit);
}

UIBridgeMFC::MainHyperEditSnapshot UIBridgeMFC::CaptureHyperEditSnapshot(CHyperEditHash *hyperEdit)
{
	MainHyperEditSnapshot snapshot;
	snapshot.text = hyperEdit->GetTextBuffer().GetBuffer();
	hyperEdit->CopyLinkOffsets(snapshot.linkOffsets);
	return snapshot;
}

void UIBridgeMFC::RestoreHyperEditSnapshot(const MainHyperEditSnapshot& snapshot,
											CHyperEditHash *hyperEdit)
{
	hyperEdit->ClearTextBuffer();
	hyperEdit->AppendTextToBuffer(snapshot.text.c_str());
	hyperEdit->SetLinkOffsets(snapshot.linkOffsets);
}

UIBridgeMFC::ResultMetaLineDisplayInfo UIBridgeMFC::GetResultMetaLineDisplayInfo(const ResultData& result,
																				ResultMetaLineType metaLine)
{
	ResultMetaLineDisplayInfo metaLineDisplayInfo;

	DispatchResultMetaLineByType(metaLine, [&]()
	{
		ResultSizeDisplayInfo resultSizeDisplayInfo = GetResultSizeDisplayInfo(result);

		metaLineDisplayInfo.label = GetStringByKey(FILESIZE_STRING);
		metaLineDisplayInfo.value = resultSizeDisplayInfo.sizeText;
		metaLineDisplayInfo.suffix = _T(" ");
		metaLineDisplayInfo.suffix += GetStringByKey(BYTE_STRING);
		if (resultSizeDisplayInfo.shortSizeText.length() > 0)
		{
			metaLineDisplayInfo.suffix += _T(" (");
			metaLineDisplayInfo.suffix += resultSizeDisplayInfo.shortSizeText;
			metaLineDisplayInfo.suffix += _T(")");
		}
	}, [&]()
	{
		metaLineDisplayInfo.label = GetStringByKey(MODIFYTIME_STRING);
		metaLineDisplayInfo.value = GetResultModifiedDate(result);
	}, [&]()
	{
		metaLineDisplayInfo.label = GetStringByKey(VERSION_STRING);
		metaLineDisplayInfo.value = GetResultVersion(result);
	});

	return metaLineDisplayInfo;
}

void UIBridgeMFC::AppendResultMetaLineDisplayInfoToHyperEdit(const ResultMetaLineDisplayInfo& metaLineDisplayInfo,
															CHyperEditHash *hyerEdit)
{
	AppendLabelValueToHyperEdit(metaLineDisplayInfo.label.c_str(),
								metaLineDisplayInfo.value,
								hyerEdit);
	hyerEdit->AppendTextToBuffer(metaLineDisplayInfo.suffix.c_str());
}

void UIBridgeMFC::AppendResultDigestDisplayInfoToHyperEdit(const ResultDigestDisplayInfo& digestDisplayInfo,
															CHyperEditHash *hyerEdit)
{
	AppendLabelLinkLineToHyperEdit(digestDisplayInfo.label.c_str(),
									digestDisplayInfo.value,
									hyerEdit);
}

void UIBridgeMFC::AppendResultMetaLineToHyperEdit(const ResultData& result,
												ResultMetaLineType metaLine,
												CHyperEditHash *hyerEdit)
{
	AppendResultMetaLineDisplayInfoToHyperEdit(GetResultMetaLineDisplayInfo(result, metaLine), hyerEdit);
}

void UIBridgeMFC::AppendFileNameToHyperEdit(const ResultData& result,
											CHyperEditHash *hyerEdit)
{
	AppendLabelValueLineToHyperEdit(GetStringByKey(FILENAME_STRING),
									GetResultPath(result),
									hyerEdit);
}

void UIBridgeMFC::AppendFileMetaToHyperEdit(const ResultData& result,
											CHyperEditHash *hyerEdit)
{
	bool appendLineBreak = false;
	VisitRenderableResultMetaLineDisplayInfos(result, [&](ResultMetaLineType metaLine, const ResultMetaLineDisplayInfo& metaLineDisplayInfo)
	{
		(void)metaLine;
		if (appendLineBreak)
		{
			AppendLineBreakToHyperEdit(hyerEdit);
		}

		AppendResultMetaLineDisplayInfoToHyperEdit(metaLineDisplayInfo, hyerEdit);
		appendLineBreak = true;
		return true;
	});
	AppendLineBreakToHyperEdit(hyerEdit);
}

void UIBridgeMFC::AppendFileHashToHyperEdit(const ResultData& result,
											bool uppercase,
											CHyperEditHash *hyerEdit)
{
	VisitResultDigestDisplayValues(result, uppercase, [&](int index, const ResultDigestMetadata& digestMetadata, const ResultDigestDisplayInfo& digestDisplayInfo)
	{
		(void)index;
		(void)digestMetadata;
		AppendResultDigestDisplayInfoToHyperEdit(digestDisplayInfo, hyerEdit);
		return true;
	});
	AppendLineBreakToHyperEdit(hyerEdit);
}

void UIBridgeMFC::AppendFileErrToHyperEdit(const ResultData& result,
											CHyperEditHash *hyerEdit)
{
	AppendTextLineToHyperEdit(GetResultError(result), hyerEdit);
	AppendLineBreakToHyperEdit(hyerEdit);
}

void UIBridgeMFC::AppendResultRenderSectionToHyperEdit(const ResultData& result,
													ResultRenderSectionType renderSection,
													bool uppercase,
													CHyperEditHash *hyerEdit)
{
	DispatchResultRenderSectionByType(renderSection, [&]()
	{
		AppendFileNameToHyperEdit(result, hyerEdit);
	}, [&]()
	{
		AppendFileMetaToHyperEdit(result, hyerEdit);
	}, [&]()
	{
		AppendFileHashToHyperEdit(result, uppercase, hyerEdit);
	}, [&]()
	{
		AppendFileErrToHyperEdit(result, hyerEdit);
	});
}

void UIBridgeMFC::AppendResultToHyperEdit(const ResultData& result,
											bool uppercase,
											CHyperEditHash *hyerEdit)
{
	ResultState resultState = GetResultState(result);
	if (IsResultStateNone(resultState))
		return;

	VisitRenderableResultSections(resultState, [&](ResultRenderSectionType renderSection)
	{
		AppendResultRenderSectionToHyperEdit(result, renderSection, uppercase, hyerEdit);
		return true;
	});

	if (ShouldAppendResultTrailingLineBreak(resultState))
	{
		AppendLineBreakToHyperEdit(hyerEdit);
	}
}
