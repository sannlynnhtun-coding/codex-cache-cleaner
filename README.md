# Codex Cache Care

A WinUI 3 utility for inspecting and clearing selected Codex caches on Windows.

## Run

Download **CodexCacheCare-Setup-x64.exe** from the [latest release](https://github.com/sannlynnhtun-coding/codex-cache-cleaner/releases/latest). The installer is for Windows 10 22H2 or later on x64 and installs for the current user. It includes the .NET and Windows App SDK runtimes and adds a Start menu shortcut. The app opens maximized and stacks the dashboard into one column in a narrow window.

1. Select **Scan cache** to see the file count and size by category.
2. Select the categories to clear and choose **Review cleanup**.
3. Exit Codex Desktop and any Codex CLI sessions, then confirm **Clear cache**. Cleanup permanently deletes the selected files. Run another scan to see what remains.

## Cleanup scope

- **Desktop cache** looks for Cache, Code Cache, GPUCache, DawnGraphiteCache and DawnWebGPUCache under the Codex roaming profile, including the observed Microsoft Store profile.
- **Previous cache copies** looks for those same directories with the timestamp suffix .old-YYYYMMDD-HHmmss-fff.
- **Model catalog** looks for models_cache.json under CODEX_HOME or the default .codex directory.

Chats, credentials, settings, browser storage, plugins and project files are outside this scope. The cleaner skips linked paths, checks file metadata again before deletion, and reports files it could not remove. Reported sizes are logical file sizes.

## Build from source

From the repository root, use the .NET 10 SDK on Windows.

```powershell
dotnet run --project CacheScanner.Check
dotnet publish CodexCacheCare -c Release -o portable
```

To build the installer, run the Inno Setup 6 compiler (6.3 or later) after publishing.

```powershell
ISCC.exe scripts\installer.iss
```

The project uses the reference Markdown Viewer app's Windows App SDK version, Mica backdrop and native WinUI control styling.

Codex artwork belongs to OpenAI. Codex Cache Care is an independent utility. [Artwork source](https://github.com/backnotprop/orchestrator/tree/main/assets/providers)
