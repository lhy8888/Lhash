#include "Common/HashBackgroundResult.h"

#include <chrono>
#include <future>
#include <iostream>
#include <memory>
#include <stdexcept>
#include <string>
#include <thread>
#include <vector>

#define CHECK(condition) do { if (!(condition)) throw std::runtime_error(#condition); } while (false)

using Result = std::vector<std::string>;
using Operation = HashBackgroundResult<Result>;

int main()
{
	Operation completed;
	Result previous = {"previous selection"};
	CHECK(!completed.TryTake(previous));
	CHECK(previous == Result{"previous selection"});
	completed.Complete(Result{"one", "two"});
	CHECK(completed.IsFinished() && completed.TryTake(previous));
	CHECK((previous == Result{"one", "two"}));
	CHECK(!completed.TryTake(previous));

	Operation cancelled;
	cancelled.Cancel();
	cancelled.Complete(Result{"late selection"});
	CHECK(cancelled.IsFinished() && !cancelled.TryTake(previous));
	CHECK((previous == Result{"one", "two"}));

	// A blocked worker owns its state independently of the caller. Cancelling
	// and destroying the caller never waits for the controlled blocking action.
	auto owner = std::make_shared<Operation>();
	std::weak_ptr<Operation> lifetime = owner;
	std::promise<void> entered, release, workerFinished;
	auto enteredFuture = entered.get_future();
	auto releaseFuture = release.get_future();
	auto finishedFuture = workerFinished.get_future();
	std::thread worker([state = owner, &entered, &releaseFuture, &workerFinished]() {
		entered.set_value();
		releaseFuture.wait();
		CHECK(state->Cancellation().load());
		state->Complete(Result{"must not reach UI"});
		Result untouched = {"original"};
		CHECK(!state->TryTake(untouched) && untouched == Result{"original"});
		workerFinished.set_value();
	});
	enteredFuture.wait();
	owner->Cancel();
	owner.reset();
	CHECK(!lifetime.expired());
	CHECK(finishedFuture.wait_for(std::chrono::milliseconds(20)) == std::future_status::timeout);
	release.set_value();
	worker.join();
	CHECK(lifetime.expired());

	// Acquire/release publication must expose the whole result, including long
	// strings and many entries, while the UI polls concurrently with completion.
	for (unsigned int repetition = 0; repetition < 64; ++repetition)
	{
		Operation result;
		std::thread publisher([&result, repetition]() {
			result.Complete(Result(256, std::string(1024, static_cast<char>('a' + repetition % 26))));
		});
		while (!result.IsFinished()) std::this_thread::yield();
		Result received;
		CHECK(result.TryTake(received) && received.size() == 256);
		for (const auto& value : received) CHECK(value == std::string(1024, static_cast<char>('a' + repetition % 26)));
		publisher.join();
	}
	std::cout << "PASS background result: cancellation, late-result rejection, independent lifetime, single delivery and publication\n";
}
