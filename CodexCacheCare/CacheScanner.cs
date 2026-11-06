using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CodexCacheCare;

internal enum CacheKind { Desktop, Backups, Models }

internal sealed record CacheFile(CacheKind Kind, string Path, long Bytes, DateTime Created, DateTime Modified);
internal sealed record ScanResult(List<CacheFile> Files, List<string> Skipped);

internal static class CacheScanner
{
    private static readonly string[] DesktopFolders =
        ["Cache", "Code Cache", "GPUCache", "DawnGraphiteCache", "DawnWebGPUCache"];

    internal static ScanResult Scan(string codexHome, params string[] profiles)
    {
        var result = new ScanResult([], []);
        foreach (string profile in profiles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Inspect(profile, () =>
            {
                foreach (string folder in Directory.EnumerateDirectories(profile))
                {
                    string name = Path.GetFileName(folder);
                    if (DesktopFolders.Contains(name, StringComparer.OrdinalIgnoreCase))
                        ScanFolder(folder, CacheKind.Desktop);
                    else if (DesktopFolders.Any(cache => Regex.IsMatch(name,
                        "^" + Regex.Escape(cache) + @"\.old-[0-9]{8}-[0-9]{6}-[0-9]{3}$",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)))
                        ScanFolder(folder, CacheKind.Backups);
                }
            });
        }

        AddFile(Path.Combine(codexHome, "models_cache.json"), CacheKind.Models);
        return result;

        void ScanFolder(string path, CacheKind kind)
        {
            Inspect(path, () =>
            {
                foreach (string file in Directory.EnumerateFiles(path))
                    AddFile(file, kind);
                foreach (string folder in Directory.EnumerateDirectories(path))
                    ScanFolder(folder, kind);
            });
        }

        void AddFile(string path, CacheKind kind) => Inspect(path, () =>
        {
            var file = new FileInfo(path);
            if ((file.Attributes & FileAttributes.Directory) == 0)
                result.Files.Add(new(kind, path, file.Length, file.CreationTimeUtc, file.LastWriteTimeUtc));
        });

        void Inspect(string path, Action action)
        {
            try
            {
                EnsurePhysicalPath(path);
                action();
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException) { }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                result.Skipped.Add(path);
            }
        }
    }

    internal static void Delete(CacheFile entry)
    {
        EnsurePhysicalPath(entry.Path);
        var file = new FileInfo(entry.Path);
        if (file.Length != entry.Bytes || file.CreationTimeUtc != entry.Created ||
            file.LastWriteTimeUtc != entry.Modified)
            throw new IOException("The file changed after scanning.");
        file.Delete();
    }

    internal static bool CodexIsRunning()
    {
        foreach (Process process in Process.GetProcesses())
        {
            using (process)
            {
                string name;
                try { name = process.ProcessName; }
                catch (Exception ex) when (ex is InvalidOperationException or Win32Exception) { continue; }
                if (name.Equals("codex", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("codex-app-server", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("codex-cli", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    private static void EnsurePhysicalPath(string path)
    {
        var ancestors = new Stack<string>();
        for (string? part = path; part is not null; part = Path.GetDirectoryName(part))
            ancestors.Push(part);

        foreach (string part in ancestors)
            if ((File.GetAttributes(part) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked paths are excluded.");
    }
}
