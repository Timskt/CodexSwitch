# 38. 依赖注入与构造函数自动注入

> **写给零基础的你**：构造函数注入就像"点外卖"——你只需要在"订单"（构造函数）里写清楚要什么，"外卖平台"（DI 容器）会自动帮你准备好所有东西。你不需要自己去买菜、做饭、打包，DI 容器全部搞定。

## 38.1 概述

### 38.1.1 什么是构造函数注入

构造函数注入（Constructor Injection）是依赖注入（Dependency Injection, DI）的一种最常见形式。简单来说，一个类通过构造函数声明它需要哪些依赖，DI 容器在创建该类的实例时自动提供这些依赖。

**没有构造函数注入时**（手动创建依赖）：

```csharp
// 手动创建所有依赖——像自己去买菜做饭
public class MainWindowViewModel
{
    private readonly AppPaths _paths;
    private readonly ConfigurationStore _store;
    private readonly PriceCalculator _priceCalculator;

    public MainWindowViewModel()
    {
        _paths = new AppPaths();                          // 自己创建
        _store = new ConfigurationStore(_paths);           // 自己创建
        _priceCalculator = new PriceCalculator(pricing);   // 自己创建
    }
}
```

**有构造函数注入时**（容器自动提供）：

```csharp
// 声明需要什么——像点外卖
public class MainWindowViewModel
{
    private readonly AppPaths _paths;
    private readonly ConfigurationStore _store;
    private readonly PriceCalculator _priceCalculator;

    public MainWindowViewModel(
        AppPaths paths,                    // 容器自动提供
        ConfigurationStore store,          // 容器自动提供
        PriceCalculator priceCalculator)   // 容器自动提供
    {
        _paths = paths;
        _store = store;
        _priceCalculator = priceCalculator;
    }
}
```

### 38.1.2 为什么需要自动注入

| 手动创建 | 自动注入 |
|---------|---------|
| 类之间紧耦合，难以替换实现 | 通过接口解耦，轻松替换实现 |
| 构造函数中大量样板代码 | 构造函数只声明依赖，干净清晰 |
| 测试时难以 mock 依赖 | 测试时注入 mock 对象即可 |
| 依赖关系散落在各处 | 所有注册集中在 `ConfigureServices` |
| 生命周期需要手动管理 | 容器自动管理创建和销毁 |
| 重构时容易遗漏 | 容器编译时检查依赖是否可用 |

### 38.1.3 Avalonia 中 DI 的挑战

Avalonia 作为 UI 框架，与 DI 集成时面临一些独特的挑战：

1. **窗口/页面由框架实例化**：Avalonia 的 XAML 解析器会调用无参构造函数来创建控件，这与构造函数注入天然冲突
2. **DataContext 绑定时机**：ViewModel 需要在 View 的构造函数完成之前设置好
3. **生命周期不对齐**：DI 容器的生命周期和 Avalonia 的窗口/页面生命周期不同步
4. **静态服务定位器**：很多 Avalonia 生态中的服务（如 `I18nService.Current`）使用了静态单例模式
5. **AOT 兼容性**：Native AOT 发布时，反射受限，需要源生成 DI

本章将逐步解决这些挑战，提供完整的实战方案。

---

## 38.2 基础：.NET 依赖注入核心概念

### 38.2.1 IServiceCollection 注册服务

`IServiceCollection` 是 DI 容器的"菜单"，你在上面列出所有可以提供的"菜品"（服务）。

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// 注册具体类（每次都创建新实例）
services.AddTransient<PriceCalculator>();

// 注册具体类（全局只创建一个实例）
services.AddSingleton<AppPaths>();

// 注册接口到实现的映射
services.AddSingleton<IConfigurationStore, ConfigurationStore>();

// 注册实例（你提供已创建的对象）
var existingConfig = new AppConfig();
services.AddSingleton(existingConfig);
```

### 38.2.2 三种生命周期

```
┌───────────────────────────────────────────────────────────┐
│                    DI 容器 (ServiceProvider)                │
│                                                           │
│  Singleton（单例）：整个应用只有一个实例                    │
│  ┌──────────────────┐                                     │
│  │  AppPaths         │  创建 1 次，永远复用                │
│  │  I18nService      │  相当于 static 实例                 │
│  └──────────────────┘                                     │
│                                                           │
│  Transient（瞬时）：每次请求都创建新实例                    │
│  ┌──┐ ┌──┐ ┌──┐                                          │
│  │VM│ │VM│ │VM│  每次 GetService 都是新对象               │
│  └──┘ └──┘ └──┘                                           │
│                                                           │
│  Scoped（作用域）：同一作用域内共享                         │
│  ┌────────────────────┐                                   │
│  │  Scope A: 共享 1 个 │  作用域内每次请求返回同一实例     │
│  └────────────────────┘                                   │
└───────────────────────────────────────────────────────────┘
```

```csharp
services.AddSingleton<AppPaths>();                    // 全局唯一
services.AddTransient<MainWindowViewModel>();          // 每次新实例
services.AddScoped<UserSession>();                    // 同作用域内共享
```

### 38.2.3 IServiceProvider 解析服务

`IServiceProvider` 是 DI 容器的"取餐窗口"——你报出服务类型，它把对应实例交给你。

```csharp
// 构建容器
var provider = services.BuildServiceProvider();

// 解析服务
var paths = provider.GetRequiredService<AppPaths>();        // 必须存在，否则抛异常
var config = provider.GetService<AppConfig>();               // 可能返回 null
var viewModel = provider.GetRequiredService<MainWindowViewModel>();

// 解析集合
var adapters = provider.GetServices<IProviderProtocolAdapter>(); // 获取所有实现
```

### 38.2.4 构造函数注入的基本用法

```csharp
// 1. 定义服务接口
public interface IGreeter
{
    string Greet(string name);
}

// 2. 实现服务
public class Greeter : IGreeter
{
    public string Greet(string name) => $"你好，{name}！";
}

// 3. 使用构造函数注入
public class UserController
{
    private readonly IGreeter _greeter;

    // 容器看到构造函数需要 IGreeter，自动提供 Greeter 实例
    public UserController(IGreeter greeter)
    {
        _greeter = greeter;
    }

    public string Welcome(string name) => _greeter.Greet(name);
}

// 4. 注册并使用
services.AddSingleton<IGreeter, Greeter>();
services.AddTransient<UserController>();

var controller = provider.GetRequiredService<UserController>();
Console.WriteLine(controller.Welcome("小明")); // 输出：你好，小明！
```

> **小白提示**：容器是怎么知道要注入什么的？它通过**反射**读取构造函数的参数类型，然后在自己管理的服务列表中找到匹配的。如果有多个构造函数，容器默认选择参数最多的那个。

---

## 38.3 Avalonia 集成 Host 的完整方案

### 38.3.1 三种集成方案对比

在 Avalonia 中使用 DI，有三种主流方案：

```
方案 1：AppBuilder + Host（推荐）     方案 2：Host + Avalonia
┌─────────────┐                       ┌──────────────────────┐
│ AppBuilder   │                       │ Host.CreateDefault() │
│   .Configure │                       │   .ConfigureServices│
│   <App>()    │                       │   .Build()           │
│              │                       │                      │
│  App.cs 中   │                       │  Program.cs 中       │
│  创建 Host   │                       │  创建 Avalonia       │
└─────────────┘                       └──────────────────────┘

方案 3：混合模式
┌──────────────────────────────────────┐
│  Program.cs 创建 Host + AppBuilder   │
│  IServiceProvider 传入 App           │
└──────────────────────────────────────┘
```

| 特性 | 方案 1：AppBuilder 内 Host | 方案 2：Host 内 Avalonia | 方案 3：混合模式 |
|------|---------------------------|-------------------------|----------------|
| 配置文件支持 | 支持 | 支持 | 支持 |
| 日志集成 | 支持 | 支持 | 支持 |
| Avalonia 生命周期兼容 | 完美 | 需要额外处理 | 良好 |
| 配置复杂度 | 中等 | 较高 | 最低 |
| AOT 兼容 | 良好 | 良好 | 良好 |
| 推荐度 | 推荐 | 可用 | 最灵活 |

### 38.3.2 方案 1：AppBuilder 内创建 Host（推荐）

这是社区最推荐的方案。核心思想是在 `App` 类中创建和持有 Host：

```csharp
// App.axaml.cs
public partial class App : Application
{
    private IHost? _host;

    public override void OnFrameworkInitializationCompleted()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(context, services);
            })
            .Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // 注册服务
        services.AddSingleton<AppPaths>();
        services.AddSingleton<ConfigurationStore>();
        // ... 更多注册
    }

    public override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}
