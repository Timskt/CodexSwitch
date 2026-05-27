# 25. Avalonia 与 ASP.NET Core 集成

> **写给零基础的你**：本章是高级内容，初学者可以先跳过。简单来说，ASP.NET Core 是微软的 Web 服务器框架（就是做网站用的）。CodexSwitch 在桌面应用里"偷偷"跑了一个小型网站服务器，用来给其他程序提供数据接口。就像你家里的路由器——你不需要知道它怎么工作，但它帮你连接了所有设备。

## 25.1 概述

将 ASP.NET Core HTTP 服务器嵌入 Avalonia 桌面应用是一种高级架构模式，能够为桌面应用提供强大的网络服务能力。CodexSwitch 项目采用了这种架构，在桌面应用中运行一个完整的 Kestrel HTTP 服务器，实现了本地代理、API 网关、协议转换等功能。

**为什么在桌面应用中嵌入 HTTP 服务器：**

> **小白提示**：HTTP 服务器就像一个"接待窗口"。你在银行柜台（桌面应用）里开了一个小窗口（HTTP 服务器），其他程序（如 Codex CLI）可以通过这个窗口来"办理业务"（请求数据、发送指令）。窗口不开，别人就没法和你交互。

- 为本地客户端（如 Codex CLI）提供 RESTful API 接口（接待窗口提供服务）
- 实现代理功能，将请求转发到远程服务（代别人办事）
- 提供本地 Web 管理界面（在浏览器里管理应用）
- 实现进程间通信（IPC）（不同程序之间对话）
- 作为 API 网关进行协议转换（翻译不同语言的请求）

**应用场景：**
- 本地 API 代理服务器
- 开发工具的后端服务
- 桌面应用与 Web 应用的桥接
- 微服务架构中的本地服务节点

## 25.2 项目配置

### 25.2.1 引用 ASP.NET Core 框架

```xml
<!-- 在 .csproj 中引用 ASP.NET Core 框架 -->
<!-- 方式 1：使用 FrameworkReference（推荐，更轻量） -->
<ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>

<!-- 方式 2：使用 NuGet 包（较少使用） -->
<ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.App" Version="8.0.0" />
</ItemGroup>
```

**FrameworkReference vs NuGet 包：**
- `FrameworkReference`：引用 .NET 运行时中已包含的 ASP.NET Core 框架，更轻量
- NuGet 包：独立安装 ASP.NET Core，适用于需要特定版本的场景

### 25.2.2 必要的命名空间

```csharp
// ASP.NET Core 核心
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// HTTP 相关
using System.Net;
using System.Net.Sockets;

// JSON 序列化
using System.Text.Json;
```

### 25.2.3 基本项目结构

```
CodexSwitch/
├── Proxy/
│   ├── ProxyHostService.cs          ← HTTP 服务器主服务
│   ├── IProviderProtocolAdapter.cs  ← 协议适配器接口
│   ├── OpenAiResponsesAdapter.cs    ← OpenAI 协议适配
│   ├── AnthropicMessagesAdapter.cs  ← Anthropic 协议适配
│   └── ProtocolAdapterCommon.cs     ← 公共工具
├── Services/
│   ├── AppThemeService.cs           ← 主题服务
│   └── ConfigurationStore.cs        ← 配置存储
└── Models/
    ├── AppConfig.cs                 ← 应用配置
    └── ProxySettings.cs             ← 代理设置
```

## 25.3 WebApplication.CreateBuilder 配置

### 25.3.1 创建 Slim Builder

```csharp
// 使用 CreateSlimBuilder 创建轻量级应用
// 比 CreateBuilder 更轻量，适合桌面应用
var builder = WebApplication.CreateSlimBuilder();

// CreateSlimBuilder 不包含：
// - 控制器支持（需要手动添加）
// - Razor Pages
// - 静态文件支持
// - 默认日志提供程序

// CreateBuilder 包含所有功能，但更重
var builder = WebApplication.CreateBuilder();
```

### 25.3.2 配置 JSON 序列化

```csharp
// 配置 JSON 序列化选项
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // 使用源生成器（AOT 兼容）
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, CodexSwitchJsonContext.Default);

    // 其他选项
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.WriteIndented = false;
});

// 源生成器定义（AOT 兼容）
[JsonSerializable(typeof(ProxyHealthResponse))]
[JsonSerializable(typeof(ModelsListResponse))]
[JsonSerializable(typeof(ResponsesRequest))]
[JsonSerializable(typeof(ResponsesResponse))]
public partial class CodexSwitchJsonContext : JsonSerializerContext
{
}
```

### 25.3.3 配置传输层

```csharp
// 配置 Socket 传输选项
builder.Services.Configure<SocketTransportOptions>(options =>
{
    // 禁用 Nagle 算法，减少延迟
    options.NoDelay = true;
});

// 配置 HTTP 客户端
builder.Services.AddHttpClient("upstream", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "CodexSwitch/1.0");
});
```

### 25.3.4 禁用日志提供程序

