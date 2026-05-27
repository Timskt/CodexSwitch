# 37. .NET 生态集成：Host、日志与 IoC

> **写给零基础的你**：Host 就像"应用程序的管家"，帮你管理启动、关闭、配置、日志等所有基础设施。IoC（控制反转）容器就像"自动装配工厂"——你只需要声明"我需要什么"，它自动帮你创建和组装。日志系统就像"飞行记录仪"，记录程序运行中的每一个关键事件，出问题时可以回溯排查。

## 37.1 概述

### 37.1.1 为什么需要 Host

当一个应用只有几行代码时，手动管理一切没有问题。但真实项目中，你需要处理：

- **配置管理**：从 JSON 文件、环境变量、命令行参数等多种来源读取配置
- **依赖注入**：服务之间的依赖关系越来越复杂，手动创建会变成噩梦
- **日志记录**：需要统一的日志框架，支持多种输出目标
- **生命周期管理**：应用启动时初始化资源，关闭时优雅释放
- **后台任务**：定时检查更新、清理缓存、同步数据等
- **HTTP 客户端管理**：连接池、重试策略、超时控制

.NET Generic Host 就是微软为了解决这些问题提供的"一站式管家"。

### 37.1.2 .NET 生态系统概览

微软围绕 `Microsoft.Extensions.*` 构建了一套完整的基础设施库：

| 包名 | 用途 | 核心接口 |
|------|------|---------|
| `Microsoft.Extensions.Hosting` | 应用宿主 | `IHost`, `IHostBuilder` |
| `Microsoft.Extensions.DependencyInjection` | 依赖注入 | `IServiceCollection`, `IServiceProvider` |
| `Microsoft.Extensions.Configuration` | 配置系统 | `IConfiguration`, `IConfigurationBuilder` |
| `Microsoft.Extensions.Logging` | 日志抽象 | `ILogger<T>`, `ILoggerFactory` |
| `Microsoft.Extensions.Options` | 强类型配置 | `IOptions<T>`, `IOptionsMonitor<T>` |
| `Microsoft.Extensions.Http` | HTTP 客户端工厂 | `IHttpClientFactory` |
| `Microsoft.Extensions.Caching.Memory` | 内存缓存 | `IMemoryCache` |
| `Microsoft.Extensions.Hosting.Abstractions` | 托管服务 | `IHostedService`, `BackgroundService` |
| `Microsoft.Extensions.Diagnostics.HealthChecks` | 健康检查 | `IHealthCheck` |

这些库不依赖 ASP.NET Core，可以在任何 .NET 应用中使用——包括 Avalonia 桌面应用。

### 37.1.3 Avalonia 与 .NET 生态的集成方式

Avalonia 11+ 提供了原生的 DI 支持。在 `AppBuilder` 上可以通过 `WithServices()` 方法注册服务：

```csharp
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
        .WithServices(services =>
        {
            // 在这里注册服务
            services.AddSingleton<IMyService, MyService>();
        });
```

但这只是最基础的集成。对于复杂应用，你通常需要完整的 Generic Host。

## 37.2 .NET Generic Host 详解

### 37.2.1 IHostBuilder vs IHost

这两个接口是 Host 系统的核心：

```
IHostBuilder（建造者）
    │
    ├── ConfigureAppConfiguration()    ← 配置源
    ├── ConfigureServices()             ← 注册服务
    ├── ConfigureLogging()              ← 日志配置
    └── UseConsoleLifetime()            ← 生命周期
    │
    ▼
IHost（成品）
    │
    ├── Services: IServiceProvider      ← 服务容器
    ├── StartAsync()                    ← 启动
    └── StopAsync()                     ← 关闭
```

**IHostBuilder** 是"蓝图"，用来配置应用的各种行为。**IHost** 是"成品"，用来实际运行应用。

### 37.2.2 Host.CreateDefaultBuilder() 做了什么

`Host.CreateDefaultBuilder()` 是一个快捷方法，它自动帮你做了很多事情：

```csharp
// 这一行代码等价于以下所有配置：
var host = Host.CreateDefaultBuilder(args).Build();

// 实际上它做了这些事：
var host = new HostBuilder()
    // 1. 配置系统
    .ConfigureAppConfiguration((context, config) =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory());
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
            optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();

        if (args is { Length: > 0 })
            config.AddCommandLine(args);
    })

    // 2. 日志系统
    .ConfigureLogging((context, logging) =>
    {
        logging.AddConfiguration(context.Configuration.GetSection("Logging"));
        logging.AddConsole();
        logging.AddDebug();
        logging.AddEventSourceLogger();
    })

    // 3. 依赖注入（默认服务）
    .UseDefaultServiceProvider((context, options) =>
    {
        options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
        options.ValidateOnBuild = true;
    })

    .Build();
```

### 37.2.3 配置（Configuration）

配置系统支持多种数据源，后添加的源会覆盖先添加的：

```csharp
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // 清除默认源（如果需要）
        config.Sources.Clear();

        // 1. JSON 文件（最常用）
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
            optional: true);

        // 2. INI 文件
        config.AddIniFile("config.ini", optional: true);

        // 3. XML 文件
        config.AddXmlFile("config.xml", optional: true);

        // 4. 环境变量（适合容器化部署）
        config.AddEnvironmentVariables(prefix: "MYAPP_");

        // 5. 命令行参数（适合 CLI 场景）
        if (args is { Length: > 0 })
            config.AddCommandLine(args);

        // 6. 内存字典（适合测试或动态配置）
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["App:Name"] = "MyApp",
            ["App:Version"] = "1.0.0"
        });

        // 7. 用户机密（开发环境，不要在生产环境使用）
        if (context.HostingEnvironment.IsDevelopment())
            config.AddUserSecrets<Program>();
    });
```

### 37.2.4 依赖注入（DI）

```csharp
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // 从配置读取
        var config = context.Configuration;

        // 注册应用服务
        services.AddSingleton<AppPaths>();
        services.AddSingleton<ConfigurationStore>();
        services.AddSingleton<ProxyHostService>();

        // 注册配置绑定
        services.Configure<ProxySettings>(config.GetSection("Proxy"));
        services.Configure<NetworkSettings>(config.GetSection("Network"));
    });
```

### 37.2.5 日志（Logging）

```csharp
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((context, logging) =>
    {
        // 清除默认日志提供程序
        logging.ClearProviders();

        // 设置最低日志级别
        logging.SetMinimumLevel(LogLevel.Information);

        // 添加控制台日志
        logging.AddConsole(options =>
        {
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            options.IncludeScopes = true;
        });

        // 添加调试日志（输出到 IDE 的输出窗口）
        logging.AddDebug();

        // 按类别设置不同级别
        logging.AddFilter("Microsoft", LogLevel.Warning);
        logging.AddFilter("System", LogLevel.Warning);
        logging.AddFilter("MyApp.Services", LogLevel.Debug);
    });
```

### 37.2.6 生命周期（Lifetime）

```csharp
var builder = Host.CreateDefaultBuilder(args)
    .UseConsoleLifetime(options =>
    {
        options.SuppressStatusMessages = true;  // 不输出启动/关闭消息
    });

// 或者使用 Windows Service 生命周期
var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService();  // 作为 Windows 服务运行

// 或者使用 Systemd 生命周期（Linux）
var builder = Host.CreateDefaultBuilder(args)
    .UseSystemd();
```

### 37.2.7 在 Avalonia 中使用 Host

将 Generic Host 与 Avalonia 结合有多种方式，下面逐一介绍。

## 37.3 Avalonia 集成 Host 的方式

### 37.3.1 方式 1：AppBuilder + 内置 DI（简单模式）

Avalonia 11+ 原生支持通过 `WithServices()` 注册服务：

```csharp
// Program.cs
using Avalonia;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .WithServices(services =>
            {
                // 基础服务注册
                services.AddSingleton<AppPaths>();
                services.AddSingleton<ConfigurationStore>();
                services.AddSingleton<I18nService>();

                // 如果有 HTTP 客户端需求
                services.AddHttpClient();
            });
}
```

在 App 中获取服务：

```csharp
// App.axaml.cs
public partial class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        // 从 Avalonia 的服务提供者中获取服务
        var services = AvaloniaLocator.Current.GetService<IServiceProvider>();
        var paths = services?.GetService<AppPaths>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(paths)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

**优点**：简单直接，无需额外 NuGet 包。
**缺点**：没有 Host 的生命周期管理、配置系统、日志集成等功能。

### 37.3.2 方式 2：Host 包裹 Avalonia（推荐模式）

用 Generic Host 包裹整个 Avalonia 应用，获得完整的基础设施支持：

```csharp
// Program.cs
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

