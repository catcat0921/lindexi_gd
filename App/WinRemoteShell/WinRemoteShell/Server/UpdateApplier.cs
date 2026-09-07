using System.Diagnostics;

namespace WinRemoteShell.Server;

internal static class UpdateApplier
{
    internal static async Task<int> ApplyAsync(
        string source,
        string target,
        int processId,
        int port,
        bool service)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Self-update is only supported on Windows.");
        }

        await WaitForProcessExitAsync(processId);
        var backupDirectory = Path.Combine(
            Path.GetDirectoryName(target)!,
            $".{Path.GetFileName(target)}.backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(backupDirectory);

        foreach (var targetPath in Directory.EnumerateFiles(target, "*", SearchOption.AllDirectories)
                     .Where(path => path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                                    path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
        {
            var backupPath = Path.Combine(backupDirectory, Path.GetRelativePath(target, targetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
            File.Move(targetPath, backupPath, true);
        }

        foreach (var sourcePath in Directory.EnumerateFileSystemEntries(source, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(source, sourcePath);
            var targetPath = Path.Combine(target, relativePath);
            if (Directory.Exists(sourcePath))
            {
                Directory.CreateDirectory(targetPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            if (File.Exists(targetPath))
            {
                var backupPath = Path.Combine(backupDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                File.Move(targetPath, backupPath, true);
            }

            File.Move(sourcePath, targetPath, true);
        }

        Directory.Delete(source, true);
        StartUpdatedServer(target, port, service);
        return 0;
    }

    private static async Task WaitForProcessExitAsync(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            await process.WaitForExitAsync();
        }
        catch (ArgumentException)
        {
        }
    }

    private static void StartUpdatedServer(string target, int port, bool service)
    {
        if (service)
        {
            StartProcess("sc.exe", ["start", WindowsServiceInstaller.ServiceName]);
            return;
        }

        var executablePath = Path.Combine(target, Path.GetFileName(Environment.ProcessPath)!);
        StartProcess(executablePath, ["server", "--port", port.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
    }

    private static void StartProcess(string fileName, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(fileName) ?? AppContext.BaseDirectory
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process.Start(startInfo)?.Dispose();
    }
}
