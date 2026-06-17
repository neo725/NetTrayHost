# NetTrayHost

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/dotnet/csharp/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6?logo=windows&logoColor=white)](https://www.microsoft.com/windows)

**正體中文** | [English](README_EN.md)

將需要長期在背景執行的 CLI 程序，或每次開機都要手動啟動的命令列工具，納入系統匣統一管理，免去繁複的手動操作。

---

## Overview

- 純系統匣運作，無主視窗，不佔用工作列空間
- 右鍵選單管理多個 CLI 程序，各自顯示執行狀態（🟢 執行中 / 🔴 已停止）
- 支援啟動、停止、顯示 / 隱藏 Console 視窗
- 程序意外崩潰時自動重啟（最多 3 次）；使用者手動停止則不觸發
- 各程序可個別設定是否隨 NetTrayHost 啟動時自動執行
- 可設定 NetTrayHost 本身是否隨 Windows 開機自動執行
- 設定檔 `config.json` 存放於 exe 同目錄，變更即時回寫
- 多語系支援：在選單中切換語言，重啟後生效；新增語系只需放入對應 JSON 檔，無需重新編譯

---

## 設定 config.json

`config.json` 會在 exe 第一次執行時自動產生於同目錄。以文字編輯器開啟即可修改。

```json
{
  "locale": "zh-TW",
  "processes": [
    {
      "name": "MyApp",
      "exe": "C:\\tools\\myapp.exe",
      "workingDirectory": "C:\\tools",
      "arguments": "--port 8080",
      "autoStart": true,
      "autoRestart": true,
      "startVisible": false
    }
  ]
}
```

| 欄位 | 說明 |
|---|---|
| `locale` | 介面語系，對應 `lang/` 資料夾下的 JSON 檔名（如 `zh-TW`、`en`） |
| `processes` | Process 設定陣列，可並列多個程序 |
| `name` | 顯示在右鍵選單上的程序名稱 |
| `exe` | 可執行檔的完整路徑 |
| `workingDirectory` | 工作目錄；留空則預設為 exe 所在資料夾 |
| `arguments` | 傳入程序的命令列參數 |
| `autoStart` | `true` 時，NetTrayHost 啟動後此程序自動跟著啟動 |
| `autoRestart` | `true` 時，程序意外結束後自動重啟（上限 3 次） |
| `startVisible` | `false` 時以背景模式啟動，不顯示 Console 視窗 |

---

## 安裝與執行

直接下載 Release 頁面的 `NetTrayHost.exe`，放到任意資料夾執行即可。
不需要安裝程式，也不需要系統管理員權限。

**系統需求：** Windows 10 / 11（需安裝 [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)，或使用下方 self-contained 發布版本）

若需要自行建置（需 Windows + [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)，不需要 Visual Studio）：

```bash
# 一般建置
dotnet build -c Release

# 單檔自包含執行檔（執行環境不需另外安裝 .NET Runtime）
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

> 本專案使用 WinForms 與 Win32 P/Invoke，Target Framework 為 `net8.0-windows`，僅支援 Windows 平台。

---

## 新增語系

在 exe 同目錄的 `lang/` 資料夾中，複製任一現有 JSON 檔並翻譯內容即可。
重啟 NetTrayHost 後，新語系會自動出現在「設定 → 語言」選單中。

每個語系 JSON 必須包含 `LocaleName` 欄位（顯示在選單上的名稱）：

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