```

**优点**：Avalonia 生命周期完全由 AppBuilder 管理，Host 作为辅助容器。
**缺点**：服务解析只能在 `OnFrameworkInitializationCompleted` 之后进行。

### 38.3.3 方案 2：Host 内启动 Avalonia

将 Avalonia 视为 Host 的一个"托管服务"：

```csharp
// Program.cs
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // 注册所有服务
                ConfigureServices(services);
                // 注册 Avalonia 生命周期为托管服务
                services.AddSingleton<IAvaloniaAppService, AvaloniaAppService>();
            })
            .Build();

        // 手动启动 Avalonia
        BuildAvaloniaApp(host.Services)
            .StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp(IServiceProvider services)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new FontManagerOptions { DefaultFamilyName = "Inter" })
            .LogToTrace();
}
```

**优点**：所有服务在 Host 中统一管理。
**缺点**：需要在 `App` 中接收 `IServiceProvider`，增加了传递依赖。

### 38.3.4 方案 3：混合模式（最灵活）

将 Host 的 `IServiceProvider` 通过静态属性或附加属性传递给 Avalonia：

```csharp
// Program.cs
public static class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();

        Services = host.Services;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }
}
```

**优点**：最简单，任何地方都能通过 `Program.Services` 解析服务。
**缺点**：静态访问是服务定位器模式（反模式），不利于测试。

### 38.3.5 推荐的完整配置

综合考虑易用性、可测试性和 AOT 兼容性，推荐方案 1。以下是完整的推荐配置骨架：

```csharp
// Program.cs
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
            .LogToTrace();
}

// App.axaml.cs — 在此创建 Host 并注册所有服务
public partial class App : Application
{
    private IHost? _host;

    public IServiceProvider Services => _host?.Services
        ?? throw new InvalidOperationException("Host 尚未初始化");

    public override void OnFrameworkInitializationCompleted()
    {
        _host = CreateHost();
        ResolveAndShowMainWindow();
        base.OnFrameworkInitializationCompleted();
    }

    private IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    private void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
    {
        // 注册所有服务、ViewModel、页面
        RegisterServices(services);
        RegisterViewModels(services);
        RegisterPages(services);
    }

    public override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}
```

---

## 38.4 Program.cs 完整配置

### 38.4.1 使用 Host.CreateDefaultBuilder()

`Host.CreateDefaultBuilder()` 提供了一站式的配置能力：

```csharp
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // 配置文件
        config.SetBasePath(AppContext.BaseDirectory);
        config.AddJsonFile("appsettings.json", optional: true);
        config.AddJsonFile(
            $"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
            optional: true);
        config.AddEnvironmentVariables();

        if (args.Length > 0)
            config.AddCommandLine(args);
    })
    .ConfigureServices((context, services) =>
    {
        // 从配置文件绑定强类型配置
        services.Configure<AppSettings>(
            context.Configuration.GetSection("App"));
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();
```

### 38.4.2 完整的 ConfigureServices 示例

```csharp
private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
{
    // ===== 基础设施 =====
    // 路径服务（全局唯一）
    services.AddSingleton<AppPaths>();

    // 配置存储
    services.AddSingleton<ConfigurationStore>();

    // 加载配置并注册为实例
    var paths = new AppPaths();
    var store = new ConfigurationStore(paths);
    var config = store.LoadConfig();
    var pricing = store.LoadPricing();
    services.AddSingleton(config);
    services.AddSingleton(pricing);

    // ===== 业务服务 =====
    services.AddSingleton<PriceCalculator>();
    services.AddSingleton<UsageMeter>();
    services.AddSingleton<UsageLogWriter>();
    services.AddSingleton<UsageLogReader>();
    services.AddSingleton<CodexConfigWriter>();
    services.AddSingleton<ClaudeCodeConfigWriter>();
    services.AddSingleton<CodexSessionMigrationService>();
    services.AddSingleton<StartupRegistrationService>();
    services.AddSingleton<ProviderAuthService>();

    // ===== 协议适配器 =====
    services.AddSingleton<IProviderProtocolAdapter, OpenAiResponsesAdapter>();
    services.AddSingleton<IProviderProtocolAdapter, OpenAiChatAdapter>();
    services.AddSingleton<IProviderProtocolAdapter, AnthropicMessagesAdapter>();

    // ===== 代理服务 =====
    services.AddSingleton<ProxyHostService>();

    // ===== ViewModel =====
    services.AddTransient<MainWindowViewModel>();

    // ===== 页面 =====
    services.AddTransient<ProvidersPage>();
    services.AddTransient<SettingsPage>();
    services.AddTransient<HomePage>();
    services.AddTransient<ModelsPage>();
    services.AddTransient<UsagePage>();

    // ===== HTTP 客户端 =====
    services.AddHttpClient("default", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CodexSwitch/1.0");
    });
}
```

### 38.4.3 从配置文件加载服务注册

```csharp
// appsettings.json
{
    "Proxy": {
        "Host": "127.0.0.1",
        "Port": 11434,
        "AuthMode": "None"
    },
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Proxy": "Debug"
        }
    }
}

// 注册时绑定
services.Configure<ProxySettings>(
    context.Configuration.GetSection("Proxy"));

// 在服务中使用
public class ProxyHostService
{
    private readonly ProxySettings _settings;

    public ProxyHostService(IOptions<ProxySettings> options)
    {
        _settings = options.Value;
    }
}
```

---

## 38.5 ViewModel 的构造函数注入

### 38.5.1 ViewModel 注入服务

这是最常见的场景——ViewModel 需要各种业务服务来完成工作。

**改造前**（CodexSwitch 当前的手动创建方式）：

```csharp
public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly AppPaths _paths;
    private readonly ConfigurationStore _store;
    private readonly PriceCalculator _priceCalculator;
    private readonly UsageMeter _usageMeter;
    private readonly UsageLogWriter _usageLogWriter;
    private readonly UsageLogReader _usageLogReader;
    private readonly CodexConfigWriter _codexConfigWriter;
    private readonly ClaudeCodeConfigWriter _claudeCodeConfigWriter;
    private readonly I18nService _i18n;

    public MainWindowViewModel()
    {
        // 手动创建每一个依赖——大量的样板代码
        _paths = new AppPaths();
        _store = new ConfigurationStore(_paths);
        _config = _store.LoadConfig();
        _i18n = I18nService.Current;
        _pricing = _store.LoadPricing();
        _priceCalculator = new PriceCalculator(_pricing);
        _usageMeter = new UsageMeter(_priceCalculator);
        _usageLogWriter = new UsageLogWriter(_paths);
        _usageLogReader = new UsageLogReader(_paths);
        _codexConfigWriter = new CodexConfigWriter(_paths);
        _claudeCodeConfigWriter = new ClaudeCodeConfigWriter(_paths);
        // ... 还有更多
    }
}
```

**改造后**（使用构造函数注入）：

```csharp
public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly AppPaths _paths;
    private readonly ConfigurationStore _store;
    private readonly PriceCalculator _priceCalculator;
    private readonly UsageMeter _usageMeter;
    private readonly UsageLogWriter _usageLogWriter;
    private readonly UsageLogReader _usageLogReader;
    private readonly CodexConfigWriter _codexConfigWriter;
    private readonly ClaudeCodeConfigWriter _claudeCodeConfigWriter;
    private readonly I18nService _i18n;

    public MainWindowViewModel(
        AppPaths paths,
        ConfigurationStore store,
        PriceCalculator priceCalculator,
        UsageMeter usageMeter,
        UsageLogWriter usageLogWriter,
        UsageLogReader usageLogReader,
        CodexConfigWriter codexConfigWriter,
        ClaudeCodeConfigWriter claudeCodeConfigWriter,
        I18nService i18n)
    {
        _paths = paths;
        _store = store;
        _priceCalculator = priceCalculator;
        _usageMeter = usageMeter;
        _usageLogWriter = usageLogWriter;
        _usageLogReader = usageLogReader;
        _codexConfigWriter = codexConfigWriter;
        _claudeCodeConfigWriter = claudeCodeConfigWriter;
        _i18n = i18n;
    }
}
```

构造函数变得干净了——只声明依赖，不负责创建。

### 38.5.2 ViewModel 注入其他 ViewModel

有时一个 ViewModel 需要创建或引用另一个 ViewModel：

```csharp
// 子 ViewModel
public class ProviderDetailViewModel : ViewModelBase
{
    private readonly ProviderAuthService _authService;

    public ProviderDetailViewModel(ProviderAuthService authService)
    {
        _authService = authService;
    }
}

// 父 ViewModel 通过工厂注入子 ViewModel
public class MainWindowViewModel : ViewModelBase
{
    private readonly Func<ProviderDetailViewModel> _providerDetailFactory;

    public MainWindowViewModel(
        Func<ProviderDetailViewModel> providerDetailFactory)
    {
        _providerDetailFactory = providerDetailFactory;
    }

    public void ShowProviderDetail(string providerId)
    {
        var detailViewModel = _providerDetailFactory();
        // 使用 detailViewModel ...
    }
}
```

### 38.5.3 IServiceProvider 直接注入

当需要在运行时动态解析服务时，可以注入 `IServiceProvider`：

```csharp
public class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;

    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo(string pageName)
    {
        // 根据页面名称动态解析对应的页面
        var page = pageName switch
        {
            "Providers" => _serviceProvider.GetRequiredService<ProvidersPage>(),
            "Settings" => _serviceProvider.GetRequiredService<SettingsPage>(),
            "Home" => _serviceProvider.GetRequiredService<HomePage>(),
            _ => throw new ArgumentException($"未知页面: {pageName}")
        };
        CurrentPage = page;
    }
}
```

> **注意**：直接注入 `IServiceProvider` 本质上是服务定位器模式，应尽量避免。优先使用强类型的 `Func<T>` 工厂或 `IEnumerable<T>` 注入。

### 38.5.4 命名服务注入（Keyed Services）

.NET 8+ 引入了 Keyed Services，允许按名称注册和解析服务：

```csharp
// 注册命名服务
services.AddKeyedSingleton<IStorage>("local", new LocalStorage());
services.AddKeyedSingleton<IStorage>("cloud", new CloudStorage());

