<p align="center">
  <img src="docs/icon.png" width="96" alt="MenuBar CryptoPrice" />
</p>

<h1 align="center">MenuBar CryptoPrice</h1>

<p align="center">Windows 11 任务栏加密货币行情显示器</p>

<p align="center">
  <img src="https://img.shields.io/badge/Windows-11-0078D4?logo=windows11&logoColor=white" alt="Windows 11" />
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/WinUI-3-0067C0" alt="WinUI 3" />
  <img src="https://img.shields.io/badge/WPF-Desktop-68217A" alt="WPF" />
  <img src="https://img.shields.io/badge/License-GPL--3.0--or--later-blue" alt="GPL-3.0-or-later" />
  <a href="https://github.com/BeyondESH/MenuBar-CryptoPrice/releases/latest">
    <img src="https://img.shields.io/github/v/release/BeyondESH/MenuBar-CryptoPrice?display_name=tag&sort=semver" alt="Latest release" />
  </a>
</p>

MenuBar CryptoPrice 是一个常驻 Windows 11 任务栏的轻量加密货币行情工具。它通过 Bitget 公开接口展示现货（SPOT）与 U 本位永续（USDT-FUTURES）的价格、24h 涨跌，并提供原生 WinUI 3 设置客户端、悬停详情、点击展开、`+N` 溢出列表和开机自启。

> 行情仅供参考，不构成投资建议。本项目与 Bitget 无隶属关系。

## 目录

