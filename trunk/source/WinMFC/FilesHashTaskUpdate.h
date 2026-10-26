#pragma once

#include "Common/strhelper.h"
#include <cstdint>
#include <limits>

enum FilesHashTaskState
{
	FILES_HASH_TASK_PENDING = 0,
	FILES_HASH_TASK_RUNNING,
	FILES_HASH_TASK_COMPLETED,
	FILES_HASH_TASK_FAILED,
	FILES_HASH_TASK_CANCELLED,
	FILES_HASH_TASK_ABORTED,
	FILES_HASH_TASK_NOT_RUN
};

enum class FilesHashSessionOutcome { Completed, Cancelled, Aborted };

struct FilesHashTaskId
{
	uint64_t session = 0;
	uint32_t index = (std::numeric_limits<uint32_t>::max)();
	bool IsValid() const { return session != 0 && index != (std::numeric_limits<uint32_t>::max)(); }
	bool operator<(const FilesHashTaskId& other) const
	{
		return session < other.session || (session == other.session && index < other.index);
	}
};

inline bool IsFilesHashTaskTerminal(FilesHashTaskState state)
{
	return state != FILES_HASH_TASK_PENDING && state != FILES_HASH_TASK_RUNNING;
}

struct FilesHashTaskUpdate
{
	FilesHashTaskUpdate()
		: state(FILES_HASH_TASK_PENDING),
		progress(0)
	{
	}

	FilesHashTaskId id;
	sunjwbase::tstring path;
	sunjwbase::tstring algorithms;
	sunjwbase::tstring status;
	FilesHashTaskState state;
	int progress;
};