```csharp
// 清除所有日志提供程序（桌面应用不需要控制台日志）
builder.Logging.ClearProviders();

// 或者只添加特定的日志提供程序
builder.Logging.AddDebug();  // 输出到调试窗口
builder.Logging.SetMinimumLevel(LogLevel.Warning);
```

## 25.4 Kestrel 配置详解

### 25.4.1 基本配置

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    // 不添加 Server 头（安全考虑）
    options.AddServerHeader = false;

    // 配置监听地址和端口
    options.Listen(IPAddress.Loopback, 5000);

    // 配置 HTTPS
    options.Listen(IPAddress.Loopback, 5001, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});
```

### 25.4.2 超时配置

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    // 保持连接超时（默认 2 分钟）
    options.Limits.KeepAliveTimeout = TimeSpan.FromHours(2);

    // 请求头超时（默认 30 秒）
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);

    // HTTP/2 Keep-Alive 配置
    options.Limits.Http2.KeepAlivePingDelay = TimeSpan.FromSeconds(30);
    options.Limits.Http2.KeepAlivePingTimeout = TimeSpan.FromSeconds(15);
});
```

### 25.4.3 并发配置

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    // 最大并发连接数（默认无限制）
    options.Limits.MaxConcurrentConnections = 1024;

    // 最大并发升级连接数（WebSocket 等）
    options.Limits.MaxConcurrentUpgradedConnections = 1024;

    // HTTP/2 最大并发流
    options.Limits.Http2.MaxStreamsPerConnection = 256;
});
```

### 25.4.4 数据速率配置

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    // 最小请求体数据速率（字节/秒）
    // 设置为 null 禁用速率限制
    options.Limits.MinRequestBodyDataRate = null;

    // 最小响应数据速率
    options.Limits.MinResponseDataRate = null;

    // 或者设置具体值
    options.Limits.MinRequestBodyDataRate = new MinDataRate(
        bytesPerSecond: 100,
        gracePeriod: TimeSpan.FromSeconds(5));
});
```

### 25.4.5 协议配置

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Loopback, 5000, listenOptions =>
    {
        // 支持 HTTP/1.1 和 HTTP/2
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;

        // 仅 HTTP/1.1
        // listenOptions.Protocols = HttpProtocols.Http1;

        // 仅 HTTP/2
        // listenOptions.Protocols = HttpProtocols.Http2;
    });
});
```

### 25.4.6 CodexSwitch 的完整 Kestrel 配置

```csharp
// ProxyHostService.cs 中的完整配置
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.KeepAliveTimeout = ClientKeepAliveTimeout;  // 2 小时
    options.Limits.RequestHeadersTimeout = ClientRequestHeadersTimeout;  // 10 秒
    options.Limits.MaxConcurrentConnections = 1024;
    options.Limits.MinRequestBodyDataRate = null;  // 禁用速率限制
    options.Limits.MinResponseDataRate = null;
    options.Limits.Http2.MaxStreamsPerConnection = 256;
    options.Limits.Http2.KeepAlivePingDelay = ClientHttp2KeepAlivePingDelay;  // 30 秒
    options.Limits.Http2.KeepAlivePingTimeout = ClientHttp2KeepAlivePingTimeout;  // 15 秒

    // 监听配置
    options.Listen(ParseAddress(config.Proxy.Host), config.Proxy.Port, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});
```

## 25.5 路由配置

### 25.5.1 MapGet

```csharp
// 简单的 GET 端点
app.MapGet("/health", () => Results.Ok("healthy"));

// 带参数的 GET 端点
app.MapGet("/api/users/{id}", (int id) =>
{
    return Results.Ok(new { Id = id, Name = "User" });
});

// 带查询参数的 GET 端点
app.MapGet("/api/users", (string? name, int? page) =>
{
    return Results.Ok(new { Name = name, Page = page ?? 1 });
});

// 异步 GET 端点
app.MapGet("/api/data", async (CancellationToken ct) =>
{
    var data = await FetchDataAsync(ct);
    return Results.Ok(data);
});
```

### 25.5.2 MapPost

```csharp
// 简单的 POST 端点
app.MapPost("/api/users", (User user) =>
{
    return Results.Created($"/api/users/{user.Id}", user);
});

// 读取请求体
app.MapPost("/api/data", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    return Results.Ok(new { Received = body });
});

// 使用 JSON 反序列化
app.MapPost("/api/messages", async (HttpContext context) =>
{
    var message = await context.Request.ReadFromJsonAsync<ChatMessage>();
    return Results.Ok(message);
});
```

### 25.5.3 MapPut 和 MapDelete

```csharp
// PUT 端点
app.MapPut("/api/users/{id}", (int id, User user) =>
{
    user.Id = id;
    return Results.Ok(user);
});

