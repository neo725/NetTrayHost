# NetTrayHost

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/dotnet/csharp/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6?logo=windows&logoColor=white)](https://www.microsoft.com/windows)

[正體中文](README.md) | **English**

A Windows system tray manager for CLI applications — keep background processes running and eliminate the need to manually start them every time you boot.

---

## Overview

- Lives entirely in the system tray; no main window, no taskbar entry
- Manage multiple CLI processes from a single right-click menu, each showing its own status (🟢 Running / 🔴 Stopped)
- Start, stop, show, or hide the console window of each process individually
- Auto-restart on unexpected crashes (configurable, default 3 attempts); manual stops never trigger a restart
- Each process can be configured to start automatically when NetTrayHost launches
- Optional Windows startup entry to launch NetTrayHost itself at login
- `config.json` lives next to the exe; changes such as `autoStart` are written back immediately
- Built-in i18n: switch languages from the tray menu, restart to apply; add a new locale by dropping in a JSON file — no recompilation needed

---

## config.json

`config.json` is generated automatically in the same directory as the exe on first run. Edit it with any text editor.

Alternatively, copy `config.default.json` (included in the project) to the same directory as the exe, rename it to `config.json`, and use it as a starting point.

```json
{
  "locale": "en",
  "processes": [
    {
      "name": "MyApp",
      "exe": "C:\\tools\\myapp.exe",
      "workingDirectory": "C:\\tools",
      "arguments": "--port 8080",
      "autoStart": true,
      "autoRestart": true,
      "maxAutoRestartAttempts": 3,
      "startVisible": false
    }
  ]
}
```

| Field | Description |
|---|---|
| `locale` | UI language; matches the filename under the `lang/` folder (e.g. `en`, `zh-TW`) |
| `processes` | Array of process entries; multiple processes can be listed |
| `name` | Display name shown in the tray context menu |
| `exe` | Full path to the executable |
| `workingDirectory` | Working directory; defaults to the exe's directory if left empty |
| `arguments` | Command-line arguments passed to the process |
| `autoStart` | When `true`, the process starts automatically with NetTrayHost |
| `autoRestart` | When `true`, the process restarts automatically after an unexpected exit |
| `maxAutoRestartAttempts` | Maximum number of auto-restart attempts (default `3`) |
| `startVisible` | When `false`, the process starts in background mode with no console window |

---

## Installation

Download `NetTrayHost.exe` from the Releases page and place it in any folder. No installer or administrator privileges required.

**System requirement:** Windows 10 / 11 with [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0), or use the self-contained build below.

To build from source (requires Windows + [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0); Visual Studio is not required):

```bash
# Standard build
dotnet build -c Release

# Single-file self-contained executable (no .NET Runtime required on the target machine)
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

> This project targets `net8.0-windows` and relies on WinForms and Win32 P/Invoke — Windows only.

---

## Adding a New Locale

Copy any existing JSON file from the `lang/` folder next to the exe, translate its values, and restart NetTrayHost. The new language will appear automatically under **Settings → Language**.

Each locale file must include a `LocaleName` field (the name shown in the menu):

```json
{
  "LocaleName": "日本語",
  "Language": "言語",
  "Start": "始める",
  ...
}
```

---

## License

MIT
