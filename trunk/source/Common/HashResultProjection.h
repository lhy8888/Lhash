#ifndef _HASH_RESULT_PROJECTION_H_
#define _HASH_RESULT_PROJECTION_H_

#include "Common/HashResult.h"
#include "Common/ResultDataProjection.h"

template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter>
static inline TResultDataNet AssignHashResultCoreToNet(TResultDataNet resultDataNet, const HashResult& result, TStringConverter convertString)
{
	resultDataNet.EnumState = ConvertResultStateToNet<TResultStateNet>(result.state);
	resultDataNet.Path = convertString(result.path.c_str());
	resultDataNet.Size = result.meta.size;
	resultDataNet.ModifiedDate = convertString(result.meta.modifiedDate.c_str());
	resultDataNet.Version = convertString(result.meta.version.c_str());
	resultDataNet.Error = convertString(result.error.c_str());
	return resultDataNet;
}

template<typename TResultDataNet, typename TStringConverter>
static inline TResultDataNet AssignHashResultDigestsToNet(TResultDataNet resultDataNet, const HashResult& result, TStringConverter convertString)
{
	for (size_t digestIndex = 0; digestIndex < result.digests.size(); ++digestIndex)
	{
		const HashDigestResult& digestResult = result.digests[digestIndex];
		resultDataNet = AssignResultDigestToNet(resultDataNet, digestResult.type, convertString(digestResult.value.c_str()));
	}

	return resultDataNet;
}

template<typename TResultDataNet, typename TResultStateNet, typename TStringConverter>
static inline TResultDataNet ProjectHashResultToNet(const HashResult& result, TStringConverter convertString)
{
	TResultDataNet resultDataNet = AssignHashResultCoreToNet<TResultDataNet, TResultStateNet>(TResultDataNet(), result, convertString);
	return AssignHashResultDigestsToNet(resultDataNet, result, convertString);
}

#endif
