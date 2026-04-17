#include "stdafx.h"

#include "WindowsComm.h"

#include <stdio.h>
#include <string>
#include <vector>
#include <VersionHelpers.h>

#include "Common/strhelper.h"

#pragma warning(disable: 4996)

using namespace std;
using namespace sunjwbase;

typedef LONG (WINAPI *PRTLGETVERSION)(LPOSVERSIONINFOEXW);
typedef BOOL (WINAPI *PGPI)(DWORD, DWORD, DWORD, DWORD, PDWORD);

namespace WindowsComm
{
	namespace
	{
		bool TryGetRealWindowsVersion(OSVERSIONINFOEXW *osvi)
		{
			if (osvi == NULL)
			{
				return false;
			}

			ZeroMemory(osvi, sizeof(OSVERSIONINFOEXW));
			osvi->dwOSVersionInfoSize = sizeof(OSVERSIONINFOEXW);

			HMODULE ntdllModule = GetModuleHandle(TEXT("ntdll.dll"));
			if (ntdllModule == NULL)
			{
				return false;
			}

			PRTLGETVERSION pRtlGetVersion = reinterpret_cast<PRTLGETVERSION>(
				GetProcAddress(ntdllModule, "RtlGetVersion"));
			if (pRtlGetVersion == NULL)
			{
				return false;
			}

			return pRtlGetVersion(osvi) == 0;
		}

		SYSTEM_INFO GetNativeWindowsSystemInfo()
		{
			SYSTEM_INFO systemInfo = {};
			GetNativeSystemInfo(&systemInfo);
			return systemInfo;
		}

		std::string GetWindowsArchitectureLabel(const SYSTEM_INFO& systemInfo)
		{
			switch (systemInfo.wProcessorArchitecture)
			{
			case PROCESSOR_ARCHITECTURE_AMD64:
				return "x64";
			case PROCESSOR_ARCHITECTURE_ARM64:
				return "ARM64";
			case PROCESSOR_ARCHITECTURE_INTEL:
				return "x86";
			case PROCESSOR_ARCHITECTURE_IA64:
				return "IA64";
			default:
				return "";
			}
		}

		std::string GetWindowsEditionLabel(const OSVERSIONINFOEXW& osvi)
		{
			HMODULE kernel32Module = GetModuleHandle(TEXT("kernel32.dll"));
			if (kernel32Module == NULL)
			{
				return "";
			}

			PGPI pGetProductInfo = reinterpret_cast<PGPI>(
				GetProcAddress(kernel32Module, "GetProductInfo"));
			if (pGetProductInfo == NULL)
			{
				return "";
			}

			DWORD productType = 0;
			if (!pGetProductInfo(osvi.dwMajorVersion, osvi.dwMinorVersion, 0, 0, &productType))
			{
				return "";
			}

			switch (productType)
			{
			case PRODUCT_ULTIMATE:
				return "Ultimate";
			case PRODUCT_PROFESSIONAL:
				return "Professional";
			case PRODUCT_CORE:
				return "Home";
			case PRODUCT_HOME_PREMIUM:
				return "Home Premium";
			case PRODUCT_HOME_BASIC:
				return "Home Basic";
			case PRODUCT_ENTERPRISE:
				return "Enterprise";
			case PRODUCT_BUSINESS:
				return "Business";
			case PRODUCT_STARTER:
				return "Starter";
			case PRODUCT_CLUSTER_SERVER:
				return "HPC Edition";
			case PRODUCT_DATACENTER_SERVER:
				return "Datacenter";
			case PRODUCT_DATACENTER_SERVER_CORE:
				return "Datacenter (core installation)";
			case PRODUCT_ENTERPRISE_SERVER:
				return "Enterprise";
			case PRODUCT_ENTERPRISE_SERVER_CORE:
				return "Enterprise (core installation)";
			case PRODUCT_ENTERPRISE_SERVER_IA64:
				return "Enterprise for Itanium-based Systems";
			case PRODUCT_SMALLBUSINESS_SERVER:
				return "Small Business Server";
			case PRODUCT_SMALLBUSINESS_SERVER_PREMIUM:
				return "Small Business Server Premium";
			case PRODUCT_STANDARD_SERVER:
				return "Standard";
			case PRODUCT_STANDARD_SERVER_CORE:
				return "Standard (core installation)";
			case PRODUCT_WEB_SERVER:
				return "Web Server";
			default:
				return "";
			}
		}

