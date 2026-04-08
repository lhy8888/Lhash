#ifndef _HASH_ENGINE_BRIDGE_H_
#define _HASH_ENGINE_BRIDGE_H_

#include "Adapters/UiBridge/HashEngineObserver.h"

class HashUiBridgeAdapter: public HashProgressEventBridge
{
public:
	HashUiBridgeAdapter() {}
	virtual ~HashUiBridgeAdapter() {}

	virtual void lockBridgeData() = 0;
	virtual void unlockBridgeData() = 0;
};

typedef HashUiBridgeAdapter HashEngineBridge;

#endif