sealed class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        // 检查是否是特殊的启动模式（如 CodexSwitch 的 bootstrap 模式）
        if (HandleSpecialMode(args))
            return;

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // 注册所有应用服务
                ConfigureServices(context, services);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddDebug();
            })
            .Build();

        // 启动 Host（初始化所有 IHostedService）
        await host.StartAsync();

        try
        {
            // 获取服务提供者并启动 Avalonia
            var services = host.Services;

            BuildAvaloniaApp(services)
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            // 优雅关闭 Host
            await host.StopAsync();
            host.Dispose();
        }
    }

    public static AppBuilder BuildAvaloniaApp(IServiceProvider services)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .WithServices(services);  // 将 Host 的服务提供者传给 Avalonia

    private static void ConfigureServices(
        HostBuilderContext context,
        IServiceCollection services)
    {
        var config = context.Configuration;

        // 基础设施服务
        services.AddSingleton<AppPaths>();
        services.AddSingleton<ConfigurationStore>();

        // 业务服务
        services.AddSingleton<PriceCalculator>();
        services.AddSingleton<UsageMeter>();
        services.AddSingleton<UsageLogWriter>();
        services.AddSingleton<UsageLogReader>();
        services.AddSingleton<CodexConfigWriter>();
        services.AddSingleton<ClaudeCodeConfigWriter>();
        services.AddSingleton<ProviderAuthService>();

        // HTTP 客户端
        services.AddHttpClient("upstream", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // 后台服务
        services.AddHostedService<AutoUpdateCheckService>();
        services.AddHostedService<UsageLogCleanupService>();

        // 配置绑定
        services.Configure<ProxySettings>(config.GetSection("Proxy"));
        services.Configure<NetworkSettings>(config.GetSection("Network"));
    }

    private static bool HandleSpecialMode(string[] args)
    {
        // 类似 CodexSwitch 的 bootstrap 模式处理
        return false;
    }
}
```

在 App 中注入服务：

```csharp
// App.axaml.cs
public partial class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 从 Avalonia 的服务提供者获取所需服务
            var appPaths = GetService<AppPaths>();
            var configStore = GetService<ConfigurationStore>();
            var proxyService = GetService<ProxyHostService>();
            var logger = GetService<ILogger<App>>();

            logger?.LogInformation("Application starting...");

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(appPaths, configStore, proxyService)
            };

            desktop.ShutdownRequested += async (_, _) =>
            {
                logger?.LogInformation("Application shutting down...");
                if (desktop.MainWindow?.DataContext is IAsyncDisposable disposable)
                    await disposable.DisposeAsync();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static T? GetService<T>() where T : class
    {
        // 从 Avalonia 的服务定位器获取
        return AvaloniaLocator.Current?.GetService<T>();
    }
}
```

**优点**：完整的 Host 功能——配置、日志、DI、生命周期管理。
**缺点**：代码稍多，需要理解 Host 的生命周期。

### 37.3.3 方式 3：混合模式（实用模式）

在不完全使用 Host 的情况下，手动组装需要的组件：

```csharp
// Program.cs
using Avalonia;
using Microsoft.Extensions.DependencyInjection;

sealed class Program
{
    // 全局服务提供者（简单但有效）
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        // 手动构建服务容器
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .WithServices(Services);

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<AppPaths>();
        services.AddSingleton<ConfigurationStore>();
        services.AddHttpClient();
    }
}
```

**优点**：灵活，不绑定 Host 生命周期。
**缺点**：没有自动的生命周期管理，需要手动处理释放。

### 37.3.4 各方式对比

| 特性 | 方式 1：WithServices | 方式 2：Host 包裹 | 方式 3：混合模式 |
|------|---------------------|-------------------|-----------------|
| 复杂度 | 低 | 高 | 中 |
| DI 支持 | 有 | 完整 | 有 |
| 配置系统 | 无 | 完整 | 手动 |
| 日志集成 | 仅 LogToTrace | 完整 | 手动 |
| 托管服务 | 无 | 有 | 无 |
| 生命周期管理 | 无 | 完整 | 手动 |
| 适合场景 | 小型应用 | 中大型应用 | 中型应用 |

**推荐**：对于有后台服务、复杂配置、多日志目标的项目，使用**方式 2**。对于简单的小工具，**方式 1** 就够了。

## 37.4 依赖注入（IoC）深入

### 37.4.1 IServiceCollection 详解

`IServiceCollection` 是服务注册的核心接口。它有四种主要的注册方式，对应四种生命周期：

```csharp
services.AddTransient<IMyService, MyService>();   // 每次请求创建新实例
services.AddScoped<IMyService, MyService>();       // 每个作用域创建一个实例
services.AddSingleton<IMyService, MyService>();    // 全局只创建一个实例
services.AddHostedService<MyBackgroundService>();   // 由 Host 管理的后台服务
```

**生命周期图示**：

```
请求 1 ──┐
请求 2 ──┼──► AddTransient：每次都 new
请求 3 ──┘

Scope A ──────┐
  请求 1 ─────┤  Scope A 内共用一个实例
  请求 2 ─────┘
Scope B ──────┐
  请求 3 ─────┤  Scope B 内共用另一个实例
  请求 4 ─────┘

整个应用 ──────────────► AddSingleton：全局共用一个实例
```

**使用建议**：
- **Singleton**：无状态的服务（如 `ConfigurationStore`、`AppPaths`）、昂贵的资源（如 `HttpClient`）
- **Scoped**：有请求/操作上下文的服务（在桌面应用中较少使用）
- **Transient**：轻量级、有状态的服务

### 37.4.2 服务注册最佳实践

**接口注册 vs 实现注册**：

```csharp
// 推荐：通过接口注册（松耦合，易于测试和替换）
services.AddSingleton<IUserService, UserService>();
services.AddSingleton<INotificationService, DesktopNotificationService>();

// 也可以：直接注册实现类（简单场景）
services.AddSingleton<AppPaths>();  // 没有接口，直接注册
services.AddSingleton<ConfigurationStore>();
```

**工厂注册**：

```csharp
// 当构造过程需要额外逻辑时使用工厂
services.AddSingleton<IConnectionManager>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var logger = provider.GetRequiredService<ILogger<ConnectionManager>>();
    var maxRetries = config.GetValue<int>("Connection:MaxRetries", 3);
    return new ConnectionManager(logger, maxRetries);
});

// 异步工厂（需要 IHostedService 配合）
services.AddSingleton<ProxyHostService>(provider =>
{
    var meter = provider.GetRequiredService<UsageMeter>();
    var calculator = provider.GetRequiredService<PriceCalculator>();
    var writer = provider.GetRequiredService<UsageLogWriter>();
    var codexWriter = provider.GetRequiredService<CodexConfigWriter>();
    var claudeWriter = provider.GetRequiredService<ClaudeCodeConfigWriter>();
    var authService = provider.GetRequiredService<ProviderAuthService>();
    var adapters = provider.GetServices<IProviderProtocolAdapter>();
    return new ProxyHostService(meter, calculator, writer,
        codexWriter, claudeWriter, authService, adapters);
});
```

**泛型注册**：

```csharp
// 注册开放泛型
services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));

// 使用时自动解析
public class UserService
{
    private readonly IRepository<User> _userRepo;

    public UserService(IRepository<User> userRepo)
    {
        _userRepo = userRepo;
    }
}
```

**Keyed Services（.NET 8+）**：

```csharp
// 按 key 注册同一接口的不同实现
services.AddKeyedSingleton<INotificationSender>("email", new EmailSender());
services.AddKeyedSingleton<INotificationSender>("sms", new SmsSender());
services.AddKeyedSingleton<INotificationSender>("push", new PushSender());

// 注入时指定 key
public class NotificationService
{
    private readonly INotificationSender _emailSender;

    public NotificationService(
        [FromKeyedServices("email")] INotificationSender emailSender)
    {
        _emailSender = emailSender;
    }
}
```

### 37.4.3 服务解析

**构造函数注入（推荐）**：

```csharp
// DI 容器自动解析构造函数中的所有参数
public class MainWindowViewModel
{
    private readonly AppPaths _paths;
    private readonly ConfigurationStore _store;
    private readonly ILogger<MainWindowViewModel> _logger;

    public MainWindowViewModel(
        AppPaths paths,
        ConfigurationStore store,
        ILogger<MainWindowViewModel> logger)
    {
        _paths = paths;         // 自动注入
        _store = store;         // 自动注入
        _logger = logger;       // 自动注入
    }
}
```

**手动解析（不推荐，但有时必要）**：

```csharp
// 从服务提供者手动获取服务
var service = serviceProvider.GetRequiredService<IMyService>();
var optional = serviceProvider.GetService<IOptionalService>();  // 可以为 null

