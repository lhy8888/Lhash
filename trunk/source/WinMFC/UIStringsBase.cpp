#include "stdafx.h"

#include "UIStringsBase.h"

#include <tchar.h>

UIStringsBase::UIStringsBase()
{
	// Global Strings
	m_stringsMap[_T("FILE_STRING")] = _T("File");
	m_stringsMap[_T("BYTE_STRING")] = _T("Byte(s)");
	m_stringsMap[_T("HASHVALUE_STRING")] = _T("Hash:");
	m_stringsMap[_T("FILENAME_STRING")] = _T("Name:");
	m_stringsMap[_T("FILESIZE_STRING")] = _T("File Size:");
	m_stringsMap[_T("MODIFYTIME_STRING")] = _T("Modified Date:");
	m_stringsMap[_T("VERSION_STRING")] = _T("Version:");
	m_stringsMap[_T("SECOND_STRING")] = _T("s");
	m_stringsMap[_T("BUTTON_OK")] = _T("OK");
	m_stringsMap[_T("BUTTON_CANCEL")] = _T("Cancel");

	// Main Dialog Strings
	m_stringsMap[_T("MAINDLG_INITINFO")] = _T("Drag files here or click open to start calculate.");
	m_stringsMap[_T("MAINDLG_WAITING_START")] = _T("Prepare to start calculation.");
	m_stringsMap[_T("MAINDLG_CONTEXT_INIT")] = _T("Need Administrator");
	m_stringsMap[_T("MAINDLG_ADD_SUCCEEDED")] = _T("Add Succeeded");
	m_stringsMap[_T("MAINDLG_ADD_FAILED")] = _T("Add Failed");
	m_stringsMap[_T("MAINDLG_REMOVE_SUCCEEDED")] = _T("Remove Succeeded");
	m_stringsMap[_T("MAINDLG_REMOVE_FAILED")] = _T("Remove Failed");
	m_stringsMap[_T("MAINDLG_REMOVE_CONTEXT_MENU")] = _T("Remove Context Menu");
	m_stringsMap[_T("MAINDLG_ADD_CONTEXT_MENU")] = _T("Add to Context Menu");
	m_stringsMap[_T("MAINDLG_CLEAR")] = _T("Clea&r");
	m_stringsMap[_T("MAINDLG_CLEAR_VERIFY")] = _T("Clea&r Verify");
	m_stringsMap[_T("MAINDLG_CALCU_TERMINAL")] = _T("Terminated");
	m_stringsMap[_T("MAINDLG_FIND_IN_RESULT")] = _T("Verify");
	m_stringsMap[_T("MAINDLG_RESULT")] = _T("Result:");
	m_stringsMap[_T("MAINDLG_NORESULT")] = _T("Nothing found");
	m_stringsMap[_T("MAINDLG_FILE_PROGRESS")] = _T("File");
	m_stringsMap[_T("MAINDLG_TOTAL_PROGRESS")] = _T("Total");
	m_stringsMap[_T("MAINDLG_UPPER_HASH")] = _T("Uppercase");
	m_stringsMap[_T("MAINDLG_TIME_TITLE")] = _T("Time Used:");
	m_stringsMap[_T("MAINDLG_OPEN")] = _T("&Open...");
	m_stringsMap[_T("MAINDLG_STOP")] = _T("&Stop");
	m_stringsMap[_T("MAINDLG_COPY")] = _T("&Copy");
	m_stringsMap[_T("MAINDLG_VERIFY")] = _T("&Verify");
	m_stringsMap[_T("MAINDLG_OPEN_FOLDER")] = _T("Open &Folder");
	m_stringsMap[_T("MAINDLG_EXPORT")] = _T("&Export");
	m_stringsMap[_T("MAINDLG_SETTINGS")] = _T("&Settings");
	m_stringsMap[_T("MAINDLG_SETTINGS_CLEAR")] = _T("Clear Results");
	m_stringsMap[_T("MAINDLG_SETTINGS_ALGORITHMS")] = _T("Algorithm Selection");
	m_stringsMap[_T("MAINDLG_SELECT_FOLDER")] = _T("Select a folder to hash");
	m_stringsMap[_T("MAINDLG_EMPTY_FOLDER")] = _T("No regular files were found in the selected folder.");
	m_stringsMap[_T("MAINDLG_EXPORT_FILTER")] = _T("Text Files (*.txt)|*.txt|All Files (*.*)|*.*||");
	m_stringsMap[_T("MAINDLG_EXPORT_DEFAULT_NAME")] = _T("LHash-results.txt");
	m_stringsMap[_T("MAINDLG_ABOUT")] = _T("&About");
	m_stringsMap[_T("MAINDLG_EXIT")] = _T("E&xit");
	m_stringsMap[_T("MAINDLG_HYPEREDIT_MENU_COPY")] = _T("Copy hash value");
	m_stringsMap[_T("MAINDLG_SELECT_HASH_ALGORITHM")] = _T("Enable at least one hash algorithm before starting.");
	m_stringsMap[_T("MAINDLG_TASK_FILE")] = _T("File");
	m_stringsMap[_T("MAINDLG_TASK_ALGORITHM")] = _T("Algorithm");
	m_stringsMap[_T("MAINDLG_TASK_STATUS")] = _T("Status");
	m_stringsMap[_T("MAINDLG_TASK_PROGRESS")] = _T("Progress");
	m_stringsMap[_T("MAINDLG_TASK_STATUS_PENDING")] = _T("Pending");
	m_stringsMap[_T("MAINDLG_TASK_STATUS_META")] = _T("Inspecting");
	m_stringsMap[_T("MAINDLG_TASK_STATUS_RUNNING")] = _T("Running");
	m_stringsMap[_T("MAINDLG_TASK_STATUS_COMPLETED")] = _T("Done");
	m_stringsMap[_T("MAINDLG_TASK_STATUS_FAILED")] = _T("Failed");
	m_stringsMap[_T("MAINDLG_STATUS_TOTAL")] = _T("Files");
	m_stringsMap[_T("MAINDLG_STATUS_DONE")] = _T("Done");
	m_stringsMap[_T("MAINDLG_STATUS_FAILED")] = _T("Failed");
	m_stringsMap[_T("MAINDLG_STATUS_RUNNING")] = _T("Running");
	m_stringsMap[_T("MAINDLG_STATUS_TIME")] = _T("Time");
	m_stringsMap[_T("MAINDLG_STATUS_SPEED")] = _T("Speed");

	// Find Dialog Strings
	m_stringsMap[_T("FINDDLG_TITLE")] = _T("Verify");

	// About Dialog Strings
	m_stringsMap[_T("ABOUTDLG_TITLE")] = _T("About LHash");
	m_stringsMap[_T("ABOUTDLG_INFO_TITLE")] = _T("LHash: Files Hash Calculator");
	m_stringsMap[_T("ABOUTDLG_INFO_SUBTITLE")] = _T("Modern native hashing utility with hardened runtime and portable release packaging.");
	m_stringsMap[_T("ABOUTDLG_INFO_RIGHT")] = _T("Copyright (C) 2026- LHY.");
	m_stringsMap[_T("ABOUTDLG_INFO_ALGORITHMS_TITLE")] = _T("Algorithms:");
	m_stringsMap[_T("ABOUTDLG_INFO_ALGORITHMS_CORE")] = _T("MD5, SHA1, SHA256, SHA512, BLAKE3-256, BLAKE3-512, BLAKE3 XOF");
	m_stringsMap[_T("ABOUTDLG_INFO_ALGORITHMS_EXTENDED")] = _T("XXH3-64, XXH3-128, CRC32C");
	m_stringsMap[_T("ABOUTDLG_INFO_SECURITY_TITLE")] = _T("Security and Runtime:");
	m_stringsMap[_T("ABOUTDLG_INFO_SECURITY")] = _T("Signed-release ready, DLL-search hardened, reparse-point aware, and UTF-8 toolchain aligned.");
	m_stringsMap[_T("ABOUTDLG_INFO_RIGHTDETAIL")] = _T("More details are on Project Site.");
	m_stringsMap[_T("ABOUTDLG_INFO_OSTITLE")] = _T("Operating System:");
	m_stringsMap[_T("ABOUTDLG_PROJECT_SITE")] = _T("<a>Hosted on GitHub</a>");
	m_stringsMap[_T("ABOUTDLG_PROJECT_URL")] = _T("https://github.com/lhy8888/Lhash");

}
