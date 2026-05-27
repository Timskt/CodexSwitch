# 31. WebView 与 JavaScript 互操作

> **写给零基础的你**：想象你的 Avalonia 桌面应用是一栋房子，WebView 就是房子里嵌入的一个"浏览器窗口"。通过这个窗口，你可以显示网页、运行 JavaScript 程序，甚至让网页里的按钮反过来控制你的桌面应用——就像你在房子里开了一个通往互联网的窗户，风（数据）可以双向吹进来。

## 31.1 概述

WebView 是一种在原生桌面应用中嵌入完整浏览器引擎的控件。它让你能够在 Avalonia 应用中直接渲染 HTML、CSS 和 JavaScript，无需用户打开外部浏览器。

### 为什么需要 WebView

在桌面开发中，有些场景使用原生控件实现起来非常复杂，但用 Web 技术却轻而易举：

- **嵌入第三方 Web 应用**：比如在桌面应用中嵌入管理后台、在线文档等
- **Markdown / 富文本预览**：Web 技术在排版和渲染方面有天然优势
- **地图组件**：高德、百度、Google Maps 等地图 SDK 都提供 JS 版本
- **图表和数据可视化**：ECharts、D3.js 等库在 Web 端功能强大
- **混合开发**：核心逻辑用 C#，界面用 HTML/CSS/JS，兼顾性能和开发效率
- **邮件 / HTML 渲染**：显示富 HTML 内容，如邮件正文、报表等

### 应用场景

```
┌─────────────────────────────────────────────┐
│           Avalonia 主窗口                    │
│  ┌──────────┐  ┌──────────────────────────┐ │
│  │ 导航菜单  │  │                          │ │
│  │          │  │      WebView 区域         │ │
│  │ - 首页   │  │   (渲染 HTML/JS/CSS)     │ │
│  │ - 设置   │  │                          │ │
│  │ - 帮助   │  │   可以加载：              │ │
│  │          │  │   - 本地 HTML 文件        │ │
│  │          │  │   - 远程 URL              │ │
│  │          │  │   - 动态生成的 HTML       │ │
│  └──────────┘  └──────────────────────────┘ │
└─────────────────────────────────────────────┘
```

### 典型使用场景

| 场景 | 说明 | 示例 |
|------|------|------|
| Markdown 预览 | 实时渲染 Markdown 为 HTML | 编辑器右侧预览面板 |
| 地图展示 | 嵌入在线地图服务 | 位置选择器、门店分布 |
| 数据可视化 | 使用 ECharts 等图表库 | 运维大屏、数据报表 |
| 富文本编辑器 | 嵌入 TinyMCE、Monaco 等 | 代码编辑器、文档编辑 |
| 第三方登录 | OAuth 授权页面 | GitHub/Google 登录 |
| 帮助文档 | 展示 HTML 格式文档 | 应用内帮助系统 |
| 打印报表 | HTML 模板生成报表后打印 | 发票、合同打印 |

## 31.2 WebView 方案对比

Avalonia 生态中有多种 WebView 方案可选，每种方案有不同的适用场景。

### 31.2.1 方案概览

#### 方案一：Avalonia.WebView（社区跨平台方案）

这是由社区维护的跨平台 WebView 封装，底层根据操作系统自动选择不同的浏览器引擎：
- Windows：WebView2（Edge Chromium）
- macOS：WKWebView（Safari/WebKit）
- Linux：WebKitGTK

```bash
# 安装核心包
dotnet add package Avalonia.WebView

# 安装平台特定包（按需）
dotnet add package Avalonia.WebView.Linux
dotnet add package Avalonia.WebView.Windows
dotnet add package Avalonia.WebView.MacOS
```

#### 方案二：WebView2（Windows 原生方案）

微软官方的 WebView2 控件，基于 Edge 浏览器引擎，仅支持 Windows。

```bash
dotnet add package Microsoft.Web.WebView2
```

#### 方案三：CefGlue / WebViewControl-Avalonia（CEF 方案）

基于 Chromium Embedded Framework（CEF）的方案，由 OutSystems 开源维护，跨平台支持良好。

```bash
# CefGlue 底层绑定
dotnet add package CefGlue

# Avalonia 封装（WebViewControl）
dotnet add package WebViewControl-Avalonia
```

#### 方案四：Photino.NET（超轻量方案）

基于操作系统原生 WebView 的超轻量封装，不需要额外下载 Chromium。

```bash
dotnet add package Photino.NET
```

### 31.2.2 方案对比表格

| 特性 | Avalonia.WebView | WebView2 | CefGlue/WebViewControl | Photino.NET |
|------|------------------|----------|------------------------|-------------|
| **跨平台** | Win/Mac/Linux | 仅 Windows | Win/Mac/Linux | Win/Mac/Linux |
| **底层引擎** | 各平台原生 | Edge Chromium | Chromium | 各平台原生 |
| **包体积** | 小 | 小（使用系统Edge） | 大（捆绑 Chromium） | 极小 |
| **JS Bridge** | 支持 | 支持 | 支持 | 有限 |
| **离线可用** | 需系统 WebView | 需 Edge 运行时 | 完全离线 | 需系统 WebView |
| **开发活跃度** | 社区维护 | 微软维护 | 社区维护 | 社区维护 |
| **适用场景** | 通用跨平台 | Windows 专用 | 需要完全控制 Chromium | 轻量嵌入 |
| **调试工具** | 依赖底层引擎 | Edge DevTools | Chrome DevTools | 依赖底层引擎 |

### 31.2.3 如何选择

```
需要 WebView 功能
    │
    ├── 仅需 Windows 支持？
    │       └── 是 → WebView2（微软官方，最省心）
    │
    ├── 需要跨平台 + 完全控制 Chromium？
    │       └── 是 → CefGlue / WebViewControl-Avalonia
    │
    ├── 需要跨平台 + 轻量？
    │       └── 是 → Avalonia.WebView（使用各平台原生引擎）
    │
    └── 只需要最简单的网页嵌入？
            └── 是 → Photino.NET
```

## 31.3 基础用法

### 31.3.1 Avalonia.WebView 安装和配置

**第一步：安装 NuGet 包**

```bash
# 核心包（必需）
dotnet add package Avalonia.WebView

# 根据目标平台安装对应包
dotnet add package Avalonia.WebView.Windows    # Windows
dotnet add package Avalonia.WebView.MacOS      # macOS
dotnet add package Avalonia.WebView.Linux      # Linux
```

**第二步：在 App.axaml.cs 中注册服务**

```csharp
using Avalonia.WebView;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 注册 WebView 服务（根据平台自动选择底层引擎）
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

**第三步：在 Program.cs 中初始化**

```csharp
using Avalonia;
using Avalonia.WebView;

class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseWebView()              // 启用 WebView 支持
            .LogToTrace();
}
```

### 31.3.2 在 XAML 中使用 WebView

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:wv="using:Avalonia.WebView"
        x:Class="MyApp.MainWindow"
        Title="WebView 示例"
        Width="1024" Height="768">

    <DockPanel>
        <!-- 顶部工具栏 -->
        <StackPanel DockPanel.Dock="Top"
                    Orientation="Horizontal"
                    Margin="8">
            <Button Name="BackBtn" Content="后退" Margin="4" />
            <Button Name="ForwardBtn" Content="前进" Margin="4" />
            <Button Name="RefreshBtn" Content="刷新" Margin="4" />
            <TextBox Name="UrlBox"
                     Width="400"
                     Watermark="输入网址..."
                     Margin="4" />
            <Button Name="GoBtn" Content="前往" Margin="4" />
        </StackPanel>

        <!-- WebView 主体 -->
        <wv:WebView Name="Browser"
                    Source="https://example.com" />
    </DockPanel>
</Window>
```

### 31.3.3 加载本地 HTML

**方式一：从字符串加载**

```csharp
using Avalonia.WebView;

public partial class MainWindow : Window
{
    private WebView _webView;

    public MainWindow()
    {
        InitializeComponent();
        _webView = this.FindControl<WebView>("Browser");
        LoadLocalHtml();
    }

    private void LoadLocalHtml()
    {
        string html = @"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                body {
                    font-family: 'Microsoft YaHei', sans-serif;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    height: 100vh;
                    margin: 0;
                }
                .card {
                    background: rgba(255,255,255,0.15);
                    backdrop-filter: blur(10px);
                    border-radius: 16px;
                    padding: 40px;
                    text-align: center;
                    box-shadow: 0 8px 32px rgba(0,0,0,0.2);
                }
                h1 { font-size: 2em; margin-bottom: 10px; }
                p { font-size: 1.2em; opacity: 0.9; }
            </style>
        </head>
        <body>
            <div class='card'>
                <h1>你好，Avalonia！</h1>
                <p>这是从字符串加载的本地 HTML</p>
            </div>
        </body>
        </html>";

        _webView.LoadHtml(html);
    }
}
```

