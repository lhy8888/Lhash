#ifndef _WIN_HANDLE_GUARD_H_
#define _WIN_HANDLE_GUARD_H_

#include <Windows.h>

namespace WinHandleGuard
{
	template<typename THandle, typename TCloseTraits>
	class UniqueHandleBase
	{
	public:
		UniqueHandleBase()
			: m_handle(TCloseTraits::Invalid())
		{
		}

		explicit UniqueHandleBase(THandle handle)
			: m_handle(handle)
		{
		}

		~UniqueHandleBase()
		{
			reset();
		}

		UniqueHandleBase(const UniqueHandleBase&) = delete;
		UniqueHandleBase& operator=(const UniqueHandleBase&) = delete;

		UniqueHandleBase(UniqueHandleBase&& other) noexcept
			: m_handle(other.release())
		{
		}

		UniqueHandleBase& operator=(UniqueHandleBase&& other) noexcept
		{
			if (this != &other)
			{
				reset(other.release());
			}

			return *this;
		}

		bool isValid() const
		{
			return TCloseTraits::IsValid(m_handle);
		}

		THandle get() const
		{
			return m_handle;
		}

		operator THandle() const
		{
			return m_handle;
		}

		THandle release()
		{
			THandle releasedHandle = m_handle;
			m_handle = TCloseTraits::Invalid();
			return releasedHandle;
		}

		void reset(THandle handle = TCloseTraits::Invalid())
		{
			if (TCloseTraits::IsValid(m_handle))
			{
				TCloseTraits::Close(m_handle);
			}

			m_handle = handle;
		}

		THandle* put()
		{
			reset();
			return &m_handle;
		}

	private:
		THandle m_handle;
	};

	struct HandleCloseTraits
	{
		static inline HANDLE Invalid()
		{
			return NULL;
		}

		static inline bool IsValid(HANDLE handle)
		{
			return handle != NULL && handle != INVALID_HANDLE_VALUE;
		}

		static inline void Close(HANDLE handle)
		{
			::CloseHandle(handle);
		}
	};

	struct FindHandleCloseTraits
	{
		static inline HANDLE Invalid()
		{
			return INVALID_HANDLE_VALUE;
		}

		static inline bool IsValid(HANDLE handle)
		{
			return handle != NULL && handle != INVALID_HANDLE_VALUE;
		}

		static inline void Close(HANDLE handle)
		{
			::FindClose(handle);
		}
	};

	struct ModuleCloseTraits
	{
		static inline HMODULE Invalid()
		{
			return NULL;
		}

		static inline bool IsValid(HMODULE handle)
		{
			return handle != NULL;
		}

		static inline void Close(HMODULE handle)
		{
			::FreeLibrary(handle);
		}
	};

	typedef UniqueHandleBase<HANDLE, HandleCloseTraits> UniqueWinHandle;
	typedef UniqueHandleBase<HANDLE, FindHandleCloseTraits> UniqueFindHandle;
	typedef UniqueHandleBase<HMODULE, ModuleCloseTraits> UniqueModuleHandle;
}

#endif