// 创建作用域并解析 Scoped 服务
using var scope = serviceProvider.CreateScope();
var scoped = scope.ServiceProvider.GetRequiredService<IScopedService>();
```

**服务定位器模式（反模式，避免使用）**：

```csharp
// 反模式：到处传递 IServiceProvider
public class BadExample
{
    private readonly IServiceProvider _services;

    public BadExample(IServiceProvider services)
    {
        _services = services;  // 服务定位器——不知道这个类到底依赖什么
    }

    public void DoWork()
    {
        // 运行时才知道需要什么服务，编译器无法检查
        var svc = _services.GetRequiredService<ISomeService>();
    }
}

// 正确做法：明确声明依赖
public class GoodExample
{
    private readonly ISomeService _someService;

    public GoodExample(ISomeService someService)
    {
        _someService = someService;  // 依赖一目了然
    }
}
```

### 37.4.4 服务作用域和生命周期

**作用域的创建和销毁**：

```csharp
// 在 ASP.NET Core 中，每个 HTTP 请求自动创建一个 Scope
// 在桌面应用中，你需要手动管理 Scope

public class DocumentService
{
    private readonly IServiceProvider _services;

    public DocumentService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task SaveDocumentAsync(Document doc)
    {
        // 为这个操作创建一个独立的作用域
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var validator = scope.ServiceProvider.GetRequiredService<IDocumentValidator>();

        await validator.ValidateAsync(doc);
        await repo.SaveAsync(doc);

        // scope 释放时，所有 Scoped 服务也会被释放
    }
}
```

**Scoped 服务的陷阱**：

```csharp
// 错误：在 Singleton 中注入 Scoped 服务
public class SingletonService  // 生命周期：整个应用
{
    private readonly IScopedService _scoped;  // 生命周期：某个作用域

    public SingletonService(IScopedService scoped)
    {
        _scoped = scoped;  // 问题：作用域结束后，_scoped 引用的是已释放的对象！
    }
}

// 正确做法：使用 IServiceScopeFactory 创建作用域
public class SingletonService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SingletonService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void DoWork()
    {
        using var scope = _scopeFactory.CreateScope();
        var scoped = scope.ServiceProvider.GetRequiredService<IScopedService>();
        scoped.Execute();
    }
}
```

### 37.4.5 服务释放和内存管理

```csharp
// DI 容器会自动释放它创建的 IDisposable 服务
// 但要注意：手动创建的对象需要自己管理

// 自动释放（容器管理）
services.AddSingleton<IMyService, MyService>();  // MyService 实现 IDisposable
// 应用关闭时，容器会自动调用 MyService.Dispose()

// 手动创建的不受容器管理
var myService = new MyService();  // 你需要自己调用 myService.Dispose()
```

## 37.5 Microsoft.Extensions.Logging 详解

### 37.5.1 ILogger&lt;T&gt; 接口

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public void DoWork()
    {
        // T 的全名会成为日志的"类别"（Category）
        // 例如：CodexSwitch.Services.MyService
    }
}
```

### 37.5.2 日志级别

.NET 定义了 7 个日志级别，从最详细到最关键：

| 级别 | 值 | 用途 | 示例 |
|------|---|------|------|
| Trace | 0 | 最详细的跟踪信息 | 进入/退出函数、变量值 |
| Debug | 1 | 调试信息 | HTTP 请求详情、SQL 查询 |
| Information | 2 | 一般信息 | 应用启动、用户登录、操作完成 |
| Warning | 3 | 警告 | 配置缺失使用默认值、重试 |
| Error | 4 | 错误 | 操作失败但应用可继续 |
| Critical | 5 | 严重错误 | 数据库连接丢失、应用即将崩溃 |
| None | 6 | 禁用日志 | — |

```csharp
public void ProcessRequest(Request request)
{
    _logger.LogTrace("Entering ProcessRequest with {RequestId}", request.Id);

    _logger.LogDebug("Processing request: {Method} {Path}", request.Method, request.Path);

    _logger.LogInformation("Request {RequestId} processed successfully", request.Id);

    _logger.LogWarning("Request {RequestId} took {Elapsed}ms, exceeding threshold",
        request.Id, elapsed);

    _logger.LogError(exception, "Failed to process request {RequestId}", request.Id);

    _logger.LogCritical(exception, "Database connection lost, cannot process request {RequestId}",
        request.Id);
}
```

### 37.5.3 结构化日志

结构化日志是现代日志系统的核心概念——日志参数不是简单地拼接成字符串，而是保留为结构化的键值对：

```csharp
// 不好的做法：字符串拼接
_logger.LogInformation("User " + userId + " logged in from " + ip);
// 输出：User 123 logged in from 192.168.1.1
// 日志系统只知道这是一个字符串，无法按 userId 搜索

// 好的做法：结构化日志
_logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ip);
// 输出：User 123 logged in from 192.168.1.1
// 日志系统保留了结构：UserId=123, IpAddress=192.168.1.1
// 可以搜索 "UserId=123 的所有登录记录"

// 复杂对象也可以结构化
_logger.LogInformation("Provider {ProviderId} changed to {Provider}", provider.Id, provider);
// provider 对象会被序列化为 JSON
```

### 37.5.4 日志事件 ID

事件 ID 可以帮助你快速定位和过滤特定类型的日志：

```csharp
// 定义事件 ID 常量
public static class LogEventIds
{
    public static readonly EventId AppStarted = new(1000, nameof(AppStarted));
    public static readonly EventId AppStopped = new(1001, nameof(AppStopped));
    public static readonly EventId ProxyStarted = new(2000, nameof(ProxyStarted));
    public static readonly EventId ProxyFailed = new(2001, nameof(ProxyFailed));
    public static readonly EventId ConfigLoaded = new(3000, nameof(ConfigLoaded));
    public static readonly EventId ProviderChanged = new(4000, nameof(ProviderChanged));
}

// 使用事件 ID
_logger.LogInformation(LogEventIds.AppStarted, "Application started");
_logger.LogError(LogEventIds.ProxyFailed, exception, "Proxy failed to start on port {Port}", port);
```

### 37.5.5 日志作用域（Scope）

作用域可以将一组日志关联在一起，方便追踪某个操作的完整流程：

```csharp
// 创建作用域——后续所有日志都会携带 RequestId
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["RequestId"] = requestId,
    ["UserId"] = userId
}))
{
    _logger.LogInformation("Processing started");

    await ProcessStep1Async();
    await ProcessStep2Async();

    _logger.LogInformation("Processing completed");
}

// 作用域内的所有日志都会包含 RequestId 和 UserId
// [RequestId:abc-123, UserId:42] Processing started
// [RequestId:abc-123, UserId:42] Step 1 completed
// [RequestId:abc-123, UserId:42] Step 2 completed
// [RequestId:abc-123, UserId:42] Processing completed
```

### 37.5.6 日志性能优化（LoggerMessage.Define）

对于高频调用的日志，使用 `LoggerMessage.Define` 可以避免装箱和字符串格式化的开销：

```csharp
// 静态定义日志消息模板
public static class LogMessages
{
    private static readonly Action<ILogger, string, Exception?> _proxyStarted =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            LogEventIds.ProxyStarted,
            "Proxy started on {Endpoint}");

    private static readonly Action<ILogger, string, int, Exception?> _requestReceived =
        LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(0, "RequestReceived"),
            "Received {Method} request, body length: {Length}");

    // .NET 8+ 高性能源生成方式
    // [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} logged in")]
    // public static partial void UserLoggedIn(this ILogger logger, string userId);

    public static void ProxyStarted(this ILogger logger, string endpoint)
        => _proxyStarted(logger, endpoint, null);

    public static void RequestReceived(this ILogger logger, string method, int length)
        => _requestReceived(logger, method, length, null);
}
```

.NET 8+ 的源生成方式更简洁：

```csharp
public static partial class LogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Proxy started on {Endpoint}")]
    public static partial void ProxyStarted(this ILogger logger, string endpoint);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Request {RequestId} completed in {Elapsed}ms")]
    public static partial void RequestCompleted(this ILogger logger, string requestId, long elapsed);
}
```

## 37.6 Serilog 集成

Serilog 是 .NET 生态中最流行的第三方日志库，以结构化日志和丰富的 Sink（输出目标）著称。

### 37.6.1 安装和配置

```xml
<!-- NuGet 包 -->
<PackageReference Include="Serilog" Version="4.*" />
<PackageReference Include="Serilog.Extensions.Hosting" Version="8.*" />
<PackageReference Include="Serilog.Settings.Configuration" Version="8.*" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.*" />
<PackageReference Include="Serilog.Sinks.File" Version="6.*" />
<PackageReference Include="Serilog.Sinks.Debug" Version="3.*" />
```

**代码配置方式**：

