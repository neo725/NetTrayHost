# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NetTrayHost is a Windows system tray manager for CLI applications. It runs as a WinForms app with no main window (`WinExe`), managing multiple background CLI processes via a system tray icon and context menus. Inspired by CommandTrayHost.

**Stack:** C#, .NET 8.0-windows, WinForms, no external NuGet dependencies (System.Text.Json only).

## Common Commands

```bash
# Build
dotnet build

# Build release
dotnet build -c Release

# Run (debug)
./NetTrayHost/bin/Debug/net8.0-windows/NetTrayHost.exe

# Publish single-file self-contained executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

There are no automated tests — manual testing uses `spike/MockCli/` (a console app that prints status every second).

## Architecture

### Data flow

```
config.json (exe dir)
    └─► ConfigLoader.cs ─► TrayApplicationContext.cs
                                ├─► NotifyIcon + ContextMenuStrip (UI)
                                ├─► RegistryRunManager.cs (startup toggle)
                                └─► ProcessManager.cs (one per process)
                                        ├─► System.Diagnostics.Process
                                        ├─► NativeMethods.cs (Win32 P/Invoke)
                                        └─► AppLogger.cs → NetTrayHost.log
```

### Key files and responsibilities

- **TrayApplicationContext.cs** — Orchestrator. Owns the tray icon and context menu. Rebuilds menu items on each open based on current process states. Manages `ProcessManager` lifetime.
- **ProcessManager.cs** — All process lifecycle logic: start, stop, show/hide console window, auto-restart (max 3 attempts), crash detection. Uses `_userRequestedStop` flag to distinguish manual stops from crashes.
- **NativeMethods.cs** — Win32 P/Invoke: `ShowWindow`, `IsWindowVisible`, `AttachConsole`, `FreeConsole`, `GetConsoleWindow`.
- **ConfigLoader.cs** — Loads/saves `config.json` from the same directory as the `.exe`. Generates a default config pointing to MockCli if none exists.
- **RegistryRunManager.cs** — Reads/writes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\NetTrayHost`.

### Threading model

Process exit events fire on background threads. All UI updates must be marshaled via `SynchronizationContext.Post()` (captured at startup as `WindowsFormsSynchronizationContext`). `ProcessManager` uses a `lock` for internal state safety.

### Console window management

CLI processes may start without a visible window. `ProcessManager` uses a retry loop (up to 3 seconds) calling `AttachConsole()` + `GetConsoleWindow()` to capture the window handle once the process creates its console. That handle is then used for `ShowWindow()` / `IsWindowVisible()`.

### Runtime files

All runtime files live next to the `.exe` in `bin/Debug/net8.0-windows/` (or the publish output):
- `config.json` — process configuration
- `NetTrayHost.log` — append-only log (thread-safe, timestamped)
