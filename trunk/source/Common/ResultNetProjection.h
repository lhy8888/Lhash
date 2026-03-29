#ifndef _RESULT_NET_PROJECTION_H_
#define _RESULT_NET_PROJECTION_H_

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

template<typename TMd5Action, typename TSha1Action, typename TSha256Action, typename TSha512Action>
static inline void DispatchResultDigestValueByType(ResultDigestType digestType, TMd5Action onMd5, TSha1Action onSha1, TSha256Action onSha256, TSha512Action onSha512)
{
	switch (digestType)
	{
	case RESULT_DIGEST_MD5:
		onMd5();
		break;
	case RESULT_DIGEST_SHA1:
		onSha1();
		break;
	case RESULT_DIGEST_SHA256:
		onSha256();
		break;
	case RESULT_DIGEST_SHA512:
		onSha512();
		break;
	}
}

template<typename TResultDataNet, typename TResultString>
static inline TResultDataNet AssignResultDigestToNet(TResultDataNet resultDataNet, ResultDigestType digestType, TResultString digestValue)
{
	switch (digestType)
	{
	case RESULT_DIGEST_MD5:
		resultDataNet.MD5 = digestValue;
		break;
	case RESULT_DIGEST_SHA1:
		resultDataNet.SHA1 = digestValue;
		break;
	case RESULT_DIGEST_SHA256:
		resultDataNet.SHA256 = digestValue;
		break;
	case RESULT_DIGEST_SHA512:
		resultDataNet.SHA512 = digestValue;
		break;
	}
	return resultDataNet;
}

#endif