```csharp
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "CodexSwitch")
    .Enrich.WithProperty("Version", "1.0.0")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        fileSizeLimitBytes: 10_000_000,
        rollOnFileSizeLimit: true)
    .WriteTo.Debug()
    .CreateLogger();

// 在 Host 中使用
var host = Host.CreateDefaultBuilder(args)
    .UseSerilog()  // 替换默认日志
    .Build();
```

**配置文件方式（推荐）**：

```json
// appsettings.json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "CodexSwitch.Proxy": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "fileSizeLimitBytes": 10485760,
          "rollOnFileSizeLimit": true
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/errors-.log",
          "restrictedToMinimumLevel": "Error",
          "rollingInterval": "Day"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

```csharp
// 在 Host 中加载配置
var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, config) =>
    {
        config.ReadFrom.Configuration(context.Configuration);
    })
    .Build();
```

### 37.6.2 Sink 配置

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()

    // 控制台（开发环境必备）
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)

    // 文件（生产环境必备）
    .WriteTo.File("logs/app.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)

    // Seq（结构化日志服务器，类似 Elasticsearch 但更轻量）
    .WriteTo.Seq("http://localhost:5341")

    // Elasticsearch（大规模日志收集）
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(
        new Uri("http://localhost:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "codexswitch-{0:yyyy.MM.dd}"
    })

    // Application Insights（Azure 监控）
    .WriteTo.ApplicationInsights(
        TelemetryConfiguration.CreateDefault(),
        TelemetryConverter.Traces)

    // 条件 Sink（只在特定条件下写入）
    .WriteTo.Conditional(
        evt => evt.Level >= LogEventLevel.Error,
        wt => wt.File("logs/errors.log"))

    .CreateLogger();
```

### 37.6.3 日志丰富（Enricher）

```csharp
// 内置丰富器
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()          // 从 AsyncLocal 中读取属性
    .Enrich.WithMachineName()         // 添加机器名
    .Enrich.WithThreadId()            // 添加线程 ID
    .Enrich.WithProcessId()           // 添加进程 ID
    .Enrich.WithProperty("App", "CodexSwitch")
    .Enrich.WithProperty("Env", "Production")
    .CreateLogger();

// 自定义丰富器
public class UserEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        logEvent.AddPropertyIfAbsent(
            factory.CreateProperty("UserId", GetCurrentUserId()));
        logEvent.AddPropertyIfAbsent(
            factory.CreateProperty("UserName", GetCurrentUserName()));
    }
}

// 使用自定义丰富器
Log.Logger = new LoggerConfiguration()
    .Enrich.With<UserEnricher>()
    .CreateLogger();

// 运行时添加属性（使用 LogContext）
using (LogContext.PushProperty("RequestId", requestId))
using (LogContext.PushProperty("UserId", userId))
{
    Log.Information("Processing request");  // 自动包含 RequestId 和 UserId
}
```

### 37.6.4 日志过滤

```csharp
Log.Logger = new LoggerConfiguration()
    // 按命名空间过滤
    .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore"))
    .Filter.ByExcluding(Matching.FromSource("System.Net.Http"))

    // 按属性过滤
    .Filter.ByExcluding(evt =>
        evt.Properties.ContainsKey("SourceContext") &&
        evt.Properties["SourceContext"].ToString().Contains("HealthCheck"))

    // 按条件过滤
    .Filter.ByIncludingOnly(evt =>
        evt.Level >= LogEventLevel.Warning ||
        evt.Properties.ContainsKey("Important"))

    .CreateLogger();
```

### 37.6.5 滚动日志文件与异步写入

```csharp
Log.Logger = new LoggerConfiguration()
    // 滚动文件：每天一个文件，最多保留 30 个，单文件最大 10MB
    .WriteTo.File("logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10_000_000,
        rollOnFileSizeLimit: true,
        retainedFileTimeLimit: TimeSpan.FromDays(90))

    // 异步写入（避免阻塞主线程）
    .WriteTo.Async(a => a.File("logs/async-.log",
        rollingInterval: RollingInterval.Day))

    // 异步 + 批量写入（高性能场景）
    .WriteTo.Async(a => a.File("logs/batch-.log",
        rollingInterval: RollingInterval.Day),
        bufferSize: 50,  // 缓冲 50 条后批量写入
        blockWhenFull: true)

    .CreateLogger();
```

## 37.7 NLog 集成

### 37.7.1 安装和配置

```xml
<PackageReference Include="NLog" Version="5.*" />
<PackageReference Include="NLog.Extensions.Logging" Version="5.*" />
```

```csharp
// Program.cs
using NLog.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddNLog();  // 添加 NLog
    })
    .Build();
```

### 37.7.2 NLog 配置文件

```xml
<!-- nlog.config -->
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true">

  <targets>
    <!-- 控制台 -->
    <target name="console" xsi:type="Console"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner= ${exception:format=tostring}}" />

    <!-- 文件：按日期滚动 -->
    <target name="file" xsi:type="File"
            fileName="logs/app-${shortdate}.log"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner= ${exception:format=tostring}}"
            maxArchiveFiles="30"
            archiveEvery="Day"
            archiveAboveSize="10485760" />

    <!-- 错误文件 -->
    <target name="errorFile" xsi:type="File"
            fileName="logs/error-${shortdate}.log"
            layout="${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner= ${exception:format=tostring}}"
            maxArchiveFiles="90" />

    <!-- 结构化 JSON 文件 -->
    <target name="jsonFile" xsi:type="File"
            fileName="logs/structured-${shortdate}.json"
            layout="${aspnet-request-url}${aspnet-mvc-action}" />
  </targets>

  <rules>
    <!-- 跳过 Microsoft 和 System 的低级别日志 -->
    <logger name="Microsoft.*" maxlevel="Info" final="true" />
    <logger name="System.*" maxlevel="Info" final="true" />

    <!-- 错误及以上级别同时写入错误文件 -->
    <logger name="*" minlevel="Error" writeTo="errorFile" />

    <!-- 所有日志写入控制台和文件 -->
    <logger name="*" minlevel="Debug" writeTo="console,file" />
  </rules>
</nlog>
```

### 37.7.3 Serilog vs NLog 对比

| 特性 | Serilog | NLog |
|------|---------|------|
| 结构化日志 | 原生支持 | 需要配置 |
| 配置方式 | 代码优先 + 配置文件 | 配置文件优先 |
| Sink/Target 丰富度 | 非常丰富 | 丰富 |
| 性能 | 优秀 | 优秀 |
| AOT 兼容性 | 好 | 一般 |
| 学习曲线 | 中等 | 低 |
| 社区活跃度 | 高 | 高 |

**建议**：新项目推荐 Serilog，其结构化日志和代码配置方式更现代。NLog 在已有项目中广泛使用，配置文件方式对运维更友好。

## 37.8 配置系统深入

### 37.8.1 IConfiguration 接口

```csharp
public class MyService
{
    private readonly IConfiguration _config;

    public MyService(IConfiguration config)
    {
        _config = config;
    }

    public void ReadSettings()
    {
        // 读取单个值
        var appName = _config["App:Name"];
        var port = _config.GetValue<int>("Proxy:Port", 12785);
        var enabled = _config.GetValue<bool>("Proxy:Enabled", true);

        // 读取节
        var proxySection = _config.GetSection("Proxy");
        var host = proxySection["Host"];
        var proxyPort = proxySection.GetValue<int>("Port");

        // 绑定到对象（强类型）
        var settings = new ProxySettings();
        _config.GetSection("Proxy").Bind(settings);

        // 使用 Get<T> 绑定
        var proxySettings = _config.GetSection("Proxy").Get<ProxySettings>();
    }
}
```

### 37.8.2 配置源优先级

后添加的配置源会覆盖先添加的（同名键）：

```
优先级（从低到高）：
1. appsettings.json
2. appsettings.{Environment}.json
3. 环境变量
4. 命令行参数
```

```csharp
var builder = Host.CreateDefaultBuilder(args);
// CreateDefaultBuilder 已经按正确顺序添加了以上所有源
// 最终的值是最后一个非空值
```

### 37.8.3 配置绑定（强类型配置）

```csharp
// 定义配置类
public class ProxySettings
{
    public bool Enabled { get; set; } = true;
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 12785;
    public string Endpoint => $"http://{Host}:{Port}";
}

public class NetworkSettings
{
    public OutboundProxyMode ProxyMode { get; set; } = OutboundProxyMode.System;
    public string CustomProxyUrl { get; set; } = "";
    public int ConnectTimeoutSeconds { get; set; } = 30;
}

// 在 appsettings.json 中
{
    "Proxy": {
        "Enabled": true,
        "Host": "127.0.0.1",
        "Port": 12785
    },
    "Network": {
        "ProxyMode": "System",
        "CustomProxyUrl": "",
        "ConnectTimeoutSeconds": 30
    }
}

// 注册绑定
services.Configure<ProxySettings>(config.GetSection("Proxy"));
services.Configure<NetworkSettings>(config.GetSection("Network"));

// 注入使用
public class ProxyHostService
{
    private readonly ProxySettings _proxySettings;

    public ProxyHostService(IOptions<ProxySettings> proxyOptions)
    {
        _proxySettings = proxyOptions.Value;  // 读取配置
    }
}
```