// DELETE 端点
app.MapDelete("/api/users/{id}", (int id) =>
{
    return Results.NoContent();
});
```

### 25.5.4 CodexSwitch 的路由配置

```csharp
// ProxyHostService.cs 中的路由配置
app.MapGet("/health", WriteHealthAsync);
app.MapGet("/v1/models", WriteModelsAsync);
app.MapGet("/v1/responses", HandleResponsesWebSocketAsync);  // WebSocket
app.MapPost("/v1/responses", HandleResponsesAsync);
app.MapPost("/v1/messages", HandleMessagesAsync);
app.MapHealthChecks("/health");
```

### 25.5.5 路由处理器实现

```csharp
// 健康检查端点
private Task WriteHealthAsync(HttpContext httpContext)
{
    var snapshot = _usageMeter.Snapshot;
    var response = new ProxyHealthResponse
    {
        Status = State.IsRunning ? "running" : "stopped",
        Endpoint = State.Endpoint,
        ActiveProviderId = State.ActiveProviderId,
        ActiveProviderProtocol = State.ActiveProviderProtocol,
        Requests = snapshot.Requests,
        Errors = snapshot.Errors
    };

    var json = JsonSerializer.Serialize(response, CodexSwitchJsonContext.Default.ProxyHealthResponse);
    httpContext.Response.ContentType = "application/json";
    return httpContext.Response.WriteAsync(json, httpContext.RequestAborted);
}

// 模型列表端点
private Task WriteModelsAsync(HttpContext httpContext)
{
    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var provider = ProviderRoutingResolver.ResolveSelectedProvider(_config, ClientAppKind.Codex);
    var models = ProviderRoutingResolver.CollectModelListings(_config)
        .Select(model => new ModelInfoResponse
        {
            Id = model.Id,
            Created = now,
            OwnedBy = model.OwnedBy
        })
        .ToArray();

    var response = new ModelsListResponse { Data = models };
    var json = JsonSerializer.Serialize(response, CodexSwitchJsonContext.Default.ModelsListResponse);
    httpContext.Response.ContentType = "application/json";
    return httpContext.Response.WriteAsync(json, httpContext.RequestAborted);
}
```

## 25.6 中间件管道

### 25.6.1 中间件顺序

```csharp
var app = builder.Build();

// 中间件顺序很重要！
// 1. 异常处理
app.UseExceptionHandler("/error");

// 2. HTTPS 重定向
app.UseHttpsRedirection();

// 3. CORS
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// 4. WebSocket
app.UseWebSockets();

// 5. 自定义中间件
app.Use(ApplyLowLatencyClientConnectionAsync);

// 6. 路由
app.MapGet("/health", WriteHealthAsync);
app.MapPost("/v1/responses", HandleResponsesAsync);
```

### 25.6.2 自定义中间件

```csharp
// 方式 1：内联中间件
app.Use(async (context, next) =>
{
    // 请求前逻辑
    var sw = Stopwatch.StartNew();

    await next();

    // 响应后逻辑
    sw.Stop();
    Debug.WriteLine($"{context.Request.Method} {context.Request.Path} - {sw.ElapsedMilliseconds}ms");
});

// 方式 2：中间件类
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();
        Debug.WriteLine($"Request: {context.Request.Method} {context.Request.Path} - {sw.ElapsedMilliseconds}ms");
    }
}

// 注册中间件类
app.UseMiddleware<RequestLoggingMiddleware>();
```

### 25.6.3 CodexSwitch 的中间件配置

```csharp
// ProxyHostService.cs 中的中间件配置
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// 低延迟中间件
app.Use(ApplyLowLatencyClientConnectionAsync);

// 低延迟中间件实现
private static Task ApplyLowLatencyClientConnectionAsync(HttpContext httpContext, Func<Task> next)
{
    // 禁用响应缓冲，减少延迟
    httpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
    return next();
}
```

### 25.6.4 CORS 配置

```csharp
// 配置 CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Custom-Header");
    });

    // 或者配置特定策略
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://example.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 使用 CORS
app.UseCors();  // 使用默认策略
// 或者
app.UseCors("AllowSpecificOrigin");  // 使用特定策略
```

## 25.7 WebSocket 代理实现

### 25.7.1 WebSocket 端点

```csharp
// WebSocket 端点实现
private async Task HandleResponsesWebSocketAsync(HttpContext httpContext)
{
    // 检查是否是 WebSocket 请求
    if (!httpContext.WebSockets.IsWebSocketRequest)
    {
        await ProtocolAdapterCommon.WriteJsonErrorAsync(
            httpContext,
            StatusCodes.Status400BadRequest,
            "Responses websocket endpoint requires a websocket upgrade request.",
            httpContext.RequestAborted);
        return;
    }

    // 接受 WebSocket 连接
    using var socket = await httpContext.WebSockets.AcceptWebSocketAsync();

    // 创建代理处理器
    var proxy = new ResponsesWebSocketProxy(
        () => _config,
        _providerAuthService,
        _responseStateStore,
        _usageMeter,
        _priceCalculator,
        _usageLogWriter);

    // 处理 WebSocket 消息
    await proxy.HandleAsync(httpContext, socket, httpContext.RequestAborted);
}
```

### 25.7.2 WebSocket 代理实现

```csharp
// WebSocket 代理的核心实现
public class ResponsesWebSocketProxy
{
    public async Task HandleAsync(HttpContext context, WebSocket socket, CancellationToken ct)
    {
        var buffer = new byte[4096];

        while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            // 接收客户端消息
            var result = await socket.ReceiveAsync(buffer, ct);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed",
                    ct);
                break;
            }

            // 处理消息
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var response = await ProcessMessageAsync(message, ct);

            // 发送响应
            var responseBytes = Encoding.UTF8.GetBytes(response);
            await socket.SendAsync(
                responseBytes,
                WebSocketMessageType.Text,
                true,
                ct);
        }
    }
}
```

### 25.7.3 WebSocket 消息中继

```csharp
// 双向 WebSocket 消息中继
private async Task ProxyWebSocketAsync(WebSocket clientWs, string upstreamUrl)
{
    using var upstreamWs = new ClientWebSocket();
    await upstreamWs.ConnectAsync(new Uri(upstreamUrl), CancellationToken.None);

    // 双向中继
    var clientToUpstream = RelayMessagesAsync(clientWs, upstreamWs);
    var upstreamToClient = RelayMessagesAsync(upstreamWs, clientWs);

    // 等待任一方向完成
    await Task.WhenAny(clientToUpstream, upstreamToClient);
}

