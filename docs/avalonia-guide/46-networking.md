# 41. 网络通信 -- HTTP、WebSocket、gRPC 与实时通信

> **写给零基础的你**：现代应用离不开网络。你需要从服务器获取数据（HTTP）、实时聊天（WebSocket）、或者高效的远程调用（gRPC）。本章教你如何在 Avalonia 应用中实现各种网络通信。

## 41.1 概述

本章涵盖 Avalonia 应用的网络通信方案：

- **HttpClient**：标准 HTTP 请求
- **Refit**：声明式 REST API 客户端
- **WebSocket**：实时双向通信
- **SignalR**：高级实时通信框架
- **gRPC**：高性能 RPC 通信
- **网络状态检测**：在线/离线检测
- **代理配置**：HTTP/SOCKS 代理

## 41.2 HttpClient 基础

### 41.2.1 推荐用法：IHttpClientFactory

```csharp
// 注册 HttpClient（在依赖注入中）
services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 使用
public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("api");
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }
}
```

### 41.2.2 进度报告

```csharp
public async Task DownloadWithProgress(string url, IProgress<double> progress)
{
    using var response = await _httpClient.GetAsync(url,
        HttpCompletionOption.ResponseHeadersRead);

    var totalBytes = response.Content.Headers.ContentLength ?? -1;
    var totalRead = 0L;

    await using var stream = await response.Content.ReadAsStreamAsync();
    await using var fileStream = File.Create("download.zip");
    var buffer = new byte[8192];
    int bytesRead;

    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
    {
        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
        totalRead += bytesRead;

        if (totalBytes > 0)
        {
            progress.Report((double)totalRead / totalBytes * 100);
        }
    }
}
```

### 41.2.3 取消请求

```csharp
public class CancellableRequest
{
    private CancellationTokenSource? _cts;

    public async Task<string> FetchDataAsync()
    {
        _cts = new CancellationTokenSource();
        _cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 秒超时

        try
        {
            return await _httpClient.GetStringAsync("https://api.example.com/data",
                _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // 请求被取消
            return "";
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }
}
```

## 41.3 Refit 声明式 API 客户端

### 41.3.1 定义 API 接口

```csharp
// NuGet: Refit
using Refit;

public interface IMyApi
{
    [Get("/users/{id}")]
    Task<User> GetUser(int id);

    [Get("/users")]
    Task<List<User>> GetUsers([Query] int page = 1, [Query] int pageSize = 20);

    [Post("/users")]
    Task<User> CreateUser([Body] CreateUserRequest request);

    [Put("/users/{id}")]
    Task<User> UpdateUser(int id, [Body] UpdateUserRequest request);

    [Delete("/users/{id}")]
    Task DeleteUser(int id);

    [Multipart]
    [Post("/upload")]
    Task<UploadResult> UploadFile(StreamPart file);
}
```

### 41.3.2 配置和使用

```csharp
// 注册
services.AddRefitClient<IMyApi>(new RefitSettings
{
    ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    })
})
.ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"));

// 使用
public class UserViewModel
{
    private readonly IMyApi _api;

    public UserViewModel(IMyApi api) => _api = api;

    [RelayCommand]
    private async Task LoadUser(int id)
    {
        var user = await _api.GetUser(id);
        // 更新 UI
    }
}
```

### 41.3.3 错误处理

```csharp
try
{
    var user = await _api.GetUser(123);
}
catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    // 404 处理
}
catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
    // 401 处理 - 刷新 Token
}
catch (ApiException ex)
{
    // 其他 API 错误
    var content = ex.Content;
    var statusCode = ex.StatusCode;
}
```

## 41.4 WebSocket 实时通信

### 41.4.1 基本 WebSocket 客户端