### 37.8.4 配置验证

```csharp
// 定义带验证的配置
public class ProxySettings
{
    [Required]
    public string Host { get; set; } = "";

    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int Port { get; set; } = 12785;
}

// 注册时启用验证
services.AddOptions<ProxySettings>()
    .Bind(config.GetSection("Proxy"))
    .ValidateDataAnnotations()  // 启用 DataAnnotation 验证
    .ValidateOnStart();         // 启动时验证，失败则抛异常

// 自定义验证
services.AddOptions<ProxySettings>()
    .Bind(config.GetSection("Proxy"))
    .Validate(settings =>
    {
        if (settings.Port < 1024 && !IsRunningAsAdmin())
            return false;
        return true;
    }, "Port below 1024 requires admin privileges")
    .ValidateOnStart();
```

### 37.8.5 配置热更新

```csharp
// IOptions<T> —— 读取一次，不会更新
// IOptionsSnapshot<T> —— 每个 Scope 读取一次（Scoped 生命周期）
// IOptionsMonitor<T> —— 每次值变化时通知（推荐用于热更新）

public class ProxyHostService
{
    private readonly ProxySettings _settings;

    public ProxyHostService(IOptionsMonitor<ProxySettings> optionsMonitor)
    {
        _settings = optionsMonitor.CurrentValue;

        // 监听配置变化
        optionsMonitor.OnChange((newSettings, name) =>
        {
            _settings = newSettings;
            Console.WriteLine($"Proxy settings changed: {newSettings.Host}:{newSettings.Port}");
            // 可以在这里触发服务重启
        });
    }
}
```

```json
// 配置文件需要设置 reloadOnChange: true
// Program.cs
config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
```

## 37.9 托管服务（Hosted Service）

### 37.9.1 IHostedService 接口

```csharp
public class StartupWarmupService : IHostedService
{
    private readonly ILogger<StartupWarmupService> _logger;

    public StartupWarmupService(ILogger<StartupWarmupService> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Warming up services...");

        // 应用启动时执行（在 Host.StartAsync 中调用）
        await PreloadDataAsync();
        await InitializeCacheAsync();

        _logger.LogInformation("Warmup complete");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutting down warmup service...");

        // 应用关闭时执行（在 Host.StopAsync 中调用）
        await CleanupResourcesAsync();
    }
}

// 注册
services.AddHostedService<StartupWarmupService>();
```

### 37.9.2 BackgroundService 基类

`BackgroundService` 是 `IHostedService` 的便捷基类，简化了长时间运行的任务：

```csharp
public class UsageLogCleanupService : BackgroundService
{
    private readonly ILogger<UsageLogCleanupService> _logger;
    private readonly AppPaths _paths;

    public UsageLogCleanupService(
        ILogger<UsageLogCleanupService> logger,
        AppPaths paths)
    {
        _logger = logger;
        _paths = paths;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Usage log cleanup service started");

        // 使用 PeriodicTimer（.NET 6+）代替 Thread.Sleep
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CleanupOldLogsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;  // 正常关闭
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during log cleanup");
                // 不要抛出异常——后台服务崩溃会导致整个 Host 关闭
                // 等待下一次重试
            }
        }

        _logger.LogInformation("Usage log cleanup service stopped");
    }

    private async Task CleanupOldLogsAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-90);
        var logDir = _paths.UsageLogDirectory;

        foreach (var file in Directory.GetFiles(logDir, "*.jsonl"))
        {
            ct.ThrowIfCancellationRequested();

            if (File.GetLastWriteTimeUtc(file) < cutoff)
            {
                _logger.LogInformation("Deleting old log file: {File}", file);
                File.Delete(file);
                await Task.Delay(10, ct);  // 避免磁盘 I/O 过载
            }
        }
    }
}
```

### 37.9.3 定时任务（PeriodicTimer）

```csharp
public class HealthCheckService : BackgroundService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly HttpClient _http;

    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _http = httpFactory.CreateClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 每 30 秒检查一次
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var response = await _http.GetAsync("http://localhost:12785/health", stoppingToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Health check failed: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Health check request failed");
            }
        }
    }
}
```

### 37.9.4 优雅关闭

```csharp
// Host 会按注册的反序关闭 HostedService
// 即最后注册的先关闭

public class GracefulShutdownService : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<GracefulShutdownService> _logger;

    public GracefulShutdownService(
        IHostApplicationLifetime lifetime,
        ILogger<GracefulShutdownService> logger)
    {
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 监听关闭信号
        _lifetime.ApplicationStopping.Register(() =>
        {
            _logger.LogInformation("Application is stopping, cleaning up...");
            // 执行清理逻辑
        });

        _lifetime.ApplicationStopped.Register(() =>
        {
            _logger.LogInformation("Application has stopped");
            // 最终清理
        });

        return Task.CompletedTask;
    }
}
```

## 37.10 HttpClientFactory

### 37.10.1 为什么需要 HttpClientFactory

直接 `new HttpClient()` 会导致两个问题：
1. **Socket 耗尽**：每个 HttpClient 实例有自己的连接池，GC 不会及时释放底层 Socket
2. **DNS 不刷新**：HttpClient 的 DNS 缓存不会自动更新

`IHttpClientFactory` 解决了这些问题，它管理底层的 `HttpMessageHandler` 生命周期。

### 37.10.2 注册和使用

```csharp
// 注册
services.AddHttpClient();

// 或者命名客户端
services.AddHttpClient("upstream", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "CodexSwitch/1.0");
});

// 或者类型化客户端（推荐）
services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### 37.10.3 类型化客户端

```csharp
// 定义接口
public interface IApiClient
{
    Task<ProviderStatus> GetProviderStatusAsync(string providerId);
    Task<bool> TestConnectionAsync(string baseUrl, string apiKey);
}

// 实现
public class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient http, ILogger<ApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ProviderStatus> GetProviderStatusAsync(string providerId)
    {
        _logger.LogDebug("Checking status for provider {ProviderId}", providerId);
        var response = await _http.GetAsync($"/v1/providers/{providerId}/status");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProviderStatus>();
    }

    public async Task<bool> TestConnectionAsync(string baseUrl, string apiKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/v1/models");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        try
        {
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection test failed for {BaseUrl}", baseUrl);
            return false;
        }
    }
}

// 使用
public class ProviderTestService
{
    private readonly IApiClient _api;

    public ProviderTestService(IApiClient api)
    {
        _api = api;
    }

    public async Task<bool> TestProviderAsync(ProviderConfig provider)
    {
        return await _api.TestConnectionAsync(provider.BaseUrl, provider.ApiKey);
    }
}
```

### 37.10.4 Polly 策略集成

```xml
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.*" />
```

```csharp
using Polly;
using Polly.Extensions.Http;

services.AddHttpClient("upstream")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy())
    .AddPolicyHandler(GetTimeoutPolicy());

// 重试策略：指数退避
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()  // 5xx 和 HttpRequestException
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                // 可以在这里记录日志
            });
}

// 断路器策略
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));
}

// 超时策略
static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
}
```

## 37.11 缓存系统

### 37.11.1 IMemoryCache（内存缓存）

```csharp
// 注册
services.AddMemoryCache();

// 使用
public class PricingService
{
    private readonly IMemoryCache _cache;
    private readonly ConfigurationStore _store;

    public PricingService(IMemoryCache cache, ConfigurationStore store)
    {
        _cache = cache;
        _store = store;
    }

    public ModelPricingCatalog GetPricing()
    {
        return _cache.GetOrCreate("pricing", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            entry.SlidingExpiration = TimeSpan.FromMinutes(2);
            entry.Priority = CacheItemPriority.High;
            return _store.LoadPricing();
        });
    }

    public async Task<ModelPricingCatalog> GetPricingAsync()
    {
        return await _cache.GetOrCreateAsync("pricing", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await LoadPricingFromDiskAsync();
        });
    }
}
```

### 37.11.2 缓存策略

```csharp
// 绝对过期：从创建时算起，到期即失效
entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

// 滑动过期：每次访问时重置过期时间
entry.SlidingExpiration = TimeSpan.FromMinutes(2);

