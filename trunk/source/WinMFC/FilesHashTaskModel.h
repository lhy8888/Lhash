#pragma once

#include "FilesHashTaskUpdate.h"
#include <array>
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
		updates.clear();
		std::map<FilesHashTaskId, size_t> retiredIndices;
		{
			std::lock_guard<std::mutex> lock(m_mutex);
			updates.swap(m_updates);
			retiredIndices.swap(m_indices);
		}
		// Destroy the previous key set after releasing the producer lock.
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
		// Preserve vector's amortized growth across many retained small sessions.
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
			++m_stateCounts[FILES_HASH_TASK_PENDING];
		}
	}

	int Apply(const FilesHashTaskUpdate& update)
	{
		if (!update.id.IsValid() || update.id.session != m_activeSession ||
			update.id.index >= m_rows.size() - m_firstRow ||
			static_cast<size_t>(update.state) >= m_stateCounts.size()) return -1;
		const size_t index = m_firstRow + update.id.index;
		auto& row = m_rows[index];
		if (IsFilesHashTaskTerminal(row.state) || update.state == FILES_HASH_TASK_PENDING) return -1;
		if (!update.algorithms.empty()) row.algorithms = update.algorithms;
		row.status = update.status;
		ChangeState(row, update.state);
		row.progress = update.progress;
		return static_cast<int>(index);
	}

	// Call only after the real worker has exited and its updates have been drained.
	// A purported successful finish with unfinished rows is an inconsistent abort.
	bool FinishSession(uint64_t session, FilesHashSessionOutcome outcome, const FilesHashTaskFinishText& text)
	{
		if (session == 0 || session != m_activeSession) return false;
		// Previous sessions contain only terminal rows. These counters therefore
		// also tell us whether the active session still has unfinished tasks.
		const bool unfinished = Count(FILES_HASH_TASK_PENDING) != 0 || Count(FILES_HASH_TASK_RUNNING) != 0;
		const bool consistent = outcome != FilesHashSessionOutcome::Completed || !unfinished;
		if (!consistent) outcome = FilesHashSessionOutcome::Aborted;
		for (size_t index = m_firstRow; index < m_rows.size(); ++index)
		{
			auto& row = m_rows[index];
			if (IsFilesHashTaskTerminal(row.state)) continue;
			row.finishReason = outcome;
			if (row.state == FILES_HASH_TASK_PENDING)
			{
				row.status = outcome == FilesHashSessionOutcome::Cancelled ? text.notRunCancelled : text.notRunAborted;
				ChangeState(row, FILES_HASH_TASK_NOT_RUN);
			}
			else
			{
				row.status = outcome == FilesHashSessionOutcome::Cancelled ? text.cancelled : text.aborted;
				ChangeState(row, outcome == FilesHashSessionOutcome::Cancelled ? FILES_HASH_TASK_CANCELLED : FILES_HASH_TASK_ABORTED);
			}
		}
		m_activeSession = 0;
		return consistent;
	}

	const std::vector<FilesHashTaskRow>& Rows() const { return m_rows; }
	size_t Count(FilesHashTaskState state) const
	{
		const auto index = static_cast<size_t>(state);
		return index < m_stateCounts.size() ? m_stateCounts[index] : 0;
	}
	void Clear()
	{
		m_rows.clear();
		m_stateCounts.fill(0);
		m_firstRow = 0;
		m_activeSession = 0;
	}
private:
	void ChangeState(FilesHashTaskRow& row, FilesHashTaskState state)
	{
		--m_stateCounts[static_cast<size_t>(row.state)];
		++m_stateCounts[static_cast<size_t>(state)];
		row.state = state;
	}

	std::vector<FilesHashTaskRow> m_rows;
	std::array<size_t, FILES_HASH_TASK_NOT_RUN + 1> m_stateCounts{};
	uint64_t m_latestSession = 0;
	uint64_t m_activeSession = 0;
	size_t m_firstRow = 0;
};
