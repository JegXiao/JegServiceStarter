# 局域网 Web 服务器快速启动器 | JegServiceStarter

> 一键把 EXE 所在文件夹变成局域网 HTTP/HTTPS 服务器的 Windows 桌面工具（WPF + ASP.NET Core Kestrel），无需命令行。
> A Windows desktop tool (WPF + ASP.NET Core Kestrel) that turns the folder containing the EXE into a LAN HTTP/HTTPS server with one click — no command line needed.

## 功能特性 | Features

- **文件托管**：以 EXE 所在目录为根目录，托管 HTML/CSS/JS/图片等静态文件
  **Static hosting**: serves the EXE's own folder as the web root (HTML/CSS/JS/images)
- **首页勾选**：文件选取列表中每个 HTML 前是单选圆点，勾选哪个，访问 `/` 即打开哪个页面
  **Homepage picker**: radio buttons in front of every HTML file — the checked one becomes the page served at `/`
- **HTTP / HTTPS 一键切换**：勾选"证书"即生成内存自签证书（RSA 2048，SAN 含本机 IP 与 localhost，有效期 5 年），不落盘
  **HTTP / HTTPS toggle**: checking the box generates an in-memory self-signed certificate (RSA 2048, SAN with local IP + localhost, valid 5 years); nothing is written to disk
- **地址自动探测**：枚举本机所有可用 IPv4（排除回环、`169.254.*` 自动专用地址和名为 `tun0` 的 VPN 虚拟网卡），`192.168.*` 网段优先排序；每个地址一行可点击网址，另有 `localhost` 与 `127.0.0.1` 各一行，全部带"复制"按钮
  **Address discovery**: lists every usable IPv4 (skips loopback, `169.254.*` link-local autoconf, and the `tun0` VPN adapter), preferring `192.168.*` subnets; each address gets a clickable URL plus one `localhost` and one `127.0.0.1` row, all with a copy button
- **自动重启**：修改端口、切换证书、切换首页、刷新文件列表或刷新网络后，服务器自动重启（只重启 Web 主机，不重启软件）
  **Auto restart**: changing the port, toggling HTTPS, switching the homepage, refreshing the file list or the network restarts the web host automatically (the app itself keeps running)
- **网络刷新**：网络环境变化（切换 WiFi 等）后点"刷新"按钮同步地址与网址列表
  **Network refresh**: a refresh button re-detects addresses after your network changes (e.g. switching Wi-Fi)
- **防火墙辅助**：首次点"运行"时自动请求管理员权限添加程序级入站放行规则（规则名 `JegServiceStarter`，覆盖域/专用/公用配置文件）
  **Firewall helper**: the first click on "运行" (Run) elevates via UAC and adds a program-level inbound allow rule named `JegServiceStarter` for domain/private/public profiles
- **IP 封禁**：命令区支持 `ban <ip>` / `unban <ip>`，输入短格式（如 `.123`）自动用本机 IP 前缀补全；被封 IP 收到 404；重启服务器清空列表
  **IP banning**: `ban <ip>` / `unban <ip>` commands in the command box; short input like `.123` is auto-completed with the local prefix; banned IPs get 404; the list clears on server restart
- **实时日志**：显示每个请求的来源 IP、方法、路径与响应码
  **Live log**: shows source IP, method, path and status code for every request

## 环境要求 | Requirements

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)（仅编译需要；发布产物为自包含单文件，使用方无需安装运行时）
  [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) is only needed to build; the published EXE is self-contained, so end users need no runtime installed

## 快速开始 | Quick Start

```bash
git clone https://github.com/JegXiao/JegServiceStarter.git
cd JegServiceStarter

# 开发调试运行（Debug）
dotnet run

# 正式构建：自动执行单文件发布，成品 EXE 导出到 export\ 文件夹
dotnet build -c Release
```

构建完成后在 `export\` 里得到 `JegServiceStarter<版本号>.exe`（自包含单文件，双击即用）。
After building, you get `JegServiceStarter<version>.exe` in `export\` — a self-contained single file, ready to run.

也可以手动发布：
Manual publishing works too:

```bash
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

## 使用说明 | Usage

1. 把 EXE 和要分享的文件放在**同一个文件夹**（服务器以 EXE 所在目录为根，无法浏览其上级目录）
   Put the EXE and the files to share in the **same folder** — the EXE's folder is the web root
2. 运行 EXE，在"文件选取"列表勾选首页 HTML
   Run the EXE and check the homepage HTML in the selection list
3. 需要时勾选"证书"启用 HTTPS，修改端口（默认 **8000**），点"运行"
   Tick the certificate box for HTTPS if needed, adjust the port (default **8000**), then click Run
4. 同一局域网的设备（如手机连同一 WiFi）打开界面显示的网址即可访问
   Devices on the same LAN (e.g. a phone on the same Wi-Fi) open one of the displayed URLs

首次点"运行"会弹出 UAC 授权窗口（添加防火墙规则），确认一次即可；未签名 EXE 可能触发 SmartScreen 提示，属正常现象。
The first Run click pops a UAC prompt (for the firewall rule) — confirm once. An unsigned EXE may trigger a SmartScreen warning; that is expected.

## 配置 | Configuration

所有配置都在界面上完成，无配置文件：
Everything is configured in the UI; there is no config file:

| 配置项 | Item | 默认值 | Default | 说明 | Notes |
|---|---|---|---|---|---|
| 端口 | Port | `8000` | `8000` | 修改后网址列表即时更新，运行中则自动重启 | URL list updates instantly; running servers restart automatically |
| 证书 | HTTPS | 关 | Off | 内存自签证书，勾选即启用 | In-memory self-signed cert, enabled by ticking |
| 首页 | Homepage | 第一个 HTML | First HTML | 单选勾选，即选即生效 | Radio-checked; takes effect immediately |

## 说明 | Notes

- **定位是局域网工具**：不支持公网直接访问（运营商普遍拦截入站），也不要在生产环境暴露敏感文件
  **LAN-only by design**: not reachable from the public internet (inbound traffic is blocked by most ISPs), and do not expose sensitive files
- **版本号**：窗口标题与导出文件名中的版本号来自 csproj 的 `<Version>`，两处永远一致
  **Versioning**: the version shown in the window title and the exported file name both come from `<Version>` in the csproj, so they always match
- **自动导出**：Release 构建会自动发布并把带版本号的 EXE 复制到 `export\`（该目录已被 gitignore）；构建前请关闭正在运行的旧 EXE，否则文件占用会导致构建失败
  **Auto export**: a Release build publishes automatically and copies the versioned EXE to `export\` (gitignored). Close a running EXE before building, or the copy step fails on a file lock
- **首次启动稍慢**：单文件自包含 EXE 首次运行要把本地库解压到临时目录，慢 1–2 秒属正常
  **Slow first launch**: the self-contained EXE extracts native libraries to a temp dir on first run — 1–2 seconds is normal

## 许可证 | License

[MIT](LICENSE)