**方式二：从文件加载**

```csharp
private async Task LoadHtmlFromFile()
{
    // 从应用资源目录加载
    string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    "Web", "index.html");
    string html = await File.ReadAllTextAsync(htmlPath);
    _webView.LoadHtml(html);
}

private async Task LoadHtmlFromEmbeddedResource()
{
    // 从嵌入资源加载
    var assembly = Assembly.GetExecutingAssembly();
    using var stream = assembly.GetManifestResourceStream("MyApp.Web.index.html");
    using var reader = new StreamReader(stream);
    string html = await reader.ReadToEndAsync();
    _webView.LoadHtml(html);
}
```

**方式三：从 Assets 目录加载**

```csharp
// 将 HTML 文件放在 Assets/Web/ 目录下，设置 Build Action 为 AvaloniaResource
private void LoadFromAssets()
{
    // 在 AXAML 中可以直接引用：
    // <wv:WebView Source="avares://MyApp/Assets/Web/index.html" />

    // 在代码中加载：
    var uri = new Uri("avares://MyApp/Assets/Web/index.html");
    var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();
    using var stream = assetLoader.Open(uri);
    using var reader = new StreamReader(stream);
    string html = reader.ReadToEnd();
    _webView.LoadHtml(html);
}
```

### 31.3.4 加载远程 URL

```csharp
// 直接导航到 URL
private void NavigateToUrl(string url)
{
    // 确保 URL 有协议前缀
    if (!url.StartsWith("http://") && !url.StartsWith("https://"))
    {
        url = "https://" + url;
    }

    _webView.Source = new Uri(url);
}

// 使用地址栏
private void OnGoClicked(object sender, RoutedEventArgs e)
{
    var url = UrlBox.Text?.Trim();
    if (!string.IsNullOrEmpty(url))
    {
        NavigateToUrl(url);
    }
}
```

### 31.3.5 基本属性和事件

```csharp
public partial class MainWindow : Window
{
    private WebView _webView;

    public MainWindow()
    {
        InitializeComponent();
        _webView = this.FindControl<WebView>("Browser");

        // 监听导航事件
        _webView.Navigated += OnNavigated;
        _webView.Navigating += OnNavigating;

        // 监听加载状态
        _webView.LoadCompleted += OnLoadCompleted;
    }

    private void OnNavigating(object sender, NavigatingEventArgs e)
    {
        // 导航开始前 —— 可以取消导航
        Console.WriteLine($"即将导航到: {e.Uri}");

        // 例如：阻止导航到特定域名
        if (e.Uri.Host.Contains("blocked-site.com"))
        {
            e.Cancel = true;
            Console.WriteLine("已阻止导航到受限网站");
        }
    }

    private void OnNavigated(object sender, NavigatedEventArgs e)
    {
        // 导航完成后
        Console.WriteLine($"已导航到: {e.Uri}");

        // 更新地址栏
        UrlBox.Text = e.Uri.ToString();
    }

    private void OnLoadCompleted(object sender, EventArgs e)
    {
        // 页面完全加载完成
        Console.WriteLine("页面加载完成");
    }
}
```

### 31.3.6 WebViewControl-Avalonia (CEF 方案) 基础用法

如果选择 CEF 方案，配置方式略有不同：

```csharp
// 在 Program.cs 中初始化
using WebViewControl;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 初始化 CEF
        WebView.Settings.OsrEnabled = false;   // 禁用离屏渲染（性能更好）
        WebView.Settings.LogSeverity = LogSeverity.Warning;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
```

```xml
<!-- 在 XAML 中使用 WebViewControl -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:wvc="using:WebViewControl"
        x:Class="MyApp.MainWindow">

    <wvc:WebView Name="Browser"
                 Address="https://example.com" />
</Window>
```

## 31.4 JavaScript 互操作（JS Bridge）

WebView 的核心价值在于 C# 和 JavaScript 之间的双向通信。

### 31.4.1 从 C# 调用 JavaScript 方法

**基础调用**

```csharp
// 调用简单的 JS 表达式
string title = await _webView.ExecuteScriptAsync("document.title");
Console.WriteLine($"页面标题: {title}");

// 调用 JS 函数并获取返回值
string json = await _webView.ExecuteScriptAsync(@"
    JSON.stringify({
        url: window.location.href,
        width: window.innerWidth,
        height: window.innerHeight,
        userAgent: navigator.userAgent
    })
");
Console.WriteLine($"页面信息: {json}");
```

**调用自定义 JavaScript 函数**

首先定义 HTML 页面中的 JS 函数：

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>JS Bridge 示例</title>
    <style>
        body { font-family: sans-serif; padding: 20px; }
        #output { background: #f0f0f0; padding: 10px; margin: 10px 0;
                  border-radius: 8px; min-height: 50px; }
        button { padding: 8px 16px; margin: 4px; cursor: pointer;
                 border: none; border-radius: 4px; background: #4a90d9;
                 color: white; }
        button:hover { background: #357abd; }
    </style>
</head>
<body>
    <h2>C# / JavaScript 通信示例</h2>
    <div id="output">等待操作...</div>
    <button onclick="callCSharp()">调用 C# 方法</button>
    <button onclick="sendDataToCSharp()">发送数据到 C#</button>

    <script>
        // 供 C# 调用的函数：显示消息
        function showMessage(text) {
            document.getElementById('output').innerText = 'C# 说: ' + text;
            return '消息已显示';
        }

        // 供 C# 调用的函数：计算
        function calculate(a, b) {
            const result = a + b;
            document.getElementById('output').innerText =
                `${a} + ${b} = ${result}`;
            return result;
        }

        // 供 C# 调用的函数：获取页面数据
        function getPageData() {
            return JSON.stringify({
                title: document.title,
                elementCount: document.querySelectorAll('*').length,
                scrollPosition: { x: window.scrollX, y: window.scrollY }
            });
        }

        // 从 JS 调用 C# 方法
        function callCSharp() {
            // 方式一：通过 window.chrome.webview（WebView2）
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify({
                    action: 'greet',
                    data: { name: 'JavaScript', time: new Date().toISOString() }
                }));
            }
            // 方式二：通过自定义桥接对象
            else if (window.CSharpBridge) {
                window.CSharpBridge.OnGreet('JavaScript');
            }
        }

        // 发送复杂数据到 C#
        function sendDataToCSharp() {
            const data = {
                action: 'updateData',
                payload: {
                    items: [1, 2, 3, 4, 5],
                    metadata: { source: 'WebView', version: '1.0' }
                }
            };

            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify(data));
            }
        }
    </script>
</body>
</html>
```

然后在 C# 中调用这些函数：

```csharp
public partial class MainWindow : Window
{
    private WebView _webView;

    public MainWindow()
    {
        InitializeComponent();
        _webView = this.FindControl<WebView>("Browser");
    }

    // 调用 JS 的 showMessage 函数
    private async Task CallShowMessage()
    {
        string result = await _webView.ExecuteScriptAsync(
            "showMessage('来自 C# 的问候！')");
        Console.WriteLine($"JS 返回: {result}");
    }

    // 调用 JS 的 calculate 函数，传递参数
    private async Task CallCalculate()
    {
        // 注意：参数需要正确转义
        int a = 42, b = 58;
        string result = await _webView.ExecuteScriptAsync(
            $"calculate({a}, {b})");
        Console.WriteLine($"计算结果: {result}");
    }

    // 调用 JS 函数并解析返回的 JSON
    private async Task CallGetPageData()
    {
        string json = await _webView.ExecuteScriptAsync("getPageData()");
        var pageData = JsonSerializer.Deserialize<PageData>(json);
        Console.WriteLine($"标题: {pageData.Title}");
        Console.WriteLine($"元素数量: {pageData.ElementCount}");
    }
}

// 数据模型
public class PageData
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("elementCount")]
    public int ElementCount { get; set; }

    [JsonPropertyName("scrollPosition")]
    public ScrollPosition ScrollPosition { get; set; }
}

