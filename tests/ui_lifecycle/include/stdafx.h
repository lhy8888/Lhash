#pragma once
#include <cstddef>
#include <cstdint>
#include <string>
#include <vector>
#if defined(_WIN32)
#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <Windows.h>
#include <tchar.h>
#else
#include "Common/strhelper.h"
typedef sunjwbase::TCHAR TCHAR;
typedef sunjwbase::_TCHAR _TCHAR;
#define _T(x) x
#define TEXT(x) x
#endif
