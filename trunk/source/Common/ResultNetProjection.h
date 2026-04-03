#ifndef _RESULT_NET_PROJECTION_H_
#define _RESULT_NET_PROJECTION_H_

#include <cstring>

#include "Common/Global.h"
#include "Common/ResultDigestMetadataAccess.h"

template<typename TResultStateNet>
static inline TResultStateNet ConvertResultStateToNet(ResultState resultState)
{
	switch (resultState)
	{
	case RESULT_PATH:
		return TResultStateNet::ResultPath;
	case RESULT_META:
		return TResultStateNet::ResultMeta;
	case RESULT_ALL:
		return TResultStateNet::ResultAll;
	case RESULT_ERROR:
		return TResultStateNet::ResultError;
	case RESULT_NONE:
	default:
		return TResultStateNet::ResultNone;
	}
}

static inline bool IsResultDigestStableName(ResultDigestType digestType, const char *stableName)
{
	const HashAlgorithmDescriptor *algorithmDescriptor = NULL;
	if (!TryGetHashAlgorithmDescriptor(digestType, &algorithmDescriptor))
	{
		return false;
	}

	if (algorithmDescriptor == NULL || algorithmDescriptor->stableName == NULL || stableName == NULL)
	{
		return false;
	}

	return std::strcmp(algorithmDescriptor->stableName, stableName) == 0;
}

template<typename TMd5Action, typename TSha1Action, typename TSha256Action, typename TSha512Action>
static inline void DispatchResultDigestValueByType(ResultDigestType digestType, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)
{
	if (IsResultDigestStableName(digestType, "md5"))
	{
		onMd5();
		return;
	}
	if (IsResultDigestStableName(digestType, "sha1"))
	{
		onSha1();
		return;
	}
	if (IsResultDigestStableName(digestType, "sha256"))
	{
		onSha256();
		return;
	}
	if (IsResultDigestStableName(digestType, "sha512"))
	{
		onSha512();
		return;
	}
}

template<typename TResultDataNet, typename TResultString>
static inline TResultDataNet AssignResultDigestToNet(TResultDataNet resultDataNet, ResultDigestType digestType, TResultString digestValue)
{
	DispatchResultDigestValueByType(digestType,
		[&]()
	{
		resultDataNet.MD5 = digestValue;
	},
		[&]()
	{
		resultDataNet.SHA1 = digestValue;
	},
		[&]()
	{
		resultDataNet.SHA256 = digestValue;
	},
		[&]()
	{
		resultDataNet.SHA512 = digestValue;
	});
	return resultDataNet;
}

#endif
