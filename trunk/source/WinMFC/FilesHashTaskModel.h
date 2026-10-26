#pragma once

#include "FilesHashTaskUpdate.h"
#include <map>
#include <mutex>
#include <stdexcept>
#include <utility>
#include <vector>

// Used by the production bridge; only replace updates for the exact same task.
class FilesHashTaskUpdateQueue
{
public:
	bool Push(const FilesHashTaskUpdate& update)
	{
		if (!update.id.IsValid()) return false;
		std::lock_guard<std::mutex> lock(m_mutex);
		const auto found = m_indices.find(update.id);
		if (found != m_indices.end())
		{
			auto& pending = m_updates[found->second];
			if (IsFilesHashTaskTerminal(pending.state)) return false;
			if (!update.algorithms.empty()) pending.algorithms = update.algorithms;
			if (!update.status.empty()) pending.status = update.status;
			pending.state = update.state;
			pending.progress = update.progress;
		}
		else
		{
			m_updates.push_back(update);
			try { m_indices.emplace(update.id, m_updates.size() - 1); }
			catch (...) { m_updates.pop_back(); throw; }
		}
		return true;
	}
	void Drain(std::vector<FilesHashTaskUpdate>& updates)
	{
		std::lock_guard<std::mutex> lock(m_mutex);
		updates.clear();
		updates.swap(m_updates);
		m_indices.clear();
	}
private:
	std::mutex m_mutex;
	std::vector<FilesHashTaskUpdate> m_updates;
	std::map<FilesHashTaskId, size_t> m_indices;
};

struct FilesHashTaskRow
{
	FilesHashTaskId id;
	sunjwbase::tstring fullPath, displayName, algorithms, status;
	FilesHashTaskState state = FILES_HASH_TASK_PENDING;
	int progress = 0;
	FilesHashSessionOutcome finishReason = FilesHashSessionOutcome::Completed;
};

struct FilesHashTaskFinishText
{
	sunjwbase::tstring cancelled, aborted, notRunCancelled, notRunAborted;
};

// UI-thread-owned history. Input positions, not paths, identify current rows.
class FilesHashTaskModel
{
public:
	void BeginSession(uint64_t session, const std::vector<sunjwbase::tstring>& files,
		const sunjwbase::tstring& algorithms, const sunjwbase::tstring& pendingText)
	{
		if (session == 0 || session <= m_latestSession || m_activeSession != 0)
			throw std::logic_error("Invalid task session transition");
		m_latestSession = m_activeSession = session;
		m_firstRow = m_rows.size();
		m_rows.reserve(m_rows.size() + files.size());
		for (size_t index = 0; index < files.size(); ++index)
		{
			FilesHashTaskRow row;
			row.id = {session, static_cast<uint32_t>(index)};
			row.fullPath = files[index];
			const auto separator = row.fullPath.find_last_of(_T("\\/"));
			row.displayName = separator == sunjwbase::tstring::npos ? row.fullPath : row.fullPath.substr(separator + 1);
			row.algorithms = algorithms;
			row.status = pendingText;
			m_rows.push_back(std::move(row));
		}
	}

	int Apply(const FilesHashTaskUpdate& update)
	{
		if (!update.id.IsValid() || update.id.session != m_activeSession ||
			update.id.index >= m_rows.size() - m_firstRow) return -1;
		const size_t index = m_firstRow + update.id.index;
		auto& row = m_rows[index];
		if (IsFilesHashTaskTerminal(row.state) || update.state == FILES_HASH_TASK_PENDING) return -1;
		if (!update.algorithms.empty()) row.algorithms = update.algorithms;
		row.status = update.status;
		row.state = update.state;
		row.progress = update.progress;
		return static_cast<int>(index);
	}

	// Call only after the real worker has exited and its updates have been drained.
	// A purported successful finish with unfinished rows is an inconsistent abort.
	bool FinishSession(uint64_t session, FilesHashSessionOutcome outcome, const FilesHashTaskFinishText& text)
	{
		if (session == 0 || session != m_activeSession) return false;
		bool unfinished = false;
		for (size_t index = m_firstRow; index < m_rows.size(); ++index)
			unfinished = unfinished || !IsFilesHashTaskTerminal(m_rows[index].state);
		const bool consistent = outcome != FilesHashSessionOutcome::Completed || !unfinished;
		if (!consistent) outcome = FilesHashSessionOutcome::Aborted;
		for (size_t index = m_firstRow; index < m_rows.size(); ++index)
		{
			auto& row = m_rows[index];
			if (IsFilesHashTaskTerminal(row.state)) continue;
			row.finishReason = outcome;
			if (row.state == FILES_HASH_TASK_PENDING)
			{
				row.state = FILES_HASH_TASK_NOT_RUN;
				row.status = outcome == FilesHashSessionOutcome::Cancelled ? text.notRunCancelled : text.notRunAborted;
			}
			else
			{
				row.state = outcome == FilesHashSessionOutcome::Cancelled ? FILES_HASH_TASK_CANCELLED : FILES_HASH_TASK_ABORTED;
				row.status = outcome == FilesHashSessionOutcome::Cancelled ? text.cancelled : text.aborted;
			}
		}
		m_activeSession = 0;
		return consistent;
	}

	const std::vector<FilesHashTaskRow>& Rows() const { return m_rows; }
	void Clear()
	{
		m_rows.clear();
		m_firstRow = 0;
		m_activeSession = 0;
	}
private:
	std::vector<FilesHashTaskRow> m_rows;
	uint64_t m_latestSession = 0;
	uint64_t m_activeSession = 0;
	size_t m_firstRow = 0;
};