private async Task RelayMessagesAsync(WebSocket source, WebSocket target)
{
    var buffer = new byte[4096];

    while (source.State == WebSocketState.Open)
    {
        var result = await source.ReceiveAsync(buffer, CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            await target.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Upstream closed",
                CancellationToken.None);
            break;
        }

        await target.SendAsync(
            buffer.AsMemory(0, result.Count),
            result.MessageType,
            result.EndOfMessage,
            CancellationToken.None);
    }
}
```

## 25.8 请求/响应处理

### 25.8.1 JSON 序列化

```csharp
// 使用源生成器进行 AOT 兼容的 JSON 序列化
var json = JsonSerializer.Serialize(response, CodexSwitchJsonContext.Default.ProxyHealthResponse);

// JSON 反序列化
var request = await JsonSerializer.DeserializeAsync(
    httpContext.Request.Body,
    CodexSwitchJsonContext.Default.ResponsesRequest,
    httpContext.RequestAborted);

// 手动写入 JSON 响应
private static Task WriteJsonErrorAsync(HttpContext context, int statusCode, string message)
{
    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/json";
    var escaped = JsonEncodedText.Encode(message).ToString();
    return context.Response.WriteAsync($"{{\"error\":\"{escaped}\"}}", context.RequestAborted);
}
```

### 25.8.2 流式响应

```csharp
// 流式响应实现
private async Task HandleStreamResponseAsync(HttpContext httpContext)
{
    httpContext.Response.ContentType = "text/event-stream";
    httpContext.Response.Headers.Add("Cache-Control", "no-cache");
    httpContext.Response.Headers.Add("Connection", "keep-alive");

    // 发送 SSE 事件
    for (int i = 0; i < 10; i++)
    {
        var data = $"data: {{\"index\": {i}, \"timestamp\": \"{DateTime.Now}\"}}\n\n";
        await httpContext.Response.WriteAsync(data, httpContext.RequestAborted);
        await httpContext.Response.Body.FlushAsync(httpContext.RequestAborted);
        await Task.Delay(1000, httpContext.RequestAborted);
    }
}
```

### 25.8.3 请求体读取

```csharp
// 读取请求体的不同方式

// 方式 1：读取为字符串
using var reader = new StreamReader(httpContext.Request.Body);
var body = await reader.ReadToEndAsync();

// 方式 2：读取为 JSON
var json = await JsonSerializer.DeserializeAsync<MyType>(
    httpContext.Request.Body,
    CodexSwitchJsonContext.Default.MyType);

// 方式 3：读取为字节数组
using var memoryStream = new MemoryStream();
await httpContext.Request.Body.CopyToAsync(memoryStream);
var bytes = memoryStream.ToArray();

