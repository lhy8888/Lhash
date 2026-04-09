#pragma once

#include "Common/strhelper.h"

enum FilesHashTaskState
{
	FILES_HASH_TASK_PENDING = 0,
	FILES_HASH_TASK_RUNNING,
	FILES_HASH_TASK_COMPLETED,
	FILES_HASH_TASK_FAILED
};

struct FilesHashTaskUpdate
{
	FilesHashTaskUpdate()
		: state(FILES_HASH_TASK_PENDING),
		progress(0)
	{
	}

	sunjwbase::tstring path;
	sunjwbase::tstring algorithms;
	sunjwbase::tstring status;
	FilesHashTaskState state;
	int progress;
};
