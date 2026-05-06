#ifndef _HASH_FILE_VERSION_RESOLVER_H_
#define _HASH_FILE_VERSION_RESOLVER_H_

#include "Common/HashTypes.h"
#include "OsUtils/OsFile.h"

namespace HashEngineInternal
{
	sunjwbase::tstring ResolveHashFileVersion(sunjwbase::OsFile& osFile, const TCHAR *path);
}

#endif