// 方式 4：使用 JsonDocument
using var document = await JsonDocument.ParseAsync(httpContext.Request.Body);
var root = document.RootElement;
var model = root.GetProperty("model").GetString();
```

### 25.8.4 CodexSwitch 的请求处理

```csharp
// ProxyHostService.cs 中的请求处理
private async Task HandleResponsesAsync(HttpContext httpContext)
{
    // 开始输入活动计量
    using var inputActivity = _usageMeter.BeginInputActivity();

    // 读取请求
    ResponsesRequestSnapshot snapshot;
    try
    {
        snapshot = await ResponsesRequestSnapshot.ReadAsync(
            httpContext.Request.Body,
            httpContext.RequestAborted);
    }
    catch (JsonException)
    {
        await WriteJsonErrorAsync(httpContext, StatusCodes.Status400BadRequest, "Invalid JSON body.");
        return;
    }

    using (snapshot)
    {
        var requestModel = snapshot.RequestModel;
        inputActivity.Dispose();

        // 开始输出活动计量
        using var outputActivity = _usageMeter.BeginOutputActivity();
        httpContext.Items[ProtocolAdapterCommon.OutputActivityItemKey] = outputActivity;

        try
        {
            // 转发请求到上游
            await ForwardWithCircuitBreakerAsync(
                httpContext,
                snapshot,
                requestModel,
                ClientAppKind.Codex,
                "No active provider configured.",
                static (adapter, context, cancellationToken) =>
                    adapter.HandleResponsesAsync(context, cancellationToken));
        }
        finally
        {
            httpContext.Items.Remove(ProtocolAdapterCommon.OutputActivityItemKey);
        }
    }
}
```

## 25.9 生命周期管理

### 25.9.1 启动服务

```csharp
public async Task StartAsync(AppConfig config, CancellationToken cancellationToken = default)
{
    if (_app is not null)
        return;

    _config = config;

    // 检查是否启用
    if (!config.Proxy.Enabled)
    {
        RestoreOriginalConfig();
        SetState(false, "Disabled", ...);
        return;
    }

    // 检查端口可用性
    if (!IsPortAvailable(config.Proxy.Host, config.Proxy.Port))
    {
        RestoreOriginalConfig();
        SetState(false, "Port unavailable", ...);
        return;
    }

    // 创建并配置应用
    var builder = WebApplication.CreateSlimBuilder();
    ConfigureBuilder(builder, config);
    var app = builder.Build();
    ConfigureApp(app);

    // 启动应用
    try
    {
        await app.StartAsync(cancellationToken);
    }
    catch (Exception ex) when (ex is IOException or SocketException)
    {
        await app.DisposeAsync();
        RestoreOriginalConfig();
        SetState(false, "Start failed", ...);
        return;
    }

    // 应用托管客户端配置
    ApplyManagedClientConfig(config);

    _app = app;
    SetState(true, "Running", ...);
}
```

### 25.9.2 停止服务

```csharp
public async Task StopAsync(CancellationToken cancellationToken = default)
{
    await StopRuntimeAsync(restoreOriginal: true, publishStoppedState: true, cancellationToken);
}

private async Task StopRuntimeAsync(
    bool restoreOriginal,
    bool publishStoppedState,
    CancellationToken cancellationToken = default)
{
    if (_app is null)
    {
        if (restoreOriginal)
            RestoreOriginalConfig();
        if (publishStoppedState)
            SetState(false, "Stopped", ...);
        return;
    }

    var app = _app;
    _app = null;

    // 停止并释放应用
    await app.StopAsync(cancellationToken);
    await app.DisposeAsync();

    // 清理状态
    _responseStateStore.Clear();

    // 恢复原始配置
    if (restoreOriginal)
        RestoreOriginalConfig();

    if (publishStoppedState)
        SetState(false, "Stopped", ...);
}
```

### 25.9.3 重启服务

```csharp
public async Task RestartAsync(AppConfig config, CancellationToken cancellationToken = default)
{
    if (!config.Proxy.Enabled)
    {
        await StopRuntimeAsync(restoreOriginal: false, publishStoppedState: false, cancellationToken);
        await StartAsync(config, cancellationToken);
        return;
    }

    SetState(false, "Starting", ...);
    await StopRuntimeAsync(restoreOriginal: false, publishStoppedState: false, cancellationToken);
    await StartAsync(config, cancellationToken);
}
```

### 25.9.4 优雅关闭

```csharp
// 实现 IAsyncDisposable 接口
public sealed class ProxyHostService : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}

// 在 ViewModel 中使用
public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly ProxyHostService _proxyHost;

    public async ValueTask DisposeAsync()
    {
        await _proxyHost.StopAsync();
    }
}

// 在 App.axaml.cs 中确保优雅关闭
public override void OnFrameworkInitializationCompleted()
{
    // ...
}

// 应用关闭时
protected override async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
{
    await _proxyHost.StopAsync();
    base.OnShutdownRequested(sender, e);
}
```

## 25.10 线程安全

### 25.10.1 UI 线程 vs HTTP 线程

```
线程模型：
UI 线程 (STA):
    └── 处理用户输入、渲染 UI
    └── Dispatcher.UIThread

ASP.NET Core 线程池:
    └── 处理 HTTP 请求
    └── 多个工作线程

通信方式:
    ├── 共享状态（需要线程安全）
    ├── 事件（UI 线程回调）
    └── Dispatcher.UIThread.InvokeAsync()
```

### 25.10.2 线程安全的状态管理

```csharp
// 使用线程安全的状态管理
public sealed class ProxyHostService
{
    // 使用 readonly 字段确保线程安全
    private readonly UsageMeter _usageMeter;
    private readonly PriceCalculator _priceCalculator;
    private readonly UsageLogWriter _usageLogWriter;

    // 使用 lock 保护共享状态
    private readonly object _stateLock = new();
    private ProxyRuntimeState _state = new();

    public ProxyRuntimeState State
    {
        get { lock (_stateLock) return _state; }
        private set { lock (_stateLock) _state = value; }
    }

    // 使用事件通知 UI 线程
    public event EventHandler<ProxyRuntimeState>? StateChanged;

    private void SetState(bool isRunning, string statusText, ...)
    {
        State = new ProxyRuntimeState
        {
            IsRunning = isRunning,
            StatusText = statusText,
            // ...
        };
        StateChanged?.Invoke(this, State);
    }
}
```

### 25.10.3 使用 Dispatcher.UIThread

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

// 使用 InvokeAsync 带返回值
var result = await Dispatcher.UIThread.InvokeAsync(() =>
{
    return viewModel.GetCurrentState();
});

// 使用 Post（异步，不等待）
Dispatcher.UIThread.Post(() =>
{
    viewModel.UpdateUI();
}, DispatcherPriority.Normal);
```

