#ifndef _HASH_DIGEST_BUFFER_PLAN_H_
#define _HASH_DIGEST_BUFFER_PLAN_H_

namespace HashEngineInternal
{
	static constexpr unsigned int kDefaultHashBufferLength = 1u * 1024u * 1024u;

	struct HashDigestBufferPlan
	{
		unsigned int preferredBufferLength;
	};

	HashDigestBufferPlan CreateDefaultHashDigestBufferPlan();
	unsigned int GetHashDigestBufferPreferredLength(const HashDigestBufferPlan& digestBufferPlan);
}

#endif