// 注入命名服务
public class FileManagerViewModel : ViewModelBase
{
    private readonly IStorage _localStorage;
    private readonly IStorage _cloudStorage;

    public FileManagerViewModel(
        [FromKeyedServices("local")] IStorage localStorage,
        [FromKeyedServices("cloud")] IStorage cloudStorage)
    {
        _localStorage = localStorage;
        _cloudStorage = cloudStorage;
    }
}
```

### 38.5.5 构造函数注入 + 无参构造函数共存

Avalonia 的 XAML 解析器有时需要无参构造函数（例如设计时预览）。可以同时提供两个构造函数：

```csharp
public class ProvidersPageViewModel : ViewModelBase
{
    private readonly ProviderAuthService _authService;

    // 设计时/XAML 用的无参构造函数
    public ProvidersPageViewModel() : this(
        DesignMode.IsDesignMode
            ? new ProviderAuthService(/* 设计时 mock */)
            : throw new InvalidOperationException("请使用 DI 容器创建"))
    {
    }

    // DI 容器用的注入构造函数
    public ProvidersPageViewModel(ProviderAuthService authService)
    {
        _authService = authService;
    }
}
```

### 38.5.6 完整的 ViewModel 注入示例

以下是结合 `CommunityToolkit.Mvvm` 和构造函数注入的完整 ViewModel：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly AppPaths _paths;
    private readonly ConfigurationStore _store;
    private readonly ProxyHostService _proxyHost;
    private readonly I18nService _i18n;
    private readonly AppThemeService _themeService;
    private readonly IServiceProvider _serviceProvider;
    private AppConfig _config;

    // 通过构造函数注入所有依赖
    public MainWindowViewModel(
        AppPaths paths,
        ConfigurationStore store,
        ProxyHostService proxyHost,
        I18nService i18n,
        AppThemeService themeService,
        IServiceProvider serviceProvider)
    {
        _paths = paths;
        _store = store;
        _proxyHost = proxyHost;
        _i18n = i18n;
        _themeService = themeService;
        _serviceProvider = serviceProvider;

        // 初始化逻辑
        _config = _store.LoadConfig();
        _i18n.SetLanguage(_config.Ui.Language);
        _themeService.Apply(_config.Ui.Theme);
    }

    // ObservableProperty 由源生成器自动处理
    [ObservableProperty]
    private string _currentPage = "Home";

    [ObservableProperty]
    private string _proxyStatus = "";

    // 命令
    [RelayCommand]
    private async Task ToggleProxyAsync()
    {
        if (_proxyHost.State.IsRunning)
            await _proxyHost.StopAsync();
        else
            await _proxyHost.StartAsync(_config);
    }

    // IAsyncDisposable 实现——容器会自动调用
    public async ValueTask DisposeAsync()
    {
        await _proxyHost.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
```

---

## 38.6 View（页面/窗口）的构造函数注入

### 38.6.1 核心问题：XAML 与构造函数注入的冲突

Avalonia 的 XAML 解析器在解析 `<local:ProvidersPage />` 时，会调用**无参构造函数**来创建控件实例。这意味着直接对 View 使用构造函数注入会遇到问题：

```csharp
// 这样写会导致 XAML 解析器报错——它找不到无参构造函数
public partial class ProvidersPage : UserControl
{
    public ProvidersPage(ProvidersPageViewModel viewModel) // XAML 无法调用这个
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

### 38.6.2 解决方案 A：从 DI 容器解析页面（推荐）

不在 XAML 中声明页面实例，而是在代码中从 DI 容器解析：

```csharp
// 1. 页面保持无参构造函数（XAML 友好）
public partial class ProvidersPage : UserControl
{
    public ProvidersPage()
    {
        InitializeComponent();
    }
}

// 2. 在 ViewModel 或 App 中从容器解析并设置 DataContext
public class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private UserControl? _currentPage;

    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    private void NavigateTo(string pageName)
    {
        CurrentPage = pageName switch
        {
            "Providers" => CreatePage<ProvidersPage, ProvidersPageViewModel>(),
            "Settings" => CreatePage<SettingsPage, SettingsPageViewModel>(),
            "Home" => CreatePage<HomePage, HomePageViewModel>(),
            _ => throw new ArgumentException($"未知页面: {pageName}")
        };
    }

    private TPage CreatePage<TPage, TViewModel>()
        where TPage : UserControl, new()
        where TViewModel : class
    {
        var page = new TPage();
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        page.DataContext = viewModel;
        return page;
    }
}
```

### 38.6.3 解决方案 B：View 注入 ViewModel（严格 DI）

如果坚持让 View 也通过 DI 管理，需要从 DI 容器中解析 View 而不是在 XAML 中声明：

```csharp
// 1. 页面有注入构造函数
public partial class ProvidersPage : UserControl
{
    public ProvidersPage(ProvidersPageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

// 2. 注册页面时使用工厂
services.AddTransient<ProvidersPage>(sp =>
{
    var viewModel = sp.GetRequiredService<ProvidersPageViewModel>();
    return new ProvidersPage(viewModel);
});

// 3. 从容器解析页面
var page = serviceProvider.GetRequiredService<ProvidersPage>();
```

### 38.6.4 解决方案 C：附加属性自动注入

创建一个附加属性，在页面加载时自动从 DI 容器解析并设置 DataContext：

```csharp
public static class AutoDataContext
{
    public static readonly AttachedProperty<Type?> ViewModelTypeProperty =
        AvaloniaProperty.RegisterAttached<Control, Type?>(
            "ViewModelType", typeof(AutoDataContext));

    public static void SetViewModelType(Control element, Type? value)
        => element.SetValue(ViewModelTypeProperty, value);

    public static Type? GetViewModelType(Control element)
        => element.GetValue(ViewModelTypeProperty);

    static AutoDataContext()
    {
        ViewModelTypeProperty.Changed.AddClassHandler<Control>(OnViewModelTypeChanged);
    }

    private static void OnViewModelTypeChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is Type viewModelType && App.Current is App app)
        {
            var viewModel = app.Services.GetRequiredService(viewModelType);
            control.DataContext = viewModel;
        }
    }
}
```

在 XAML 中使用：

```xml
<UserControl
    xmlns:local="using:MyApp.ViewModels"
    xmlns:di="using:MyApp.Helpers"
    di:AutoDataContext.ViewModelType="local:ProvidersPageViewModel">
    <!-- DataContext 自动设置 -->
</UserControl>
```

### 38.6.5 窗口的注入

窗口的注入和页面类似，但通常在 `App.OnFrameworkInitializationCompleted` 中完成：

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        // 从 DI 容器解析 ViewModel
        var viewModel = _host.Services.GetRequiredService<MainWindowViewModel>();

        // 创建窗口并设置 DataContext
        desktop.MainWindow = new MainWindow
        {
            DataContext = viewModel
        };

        // 子窗口也需要从容器解析
        desktop.ShutdownRequested += (_, _) =>
        {
            if (viewModel is IAsyncDisposable asyncDisposable)
                asyncDisposable.DisposeAsync().AsTask().Wait();
        };
    }

    base.OnFrameworkInitializationCompleted();
}
```

---

## 38.7 服务注册最佳实践

### 38.7.1 接口注册 vs 实现注册

```csharp
// 方式 1：接口注册（推荐用于可替换的服务）
services.AddSingleton<IConfigurationStore, ConfigurationStore>();

// 方式 2：直接注册实现类（适用于不需要替换的类）
services.AddSingleton<AppPaths>();

// 方式 3：注册为多个接口
services.AddSingleton<ProxyHostService>();
services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ProxyHostService>());
```

**何时用接口注册**：
- 需要在测试中 mock
- 有多种实现（如协议适配器）
- 实现可能在未来替换

**何时用实现注册**：
- 只有一种实现且不会变化
- 类本身已经是抽象（如 `AppPaths`）
- 简化注册减少样板代码

### 38.7.2 程序集扫描自动注册

手动逐个注册服务很繁琐。使用 `Scrutor` 库可以自动扫描并注册：

```bash
dotnet add package Scrutor
```

```csharp
using Scrutor;

services.Scan(scan => scan
    .FromAssemblyOf<MainWindowViewModel>()
    // 扫描 Services 命名空间下的所有类
    .AddClasses(classes => classes
        .InNamespaces("CodexSwitch.Services"))
    .AsImplementedInterfaces()
    .WithSingletonLifetime());

// 更多扫描规则
services.Scan(scan => scan
    .FromAssemblyOf<MainWindowViewModel>()
    // 注册所有 ViewModel
    .AddClasses(classes => classes
        .InNamespaces("CodexSwitch.ViewModels"))
    .AsSelf()
    .WithTransientLifetime()
    // 注册所有协议适配器
    .AddClasses(classes => classes
        .AssignableTo<IProviderProtocolAdapter>())
    .AsImplementedInterfaces()
    .WithSingletonLifetime());
```

### 38.7.3 条件注册

