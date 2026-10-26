#include "WinMFC/ResultTextBuffer.h"
#include <chrono>
#include <condition_variable>
#include <future>
#include <iostream>
#include <mutex>
#include <stdexcept>
#include <thread>

static void Require(bool value, const char* message)
{
    if (!value) throw std::runtime_error(message);
}

static void CheckIncrementalHistory()
{
    BasicResultTextBuffer<char> buffer;
    BasicResultTextView<char> view;
    buffer.Append(nullptr);
    buffer.Append("", true);
    std::string expected;
    size_t deliveredCharacters = 0, deliveredLinks = 0;
    for (int index = 0; index < 8192; ++index)
    {
        const auto name = std::string("file-") + std::to_string(index);
        buffer.Append(name.c_str(), true);
        buffer.Append(" SHA256 abcdef0123456789\r\n");
        expected += name + " SHA256 abcdef0123456789\r\n";
        auto update = buffer.Drain();
        Require(update.reset == (index == 0), "append unexpectedly resends a reset");
        Require(update.links.size() == 1 && update.links[0].length == static_cast<int>(name.size()), "link delta mismatch");
        Require(update.links[0].start == static_cast<int>(update.start), "link absolute offset lost");
        deliveredCharacters += update.text.size();
        deliveredLinks += update.links.size();
        Require(view.Apply(update), "ordered append rejected");
        Require(!view.Apply(update), "duplicate append/reset was accepted");
    }
    Require(expected.size() > 65536, "large control index boundary was not reached");
    Require(deliveredCharacters == expected.size() && deliveredLinks == 8192, "history was copied into incremental batches");
    Require(view.Range(0, view.Size()) == expected && buffer.Text() == expected, "append history corrupted");
    Require(buffer.Drain().Empty(), "drain repeated already delivered text");
}

static void CheckResetAndStaleBatches()
{
    BasicResultTextBuffer<char> buffer;
    BasicResultTextView<char> view;
    buffer.Append("old result", true);
    auto old = buffer.Drain();
    buffer.Append(" pending digest");
    auto oldAppend = buffer.Drain();
    buffer.Clear();
    buffer.Append("filtered result", true);
    auto replacement = buffer.Drain();
    Require(view.Apply(replacement), "new view reset rejected");
    Require(!view.Apply(old) && !view.Apply(oldAppend), "pre-clear batches restored obsolete results");
    Require(view.Range(0, 100) == "filtered result", "reset content mismatch");
    Require(replacement.links.size() == 1 && replacement.links[0].start == 0, "reset retained old link offsets");
    buffer.Append("\r\n");
    auto next = buffer.Drain();
    Require(view.Apply(next) && view.Range(0, 100) == "filtered result\r\n", "post-reset append lost");
    buffer.Clear();
    auto empty = buffer.Drain();
    Require(!empty.Empty() && view.Apply(empty) && view.Size() == 0, "clear did not clear the display");
}

static void CheckSnapshotRestorationAndUnicode()
{
    BasicResultTextBuffer<wchar_t> buffer;
    BasicResultTextView<wchar_t> view;
    const std::wstring name = L"\u6587\u4ef6-\u03a9-\U0001F4C4";
    buffer.Append(name.c_str(), true);
    buffer.Append(L"\r\nSHA256: abc");
    const auto savedText = buffer.Text();
    const auto savedLinks = buffer.Links();
    Require(view.Apply(buffer.Drain()), "initial wide text rejected");
    buffer.Append(L"\r\nPreparing...");
    Require(view.Apply(buffer.Drain()), "preparing append rejected");
    buffer.Clear();
    buffer.Append(savedText.c_str());
    buffer.ReplaceLinks(savedLinks);
    const auto restored = buffer.Drain();
    Require(restored.reset && view.Apply(restored), "snapshot restore must replace temporary text");
    Require(view.Range(0, view.Size()) == savedText, "temporary preparation text survived restoration");
    Require(view.Range(0, name.size()) == name, "Unicode link code-unit range mismatch");
    Require(view.Range(view.Size() + 1, 10).empty(), "out-of-range text access");
    const auto textBeforeInvalid = buffer.Text();
    bool rejected = false;
    try { buffer.ReplaceLinks({{0, static_cast<int>(savedText.size() + 1)}}); }
    catch (const std::invalid_argument&) { rejected = true; }
    Require(rejected && buffer.Text() == textBeforeInvalid && buffer.Drain().Empty(), "invalid links changed pending state");
}

static void CheckVisibleLinkBoundaries()
{
    std::vector<ResultTextLink> links{{0, 5}, {8, 7}, {20, 3}, {30, 10}};
    const auto offset = [](const ResultTextLink& link) { return link; };
    Require(VisibleResultLinkRange(links, 5, 8, offset) == std::make_pair(size_t(1), size_t(1)), "touching links leaked into empty gap");
    Require(VisibleResultLinkRange(links, 10, 22, offset) == std::make_pair(size_t(1), size_t(3)), "links spanning visible boundaries omitted");
    Require(VisibleResultLinkRange(links, 0, 5, offset) == std::make_pair(size_t(0), size_t(1)), "first visible link omitted");
    Require(VisibleResultLinkRange(links, 40, 50, offset) == std::make_pair(size_t(4), size_t(4)), "last boundary includes finished link");
    Require(VisibleResultLinkRange(links, -1, 5, offset) == std::make_pair(size_t(0), size_t(0)), "invalid viewport accepted");
    links.clear();
    for (int index = 0; index < 8192; ++index) links.push_back({index * 10, 5});
    size_t inspected = 0;
    const auto visible = VisibleResultLinkRange(links, 40960, 40975, [&](const ResultTextLink& link) { ++inspected; return link; });
    Require(visible == std::make_pair(size_t(4096), size_t(4098)) && inspected < 64, "visible link lookup scans retained history");
}

static void CheckProducerRunsDuringDisplay()
{
    BasicResultTextBuffer<char> buffer;
    BasicResultTextView<char> view;
    std::mutex mutex;
    buffer.Append("before ");
    std::promise<void> renderingStarted;
    auto signal = renderingStarted.get_future();
    auto producer = std::async(std::launch::async, [&]()
    {
        signal.wait();
        std::lock_guard<std::mutex> lock(mutex);
        buffer.Append("during display", true);
    });
    bool completedWhileDisplaying = false;
    DeliverResultTextUpdate(mutex, [&]() { return buffer.Drain(); }, [&](const auto& update)
    {
        renderingStarted.set_value();
        completedWhileDisplaying = producer.wait_for(std::chrono::seconds(2)) == std::future_status::ready;
        Require(view.Apply(update), "initial batch failed during concurrent append");
    });
    producer.get();
    Require(completedWhileDisplaying, "display held the producer mutex");
    DeliverResultTextUpdate(mutex, [&]() { return buffer.Drain(); }, [&](const auto& update)
    {
        Require(update.text == "during display" && update.links[0].start == 7, "concurrent append lost or replayed prefix");
        Require(view.Apply(update), "concurrent follow-up batch rejected");
    });
    Require(view.Range(0, 100) == "before during display", "concurrent final text mismatch");
}

int main()
{
    try
    {
        CheckIncrementalHistory();
        CheckResetAndStaleBatches();
        CheckSnapshotRestorationAndUnicode();
        CheckVisibleLinkBoundaries();
        CheckProducerRunsDuringDisplay();
        std::cout << "PASS result text: incremental history, reset/stale batches, snapshots, Unicode, visible links, concurrent producer\n";
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << "FAIL result text: " << error.what() << '\n';
        return 1;
    }
}
