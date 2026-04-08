#include "stdafx.h"

#include "Common/HashEngineInternal.h"

namespace HashEngineInternal
{
	void InitializeFileAttemptState(const TCHAR *path, sunjwbase::OsFile *osFile, FileAttemptState *fileAttemptState)
	{
		fileAttemptState->path = path;
		fileAttemptState->osFile = osFile;
		fileAttemptState->fileVersion.clear();
		fileAttemptState->readFailed = false;
		fileAttemptState->isFileOpened = false;
		fileAttemptState->openErrorText = NULL;
	}

	bool OpenFileForHashing(FileAttemptState *fileAttemptState, void *openErrorBuffer)
	{
		fileAttemptState->readFailed = false;
		fileAttemptState->openErrorText = (const TCHAR *)openErrorBuffer;
		fileAttemptState->isFileOpened = fileAttemptState->osFile->openReadScan(openErrorBuffer);
		return fileAttemptState->isFileOpened;
	}

	void ResetFileProgressState(FileProgressState *progressState)
	{
		progressState->finishedSize = 0;
		progressState->finishedSizeWhole = 0;
		progressState->position = 0;
		progressState->positionWhole = 0;
	}
}
