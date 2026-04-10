#pragma once

#include <memory>
#include "Common/Global.h"
#include "LegacyCompat/LegacyThreadData.h"
#include "WinCommon/WinHandleGuard.h"
#include "HashResultNet.h"
#include "UIBridgeDelegate.h"
#include "UIBridgeUwp.h"

namespace FilesHashUwp
{
	public ref class HashAlgorithmDescriptorNet sealed
	{
public:
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
		void SetHashAlgorithmEnabledById(Platform::String^ algorithmId, Platform::Boolean val);
		Platform::Boolean GetHashAlgorithmEnabledById(Platform::String^ algorithmId);
		uint64 GetTotalSize();

		void AddFiles(const Platform::Array<Platform::String^>^ filePaths);
		void StartHashThread();

		Platform::Array<HashResultNet>^ FindHashResults(Platform::String^ pstrHashToFind);

	private:
		std::shared_ptr<UIBridgeUwp> m_spUiBridgeUwp;
		ThreadData m_threadData;
		WinHandleGuard::UniqueWinHandle m_hWorkThread;
	};
}