public class ScrollPosition
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}
```

### 31.4.2 从 JavaScript 调用 C# 方法

**方式一：使用 WebMessageReceived（WebView2 方案）**

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetupJsBridge();
    }

    private void SetupJsBridge()
    {
        // 注册消息接收事件
        _webView.WebMessageReceived += OnWebMessageReceived;
    }

    private void OnWebMessageReceived(object sender, WebMessageReceivedEventArgs e)
    {
        // 解析来自 JavaScript 的消息
        string message = e.Message;
        Console.WriteLine($"收到 JS 消息: {message}");

        try
        {
            var jsonDoc = JsonDocument.Parse(message);
            var root = jsonDoc.RootElement;
            string action = root.GetProperty("action").GetString();

            switch (action)
            {
                case "greet":
                    var name = root.GetProperty("data")
                                   .GetProperty("name").GetString();
                    HandleGreet(name);
                    break;

                case "updateData":
                    var payload = root.GetProperty("payload");
                    HandleUpdateData(payload);
                    break;

                case "requestConfig":
                    SendConfigToJs();
                    break;

                default:
                    Console.WriteLine($"未知操作: {action}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析消息失败: {ex.Message}");
        }
    }

    private void HandleGreet(string name)
    {
        Console.WriteLine收到来自 {name} 的问候");
        // 可以回复 JS
        _ = _webView.ExecuteScriptAsync(
            $"showMessage('C# 已收到你的问候，{name}！')");
    }

    private void SendConfigToJs()
    {
        var config = new
        {
            theme = "dark",
            language = "zh-CN",
            version = "1.0.0"
        };
        string json = JsonSerializer.Serialize(config);
        _ = _webView.ExecuteScriptAsync(
            $"window.receiveConfig({json})");
    }
}
```

**方式二：使用 AddHostObjectToScript（高级方式）**

```csharp
// 定义要暴露给 JavaScript 的 C# 类
[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
public class CSharpBridge
{
    private readonly MainWindow _window;

    public CSharpBridge(MainWindow window)
    {
        _window = window;
    }

    // JS 可以直接调用这个方法
    public string OnGreet(string name)
    {
        Console.WriteLine($"JS 调用了 OnGreet: {name}");
        return $"C# 收到了来自 {name} 的问候";
    }

    // 传递复杂数据
    public string ProcessData(string jsonData)
    {
        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
            // 处理数据...
            return JsonSerializer.Serialize(new { success = true, message = "处理完成" });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    // 异步操作
    public async Task<string> FetchDataAsync(string url)
    {
        using var client = new HttpClient();
        var response = await client.GetStringAsync(url);
        return response;
    }
}

// 在初始化时注册桥接对象
private void SetupHostObject()
{
    var bridge = new CSharpBridge(this);
    // 将对象暴露给 JavaScript，JS 中通过 window.CSharpBridge 访问
    _webView.AddHostObjectToScript("CSharpBridge", bridge);
}
```

**方式三：自定义 JavaScript 注入（通用方案）**

这种方式在所有 WebView 方案中都能工作：

```csharp
public class JsBridgeManager
{
    private readonly WebView _webView;
    private readonly Dictionary<string, Func<string, Task<string>>> _handlers = new();

    public JsBridgeManager(WebView webView)
    {
        _webView = webView;
        InitializeBridge();
    }

    private async void InitializeBridge()
    {
        // 注入桥接脚本
        string bridgeScript = @"
        (function() {
            // 创建全局桥接对象
            window.CSharpBridge = {
                _callbacks: {},
                _nextId: 0,

                // 调用 C# 方法
                invoke: function(method, args) {
                    return new Promise((resolve, reject) => {
                        const id = ++this._nextId;
                        this._callbacks[id] = { resolve, reject };

                        // 通过 URL scheme 或 postMessage 传递
                        const message = JSON.stringify({
                            id: id,
                            method: method,
                            args: args
                        });

                        // 使用 postMessage
                        if (window.chrome && window.chrome.webview) {
                            window.chrome.webview.postMessage(message);
                        }

                        // 超时处理
                        setTimeout(() => {
                            if (this._callbacks[id]) {
                                reject(new Error('调用超时'));
                                delete this._callbacks[id];
                            }
                        }, 30000);
                    });
                },

                // C# 调用此方法返回结果
                _resolve: function(id, result) {
                    if (this._callbacks[id]) {
                        this._callbacks[id].resolve(result);
                        delete this._callbacks[id];
                    }
                },

                _reject: function(id, error) {
                    if (this._callbacks[id]) {
                        this._callbacks[id].reject(new Error(error));
                        delete this._callbacks[id];
                    }
                }
            };

            console.log('[Bridge] C# 桥接已就绪');
        })();
        ";

        await _webView.ExecuteScriptAsync(bridgeScript);
    }

    // 注册 C# 方法处理器
    public void RegisterHandler(string method, Func<string, Task<string>> handler)
    {
        _handlers[method] = handler;
    }

    // 处理来自 JS 的调用
    public async Task HandleMessage(string message)
    {
        var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;
        int id = root.GetProperty("id").GetInt32();
        string method = root.GetProperty("method").GetString();
        string args = root.GetProperty("args").GetRawText();

        try
        {
            if (_handlers.TryGetValue(method, out var handler))
            {
                string result = await handler(args);
                await _webView.ExecuteScriptAsync(
                    $"window.CSharpBridge._resolve({id}, {result})");
            }
            else
            {
                await _webView.ExecuteScriptAsync(
                    $"window.CSharpBridge._reject({id}, '方法未注册: {method}')");
            }
        }
        catch (Exception ex)
        {
            await _webView.ExecuteScriptAsync(
                $"window.CSharpBridge._reject({id}, '{ex.Message}')");
        }
    }
}
```

### 31.4.3 数据传递（JSON 序列化）

在 C# 和 JavaScript 之间传递复杂数据时，JSON 是通用格式。

```csharp
// C# 侧：发送复杂对象到 JS
public async Task SendComplexDataToJs()
{
    var data = new UserProfile
    {
        Id = 1,
        Name = "张三",
        Email = "zhangsan@example.com",
        Roles = new[] { "admin", "editor" },
        Settings = new UserSettings
        {
            Theme = "dark",
            Language = "zh-CN",
            Notifications = true
        }
    };

    string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    });

    // 将 JSON 传给 JS 函数
    await _webView.ExecuteScriptAsync($"window.receiveProfile({json})");
}

// C# 侧：接收 JS 发送的复杂数据
private void HandleJsData(string jsonData)
{
    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    var formData = JsonSerializer.Deserialize<FormData>(jsonData, options);

    // 使用数据...
    Console.WriteLine($"表单: {formData.Title}");
    foreach (var field in formData.Fields)
    {
        Console.WriteLine($"  {field.Name}: {field.Value}");
    }
}

// 数据模型
public class UserProfile
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string[] Roles { get; set; }
    public UserSettings Settings { get; set; }
}

public class UserSettings
{
    public string Theme { get; set; }
    public string Language { get; set; }
    public bool Notifications { get; set; }
}

public class FormData
{
    public string Title { get; set; }
    public List<FormField> Fields { get; set; }
}

public class FormField
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Type { get; set; }
}
```

JavaScript 侧对应的数据处理：

```html
<script>
    // 接收 C# 发送的数据
    function receiveProfile(profile) {
        console.log('收到用户资料:', profile);

        document.getElementById('username').textContent = profile.name;
        document.getElementById('email').textContent = profile.email;

        // 应用主题
        if (profile.settings.theme === 'dark') {
            document.body.classList.add('dark-theme');
        }
    }

    // 发送表单数据到 C#
    function submitForm() {
        const form = {
            title: document.getElementById('formTitle').value,
            fields: []
        };

        document.querySelectorAll('.form-field').forEach(field => {
            form.fields.push({
                name: field.dataset.name,
                value: field.value,
                type: field.type
            });
        });

        // 通过桥接发送
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(JSON.stringify(form));
        }
    }

    // 日期等特殊类型的序列化处理
    function serializeWithDates(obj) {
        return JSON.stringify(obj, (key, value) => {
            if (value instanceof Date) {
                return { __type: 'Date', value: value.toISOString() };
            }
            return value;
        });
    }

    // 反序列化时恢复日期
    function deserializeWithDates(json) {
        return JSON.parse(json, (key, value) => {
            if (value && value.__type === 'Date') {
                return new Date(value.value);
            }
            return value;
        });
    }
</script>
```

### 31.4.4 异步通信模式

在 WebView 中，C# 调用 JS 和 JS 调用 C# 本质上都是异步的。

```csharp
// 异步调用模式 1：简单的 await
private async Task<string> GetJsResult()
{
    string result = await _webView.ExecuteScriptAsync("fetchDataFromApi()");
    return result;
}

// 异步调用模式 2：带超时的调用
private async Task<string> GetJsResultWithTimeout(int timeoutMs = 5000)
{
    using var cts = new CancellationTokenSource(timeoutMs);

    var task = _webView.ExecuteScriptAsync("longRunningOperation()");
    var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMs));

    if (completedTask == task)
    {
        return await task;
    }
    else
    {
        throw new TimeoutException("JavaScript 调用超时");
    }
}

// 异步调用模式 3：进度报告
private async Task RunWithProgress()
{
    // 先设置进度回调
    await _webView.ExecuteScriptAsync(@"
        window.reportProgress = function(percent, message) {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify({
                    type: 'progress',
                    percent: percent,
                    message: message
                }));
            }
        };
    ");

    // 启动长时间任务
    await _webView.ExecuteScriptAsync(@"
        (async function() {
            const total = 100;
            for (let i = 0; i <= total; i++) {
                await new Promise(r => setTimeout(r, 50));
                window.reportProgress(i, `处理中... ${i}%`);
            }
            window.reportProgress(100, '完成！');
        })();
    ");
}

// 异步调用模式 4：轮询结果
private async Task<T> PollForResult<T>(string checkFunction, int intervalMs = 100)
{
    while (true)
    {
        string json = await _webView.ExecuteScriptAsync(checkFunction);
        var result = JsonSerializer.Deserialize<PollResult<T>>(json);

        if (result.IsComplete)
        {
            return result.Data;
        }

        await Task.Delay(intervalMs);
    }
}
```

