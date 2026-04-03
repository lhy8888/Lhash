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

static inline bool IsResultDigestStableNameById(const HashAlgorithmId& algorithmId, const char *stableName);

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

static inline bool IsResultDigestStableNameById(const HashAlgorithmId& algorithmId, const char *stableName)
{
	const HashAlgorithmDescriptor *algorithmDescriptor = NULL;
	if (!TryGetHashAlgorithmDescriptorById(algorithmId, &algorithmDescriptor))
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
static inline void DispatchResultDigestValueById(const HashAlgorithmId& algorithmId, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)
{
	if (IsResultDigestStableNameById(algorithmId, "md5"))
	{
		onMd5();
		return;
	}
	if (IsResultDigestStableNameById(algorithmId, "sha1"))
	{
		onSha1();
		return;
	}
	if (IsResultDigestStableNameById(algorithmId, "sha256"))
	{
		onSha256();
		return;
	}
	if (IsResultDigestStableNameById(algorithmId, "sha512"))
	{
		onSha512();
		return;
	}
}

template<typename TMd5Action, typename TSha1Action, typename TSha256Action, typename TSha512Action>
static inline void DispatchResultDigestValueByType(ResultDigestType digestType, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)
{
	DispatchResultDigestValueById(GetHashAlgorithmId(digestType), onMd5, onSha1, onSha256, onSha512);
}

template<typename TResultDataNet, typename TResultString>
static inline TResultDataNet AssignResultDigestToNetById(TResultDataNet resultDataNet, const HashAlgorithmId& algorithmId, TResultString digestValue)
{
#if defined (_MANAGED)
	if (IsResultDigestStableNameById(algorithmId, "md5"))
	{
		resultDataNet.MD5 = digestValue;
		return resultDataNet;
	}
	if (IsResultDigestStableNameById(algorithmId, "sha1"))
	{
		resultDataNet.SHA1 = digestValue;
		return resultDataNet;
	}
	if (IsResultDigestStableNameById(algorithmId, "sha256"))
	{
		resultDataNet.SHA256 = digestValue;
		return resultDataNet;
	}
	if (IsResultDigestStableNameById(algorithmId, "sha512"))
	{
		resultDataNet.SHA512 = digestValue;
		return resultDataNet;
	}
#else
	DispatchResultDigestValueById(algorithmId,
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
#endif
	return resultDataNet;
}

template<typename TResultDataNet, typename TResultString>
static inline TResultDataNet AssignResultDigestToNet(TResultDataNet resultDataNet, ResultDigestType digestType, TResultString digestValue)
{
#if defined (_MANAGED)
	if (IsResultDigestStableName(digestType, "md5"))
	{
		resultDataNet.MD5 = digestValue;
		return resultDataNet;
	}
	if (IsResultDigestStableName(digestType, "sha1"))
	{
		resultDataNet.SHA1 = digestValue;
		return resultDataNet;
	}
	if (IsResultDigestStableName(digestType, "sha256"))
	{
		resultDataNet.SHA256 = digestValue;
		return resultDataNet;
	}
	if (IsResultDigestStableName(digestType, "sha512"))
	{
		resultDataNet.SHA512 = digestValue;
		return resultDataNet;
	}
#else
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
#endif
	return resultDataNet;
}

#endif
