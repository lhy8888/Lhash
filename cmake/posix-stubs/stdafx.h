#pragma once
// POSIX CMake build compatibility stub - no MFC, no Windows headers.

#include <stdint.h>
#include <stddef.h>
#include <string>
#include <vector>

#include "Common/strhelper.h"

#include "targetver.h"

#ifndef _T
#define _T(x) x
#endif

#ifndef TEXT
#define TEXT(x) x
#endif

typedef sunjwbase::TCHAR TCHAR;
typedef sunjwbase::_TCHAR _TCHAR;
