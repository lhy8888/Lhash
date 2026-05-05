#ifndef _PLATFORM_COMPAT_H_
#define _PLATFORM_COMPAT_H_

#ifndef WINAPI
#if defined(_WIN32)
#define WINAPI __stdcall
#else
#define WINAPI
#endif
#endif

#endif
