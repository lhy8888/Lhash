#ifndef _CHECKED_ARITHMETIC_H_
#define _CHECKED_ARITHMETIC_H_

#include <limits>
#include <stdint.h>

static inline bool TryAddUInt64(uint64_t left, uint64_t right, uint64_t *result)
{
	if (result == NULL)
	{
		return false;
	}

	if (left > ((std::numeric_limits<uint64_t>::max)() - right))
	{
		return false;
	}

	*result = left + right;
	return true;
}

static inline bool TrySubtractUInt64(uint64_t left, uint64_t right, uint64_t *result)
{
	if (result == NULL || left < right)
	{
		return false;
	}

	*result = left - right;
	return true;
}

static inline bool TryMultiplyUInt64(uint64_t left, uint64_t right, uint64_t *result)
{
	if (result == NULL)
	{
		return false;
	}

	if (left == 0 || right == 0)
	{
		*result = 0;
		return true;
	}

	if (left > ((std::numeric_limits<uint64_t>::max)() / right))
	{
		return false;
	}

	*result = left * right;
	return true;
}

static inline uint64_t SaturatingAddUInt64(uint64_t left, uint64_t right)
{
	uint64_t result = 0;
	if (!TryAddUInt64(left, right, &result))
	{
		return (std::numeric_limits<uint64_t>::max)();
	}

	return result;
}

static inline uint64_t ReplaceSizedValueUInt64(uint64_t total, uint64_t previousValue, uint64_t currentValue)
{
	uint64_t totalWithoutPrevious = 0;
	if (!TrySubtractUInt64(total, previousValue, &totalWithoutPrevious))
	{
		return currentValue;
	}

	return SaturatingAddUInt64(totalWithoutPrevious, currentValue);
}

static inline int ClampProgressValue(int value, int progressMax)
{
	if (progressMax <= 0)
	{
		return 0;
	}
	if (value < 0)
	{
		return 0;
	}
	if (value > progressMax)
	{
		return progressMax;
	}
	return value;
}

static inline int CalculateBoundedProgressValue(uint64_t completedValue, uint64_t totalValue, int progressMax)
{
	if (progressMax <= 0)
	{
		return 0;
	}

	if (totalValue == 0)
	{
		return progressMax;
	}
	if (completedValue >= totalValue)
	{
		return progressMax;
	}

	uint64_t numerator = 0;
	const uint64_t scale = static_cast<uint64_t>(progressMax);
	if (TryMultiplyUInt64(scale, completedValue, &numerator))
	{
		return static_cast<int>(numerator / totalValue);
	}

	// Find the largest progress value whose rounded-up byte threshold has
	// been reached. Splitting totalValue into quotient and remainder avoids
	// multiplying a potentially 64-bit byte count by the progress scale.
	// The scale comes from a positive 32-bit int on the supported platforms,
	// so remainder * candidate and its rounding addition fit in uint64_t.
	const uint64_t quotient = totalValue / scale;
	const uint64_t remainder = totalValue % scale;
	uint64_t lower = 0;
	uint64_t upper = scale;
	while (lower < upper)
	{
		const uint64_t candidate = lower + (upper - lower + 1) / 2;
		const uint64_t threshold = quotient * candidate +
			(remainder * candidate + scale - 1) / scale;
		if (completedValue >= threshold)
		{
			lower = candidate;
		}
		else
		{
			upper = candidate - 1;
		}
	}

	return static_cast<int>(lower);
}

static inline int CalculateIndexedProgressValue(uint64_t completedCount, uint64_t totalCount, int progressMax)
{
	return CalculateBoundedProgressValue(completedCount, totalCount, progressMax);
}

#endif
