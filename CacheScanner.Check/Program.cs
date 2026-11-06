using CodexCacheCare;

string root = Directory.CreateTempSubdirectory("CacheCareCheck-").FullName;
try
{
    string active = Put("profile/Cache/Cache_Data/entry");
    string backup = Put("profile/GPUCache.old-20261106-144620-984/entry");
    string model = Put("home/models_cache.json");
    string session = Put("home/sessions/chat.jsonl");
    string settings = Put("profile/Local Storage/entry");
    string unknown = Put("profile/GPUCache.old-unknown/entry");
    string profile = Path.Combine(root, "profile");
    string home = Path.Combine(root, "home");

    var scan = CacheScanner.Scan(home, profile);
    Assert(scan.Files.Count == 3 && scan.Skipped.Count == 0, "Only known caches are scanned.");
    Assert(scan.Files.Single(file => file.Kind == CacheKind.Models).Path == model, "Model cache found.");
    Assert(scan.Files.Single(file => file.Kind == CacheKind.Backups).Path == backup, "Timestamped backup found.");

    File.AppendAllText(active, "changed");
    try
    {
        CacheScanner.Delete(scan.Files.Single(file => file.Kind == CacheKind.Desktop));
        throw new InvalidOperationException("Changed cache was deleted.");
    }
    catch (IOException) { }

    CacheScanner.Delete(scan.Files.Single(file => file.Kind == CacheKind.Backups));
    Assert(!File.Exists(backup), "Selected backup was deleted.");
    Assert(new[] { model, active, session, settings, unknown }.All(File.Exists), "Other files were preserved.");
    Console.WriteLine("Cache scanner checks passed.");
}
finally { Directory.Delete(root, recursive: true); }

string Put(string relative)
{
    string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, "fixture");
    return path;
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
