using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;


namespace JegServiceStarter
{
    public partial class MainWindow : Window
    {
        // ASP.NET 主机实例
        private IHost? _webHost;

        // 是否正在运行
        private bool _isRunning = false;
        
        // 封禁IP列表
        private readonly HashSet<string> _bannedIps = new();

        // 静态文件根目录（当前 exe 目录）
        private string _baseDir;

        // 本机所有 IPv4 地址（优先 192.168 开头的局域网地址）
        private readonly List<string> _ips = new();

        // 服务器绑定的 IP（第一个）
        private string SelectedIp => _ips.FirstOrDefault() ?? "127.0.0.1";

        // 第一个网址（运行后自动打开用）
        private string? _firstUrl;

        public MainWindow()
        {
            InitializeComponent();

            // 标题带版本号（读取 csproj 中的 Version，只显示主版本.次版本）
            var ver = typeof(MainWindow).Assembly.GetName().Version;
            Title = $"JegServiceStarter v{ver?.Major}.{ver?.Minor}";

            // 当前运行目录（bin/Debug/...）
            _baseDir = AppDomain.CurrentDomain.BaseDirectory;

            LoadLocalIps();

            LoadFiles();

            UpdateIpDisplay();
            UpdateLinks();
            
            UpdateButtonState(); 
        }

        // =========================
        // 文件读取
        // =========================
        private void LoadFiles()
        {
            var selectedHtml = HtmlList.SelectedItem?.ToString();

            var files = Directory.GetFiles(_baseDir);

            // 左侧：Web相关文件
            var webFiles = files
                .Where(f =>
                    f.EndsWith(".html") ||
                    f.EndsWith(".htm") ||
                    f.EndsWith(".css") ||
                    f.EndsWith(".js") ||
                    f.EndsWith(".png") ||
                    f.EndsWith(".jpg") ||
                    f.EndsWith(".jpeg") ||
                    f.EndsWith(".gif"))
                .Select(Path.GetFileName)
                .ToList();

            AllFilesList.ItemsSource = webFiles;

            // 中间：只HTML
            var htmlFiles = files
                .Where(f => f.EndsWith(".html") || f.EndsWith(".htm"))
                .Select(Path.GetFileName)
                .ToList();

            HtmlList.ItemsSource = htmlFiles;

            // 恢复选中项
            if (selectedHtml != null && htmlFiles.Contains(selectedHtml))
                HtmlList.SelectedItem = selectedHtml;
            else if (htmlFiles.Any())
                HtmlList.SelectedIndex = 0;
        }

        // 刷新文件列表
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadFiles();
            Log("文件列表已刷新");
            AutoRestartServer("刷新文件列表");
        }
        