```csharp
// 只在未注册时注册（避免覆盖）
services.TryAddSingleton<IEmailSender, SmtpEmailSender>();

// 只在未注册该接口时注册
services.TryAddSingleton<IEmailSender, SmtpEmailSender>();
services.TryAddSingleton<IEmailSender, SendGridEmailSender>(); // 不会覆盖上面的

// 替换已注册的服务
services.AddSingleton<IEmailSender, SmtpEmailSender>();
services.Replace(ServiceDescriptor.Singleton<IEmailSender, SendGridEmailSender>());

// 移除已注册的服务
services.RemoveAll<IEmailSender>();
```

### 38.7.4 装饰器注册

```csharp
// 基础注册
services.AddSingleton<IUsageLogWriter, UsageLogWriter>();

// 添加装饰器——在不修改原始类的情况下增加功能
services.Decorate<IUsageLogWriter, UsageLogWriterWithMetrics>();

// 装饰器类
public class UsageLogWriterWithMetrics : IUsageLogWriter
{
    private readonly IUsageLogWriter _inner;
    private readonly ILogger<UsageLogWriterWithMetrics> _logger;

    public UsageLogWriterWithMetrics(
        IUsageLogWriter inner,
        ILogger<UsageLogWriterWithMetrics> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public void Write(UsageLogEntry entry)
    {
        _logger.LogInformation("写入用量日志: {Model}", entry.Model);
        _inner.Write(entry);
    }
}
```

### 38.7.5 开放泛型注册

```csharp
// 注册开放泛型
services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));

// 解析时自动填充类型参数
var providerRepo = serviceProvider.GetRequiredService<IRepository<Provider>>();
var modelRepo = serviceProvider.GetRequiredService<IRepository<Model>>();

// 实现
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task SaveAsync(T entity);
}

public class Repository<T> : IRepository<T> where T : class
{
    private readonly AppPaths _paths;

    public Repository(AppPaths paths) => _paths = paths;

    public async Task<T?> GetByIdAsync(string id) { /* ... */ }
    public async Task SaveAsync(T entity) { /* ... */ }
}
```

---

## 38.8 服务作用域详解

### 38.8.1 Transient（瞬时）

```csharp
services.AddTransient<PriceCalculator>();

// 每次解析都获得新实例
var calc1 = provider.GetRequiredService<PriceCalculator>();
var calc2 = provider.GetRequiredService<PriceCalculator>();
Console.WriteLine(ReferenceEquals(calc1, calc2)); // False
```

**适用场景**：无状态的服务、每次操作需要独立状态的服务。

### 38.8.2 Singleton（单例）

```csharp
services.AddSingleton<AppPaths>();

// 每次解析都获得同一个实例
var paths1 = provider.GetRequiredService<AppPaths>();
var paths2 = provider.GetRequiredService<AppPaths>();
Console.WriteLine(ReferenceEquals(paths1, paths2)); // True
```

**适用场景**：全局配置、状态管理、需要跨组件共享数据的服务。

### 38.8.3 Scoped（作用域）

```csharp
services.AddScoped<UserSession>();

// 创建作用域
using var scope1 = provider.CreateScope();
using var scope2 = provider.CreateScope();

var session1a = scope1.ServiceProvider.GetRequiredService<UserSession>();
var session1b = scope1.ServiceProvider.GetRequiredService<UserSession>();
var session2 = scope2.ServiceProvider.GetRequiredService<UserSession>();

Console.WriteLine(ReferenceEquals(session1a, session1b)); // True（同一作用域）
Console.WriteLine(ReferenceEquals(session1a, session2));   // False（不同作用域）
```

**适用场景**：ASP.NET Core 请求生命周期、需要在一组操作中共享状态的场景。

### 38.8.4 作用域在 Avalonia 中的陷阱

> **重要警告**：在 Avalonia 桌面应用中，Scoped 生命周期几乎没有实际意义！
>
> ASP.NET Core 的 Scoped 对应一个 HTTP 请求，但 Avalonia 是长生命周期的桌面应用，没有"请求"的概念。如果错误地使用 Scoped 注册一个有状态的服务，它可能会存活很长时间，导致意外的内存泄漏或数据不一致。

```csharp
// 错误示范：Scoped 在 Avalonia 中可能存活到应用关闭
services.AddScoped<ProviderAuthService>(); // 千万不要这样做！

// 正确做法：根据实际需求选择 Singleton 或 Transient
services.AddSingleton<ProviderAuthService>();  // 需要全局共享
services.AddTransient<ProviderAuthService>();   // 每次独立
```

**何时在 Avalonia 中使用 Scoped**：只有当你明确创建了 `IServiceScope` 并在完成后释放时才使用：

```csharp
// 显式创建和销毁作用域
using (var scope = serviceProvider.CreateScope())
{
    var service = scope.ServiceProvider.GetRequiredService<SomeScopedService>();
    // 使用 service ...
} // scope 被释放，scoped 服务也被释放
```

---

## 38.9 IServiceLocator 模式（不推荐但有时需要）

### 38.9.1 服务定位器模式

服务定位器（Service Locator）是一个静态类或单例，提供全局的服务解析能力：

```csharp
// 服务定位器实现
public static class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static T GetRequiredService<T>() where T : notnull
    {
        return _serviceProvider is null
            ? throw new InvalidOperationException("ServiceLocator 尚未初始化")
            : _serviceProvider.GetRequiredService<T>();
    }

    public static T? GetService<T>()
    {
        return _serviceProvider?.GetService<T>();
    }
}

// 使用
var themeService = ServiceLocator.GetRequiredService<IAppThemeService>();
```

### 38.9.2 为什么是反模式

| 构造函数注入（推荐） | 服务定位器（反模式） |
|---------------------|---------------------|
| 依赖在构造函数中显式声明 | 依赖隐藏在方法体中 |
| 看构造函数就知道需要什么 | 需要读完整个类才能知道依赖 |
| 编译时检查——缺少依赖会报错 | 运行时才发现服务未注册 |
| 易于单元测试——直接传 mock | 测试时需要配置全局容器 |
| 依赖关系图清晰可追踪 | 依赖关系图不可追踪 |

### 38.9.3 何时不得不使用

在 Avalonia 中，以下场景可能不得不使用服务定位器：

1. **附加属性/标记扩展**：无法通过构造函数注入

```csharp
// 标记扩展无法使用构造函数注入
public class TrExtension : MarkupExtension
{
    public TrExtension(string key) => Key = key;
    public string Key { get; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // 只能通过服务定位器获取 I18nService
        var i18n = ServiceLocator.GetRequiredService<I18nService>();
        return i18n[Key];
    }
}
```

2. **静态工具方法**：需要访问服务但无法注入

```csharp
public static class AppThemeService
{
    public static void Apply(string? theme)
    {
        var app = Application.Current;
        // 静态方法无法注入，只能直接使用 Application.Current
    }
}
```

3. **事件处理器**：框架回调中无法控制参数

```csharp
// Avalonia 的路由事件处理器
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    // 事件处理器的签名由框架定义，无法添加参数
    // 如果需要服务，只能通过服务定位器或成员变量
}
```

### 38.9.4 Avalonia 中的最佳实践

尽量减少服务定位器的使用。如果必须使用，将其限制在"边缘层"（UI 代码），核心业务逻辑始终使用构造函数注入：

```csharp
// 边缘层：UI 代码可能需要服务定位器
public partial class MainWindow : Window
{
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 只在 UI 边缘使用服务定位器
        var service = ServiceLocator.GetRequiredService<SomeService>();
        service.DoSomething();
    }
}

// 核心层：ViewModel 始终使用构造函数注入
public class MainWindowViewModel : ViewModelBase
{
    private readonly SomeService _service;

    public MainWindowViewModel(SomeService service) // 纯构造函数注入
    {
        _service = service;
    }
}
```

---

## 38.10 工厂模式注入

### 38.10.1 Func<T> 工厂

当需要延迟创建服务或创建多个实例时，使用 `Func<T>` 工厂：

```csharp
// 注册工厂
services.AddSingleton<Func<ProviderDetailViewModel>>(sp =>
    () => new ProviderDetailViewModel(
        sp.GetRequiredService<ProviderAuthService>(),
        sp.GetRequiredService<ConfigurationStore>()));

// 注入工厂
public class MainWindowViewModel : ViewModelBase
{
    private readonly Func<ProviderDetailViewModel> _detailFactory;

    public MainWindowViewModel(Func<ProviderDetailViewModel> detailFactory)
    {
        _detailFactory = detailFactory;
    }

    public void ShowProviderDetail(string providerId)
    {
        // 每次调用工厂都创建新实例
        var detail = _detailFactory();
        detail.LoadProvider(providerId);
        // ...
    }
}
```

### 38.10.2 自定义工厂接口

对于复杂的创建逻辑，定义专用工厂接口更清晰：

```csharp
// 工厂接口
public interface IViewModelFactory<T> where T : ViewModelBase
{
    T Create();
}

// 工厂实现
public class ViewModelFactory<T> : IViewModelFactory<T> where T : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;

    public ViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T Create() => _serviceProvider.GetRequiredService<T>();
}

// 注册开放泛型工厂
services.AddSingleton(typeof(IViewModelFactory<>), typeof(ViewModelFactory<>));

// 使用
public class ShellViewModel : ViewModelBase
{
    private readonly IViewModelFactory<ProviderDetailViewModel> _detailFactory;

    public ShellViewModel(IViewModelFactory<ProviderDetailViewModel> detailFactory)
    {
        _detailFactory = detailFactory;
    }

    public void OpenDetail()
    {
        var detail = _detailFactory.Create();
    }
}
```

