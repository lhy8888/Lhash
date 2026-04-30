using System.Text;

namespace LHash.UnitTests;

internal static class RepositoryTestContext
{
    static RepositoryTestContext()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static string RepoRoot { get; } = FindRepoRoot();

    public static string ReadUtf8File(string relativePath)
    {
        string path = ResolveRepoPath(relativePath);
        return File.ReadAllText(path, Encoding.UTF8);
    }

    public static string ReadTextFile(string relativePath)
    {
        string path = ResolveRepoPath(relativePath);
        return File.ReadAllText(path, DetectEncoding(path));
    }

    public static void AssertContainsInOrder(string content, params string[] fragments)
    {
        int currentIndex = -1;
        foreach (string fragment in fragments)
        {
            int nextIndex = content.IndexOf(fragment, currentIndex + 1, StringComparison.Ordinal);
            Assert.True(nextIndex >= 0, $"Expected to find '{fragment}' after index {currentIndex}.");
            currentIndex = nextIndex;
        }
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) &&
                Directory.Exists(Path.Combine(current.FullName, "trunk")) &&
                Directory.Exists(Path.Combine(current.FullName, ".github")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root for unit tests.");
    }

    private static string ResolveRepoPath(string relativePath)
    {
        string livePath = Path.Combine(RepoRoot, relativePath);
        if (File.Exists(livePath) || Directory.Exists(livePath))
        {
            return livePath;
        }

        return livePath;
    }

    private static Encoding DetectEncoding(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 3 &&
            bytes[0] == 0xEF &&
            bytes[1] == 0xBB &&
            bytes[2] == 0xBF)
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        }

        try
        {
            Encoding strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            _ = strictUtf8.GetString(bytes);
            return strictUtf8;
        }
        catch (ArgumentException)
        {
            // Fall through to the legacy code-page reader for historical files
            // that have not yet been migrated.
        }

        return Encoding.GetEncoding(936);
    }
}