		std::string GetWindowsFamilyLabel(const OSVERSIONINFOEXW& osvi)
		{
			const bool isWorkstation = osvi.wProductType == VER_NT_WORKSTATION;

			if (osvi.dwMajorVersion == 10)
			{
				if (isWorkstation)
				{
					return osvi.dwBuildNumber >= 22000 ? "Windows 11" : "Windows 10";
				}

				return "Windows Server";
			}

			if (osvi.dwMajorVersion == 6)
			{
				if (osvi.dwMinorVersion == 3)
				{
					return isWorkstation ? "Windows 8.1" : "Windows Server 2012 R2";
				}
				if (osvi.dwMinorVersion == 2)
				{
					return isWorkstation ? "Windows 8" : "Windows Server 2012";
				}
				if (osvi.dwMinorVersion == 1)
				{
					return isWorkstation ? "Windows 7" : "Windows Server 2008 R2";
				}
				if (osvi.dwMinorVersion == 0)
				{
					return isWorkstation ? "Windows Vista" : "Windows Server 2008";
				}
			}

			if (osvi.dwMajorVersion == 5 && osvi.dwMinorVersion == 1)
			{
				return "Windows XP";
			}

			std::string label("Windows ");
			return strappendformat(label, "%lu.%lu", osvi.dwMajorVersion, osvi.dwMinorVersion);
		}
	}

	/*
	 * GetExeFileVersion
	 * 获得指定路径文件版本
	 * 格式化为 主版本号.副版本号.低版本号.编译版本号
	 * 如果文件没有版本号，返回""
	 */
	tstring GetExeFileVersion(TCHAR* path)
	{
		if (path == NULL || path[0] == TEXT('\0'))
		{
			return _T("");
		}

		string strVer("");
		DWORD dwHandle = 0;
		DWORD cchver = GetFileVersionInfoSize(path, &dwHandle);
		if (cchver == 0)
		{
			return _T("");
		}

		std::vector<BYTE> pver(cchver, 0);
		if (!GetFileVersionInfo(path, dwHandle, cchver, pver.data()))
		{
			return _T("");
		}

		UINT uLen = 0;
		void *pbuf = NULL;
		if (!VerQueryValue(pver.data(), TEXT("\\"), &pbuf, &uLen) ||
			pbuf == NULL ||
			uLen < sizeof(VS_FIXEDFILEINFO))
		{
			return _T("");
		}

		unsigned int MVer = 0;
		unsigned int SVer = 0;
		unsigned int LVer = 0;
		unsigned int BVer = 0;
		VS_FIXEDFILEINFO pvsf = {};
		memcpy(&pvsf, pbuf, sizeof(VS_FIXEDFILEINFO));

		// 将版本号转换为数字 //
		MVer = pvsf.dwFileVersionMS / 65536;
		SVer = pvsf.dwFileVersionMS - 65536 * MVer;
		LVer = pvsf.dwFileVersionLS / 65536;
		BVer = pvsf.dwFileVersionLS - 65536 * LVer;
		strVer = strappendformat(strVer, ("%d.%d.%d.%d"), MVer, SVer, LVer, BVer);
		// Ver.Format(_T("%d.%d.%d.%d"), MVer, SVer, LVer, BVer);
		// 将版本号转换为数字 //

		return strtotstr(strVer);
	}

	bool IsWindowsVistaOrGreater()
	{
		OSVERSIONINFOEXW osvi = {};
		if (!TryGetRealWindowsVersion(&osvi))
		{
			return ::IsWindowsVistaOrGreater() ? true : false;
		}

		return osvi.dwMajorVersion >= 6;
	}

	/*
	 * GetWindowsInfo()
	 * 获得 Windows 版本信息
	 */
	tstring GetWindowsInfo()
	{
		OSVERSIONINFOEXW osvi = {};
		if (!TryGetRealWindowsVersion(&osvi))
		{
			return _T("Windows");
		}

		const SYSTEM_INFO systemInfo = GetNativeWindowsSystemInfo();
		std::string windowsInfo = GetWindowsFamilyLabel(osvi);

		const std::string editionLabel = GetWindowsEditionLabel(osvi);
		if (!editionLabel.empty())
		{
			windowsInfo.append(" ");
			windowsInfo.append(editionLabel);
		}

		const std::string architectureLabel = GetWindowsArchitectureLabel(systemInfo);
		if (!architectureLabel.empty())
		{
			windowsInfo.append(" ");
			windowsInfo.append(architectureLabel);
		}

		windowsInfo = strappendformat(windowsInfo, "\r\n(Build %lu)", osvi.dwBuildNumber);
		if (osvi.szCSDVersion[0] != L'\0')
		{
			windowsInfo.append(" ");
			windowsInfo.append(sunjwbase::tstrtostr(sunjwbase::tstring(osvi.szCSDVersion)));
		}

		return strtotstr(windowsInfo);
	}

}