### 38.10.3 工厂 + 参数注入

有时创建对象时需要传入运行时参数（这些参数在注册时未知）：

```csharp
// 自定义工厂，接受运行时参数
public interface IProviderDetailViewModelFactory
{
    ProviderDetailViewModel Create(string providerId);
}

public class ProviderDetailViewModelFactory : IProviderDetailViewModelFactory
{
    private readonly ProviderAuthService _authService;
    private readonly ConfigurationStore _store;

    public ProviderDetailViewModelFactory(
        ProviderAuthService authService,
        ConfigurationStore store)
    {
        _authService = authService;
        _store = store;
    }

    public ProviderDetailViewModel Create(string providerId)
    {
        // 运行时参数 providerId 在这里传入
        return new ProviderDetailViewModel(_authService, _store, providerId);
    }
}

// 注册
services.AddSingleton<IProviderDetailViewModelFactory, ProviderDetailViewModelFactory>();

// 使用
public class MainWindowViewModel : ViewModelBase
{
    private readonly IProviderDetailViewModelFactory _detailFactory;

    public MainWindowViewModel(IProviderDetailViewModelFactory detailFactory)
    {
        _detailFactory = detailFactory;
    }

    public void ShowProviderDetail(string providerId)
    {
        var detail = _detailFactory.Create(providerId);
        // ...
    }
}
```

### 38.10.4 异步工厂

当对象创建涉及异步操作时：

```csharp
public interface IAsyncViewModelFactory<T> where T : ViewModelBase
{
    Task<T> CreateAsync(CancellationToken cancellationToken = default);
}

public class ProxyViewModelFactory : IAsyncViewModelFactory<ProxyHostViewModel>
{
    private readonly ProxyHostService _proxyService;

    public ProxyViewModelFactory(ProxyHostService proxyService)
    {
        _proxyService = proxyService;
    }

    public async Task<ProxyHostViewModel> CreateAsync(CancellationToken ct = default)
    {
        // 异步初始化——比如检查代理状态
        var state = await _proxyService.GetStateAsync(ct);
        return new ProxyHostViewModel(_proxyService, state);
    }
}
```

---

## 38.11 生命周期管理

### 38.11.1 IDisposable 服务

如果服务实现了 `IDisposable`，DI 容器会在容器释放时自动调用 `Dispose()`：

```csharp
public class DatabaseConnection : IDisposable
{
    private SqlConnection? _connection;

    public DatabaseConnection(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }
}

// 注册——容器会跟踪并自动释放
services.AddSingleton<DatabaseConnection>();
```

### 38.11.2 IAsyncDisposable 服务

对于需要异步清理的服务，实现 `IAsyncDisposable`：

```csharp
public sealed class ProxyHostService : IAsyncDisposable
{
    private WebApplication? _app;

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }
    }
}
```

### 38.11.3 容器释放时的行为

```csharp
var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ServiceA>();  // IDisposable
        services.AddSingleton<ServiceB>();  // IDisposable
    })
    .Build();

// 使用服务 ...

// 释放容器——所有 IDisposable 服务被释放
// 释放顺序与注册顺序相反（后注册的先释放）
host.Dispose();
```

### 38.11.4 CodexSwitch 的生命周期管理

CodexSwitch 的 `MainWindowViewModel` 实现了 `IAsyncDisposable`，在应用关闭时清理资源：

```csharp
// App.axaml.cs 中的关闭处理
desktop.ShutdownRequested += async (_, _) =>
{
    _trayMenuController?.Dispose();
    _trayMenuController = null;
    CloseMainWindow();

    // 释放 ViewModel（级联释放其依赖的资源）
    if (_viewModel is not null)
        await _viewModel.DisposeAsync();
    _viewModel = null;
};
```

**要点**：
- 容器管理 Singleton 服务的生命周期
- ViewModel 的生命周期由 App 手动管理
- `IAsyncDisposable` 优于 `IDisposable`（支持异步清理）
- 释放顺序：UI 组件 -> ViewModel -> 服务 -> 容器

### 38.11.5 内存泄漏预防

```csharp
// 1. 事件订阅必须在释放时取消
public class SomeViewModel : ViewModelBase, IDisposable
{
    private readonly I18nService _i18n;

    public SomeViewModel(I18nService i18n)
    {
        _i18n = i18n;
        _i18n.LanguageChanged += OnLanguageChanged;  // 订阅
    }

    public void Dispose()
    {
        _i18n.LanguageChanged -= OnLanguageChanged;  // 取消订阅
    }

    private void OnLanguageChanged(object? sender, EventArgs e) { }
}

// 2. 定时器必须在释放时停止
public class UsageViewModel : ViewModelBase, IDisposable
{
    private readonly DispatcherTimer _timer;

    public UsageViewModel()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }
}

// 3. CancellationTokenSource 必须在释放时取消和释放
public class SearchViewModel : ViewModelBase, IDisposable
{
    private CancellationTokenSource? _searchCts;

    [RelayCommand]
    private async Task SearchAsync(string query)
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();

        var results = await SearchCoreAsync(query, _searchCts.Token);
        Results = results;
    }

    public void Dispose()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
    }
}
```

---

## 38.12 高级：动态服务解析

### 38.12.1 运行时解析服务

```csharp
public class PluginManager
{
    private readonly IServiceProvider _serviceProvider;

    public PluginManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IPlugin LoadPlugin(string pluginName)
    {
        // 根据运行时信息动态解析
        return pluginName switch
        {
            "csv" => _serviceProvider.GetRequiredService<CsvExportPlugin>(),
            "pdf" => _serviceProvider.GetRequiredService<PdfExportPlugin>(),
            _ => throw new PluginNotFoundException(pluginName)
        };
    }
}
```

### 38.12.2 命名服务解析（Keyed Services）

.NET 8+ 的 Keyed Services 提供了原生的命名解析能力：

```csharp
// 注册
services.AddKeyedSingleton<ILogWriter, FileLogWriter>("file");
services.AddKeyedSingleton<ILogWriter, ConsoleLogWriter>("console");
services.AddKeyedSingleton<ILogWriter, DatabaseLogWriter>("database");

// 解析——通过 IServiceProvider
public class LogManager
{
    private readonly IServiceProvider _serviceProvider;

    public LogManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ILogWriter GetWriter(string destination)
    {
        return _serviceProvider.GetRequiredKeyedService<ILogWriter>(destination);
    }
}
```

### 38.12.3 条件解析

```csharp
public class ProxyHostService
{
    private readonly IProviderProtocolAdapter[] _adapters;

    // 注入所有适配器，在运行时根据协议选择
    public ProxyHostService(IEnumerable<IProviderProtocolAdapter> adapters)
    {
        _adapters = adapters.ToArray();
    }

    public IProviderProtocolAdapter GetAdapter(ProviderProtocol protocol)
    {
        return _adapters.FirstOrDefault(a => a.Protocol == protocol)
            ?? throw new NotSupportedException($"不支持的协议: {protocol}");
    }
}
```

### 38.12.4 服务装饰器链

```csharp
// 装饰器可以链式叠加
services.AddSingleton<IUsageLogWriter, UsageLogWriter>();
services.Decorate<IUsageLogWriter, UsageLogWriterWithMetrics>();
services.Decorate<IUsageLogWriter, UsageLogWriterWithRetry>();

// 最终调用链：WithRetry -> WithMetrics -> UsageLogWriter
var writer = serviceProvider.GetRequiredService<IUsageLogWriter>();
writer.Write(entry);
// WithRetry.Write  ->  WithMetrics.Write  ->  UsageLogWriter.Write
```

---

## 38.13 高级：Avalonia 特定的 DI 技巧

### 38.13.1 页面导航 + DI

实现一个支持 DI 的导航服务：

```csharp
// 导航服务接口
public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateTo<TViewModel>(Action<TViewModel> init) where TViewModel : ViewModelBase;
}

// 导航服务实现
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Action<UserControl> _setContent;

    public NavigationService(
        IServiceProvider serviceProvider,
        Action<UserControl> setContent)
    {
        _serviceProvider = serviceProvider;
        _setContent = setContent;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        var page = ResolvePageForViewModel<TViewModel>();
        page.DataContext = viewModel;
        _setContent(page);
    }

    public void NavigateTo<TViewModel>(Action<TViewModel> init) where TViewModel : ViewModelBase
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        init(viewModel);
        var page = ResolvePageForViewModel<TViewModel>();
        page.DataContext = viewModel;
        _setContent(page);
    }

    private static UserControl ResolvePageForViewModel<TViewModel>()
    {
        return typeof(TViewModel).Name switch
        {
            nameof(ProvidersPageViewModel) => new ProvidersPage(),
            nameof(SettingsPageViewModel) => new SettingsPage(),
            nameof(HomePageViewModel) => new HomePage(),
            _ => throw new ArgumentException($"未知 ViewModel: {typeof(TViewModel).Name}")
        };
    }
}

// 注册
services.AddSingleton<INavigationService>(sp =>
{
    // setContent 回调由 MainWindowViewModel 提供
    return new NavigationService(sp, page =>
    {
        sp.GetRequiredService<MainWindowViewModel>().CurrentPage = page;
    });
});
```