### 25.10.4 异步操作最佳实践

```csharp
// 正确的异步操作模式
public async Task StartAsync(AppConfig config, CancellationToken cancellationToken = default)
{
    // 避免在 UI 线程上执行耗时操作
    await Task.Run(async () =>
    {
        // 在后台线程执行
        var builder = WebApplication.CreateSlimBuilder();
        // ...
        await app.StartAsync(cancellationToken);
    }, cancellationToken);

    // 在 UI 线程上更新状态
    await Dispatcher.UIThread.InvokeAsync(() =>
    {
        SetState(true, "Running", ...);
    });
}
```

## 25.11 端口冲突处理

### 25.11.1 检查端口可用性

```csharp
// 检查端口是否可用
private static bool IsPortAvailable(string host, int port)
{
    try
    {
        var listener = new TcpListener(ParseAddress(host), port);
        listener.Start();
        listener.Stop();
        return true;
    }
    catch (SocketException)
    {
        return false;
    }
}

// 解析地址
private static IPAddress ParseAddress(string host)
{
    return IPAddress.TryParse(host, out var address) ? address : IPAddress.Loopback;
}
```

### 25.11.2 自动端口选择

```csharp
// 自动选择可用端口
private int FindAvailablePort(int preferredPort)
{
    if (IsPortAvailable("127.0.0.1", preferredPort))
        return preferredPort;

    // 尝试其他端口
    for (int port = preferredPort + 1; port < preferredPort + 100; port++)
    {
        if (IsPortAvailable("127.0.0.1", port))
            {
            return port;
        }
    }

    throw new InvalidOperationException("No available port found");
}

// 使用随机端口
private int FindRandomAvailablePort()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}
```

### 25.11.3 CodexSwitch 的端口处理

```csharp
// ProxyHostService.cs 中的端口处理
public async Task StartAsync(AppConfig config, CancellationToken cancellationToken = default)
{
    // ...

    // 检查端口可用性
    if (!IsPortAvailable(config.Proxy.Host, config.Proxy.Port))
    {
        var message = $"Port {config.Proxy.Port} on {config.Proxy.Host} is already in use.";
        _codexConfigWriter.RestoreOriginal();
        _claudeCodeConfigWriter.RestoreOriginal();
        SetState(false, "Port unavailable", config.Proxy.Endpoint, provider.Id, provider.Protocol.ToString(), message);
        return;
    }

    // 配置 Kestrel 监听
    options.Listen(ParseAddress(config.Proxy.Host), config.Proxy.Port, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // ...
}
```

## 25.12 错误处理和弹性

### 25.12.1 熔断器模式

```csharp
// 使用熔断器保护上游服务
private async Task ForwardWithCircuitBreakerAsync(
    HttpContext httpContext,
    string? requestModel,
    ClientAppKind clientApp,
    string noProviderMessage,
    Func<IProviderProtocolAdapter, ProviderRequestContext, CancellationToken, Task<ProviderAdapterResult>> invokeAdapter)
{
    var candidates = ResolveRouteCandidates(_config, requestModel, clientApp);
    if (candidates.Count == 0)
    {
        await WriteJsonErrorAsync(httpContext, StatusCodes.Status503ServiceUnavailable, noProviderMessage);
        return;
    }

    var attempts = new List<string>();
    foreach (var selection in candidates)
    {
        var provider = selection.Provider;

        // 检查熔断器状态
        var circuitAttempt = _circuitBreakers.Evaluate(provider.Id, _config.Resilience);
        if (!circuitAttempt.CanAttempt)
        {
            attempts.Add(FormatCircuitOpenAttempt(provider, circuitAttempt));
            continue;
        }

        // 获取访问令牌
        var accessToken = await _providerAuthService.ResolveAccessTokenAsync(
            provider, forceRefresh: false, httpContext.RequestAborted);

        // 创建请求上下文
        var context = new ProviderRequestContext(...);

        // 调用适配器
        var result = await invokeAdapter(adapter, context, httpContext.RequestAborted);

        if (result.Kind == ProviderAdapterResultKind.Success)
        {
            // 成功，报告给熔断器
            _circuitBreakers.ReportSuccess(provider.Id, _config.Resilience);
            return;
        }

        // 失败，报告给熔断器
        if (result.CountsAsCircuitFailure)
            _circuitBreakers.ReportFailure(provider.Id, _config.Resilience);

        attempts.Add(FormatProviderAttempt(provider, result));

        // 检查是否可以重试
        if (result.Kind == ProviderAdapterResultKind.RetryableFailureBeforeResponseStarted)
            continue;

        if (result.Kind == ProviderAdapterResultKind.ResponseAlreadyStartedFailure ||
            result.Kind == ProviderAdapterResultKind.NonRetryableFailure ||
            httpContext.Response.HasStarted)
        {
            return;
        }
    }

    // 所有提供商都不可用
    await WriteAllProvidersUnavailableAsync(httpContext, attempts);
}
```

