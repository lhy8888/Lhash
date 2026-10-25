#ifndef _MFC_HASH_WORKER_SESSION_STATE_H_
#define _MFC_HASH_WORKER_SESSION_STATE_H_

#include <atomic>

// UI ownership is independent of the engine's working flag. The owner may only
// release this lease after a failed launch or a signalled/joined worker handle.
class HashWorkerSessionState
{
public:
	HashWorkerSessionState() : m_active(false), m_finalizationQueued(false) {}
	HashWorkerSessionState(const HashWorkerSessionState&) = delete;
	HashWorkerSessionState& operator=(const HashWorkerSessionState&) = delete;

	bool TryBeginLaunch()
	{
		bool expected = false;
		if (!m_active.compare_exchange_strong(expected, true)) return false;
		m_finalizationQueued = false;
		return true;
	}
	bool IsActive() const { return m_active.load(); }
	void LaunchFailed() { m_active.store(false); }
	void WorkerJoined() { m_active.store(false); }

	// Only the UI owner calls these. Posting a sentinel after the handle is
	// signalled puts it behind every notification posted by that worker.
	template<typename TPost>
	bool QueueFinalization(bool workerExited, TPost post)
	{
		if (!IsActive() || !workerExited || m_finalizationQueued || !post()) return false;
		m_finalizationQueued = true;
		return true;
	}
	void FinalizationHandled() { m_finalizationQueued = false; }

private:
	std::atomic<bool> m_active;
	bool m_finalizationQueued;
};

#endif