### 31.4.5 错误处理

```csharp
public class WebViewErrorHandler
{
    private readonly WebView _webView;

    public WebViewErrorHandler(WebView webView)
    {
        _webView = webView;
    }

    // 安全地执行 JS，捕获异常
    public async Task<JsResult<T>> SafeExecuteAsync<T>(string script)
    {
        try
        {
            // 用 try-catch 包裹 JS 执行
            string wrappedScript = $@"
                (function() {{
                    try {{
                        const result = eval({JsonSerializer.Serialize(script)});
                        return JSON.stringify({{ success: true, data: result }});
                    }} catch (e) {{
                        return JSON.stringify({{
                            success: false,
                            error: e.message,
                            stack: e.stack
                        }});
                    }}
                }})()
            ";

            string json = await _webView.ExecuteScriptAsync(wrappedScript);
            var jsResult = JsonSerializer.Deserialize<JsResult<T>>(json);

            if (!jsResult.Success)
            {
                Console.WriteLine($"JS 执行错误: {jsResult.Error}");
                Console.WriteLine($"堆栈: {jsResult.Stack}");
            }

            return jsResult;
        }
        catch (Exception ex)
        {
            // C# 侧的异常（如 WebView 未初始化）
            return new JsResult<T>
            {
                Success = false,
                Error = $"C# 异常: {ex.Message}"
            };
        }
    }

    // 注入全局错误处理器
    public async Task SetupGlobalErrorHandler()
    {
        await _webView.ExecuteScriptAsync(@"
            // 捕获未处理的错误
            window.onerror = function(message, source, lineno, colno, error) {
                if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage(JSON.stringify({
                        type: 'error',
                        message: message,
                        source: source,
                        line: lineno,
                        column: colno,
                        stack: error ? error.stack : null
                    }));
                }
                return true; // 阻止默认错误处理
            };

            // 捕获未处理的 Promise 拒绝
            window.addEventListener('unhandledrejection', function(event) {
                if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage(JSON.stringify({
                        type: 'unhandledRejection',
                        reason: event.reason ? event.reason.toString() : 'Unknown'
                    }));
                }
            });

            console.log('[Error] 全局错误处理器已注册');
        ");
    }
}

// 结果包装类
public class JsResult<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T Data { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("stack")]
    public string Stack { get; set; }
}
```

## 31.5 高级用法

### 31.5.1 自定义 HTTP 请求拦截

拦截 WebView 中的 HTTP 请求，可以实现自定义协议、资源替换等功能。

```csharp
public class RequestInterceptor
{
    private readonly WebView _webView;

    public RequestInterceptor(WebView webView)
    {
        _webView = webView;
    }

    // WebView2 方式：拦截请求
    public void SetupWebView2Interception()
    {
        // 需要在 WebView 初始化后调用
        var coreWebView = _webView.CoreWebView2;

        // 添加自定义资源过滤器
        coreWebView.AddWebResourceRequestedFilter(
            "*",
            CoreWebView2WebResourceContext.All);

        coreWebView.WebResourceRequested += (s, e) =>
        {
            var uri = e.Request.Uri;
            Console.WriteLine($"请求: {uri}");

            // 拦截特定请求
            if (uri.Contains("/api/local/"))
            {
                // 本地 API 拦截 —— 返回自定义响应
                string path = new Uri(uri).AbsolutePath;
                string responseBody = HandleLocalApi(path);

                var response = coreWebView.Environment.CreateWebResourceResponse(
                    responseBody,
                    200,
                    "OK",
                    "Content-Type: application/json");

                e.Response = response;
            }

            // 替换图片资源
            if (uri.EndsWith(".png") && uri.Contains("/images/"))
            {
                // 替换为本地图标
                string localImagePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Assets", "replaced-icon.png");

                if (File.Exists(localImagePath))
                {
                    var stream = File.OpenRead(localImagePath);
                    var response = coreWebView.Environment.CreateWebResourceResponse(
                        stream,
                        200,
                        "OK",
                        "Content-Type: image/png");
                    e.Response = response;
                }
            }
        };
    }

    private string HandleLocalApi(string path)
    {
        return path switch
        {
            "/api/local/config" => JsonSerializer.Serialize(new
            {
                theme = "dark",
                version = "1.0"
            }),
            "/api/local/status" => JsonSerializer.Serialize(new
            {
                status = "running",
                uptime = 3600
            }),
            _ => JsonSerializer.Serialize(new { error = "Not Found" })
        };
    }
}
```

### 31.5.2 Cookie 管理

```csharp
public class CookieManager
{
    private readonly WebView _webView;

    public CookieManager(WebView webView)
    {
        _webView = webView;
    }

    // WebView2 方式
    public async Task SetCookie(string name, string value, string domain)
    {
        var cookieManager = _webView.CoreWebView2.CookieManager;

        var cookie = cookieManager.CreateCookie(name, value, domain, "/");
        cookie.IsHttpOnly = true;
        cookie.IsSecure = true;
        cookie.Expires = DateTime.Now.AddDays(30);

        cookieManager.AddOrUpdateCookie(cookie);
    }

    public async Task<string> GetCookie(string name, string url)
    {
        var cookieManager = _webView.CoreWebView2.CookieManager;
        var cookies = await cookieManager.GetCookiesAsync(url);

        var cookie = cookies.FirstOrDefault(c => c.Name == name);
        return cookie?.Value;
    }

    public async Task<List<CookieInfo>> GetAllCookies(string url)
    {
        var cookieManager = _webView.CoreWebView2.CookieManager;
        var cookies = await cookieManager.GetCookiesAsync(url);

        return cookies.Select(c => new CookieInfo
        {
            Name = c.Name,
            Value = c.Value,
            Domain = c.Domain,
            Path = c.Path,
            Expires = c.Expires,
            IsHttpOnly = c.IsHttpOnly,
            IsSecure = c.IsSecure
        }).ToList();
    }

    public void DeleteAllCookies()
    {
        _webView.CoreWebView2.CookieManager.DeleteAllCookies();
    }
}

public class CookieInfo
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Domain { get; set; }
    public string Path { get; set; }
    public DateTime Expires { get; set; }
    public bool IsHttpOnly { get; set; }
    public bool IsSecure { get; set; }
}
```

### 31.5.3 本地资源注入

在页面加载前或加载后注入 CSS 和 JavaScript。

```csharp
public class ResourceInjector
{
    private readonly WebView _webView;

    public ResourceInjector(WebView webView)
    {
        _webView = webView;
    }

    // 注入自定义 CSS
    public async Task InjectCss(string css)
    {
        string script = $@"
            (function() {{
                const style = document.createElement('style');
                style.textContent = {JsonSerializer.Serialize(css)};
                document.head.appendChild(style);
            }})();
        ";
        await _webView.ExecuteScriptAsync(script);
    }

    // 注入外部 CSS 文件
    public async Task InjectCssFile(string url)
    {
        string script = $@"
            (function() {{
                const link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = '{url}';
                document.head.appendChild(link);
            }})();
        ";
        await _webView.ExecuteScriptAsync(script);
    }

    // 注入 JavaScript 文件
    public async Task InjectScript(string scriptUrl)
    {
        string script = $@"
            new Promise((resolve, reject) => {{
                const script = document.createElement('script');
                script.src = '{scriptUrl}';
                script.onload = resolve;
                script.onerror = reject;
                document.head.appendChild(script);
            }});
        ";
        await _webView.ExecuteScriptAsync(script);
    }

    // 注入内联 JavaScript
    public async Task InjectInlineScript(string jsCode)
    {
        string script = $@"
            (function() {{
                const script = document.createElement('script');
                script.textContent = {JsonSerializer.Serialize(jsCode)};
                document.head.appendChild(script);
            }})();
        ";
        await _webView.ExecuteScriptAsync(script);
    }

    // 在页面加载前注入（使用 AddScriptToExecuteOnDocumentCreated）
    public void InjectBeforeLoad(string jsCode)
    {
        // WebView2 方式
        _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreated(jsCode);
    }
}

// 使用示例
public partial class MainWindow : Window
{
    private async void OnPageLoaded(object sender, EventArgs e)
    {
        var injector = new ResourceInjector(_webView);

        // 注入暗色主题覆盖样式
        await injector.InjectCss(@"
            body {
                background-color: #1e1e1e !important;
                color: #d4d4d4 !important;
            }
            a { color: #569cd6 !important; }
        ");

        // 注入自定义工具栏
        await injector.InjectInlineScript(@"
            const toolbar = document.createElement('div');
            toolbar.style.cssText = 'position:fixed;top:0;left:0;right:0;' +
                'background:#2d2d2d;padding:8px;z-index:99999;' +
                'display:flex;gap:8px;';
            toolbar.innerHTML = `
                <button onclick='history.back()'>返回</button>
                <button onclick='history.forward()'>前进</button>
                <button onclick='location.reload()'>刷新</button>
            `;
            document.body.prepend(toolbar);
        ");
    }
}
```

