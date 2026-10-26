#pragma once

#include <algorithm>
#include <cstdint>
#include <limits>
#include <mutex>
#include <stdexcept>
#include <string>
#include <utility>
#include <vector>

struct ResultTextLink
{
	int start;
	int length;
};

// Links are sorted and non-overlapping. Only links intersecting [start, end)
// participate in painting/hit testing, independent of retained history size.
template<class Links, class Offset>
std::pair<size_t, size_t> VisibleResultLinkRange(const Links& links, int start, int end, Offset offset)
{
	if (start < 0 || end <= start) return {0, 0};
	const auto first = std::lower_bound(links.begin(), links.end(), start,
		[&](const auto& item, int position) { const auto link = offset(item); return link.start + link.length <= position; });
	const auto last = std::lower_bound(first, links.end(), end,
		[&](const auto& item, int position) { return offset(item).start < position; });
	return {static_cast<size_t>(first - links.begin()), static_cast<size_t>(last - links.begin())};
}

// The owner holds mainMutex for mutations/snapshots/drain. No window calls belong
// inside that lock. Display batches own their text and links after Drain returns.
template<class Char>
class BasicResultTextBuffer
{
public:
	using String = std::basic_string<Char>;
	struct Update
	{
		uint64_t generation = 0;
		size_t start = 0;
		bool reset = false;
		String text;
		std::vector<ResultTextLink> links;
		bool Empty() const { return !reset && text.empty() && links.empty(); }
	};

	void Clear()
	{
		AdvanceGeneration();
		m_text.clear();
		m_links.clear();
		m_pendingText.clear();
		m_pendingLinks.clear();
		m_deliveredSize = 0;
		m_reset = true;
	}

	void Append(const Char* text, bool link = false)
	{
		if (text == nullptr) return;
		const size_t length = std::char_traits<Char>::length(text);
		if (length == 0) return;
		const size_t limit = static_cast<size_t>((std::numeric_limits<int>::max)());
		if (length > limit - m_text.size()) throw std::length_error("Result text exceeds edit control range");
		// Allocate before mutating either canonical or pending data. Reserve
		// geometrically so small metadata/digest fragments stay amortized linear.
		ReserveAppend(m_text, length, limit);
		ReserveAppend(m_pendingText, length, limit);
		if (link)
		{
			ReserveAppend(m_links, 1, limit);
			ReserveAppend(m_pendingLinks, 1, limit);
		}
		const ResultTextLink offset{static_cast<int>(m_text.size()), static_cast<int>(length)};
		m_text.append(text, length);
		m_pendingText.append(text, length);
		if (link)
		{
			m_links.push_back(offset);
			m_pendingLinks.push_back(offset);
		}
	}

	void ReplaceLinks(const std::vector<ResultTextLink>& links)
	{
		size_t previousEnd = 0;
		for (const auto& link : links)
		{
			if (link.start < 0 || link.length <= 0 || static_cast<size_t>(link.start) < previousEnd ||
				static_cast<size_t>(link.start) > m_text.size() ||
				static_cast<size_t>(link.length) > m_text.size() - static_cast<size_t>(link.start))
				throw std::invalid_argument("Invalid result link range");
			previousEnd = static_cast<size_t>(link.start) + static_cast<size_t>(link.length);
		}
		String pendingText = m_text;
		auto canonicalLinks = links;
		auto pendingLinks = links;
		AdvanceGeneration();
		m_links.swap(canonicalLinks);
		m_pendingLinks.swap(pendingLinks);
		m_pendingText.swap(pendingText);
		m_deliveredSize = 0;
		m_reset = true;
	}

	Update Drain()
	{
		Update update;
		update.generation = m_generation;
		update.start = m_deliveredSize;
		update.reset = m_reset;
		update.text.swap(m_pendingText);
		update.links.swap(m_pendingLinks);
		m_reset = false;
		m_deliveredSize = m_text.size();
		return update;
	}

	const String& Text() const { return m_text; }
	const std::vector<ResultTextLink>& Links() const { return m_links; }

private:
	template<class Container>
	static void ReserveAppend(Container& value, size_t count, size_t limit)
	{
		const size_t needed = value.size() + count;
		if (needed > value.capacity())
		{
			const size_t grown = value.capacity() > limit / 2 ? limit : value.capacity() * 2;
			value.reserve((std::max)(needed, grown));
		}
	}
	void AdvanceGeneration()
	{
		if (m_generation == (std::numeric_limits<uint64_t>::max)())
			throw std::overflow_error("Result text generation exhausted");
		++m_generation;
	}
	String m_text, m_pendingText;
	std::vector<ResultTextLink> m_links, m_pendingLinks;
	uint64_t m_generation = 1;
	size_t m_deliveredSize = 0;
	bool m_reset = true;
};

// UI-owned cursor/cache. Generations make a drained pre-clear batch harmless
// after a newer reset; offsets reject duplicate or out-of-order append batches.
template<class Char>
class BasicResultTextView
{
public:
	using Update = typename BasicResultTextBuffer<Char>::Update;
	bool CanApply(const Update& update) const
	{
		return update.reset ? update.generation > m_generation :
			update.generation == m_generation && update.start == m_text.size();
	}
	bool Apply(const Update& update)
	{
		if (!CanApply(update)) return false;
		if (update.reset) m_text = update.text;
		else m_text.append(update.text);
		m_generation = update.generation;
		return true;
	}
	std::basic_string<Char> Range(size_t start, size_t length) const
	{
		if (start > m_text.size()) return {};
		return m_text.substr(start, length);
	}
	size_t Size() const { return m_text.size(); }
private:
	uint64_t m_generation = 0;
	std::basic_string<Char> m_text;
};

template<class Mutex, class Take, class Display>
void DeliverResultTextUpdate(Mutex& mutex, Take take, Display display)
{
	auto update = [&]()
	{
		std::lock_guard<Mutex> lock(mutex);
		return take();
	}();
	display(update); // Intentionally outside the producer mutex.
}
