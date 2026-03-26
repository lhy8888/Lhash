#pragma once

#include <memory>
#include "Common/Global.h"
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

	public ref class HashMgmt sealed
	{
public:
		HashMgmt(UIBridgeDelegate^ uiBridgeDelegate);

		void Init();
		void Clear();

		void SetStop(Platform::Boolean val);
		void SetUppercase(Platform::Boolean val);
		void ResetHashAlgorithms();
		void SetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm, Platform::Boolean val);
		Platform::Boolean GetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm);
		uint64 GetTotalSize();

		void AddFiles(const Platform::Array<Platform::String^>^ filePaths);
		void StartHashThread();

		Platform::Array<ResultDataNet>^ FindResult(Platform::String^ pstrHashToFind);

	private:
		std::shared_ptr<UIBridgeUwp> m_spUiBridgeUwp;
		ThreadData m_threadData;
		HANDLE m_hWorkThread;
	};
}
