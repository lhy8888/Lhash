#include "stdafx.h"

#include "FileVersionHelper.h"
#include "WindowsComm.h"

namespace WindowsComm
{
    FileVersionHelper::FileVersionHelper(sunjwbase::OsFile& osFile)
        : m_osFile(osFile)
    {
    }

    FileVersionHelper::~FileVersionHelper() = default;

    sunjwbase::tstring FileVersionHelper::Find()
    {
        // Optional display metadata only. Keep PE parsing in the Windows
        // version API; never seek/read untrusted resource offsets ourselves.
        // This path-based lookup is deliberately not part of the hash pipeline.
        return GetExeFileVersion(m_osFile.getPath().c_str());
    }
}
