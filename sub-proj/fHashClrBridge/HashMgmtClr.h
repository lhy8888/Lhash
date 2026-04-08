#pragma once

#include "Common/Global.h"
#include "HashResultNet.h"
#include "UIBridgeDelegates.h"
#include "UIBridgeWUI.h"
struct ThreadData;

namespace FilesHashWUI
{
	public ref class HashAlgorithmDescriptorNet sealed
	{
public:
		property System::String^ AlgorithmId;
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
		void SetHashAlgorithmEnabledById(System::String^ algorithmId, bool val);
		bool GetHashAlgorithmEnabledById(System::String^ algorithmId);
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