### 31.5.4 开发者工具集成

```csharp
public class DevToolsManager
{
    private readonly WebView _webView;

    public DevToolsManager(WebView webView)
    {
        _webView = webView;
    }

    // 打开开发者工具
    public void OpenDevTools()
    {
        // WebView2 方式
        _webView.CoreWebView2.OpenDevToolsWindow();
    }

    // 通过 JS 打开（部分引擎支持）
    public async Task OpenDevToolsViaJs()
    {
        await _webView.ExecuteScriptAsync("debugger;"); // 触发断点
    }

    // 设置控制台消息转发到 C#
    public async Task SetupConsoleForward()
    {
        await _webView.ExecuteScriptAsync(@"
            (function() {
                const originalConsole = {};
                ['log', 'warn', 'error', 'info'].forEach(method => {
                    originalConsole[method] = console[method];
                    console[method] = function(...args) {
                        // 调用原始方法
                        originalConsole[method].apply(console, args);

                        // 转发到 C#
                        if (window.chrome && window.chrome.webview) {
                            window.chrome.webview.postMessage(JSON.stringify({
                                type: 'console',
                                level: method,
                                message: args.map(a =>
                                    typeof a === 'object' ? JSON.stringify(a) : String(a)
                                ).join(' '),
                                timestamp: new Date().toISOString()
                            }));
                        }
                    };
                });

                console.log('[DevTools] 控制台转发已启用');
            })();
        ");
    }
}
```

### 31.5.5 打印和导出 PDF

```csharp
public class PrintManager
{
    private readonly WebView _webView;

    public PrintManager(WebView webView)
    {
        _webView = webView;
    }

    // 打印页面
    public async Task Print()
    {
        await _webView.ExecuteScriptAsync("window.print()");
    }

    // 导出为 PDF（WebView2 方式）
    public async Task ExportToPdf(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create);
        await _webView.CoreWebView2.PrintToPdfAsync(stream, new CoreWebView2PrintSettings
        {
            Orientation = CoreWebView2PrintOrientation.Portrait,
            ScaleFactor = 1.0,
            PageWidth = 210.0,   // A4 宽度（mm）
            PageHeight = 297.0,  // A4 高度（mm）
            MarginTop = 20.0,
            MarginBottom = 20.0,
            MarginLeft = 15.0,
            MarginRight = 15.0,
            ShouldPrintBackgrounds = true,
            HeaderTitle = "导出文档",
            FooterUri = "页码: &P"
        });

        Console.WriteLine($"PDF 已导出到: {filePath}");
    }

    // 通过 JS 生成打印内容并打印
    public async Task PrintCustomContent(string html)
    {
        string printScript = $@"
            (function() {{
                const printWindow = window.open('', '_blank');
                printWindow.document.write({JsonSerializer.Serialize(html)});
                printWindow.document.close();
                printWindow.onload = function() {{
                    printWindow.print();
                    printWindow.close();
                }};
            }})();
        ";
        await _webView.ExecuteScriptAsync(printScript);
    }
}
```

## 31.6 实战：混合应用架构

### 31.6.1 C# 主窗口 + WebView 子页面

构建一个典型的混合应用：主窗口使用 Avalonia 原生控件，内容区域使用 WebView 渲染。

```xml
<!-- MainWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:wv="using:Avalonia.WebView"
        x:Class="MyApp.MainWindow"
        Title="混合应用示例"
        Width="1200" Height="800">

    <DockPanel>
        <!-- 左侧导航栏（Avalonia 原生控件） -->
        <Border DockPanel.Dock="Left"
                Width="220"
                Background="#2d2d30"
                Padding="0">
            <StackPanel>
                <TextBlock Text="我的应用"
                           FontSize="18"
                           FontWeight="Bold"
                           Foreground="White"
                           Margin="16,20,16,16" />

                <ListBox Name="NavList"
                         Background="Transparent"
                         SelectedIndex="0">
                    <ListBoxItem>
                        <StackPanel Orientation="Horizontal" Margin="8">
                            <PathIcon Data="{StaticResource HomeIcon}"
                                      Width="16" Height="16"
                                      Foreground="#cccccc" />
                            <TextBlock Text="首页" Margin="12,0,0,0"
                                       Foreground="#cccccc" />
                        </StackPanel>
                    </ListBoxItem>
                    <ListBoxItem>
                        <StackPanel Orientation="Horizontal" Margin="8">
                            <PathIcon Data="{StaticResource ChartIcon}"
                                      Width="16" Height="16"
                                      Foreground="#cccccc" />
                            <TextBlock Text="数据看板" Margin="12,0,0,0"
                                       Foreground="#cccccc" />
                        </StackPanel>
                    </ListBoxItem>
                    <ListBoxItem>
                        <StackPanel Orientation="Horizontal" Margin="8">
                            <PathIcon Data="{StaticResource SettingsIcon}"
                                      Width="16" Height="16"
                                      Foreground="#cccccc" />
                            <TextBlock Text="设置" Margin="12,0,0,0"
                                       Foreground="#cccccc" />
                        </StackPanel>
                    </ListBoxItem>
                </ListBox>
            </StackPanel>
        </Border>

        <!-- 右侧内容区域（WebView） -->
        <Panel>
            <wv:WebView Name="ContentWebView" />

            <!-- 加载指示器 -->
            <Border Name="LoadingOverlay"
                    Background="#80000000"
                    IsVisible="False">
                <StackPanel HorizontalAlignment="Center"
                            VerticalAlignment="Center">
                    <ProgressBar IsIndeterminate="True"
                                 Width="200" />
                    <TextBlock Text="加载中..."
                               Foreground="White"
                               Margin="0,8,0,0" />
                </StackPanel>
            </Border>
        </Panel>

        <!-- 底部状态栏（Avalonia 原生） -->
        <Border DockPanel.Dock="Bottom"
                Height="28"
                Background="#007acc"
                Padding="8,4">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="就绪"
                           Foreground="White"
                           FontSize="12"
                           Name="StatusText" />
                <TextBlock Text="|"
                           Foreground="#80ffffff"
                           Margin="12,0" />
                <TextBlock Text="版本 1.0.0"
                           Foreground="#b0d4f1"
                           FontSize="12" />
            </StackPanel>
        </Border>
    </DockPanel>
</Window>
```

```csharp
// MainWindow.axaml.cs
public partial class MainWindow : Window
{
    private WebView _webView;
    private Dictionary<int, string> _pageMap;

    public MainWindow()
    {
        InitializeComponent();

        _webView = this.FindControl<WebView>("ContentWebView");
        var navList = this.FindControl<ListBox>("NavList");
        var loadingOverlay = this.FindControl<Border>("LoadingOverlay");
        var statusText = this.FindControl<TextBlock>("StatusText");

        // 页面路由映射
        _pageMap = new Dictionary<int, string>
        {
            [0] = "avares://MyApp/Web/home.html",
            [1] = "avares://MyApp/Web/dashboard.html",
            [2] = "avares://MyApp/Web/settings.html"
        };

        // 导航切换
        navList.SelectionChanged += (s, e) =>
        {
            if (navList.SelectedIndex >= 0 &&
                _pageMap.TryGetValue(navList.SelectedIndex, out var url))
            {
                _webView.Source = new Uri(url);
            }
        };

        // 加载状态
        _webView.Navigating += (s, e) =>
        {
            loadingOverlay.IsVisible = true;
            statusText.Text = $"加载中: {e.Uri}";
        };

        _webView.LoadCompleted += (s, e) =>
        {
            loadingOverlay.IsVisible = false;
            statusText.Text = "就绪";

            // 注入桥接脚本
            SetupBridge();
        };

        // 默认加载首页
        _webView.Source = new Uri(_pageMap[0]);
    }

    private async void SetupBridge()
    {
        // 注入导航函数供 JS 调用
        await _webView.ExecuteScriptAsync(@"
            window.avaloniaApp = {
                navigate: function(page) {
                    if (window.chrome && window.chrome.webview) {
                        window.chrome.webview.postMessage(JSON.stringify({
                            action: 'navigate',
                            page: page
                        }));
                    }
                },
                updateStatus: function(text) {
                    if (window.chrome && window.chrome.webview) {
                        window.chrome.webview.postMessage(JSON.stringify({
                            action: 'status',
                            text: text
                        }));
                    }
                }
            };
        ");
    }
}
```

