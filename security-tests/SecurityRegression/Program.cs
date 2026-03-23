using System.Runtime.InteropServices;
using System.Text;

internal static partial class Program
{
    private static int Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string repoRoot = FindRepoRoot();
        var failures = new List<string>();

        Run("CommandLine parser handles quoted file lists safely", TestCommandLineParsing, failures);
        Run("WinMFC copy-data validation guard exists", () =>
        {
            string content = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\FilesHashDlg.cpp");
            AssertContains(content, "IsValidCopyDataString", "Missing WM_COPYDATA input validation helper.");
            AssertContains(content, "CommandLineToArgvW", "Missing hardened Windows command-line parsing.");
            AssertContains(content, "CopyDraggedPath", "Missing long-path-safe drag/drop path extraction.");
        }, failures);
        Run("Win32 read failures propagate as errors", () =>
        {
            string winApi = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFileWinApi.cpp");
            string winUwp = ReadRepoFile(repoRoot, @"trunk\source\OsUtils\OsFileWinUwp.cpp");
            string hashEngine = ReadRepoFile(repoRoot, @"trunk\source\Common\HashEngine.cpp");

            AssertContains(winApi, "return -1;", "OsFileWinApi.cpp no longer returns -1 on ReadFile/WriteFile failure.");
            AssertContains(winUwp, "return -1;", "OsFileWinUwp.cpp no longer returns -1 on ReadFile/WriteFile failure.");
            AssertContains(hashEngine, "bool readFailed = false;", "HashEngine.cpp is missing explicit read failure tracking.");
            AssertContains(hashEngine, "RESULT_ERROR", "HashEngine.cpp is missing the read-failure error path.");
            AssertContains(hashEngine, "Failed to read file while hashing.", "HashEngine.cpp is missing the user-visible read failure message.");
        }, failures);
        Run("Shell extension hardening is present", () =>
        {
            string legacyShell = ReadRepoFile(repoRoot, @"sub-proj\fHashShlExt\fHashShellExt.cpp");
            string wuiShell = ReadRepoFile(repoRoot, @"sub-proj\fHashWUIShellExt\ExplorerCommandVerb.cpp");
            string uwpShell = ReadRepoFile(repoRoot, @"sub-proj\fHashUwpShellExt\ExplorerCommandVerb.cpp");
            string windowsUtils = ReadRepoFile(repoRoot, @"trunk\source\WinMFC\WindowsUtils.cpp");

            AssertContains(legacyShell, "PROCESS_QUERY_LIMITED_INFORMATION", "Legacy shell extension still asks for excessive process rights.");
            AssertContains(legacyShell, "CloseHandle(pInfo.hThread);", "Legacy shell extension does not close thread handles after CreateProcess.");
            AssertContains(legacyShell, "CloseHandle(pInfo.hProcess);", "Legacy shell extension does not close process handles after CreateProcess.");
            AssertContains(legacyShell, "CopyDraggedPath", "Legacy shell extension still relies on fixed-size drag/drop buffers.");
            AssertContains(wuiShell, "BOOL bCreated = CreateProcess", "WinUI shell extension launch hardening is missing.");
            AssertContains(wuiShell, "CloseHandle(pInfo.hThread);", "WinUI shell extension does not close thread handles.");
            AssertContains(uwpShell, "BOOL bCreated = CreateProcess", "UWP shell extension launch hardening is missing.");
            AssertContains(uwpShell, "CloseHandle(pInfo.hProcess);", "UWP shell extension does not close process handles.");
            AssertContains(windowsUtils, "FreeLibrary(hModule);", "WindowsUtils shell-extension registration helpers still leak module handles.");
        }, failures);

        if (failures.Count > 0)
        {
            Console.Error.WriteLine("Security regression checks failed:");
            foreach (string failure in failures)
            {
                Console.Error.WriteLine($"- {failure}");
            }

            return 1;
        }

        Console.WriteLine("All security regression checks passed.");
        return 0;
    }

    private static void TestCommandLineParsing()
    {
        string[] args = SplitCommandLine(" \"C:\\Program Files\\a.bin\" \"D:\\hash.txt\"");
        string[] filtered = args.Where(static arg => !string.IsNullOrEmpty(arg)).ToArray();

        if (filtered.Length != 2 ||
            filtered[0] != @"C:\Program Files\a.bin" ||
            filtered[1] != @"D:\hash.txt")
        {
            throw new InvalidOperationException("Quoted path parsing no longer matches the hardened parser expectations.");
        }

        string expectedUnicodePath = "C:\\\u6D4B\u8BD5 \u76EE\u5F55\\\u54C8\u5E0C \u6587\u4EF6.txt";
        string[] unicodeArgs = SplitCommandLine($" \"{expectedUnicodePath}\"");
        string[] unicodeFiltered = unicodeArgs.Where(static arg => !string.IsNullOrEmpty(arg)).ToArray();
        if (unicodeFiltered.Length != 1 || unicodeFiltered[0] != expectedUnicodePath)
        {
            throw new InvalidOperationException("Unicode path parsing no longer matches the hardened parser expectations.");
        }

        _ = SplitCommandLine("\"C:\\unterminated");
    }

    private static string ReadRepoFile(string repoRoot, string relativePath)
    {
        string path = Path.Combine(repoRoot, relativePath);
        return File.ReadAllText(path, DetectEncoding(path));
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

        return Encoding.GetEncoding(936);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }

    private static void Run(string name, Action test, List<string> errors)
    {
        try
        {
            test();
            Console.WriteLine($"PASS: {name}");
        }
        catch (Exception ex)
        {
            errors.Add($"{name}: {ex.Message}");
            Console.WriteLine($"FAIL: {name}");
        }
    }

    private static void AssertContains(string content, string expected, string message)
    {
        if (!content.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static string[] SplitCommandLine(string commandLine)
    {
        IntPtr argv = IntPtr.Zero;
        try
        {
            argv = CommandLineToArgvW(commandLine, out int argc);
            if (argv == IntPtr.Zero)
            {
                throw new InvalidOperationException("CommandLineToArgvW returned null.");
            }

            var args = new string[argc];
            for (int i = 0; i < argc; i++)
            {
                IntPtr argPtr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                args[i] = Marshal.PtrToStringUni(argPtr) ?? string.Empty;
            }

            return args;
        }
        finally
        {
            if (argv != IntPtr.Zero)
            {
                _ = LocalFree(argv);
            }
        }
    }

    [LibraryImport("shell32.dll", EntryPoint = "CommandLineToArgvW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr CommandLineToArgvW(string commandLine, out int argc);

    [LibraryImport("kernel32.dll", SetLastError = false)]
    private static partial IntPtr LocalFree(IntPtr hMem);
}