// 绝对过期（固定时间点）
entry.AbsoluteExpiration = DateTimeOffset.UtcNow.Date.AddDays(1);

// 组合使用：滑动过期 + 最大绝对过期
entry.SlidingExpiration = TimeSpan.FromMinutes(2);
entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
```

### 37.11.3 缓存问题

```csharp
// 缓存穿透：查询不存在的数据，每次都打到数据库
// 解决：缓存空值
var result = _cache.GetOrCreate(key, entry =>
{
    var data = _store.Find(id);
    if (data is null)
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
        return EmptyResult.Instance;  // 缓存空值
    }
    return data;
});

// 缓存击穿：热点 key 过期，大量请求同时打到数据库
// 解决：使用 SemaphoreSlim 限流
private static readonly SemaphoreSlim _lock = new(1, 1);

public async Task<Data> GetHotDataAsync(string key)
{
    var cached = _cache.Get<Data>(key);
    if (cached is not null) return cached;

    await _lock.WaitAsync();
    try
    {
        // 双重检查
        cached = _cache.Get<Data>(key);
        if (cached is not null) return cached;

        var data = await _store.LoadAsync(key);
        _cache.Set(key, data, TimeSpan.FromMinutes(5));
        return data;
    }
    finally
    {
        _lock.Release();
    }
}

// 缓存雪崩：大量 key 同时过期
// 解决：添加随机过期时间
var jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 60));
entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) + jitter;
```

## 37.12 健康检查

### 37.12.1 注册和使用

```csharp
// 注册
services.AddHealthChecks()
    .AddCheck<ProxyHealthCheck>("proxy")
    .AddCheck<DiskSpaceHealthCheck>("disk")
    .AddUrlGroup(new Uri("https://api.example.com/health"), "upstream");

// 映射端点
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/detail", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### 37.12.2 自定义健康检查

```csharp
public class ProxyHealthCheck : IHealthCheck
{
    private readonly ProxyHostService _proxy;

    public ProxyHealthCheck(ProxyHostService proxy)
    {
        _proxy = proxy;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_proxy.State.IsRunning)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                "Proxy is running",
                new Dictionary<string, object>
                {
                    ["endpoint"] = _proxy.State.Endpoint,
                    ["provider"] = _proxy.State.ProviderId
                }));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy(
            "Proxy is not running",
            data: new Dictionary<string, object>
            {
                ["status"] = _proxy.State.StatusMessage
            }));
    }
}

public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly AppPaths _paths;

    public DiskSpaceHealthCheck(AppPaths paths)
    {
        _paths = paths;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var drive = new DriveInfo(Path.GetPathRoot(_paths.RootDirectory)!);
        var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);

        if (freeGb < 0.1)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Disk space critically low: {freeGb:F2} GB free"));
        }

        if (freeGb < 1.0)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Disk space low: {freeGb:F2} GB free"));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Disk space OK: {freeGb:F2} GB free"));
    }
}
```

## 37.13 后台任务队列

### 37.13.1 Channel&lt;T&gt; 实现任务队列

```csharp
// 定义任务
public record BackgroundWorkItem(
    string Id,
    string Type,
    object Payload,
    int Priority = 0);

// 任务队列
public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(BackgroundWorkItem item);
    ValueTask<BackgroundWorkItem> DequeueAsync(CancellationToken ct);
}

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<BackgroundWorkItem> _queue;

    public BackgroundTaskQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        _queue = Channel.CreateBounded<BackgroundWorkItem>(options);
    }

    public async ValueTask QueueAsync(BackgroundWorkItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        await _queue.Writer.WriteAsync(item);
    }

    public async ValueTask<BackgroundWorkItem> DequeueAsync(CancellationToken ct)
    {
        return await _queue.Reader.ReadAsync(ct);
    }
}
```

### 37.13.2 后台任务处理器

```csharp
public class BackgroundTaskProcessor : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly ILogger<BackgroundTaskProcessor> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public BackgroundTaskProcessor(
        IBackgroundTaskQueue queue,
        ILogger<BackgroundTaskProcessor> logger,
        IServiceScopeFactory scopeFactory)
    {
        _queue = queue;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background task processor started");

        await foreach (var item in ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await ProcessItemAsync(item, scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task {TaskId} of type {TaskType}",
                    item.Id, item.Type);
                // 可以在这里实现重试逻辑
            }
        }

        _logger.LogInformation("Background task processor stopped");
    }

    private async IAsyncEnumerable<BackgroundWorkItem> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            BackgroundWorkItem item;
            try
            {
                item = await _queue.DequeueAsync(ct);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            yield return item;
        }
    }

    private static async Task ProcessItemAsync(
        BackgroundWorkItem item,
        IServiceProvider services,
        CancellationToken ct)
    {
        // 根据任务类型分发处理
        switch (item.Type)
        {
            case "icon-download":
                var iconService = services.GetRequiredService<IconCacheService>();
                await iconService.DownloadIconAsync((string)item.Payload, ct);
                break;
            case "usage-export":
                var exportService = services.GetRequiredService<UsageExportService>();
                await exportService.ExportAsync((ExportRequest)item.Payload, ct);
                break;
            default:
                throw new InvalidOperationException($"Unknown task type: {item.Type}");
        }
    }
}

// 注册
services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
services.AddHostedService<BackgroundTaskProcessor>();
```

## 37.14 实战：完整的 Host 配置

下面是一个完整的 Avalonia + Generic Host 项目的 `Program.cs` 配置：

```csharp
// Program.cs - 完整示例
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace MyApp;

sealed class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        // 1. 配置 Serilog（最早的日志，捕获启动过程中的错误）
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Application starting...");

            // 2. 构建 Host
            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog((context, config) =>
                {
                    config
                        .ReadFrom.Configuration(context.Configuration)
                        .Enrich.WithProperty("Application", "MyApp");
                })
                .ConfigureServices((context, services) =>
                {
                    var config = context.Configuration;

                    // 基础设施
                    services.AddSingleton<AppPaths>();
                    services.AddSingleton<ConfigurationStore>();

                    // 配置绑定
                    services.Configure<ProxySettings>(config.GetSection("Proxy"));
                    services.Configure<NetworkSettings>(config.GetSection("Network"));
                    services.Configure<UiSettings>(config.GetSection("Ui"));

                    // 业务服务
                    services.AddSingleton<PriceCalculator>();
                    services.AddSingleton<UsageMeter>();
                    services.AddSingleton<UsageLogWriter>();
                    services.AddSingleton<UsageLogReader>();
                    services.AddSingleton<ProviderAuthService>();
                    services.AddSingleton<ProxyHostService>();
                    services.AddSingleton<CodexConfigWriter>();
                    services.AddSingleton<ClaudeCodeConfigWriter>();
                    services.AddSingleton<I18nService>();

                    // HTTP 客户端
                    services.AddHttpClient("upstream", client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
                    })
                    .AddPolicyHandler(GetRetryPolicy());

                    // 内存缓存
                    services.AddMemoryCache();

                    // 健康检查
                    services.AddHealthChecks()
                        .AddCheck<ProxyHealthCheck>("proxy");

                    // 后台任务
                    services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
                    services.AddHostedService<BackgroundTaskProcessor>();

                    // 托管服务
                    services.AddHostedService<AutoUpdateService>();
                    services.AddHostedService<UsageLogCleanupService>();
                })
                .Build();

            // 3. 启动 Host
            await host.StartAsync();

            Log.Information("Host started, launching Avalonia...");

            try
            {
                // 4. 启动 Avalonia，传入服务提供者
                BuildAvaloniaApp(host.Services)
                    .StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                // 5. 优雅关闭
                Log.Information("Avalonia closed, stopping host...");
                await host.StopAsync();
                host.Dispose();
            }

            Log.Information("Application stopped normally");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    public static AppBuilder BuildAvaloniaApp(IServiceProvider services)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .WithServices(services);
}
```

对应的 `appsettings.json`：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "MyApp.Proxy": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "fileSizeLimitBytes": 10485760,
          "rollOnFileSizeLimit": true
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/errors-.log",
          "restrictedToMinimumLevel": "Error",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 90
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithThreadId"]
  },
  "Proxy": {
    "Enabled": true,
    "Host": "127.0.0.1",
    "Port": 12785
  },
  "Network": {
    "ProxyMode": "System",
    "CustomProxyUrl": "",
    "ConnectTimeoutSeconds": 30,
    "OutboundHttpVersion": "Http2"
  },
  "Ui": {
    "Theme": "system",
    "Language": "zh-CN"
  }
}
```

环境特定配置 `appsettings.Development.json`：

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  },
  "Proxy": {
    "Port": 22785
  }
}
```