### 31.6.2 WebView 中的 Avalonia 控件通信

实现 Avalonia 原生控件与 WebView 内容的双向数据同步。

```csharp
// ViewModel 层：管理双向数据同步
public class HybridViewModel : INotifyPropertyChanged
{
    private readonly WebView _webView;
    private string _searchText;
    private string _selectedTheme;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                // Avalonia 输入变化 -> 同步到 WebView
                SyncToWebView("searchText", value);
            }
        }
    }

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                SyncToWebView("theme", value);
            }
        }
    }

    public HybridViewModel(WebView webView)
    {
        _webView = webView;
    }

    // 将数据从 Avalonia 同步到 WebView
    private async void SyncToWebView(string key, object value)
    {
        string json = JsonSerializer.Serialize(value);
        await _webView.ExecuteScriptAsync(
            $"window.avaloniaSync && window.avaloniaSync.update('{key}', {json})");
    }

    // 从 WebView 接收数据更新
    public void HandleWebViewUpdate(string key, JsonElement value)
    {
        switch (key)
        {
            case "searchText":
                SearchText = value.GetString();
                break;
            case "theme":
                SelectedTheme = value.GetString();
                break;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
```

对应的 JavaScript 同步脚本：

```html
<script>
    // 双向同步管理器
    window.avaloniaSync = {
        _handlers: {},

        // 注册变更处理器
        on: function(key, callback) {
            if (!this._handlers[key]) this._handlers[key] = [];
            this._handlers[key].push(callback);
        },

        // 从 Avalonia 接收更新
        update: function(key, value) {
            console.log(`[Sync] 收到更新: ${key} =`, value);

            // 触发处理器
            if (this._handlers[key]) {
                this._handlers[key].forEach(cb => cb(value));
            }

            // 更新 UI
            this._updateUI(key, value);
        },

        // 发送更新到 Avalonia
        send: function(key, value) {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify({
                    action: 'sync',
                    key: key,
                    value: value
                }));
            }
        },

        _updateUI: function(key, value) {
            const el = document.querySelector(`[data-sync="${key}"]`);
            if (el) {
                if (el.tagName === 'INPUT') {
                    el.value = value;
                } else {
                    el.textContent = value;
                }
            }
        }
    };

    // 监听输入变化，自动同步到 Avalonia
    document.querySelectorAll('[data-sync]').forEach(el => {
        el.addEventListener('input', function() {
            const key = this.dataset.sync;
            window.avaloniaSync.send(key, this.value);
        });
    });
</script>
```

### 31.6.3 导航拦截和路由

```csharp
public class NavigationRouter
{
    private readonly WebView _webView;
    private readonly Dictionary<string, Func<Task>> _routes = new();
    private readonly List<string> _allowedDomains;
    private readonly Stack<string> _history = new();
    private string _currentPage;

    public NavigationRouter(WebView webView, string[] allowedDomains = null)
    {
        _webView = webView;
        _allowedDomains = allowedDomains?.ToList() ?? new List<string>();

        // 监听导航事件
        _webView.Navigating += OnNavigating;
    }

    // 注册路由
    public void RegisterRoute(string path, Func<Task> handler)
    {
        _routes[path] = handler;
    }

    // 导航拦截
    private async void OnNavigating(object sender, NavigatingEventArgs e)
    {
        var uri = e.Uri;

        // 检查域名白名单
        if (_allowedDomains.Count > 0 &&
            !_allowedDomains.Contains(uri.Host))
        {
            e.Cancel = true;

            // 通知 JS 导航被阻止
            await _webView.ExecuteScriptAsync($@"
                if (window.onNavigationBlocked) {{
                    window.onNavigationBlocked('{uri}');
                }}
            ");
            return;
        }

        // 处理自定义路由
        string path = uri.AbsolutePath;
        if (_routes.TryGetValue(path, out var handler))
        {
            e.Cancel = true;
            _history.Push(_currentPage);
            _currentPage = path;
            await handler();
        }
    }

    // 前进后退支持
    public bool CanGoBack => _history.Count > 0;

    public async Task GoBack()
    {
        if (CanGoBack)
        {
            var previous = _history.Pop();
            _currentPage = previous;
            await _webView.ExecuteScriptAsync(
                $"window.location.hash = '{previous}'");
        }
    }

    // 注入路由管理器到 JS
    public async Task InitializeJsRouter()
    {
        await _webView.ExecuteScriptAsync(@"
            window.appRouter = {
                routes: {},
                currentRoute: '/',

                register: function(path, handler) {
                    this.routes[path] = handler;
                },

                navigate: function(path) {
                    if (this.routes[path]) {
                        this.currentRoute = path;
                        this.routes[path]();
                    }
                    // 通知 C# 侧
                    if (window.chrome && window.chrome.webview) {
                        window.chrome.webview.postMessage(JSON.stringify({
                            action: 'routeChange',
                            path: path
                        }));
                    }
                }
            };
        ");
    }
}
```

## 31.7 安全考虑

### 31.7.1 XSS 防护

```csharp
public class SecurityManager
{
    private readonly WebView _webView;

    public SecurityManager(WebView webView)
    {
        _webView = webView;
    }

    // 注入 XSS 防护脚本
    public async Task SetupXssProtection()
    {
        await _webView.ExecuteScriptAsync(@"
            (function() {
                // 1. 拦截 innerHTML 设置
                const originalInnerHTML = Object.getOwnPropertyDescriptor(
                    Element.prototype, 'innerHTML');
                Object.defineProperty(Element.prototype, 'innerHTML', {
                    set: function(value) {
                        // 使用 DOMPurify 清理（需要先引入 DOMPurify）
                        if (window.DOMPurify) {
                            value = DOMPurify.sanitize(value);
                        }
                        originalInnerHTML.set.call(this, value);
                    },
                    get: originalInnerHTML.get
                });

                // 2. 阻止 eval
                window.eval = function() {
                    console.warn('[Security] eval() 已被禁用');
                    throw new Error('eval() is disabled for security reasons');
                };

                // 3. 阻止内联事件处理器
                document.addEventListener('beforeunload', function() {});

                // 4. 过滤危险的 URL scheme
                const originalOpen = window.open;
                window.open = function(url, ...args) {
                    if (url && /^(javascript|data|vbscript):/i.test(url)) {
                        console.warn('[Security] 阻止危险 URL:', url);
                        return null;
                    }
                    return originalOpen.call(window, url, ...args);
                };

                console.log('[Security] XSS 防护已启用');
            })();
        ");
    }

    // 安全地设置 HTML 内容（转义后注入）
    public async Task SetSafeHtml(string elementId, string htmlContent)
    {
        // 在 C# 端转义
        string escaped = JsonSerializer.Serialize(htmlContent);

        await _webView.ExecuteScriptAsync($@"
            (function() {{
                const el = document.getElementById('{elementId}');
                if (el) {{
                    // 使用 textContent 防止 XSS
                    const div = document.createElement('div');
                    div.innerHTML = {escaped};  // JSON 已转义
                    el.textContent = '';
                    el.appendChild(div);
                }}
            }})();
        ");
    }
}
```

### 31.7.2 CSP（内容安全策略）

```csharp
// 方式一：通过 meta 标签设置 CSP
public async Task SetContentSecurityPolicy()
{
    await _webView.ExecuteScriptAsync(@"
        (function() {
            const meta = document.createElement('meta');
            meta.httpEquiv = 'Content-Security-Policy';
            meta.content = [
                ""default-src 'self'"",
                ""script-src 'self' 'unsafe-inline'"",
                ""style-src 'self' 'unsafe-inline'"",
                ""img-src 'self' data: https:;"",
                ""connect-src 'self' https://api.example.com"",
                ""font-src 'self'"",
                ""object-src 'none'"",
                ""base-uri 'self'"",
                ""form-action 'self'""
            ].join('; ');
            document.head.prepend(meta);
        })();
    ");
}

// 方式二：通过 HTTP 头设置（WebView2 拦截方式）
public void SetCspHeader(CoreWebView2WebResourceRequestedEventArgs e)
{
    // 在请求拦截中添加 CSP 头
    // 注意：这需要修改响应头，具体方式取决于 WebView 实现
}
```

### 31.7.3 权限控制

