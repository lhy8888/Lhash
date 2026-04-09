using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Program
{
    private const uint GENERIC_READ = 0x80000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
    private const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;
    private const int ERROR_SHARING_VIOLATION = 32;

    private static void TestJunctionAncestorAttackSurface()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string root = CreateUniqueTempDirectory("junction-attack");
        string targetDirectory = Path.Combine(root, "real-target");
        string junctionDirectory = Path.Combine(root, "linked-target");
        string linkedFile = Path.Combine(junctionDirectory, "secret.txt");

        Directory.CreateDirectory(targetDirectory);
        File.WriteAllText(Path.Combine(targetDirectory, "secret.txt"), "secret");

        try
        {
            CreateDirectoryJunction(junctionDirectory, targetDirectory);

            if (!Directory.Exists(junctionDirectory))
            {
                throw new InvalidOperationException("The security harness did not create the directory junction.");
            }

            if (!File.Exists(linkedFile))
            {
                throw new InvalidOperationException("The linked file path did not resolve through the directory junction.");
            }

            FileAttributes junctionAttributes = File.GetAttributes(junctionDirectory);
            if (!junctionAttributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException("The directory junction did not expose the expected reparse-point attribute.");
            }

            FileAttributes leafAttributes = File.GetAttributes(linkedFile);
            if (leafAttributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException("The linked file unexpectedly exposed the reparse-point bit at the leaf path.");
            }

            using SafeFileHandle handle = OpenFileForHashStyleRead(linkedFile);
            if (handle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "A hash-style open should succeed through a directory junction when ancestor reparse points are not blocked.");
            }
        }
        finally
        {
            TryDeleteDirectoryJunction(junctionDirectory);
            TryDeleteDirectory(root);
        }
    }

    private static void TestHashStyleOpenSharingViolation()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string root = CreateUniqueTempDirectory("sharing-attack");
        string filePath = Path.Combine(root, "locked.bin");
        File.WriteAllText(filePath, "locked");

        try
        {
            using FileStream lockStream = new(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            using SafeFileHandle handle = OpenFileForHashStyleRead(filePath);

            if (!handle.IsInvalid)
            {
                throw new InvalidOperationException("A hash-style open unexpectedly succeeded while another process held an exclusive lock.");
            }

            int error = Marshal.GetLastPInvokeError();
            if (error != ERROR_SHARING_VIOLATION)
            {
                throw new Win32Exception(error, "The exclusive-lock harness did not reproduce the expected sharing violation.");
            }
        }
        finally
        {
            TryDeleteDirectory(root);
        }
    }

    private static string CreateUniqueTempDirectory(string purpose)
    {
        string root = Path.Combine(Path.GetTempPath(), "fhash-security-tests", $"{purpose}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    private static void CreateDirectoryJunction(string junctionPath, string targetPath)
    {
        var startInfo = new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{junctionPath}\" \"{targetPath}\"")
        {
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start cmd.exe to create the directory junction.");
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            string stderr = process.StandardError.ReadToEnd();
            string stdout = process.StandardOutput.ReadToEnd();
            throw new InvalidOperationException($"mklink /J failed with exit code {process.ExitCode}: {stderr}{stdout}");
        }
    }

    private static void TryDeleteDirectoryJunction(string junctionPath)
    {
        if (!Directory.Exists(junctionPath))
        {
            return;
        }

        try
        {
            Directory.Delete(junctionPath);
        }
        catch
        {
            // Best-effort cleanup for local temp harnesses.
        }
    }

    private static void TryDeleteDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        try
        {
            Directory.Delete(directoryPath, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for local temp harnesses.
        }
    }

    private static SafeFileHandle OpenFileForHashStyleRead(string path)
    {
        return CreateFileW(
            path,
            GENERIC_READ,
            FILE_SHARE_READ,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,
            IntPtr.Zero);
    }

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial SafeFileHandle CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);
}
