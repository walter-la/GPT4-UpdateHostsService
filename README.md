# UpdateHostsService

UpdateHostsService 是一個基於 .NET 7 的 Windows 服務，用於自動更新 Windows 系統中的 hosts 文件。該服務會定期根據在 appsettings.json 文件中指定的區段名稱和 URL 或本地文件路徑，將內容添加到 hosts 文件中的相應區段。它可以從遠程 URL 或本地路徑獲取內容，並保留 hosts 文件中的原始內容。

## Features

- 可作為 Windows 服務運行
- 保留 hosts 文件中的原始內容
- 自動更新 hosts 文件中的指定區段
- 支持從遠程 URL 或本地文件路徑獲取內容
- 使用 appsettings.json 配置區段名稱和內容源
- 將每個 `HostsSection` 獨立作為一個 Quartz 排程任務，以支持不同的更新時間間隔。
- 針對本地文件路徑，如果 `section.IntervalInSeconds` 小於等於 0，則使用 `FileSystemWatcher` 監聽文件更改，並在文件發生更改時立即執行更新。
- 刪除 `hosts` 文件中不再存在於 `appsettings.json` 的 "Sections" 列表中的部分。
- 在 `appsettings.json` 中添加網域名稱白名單，並在更新 `hosts` 文件時過濾掉與白名單匹配的網域名稱。


## 發佈

```sh
dotnet publish --configuration Release
```

## 安裝

要將應用程序安裝為 Windows 服務，請確保具有管理員權限的命令提示符或 PowerShell 中打開，導航到發布目錄，然後運行以下命令：

```sh
sc create UpdateHostsService binPath= "<full_path_to_your_publish_directory>\UpdateHostsService"
```

## 設定

1. 啟動服務：

```sh
sc start UpdateHostsService
```

2. 如果需要，停止服務：

```sh
sc stop UpdateHostsService
```

3. 如果您需要卸載服務，首先停止它，然後運行：

```sh
sc delete UpdateHostsService
```

現在，UpdateHostsService 作為 Windows 服務運行，根據您在 appsettings.json 文件中的設定定期更新 hosts 文件。記住，您可以根據需要調整更新間隔。