### 25.12.2 错误响应

```csharp
// 统一的错误响应格式
private static Task WriteJsonErrorAsync(HttpContext context, int statusCode, string message)
{
    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/json";
    var escaped = JsonEncodedText.Encode(message).ToString();
    return context.Response.WriteAsync($"{{\"error\":\"{escaped}\"}}", context.RequestAborted);
}

// 使用示例
await WriteJsonErrorAsync(httpContext, StatusCodes.Status400BadRequest, "Invalid JSON body.");
await WriteJsonErrorAsync(httpContext, StatusCodes.Status401Unauthorized, "Invalid API key.");
await WriteJsonErrorAsync(httpContext, StatusCodes.Status503ServiceUnavailable, "Service unavailable.");
```

### 25.12.3 异常处理

```csharp
// 全局异常处理
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null)
        {
            var message = error.Error.Message;
            var json = JsonSerializer.Serialize(new { error = message });
            await context.Response.WriteAsync(json);
        }
    });
});
```

## 25.13 CodexSwitch 实战：完整的代理服务架构

### 25.13.1 架构概览

```
CodexSwitch 代理架构：

Codex CLI ──HTTP──→ CodexSwitch Proxy ──HTTP──→ 远程 API
                         │
                         ├── 用量记录
                         ├── 协议转换
                         ├── 负载均衡
                         └── 熔断保护
```

### 25.13.2 协议适配器

```csharp
// 协议适配器接口
public interface IProviderProtocolAdapter
{
    ProviderProtocol Protocol { get; }
    Task<ProviderAdapterResult> HandleResponsesAsync(ProviderRequestContext context, CancellationToken ct);
    Task<ProviderAdapterResult> HandleMessagesAsync(ProviderRequestContext context, CancellationToken ct);
}

// OpenAI 协议适配器
public class OpenAiResponsesAdapter : IProviderProtocolAdapter
{
    public ProviderProtocol Protocol => ProviderProtocol.OpenAI;

    public async Task<ProviderAdapterResult> HandleResponsesAsync(
        ProviderRequestContext context,
        CancellationToken ct)
    {
        // 构建 OpenAI 请求
        var payload = ResponsesPayloadBuilder.Build(context);

        // 发送到上游
        var response = await SendToUpstreamAsync(context, payload, ct);

        // 处理响应
        return await ProcessResponseAsync(context, response, ct);
    }
}

// Anthropic 协议适配器
public class AnthropicMessagesAdapter : IProviderProtocolAdapter
{
    public ProviderProtocol Protocol => ProviderProtocol.Anthropic;

    public async Task<ProviderAdapterResult> HandleMessagesAsync(
        ProviderRequestContext context,
        CancellationToken ct)
    {
        // 转换为 Anthropic 格式
        var payload = AnthropicMessagesToResponsesPayloadBuilder.Build(context);

        // 发送到上游
        var response = await SendToUpstreamAsync(context, payload, ct);

        // 处理响应
        return await ProcessResponseAsync(context, response, ct);
    }
}
```

### 25.13.3 托管客户端配置

```csharp
// 应用托管客户端配置
private void ApplyManagedClientConfig(AppConfig config)
{
    var codexProvider = ProviderRoutingResolver.ResolveActiveProvider(config, ClientAppKind.Codex);
    if (codexProvider is null)
        _codexConfigWriter.RestoreOriginal();
    else
        _codexConfigWriter.Apply(config);

    _claudeCodeConfigWriter.Apply(config);
}

// Codex 配置写入器
public class CodexConfigWriter
{
    public void Apply(AppConfig config)
    {
        // 读取现有配置
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".codex", "config.toml");

        var configContent = File.Exists(configPath)
            ? File.ReadAllText(configPath)
            : "";

        // 修改配置
        configContent = UpdateConfigValue(configContent, "api_base_url", config.Proxy.Endpoint);
        configContent = UpdateConfigValue(configContent, "api_key", config.Proxy.InboundApiKey);

        // 写入配置
        File.WriteAllText(configPath, configContent);
    }

    public void RestoreOriginal()
    {
        // 恢复原始配置
        // ...
    }
}
```

## 25.14 最佳实践

### 25.14.1 架构最佳实践

```csharp
// 1. 使用依赖注入管理服务
builder.Services.AddSingleton<ProxyHostService>();
builder.Services.AddSingleton<UsageMeter>();
builder.Services.AddSingleton<PriceCalculator>();
builder.Services.AddSingleton<IProviderProtocolAdapter, OpenAiResponsesAdapter>();
builder.Services.AddSingleton<IProviderProtocolAdapter, AnthropicMessagesAdapter>();

// 2. 使用接口解耦
public interface IProxyHostService
{
    Task StartAsync(AppConfig config, CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    ProxyRuntimeState State { get; }
}

// 3. 配置与逻辑分离
public class ProxySettings
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5000;
}
```

### 25.14.2 性能最佳实践