### 38.13.2 对话框 + DI

```csharp
// 对话框服务接口
public interface IDialogService
{
    Task<TResult?> ShowDialogAsync<TDialog, TResult>(Action<TDialog>? configure = null)
        where TDialog : Window, new();
}

// 对话框服务实现
public class DialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;

    public DialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult?> ShowDialogAsync<TDialog, TResult>(Action<TDialog>? configure = null)
        where TDialog : Window, new()
    {
        var dialog = _serviceProvider.GetService<TDialog>() ?? new TDialog();
        configure?.dialog);

        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is { } mainWindow)
        {
            return await dialog.ShowDialog<TResult?>(mainWindow);
        }

        return default;
    }
}

// 使用
public class MainWindowViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;

    public MainWindowViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    [RelayCommand]
    private async Task AddProviderAsync()
    {
        var result = await _dialogService.ShowDialogAsync<AddProviderDialog, Provider>(
            dialog => dialog.Title = "添加新的 Provider");

        if (result is not null)
        {
            // 处理结果
        }
    }
}
```

### 38.13.3 子窗口 + DI

```csharp
// 子窗口注册
services.AddTransient<MiniStatusWindow>(sp =>
{
    var viewModel = sp.GetRequiredService<MainWindowViewModel>();
    return new MiniStatusWindow
    {
        DataContext = viewModel
    };
});

// 创建子窗口
public class TrayMenuController : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private MiniStatusWindow? _miniWindow;

    public TrayMenuController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void ShowMiniStatus()
    {
        _miniWindow ??= _serviceProvider.GetRequiredService<MiniStatusWindow>();
        _miniWindow.Show();
    }

    public void Dispose()
    {
        _miniWindow?.Close();
        _miniWindow = null;
    }
}
```

### 38.13.4 自定义控件注入

自定义控件通常由 XAML 实例化，无法直接使用构造函数注入。但可以使用属性注入或服务定位器：

```csharp
// 方式 1：通过 Application.Current 获取服务
public class CsProviderContextMenu : ContextMenu
{
    public void OpenFor(Control target, MainWindowViewModel viewModel, ProviderListItem item)
    {
        // 直接从 Application 获取服务
        if (Application.Current is App app)
        {
            var authService = app.Services.GetRequiredService<ProviderAuthService>();
            // 使用 authService ...
        }
    }
}

// 方式 2：通过附加属性传递
public static class ServiceProviderExtensions
{
    public static readonly AttachedProperty<IServiceProvider?> ServicesProperty =
        AvaloniaProperty.RegisterAttached<Control, IServiceProvider?>(
            "Services", typeof(ServiceProviderExtensions));

    public static void SetServices(Control element, IServiceProvider? value)
        => element.SetValue(ServicesProperty, value);

    public static IServiceProvider? GetServices(Control element)
        => element.GetValue(ServicesProperty);
}
```

### 38.13.5 ContentControl 动态内容 + DI

利用 `ContentControl` 和 `DataTemplate` 实现基于类型的动态页面解析：

```xml
<!-- MainWindow.axaml -->
<ContentControl Content="{Binding CurrentPageViewModel}">
    <ContentControl.DataTemplates>
        <DataTemplate x:DataType="vm:ProvidersPageViewModel">
            <pages:ProvidersPage />
        </DataTemplate>
        <DataTemplate x:DataType="vm:SettingsPageViewModel">
            <pages:SettingsPage />
        </DataTemplate>
        <DataTemplate x:DataType="vm:HomePageViewModel">
            <pages:HomePage />
        </DataTemplate>
    </ContentControl.DataTemplates>
</ContentControl>
```

```csharp
// ViewModel 中切换页面——只需设置 ViewModel，View 自动匹配
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ViewModelBase? _currentPageViewModel;

    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    private void NavigateTo(string page)
    {
        CurrentPageViewModel = page switch
        {
            "Providers" => _serviceProvider.GetRequiredService<ProvidersPageViewModel>(),
            "Settings" => _serviceProvider.GetRequiredService<SettingsPageViewModel>(),
            "Home" => _serviceProvider.GetRequiredService<HomePageViewModel>(),
            _ => throw new ArgumentException($"未知页面: {page}")
        };
    }
}
```

---

## 38.14 测试中的 DI

### 38.14.1 Mock 服务

```csharp
// 使用 Moq 创建 mock
[Fact]
public async Task ToggleProxy_StartsProxy_WhenStopped()
{
    // 创建 mock 服务
    var mockProxy = new Mock<ProxyHostService>();
    mockProxy.Setup(p => p.State.IsRunning).Returns(false);
    mockProxy.Setup(p => p.StartAsync(It.IsAny<AppConfig>(), It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    var mockStore = new Mock<ConfigurationStore>();
    mockStore.Setup(s => s.LoadConfig()).Returns(new AppConfig());

    // 注入 mock 服务
    var viewModel = new MainWindowViewModel(
        paths: new AppPaths(),
        store: mockStore.Object,
        proxyHost: mockProxy.Object,
        i18n: Mock.Of<I18nService>(),
        themeService: Mock.Of<AppThemeService>(),
        serviceProvider: Mock.Of<IServiceProvider>());

    // 执行命令
    await viewModel.ToggleProxyCommand.ExecuteAsync(null);

    // 验证
    mockProxy.Verify(p => p.StartAsync(It.IsAny<AppConfig>(), It.IsAny<CancellationToken>()), Times.Once);
}
```

### 38.14.2 测试专用服务注册

```csharp
public class TestServiceProvider
{
    public static IServiceProvider Create(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();

        // 注册测试默认服务
        services.AddSingleton<AppPaths>();
        services.AddSingleton<ConfigurationStore>();
        services.AddSingleton<I18nService>(I18nService.LoadDefault());

        // 替换真实服务为测试实现
        services.AddSingleton<ProxyHostService>(sp =>
        {
            var mock = new Mock<ProxyHostService>();
            // 配置 mock 行为
            return mock.Object;
        });

        // 允许测试代码自定义注册
        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }
}

// 测试中使用
[Fact]
public void TestSomething()
{
    var provider = TestServiceProvider.Create(services =>
    {
        // 测试特定的自定义配置
        services.AddSingleton<IAppThemeService>(new MockThemeService());
    });

    var viewModel = provider.GetRequiredService<MainWindowViewModel>();
    // ...
}
```

### 38.14.3 集成测试中的 DI

```csharp
public class IntegrationTestBase : IAsyncLifetime
{
    protected IHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // 使用真实的基础设施
                services.AddSingleton<AppPaths>();
                services.AddSingleton<ConfigurationStore>();

                // 但替换外部依赖
                services.AddSingleton<ProxyHostService>(sp =>
                {
                    // 返回一个不实际启动代理的 mock
                    return new Mock<ProxyHostService>().Object;
                });
            })
            .Build();

        await Host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Host.StopAsync();
        Host.Dispose();
    }
}

public class MainWindowViewModelTests : IntegrationTestBase
{
    [Fact]
    public async Task LoadConfig_AppliesCorrectTheme()
    {
        var viewModel = Host.Services.GetRequiredService<MainWindowViewModel>();
        // 集成测试 ...
    }
}
```

---

## 38.15 实战：CodexSwitch 的 DI 架构分析

### 38.15.1 现状：手动 DI

CodexSwitch 当前采用手动依赖注入模式——`MainWindowViewModel` 在无参构造函数中逐一创建所有依赖：

```csharp
// CodexSwitch 当前的 MainWindowViewModel 构造函数（简化版）
public MainWindowViewModel()
{
    _paths = new AppPaths();                              // 手动创建
    _store = new ConfigurationStore(_paths);               // 手动创建，依赖 _paths
    _config = _store.LoadConfig();                         // 手动加载配置
    _i18n = I18nService.Current;                           // 使用静态单例
    _pricing = _store.LoadPricing();                       // 手动加载定价
    _priceCalculator = new PriceCalculator(_pricing);      // 手动创建
    _usageMeter = new UsageMeter(_priceCalculator);        // 手动创建，依赖 _priceCalculator
    _usageLogWriter = new UsageLogWriter(_paths);          // 手动创建
    _usageLogReader = new UsageLogReader(_paths);          // 手动创建
    _codexConfigWriter = new CodexConfigWriter(_paths);    // 手动创建
    _claudeCodeConfigWriter = new ClaudeCodeConfigWriter(_paths); // 手动创建
    // ... 更多
}
```

### 38.15.2 服务依赖图

```
MainWindowViewModel
├── AppPaths
├── ConfigurationStore ← AppPaths
├── PriceCalculator ← ModelPricingCatalog
├── UsageMeter ← PriceCalculator
├── UsageLogWriter ← AppPaths
├── UsageLogReader ← AppPaths
├── CodexConfigWriter ← AppPaths
├── ClaudeCodeConfigWriter ← AppPaths
├── CodexSessionMigrationService ← AppPaths
├── I18nService (静态单例)
├── StartupRegistrationService
├── ProxyHostService
│   ├── UsageMeter
│   ├── PriceCalculator
│   ├── UsageLogWriter
│   ├── CodexConfigWriter
│   ├── ClaudeCodeConfigWriter
│   ├── ProviderAuthService
│   └── IEnumerable<IProviderProtocolAdapter>
│       ├── OpenAiResponsesAdapter
│       ├── OpenAiChatAdapter
│       └── AnthropicMessagesAdapter
└── HttpClient
```

