# 局域网 Web 服务器快速启动器 | JegServiceStarter

[![Build and Release](https://github.com/JegXiao/JegServiceStarter/actions/workflows/release.yml/badge.svg)](https://github.com/JegXiao/JegServiceStarter/actions/workflows/release.yml)

> 一键把 EXE 所在文件夹变成局域网 HTTP/HTTPS 服务器的 Windows 桌面工具（WPF + ASP.NET Core Kestrel），无需命令行。

**中文** · [ English](#english)

---

## 下载 | Download

直接下载编译好的 EXE（自包含单文件，无需安装运行时）：

- **最新版**：<https://github.com/JegXiao/JegServiceStarter/releases/latest>
- **v2.2 直链**：<https://github.com/JegXiao/JegServiceStarter/releases/download/v2.2/JegServiceStarter2.2.exe>

EXE 由 GitHub Actions 在推送 `v*` 标签时自动编译发布（工作流：[release.yml](.github/workflows/release.yml)，也可在 [Actions 页面](https://github.com/JegXiao/JegServiceStarter/actions)手动触发）。

## 功能特性

- **文件托管**：以 EXE 所在目录为根目录，托管 HTML/CSS/JS/图片等静态文件
- **首页勾选**：文件选取列表中每个 HTML 前是单选圆点，勾选哪个，访问 `/` 即打开哪个页面
- **HTTP / HTTPS 一键切换**：勾选"证书"即生成内存自签证书（RSA 2048，SAN 含本机 IP 与 localhost，有效期 5 年），不落盘
- **地址自动探测**：枚举本机所有可用 IPv4（排除回环、`169.254.*` 自动专用地址和名为 `tun0` 的 VPN 虚拟网卡），`192.168.*` 网段优先排序；每个地址一行可点击网址，另有 `localhost` 与 `127.0.0.1` 各一行，全部带"复制"按钮
- **自动重启**：修改端口、切换证书、切换首页、刷新文件列表或刷新网络后，服务器自动重启（只重启 Web 主机，不重启软件）
- **网络刷新**：网络环境变化（切换 WiFi 等）后点"刷新"按钮同步地址与网址列表
- **防火墙辅助**：首次点"运行"时自动请求管理员权限添加程序级入站放行规则（规则名 `JegServiceStarter`，覆盖域/专用/公用配置文件）
- **IP 封禁**：命令区支持 `ban <ip>` / `unban <ip>`，输入短格式（如 `.123`）自动用本机 IP 前缀补全；被封 IP 收到 404；重启服务器清空列表
- **实时日志**：显示每个请求的来源 IP、方法、路径与响应码

## 环境要求

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)（仅编译需要；发布的 EXE 为自包含单文件，使用方无需安装运行时）

## 快速开始

```bash
git clone https://github.com/JegXiao/JegServiceStarter.git
cd JegServiceStarter

# 开发调试运行（Debug）
dotnet run

# 正式构建：自动执行单文件发布，成品 EXE 导出到 export\ 文件夹
dotnet build -c Release
```

构建完成后在 `export\` 里得到 `JegServiceStarter<版本号>.exe`（自包含单文件，双击即用）。

也可以手动发布：

```bash
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

## 使用说明

1. 把 EXE 和要分享的文件放在**同一个文件夹**（服务器以 EXE 所在目录为根，无法浏览其上级目录）
2. 运行 EXE，在"文件选取"列表勾选首页 HTML
3. 需要时勾选"证书"启用 HTTPS，修改端口（默认 **8000**），点"运行"
4. 同一局域网的设备（如手机连同一 WiFi）打开界面显示的网址即可访问

首次点"运行"会弹出 UAC 授权窗口（添加防火墙规则），确认一次即可；未签名 EXE 可能触发 SmartScreen 提示，属正常现象。

## 配置

所有配置都在界面上完成，无配置文件：

| 配置项 | 默认值 | 说明 |
|---|---|---|
| 端口 | `8000` | 修改后网址列表即时更新，运行中则自动重启 |
| 证书（HTTPS） | 关 | 内存自签证书，勾选即启用 |
| 首页 | 第一个 HTML | 单选勾选，即选即生效 |

## 说明

- **定位是局域网工具**：不支持公网直接访问（运营商普遍拦截入站），也不要在生产环境暴露敏感文件
- **版本号**：窗口标题与导出文件名中的版本号来自 csproj 的 `<Version>`，两处永远一致；GitHub Actions 发布时也读它拼文件名
- **自动导出**：Release 构建会自动发布并把带版本号的 EXE 复制到 `export\`（该目录已被 gitignore）；构建前请关闭正在运行的旧 EXE，否则文件占用会导致构建失败
- **首次启动稍慢**：单文件自包含 EXE 首次运行要把本地库解压到临时目录，慢 1–2 秒属正常

## 版权

版权所有 © 2026 JegXiao，保留所有权利。未经许可不得复制、分发或修改本项目代码。

---

# English

[![Build and Release](https://github.com/JegXiao/JegServiceStarter/actions/workflows/release.yml/badge.svg)](https://github.com/JegXiao/JegServiceStarter/actions/workflows/release.yml)

> A Windows desktop tool (WPF + ASP.NET Core Kestrel) that turns the folder containing the EXE into a LAN HTTP/HTTPS server with one click — no command line needed.

[中文](#局域网-web-服务器快速启动器--jegservicestarter) · **English**

---

## Download

Grab a prebuilt EXE directly (self-contained single file, no runtime required):

- **Latest**: <https://github.com/JegXiao/JegServiceStarter/releases/latest>
- **v2.2 direct link**: <https://github.com/JegXiao/JegServiceStarter/releases/download/v2.2/JegServiceStarter2.2.exe>

The EXE is built and released automatically by GitHub Actions whenever a `v*` tag is pushed (see [release.yml](.github/workflows/release.yml); manual runs via the [Actions page](https://github.com/JegXiao/JegServiceStarter/actions)).

## Features

- **Static hosting**: serves the EXE's own folder as the web root (HTML/CSS/JS/images)
- **Homepage picker**: radio buttons in front of every HTML file — the checked one becomes the page served at `/`
- **HTTP / HTTPS toggle**: checking the box generates an in-memory self-signed certificate (RSA 2048, SAN with local IP + localhost, valid 5 years); nothing is written to disk
- **Address discovery**: lists every usable IPv4 (skips loopback, `169.254.*` link-local autoconf, and the `tun0` VPN adapter), preferring `192.168.*` subnets; each address gets a clickable URL plus one `localhost` and one `127.0.0.1` row, all with a copy button
- **Auto restart**: changing the port, toggling HTTPS, switching the homepage, refreshing the file list or the network restarts the web host automatically (the app itself keeps running)
- **Network refresh**: a refresh button re-detects addresses after your network changes (e.g. switching Wi-Fi)
- **Firewall helper**: the first click on Run elevates via UAC and adds a program-level inbound allow rule named `JegServiceStarter` for domain/private/public profiles
- **IP banning**: `ban <ip>` / `unban <ip>` commands in the command box; short input like `.123` is auto-completed with the local prefix; banned IPs get 404; the list clears on server restart
- **Live log**: shows source IP, method, path and status code for every request

## Requirements

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (only for building; the published EXE is self-contained, so end users need no runtime installed)

## Quick Start

```bash
git clone https://github.com/JegXiao/JegServiceStarter.git
cd JegServiceStarter

# Dev run (Debug)
dotnet run

# Release build: publishes a single-file EXE automatically into the export\ folder
dotnet build -c Release
```

After building you get `JegServiceStarter<version>.exe` in `export\` — a self-contained single file, ready to run.

Manual publishing works too:

```bash
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

## Usage

1. Put the EXE and the files to share in the **same folder** — the EXE's folder is the web root; parent directories are not browsable
2. Run the EXE and check the homepage HTML in the selection list
3. Tick the certificate box for HTTPS if needed, adjust the port (default **8000**), then click Run
4. Devices on the same LAN (e.g. a phone on the same Wi-Fi) open one of the displayed URLs

The first Run click pops a UAC prompt (for the firewall rule) — confirm once. An unsigned EXE may trigger a SmartScreen warning; that is expected.

## Configuration

Everything is configured in the UI; there is no config file:

| Item | Default | Notes |
|---|---|---|
| Port | `8000` | URL list updates instantly; a running server restarts automatically |
| HTTPS | Off | In-memory self-signed cert, enabled by ticking |
| Homepage | First HTML file | Radio-checked; takes effect immediately |

## Notes

- **LAN-only by design**: not reachable from the public internet (inbound traffic is blocked by most ISPs), and do not expose sensitive files
- **Versioning**: the version shown in the window title and the exported file name both come from `<Version>` in the csproj, so they always match; GitHub Actions reads the same value when naming release assets
- **Auto export**: a Release build publishes automatically and copies the versioned EXE to `export\` (gitignored). Close a running EXE before building, or the copy step fails on a file lock
- **Slow first launch**: the self-contained EXE extracts native libraries to a temp dir on first run — 1–2 seconds is normal

## Copyright

Copyright © 2026 JegXiao. All rights reserved. Copying, distribution or modification of this code is not permitted without permission.
