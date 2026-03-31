#ifndef _HASH_ENGINE_BRIDGE_H_
#define _HASH_ENGINE_BRIDGE_H_

#include "Adapters/UiBridge/HashEngineObserver.h"

class HashEngineBridge: public HashEngineObserver
{
public:
	HashEngineBridge() {}
	virtual ~HashEngineBridge() {}

	virtual void lockData() = 0;
	virtual void unlockData() = 0;
};

#endif