        //打开文件夹
        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = _baseDir,
                UseShellExecute = true
            });
        }


        // =========================
        // 获取本机所有 IPv4 地址（枚举所有网卡，192.168 开头的排在前面）
        // =========================
        private void LoadLocalIps()
        {
            var found = new List<string>();

            try
            {
                foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                        continue;

                    // 跳过 VPN 虚拟网卡（tun0）
                    if (nic.Name.Equals("tun0", StringComparison.OrdinalIgnoreCase))
                        continue;

                    foreach (var addr in nic.GetIPProperties().UnicastAddresses)
                    {
                        var ip = addr.Address;

                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(ip) &&
                            !ip.ToString().StartsWith("169.254."))   // 排除未连接的自动专用地址
                        {
                            found.Add(ip.ToString());
                        }
                    }
                }
            }
            catch
            {
                // 获取失败则保留默认回环地址
            }

            _ips.Clear();

            // 192.168 开头的局域网地址优先，其余（如 10.x）排在后面
            _ips.AddRange(found
                .Where(a => a.StartsWith("192.168."))
                .Distinct());
            _ips.AddRange(found
                .Where(a => !a.StartsWith("192.168."))
                .Distinct());
        }

        // 更新 IP 显示区（纯数字，无边框；多地址用 " / " 分隔）
        private void UpdateIpDisplay()
        {
            if (SelectedIpText == null) return;

            SelectedIpText.Text = _ips.Count > 0
                ? string.Join("  /  ", _ips)
                : "127.0.0.1";
        }

        // 网络变化后手动刷新本机地址
        private void RefreshNetwork_Click(object sender, RoutedEventArgs e)
        {
            LoadLocalIps();
            UpdateIpDisplay();
            UpdateLinks();
            Log($"本机IP已刷新: {string.Join(" / ", _ips)}");
            AutoRestartServer("网络刷新");
        }

        // =========================
        // 启动服务器
        // =========================
        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                Log("服务器已在运行中");
                return;
            }

            if (await StartServerCore())
            {
                EnsureFirewallRule();
                OpenFirstLink();
            }
        }

        // =========================
        // 尝试为本程序添加防火墙入站放行规则（每次软件运行只提示一次）
        // =========================
        private bool _firewallRuleAttempted = false;

        private void EnsureFirewallRule()
        {
            if (_firewallRuleAttempted)
                return;

            _firewallRuleAttempted = true;

            try
            {
                string exe = Environment.ProcessPath
                             ?? System.Reflection.Assembly.GetExecutingAssembly().Location;

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall add rule name=\"JegServiceStarter\" dir=in action=allow " +
                                $"program=\"{exe}\" enable=yes profile=any",
                    Verb = "runas",          // 请求管理员权限（弹出 UAC）
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };

                System.Diagnostics.Process.Start(psi);
                Log("已尝试添加防火墙放行规则（如弹出 UAC 请确认）");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Log("防火墙规则添加被取消，公网设备可能无法访问；可在防火墙中手动放行本程序");
            }
            catch (Exception ex)
            {
                Log($"添加防火墙规则失败: {ex.Message}");
            }
        }

        // =========================
        // 自动重启服务器（软件内任何设置发生变化时调用）
        // 注意：重启的是服务器，不是本软件
        // =========================
        private async void AutoRestartServer(string reason)
        {
            if (!_isRunning)
                return;   // 未运行时无需处理

            try
            {
                Log($"检测到操作（{reason}），自动重启服务器...");

                await StopServerCore();
                await StartServerCore();

                Log("服务器自动重启完成");
            }
            catch (Exception ex)
            {
                Log($"自动重启失败: {ex.Message}");
            }
        }

        // =========================
        // 停止服务器（核心，不做日志装饰）
        // =========================
        private async Task StopServerCore()
        {
            if (_webHost != null)
            {
                await _webHost.StopAsync();
                _webHost.Dispose();
                _webHost = null;
            }

            _bannedIps.Clear();
            CommandLog("封禁列表已清空（重启）", "system");

            _isRunning = false;
            UpdateButtonState();
        }

        // =========================
        // 启动服务器（核心）
        // =========================
        private async Task<bool> StartServerCore()
        {
            if (_isRunning)
                return false;

            if (HtmlList.SelectedItem == null)
            {
                Log("请选择一个HTML文件（勾选单选框）");
                return false;
            }

            if (!int.TryParse(PortBox.Text, out int port))
            {
                Log("端口格式错误");
                return false;
            }

            string ip = SelectedIp;
            string file = HtmlList.SelectedItem.ToString();
            string protocol = HttpsCheck.IsChecked == true ? "https" : "http";

            try
            {
                _webHost = Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.ConfigureKestrel(options =>
                        {
                            if (HttpsCheck.IsChecked == true)
                            {
                                // 生成内存自签证书
                                var cert = CreateSelfSignedCertificate(ip);

                                options.ListenAnyIP(port, listenOptions =>
                                {
                                    listenOptions.UseHttps(cert);
                                });

                                Log("已启用 HTTPS（内存自签证书）");
                            }
                            else
                            {
                                options.ListenAnyIP(port);
                                Log("已启用 HTTP");
                            }
                        });


                        webBuilder.Configure(app =>
                        {
                            // 开发异常页面（调试时可打开）
                            // app.UseDeveloperExceptionPage();
                            
                            // 封禁IP拦截
                            app.Use(async (context, next) =>
                            {
                                var remoteIp = context.Connection.RemoteIpAddress;

                                if (remoteIp != null)
                                {
                                    // 统一转换为 IPv4 格式
                                    var ipString = remoteIp.MapToIPv4().ToString();

                                    Log($"检测IP: {ipString}");

                                    if (_bannedIps.Contains(ipString))
                                    {
                                        // await Task.Delay(2000);
                                        // context.Abort();   // 🔥 直接断开 TCP 连接
                                        context.Response.StatusCode = 404;
                                        // await context.Response.WriteAsync("Forbidden: You are banned.");
                                        Log($"已拦截封禁IP访问: {ipString}");
                                        return;
                                    }
                                }

                                await next();
                            });

                            // 访问日志中间件
                            app.Use(async (context, next) =>
                            {
                                var remoteIp = context.Connection.RemoteIpAddress?.ToString();
                                var agent = context.Request.Headers["User-Agent"].ToString();

                                await next();

                                var status = context.Response.StatusCode;
                                
                                // Log($"检测IP: {remoteIp}");
                                Log($"[{remoteIp}] {context.Request.Method} {context.Request.Path} → {status}");
                            });



                            // 首页映射
                            app.Use(async (context, next) =>
                            {
                                if (context.Request.Path == "/")
                                {
                                    context.Request.Path = "/" + file;
                                }

                                await next();
                            });

                            // 静态文件托管
                            app.UseStaticFiles(new StaticFileOptions
                            {
                                FileProvider = new PhysicalFileProvider(_baseDir)
                            });
                        });
                    })
                    .Build();

                await _webHost.StartAsync();

                _isRunning = true;
                UpdateButtonState();
                
                Log($"服务器启动成功，托管首页: {file}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"启动失败: {ex.Message}");
                return false;
            }
        }
        
        // =========================
        // 自动打开浏览器
        // =========================
        private void OpenFirstLink()
        {
            if (string.IsNullOrEmpty(_firstUrl))
                return;

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _firstUrl,
                UseShellExecute = true
            };

            System.Diagnostics.Process.Start(psi);
        }

        // =========================
        // 重启逻辑
        // =========================
        // =========================
        // 停止服务器
        // =========================
        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                Log("服务器未运行");
                return;
            }

            try
            {
                await StopServerCore();
                Log("服务器已停止");
            }
            catch (Exception ex)
            {
                Log($"停止失败: {ex.Message}");
            }
        }
        
        // =========================
        // 创建内存自签名证书
        // =========================
        private X509Certificate2 CreateSelfSignedCertificate(string ip)
        {
            using var rsa = RSA.Create(2048);

            var request = new CertificateRequest(
                $"CN={ip}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // 添加 SAN（支持 IP + localhost）
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(IPAddress.Parse(ip));
            sanBuilder.AddDnsName("localhost");

            request.CertificateExtensions.Add(sanBuilder.Build());

            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false));

            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature |
                    X509KeyUsageFlags.KeyEncipherment,
                    false));

            var cert = request.CreateSelfSigned(
                DateTimeOffset.Now,
                DateTimeOffset.Now.AddYears(5));

            // 转为可被 Kestrel 使用的证书
            return new X509Certificate2(cert.Export(X509ContentType.Pfx));
        }

        // =========================
        // 按钮控制方法
        // =========================
        private void UpdateButtonState()
        {
            if (_isRunning)
            {
                RunButton.IsEnabled = false;
                StopButton.IsEnabled = true;
            }
            else
            {
                RunButton.IsEnabled = true;
                StopButton.IsEnabled = false;
            }
        }

        // =========================
        // 安全日志输出（跨线程安全）
        // =========================
        private void Log(string msg)
        {
            // 如果不是UI线程，切换回UI线程
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => Log(msg));
                return;
            }

            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            LogBox.ScrollToEnd();
        }
        
        // =========================
        // 命令区输出（支持颜色）
        // =========================
        private void CommandLog(string msg, string type = "normal")
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => CommandLog(msg, type));
                return;
            }

            var paragraph = new System.Windows.Documents.Paragraph();
            var run = new System.Windows.Documents.Run($"[{DateTime.Now:HH:mm:ss}] {msg}");

            switch (type)
            {
                case "success":
                    run.Foreground = System.Windows.Media.Brushes.Green;
                    break;
                case "error":
                    run.Foreground = System.Windows.Media.Brushes.Red;
                    break;
                case "system":
                    run.Foreground = System.Windows.Media.Brushes.Blue;
                    break;
                default:
                    run.Foreground = System.Windows.Media.Brushes.Black;
                    break;
            }

            paragraph.Inlines.Add(run);

            CommandHistoryBox.Document.Blocks.Add(paragraph);
            CommandHistoryBox.ScrollToEnd();
        }

        // =========================
        // 管理输入发送
        // =========================
        private void CommandSend_Click(object sender, RoutedEventArgs e)
        {
            string cmd = CommandInputBox.Text.Trim();
            CommandLog($"> {cmd}", "system");
            if (string.IsNullOrWhiteSpace(cmd))
                return;

            if (cmd.StartsWith("ban "))
            {
                string ipInput = cmd.Substring(4).Trim();

                string? fullIp = BuildFullIp(ipInput);

                if (fullIp != null)
                {
                    _bannedIps.Add(fullIp);
                    CommandLog($"已封禁设备: {fullIp}", "success");

                }
                else
                {
                    CommandLog("ban 命令格式错误", "error");

                }
            }
            else if (cmd.StartsWith("unban "))
            {
                string ipInput = cmd.Substring(6).Trim();

                string? fullIp = BuildFullIp(ipInput);

                if (fullIp != null)
                {
                    if (_bannedIps.Remove(fullIp))
                    {
                        CommandLog($"已解除封禁: {fullIp}", "success");
                    }
                    else
                    {
                        CommandLog($"该IP未被封禁: {fullIp}", "error");
                    }
                }
                else
                {
                    CommandLog("unban 命令格式错误", "error");
                }
            }
            else
            {
                CommandLog($"未知命令: {cmd}", "error");

            }

            CommandInputBox.Clear();
        }


        // 回车发送
        private void CommandInputBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                CommandSend_Click(null, null);
            }
        }

        // =========================
        // 点击超链接打开浏览器
        // =========================
        private void OpenLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Documents.Hyperlink link)
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = link.NavigateUri.ToString(),
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(psi);
            }
        }

        // =========================
        // 自动更新链接显示（动态生成：每个IP一组，每组三行）
        // =========================
        private void UpdateLinks()
        {
            if (LinksPanel == null)
                return;

            if (!int.TryParse(PortBox.Text, out int port))
                return;

            string protocol = HttpsCheck.IsChecked == true ? "https" : "http";

            LinksPanel.Children.Clear();
            _firstUrl = null;

            var ips = _ips.Count > 0 ? _ips : new List<string> { "127.0.0.1" };

            // 每个IP一行网址，localhost 和 127.0.0.1 只列一次
            foreach (var ip in ips)
            {
                AddLinkRow($"{protocol}://{ip}:{port}");
            }
            AddLinkRow($"{protocol}://localhost:{port}");
            AddLinkRow($"{protocol}://127.0.0.1:{port}");
        }

        // 添加一行网址 + 复制按钮
        private void AddLinkRow(string url)
        {
            _firstUrl ??= url;

            var link = new System.Windows.Documents.Hyperlink
            {
                NavigateUri = new Uri(url)
            };
            link.Inlines.Add(url);
            link.Click += OpenLink_Click;

            var text = new System.Windows.Controls.TextBlock
            {
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            text.Inlines.Add(link);

            var copy = new System.Windows.Controls.Button
            {
                Content = "复制",
                FontSize = 11,
                Height = 22,
                Padding = new Thickness(6, 0, 6, 0),
                Margin = new Thickness(5, 0, 0, 0),
                Tag = url
            };
            copy.Click += CopyLink_Click;

            var grid = new System.Windows.Controls.Grid();
            grid.Children.Add(text);
            grid.Children.Add(copy);
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });
            System.Windows.Controls.Grid.SetColumn(copy, 1);

            LinksPanel.Children.Add(grid);
        }

        // =========================
        // 复制链接按钮
        // =========================
        private void CopyLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button btn ||
                btn.Tag is not string url)
                return;

            try
            {
                Clipboard.SetText(url);
                Log($"已复制链接: {url}");
            }
            catch (Exception ex)
            {
                Log($"复制失败: {ex.Message}");
            }
        }

        // =========================
        // HTML 文件勾选变化
        // =========================
        private void HtmlRadio_Checked(object sender, RoutedEventArgs e)
        {
            // RadioButton 勾选即选中 ListBoxItem，确保被勾选的才是服务器托管的首页
            if (sender is System.Windows.Controls.RadioButton radio &&
                radio.DataContext is string fileName)
            {
                Log($"已勾选首页文件: {fileName}");
            }
        }

        private void HtmlList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            AutoRestartServer("首页文件切换");
        }

        private void NetworkSettingChanged(object sender, RoutedEventArgs e)
        {
            UpdateLinks();
            AutoRestartServer("端口/证书设置变化");
        }
        
        // =========================
        // 根据输入自动补全IP
        // =========================
        private string? BuildFullIp(string input)
        {
            var localIp = SelectedIp;

            if (string.IsNullOrWhiteSpace(localIp))
                return null;

            var localParts = localIp.Split('.');
            var inputParts = input.Split('.');

            if (inputParts.Length == 0 || inputParts.Length > 4)
                return null;

            string[] result = new string[4];

            int prefixCount = 4 - inputParts.Length;

            // 用本机IP前缀补齐
            for (int i = 0; i < prefixCount; i++)
            {
                result[i] = localParts[i];
            }

            // 填入输入部分
            for (int i = 0; i < inputParts.Length; i++)
            {
                result[prefixCount + i] = inputParts[i];
            }

            return string.Join(".", result);
        }

    }
}