```csharp
// 1. 使用 CreateSlimBuilder 减少启动时间
var builder = WebApplication.CreateSlimBuilder();

// 2. 禁用不必要的功能
builder.Logging.ClearProviders();

// 3. 配置适当的并发限制
options.Limits.MaxConcurrentConnections = 1024;

// 4. 禁用速率限制（本地服务）
options.Limits.MinRequestBodyDataRate = null;
options.Limits.MinResponseDataRate = null;

// 5. 使用 NoDelay 减少延迟
options.NoDelay = true;
```

### 25.14.3 安全最佳实践

```csharp
// 1. 不添加 Server 头
options.AddServerHeader = false;

// 2. 使用 API 密钥认证
private bool IsAuthorized(HttpContext httpContext)
{
    var apiKey = _config.Proxy.InboundApiKey;
    if (string.IsNullOrWhiteSpace(apiKey))
        return true;

    var header = httpContext.Request.Headers.Authorization.ToString();
    return string.Equals(header, "Bearer " + apiKey, StringComparison.Ordinal);
}

// 3. 限制监听地址
options.Listen(IPAddress.Loopback, port);  // 只监听本地

// 4. 输入验证
var snapshot = await ResponsesRequestSnapshot.ReadAsync(httpContext.Request.Body, ct);
if (snapshot == null)
{
    await WriteJsonErrorAsync(httpContext, 400, "Invalid request");
    return;
}
```

---

## Deep Dive：Kestrel 内部原理

### Kestrel 架构

```
Kestrel 架构：
┌─────────────────────────────────────────┐
│              Kestrel Server              │
├─────────────────────────────────────────┤
│  Connection Listener (Socket)           │
│  ├── HTTP/1.1 Parser                    │
│  ├── HTTP/2 Parser                      │
│  └── TLS Handler                        │
├─────────────────────────────────────────┤
│  Request Processing Pipeline            │
│  ├── Middleware 1                        │
│  ├── Middleware 2                        │
│  └── Endpoint                           │
├─────────────────────────────────────────┤
│  Response Processing                    │
│  ├── Headers                            │
│  ├── Body                               │
│  └── Flush                              │
└─────────────────────────────────────────┘
```

### 请求处理流程

```
HTTP 请求处理流程：
1. Socket 接收数据
2. HTTP 解析器解析请求
3. TLS 解密（如果使用 HTTPS）
4. 创建 HttpContext
5. 执行中间件管道
6. 调用端点处理器
7. 生成响应
8. HTTP 序列化响应
9. TLS 加密（如果使用 HTTPS）
10. Socket 发送数据
```

### 连接管理

```csharp
// Kestrel 使用连接池管理连接
// 连接状态：
// - Idle: 空闲连接
// - Active: 活动连接
// - Draining: 正在关闭的连接

// 连接超时：
// - KeepAliveTimeout: HTTP/1.1 保持连接超时
// - Http2.KeepAlivePingDelay: HTTP/2 Keep-Alive 延迟
// - Http2.KeepAlivePingTimeout: HTTP/2 Keep-Alive 超时
```

## Cross References

- [第 2 章 项目结构与启动流程](02-project-structure.md) — 项目配置
- [第 6 章 MVVM 模式实战](06-mvvm-pattern.md) — ViewModel 中的服务管理
- [第 21 章 调试与诊断](21-debugging.md) — HTTP 请求调试
- [第 22 章 Avalonia 属性系统](22-property-system.md) — 属性与状态管理
- [第 23 章 视觉树与逻辑树](23-visual-logical-tree.md) — 线程模型

## Common Pitfalls

1. **在 UI 线程上执行 HTTP 请求**: 会导致 UI 卡顿，应使用异步操作
2. **不处理端口冲突**: 应用启动失败，应检查端口可用性
3. **不正确释放资源**: HTTP 服务器未正确关闭，应实现 IAsyncDisposable
4. **线程安全问题**: 多线程访问共享状态，应使用锁或线程安全的集合
5. **不处理异常**: 未捕获的异常会导致服务崩溃，应添加异常处理
6. **配置错误**: Kestrel 配置不当会导致性能问题或安全漏洞
7. **不使用源生成器**: JSON 序列化在 AOT 环境下可能失败
8. **中间件顺序错误**: 中间件执行顺序可能影响功能
9. **不处理取消请求**: 长时间运行的操作应支持取消
10. **日志配置不当**: 日志过多或过少都会影响调试

## Try It Yourself

1. **基础练习**: 在 CodexSwitch 中找到 `ProxyHostService`，研究它的完整实现

2. **添加端点**: 添加一个新的 API 端点 `/v1/status`，返回服务状态信息

3. **WebSocket 练习**: 实现一个简单的 WebSocket 回显服务器

4. **中间件开发**: 创建一个请求日志中间件，记录每个请求的方法、路径、耗时

5. **端口处理**: 实现自动端口选择功能，当首选端口不可用时自动选择其他端口

6. **并发测试**: 使用工具测试代理服务器的并发处理能力

7. **错误处理**: 实现全局异常处理中间件，统一处理未捕获的异常

8. **综合项目**: 实现一个完整的本地代理服务器，支持 HTTP/1.1、HTTP/2、WebSocket，包含认证、日志、熔断等功能
