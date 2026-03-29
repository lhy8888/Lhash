#pragma once

#include "Common/Global.h"
#include "HashResultNet.h"
#include "UIBridgeDelegates.h"
#include "UIBridgeWUI.h"

namespace FilesHashWUI
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
		property System::String^ StableName;
		property System::String^ DisplayLabel;
	};

	public ref class HashMgmtClr sealed
	{
public:
		HashMgmtClr(UIBridgeDelegates^ uiBridgeDelegates);

		virtual ~HashMgmtClr()
		{
			// clean up code to release managed resource
			// ...
			// to avoid code duplication,
			// call finalizer to release unmanaged resources
			this->!HashMgmtClr();
		}

		// finalizer cleans up unmanaged resources
		// destructor or garbage collector will
		// clean up managed resources
		!HashMgmtClr();

		void Init();
		void Clear();

		void SetStop(bool val);
		void SetUppercase(bool val);
		void ResetHashAlgorithms();
		cli::array<HashAlgorithmDescriptorNet^>^ GetSupportedHashAlgorithms();
		void SetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm, bool val);
		bool GetHashAlgorithmEnabled(HashAlgorithmTypeNet hashAlgorithm);
		void SetHashAlgorithmEnabledByDigestType(int digestType, bool val);
		bool GetHashAlgorithmEnabledByDigestType(int digestType);
		System::UInt64 GetTotalSize();

		void AddFiles(cli::array<System::String^>^ filePaths);
		void StartHashThread();

		cli::array<HashResultNet>^ FindHashResults(System::String^ sstrHashToFind);
		System::UInt64 GetResultCount();

	private:
		UIBridgeWUI *m_pUiBridgeWUI;
		ThreadData *m_pThreadData;
		HANDLE m_hWorkThread;
	};
}