```csharp
public class PermissionManager
{
    private readonly WebView _webView;
    private readonly HashSet<string> _allowedPermissions;

    public PermissionManager(WebView webView)
    {
        _webView = webView;
        _allowedPermissions = new HashSet<string>
        {
            "clipboard-read",
            "clipboard-write"
        };
    }

    // WebView2 权限请求处理
    public void SetupPermissionHandling()
    {
        _webView.CoreWebView2.PermissionRequested += (s, e) =>
        {
            string permission = e.PermissionKind.ToString();
            Console.WriteLine($"权限请求: {permission} for {e.Uri}");

            if (_allowedPermissions.Contains(permission.ToLower()))
            {
                e.State = CoreWebView2PermissionState.Allow;
            }
            else
            {
                e.State = CoreWebView2PermissionState.Deny;
            }
        };
    }

    // JS 端权限检查注入
    public async Task InjectPermissionGuard()
    {
        await _webView.ExecuteScriptAsync(@"
            (function() {
                // 限制剪贴板访问
                const originalWriteText = navigator.clipboard.writeText;
                navigator.clipboard.writeText = async function(text) {
                    // 验证文本长度
                    if (text.length > 100000) {
                        throw new Error('剪贴板内容过长');
                    }
                    return originalWriteText.call(navigator.clipboard, text);
                };

                // 限制地理位置请求
                const originalGetCurrentPosition = navigator.geolocation.getCurrentPosition;
                navigator.geolocation.getCurrentPosition = function(success, error, options) {
                    console.log('[Permission] 地理位置请求被拦截');
                    if (error) {
                        error({ code: 1, message: '权限被拒绝' });
                    }
                };

                // 禁用通知 API
                window.Notification = {
                    requestPermission: function() {
                        return Promise.resolve('denied');
                    }
                };
            })();
        ");
    }
}
```

### 31.7.4 HTTPS 证书验证

```csharp
public class CertificateManager
{
    private readonly WebView _webView;

    public CertificateManager(WebView webView)
    {
        _webView = webView;
    }

    // WebView2 自定义证书验证
    public void SetupCertificateValidation()
    {
        _webView.CoreWebView2.ServerCertificateErrorDetected += (s, e) =>
        {
            var error = e.ErrorStatus;
            var uri = e.RequestUri;

            Console.WriteLine($"证书错误: {error} for {uri}");

            // 根据策略决定是否信任
            if (IsTrustedHost(uri))
            {
                // 信任自签名证书（仅用于开发环境！）
                e.Action = CoreWebView2ServerCertificateErrorAction.AlwaysAllow;
            }
            else
            {
                e.Action = CoreWebView2ServerCertificateErrorAction.Cancel;
            }
        };
    }

    private bool IsTrustedHost(string uri)
    {
        // 仅允许本地开发地址
        var host = new Uri(uri).Host;
        return host == "localhost" || host == "127.0.0.1";
    }

    // 忽略所有证书错误（仅用于开发！）
    public void IgnoreAllCertificateErrors_DevOnly()
    {
        #if DEBUG
        _webView.CoreWebView2.ServerCertificateErrorDetected += (s, e) =>
        {
            e.Action = CoreWebView2ServerCertificateErrorAction.AlwaysAllow;
        };
        #endif
    }
}
```

## Deep Dive

### WebView 渲染原理

WebView 本质上是在 Avalonia 窗口中嵌入了一个独立的浏览器进程。以 Windows 上的 WebView2 为例：

```
┌──────────────────────────────────────────┐
│ Avalonia 应用进程                          │
│                                          │
│  ┌─────────────────────────────────────┐ │
│  │ WebView2 控件                       │ │
│  │  (Avalonia 侧只是一个"占位符")       │ │
│  └─────────────┬───────────────────────┘ │
│                │ COM 接口                  │
└────────────────┼──────────────────────────┘
                 │
    ┌────────────┴────────────┐
    │ WebView2 浏览器进程       │
    │ (msedgewebview2.exe)    │
    │                         │
    │ ┌───────────────────┐   │
    │ │ Chromium 渲染引擎  │   │
    │ │ - HTML 解析        │   │
    │ │ - CSS 渲染         │   │
    │ │ - JS 执行          │   │
    │ │ - GPU 加速         │   │
    │ └───────────────────┘   │
    └─────────────────────────┘
```

**关键要点：**
- WebView 有自己的进程，与 Avalonia UI 线程隔离
- JS 执行不会阻塞 Avalonia UI 线程
- 但 `ExecuteScriptAsync` 是从 UI 线程发起的异步调用
- 大量频繁的 C#/JS 通信会带来序列化开销

### 内存管理

WebView 进程会消耗独立的内存，需要注意：

```csharp
public class WebViewMemoryManager
{
    private readonly WebView _webView;

    public WebViewMemoryManager(WebView webView)
    {
        _webView = webView;
    }

    // 监控内存使用
    public async Task<MemoryInfo> GetMemoryUsage()
    {
        string json = await _webView.ExecuteScriptAsync(@"
            JSON.stringify(performance.memory ? {
                usedJSHeapSize: performance.memory.usedJSHeapSize,
                totalJSHeapSize: performance.memory.totalJSHeapSize,
                jsHeapSizeLimit: performance.memory.jsHeapSizeLimit
            } : { available: false })
        ");

        return JsonSerializer.Deserialize<MemoryInfo>(json);
    }

    // 清理 WebView 内存
    public async Task Cleanup()
    {
        // 清除 JS 缓存
        await _webView.ExecuteScriptAsync(@"
            // 清除不需要的全局变量
            if (window.tempData) {
                window.tempData = null;
                delete window.tempData;
            }

            // 建议垃圾回收
            if (window.gc) window.gc();
        ");
    }

    // 窗口关闭时释放资源
    public void Dispose()
    {
        // 导航到空白页释放渲染资源
        _webView.Source = new Uri("about:blank");
    }
}

public class MemoryInfo
{
    [JsonPropertyName("available")]
    public bool Available { get; set; }

    [JsonPropertyName("usedJSHeapSize")]
    public long UsedJSHeapSize { get; set; }

    [JsonPropertyName("totalJSHeapSize")]
    public long TotalJSHeapSize { get; set; }

    [JsonPropertyName("jsHeapSizeLimit")]
    public long JsHeapSizeLimit { get; set; }
}
```

### WebView 与 Avalonia 生命周期

```csharp
public partial class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownRequested += (s, e) =>
            {
                // 关闭前清理所有 WebView
                CleanupAllWebViews();
            };

            desktop.Exit += (s, e) =>
            {
                // 确保浏览器进程被终止
                ForceKillBrowserProcesses();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void CleanupAllWebViews()
    {
        // 遍历所有窗口，关闭 WebView
        foreach (var window in Application.Current.Windows)
        {
            var webView = FindWebViewInVisualTree(window);
            webView?.Dispose();
        }
    }
}
```

## Cross References

- **第 5 章 数据绑定** — WebView 中的数据需要通过桥接机制手动同步，与 Avalonia 原生绑定不同
- **第 6 章 MVVM 模式** — 混合应用架构中，ViewModel 可同时管理原生控件和 WebView 数据
- **第 16 章 输入事件** — WebView 内部的鼠标/键盘事件由浏览器引擎处理，不经过 Avalonia 事件系统
- **第 17 章 对话框与弹出** — WebView 内部的 `alert()`、`confirm()` 等由浏览器引擎处理
- **第 20 章 跨平台考虑** — WebView 方案的平台兼容性差异是跨平台开发的重要考量
- **第 25 章 ASP.NET Core 集成** — 可以在本地 HTTP 服务器上托管 WebView 加载的 HTML 内容
- **第 26 章 导航控件** — WebView 有自己的导航模型，与 Avalonia Frame 导航不同

## Common Pitfalls

### Pitfall 1：在非 UI 线程调用 ExecuteScriptAsync

```csharp
// 错误：在后台线程直接调用
Task.Run(async () =>
{
    await _webView.ExecuteScriptAsync("alert('hello')"); // 可能崩溃！
});

// 正确：切换到 UI 线程
Task.Run(async () =>
{
    await Dispatcher.UIThread.InvokeAsync(async () =>
    {
        await _webView.ExecuteScriptAsync("alert('hello')");
    });
});
```

### Pitfall 2：忘记处理 JS 返回值的引号

```csharp
// 错误：直接拼接字符串（可能导致 XSS 或语法错误）
string userInput = GetInput(); // 可能包含引号
await _webView.ExecuteScriptAsync($"showMessage('{userInput}')");

// 正确：使用 JSON 序列化安全传递
string safeInput = JsonSerializer.Serialize(userInput);
await _webView.ExecuteScriptAsync($"showMessage({safeInput})");
```

### Pitfall 3：WebView 未加载完成就执行 JS

