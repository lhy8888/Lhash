#ifndef _LEGACY_HASH_THREAD_ENTRY_H_
#define _LEGACY_HASH_THREAD_ENTRY_H_

#include "Common/Global.h"

#if defined (_WIN32)
#include <WinDef.h>
#endif

int WINAPI HashThreadFunc(void *param);

#endif