`appsettings.Production.json`：

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning"
    }
  }
}
```

## 37.15 实战：CodexSwitch 的架构分析

### 37.15.1 CodexSwitch 的启动流程

CodexSwitch 采用了一种**务实的混合模式**——不使用完整的 Generic Host，而是手动组装需要的组件。这在中小型桌面应用中很常见：

```
Program.Main()
    │
    ├── 特殊模式检查（bootstrap Claude 配置）
    │
    └── BuildAvaloniaApp().StartWithClassicDesktopLifetime()
            │
            ├── App.Initialize()          ← 加载 AXAML
            │
            └── App.OnFrameworkInitializationCompleted()
                    │
                    ├── new MainWindowViewModel()    ← 手动创建所有服务
                    │       │
                    │       ├── new AppPaths()                ← 文件路径
                    │       ├── new ConfigurationStore()       ← 配置读写
                    │       ├── new PriceCalculator()          ← 价格计算
                    │       ├── new UsageMeter()               ← 用量统计
                    │       ├── new UsageLogWriter()           ← 日志写入
                    │       ├── new UsageLogReader()           ← 日志读取
                    │       ├── new CodexConfigWriter()        ← Codex 配置
                    │       ├── new ClaudeCodeConfigWriter()   ← Claude 配置
                    │       └── CreateNetworkServices()        ← 网络相关
                    │
                    ├── new TrayMenuController()   ← 系统托盘
                    ├── ShowMainWindow()             ← 显示主窗口
                    └── ShutdownRequested            ← 关闭处理
```

### 37.15.2 服务注册和解析

CodexSwitch 没有使用 DI 容器，而是在 `MainWindowViewModel` 构造函数中**手动创建**所有服务：

```csharp
// CodexSwitch/ViewModels/MainWindowViewModel.cs (简化)
public MainWindowViewModel()
{
    // 手动创建服务链
    _paths = new AppPaths();
    _store = new ConfigurationStore(_paths);     // 依赖 AppPaths
    _config = _store.LoadConfig();                // 从文件加载配置
    _pricing = _store.LoadPricing();              // 从文件加载价格
    _priceCalculator = new PriceCalculator(_pricing);
    _usageMeter = new UsageMeter(_priceCalculator);
    _usageLogWriter = new UsageLogWriter(_paths);
    _usageLogReader = new UsageLogReader(_paths);
    _codexConfigWriter = new CodexConfigWriter(_paths);
    _claudeCodeConfigWriter = new ClaudeCodeConfigWriter(_paths);
    // ...
}
```

**如果要用 DI 重构**，代码会变成：

```csharp
// 服务注册（在 Program.cs 或 App.axaml.cs 中）
services.AddSingleton<AppPaths>();
services.AddSingleton<ConfigurationStore>();
services.AddSingleton(sp =>
{
    var store = sp.GetRequiredService<ConfigurationStore>();
    return store.LoadConfig();
});
services.AddSingleton(sp =>
{
    var store = sp.GetRequiredService<ConfigurationStore>();
    return store.LoadPricing();
});
services.AddSingleton<PriceCalculator>();
services.AddSingleton<UsageMeter>();
services.AddSingleton<UsageLogWriter>();
services.AddSingleton<UsageLogReader>();
services.AddSingleton<CodexConfigWriter>();
services.AddSingleton<ClaudeCodeConfigWriter>();

// ViewModel 构造函数——DI 自动解析
public MainWindowViewModel(
    AppPaths paths,
    ConfigurationStore store,
    PriceCalculator calculator,
    UsageMeter meter,
    UsageLogWriter logWriter,
    UsageLogReader logReader,
    CodexConfigWriter codexWriter,
    ClaudeCodeConfigWriter claudeWriter)
{
    _paths = paths;           // 自动注入
    _store = store;           // 自动注入
    _config = store.LoadConfig();
    // ... 其他字段
}
```

### 37.15.3 配置管理

CodexSwitch 的配置系统完全自建，不依赖 `IConfiguration`：

```csharp
// 配置模型
public class AppConfig
{
    public AppUiSettings Ui { get; set; } = new();
    public ProxySettings Proxy { get; set; } = new();
    public NetworkSettings Network { get; set; } = new();
    public Collection<ProviderConfig> Providers { get; set; } = [];
    // ...
}

// 配置存储
public sealed class ConfigurationStore
{
    private readonly AppPaths _paths;

    public ConfigurationStore(AppPaths paths) { _paths = paths; }

    public AppConfig LoadConfig()
    {
        // 从 JSON 文件读取
        using var stream = File.OpenRead(_paths.ConfigPath);
        return JsonSerializer.Deserialize(stream, context) ?? CreateDefaultConfig();
    }

    public void SaveConfig(AppConfig config)
    {
        // 原子写入（先写临时文件，再重命名）
        SaveJsonAtomically(_paths.ConfigPath, config, typeInfo);
    }
}
```

这种自建配置系统的好处是完全控制序列化格式和迁移逻辑，代价是失去了 `IOptionsMonitor` 的热更新能力。

### 37.15.4 日志系统

CodexSwitch 内部没有使用 `ILogger<T>`，而是通过 Avalonia 的 `LogToTrace()` 将框架日志输出到调试窗口。业务日志通过自建的 `UsageLogWriter` 写入 JSONL 文件：

```csharp
// Avalonia 框架日志 → Trace 输出（开发时在 IDE 中查看）
AppBuilder.Configure<App>().LogToTrace();

// 业务日志（API 调用记录）→ JSONL 文件
public class UsageLogWriter
{
    private readonly AppPaths _paths;

    public void Write(UsageLogEntry entry)
    {
        // 追加写入 usage-log.jsonl
        var json = JsonSerializer.Serialize(entry, context);
        File.AppendAllText(_paths.UsageLogPath, json + Environment.NewLine);
    }
}
```

### 37.15.5 生命周期管理

```csharp
// App.axaml.cs 中的生命周期管理
desktop.ShutdownRequested += async (_, _) =>
{
    // 1. 释放系统托盘
    _trayMenuController?.Dispose();

    // 2. 关闭主窗口
    CloseMainWindow();

    // 3. 释放 ViewModel（包括停止代理服务器）
    if (_viewModel is not null)
        await _viewModel.DisposeAsync();
};

// MainWindowViewModel 中
public async ValueTask DisposeAsync()
{
    // 停止代理服务器（优雅关闭 Kestrel）
    await _proxyHostService.StopAsync();

    // 释放 HTTP 客户端
    _sharedHttpClient.Dispose();

    // 恢复客户端配置（移除 CodexSwitch 的管理配置）
    _codexConfigWriter.RestoreOriginal();
    _claudeCodeConfigWriter.RestoreOriginal();

    // 停止定时器
    _usageQueryTimer.Stop();
    _miniStatusTimer.Stop();
}
```

## Deep Dive

### Host 的内部实现原理

`Host` 本质上是一个服务提供者的包装器：

```
Host.CreateDefaultBuilder()
    │
    ├── HostBuilder（配置阶段）
    │   ├── _configureAppConfigActions    ← 配置回调列表
    │   ├── _configureServicesActions     ← 服务注册回调列表
    │   └── _configureLoggingActions      ← 日志回调列表
    │
    └── .Build()
        │
        ├── 执行所有配置回调 → 构建 IConfiguration
        ├── 执行所有服务回调 → 构建 IServiceCollection
        ├── 创建 ServiceProvider（根容器）
        ├── 注册默认服务（IHostLifetime, IHost 等）
        └── 返回 Host 实例
            │
            ├── .StartAsync()
            │   ├── 触发 ApplicationStarted
            │   ├── 按注册顺序启动所有 IHostedService
            │   └── 触发 ApplicationStarting
            │
            └── .StopAsync()
                ├── 触发 ApplicationStopping
                ├── 按注册反序停止所有 IHostedService
                ├── 释放 ServiceProvider
                └── 触发 ApplicationStopped
```

### DI 容器的实现原理

.NET 的 DI 容器内部使用两种策略创建服务实例：

1. **反射方式**（默认）：使用 `ActivatorUtilities.CreateInstance()` 通过反射调用构造函数
2. **编译表达式树**：将构造函数调用编译为 `Expression<Func<IServiceProvider, T>>`
3. **源生成**（.NET 8+，NativeAOT 友好）：使用 `Microsoft.Extensions.DependencyInjection.SourceGenerator`

```csharp
// .NET 8+ 源生成 DI（AOT 兼容）
[GenerateActivator]
public static partial class ServiceCollectionExtensions
{
    [ActivatorUtilitiesConstructor]
    public static IServiceCollection AddMyServices(this IServiceCollection services)
    {
        services.AddSingleton<AppPaths>();
        services.AddSingleton<ConfigurationStore>();
        return services;
    }
}
```

### 日志系统的性能影响

日志系统的设计考虑了性能：

```csharp
// 不好的做法：即使日志不输出，也会执行字符串格式化
_logger.LogInformation("Data: " + expensiveObject.ToString());
// 即使日志级别是 Warning，这里也会调用 ToString()