### 38.15.3 推荐的注册策略

如果 CodexSwitch 要迁移到构造函数注入，推荐以下注册策略：

```csharp
private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
{
    // ===== 基础设施层（Singleton）=====
    services.AddSingleton<AppPaths>();
    services.AddSingleton<ConfigurationStore>();

    // 配置作为实例注册——启动时加载一次
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

    // ===== 业务服务层（Singleton）=====
    services.AddSingleton<PriceCalculator>();
    services.AddSingleton<UsageMeter>();
    services.AddSingleton<UsageLogWriter>();
    services.AddSingleton<UsageLogReader>();
    services.AddSingleton<CodexConfigWriter>();
    services.AddSingleton<ClaudeCodeConfigWriter>();
    services.AddSingleton<CodexSessionMigrationService>();
    services.AddSingleton<StartupRegistrationService>();
    services.AddSingleton<ProviderAuthService>();

    // I18nService——保留静态 Current 模式或改为 Singleton
    services.AddSingleton<I18nService>(I18nService.Current);

    // ===== 协议适配器（Singleton，多实现）=====
    services.AddSingleton<IProviderProtocolAdapter, OpenAiResponsesAdapter>();
    services.AddSingleton<IProviderProtocolAdapter, OpenAiChatAdapter>();
    services.AddSingleton<IProviderProtocolAdapter, AnthropicMessagesAdapter>();

    // ===== 代理服务（Singleton）=====
    services.AddSingleton<ProxyHostService>();

    // ===== HTTP 客户端 =====
    services.AddHttpClient();

    // ===== ViewModel 层（Transient）=====
    services.AddTransient<MainWindowViewModel>();

    // ===== 页面层（Transient）=====
    services.AddTransient<ProvidersPage>();
    services.AddTransient<SettingsPage>();
    services.AddTransient<HomePage>();
    services.AddTransient<ModelsPage>();
    services.AddTransient<UsagePage>();
}
```

### 38.15.4 迁移步骤

从手动 DI 迁移到构造函数注入的推荐步骤：

```
步骤 1：添加 NuGet 包
    dotnet add package Microsoft.Extensions.Hosting

步骤 2：在 App.axaml.cs 中创建 Host
    - 添加 _host 字段
    - 在 OnFrameworkInitializationCompleted 中 Build
    - 在 OnExit 中 Dispose

步骤 3：注册基础设施服务
    - AppPaths, ConfigurationStore
    - 验证容器能正确创建这些服务

步骤 4：注册业务服务
    - PriceCalculator, UsageMeter 等
    - 使用工厂注册（处理依赖配置文件的情况）

步骤 5：改造 ViewModel 构造函数
    - 添加构造函数参数
    - 删除手动创建代码
    - 从容器解析 ViewModel

步骤 6：注册页面和窗口
    - 注册各页面
    - 修改导航逻辑使用容器解析

步骤 7：运行测试
    - 确保所有功能正常
    - 检查内存泄漏
    - 验证 AOT 兼容性
```

### 38.15.5 ProxyHostService 已经是 DI 的好例子

值得注意的是，CodexSwitch 的 `ProxyHostService` **已经**使用了构造函数注入，它是项目中 DI 的典范：

```csharp
public sealed class ProxyHostService : IAsyncDisposable
{
    private readonly UsageMeter _usageMeter;
    private readonly PriceCalculator _priceCalculator;
    private readonly UsageLogWriter _usageLogWriter;
    private readonly CodexConfigWriter _codexConfigWriter;
    private readonly ClaudeCodeConfigWriter _claudeCodeConfigWriter;
    private readonly ProviderAuthService _providerAuthService;
    private readonly Dictionary<ProviderProtocol, IProviderProtocolAdapter> _adapters;

    // 构造函数注入——清晰声明所有依赖
    public ProxyHostService(
        UsageMeter usageMeter,
        PriceCalculator priceCalculator,
        UsageLogWriter usageLogWriter,
        CodexConfigWriter codexConfigWriter,
        ClaudeCodeConfigWriter claudeCodeConfigWriter,
        ProviderAuthService providerAuthService,
        IEnumerable<IProviderProtocolAdapter> adapters)  // 多实现注入
    {
        _usageMeter = usageMeter;
        _priceCalculator = priceCalculator;
        _usageLogWriter = usageLogWriter;
        _codexConfigWriter = codexConfigWriter;
        _claudeCodeConfigWriter = claudeCodeConfigWriter;
        _providerAuthService = providerAuthService;
        _adapters = adapters.ToDictionary(adapter => adapter.Protocol);
    }
}
```

---

## Deep Dive

### DI 容器的内部实现：反射 vs 源生成

#### 反射方式（默认）

.NET 默认的 DI 容器使用**反射**来创建服务实例：

```csharp
// 容器内部大致逻辑
public object CreateInstance(Type serviceType)
{
    // 1. 反射获取构造函数
    var constructor = serviceType.GetConstructors()
        .OrderByDescending(c => c.GetParameters().Length)
        .First();

    // 2. 递归解析每个参数
    var parameters = constructor.GetParameters()
        .Select(p => GetService(p.ParameterType))
        .ToArray();

    // 3. 调用构造函数创建实例
    return constructor.Invoke(parameters);
}
```

**性能影响**：反射创建比 `new` 慢 10-100 倍，但对于 Singleton 服务，这个开销只发生一次。

#### 源生成方式（.NET 8+ 高性能）

.NET 8 引入了 DI 源生成器，在编译时生成工厂代码，避免运行时反射：

```csharp
// 启用源生成
var host = Host.CreateDefaultBuilder()
    .UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    })
    .Build();

// 源生成器会自动生成类似以下的代码：
// [GeneratedCode]
// internal static class ServiceProvider_Singleton_1234
// {
//     public static AppPaths CreateAppPaths(IServiceProvider sp)
//         => new AppPaths();
// }
```

#### 性能对比

| 方式 | 首次解析 | 后续解析 | 内存 | AOT 兼容 |
|------|---------|---------|------|---------|
| 反射 | 慢 | 快（缓存） | 中等 | 需要提示 |
| 源生成 | 极快 | 极快 | 低 | 完美 |
| 手动 new | 极快 | 极快 | 最低 | 完美 |

#### .NET 8+ 的 Keyed Services 实现

Keyed Services 在容器内部维护了一个 `Dictionary<object, ServiceDescriptor>` 的映射：

```csharp
// 容器内部
internal sealed class ServiceProviderEngine
{
    private readonly ConcurrentDictionary<object, Func<ServiceProviderEngineScope, object>>
        _keyedServiceFactories;

    public object GetKeyedService(Type serviceType, object serviceKey)
    {
        var key = (serviceType, serviceKey);
        var factory = _keyedServiceFactories.GetOrAdd(key, CreateKeyedFactory);
        return factory(_rootScope);
    }
}
```

---

## Cross References

| 关联章节 | 关联内容 |
|---------|---------|
| [第 2 章：项目结构与启动流程](02-project-structure.md) | Program.cs 启动流程、App 生命周期中的服务创建 |
| [第 6 章：MVVM 模式实战](06-mvvm-pattern.md) | ViewModelBase 基类、ObservableObject、ViewModel 与 DI 的配合 |
| [第 25 章：ASP.NET Core 集成](25-aspnet-integration.md) | ProxyHostService 的 DI 注册、Kestrel 内嵌服务器 |
| [第 36 章：通知与任务栏](36-notifications-taskbar.md) | TrayMenuController 的生命周期管理 |
| [第 37 章：数据可视化](37-data-visualization.md) | ECharts 控件的 DI 注入 |
| [第 39 章：测试](39-testing.md) | 测试中的 Mock 服务和 DI 配置 |

---

## Common Pitfalls

### Pitfall 1：循环依赖

```csharp
// 错误：A 依赖 B，B 又依赖 A
public class ServiceA
{
    public ServiceA(ServiceB b) { } // 需要 B
}

public class ServiceB
{
    public ServiceB(ServiceA a) { } // 又需要 A
}

// 容器抛出 StackOverflowException！
```

**解决**：提取共享逻辑到第三个服务，或使用 `Lazy<T>` 打破循环：

```csharp
public class ServiceB
{
    private readonly Lazy<ServiceA> _lazyA;

    public ServiceB(Lazy<ServiceA> lazyA)
    {
        _lazyA = lazyA; // 延迟解析，避免循环
    }
}
```

### Pitfall 2：Transient 服务注入 Singleton

```csharp
// 错误：Transient 的 ViewModel 被 Singleton 持有
services.AddSingleton<MainWindowViewModel>(); // 不要这样做！
services.AddTransient<SomeTransientService>();

// ViewModel 永远不会被释放，其依赖的 Transient 服务也不会
```

**解决**：ViewModel 使用 Transient 生命周期，由 App 手动管理释放。

### Pitfall 3：忘记注册服务

```csharp
// 注册了接口但忘了注册实现
services.AddSingleton<IAppThemeService>();  // 缺少实现类型！

// 解析时抛出 InvalidOperationException
var service = provider.GetRequiredService<IAppThemeService>();
```

