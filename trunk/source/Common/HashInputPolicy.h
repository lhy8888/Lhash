#pragma once

#include <cstddef>
#include <string>

// Limit enumeration as well as the eventual hash workload. Empty directories
// and reparse entries still consume work, even when they produce no files.
struct HashFolderScanBudget
{
	static constexpr size_t maxEntries = 65536;
	static constexpr size_t maxDirectories = 8192;
	static constexpr size_t maxDepth = 128;
	size_t entries = 0;
	size_t directories = 1; // Includes the selected root.

	bool VisitEntry()
	{
		if (entries >= maxEntries) return false;
		++entries;
		return true;
	}

	bool QueueDirectory(size_t depth)
	{
		if (depth > maxDepth || directories >= maxDirectories) return false;
		++directories;
		return true;
	}
};

template <typename Char>
bool TryBuildHashShellCommand(const std::basic_string<Char>& executable,
	std::basic_string<Char>& command)
{
	command.clear();
	if (executable.empty() || executable.find(Char('"')) != executable.npos ||
		executable.find(Char(0)) != executable.npos)
	{
		return false;
	}
	command.push_back(Char('"'));
	command += executable;
	command.push_back(Char('"'));
	for (char c : std::string(" \"%1\"")) command.push_back(Char(c));
	return true;
}
