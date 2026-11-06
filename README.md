# Codex Cache Care

A WinUI 3 utility for inspecting and clearing selected Codex caches on Windows.

## Run

Extract the entire portable ZIP and open **CodexCacheCare.exe**. The x64 build includes the .NET and Windows App SDK runtimes.

1. Select **Scan cache** to see the file count and size by category.
2. Select the categories to clear and choose **Review cleanup**.
3. Exit Codex Desktop and any Codex CLI sessions, then confirm **Clear cache**. Cleanup permanently deletes the selected files. Run another scan to see what remains.

## Cleanup scope

- **Desktop cache** looks for Cache, Code Cache, GPUCache, DawnGraphiteCache and DawnWebGPUCache under the Codex roaming profile, including the observed Microsoft Store profile.
- **Previous cache copies** looks for those same directories with the timestamp suffix .old-YYYYMMDD-HHmmss-fff.
- **Model catalog** looks for models_cache.json under CODEX_HOME or the default .codex directory.

Chats, credentials, settings, browser storage, plugins and project files are outside this scope. The cleaner skips linked paths, checks file metadata again before deletion, and reports files it could not remove. Reported sizes are logical file sizes.

## Build from source

From the extracted source folder, use the .NET 10 SDK on Windows:

```powershell
dotnet run --project CacheScanner.Check
dotnet publish CodexCacheCare -c Release -o portable
```

The project uses the reference Markdown Viewer app's Windows App SDK version, Mica backdrop and native WinUI control styling.