```csharp
public class WebSocketClient : IDisposable
{
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;

    public event Action<string>? MessageReceived;
    public event Action? Connected;
    public event Action? Disconnected;

    public async Task ConnectAsync(string url)
    {
        _ws = new ClientWebSocket();
        _cts = new CancellationTokenSource();

        await _ws.ConnectAsync(new Uri(url), _cts.Token);
        Connected?.Invoke();

        _ = ReceiveLoop();
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[4096];
        try
        {
            while (_ws?.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(
                    new ArraySegment<byte>(buffer), _cts!.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Disconnected?.Invoke();
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                MessageReceived?.Invoke(message);
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { Disconnected?.Invoke(); }
    }

    public async Task SendAsync(string message)
    {
        if (_ws?.State != WebSocketState.Open) return;

        var bytes = Encoding.UTF8.GetBytes(message);
        await _ws.SendAsync(new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text, true, _cts!.Token);
    }

    public async Task DisconnectAsync()
    {
        if (_ws?.State == WebSocketState.Open)
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
                "Client closing", CancellationToken.None);
        }
        Disconnected?.Invoke();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _ws?.Dispose();
        _cts?.Dispose();
    }
}
```

### 41.4.2 自动重连

```csharp
public class ResilientWebSocket : IDisposable
{
    private readonly WebSocketClient _client = new();
    private readonly string _url;
    private readonly TimeSpan _reconnectDelay;
    private bool _shouldReconnect = true;

    public event Action<string>? MessageReceived;

    public ResilientWebSocket(string url, TimeSpan? reconnectDelay = null)
    {
        _url = url;
        _reconnectDelay = reconnectDelay ?? TimeSpan.FromSeconds(5);

        _client.MessageReceived += msg => MessageReceived?.Invoke(msg);
        _client.Disconnected += OnDisconnected;
    }

    private async void OnDisconnected()
    {
        if (!_shouldReconnect) return;

        await Task.Delay(_reconnectDelay);
        try
        {
            await _client.ConnectAsync(_url);
        }
        catch
        {
            // 重连失败，继续等待
            OnDisconnected();
        }
    }

    public Task ConnectAsync() => _client.ConnectAsync(_url);
    public Task SendAsync(string msg) => _client.SendAsync(msg);

    public void Dispose()
    {
        _shouldReconnect = false;
        _client.Dispose();
    }
}
```

## 41.5 SignalR 集成

### 41.5.1 安装配置

```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="10.*" />
```

### 41.5.2 SignalR 客户端

```csharp
public class SignalRService : IAsyncDisposable
{
    private HubConnection? _connection;

    public event Action<string, string>? MessageReceived;
    public event Action<string>? UserJoined;
    public event Action<string>? UserLeft;

    public async Task StartAsync(string hubUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        // 注册服务端调用的方法
        _connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            MessageReceived?.Invoke(user, message);
        });

        _connection.On<string>("UserJoined", user =>
        {
            UserJoined?.Invoke(user);
        });

        _connection.On<string>("UserLeft", user =>
        {
            UserLeft?.Invoke(user);
        });

        // 连接状态变化
        _connection.Reconnecting += error =>
        {
            Console.WriteLine("正在重连...");
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            Console.WriteLine($"已重连: {connectionId}");
            return Task.CompletedTask;
        };

        _connection.Closed += error =>
        {
            Console.WriteLine("连接已关闭");
            return Task.CompletedTask;
        };

        await _connection.StartAsync();
    }

    public async Task SendMessage(string user, string message)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("SendMessage", user, message);
        }
    }

    public async Task JoinGroup(string groupName)
    {
        await _connection!.InvokeAsync("JoinGroup", groupName);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}
```

### 41.5.3 在 ViewModel 中使用

```csharp
public partial class ChatViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly SignalRService _signalR;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    [ObservableProperty]
    private string _inputText = "";

    public ChatViewModel(SignalRService signalR)
    {
        _signalR = signalR;

        _signalR.MessageReceived += (user, message) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                Messages.Add(new ChatMessage(user, message));
            });
        };

        _ = _signalR.StartAsync("https://localhost:5000/chat");
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;

        await _signalR.SendMessage("Me", InputText);
        InputText = "";
    }

    public async ValueTask DisposeAsync()
    {
        await _signalR.DisposeAsync();
    }
}
```

## 41.6 gRPC 通信

### 41.6.1 安装配置

```xml
<PackageReference Include="Grpc.Net.Client" Version="2.*" />
<PackageReference Include="Google.Protobuf" Version="3.*" />
<PackageReference Include="Grpc.Tools" Version="2.*" PrivateAssets="All" />

<!-- Proto 文件 -->
<Protobuf Include="Protos\myservice.proto" GrpcServices="Client" />
```

### 41.6.2 定义 Proto 文件

