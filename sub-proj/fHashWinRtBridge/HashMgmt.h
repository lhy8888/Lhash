#pragma once

#include <memory>
#include "Common/Global.h"
#include "LegacyCompat/LegacyThreadData.h"
#include "HashResultNet.h"
#include "UIBridgeDelegate.h"
#include "UIBridgeUwp.h"

namespace FilesHashUwp
{
	public enum class HashAlgorithmTypeNet
	{
		MD5 = 0,
		SHA1,
		SHA256,
		SHA512
	};

	public ref class HashAlgorithmDescriptorNet sealed
	{
public:
		property int DigestType;
		property Platform::String^ AlgorithmId;
		property Platform::String^ StableName;
		property Platform::String^ DisplayLabel;
	};

	public ref class HashMgmt sealed
	{
public:
		HashMgmt(UIBridgeDelegate^ uiBridgeDelegate);

		void Init();
		void Clear();

		void SetStop(Platform::Boolean val);
		void SetUppercase(Platform::Boolean val);
		void ResetHashAlgorithms();
		Platform::Array<HashAlgorithmDescriptorNet^>^ GetSupportedHashAlgorithms();
		void SetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm, Platform::Boolean val);
		Platform::Boolean GetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm);
		void SetHashAlgorithmEnabledById(Platform::String^ algorithmId, Platform::Boolean val);
		Platform::Boolean GetHashAlgorithmEnabledById(Platform::String^ algorithmId);
		void SetHashAlgorithmEnabledByDigestType(int digestType, Platform::Boolean val);
		Platform::Boolean GetHashAlgorithmEnabledByDigestType(int digestType);
		uint64 GetTotalSize();

		void AddFiles(const Platform::Array<Platform::String^>^ filePaths);
		void StartHashThread();

		Platform::Array<HashResultNet>^ FindHashResults(Platform::String^ pstrHashToFind);

	private:
		std::shared_ptr<UIBridgeUwp> m_spUiBridgeUwp;
		ThreadData m_threadData;
		HANDLE m_hWorkThread;
	};
}
