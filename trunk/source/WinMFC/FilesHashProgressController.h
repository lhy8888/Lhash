#pragma once

#include "afxwin.h"
#include "afxcmn.h"

struct ITaskbarList3;

class FilesHashProgressController
{
public:
	FilesHashProgressController();
	~FilesHashProgressController();

	void Initialize(CDialog* parentWnd, CProgressCtrl* progressCtrl);
	void PrepareAdvTaskbar();
	void StartTiming(LPCTSTR secondText);
	void AdvanceTimeTick(LPCTSTR secondText);
	void FinishTiming(ULONGLONG totalSize);
	void ResetAfterStop();
	void SetWholeProgress(UINT pos);

private:
	void CloseTaskbarList();
	void SetTimeText(LPCTSTR text);
	void SetSpeedText(LPCTSTR text);

	CDialog* m_parentWnd;
	CProgressCtrl* m_progressCtrl;
	float m_calculateTime;
	UINT_PTR m_timerId;
	BOOL m_bAdvTaskbar;
	ITaskbarList3* m_taskbarList;
};