**解决**：使用 `ValidateOnBuild` 在构建时检查：

```csharp
var host = Host.CreateDefaultBuilder()
    .UseDefaultServiceProvider(options =>
    {
        options.ValidateOnBuild = true;  // 构建时验证所有服务
        options.ValidateScopes = true;    // 验证作用域正确性
    })
    .Build();
```

### Pitfall 4：在构造函数中做太多事

```csharp
// 错误：构造函数中启动了代理、加载了数据、初始化了 UI
public MainWindowViewModel(/* ... */)
{
    _proxyHost.StartAsync(_config).Wait();  // 不要在构造函数中阻塞！
    LoadDataAsync().Wait();                  // 不要这样做！
}
```

**解决**：构造函数只做字段赋值，异步操作放到显式的初始化方法中：

```csharp
public MainWindowViewModel(/* ... */)
{
    // 构造函数只赋值
    _proxyHost = proxyHost;
    _config = config;
}

// 异步初始化由调用方显式调用
public async Task InitializeAsync()
{
    await _proxyHost.StartAsync(_config);
    await LoadDataAsync();
}
```

### Pitfall 5：在 XAML 中使用注入构造函数

```xml
<!-- 错误：XAML 解析器无法调用带参数的构造函数 -->
<Pages:ProvidersPage />  <!-- 找不到无参构造函数，运行时崩溃 -->
```

**解决**：页面保持无参构造函数，DataContext 在代码中设置。

### Pitfall 6：Singleton 注入 Scoped 服务

```csharp
// 错误：Singleton 捕获了 Scoped 服务
services.AddScoped<UserSession>();
services.AddSingleton<SomeManager>();

public SomeManager(UserSession session) // Singleton 永远持有第一个 Scoped 实例
{
    _session = session; // 这个 session 永远不会更新！
}
```

**解决**：注入 `IServiceScopeFactory` 在需要时创建新的作用域：

```csharp
public class SomeManager
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SomeManager(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void DoWork()
    {
        using var scope = _scopeFactory.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<UserSession>();
        // 使用新的 session
    }
}
```

### Pitfall 7：释放容器后继续使用服务

```csharp
var host = Host.CreateDefaultBuilder().Build();
var service = host.Services.GetRequiredService<SomeService>();

host.Dispose();          // 容器已释放
service.DoSomething();   // 可能抛出 ObjectDisposedException
```

**解决**：确保在容器生命周期内使用服务，或在 App.OnExit 中最后释放容器。

### Pitfall 8：测试时没有 mock 所有依赖

```csharp
// 错误：只 mock 了部分依赖，其余的试图解析真实服务
var viewModel = new MainWindowViewModel(
    mockPaths.Object,
    mockStore.Object,
    // 其余参数传 null——运行时 NullReferenceException！
    null, null, null, null, null, null);
```

**解决**：使用完整的测试 DI 容器或 mock 所有依赖。

### Pitfall 9：注册顺序导致覆盖

```csharp
// 后注册的会覆盖先注册的
services.AddSingleton<IEmailSender, SmtpSender>();
services.AddSingleton<IEmailSender, SendGridSender>(); // SMTP 被覆盖了

// 如果想保留两者，使用 Keyed Services 或 IEnumerable 注入
```

### Pitfall 10：构造函数参数过多（代码异味）

```csharp
// 代码异味：构造函数有 10+ 个参数
public MainWindowViewModel(
    AppPaths paths,
    ConfigurationStore store,
    PriceCalculator priceCalculator,
    UsageMeter usageMeter,
    UsageLogWriter usageLogWriter,
    UsageLogReader usageLogReader,
    CodexConfigWriter codexConfigWriter,
    ClaudeCodeConfigWriter claudeCodeConfigWriter,
    CodexSessionMigrationService migrationService,
    I18nService i18n,
    StartupRegistrationService startupService,
    ProxyHostService proxyHost,
    IServiceProvider serviceProvider)
```

**解决**：将相关依赖组合成"聚合服务"：

```csharp
// 将路径和配置相关服务组合
public class AppInfrastructure
{
    public AppPaths Paths { get; }
    public ConfigurationStore Store { get; }
    public AppConfig Config { get; }
    public ModelPricingCatalog Pricing { get; }

    public AppInfrastructure(AppPaths paths, ConfigurationStore store)
    {
        Paths = paths;
        Store = store;
        Config = store.LoadConfig();
        Pricing = store.LoadPricing();
    }
}

// 构造函数更简洁
public MainWindowViewModel(
    AppInfrastructure infra,
    UsageServices usageServices,
    ProxyHostService proxyHost)
{
    // ...
}
```

### Pitfall 11：AOT 发布时 DI 注册失败

```csharp
// AOT 模式下，泛型服务注册可能失败
services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));

// AOT 需要编译时知道所有泛型特化
```

**解决**：为 AOT 显式注册所有需要的泛型特化：

```csharp
// 显式注册而非开放泛型
services.AddSingleton<IRepository<Provider>, Repository<Provider>>();
services.AddSingleton<IRepository<Model>, Repository<Model>>();
```

### Pitfall 12：忘记 Dispose IHost

```csharp
// 错误：Host 没有被释放，所有 Singleton 服务都不会释放
public static void Main(string[] args)
{
    var host = Host.CreateDefaultBuilder().Build();
    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    // host 从未释放——数据库连接、HTTP 客户端等全部泄漏
}
```

**解决**：始终在应用退出时释放 Host：

```csharp
public override void OnExit(ExitEventArgs e)
{
    _host?.Dispose();
    base.OnExit(e);
}
```

---

## Try It Yourself

### 练习 1：基础 DI 容器搭建

创建一个最小的 DI 容器，注册 `AppPaths` 和 `ConfigurationStore`，验证构造函数注入正常工作：

```csharp
// 你的任务：完成以下代码
var services = new ServiceCollection();
// TODO：注册服务
var provider = services.BuildServiceProvider();
// TODO：解析 ConfigurationStore 并加载配置
```

### 练习 2：改造 ViewModel 为构造函数注入

将 `MainWindowViewModel` 的前 3 个依赖（`AppPaths`、`ConfigurationStore`、`PriceCalculator`）改为构造函数注入：

```csharp
// 改造前
public MainWindowViewModel()
{
    _paths = new AppPaths();
    _store = new ConfigurationStore(_paths);
    _pricing = _store.LoadPricing();
    _priceCalculator = new PriceCalculator(_pricing);
}

// 你的任务：改为构造函数注入
```

### 练习 3：注册多实现适配器

注册 3 个协议适配器（`OpenAiResponsesAdapter`、`OpenAiChatAdapter`、`AnthropicMessagesAdapter`），并验证通过 `IEnumerable<IProviderProtocolAdapter>` 能全部解析：

```csharp
// 你的任务：
// 1. 注册 3 个适配器
// 2. 解析 IEnumerable<IProviderProtocolAdapter>
// 3. 验证数量和类型正确
```

### 练习 4：实现页面工厂

创建一个 `IPageFactory` 接口，根据页面名称从 DI 容器动态解析页面：

```csharp
// 你的任务：
public interface IPageFactory
{
    UserControl CreatePage(string pageName);
}
```

### 练习 5：Func<T> 工厂注入

为 `ProviderDetailViewModel` 创建 `Func<ProviderDetailViewModel>` 工厂，并在 `MainWindowViewModel` 中使用：

```csharp
// 你的任务：注册工厂并在 ViewModel 中使用
```

### 练习 6：测试 ViewModel

编写单元测试，使用 Mock 服务测试 `MainWindowViewModel` 的 `ToggleProxyAsync` 命令：

```csharp
// 你的任务：
[Fact]
public async Task ToggleProxy_ShouldStartProxy_WhenStopped()
{
    // 1. 创建 mock 服务
    // 2. 注入到 ViewModel
    // 3. 执行命令
    // 4. 验证行为
}
```

### 练习 7：装饰器模式

为 `UsageLogWriter` 创建一个装饰器，在写入日志前后记录时间戳：

```csharp
// 你的任务：
public class TimedUsageLogWriter : IUsageLogWriter
{
    // 实现装饰器逻辑
}
```

### 练习 8：Keyed Services 实现

使用 .NET 8+ 的 Keyed Services 注册多个日志输出目标，并根据配置动态选择：

```csharp
// 你的任务：
// 注册 "file"、"console"、"database" 三种日志写入器
// 根据配置选择使用哪一个
```

### 练习 9：Scoped 生命周期陷阱

演示在 Avalonia 中错误使用 Scoped 生命周期的问题，并提供修复方案：

```csharp
// 你的任务：
// 1. 创建一个 Scoped 服务
// 2. 注入到 Singleton 中
// 3. 展示问题
// 4. 使用 IServiceScopeFactory 修复
```

### 练习 10：完整迁移

将 CodexSwitch 的 `App.axaml.cs` 改造为使用 `IHost`，创建 Host 并从容器解析 `MainWindowViewModel`，保持所有现有功能不变：

```csharp
// 你的任务：改造 App.axaml.cs
// 1. 添加 _host 字段
// 2. 在 OnFrameworkInitializationCompleted 中创建 Host
// 3. 注册所有服务
// 4. 从容器解析 ViewModel
// 5. 在 OnExit 中释放 Host
```
