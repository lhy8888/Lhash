#ifndef _HASH_FILE_ATTEMPT_STATE_OPS_H_
#define _HASH_FILE_ATTEMPT_STATE_OPS_H_

#include "Common/Global.h"
#include "OsUtils/OsFile.h"

namespace HashEngineInternal
{
	struct FileAttemptState;
	struct FileProgressState;

	void InitializeFileAttemptState(const TCHAR *path, sunjwbase::OsFile *osFile, FileAttemptState *fileAttemptState);
	bool OpenFileForHashing(FileAttemptState *fileAttemptState, void *openErrorBuffer);
	void ResetFileProgressState(FileProgressState *progressState);
}

#endif
