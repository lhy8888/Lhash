#ifndef _UI_BRIDGE_MFC_
#define _UI_BRIDGE_MFC_

#include "Adapters/UiBridge/HashEngineBridge.h"

#include <Windows.h>

#include "Common/strhelper.h"
#include "OsUtils/OsThread.h"
#include "Common/Global.h"
#include "Common/HashResultRender.h"
#include "HyperEditHash.h"

class UIBridgeMFC: public HashEngineBridge
{
public:
	struct ResultMetaLineDisplayInfo
	{
		sunjwbase::tstring label;
		sunjwbase::tstring value;
		sunjwbase::tstring suffix;
	};

	struct MainHyperEditSnapshot
	{
		sunjwbase::tstring text;
		OFFSETS linkOffsets;
	};

	UIBridgeMFC(HWND hWnd,
				sunjwbase::OsMutex *mainMtx,
				CHyperEditHash *hyperEdit);
	virtual ~UIBridgeMFC();

	virtual void lockData();
	virtual void unlockData();

	virtual void onJobPreparing();
	virtual void onJobPreparationFinished();
	virtual void onJobCancelled();
	virtual void onJobCompleted();

	virtual void onFileResultEvent(const HashResult& result,
									ProgressEventType eventType,
									bool uppercaseDigest);

	virtual int queryProgressMax();
	virtual void onFileProgressValue(int value);
	virtual void onTotalProgressValue(int value);

	virtual void onFileCalculated();
	virtual void onFileFinished();

	static void AppendLineBreakToHyperEdit(CHyperEditHash *hyerEdit);
	static void AppendTextLineToHyperEdit(const sunjwbase::tstring& text,
											CHyperEditHash *hyerEdit);
	static void AppendLabelValueToHyperEdit(const TCHAR *label,
											const sunjwbase::tstring& value,
											CHyperEditHash *hyerEdit);
	static void AppendLabelValueLineToHyperEdit(const TCHAR *label,
												const sunjwbase::tstring& value,
												CHyperEditHash *hyerEdit);
	static void AppendLabelLinkLineToHyperEdit(const TCHAR *label,
												const sunjwbase::tstring& value,
												CHyperEditHash *hyerEdit);
	static MainHyperEditSnapshot CaptureHyperEditSnapshot(CHyperEditHash *hyperEdit);
	static void RestoreHyperEditSnapshot(const MainHyperEditSnapshot& snapshot,
											CHyperEditHash *hyperEdit);
	static ResultMetaLineDisplayInfo GetResultMetaLineDisplayInfo(const ResultData& result,
																ResultMetaLineType metaLine);
	static ResultMetaLineDisplayInfo GetHashResultMetaLineDisplayInfo(const HashResult& result,
																	ResultMetaLineType metaLine);
	static void AppendResultMetaLineDisplayInfoToHyperEdit(const ResultMetaLineDisplayInfo& metaLineDisplayInfo,
															CHyperEditHash *hyerEdit);
	static void AppendResultDigestDisplayInfoToHyperEdit(const ResultDigestDisplayInfo& digestDisplayInfo,
															CHyperEditHash *hyerEdit);
	template<typename TResultMetaLineDisplayInfoVisitor>
	static inline bool VisitRenderableResultMetaLineDisplayInfos(const ResultData& result,
																	TResultMetaLineDisplayInfoVisitor visitor)
	{
		return VisitRenderableResultMetaLines(result, [&](ResultMetaLineType metaLine)
		{
			return visitor(metaLine, GetResultMetaLineDisplayInfo(result, metaLine));
		});
	}
	static void AppendResultMetaLineToHyperEdit(const ResultData& result,
												ResultMetaLineType metaLine,
												CHyperEditHash *hyerEdit);
	static void AppendFileNameToHyperEdit(const ResultData& result,
											CHyperEditHash *hyerEdit);
	static void AppendFileNameToHyperEdit(const HashResult& result,
											CHyperEditHash *hyerEdit);
	static void AppendFileMetaToHyperEdit(const ResultData& result,
											CHyperEditHash *hyerEdit);
	static void AppendFileMetaToHyperEdit(const HashResult& result,
											CHyperEditHash *hyerEdit);
	static void AppendFileHashToHyperEdit(const ResultData& result,
											bool uppercase,
											CHyperEditHash *hyerEdit);
	static void AppendFileHashToHyperEdit(const HashResult& result,
											bool uppercase,
											CHyperEditHash *hyerEdit);
	static void AppendFileErrToHyperEdit(const ResultData& result,
											CHyperEditHash *hyerEdit);
	static void AppendFileErrToHyperEdit(const HashResult& result,
											CHyperEditHash *hyerEdit);
	static void AppendResultRenderSectionToHyperEdit(const ResultData& result,
													ResultRenderSectionType renderSection,
													bool uppercase,
													CHyperEditHash *hyerEdit);
	static void AppendResultRenderSectionToHyperEdit(const HashResult& result,
													ResultRenderSectionType renderSection,
													bool uppercase,
													CHyperEditHash *hyerEdit);
	static void AppendResultToHyperEdit(const ResultData& result,
										bool uppercase,
										CHyperEditHash *hyerEdit);
	static void AppendResultToHyperEdit(const HashResult& result,
										bool uppercase,
										CHyperEditHash *hyerEdit);

private:
	void PostThreadInfoMessage(WPARAM wParam, LPARAM lParam = 0)
	{
		::PostMessage(m_hWnd, WM_THREAD_INFO, wParam, lParam);
	}

	void RefreshMainHyperEdit()
	{
		PostThreadInfoMessage(WP_REFRESH_TEXT);
	}

	template<typename TAppendAction>
	void UpdateMainHyperEdit(TAppendAction appendAction, bool refreshAfterUpdate = false)
	{
		lockData();
		{
			appendAction(m_mainHyperEdit);
		}
		unlockData();

		if (refreshAfterUpdate)
		{
			RefreshMainHyperEdit();
		}
	}

	template<typename TAppendAction>
	void AppendToMainHyperEditAndRefresh(TAppendAction appendAction)
	{
		UpdateMainHyperEdit(appendAction, true);
	}

	void AppendResultSectionAndRefresh(const ResultData& result,
										ResultRenderSectionType renderSection,
										bool uppercase)
	{
		AppendToMainHyperEditAndRefresh([&](CHyperEditHash *hyperEdit)
		{
			AppendResultRenderSectionToHyperEdit(result, renderSection, uppercase, hyperEdit);
		});
	}

	void AppendResultSectionAndRefresh(const HashResult& result,
										ResultRenderSectionType renderSection,
										bool uppercase)
	{
		AppendToMainHyperEditAndRefresh([&](CHyperEditHash *hyperEdit)
		{
			AppendResultRenderSectionToHyperEdit(result, renderSection, uppercase, hyperEdit);
		});
	}

	HWND m_hWnd;
	sunjwbase::OsMutex *m_mainMtx;
	CHyperEditHash *m_mainHyperEdit;

	MainHyperEditSnapshot m_preparingSnapshot;
};

#endif