// 好的做法：使用消息模板，只有在日志实际输出时才格式化
_logger.LogInformation("Data: {Data}", expensiveObject);
// 如果日志级别不够，expensiveObject 不会被格式化

// 最佳做法：高频路径使用 LoggerMessage.Define 或源生成
// 避免装箱、避免闭包分配
```

### Options 模式的实现

```
IOptionsMonitor<T> 的内部结构：
    │
    ├── OptionsFactory<T>        ← 创建 T 实例
    │   └── 调用所有 IConfigureOptions<T>
    │
    ├── OptionsMonitorCache<T>   ← 缓存已创建的实例
    │   └── ConcurrentDictionary<string, Lazy<T>>
    │
    └── OptionsMonitor<T>        ← 对外接口
        ├── CurrentValue          ← 获取当前值
        ├── Get(name)             ← 按名称获取
        └── OnChange(callback)    ← 注册变化回调
            └── 通过 IOptionsChangeTokenSource 监听变化
```

## Cross References

- **第 2 章（项目结构与启动流程）**：本章是第 2 章的深度扩展，详细讲解了 Program.cs 背后的完整基础设施
- **第 6 章（MVVM 模式实战）**：IoC 容器是 MVVM 模式中 ViewModel 创建和注入的核心机制
- **第 15 章（编译绑定）**：`IOptionsMonitor` 的变化通知机制与编译绑定的数据更新有相似的响应式模式
- **第 21 章（调试）**：日志系统是调试的重要工具，结构化日志比断点更适合排查生产问题
- **第 25 章（ASP.NET Core 集成）**：CodexSwitch 内嵌的 Kestrel 服务器就是通过 `WebApplication.CreateSlimBuilder()` 创建的，它内部使用了完整的 DI 和配置系统
- **第 36 章（通知与任务栏）**：后台服务可以通过系统通知向用户报告状态变化

## Common Pitfalls

### 1. Singleton 注入 Scoped 服务

```csharp
// 错误：Singleton 持有 Scoped 服务引用
public class MySingleton
{
    public MySingleton(IScopedService scoped) { }  // 编译通过但运行时行为不可预测
}

// 正确：使用 IServiceScopeFactory
public class MySingleton
{
    private readonly IServiceScopeFactory _factory;
    public MySingleton(IServiceScopeFactory factory) { _factory = factory; }

    public void DoWork()
    {
        using var scope = _factory.CreateScope();
        var scoped = scope.ServiceProvider.GetRequiredService<IScopedService>();
    }
}
```

### 2. 忘记注册服务

```csharp
// 注册了接口，但忘记注册实现
services.AddSingleton<IMyService>();  // 编译通过，运行时抛异常

// 正确：同时注册接口和实现
services.AddSingleton<IMyService, MyService>();
```

### 3. 在构造函数中做重活

```csharp
// 不好的做法：构造函数中执行耗时操作
public MyService(ConfigurationStore store)
{
    var config = store.LoadConfig();  // 涉及文件 I/O
    InitializeDatabase(config);       // 涉及数据库连接
}

// 好的做法：延迟初始化
public class MyService
{
    private readonly Lazy<Task> _initialization;

    public MyService(ConfigurationStore store)
    {
        _initialization = new Lazy<Task>(() => InitializeAsync(store));
    }

    private async Task InitializeAsync(ConfigurationStore store)
    {
        var config = await store.LoadConfigAsync();
        await InitializeDatabaseAsync(config);
    }
}
```

### 4. HttpClient 直接 new

```csharp
// 错误：直接创建 HttpClient（Socket 耗尽 + DNS 不刷新）
var client = new HttpClient();

// 正确：使用 IHttpClientFactory
services.AddHttpClient("upstream", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
});
```

### 5. 日志中拼接字符串

```csharp
// 不好的做法
_logger.LogInformation("User " + userId + " logged in from " + ip);

// 好的做法（结构化日志）
_logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ip);
```

### 6. 配置不设 reloadOnChange

```csharp
// 不好的做法：修改配置文件后需要重启应用
config.AddJsonFile("appsettings.json");

// 好的做法：自动重新加载
config.AddJsonFile("appsettings.json", reloadOnChange: true);
```

### 7. HostedService 异常导致应用关闭

```csharp
// 不好的做法：异常会传播到 Host，导致应用关闭
protected override async Task ExecuteAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        await DoWorkAsync();  // 如果这里抛异常，Host 会关闭
    }
}

// 好的做法：捕获异常但不中断服务
protected override async Task ExecuteAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        try
        {
            await DoWorkAsync();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            break;  // 正常关闭
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in background work");
            await Task.Delay(TimeSpan.FromSeconds(5), ct);  // 等待后重试
        }
    }
}
```

### 8. Dispose 后继续使用服务

```csharp
// 错误：Host 停止后继续使用服务
await host.StopAsync();
var service = host.Services.GetRequiredService<IMyService>();  // 服务可能已释放
```

### 9. 循环依赖

```csharp
// 错误：A 依赖 B，B 依赖 A
public class ServiceA { public ServiceA(ServiceB b) { } }
public class ServiceB { public ServiceB(ServiceA a) { } }
// DI 容器会抛出 InvalidOperationException

// 解决：引入第三个服务，或使用事件/委托打破循环
```

### 10. 忘记释放作用域

```csharp
// 不好的做法：忘记释放 scope
var scope = _services.CreateScope();
var svc = scope.ServiceProvider.GetRequiredService<IMyService>();
// scope 没有被释放，Scoped 服务不会被清理

// 好的做法：使用 using
using var scope = _services.CreateScope();
var svc = scope.ServiceProvider.GetRequiredService<IMyService>();
```

### 11. 在 Avalonia 线程中使用阻塞式 DI 解析

```csharp
// 不好的做法：在 UI 线程中同步解析可能阻塞的服务
var service = _services.GetRequiredService<IHeavyService>();  // 如果构造函数阻塞...

// 好的做法：预解析轻量级服务，重量级服务异步初始化
public class ViewModel
{
    private readonly Lazy<Task<IHeavyService>> _heavyService;

    public ViewModel(IServiceProvider services)
    {
        _heavyService = new Lazy<Task<IHeavyService>>(() =>
            Task.Run(() => services.GetRequiredService<IHeavyService>()));
    }
}
```

### 12. 配置绑定的大小写敏感

```csharp
// 配置键默认不区分大小写
config["proxy:port"]  // OK
config["Proxy:Port"]  // OK（同一个值）

// 但绑定到对象时，属性名必须匹配
public class Settings
{
    public int Port { get; set; }  // 匹配 "Port"
    // public int port { get; set; }  // 不匹配（C# 属性名大写开头）
}
```

## Try It Yourself

### 练习 1：基础 DI 设置

创建一个最小的 Avalonia 项目，使用 `WithServices()` 注册一个 `IAppPaths` 服务，在 ViewModel 中注入并使用它。

### 练习 2：Generic Host 集成

将练习 1 的项目升级为使用 Generic Host，添加 `appsettings.json` 配置文件和 `IConfiguration` 支持。

### 练习 3：Serilog 日志

为练习 2 的项目添加 Serilog，配置控制台和文件两个 Sink，实现不同命名空间不同日志级别。

### 练习 4：强类型配置

定义一个 `AppSettings` 类，通过 `IOptions<T>` 绑定到 `appsettings.json`，实现运行时修改配置文件后自动更新。

### 练习 5：后台服务

创建一个 `PeriodicTimer` 驱动的后台服务，每 10 秒检查一次某个 URL 是否可达，通过事件通知 UI 更新状态。

### 练习 6：HttpClientFactory

使用 `IHttpClientFactory` 创建一个类型化的 API 客户端，添加 Polly 重试策略（3 次，指数退避）和断路器策略。

### 练习 7：健康检查

为你的应用添加自定义健康检查：检查磁盘空间（> 100MB）、检查配置文件是否存在、检查 API 是否可达。映射 `/health` 端点。

### 练习 8：后台任务队列

使用 `Channel<T>` 实现一个后台任务队列，支持任务入队、异步处理和优雅关闭。实现一个下载图标的任务处理器。

### 练习 9：配置验证

为 `ProxySettings` 添加数据注解验证（端口范围 1-65535、Host 非空），在应用启动时自动验证，失败时给出清晰的错误消息。

### 练习 10：内存缓存

为配置文件添加内存缓存，设置 5 分钟绝对过期。实现缓存失效时自动从磁盘重新加载。考虑缓存穿透和缓存击穿的防护。
