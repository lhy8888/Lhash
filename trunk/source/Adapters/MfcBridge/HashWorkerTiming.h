#pragma once

#include <chrono>
#include <mutex>

// The worker records its own boundaries. UI timer delivery and delayed joining
// cannot change the completed duration. Ownership lasts until the worker joins.
class HashWorkerTiming
{
public:
	using Clock = std::chrono::steady_clock;
	using TimePoint = Clock::time_point;

	void Reset()
	{
		std::lock_guard<std::mutex> lock(m_mutex);
		m_started = m_finished = false;
	}
	void Start(TimePoint now = Clock::now())
	{
		std::lock_guard<std::mutex> lock(m_mutex);
		m_start = now;
		m_started = true;
		m_finished = false;
	}
	void Finish(TimePoint now = Clock::now())
	{
		std::lock_guard<std::mutex> lock(m_mutex);
		if (!m_started || m_finished) return;
		m_end = now;
		m_finished = true;
	}
	double ElapsedSeconds(TimePoint now = Clock::now()) const
	{
		std::lock_guard<std::mutex> lock(m_mutex);
		if (!m_started) return 0.0;
		const auto end = m_finished ? m_end : now;
		return end > m_start ? std::chrono::duration<double>(end - m_start).count() : 0.0;
	}

private:
	mutable std::mutex m_mutex;
	TimePoint m_start{}, m_end{};
	bool m_started = false, m_finished = false;
};

class HashWorkerTimingScope
{
public:
	explicit HashWorkerTimingScope(HashWorkerTiming& timing) : m_timing(timing) { m_timing.Start(); }
	~HashWorkerTimingScope() { m_timing.Finish(); }
	HashWorkerTimingScope(const HashWorkerTimingScope&) = delete;
	HashWorkerTimingScope& operator=(const HashWorkerTimingScope&) = delete;
private:
	HashWorkerTiming& m_timing;
};
