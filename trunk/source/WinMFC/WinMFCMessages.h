#ifndef _WINMFC_MESSAGES_H_
#define _WINMFC_MESSAGES_H_

#if defined(_WIN32)
#include <WinUser.h>
#else
#ifndef WM_USER
#define WM_USER 0x0400
#endif
#endif

#define WM_THREAD_INFO		(WM_USER + 1)
#define WP_WORKING			(WM_USER + 2)
#define WP_FINISHED			(WM_USER + 3)
#define WP_STOPPED			(WM_USER + 4)
#define WP_REFRESH_TEXT		(WM_USER + 5)
#define WP_PROG			(WM_USER + 6)
#define WP_PROG_WHOLE		(WM_USER + 7)
#define WP_TASK_UPDATE		(WM_USER + 8)
#define WM_CUSTOM_MSG		(WM_USER + 16)
#define WM_HYPEREDIT_MENU	(WM_USER + 17)

#endif
