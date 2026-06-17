# Project Brief：NetTrayHost — Windows CLI Tray Manager

## 背景說明

我目前使用一個叫做 **CLIProxyAPI（CPA）** 的開源 CLI 工具（Go 語言撰寫的 exe），
它需要在背景持續執行，每次開機都要手動啟動，不方便。

我參考了 [CommandTrayHost](https://github.com/rexdf/CommandTrayHost)（C++，415 stars）
這個工具的設計概念，想用 **C# / WinForms / .NET 8** 自行實作一個功能對等的版本，
專案名稱為 **NetTrayHost**。

---

## 專案資訊

| 項目 | 內容 |
|------|------|
| 專案名稱 | NetTrayHost |
| 方案路徑 | `C:\github\NetTrayHost\` |
| 專案路徑 | `C:\github\NetTrayHost\NetTrayHost\` |
| 專案類型 | Windows Forms 應用程式 |
| 語言 | C# |
| .NET 版本 | .NET 8.0（長期支援） |
| IDE | Visual Studio 2022 |
| 設定格式 | JSON（System.Text.Json，無需額外套件） |
| 發布方式 | SelfContained 單檔（PublishSingleFile） |

---

## 專案初始狀態說明（給 Claude Code）

Visual Studio 2022 建立 WinForms 專案後，預設會產生以下檔案：

```
NetTrayHost/
├── Form1.cs          ← 預設主視窗，本專案不需要，需刪除
├── Form1.Designer.cs ← 同上，需刪除
├── Program.cs        ← 入口，需改寫為無主視窗模式
└── NetTrayHost.csproj
```

本專案**沒有主視窗**，需將 `Program.cs` 改為使用 `ApplicationContext` 模式：

```csharp
// Program.cs 目標結構
Application.Run(new TrayApplicationContext());
```

---

## 核心需求

### 功能一：系統匣常駐
- App 啟動後直接縮入系統匣（Windows System Tray），**不出現任何主視窗**
- 系統匣 icon 右鍵可展開選單

### 功能二：CLI 程序管理（多程序支援）
每個被管理的 CLI 程序，在右鍵選單中有獨立的 submenu，提供：
- 程序目前狀態標示（● Running / ○ Stopped）
- 啟動（Start）— 僅 Stopped 時可用
- 停止（Stop）— 僅 Running 時可用
- 顯示 Console 視窗（Show）
- 隱藏 Console 視窗（Hide）
- `autoStart` toggle（勾選後，TrayHost 啟動時此程序自動跟著啟動，狀態即時回寫 config.json）

### 功能三：程序崩潰自動重啟
- 程序意外中止時，若 `autoRestart: true` 則自動重啟
- 注意：使用者手動按「停止」不應觸發 autoRestart

### 功能四：TrayHost 本身開機自動啟動
- 右鍵選單「設定」區塊提供 toggle
- 實作方式：寫入 / 移除 **Windows Registry Run key**
  - Key 路徑：`HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
  - Key 名稱：`NetTrayHost`
- 勾選狀態需反映目前 Registry 的實際狀態

### 功能五：設定檔（config.json）
- 放在 **exe 同目錄**
- 管理所有 CLI 程序設定
- `autoStart` toggle 變更時即時回寫
- 格式如下：

```json
{
  "processes": [
    {
      "name": "CLIProxyAPI",
      "exe": "C:\\github\\CLIProxyAPI_7.2.2_windows_amd64\\cli-proxy-api.exe",
      "workingDirectory": "C:\\github\\CLIProxyAPI_7.2.2_windows_amd64",
      "arguments": "",
      "autoStart": true,
      "autoRestart": true,
      "startVisible": false
    }
  ]
}
```

### 功能六：TrayHost 關閉行為
- 使用者點「結束 NetTrayHost」時，所有被管理的子程序一併終止

---

## 預期右鍵選單結構

```
[ CLIProxyAPI  ● Running ]
  ├── 停止
  ├── 顯示視窗
  ├── 隱藏視窗
  └── ✅ 開機自動啟動
───────────────────────────
[ 設定 ]
  └── ✅ NetTrayHost 開機自動啟動
───────────────────────────
[ 結束 NetTrayHost ]
```

---

## 刻意不做的功能（Out of Scope）

| 功能 | 說明 |
|------|------|
| Log 查看 UI | 使用者透過「顯示視窗」直接看 Console，與 CommandTrayHost 一致 |
| Toast / 桌面通知 | 不做 |
| 設定 UI 介面 | 手動編輯 config.json |
| 安裝程式（Installer） | 不做，直接執行 exe |

---

## 已知技術挑戰（Bottleneck）

### Bottleneck 1：Show / Hide CLI Console 視窗（P/Invoke）⚠️ 建議 Spike
- CLI 程序的視窗 handle 不屬於 NetTrayHost
- 需透過 Win32 API P/Invoke 操作：
  ```csharp
  [DllImport("user32.dll")]
  static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  ```
- **關鍵問題**：`startVisible: false` 啟動時（`CreateNoWindow = true`），
  程序沒有視窗 handle（`MainWindowHandle == IntPtr.Zero`），
  此時使用者點「顯示視窗」的行為需要確認可行方案

### Bottleneck 2：Kill process tree
- CPA 可能產生 child process
- `Process.Kill()` 只殺父層程序
- 需確認是否要用遞迴查詢或 Win32 Job Object

### Bottleneck 3：autoRestart 競態條件
- `Process.Exited` 事件在非 UI 執行緒觸發
- 重啟邏輯與「使用者手動停止」的狀態需要正確區分
- 需透過 `Control.Invoke` 或 `SynchronizationContext` 回到 UI 執行緒操作選單狀態

---

## 目標專案結構（供 Claude Code 參考）

```
NetTrayHost/
├── Program.cs                    # 入口，Application.Run(new TrayApplicationContext())
├── TrayApplicationContext.cs     # 核心：NotifyIcon、ContextMenuStrip、生命週期
├── ProcessManager.cs             # 單一程序的啟動/停止/監控邏輯
├── ConfigLoader.cs               # 讀寫 config.json
├── NativeMethods.cs              # P/Invoke 宣告（ShowWindow 等 Win32 API）
├── Models/
│   ├── AppConfig.cs              # config.json 根結構
│   └── ProcessConfig.cs          # 單一程序設定結構
└── config.json                   # 放在 exe 同目錄（不納入專案編譯）
```

---

## 請 Claude Code 執行以下流程

1. **閱讀本 Brief，確認對需求的理解，指出任何疑問或衝突點**

2. **針對以下決策點逐一確認：**
   - Bottleneck 1（Show/Hide）是否需要先做 Spike？建議方案為何？
   - Bottleneck 2（Kill process tree）的實作策略
   - Bottleneck 3（autoRestart 競態）的處理方式
   - 專案結構是否需要調整

3. **訂出分 Phase 的開發計畫：**
   - 列出每個 Phase 的目標與 deliverable
   - 標注需要 Spike 的 Phase
   - Spike 結果需回報再繼續

4. **取得我明確確認後，才開始第一個 Phase 的實作**

---

## 補充：CommandTrayHost 參考行為

- 每個 process 在系統匣右鍵有獨立 submenu
- `enabled: true` → TrayHost 啟動時自動執行該程序
- `start_show: false` → 背景執行，不顯示 console 視窗
- TrayHost 關閉時，所有子程序一併終止
- 程序可個別 enable / disable，不影響其他程序
- 程序狀態變化時，右鍵選單動態更新可用項目