```protobuf
// Protos/myservice.proto
syntax = "proto3";
option csharp_namespace = "MyApp.Grpc";

service MyService {
    rpc GetItem (GetItemRequest) returns (Item);
    rpc ListItems (ListItemsRequest) returns (stream Item);  // 服务端流
    rpc CreateItem (stream Item) returns (CreateItemResponse); // 客户端流
}

message GetItemRequest {
    int32 id = 1;
}

message Item {
    int32 id = 1;
    string name = 2;
    string description = 3;
}

message ListItemsRequest {
    int32 page = 1;
    int32 page_size = 2;
}

message CreateItemResponse {
    int32 id = 1;
}
```

### 41.6.3 gRPC 客户端使用

```csharp
public class GrpcService
{
    private readonly MyService.MyServiceClient _client;

    public GrpcService(string serverUrl)
    {
        var channel = GrpcChannel.ForAddress(serverUrl);
        _client = new MyService.MyServiceClient(channel);
    }

    // 一元调用
    public async Task<Item> GetItemAsync(int id)
    {
        return await _client.GetItemAsync(new GetItemRequest { Id = id });
    }

    // 服务端流
    public async IAsyncEnumerable<Item> ListItemsAsync()
    {
        using var call = _client.ListItems(new ListItemsRequest { Page = 1, PageSize = 100 });

        await foreach (var item in call.ResponseStream.ReadAllAsync())
        {
            yield return item;
        }
    }

    // 客户端流
    public async Task<int> CreateItemsAsync(IEnumerable<Item> items)
    {
        using var call = _client.CreateItem();

        foreach (var item in items)
        {
            await call.RequestStream.WriteAsync(item);
        }

        await call.RequestStream.CompleteAsync();
        var response = await call;
        return response.Id;
    }
}
```

## 41.7 网络状态检测

```csharp
public class NetworkMonitor : IDisposable
{
    private readonly DispatcherTimer _timer;

    public bool IsOnline { get; private set; } = true;
    public event Action<bool>? StatusChanged;

    public NetworkMonitor(TimeSpan? checkInterval = null)
    {
        _timer = new DispatcherTimer
        {
            Interval = checkInterval ?? TimeSpan.FromSeconds(30)
        };
        _timer.Tick += CheckNetwork;
    }

    private async void CheckNetwork(object? sender, EventArgs e)
    {
        var wasOnline = IsOnline;
        IsOnline = await CheckConnectivity();

        if (wasOnline != IsOnline)
        {
            Dispatcher.UIThread.Post(() => StatusChanged?.Invoke(IsOnline));
        }
    }

    private async Task<bool> CheckConnectivity()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync("https://www.google.com/generate_204");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();
    public void Dispose() => _timer.Stop();
}
```

## 41.8 代理配置

```csharp
public class ProxySettings
{
    public string? Host { get; set; }
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public ProxyType Type { get; set; } = ProxyType.Http;
}

public enum ProxyType { Http, Socks4, Socks5 }

public static class HttpClientFactory
{
    public static HttpClient CreateWithProxy(ProxySettings proxy)
    {
        var handler = new HttpClientHandler();

        if (proxy.Host != null)
        {
            handler.Proxy = new WebProxy(proxy.Host, proxy.Port)
            {
                Credentials = proxy.Username != null
                    ? new NetworkCredential(proxy.Username, proxy.Password)
                    : null
            };
            handler.UseProxy = true;
        }

        return new HttpClient(handler);
    }
}
```

## 41.9 Cross References

- **第 25 章**：ASP.NET Core 集成（服务端 API 实现）
- **第 34 章**：本地数据存储（离线数据缓存）

## 41.10 Common Pitfalls

1. **HttpClient 生命周期**：不要频繁创建和销毁 HttpClient，使用 IHttpClientFactory
2. **WebSocket 超时**：需要定期发送 Ping 保持连接
3. **SignalR 重连策略**：配置自动重连，但要限制重试次数
4. **gTLS 证书**：开发环境可能需要忽略证书验证
5. **网络超时**：所有网络操作都应该设置超时
6. **线程安全**：网络回调可能在非 UI 线程

## 41.11 Try It Yourself

1. 使用 Refit 创建一个 REST API 客户端
2. 实现一个 WebSocket 实时聊天客户端
3. 使用 SignalR 创建一个实时通知系统
4. 实现网络状态监控，离线时显示提示