- [功能](#功能)
- [界面与交互](#界面与交互)
- [架构](#架构)
- [安装](#安装)
- [使用](#使用)
- [配置](#配置)
- [数据源与隐私](#数据源与隐私)
- [从源码构建](#从源码构建)
- [测试](#测试)
- [项目结构](#项目结构)
- [已知限制与 FAQ](#已知限制与-faq)
- [参与贡献](#参与贡献)
- [支持作者](#支持作者)
- [开源协议](#开源协议)
- [第三方组件](#第三方组件)
- [致谢与免责声明](#致谢与免责声明)
- [版本历史](#版本历史)

## 功能

### 任务栏行情

- 在 Windows 11 主任务栏空白区显示 1–8 个自选币种，支持现货和 U 本位永续混合显示。
- 展示币种、最新价、24h 涨跌幅；按市场使用不同价格精度，价格带千分位。
- 支持绿涨红跌/红涨绿跌两套配色。
- 实时 WebSocket 行情 + REST 首屏与断流兜底；超过 30 秒未更新的数据会灰显。
- 支持单行与双行紧凑布局、四档字号、任务栏内/任务栏上方两种位置。
- 支持左贴靠/右贴靠；空间不足时自动隐藏尾部并显示 `+N`，悬停 `+N` 查看完整列表。
- 任务栏条不抢焦点（`WS_EX_NOACTIVATE`），点击不会打断当前应用。
- 全屏应用、开始菜单、搜索、任务视图、自动隐藏任务栏出现时自动让位。
- 支持虚拟桌面切换与单实例运行；重复启动会唤起已有实例。

### 悬停与展开

- 悬停价格：显示完整信息（最新价、涨跌幅、24h 高低、24h 成交额、买一/卖一、资金费率、标记价格、更新时间）。
- 悬停 `+N`：列出全部已配置币种，并标记当前显示/待命/超宽隐藏状态。
- 点击价格或 `+N`：展开持久浮层。
- 点击桌面、其他应用或任务栏空白：浮层自动关闭；点击浮层内部保持展开。
- 窗口隐藏或失焦时也会关闭展开层。

### 原生 WinUI 3 设置客户端

- Windows 11 设置式左侧 `NavigationView`：行情 / 外观 / 关于。
- 使用 `SettingsCard`、原生 `ComboBox`、`ToggleSwitch`、`Button`、`ContentDialog`。
- 行情页：显示数量、币种排序、品类切换、实时价格、删除、添加币种、刷新目录。
- 添加币种：市场选择、关键字搜索、非 USDT 开关、在线/存在/去重校验。
- 外观页：布局、字号、涨跌颜色、位置、贴靠方向、开机自启、恢复默认。
- 关于页：作者 BeyondESH、GitHub 个人页、项目地址、版本、数据来源、配置文件路径、开源组件、打赏按钮与打赏码。

### 安装与系统集成

- Inno Setup 安装程序，按用户安装，无需管理员权限。
- 默认安装目录：`%LOCALAPPDATA%\Programs\MenuBarCryptoPrice`。
- 开始菜单提供主程序、设置、卸载快捷方式；桌面快捷方式可选。
- 卸载时结束主程序和设置进程，清理安装目录、快捷方式、卸载项和开机自启值。
- 用户配置保留在 `%APPDATA%\MenuBarCryptoPrice`，卸载不会删除自选币种与外观设置。

## 界面与交互

### 托盘菜单

| 菜单项 | 说明 |
| --- | --- |
| 设置… | 打开 WinUI 3 设置客户端 |
| 重新定位 | 立即重新计算任务栏位置与宽度 |
| 开机自启 | 切换当前用户登录时自动启动 |
| 关于 | 打开设置客户端的关于页 |
| 退出 | 退出主程序并清理托盘图标 |

双击托盘图标也可以打开设置。

### 任务栏

| 操作 | 效果 |
| --- | --- |
| 悬停价格 | 显示完整行情详情 |
| 悬停 `+N` | 显示全部已配置币种 |
| 点击价格/`+N` | 展开持久浮层 |
| 点击其他位置 | 关闭展开层 |
| 任务栏空间不足 | 自动隐藏尾部并显示 `+N` |

### 设置页面

| 页面 | 内容 |
| --- | --- |
| 行情 | 显示数量、币种列表、品类、价格、排序、删除、添加币种、目录刷新 |
| 外观 | 布局、字号、涨跌颜色、位置、贴靠方向、开机自启、恢复默认 |
| 关于 | 作者、GitHub、项目地址、打赏、版本、数据来源、配置路径、开源组件 |

首次运行且没有配置文件时，主程序会自动打开设置客户端。

## 架构

```text
┌─────────────────────────────────────────────────────────────┐
│ WPF 主程序 (MenuBarCryptoPrice.exe)                         │
│                                                             │
│  Bitget REST / WebSocket ──▶ TickerStore ──▶ TickerWindow   │
│                                      │        (任务栏覆盖层) │
│                                      │                       │
│  托盘 / 开机自启 / 单实例 / 虚拟桌面 / 位置计算              │
└───────────────────────────┬─────────────────────────────────┘
                            │ 共享 config.json
                            │ %APPDATA%\MenuBarCryptoPrice
┌───────────────────────────┴─────────────────────────────────┐
│ WinUI 3 设置客户端 (MenuBarCryptoPrice.Settings.WinUI.exe)  │
│                                                             │
│  NavigationView + SettingsCard                              │
│  行情 / 外观 / 关于                                          │
└─────────────────────────────────────────────────────────────┘
```

### 为什么是两个进程

任务栏覆盖层需要**逐像素透明、无激活、置顶且可命中**的窗口，这是 WPF 的强项；WinUI 3 不适合做这种任务栏覆盖窗口。设置客户端则需要真正的 Windows 11 原生组件（NavigationView、SettingsCard、Mica、系统强调色），因此使用 WinUI 3 独立进程。

两个进程通过同一份 `config.json` 协作：设置客户端保存后，主程序通过 `FileSystemWatcher` 监听文件变化，300–700ms 防抖后重新排布任务栏条并按需重订阅行情。

## 安装

### 系统要求

- Windows 11，版本 22000 或更高。
- x64 处理器。
- 安装程序已自包含 .NET 8 运行时与 Windows App SDK，无需额外安装 .NET 或 Windows App Runtime。

### 安装步骤

1. 打开 [Releases](https://github.com/BeyondESH/MenuBar-CryptoPrice/releases/latest)。
2. 下载 `MenuBarCryptoPrice-Setup-1.0.1.exe`。
3. 运行安装程序。安装为当前用户安装，不触发 UAC。
4. 可选勾选“创建桌面快捷方式”。
5. 安装完成后可勾选启动主程序；首次运行会自动打开设置。

安装包当前未进行代码签名，Windows SmartScreen 可能提示“Windows 已保护你的电脑”。如确认来源可信，点击“更多信息”后选择“仍要运行”。

### 升级

直接运行新版本安装包覆盖安装即可，无需先卸载。用户配置和开机自启设置会保留。如果安装时提示主程序正在运行，请从托盘菜单选择“退出”后重试。

### 卸载

通过 Windows 设置 → 应用 → 已安装的应用 → MenuBar CryptoPrice 卸载，或使用开始菜单中的“卸载”快捷方式。

卸载会清理安装目录、开始菜单快捷方式、卸载注册表项和开机自启值；`%APPDATA%\MenuBarCryptoPrice` 中的用户配置会保留。如需彻底恢复干净状态，可手动删除该目录。

## 使用

### 首次运行

1. 启动 `MenuBarCryptoPrice.exe`。
2. 程序在任务栏空白区显示行情条，并在托盘创建图标。
3. 首次运行没有配置时，会自动打开设置客户端。
4. 在“行情”页添加币种、调整数量与顺序。
5. 在“外观”页选择布局、字号、配色、位置和贴靠方向。
6. 关闭设置窗口即完成；修改已实时生效。

### 添加币种

1. 设置 → 行情 → 添加币种。
2. 选择“现货”或“U 本位永续”。
3. 输入币种关键字（如 `BTC`、`ETH`），或打开“包含非 USDT 交易对”。
4. 从结果列表选择交易对，点击“添加”。
5. 同一交易对在同一市场只能添加一次；现货与永续可以同时存在。

### 调整显示

- “显示数量”控制任务栏最多显示多少项；超出部分折叠为 `+N`。
- 上下箭头调整顺序；删除按钮移除条目。
- 行情页的品类下拉可以在现货/永续之间切换；切换后使用对应市场的精度。
- 外观页的“贴靠方向”用于优先使用任务栏左侧或右侧空白区。
- 空间不足时可减少数量、切换双行布局或选择“任务栏上方”。

### 开机自启

托盘菜单或外观页均可切换。程序通过当前用户注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 写入启动项，并带 `--autostart` 参数静默启动。

### 关于与打赏

设置 → 关于页包含：

- 作者 BeyondESH 与 GitHub 个人页：<https://github.com/BeyondESH>
- 项目地址：<https://github.com/BeyondESH/MenuBar-CryptoPrice>
- 版本、数据来源、配置文件路径、开源组件。
- “查看打赏码”按钮，打开 `dashang.jpg` 打赏图。

## 配置

配置文件位置：

```text
%APPDATA%\MenuBarCryptoPrice\config.json
```

示例：

```json
{
  "version": 2,
  "maxVisibleItems": 4,
  "layout": "singleLine",
  "fontSize": "medium",
  "colorScheme": "greenUp",
  "positionMode": "taskbar",
  "anchor": "left",
  "startWithWindows": true,
  "offsetX": 0,
  "items": [
    { "market": "USDT-FUTURES", "symbol": "SOLUSDT", "precision": 3 },
    { "market": "USDT-FUTURES", "symbol": "BTCUSDT", "precision": 1 },
    { "market": "USDT-FUTURES", "symbol": "ETHUSDT", "precision": 2 }
  ]
}
```

| 字段 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `version` | int | `2` | 配置版本，用于迁移 |
| `maxVisibleItems` | int | `4` | 最多显示数量，范围 1–8 |
| `layout` | string | `singleLine` | `singleLine` / `compactTwoLine` |
| `fontSize` | string | `medium` | `small` / `medium` / `large` / `extraLarge` |
| `colorScheme` | string | `greenUp` | `greenUp` / `redUp` |
| `positionMode` | string | `taskbar` | `taskbar` / `aboveTaskbar` |
| `anchor` | string | `right` | `right` / `left` |
| `startWithWindows` | bool | `false` | 是否写入当前用户启动项 |
| `offsetX` | int | `0` | 手动水平偏移，范围 -4000–4000 |
| `items` | array | — | 自选币种列表 |
| `items[].market` | string | `SPOT` | `SPOT` / `USDT-FUTURES` |
| `items[].symbol` | string | — | 交易对，如 `BTCUSDT` |
| `items[].precision` | int | — | 价格小数位，0–8 |

配置写入采用临时文件 + 原子替换；文件损坏时会备份为 `config.json.bak` 并回退默认配置。升级时自动执行版本迁移。

其他目录：

```text
%APPDATA%\MenuBarCryptoPrice\cache\   币种目录缓存（24h TTL）
%APPDATA%\MenuBarCryptoPrice\logs\    运行日志
```

## 数据源与隐私

行情来自 Bitget 公开接口：

- WebSocket：`wss://ws.bitget.com/v2/ws/public`，频道 `ticker`，同时订阅 `SPOT` 与 `USDT-FUTURES`。
- REST 现货：`/api/v2/spot/market/tickers?symbol=...`
- REST 合约：`/api/v2/mix/market/ticker?symbol=...&productType=USDT-FUTURES`
- 币种目录：`/api/v2/spot/public/symbols` 与 `/api/v2/mix/market/contracts?productType=USDT-FUTURES`

程序不需要 API Key，不访问账户、持仓或交易接口。

隐私说明：

- 不上传配置、自选币种或使用数据。
- 不包含遥测、统计或广告 SDK。
- 仅向 Bitget 公开行情接口发起网络请求。
- 配置、缓存和日志全部保存在本机 `%APPDATA%\MenuBarCryptoPrice`。

## 从源码构建

### 环境

- Windows 11 x64。
- .NET SDK 8.0 或更高版本。
- Git。
- 构建安装程序需要 Inno Setup 6。

安装 Inno Setup 6：

```powershell
winget install --id JRSoftware.InnoSetup -e --source winget
```

### 构建

```powershell
git clone https://github.com/BeyondESH/MenuBar-CryptoPrice.git
cd MenuBar-CryptoPrice
dotnet build MenuBarCryptoPrice.slnx
```

解决方案包含：

- `src/MenuBarCryptoPrice`：WPF 主程序。
- `src/MenuBarCryptoPrice.Settings.WinUI`：WinUI 3 设置客户端。
- `tests/MenuBarCryptoPrice.Tests`：xUnit 测试。

### 本地运行

```powershell
dotnet run --project src/MenuBarCryptoPrice/MenuBarCryptoPrice.csproj
```

主程序会从调试输出目录寻找设置客户端；首次构建请同时构建 WinUI 项目。也可以直接启动：

```powershell
src\MenuBarCryptoPrice\bin\Debug\net8.0-windows\win-x64\MenuBarCryptoPrice.exe
```

### 发布

```powershell
.\scripts\publish.ps1
```

输出：

```text
dist\
  MenuBarCryptoPrice.exe
  settings\
    MenuBarCryptoPrice.Settings.WinUI.exe
    Assets\
    ...
```

主程序为单文件自包含；WinUI 设置客户端为自包含目录（XAML 资源、`Assets`、运行时文件需要一起发布）。

### 构建安装程序

```powershell
.\scripts\build-installer.ps1
```

该脚本会先执行 `scripts\publish.ps1`，再调用 Inno Setup 编译：

```text
dist\MenuBarCryptoPrice-Setup-1.0.1.exe
```

如果 `dist` 已是最新发布结果，可跳过发布：

```powershell
.\scripts\build-installer.ps1 -SkipPublish
```

安装程序脚本位于 `installer\MenuBarCryptoPrice.iss`，简体中文语言文件位于 `installer\ChineseSimplified.isl`。

## 测试

```powershell
dotnet test tests/MenuBarCryptoPrice.Tests/MenuBarCryptoPrice.Tests.csproj
```

当前覆盖 61 个测试，主要包括：

- 配置读取/迁移/损坏恢复/round-trip。
- Bitget 报文解析与格式化边界。
- 重连退避与数据新鲜度。
- 任务栏可见性策略与几何计算。
- 任务栏条溢出、tooltip 与浮层状态。
- 币种目录解析与缓存。
- 市场/交易对去重与精度。

手工验收建议：

- 任务栏左/右贴靠与 100%/125%/150% DPI。
- 全屏应用、开始菜单、搜索、任务视图下的让位。
- 悬停详情、`+N` 列表、点击外部关闭。
- 设置页修改后任务栏即时生效。
- 安装/升级/卸载与开机自启清理。

## 项目结构

```text
MenuBar-CryptoPrice/
├─ src/
│  ├─ MenuBarCryptoPrice/                     WPF 主程序
│  │  ├─ Core/                                配置模型、市场与交易对
│  │  ├─ MarketData/                          REST/WS、目录、解析、格式化
│  │  ├─ Taskbar/                             定位、覆盖窗口、可见性、虚拟桌面
│  │  ├─ Controls/                            任务栏条、浮层与交互
│  │  ├─ Infrastructure/                      配置、日志、托盘、自启、单实例
│  │  ├─ Assets/app.ico
│  │  └─ refs/                                UIAutomation 引用程序集
│  └─ MenuBarCryptoPrice.Settings.WinUI/      WinUI 3 设置客户端
│     ├─ Pages/                               行情 / 外观 / 关于
│     ├─ ViewModels/
│     ├─ Dialogs/                             添加币种
│     ├─ Assets/                              app.ico / dashang.jpg
│     └─ MenuBarCryptoPrice.Settings.WinUI.csproj
├─ tests/MenuBarCryptoPrice.Tests/            xUnit 测试
├─ installer/                                 Inno Setup 脚本与中文语言文件
├─ scripts/                                   publish.ps1 / build-installer.ps1
├─ docs/icon.png                              README 图标
├─ LICENSE                                    GPL-3.0
├─ README.md
└─ CHANGELOG.md
```

## 已知限制与 FAQ

### 支持哪些 Windows 版本？

Windows 11 x64（22000+）。任务栏覆盖依赖 Windows 11 的 `Shell_TrayWnd` 与任务栏空白区布局；Windows 10 和 Windows 11 早期版本未验证。

### 支持副屏任务栏吗？

当前主要支持主屏任务栏。副屏 `Shell_SecondaryTrayWnd` 不在 v1 的验收范围内。

### 任务栏条不显示或位置不对怎么办？

- 托盘菜单 → 重新定位。
- 在外观页切换“贴靠方向”或调整“显示数量”。
- 检查任务栏是否自动隐藏、是否处于全屏应用/开始菜单/任务视图。
- 多显示器或高 DPI 下可在配置中微调 `offsetX`。
- 查看 `%APPDATA%\MenuBarCryptoPrice\logs` 中的定位日志。

### 设置窗口最小化后为什么又出现？

1.0.1 已修复。此前启动阶段的可见性守护会在 8 秒内恢复任何最小化窗口，导致用户点最小化后又被拉起。现在守护在首次激活后立即停止；旧版本请升级到 1.0.1。

### 数据不更新/显示“数据延迟”？

- 检查网络、代理或防火墙是否拦截 `api.bitget.com` 与 `ws.bitget.com`。
- 程序会在 WebSocket 不可用时退化为 REST 轮询；超过 30 秒未更新的价格会灰显。
- 查看日志中的 WebSocket/REST 错误。

### 配置损坏了怎么办？

程序会尝试把损坏文件备份为 `config.json.bak` 并回退默认配置。也可以关闭程序后删除 `%APPDATA%\MenuBarCryptoPrice\config.json`，下次启动重新生成。

### 安装包为什么提示未知发布者？

安装包尚未使用代码签名证书签名。SmartScreen 会提示未知发布者；确认从本仓库 Releases 下载后，可选择“更多信息 → 仍要运行”。后续版本如获得证书会进行签名。

### 为什么安装目录不在 Program Files？

安装程序采用按用户安装（`%LOCALAPPDATA%\Programs`），不需要管理员权限，也避免 UAC。应用只服务当前用户，配置和开机自启均在 HKCU。

### 卸载会删除我的配置吗？

不会。卸载保留 `%APPDATA%\MenuBarCryptoPrice`。如需彻底清理，手动删除该目录。

## 参与贡献

欢迎提交 Issue 和 Pull Request：

1. Fork 本仓库。
2. 创建分支：`git checkout -b feature/your-feature`。
3. 修改代码并运行 `dotnet build MenuBarCryptoPrice.slnx` 与 `dotnet test`。
4. 提交 PR，说明变更目的、验证方式和截图（如有）。
5. 不要提交 API Key、个人配置、日志或构建产物。

项目使用 GPL-3.0-or-later；提交贡献即表示同意以相同协议发布。

## 支持作者

如果这个工具对你有帮助，欢迎在 GitHub 点一个 Star，或在客户端“关于 → 查看打赏码”请作者喝杯咖啡。打赏完全自愿。

<p align="center">
  <img src="dashang.jpg" width="200" alt="打赏作者" />
</p>

## 开源协议

本项目采用 **GNU General Public License v3.0 or later（GPL-3.0-or-later）**。

```text
Copyright (C) 2026 BeyondESH

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
```

完整协议文本见 [LICENSE](LICENSE)。分发本软件的二进制版本时，请同时遵守 GPL 的源代码提供义务。

## 第三方组件

- .NET / WPF：MIT License。
- Windows App SDK / WinUI 3：MIT License。
- CommunityToolkit.Mvvm：MIT License。
- CommunityToolkit.WinUI.Controls.SettingsControls：MIT License。
- Inno Setup：Inno Setup License。
- UIAutomation 引用程序集：Microsoft Windows SDK 组件。
- Bitget 公开行情接口：由 Bitget 提供，本项目与其无隶属关系。

各组件版权归其各自作者所有。

## 致谢与免责声明

感谢 Bitget 提供公开行情接口，感谢 .NET、WPF、WinUI 3、CommunityToolkit 与 Inno Setup 开源社区。

本项目仅用于学习与个人行情展示，不提供交易、投资建议或任何形式的收益承诺。加密货币价格波动剧烈，请自行判断风险。

## 版本历史

- **v1.0.1**：修复设置窗口最小化后被启动可见性守护恢复的问题；WinUI 3 设置客户端、任务栏展开层外部点击关闭、GitHub/打赏/图标与安装程序。
- **v1.0.0**：首个版本，任务栏行情覆盖层、Bitget 现货/永续、设置客户端与安装包。

完整记录见 [CHANGELOG.md](CHANGELOG.md)。