```csharp
// 错误：立即执行（页面可能还没加载）
_webView.Source = new Uri("https://example.com");
await _webView.ExecuteScriptAsync("document.title"); // 可能失败

// 正确：等待加载完成
_webView.LoadCompleted += async (s, e) =>
{
    string title = await _webView.ExecuteScriptAsync("document.title");
};
```

### Pitfall 4：内存泄漏 —— 未在窗口关闭时释放 WebView

```csharp
// 错误：窗口关闭时未清理
public MainWindow()
{
    InitializeComponent();
    // WebView 会继续占用内存和浏览器进程
}

// 正确：窗口关闭时释放
protected override void OnClosed(EventArgs e)
{
    _webView?.Dispose();
    base.OnClosed(e);
}
```

### Pitfall 5：频繁的 C#/JS 通信导致性能问题

```csharp
// 错误：在循环中频繁调用 JS
foreach (var item in items)
{
    await _webView.ExecuteScriptAsync($"addItem('{item}')"); // 1000 次调用！
}

// 正确：批量传递
string json = JsonSerializer.Serialize(items);
await _webView.ExecuteScriptAsync($"addItems({json})"); // 1 次调用
```

### Pitfall 6：未处理 WebView 加载失败的情况

```csharp
// 错误：假设加载总是成功
_webView.Source = new Uri("https://unreliable-site.com");

// 正确：处理加载错误
_webView.LoadCompleted += (s, e) =>
{
    if (e.IsError)
    {
        // 显示错误页面
        _webView.LoadHtml(@"
            <html><body>
                <h2>页面加载失败</h2>
                <p>请检查网络连接后重试</p>
                <button onclick='location.reload()'>重试</button>
            </body></html>
        ");
    }
};
```

### Pitfall 7：在 WebView 中使用 Avalonia 主题变量

```csharp
// 无法直接在 HTML 中使用 Avalonia 主题变量
// 需要通过桥接传递当前主题颜色

// 正确方式：在 C# 中获取颜色后传给 JS
private async Task SyncTheme()
{
    var bg = this.FindResource("SystemAltHighColor") as Color?;
    string hex = bg.HasValue ? $"#{bg.Value.R:X2}{bg.Value.G:X2}{bg.Value.B:X2}" : "#ffffff";

    await _webView.ExecuteScriptAsync($@"
        document.documentElement.style.setProperty('--app-bg', '{hex}');
    ");
}
```

### Pitfall 8：忽略 WebView 的 DPI 缩放问题

```csharp
// 在高 DPI 显示器上，WebView 内容可能模糊
// 确保 WebView 支持 DPI 感知

// WebView2 通常自动处理 DPI，但嵌入的 HTML 可能需要：
await _webView.ExecuteScriptAsync(@"
    // 获取设备像素比
    const dpr = window.devicePixelRatio;
    document.body.style.zoom = dpr;  // 不推荐
    // 推荐：使用 meta viewport
    // <meta name='viewport' content='width=device-width, initial-scale=1'>
");
```

### Pitfall 9：同时打开多个 WebView 实例导致资源耗尽

```csharp
// 每个 WebView 实例都会创建独立的浏览器进程
// 错误：不加限制地创建 WebView

// 正确：使用池化管理
public class WebViewPool : IDisposable
{
    private readonly Queue<WebView> _available = new();
    private readonly List<WebView> _all = new();
    private readonly int _maxSize;

    public WebViewPool(int maxSize = 5)
    {
        _maxSize = maxSize;
    }

    public WebView Acquire()
    {
        if (_available.Count > 0)
            return _available.Dequeue();

        if (_all.Count < _maxSize)
        {
            var webView = new WebView();
            _all.Add(webView);
            return webView;
        }

        throw new InvalidOperationException("WebView 池已满");
    }

    public void Release(WebView webView)
    {
        webView.Source = new Uri("about:blank"); // 释放页面资源
        _available.Enqueue(webView);
    }

    public void Dispose()
    {
        foreach (var wv in _all) wv?.Dispose();
    }
}
```

### Pitfall 10：将敏感信息暴露给 WebView 中的 JavaScript

```csharp
// 错误：将 API 密钥等敏感信息传给 JS
await _webView.ExecuteScriptAsync($"apiKey = '{_secretApiKey}'");

// 正确：敏感操作在 C# 端完成
// JS 只负责 UI 交互，数据获取通过 C# 桥接
_webView.WebMessageReceived += async (s, e) =>
{
    var msg = JsonDocument.Parse(e.Message);
    if (msg.RootElement.GetProperty("action").GetString() == "fetchData")
    {
        var data = await FetchFromApiWithSecret(_secretApiKey);
        await _webView.ExecuteScriptAsync(
            $"window.receiveData({data})");
    }
};
```

## Try It Yourself

### 练习 1：基础 WebView 嵌入

**目标**：创建一个 Avalonia 应用，在 WebView 中显示一个带样式的 HTML 页面。

**要求**：
- 创建一个包含标题、段落和按钮的 HTML 页面
- 使用内联 CSS 添加渐变背景和卡片样式
- 在 Avalonia 窗口中显示该页面

```csharp
// 提示：使用 LoadHtml 方法加载字符串形式的 HTML
// <提示结束>
```

---

### 练习 2：C# 调用 JavaScript

**目标**：实现从 Avalonia 按钮点击触发 JavaScript 函数，并将结果显示在 TextBlock 中。

**要求**：
- HTML 页面中定义一个 `getSystemInfo()` 函数，返回 JSON 格式的系统信息
- C# 按钮点击后调用该函数
- 将返回的 JSON 解析后显示在界面上

```csharp
// 提示：使用 ExecuteScriptAsync 和 JsonSerializer.Deserialize
// <提示结束>
```

---

### 练习 3：JavaScript 调用 C#

**目标**：实现 JavaScript 中的按钮点击后，触发 C# 方法修改 Avalonia 窗口标题。

**要求**：
- HTML 中有一个文本框和按钮
- 用户输入新标题后点击按钮
- 通过 WebMessageReceived 接收消息
- 修改窗口标题

```csharp
// 提示：使用 window.chrome.webview.postMessage 和 WebMessageReceived 事件
// <提示结束>
```

---

### 练习 4：Markdown 实时预览

**目标**：构建一个左右分栏的 Markdown 编辑器，左侧是 Avalonia TextBox，右侧是 WebView 渲染预览。

**要求**：
- 左侧使用 Avalonia 的 TextBox 输入 Markdown 文本
- 右侧 WebView 实时渲染为 HTML
- 使用标记库（如 marked.js）在 JS 端进行 Markdown 转换
- 输入变化时实时更新预览

```csharp
// 提示：监听 TextBox.TextChanged 事件，通过 ExecuteScriptAsync 传入文本
// <提示结束>
```

---

### 练习 5：主题同步

**目标**：实现 Avalonia 暗色/亮色主题切换时，WebView 内容自动跟随变化。

**要求**：
- 在 Avalonia 中使用 ToggleSwitch 切换主题
- 主题切换时，通过 JS Bridge 将当前主题颜色发送到 WebView
- WebView 中的 CSS 变量根据主题更新
- 提供平滑的过渡动画

```csharp
// 提示：在 C# 中读取主题资源值，序列化后传给 JS 更新 CSS 变量
// <提示结束>
```

---

### 练习 6：错误处理和加载状态

**目标**：实现完善的 WebView 错误处理和加载状态指示。

**要求**：
- 显示加载进度条（响应 Navigating/LoadCompleted 事件）
- 加载失败时显示自定义错误页面（含重试按钮）
- JS 报错时在 Avalonia 状态栏显示错误信息
- 实现全局 JS 错误捕获并转发到 C#

```csharp
// 提示：结合 LoadCompleted 事件和 window.onerror 全局错误处理器
// <提示结束>
```

---

### 练习 7：文件上传对话框

**目标**：在 WebView 中实现文件上传功能，并使用 Avalonia 原生文件选择对话框。

**要求**：
- HTML 中有文件上传按钮
- 拦截文件选择请求
- 使用 Avalonia 的 OpenFileDialog 替代浏览器原生对话框
- 将选择的文件信息返回给 WebView

```csharp
// 提示：使用 WebView2 的 FileRequested 事件或自定义桥接
// <提示结束>
```

---

### 练习 8：构建混合应用（综合练习）

**目标**：构建一个完整的混合应用：Avalonia 原生侧边栏 + WebView 仪表板。

**要求**：
- 左侧：Avalonia ListBox 显示数据项列表
- 右侧：WebView 显示 ECharts 图表，展示选中项的详细数据
- 点击左侧列表项，右侧图表实时更新
- WebView 中的图表点击事件，左侧列表自动选中对应项
- 实现主题同步（深色/浅色模式切换）

```csharp
// 提示：综合运用 ExecuteScriptAsync、WebMessageReceived、JSON 序列化
// <提示结束>
```
