#ifndef _LEGACY_MANAGED_HASH_MGMT_ACCESS_H_
#define _LEGACY_MANAGED_HASH_MGMT_ACCESS_H_

#include "Domain/HashAlgorithmRegistryCore.h"
#include "Common/HashResultProjection.h"
#include "Common/HashResultSearch.h"
#include "Adapters/ThreadDataBridge/ThreadDataAccess.h"
#include "Adapters/ThreadDataBridge/ThreadDataExecutionAccess.h"
#include "Adapters/ThreadDataBridge/ThreadDataInputAccess.h"
#include "Adapters/ThreadDataBridge/ThreadDataResultAccess.h"

static inline bool TryConvertManagedHashAlgorithmId(const sunjwbase::tstring& managedAlgorithmId, HashAlgorithmId *algorithmId)
{
	HashAlgorithmId normalizedAlgorithmId = NormalizeHashAlgorithmId(managedAlgorithmId);
	if (normalizedAlgorithmId.empty() || !IsRegisteredHashAlgorithmId(normalizedAlgorithmId))
	{
		return false;
	}

	if (algorithmId != NULL)
	{
		*algorithmId = normalizedAlgorithmId;
	}
	return true;
}

template<typename TDescriptorNet, typename TDescriptorFactory, typename TStringConverter>
static inline TDescriptorNet CreateManagedHashAlgorithmDescriptorNet(
	const HashAlgorithmDescriptor& algorithmDescriptor,
	TDescriptorFactory createDescriptor,
	TStringConverter convertText)
{
	TDescriptorNet descriptorNet = createDescriptor();
	HashAlgorithmId algorithmId = GetHashAlgorithmDescriptorId(algorithmDescriptor);
	sunjwbase::tstring stableName = GetHashAlgorithmDescriptorStableName(algorithmDescriptor);
	sunjwbase::tstring displayLabel = GetHashAlgorithmDescriptorDisplayLabel(algorithmDescriptor);
	descriptorNet->AlgorithmId = convertText(algorithmId.c_str());
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
static inline void SetManagedHashAlgorithmEnabledById(
	TThreadData& threadData,
	const HashAlgorithmId& algorithmId,
	TEnabled enabled)
{
	HashAlgorithmId normalizedAlgorithmId;
	if (!TryConvertManagedHashAlgorithmId(algorithmId, &normalizedAlgorithmId))
	{
		return;
	}

	SetThreadDataHashAlgorithmEnabledById(threadData, normalizedAlgorithmId, (enabled ? true : false));
}

template<typename TThreadData>
static inline bool GetManagedHashAlgorithmEnabledById(
	const TThreadData& threadData,
	const HashAlgorithmId& algorithmId)
{
	HashAlgorithmId normalizedAlgorithmId;
	if (!TryConvertManagedHashAlgorithmId(algorithmId, &normalizedAlgorithmId))
	{
		return false;
	}

	return IsThreadDataHashAlgorithmEnabledById(threadData, normalizedAlgorithmId);
}

template<typename TThreadData, typename TManagedArray, typename TStringConverter>
static inline void ReplaceThreadDataInputFilesFromManagedArray(
	TThreadData& threadData,
	const TManagedArray& filePaths,
	TStringConverter convertText)
{
	ResetThreadDataInputFiles(threadData);
	for (uint32_t fileIndex = 0; fileIndex < static_cast<uint32_t>(filePaths->Length); ++fileIndex)
	{
		AppendThreadDataInputFile(threadData, convertText(filePaths[fileIndex]));
	}
}

template<typename THashResultNet, typename THashResultStateNet, typename TResultArray, typename TThreadData, typename TArrayFactory, typename TStringConverter, typename TResultSetter>
static inline TResultArray CreateProjectedManagedDigestMatchingHashResults(
	const TThreadData& threadData,
	const sunjwbase::tstring& hashToFind,
	TArrayFactory createArray,
	TStringConverter convertText,
	TResultSetter setResult)
{
	sunjwbase::tstring normalizedHashToFind = NormalizeHashResultDigestSearchText(hashToFind);
	return CreateProjectedDigestMatchingHashResults<THashResultNet, THashResultStateNet, TResultArray>(
		GetThreadDataResults(threadData),
		normalizedHashToFind,
		createArray,
		convertText,
		setResult);
}

template<typename TResultDataNet, typename TResultStateNet, typename TResultArray, typename TThreadData, typename TArrayFactory, typename TStringConverter, typename TResultSetter>
static inline TResultArray CreateProjectedManagedDigestMatchingResults(
	const TThreadData& threadData,
	const sunjwbase::tstring& hashToFind,
	TArrayFactory createArray,
	TStringConverter convertText,
	TResultSetter setResult)
{
	return CreateProjectedManagedDigestMatchingHashResults<TResultDataNet, TResultStateNet, TResultArray>(
		threadData,
		hashToFind,
		createArray,
		convertText,
		setResult);
}

#endif
