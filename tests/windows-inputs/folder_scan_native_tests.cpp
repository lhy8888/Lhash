#include "WinCommon/HashFolderScanner.h"

#include <algorithm>
#include <chrono>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <stdexcept>

#define CHECK(condition) do { if (!(condition)) throw std::runtime_error(#condition); } while (false)

namespace
{
	HANDLE entered = NULL, releaseWorker = NULL;

	FolderScanOutcome ControlledBlockingScan(const sunjwbase::tstring&, const std::atomic<bool>& cancelled)
	{
		SetEvent(entered);
		WaitForSingleObject(releaseWorker, INFINITE);
		FolderScanOutcome result;
		result.cancelled = cancelled.load();
		result.files.push_back(L"late result");
		return result;
	}

	FolderScanOutcome ThrowingScan(const sunjwbase::tstring&, const std::atomic<bool>&)
	{
		throw std::runtime_error("controlled worker failure");
	}

	FolderScanOutcome Collect(const std::shared_ptr<HashFolderScanJob>& job)
	{
		CHECK(job && WaitForSingleObject(job->ThreadHandle(), 10000) == WAIT_OBJECT_0);
		CHECK(job->HasExited() && job->IsFinished());
		FolderScanOutcome result;
		CHECK(job->TryTake(result));
		return result;
	}

	struct TemporaryFolder
	{
		std::filesystem::path path = std::filesystem::temp_directory_path() /
			(L"LHash-scan-" + std::to_wstring(GetCurrentProcessId()) + L"-" + std::to_wstring(GetTickCount64()));
		TemporaryFolder() { CHECK(std::filesystem::create_directory(path)); }
		~TemporaryFolder() { std::error_code error; std::filesystem::remove_all(path, error); }
	};
}

int main()
{
	TemporaryFolder temporary;
	const auto nested = temporary.path / L"Unicode \u6587\u4ef6";
	CHECK(std::filesystem::create_directory(nested));
	std::ofstream(temporary.path / L"first.txt") << "first";
	std::ofstream(nested / L"second.txt") << "second";
	auto job = HashFolderScanJob::Start(temporary.path.wstring());
	auto result = Collect(job);
	CHECK(!result.failed && !result.cancelled && !result.budgetExceeded && !result.overFileLimit);
	std::sort(result.files.begin(), result.files.end());
	TStrVector expected = {(temporary.path / L"first.txt").wstring(), (nested / L"second.txt").wstring()};
	std::sort(expected.begin(), expected.end());
	CHECK(result.files == expected);
	CHECK(!job->TryTake(result));

	const auto empty = temporary.path / L"empty";
	CHECK(std::filesystem::create_directory(empty));
	result = Collect(HashFolderScanJob::Start(empty.wstring()));
	CHECK(result.files.empty() && !result.failed && !result.cancelled);
	result = Collect(HashFolderScanJob::Start((temporary.path / L"missing").wstring()));
	CHECK(result.failed && result.files.empty());
	result = Collect(HashFolderScanJob::Start(temporary.path.wstring(), &ThrowingScan));
	CHECK(result.failed && result.files.empty());

	WinHandleGuard::UniqueWinHandle enteredHandle(CreateEvent(NULL, TRUE, FALSE, NULL));
	WinHandleGuard::UniqueWinHandle releaseHandle(CreateEvent(NULL, TRUE, FALSE, NULL));
	CHECK(enteredHandle.isValid() && releaseHandle.isValid());
	entered = enteredHandle.get(); releaseWorker = releaseHandle.get();
	job = HashFolderScanJob::Start(L"controlled", &ControlledBlockingScan);
	CHECK(job && WaitForSingleObject(entered, 10000) == WAIT_OBJECT_0);
	CHECK(!job->IsFinished() && !job->HasExited());
	const auto beforeCancel = std::chrono::steady_clock::now();
	job->Cancel(); // CancelSynchronousIo may not cancel this controlled event wait.
	CHECK(std::chrono::steady_clock::now() - beforeCancel < std::chrono::seconds(1));
	CHECK(!job->IsFinished() && !job->HasExited());
	FolderScanOutcome oldSelection; oldSelection.files = {L"previous selection"};
	CHECK(!job->TryTake(oldSelection) && oldSelection.files.front() == L"previous selection");
	SetEvent(releaseWorker);
	CHECK(WaitForSingleObject(job->ThreadHandle(), 10000) == WAIT_OBJECT_0);
	CHECK(job->IsFinished() && !job->TryTake(oldSelection));
	CHECK(oldSelection.files.front() == L"previous selection");

	ResetEvent(entered); ResetEvent(releaseWorker);
	job = HashFolderScanJob::Start(L"controller destroyed", &ControlledBlockingScan);
	CHECK(job && WaitForSingleObject(entered, 10000) == WAIT_OBJECT_0);
	HANDLE duplicate = NULL;
	CHECK(DuplicateHandle(GetCurrentProcess(), job->ThreadHandle(), GetCurrentProcess(), &duplicate, SYNCHRONIZE, FALSE, 0));
	WinHandleGuard::UniqueWinHandle observation(duplicate);
	std::weak_ptr<HashFolderScanJob> lifetime = job;
	job->Cancel(); job.reset();
	CHECK(!lifetime.expired());
	SetEvent(releaseWorker);
	CHECK(WaitForSingleObject(observation.get(), 10000) == WAIT_OBJECT_0);
	CHECK(lifetime.expired());
	std::cout << "PASS native folder worker: real Unicode directory enumeration, empty/error, exception, cancel while blocked, discarded late result and owner destruction\n";
}
