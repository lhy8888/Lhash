#ifndef _MANAGED_HASH_MGMT_ACCESS_H_
#define _MANAGED_HASH_MGMT_ACCESS_H_

#include "Common/HashAlgorithmRegistry.h"
#include "Common/ResultDataProjection.h"
#include "Common/ResultDataSearch.h"
#include "Common/ThreadDataExecutionAccess.h"
#include "Common/ThreadDataInputAccess.h"
#include "Common/ThreadDataResultAccess.h"

static inline bool TryConvertManagedHashAlgorithmDigestType(int digestTypeValue, ResultDigestType *digestType)
{
	return TryGetHashAlgorithmType(digestTypeValue, digestType);
}

template<typename TDescriptorNet, typename TDescriptorFactory, typename TStringConverter>
static inline TDescriptorNet CreateManagedHashAlgorithmDescriptorNet(
	const HashAlgorithmDescriptor& algorithmDescriptor,
	TDescriptorFactory createDescriptor,
	TStringConverter convertText)
{
	TDescriptorNet descriptorNet = createDescriptor();
	descriptorNet->DigestType = static_cast<int>(GetHashAlgorithmDescriptorType(algorithmDescriptor));
	sunjwbase::tstring stableName = GetHashAlgorithmDescriptorStableName(algorithmDescriptor);
	sunjwbase::tstring displayLabel = GetHashAlgorithmDescriptorDisplayLabel(algorithmDescriptor);
	descriptorNet->StableName = convertText(stableName.c_str());
	descriptorNet->DisplayLabel = convertText(displayLabel.c_str());
	return descriptorNet;
}

template<typename TDescriptorNet, typename TDescriptorArray, typename TArrayFactory, typename TDescriptorFactory, typename TStringConverter>
static inline TDescriptorArray CreateSupportedManagedHashAlgorithmDescriptors(
	TArrayFactory createArray,
	TDescriptorFactory createDescriptor,
	TStringConverter convertText)
{
	TDescriptorArray algorithmDescriptors = createArray(GetRegisteredHashAlgorithmCount());

	for (int algorithmIndex = 0; algorithmIndex < GetRegisteredHashAlgorithmCount(); ++algorithmIndex)
	{
		algorithmDescriptors[algorithmIndex] = CreateManagedHashAlgorithmDescriptorNet<TDescriptorNet>(
			GetHashAlgorithmDescriptorAt(algorithmIndex),
			createDescriptor,
			convertText);
	}

	return algorithmDescriptors;
}

template<typename TThreadData, typename TEnabled>
static inline void SetManagedHashAlgorithmEnabledByDigestType(
	TThreadData& threadData,
	int digestTypeValue,
	TEnabled enabled)
{
	ResultDigestType digestType;
	if (!TryConvertManagedHashAlgorithmDigestType(digestTypeValue, &digestType))
	{
		return;
	}

	SetThreadDataHashAlgorithmEnabled(threadData, digestType, (enabled ? true : false));
}

template<typename TThreadData>
static inline bool GetManagedHashAlgorithmEnabledByDigestType(
	const TThreadData& threadData,
	int digestTypeValue)
{
	ResultDigestType digestType;
	if (!TryConvertManagedHashAlgorithmDigestType(digestTypeValue, &digestType))
	{
		return false;
	}

	return IsThreadDataHashAlgorithmEnabled(threadData, digestType);
}

template<typename TThreadData, typename TManagedArray, typename TStringConverter>
static inline void ReplaceThreadDataInputFilesFromManagedArray(
	TThreadData& threadData,
	const TManagedArray& filePaths,
	TStringConverter convertText)
{
	ResetThreadDataInputFilesAndAppend(threadData, static_cast<uint32_t>(filePaths->Length), [&](uint32_t fileIndex)
	{
		return convertText(filePaths[fileIndex]);
	});
}

template<typename TResultDataNet, typename TResultStateNet, typename TResultArray, typename TThreadData, typename TArrayFactory, typename TStringConverter, typename TResultSetter>
static inline TResultArray CreateProjectedManagedDigestMatchingResults(
	const TThreadData& threadData,
	const sunjwbase::tstring& hashToFind,
	TArrayFactory createArray,
	TStringConverter convertText,
	TResultSetter setResult)
{
	sunjwbase::tstring normalizedHashToFind = NormalizeDigestSearchText(hashToFind);
	return CreateProjectedDigestMatchingResults<TResultDataNet, TResultStateNet, TResultArray>(
		GetThreadDataResults(threadData),
		normalizedHashToFind,
		createArray,
		convertText,
		setResult);
}

#endif
