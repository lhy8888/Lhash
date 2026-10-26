#pragma once

#include <atomic>
#include <utility>

// One worker publishes one result; its UI owner may stop waiting at any time.
// Neither cancellation nor destruction of the UI requires joining that worker.
template<class TResult>
class HashBackgroundResult
{
public:
	void Cancel() noexcept { m_cancelled.store(true, std::memory_order_relaxed); }
	const std::atomic<bool>& Cancellation() const noexcept { return m_cancelled; }
	bool IsFinished() const noexcept { return m_finished.load(std::memory_order_acquire); }

	void Complete(TResult result)
	{
		m_result = std::move(result);
		m_finished.store(true, std::memory_order_release);
	}

	// Only the UI owner calls this. Cancellation wins over a late completion.
	bool TryTake(TResult& result)
	{
		if (m_cancelled.load(std::memory_order_relaxed) || !IsFinished() || m_taken) return false;
		result = std::move(m_result);
		m_taken = true;
		return true;
	}

private:
	std::atomic<bool> m_cancelled{false};
	std::atomic<bool> m_finished{false};
	TResult m_result;
	bool m_taken = false;
};
