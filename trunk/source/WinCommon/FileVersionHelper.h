#pragma once

#include "OsUtils/OsFile.h"
#include "Common/strhelper.h"

namespace WindowsComm
{
    class FileVersionHelper
    {
    public:
        explicit FileVersionHelper(sunjwbase::OsFile& osFile);
        virtual ~FileVersionHelper();
        sunjwbase::tstring Find();

    private:
        sunjwbase::OsFile& m_osFile;
    };
}
