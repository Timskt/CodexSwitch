# 25. Avalonia 与 ASP.NET Core 集成

CodexSwitch 在桌面应用中嵌入了一个 ASP.NET Core HTTP 服务器，这是一个高级架构模式。

## 25.1 为什么在桌面应用中嵌入 HTTP 服务器

- 为本地客户端（如 Codex CLI）提供 API 接口
- 代理请求到远程服务
- 提供本地 Web 管理界面
- 实现进程间通信

### CodexSwitch 的代理架构

```
Codex CLI ──HTTP──→ CodexSwitch Proxy ──HTTP──→ 远程 API (OpenAI/Anthropic)
                         ↓
                    用量记录
                    协议转换
                    负载均衡
```

## 25.2 项目配置

```xml
<!-- 引用 ASP.NET Core 框架 -->
<ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

这比引用 NuGet 包更轻量，因为 ASP.NET Core 框架已经包含在 .NET 运行时中。

## 25.3 代理服务实现

```csharp
public class ProxyHostService
{
    private WebApplication? _app;

    public async Task StartAsync(ProxySettings settings)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(settings.Port);
        });

        _app = builder.Build();

        // 配置路由
        _app.MapGet("/health", () => Results.Ok("healthy"));
        _app.MapGet("/v1/models", () => Results.Ok(models));
        _app.MapPost("/v1/responses", async (HttpRequest req) => { ... });
        _app.MapPost("/v1/messages", async (HttpRequest req) => { ... });

        await _app.StartAsync();
    }

    public async Task StopAsync()
    {
        if (_app is not null)
            await _app.StopAsync();
    }
}
```

## 25.4 生命周期管理

```csharp
// ViewModel 中管理代理服务
public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly ProxyHostService _proxyHost;

    [RelayCommand]
    private async Task ToggleProxyAsync()
    {
        if (_isProxyRunning)
            await _proxyHost.StopAsync();
        else
            await _proxyHost.StartAsync(_proxySettings);
    }

    public async ValueTask DisposeAsync()
    {
        await _proxyHost.StopAsync();
    }
}
```

### 代理状态指示

```xml
<!-- 在 UI 中显示代理状态 -->
<StackPanel>
    <TextBlock Text="{Binding ServiceStateText}"/>
    <ui:CodexBadge Variant="{Binding IsProxyRunning, Converter={x:Static converters:BoolToBadgeVariantConverter.Instance}}">
        <TextBlock Text="{Binding ProxyStatusText}"/>
    </ui:CodexBadge>
</StackPanel>
```

## 25.5 端口冲突处理

```csharp
// 检查端口是否可用
private static bool IsPortAvailable(int port)
{
    try
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();
        return true;
    }
    catch
    {
        return false;
    }
}

// 自动选择可用端口
private int FindAvailablePort(int preferredPort)
{
    if (IsPortAvailable(preferredPort))
        return preferredPort;

    // 尝试其他端口
    for (int port = preferredPort + 1; port < preferredPort + 100; port++)
    {
        if (IsPortAvailable(port))
            return port;
    }

    throw new InvalidOperationException("No available port found");
}
```

## 25.6 WebSocket 代理

```csharp
// WebSocket 代理支持
app.Map("/v1/realtime", async (HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var ws = await context.WebSockets.AcceptWebSocketAsync();
        await ProxyWebSocketAsync(ws, upstreamUrl);
    }
});
```

### WebSocket 代理实现

```csharp
private async Task ProxyWebSocketAsync(WebSocket clientWs, string upstreamUrl)
{
    using var upstreamWs = new ClientWebSocket();
    await upstreamWs.ConnectAsync(new Uri(upstreamUrl), CancellationToken.None);

    var clientToUpstream = RelayMessagesAsync(clientWs, upstreamWs);
    var upstreamToClient = RelayMessagesAsync(upstreamWs, clientWs);

    await Task.WhenAny(clientToUpstream, upstreamToClient);
}

private async Task RelayMessagesAsync(WebSocket source, WebSocket target)
{
    var buffer = new byte[4096];
    while (source.State == WebSocketState.Open)
    {
        var result = await source.ReceiveAsync(buffer, CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Close)
            break;

        await target.SendAsync(
            buffer.AsMemory(0, result.Count),
            result.MessageType,
            result.EndOfMessage,
            CancellationToken.None);
    }
}
```

## 25.7 中间件管道

```csharp
var app = builder.Build();

// 请求日志中间件
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();
    Debug.WriteLine($"{context.Request.Method} {context.Request.Path} - {sw.ElapsedMilliseconds}ms");
});

// CORS 中间件
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// 路由
app.MapGet("/health", () => Results.Ok("healthy"));
app.MapPost("/v1/responses", HandleResponses);
```

---

## Deep Dive：HTTP 服务器与 UI 线程

### 线程模型

```
UI 线程 (STA):
    └── 处理用户输入、渲染 UI

ASP.NET Core 线程池:
    └── 处理 HTTP 请求

通信方式:
    ├── 共享状态（需要线程安全）
    ├── 事件（UI 线程回调）
    └── Dispatcher.UIThread.InvokeAsync()
```

### 线程安全

```csharp
// 在 HTTP 请求中更新 UI
app.MapPost("/api/update", async (HttpContext context) =>
{
    var data = await context.Request.ReadFromJsonAsync<UpdateData>();

    // 在 UI 线程上执行
    await Dispatcher.UIThread.InvokeAsync(() =>
    {
        viewModel.UpdateData(data);
    });

    return Results.Ok();
});
```

## Cross References

- [第 2 章 项目结构与启动流程](02-project-structure.md) — 项目配置
- [第 6 章 MVVM 模式实战](06-mvvm-pattern.md) — ViewModel 中的服务管理
- [第 21 章 调试与诊断](21-debugging.md) — HTTP 请求调试

## Common Pitfalls

1. **在 UI 线程上执行 HTTP 请求**: 会导致 UI 卡顿
2. **不处理端口冲突**: 应用启动失败
3. **不正确释放资源**: HTTP 服务器未正确关闭
4. **线程安全问题**: 多线程访问共享状态

## Try It Yourself

1. 在 CodexSwitch 中找到 `ProxyHostService`，研究它的实现
2. 尝试添加一个新的 API 端点
3. 测试代理服务器的并发处理能力
