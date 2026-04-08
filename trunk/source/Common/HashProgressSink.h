#ifndef _HASH_PROGRESS_SINK_H_
#define _HASH_PROGRESS_SINK_H_

#include "Domain/ProgressEvent.h"

class HashProgressSink
{
public:
	HashProgressSink() {}
	virtual ~HashProgressSink() {}

	virtual int progressMax() = 0;
	virtual void onProgressEvent(const ProgressEvent& progressEvent) = 0;
};

#endif
