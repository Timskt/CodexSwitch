# 36. 架构设计与性能优化

> **写给零基础的你**：架构设计就像"盖房子的图纸"——在动工之前，你得想清楚房间怎么布局、电线怎么走、水管怎么排。性能优化就像"让房子住得更舒适"——门要轻、窗要亮、空调要安静。一个没有图纸的房子迟早会塌，一个不注重舒适的房子住着难受。写代码也是一样的道理。

## 36.1 概述

### 36.1.1 为什么需要架构设计

当你的 Avalonia 应用只有 5 个文件、200 行代码时，架构设计似乎毫无意义——所有逻辑放在一个文件里也跑得起来。但当项目增长到 50 个文件、5000 行代码时，问题就开始暴露：

| 没有架构设计的后果 | 有架构设计的好处 |
|---|---|
| 改一个按钮，牵连三个页面 | 改一个按钮，只动一个文件 |
| 找一个功能，翻遍所有文件 | 按层级快速定位 |
| 新增功能，复制粘贴旧代码 | 继承基类，几行搞定 |
| 测试无从下手 | ViewModel 可以独立单元测试 |
| 多人协作互相冲突 | 各层独立开发，并行推进 |

CodexSwitch 就是一个典型的中大型 Avalonia 项目：代理服务、OAuth 登录、使用量统计、配置迁移、多语言、主题系统——如果一开始没有分层架构，维护起来将是噩梦。

### 36.1.2 架构设计的目标

架构设计追求三个核心目标：

```
可维护性（Maintainability）
├── 代码结构清晰，新人能快速上手
├── 修改影响范围可控
└── 文档和注释充分

可扩展性（Extensibility）
├── 新增功能不破坏已有功能
├── 支持插件和模块化
└── 接口设计支持多种实现

可测试性（Testability）
├── ViewModel 可以脱离 UI 测试
├── Service 可以用 Mock 替代依赖
└── 集成测试覆盖关键路径
```

### 36.1.3 性能优化的重要性

性能优化不是"锦上添花"，而是"雪中送炭"：

- **启动慢**：用户等 3 秒以上就会关掉你的应用
- **内存泄漏**：长时间运行的应用会越来越卡，最终崩溃
- **渲染卡顿**：列表滚动掉帧，用户体验极差
- **绑定慢**：反射绑定比编译绑定慢 10-50 倍

Avalonia 的渲染管线基于 Skia，性能本身不错，但不当的使用方式会严重拖累表现。

## 36.2 项目分层架构

### 36.2.1 三层架构（View -> ViewModel -> Model）

Avalonia 推荐的 MVVM 三层架构：

```
┌─────────────────────────────────────────────────────┐
│                   View 层                            │
│  ┌─────────────┐ ┌──────────────┐ ┌──────────────┐  │
│  │ MainWindow  │ │ ProvidersPage│ │ SettingsPage │  │
│  │  .axaml     │ │  .axaml      │ │  .axaml      │  │
│  └──────┬──────┘ └──────┬───────┘ └──────┬───────┘  │
│         │DataContext      │               │          │
├─────────┼────────────────┼───────────────┼──────────┤
│         ▼                ▼               ▼          │
│                   ViewModel 层                       │
│  ┌──────────────────────────────────────────────┐   │
│  │         MainWindowViewModel                   │   │
│  │  ┌──────────┐ ┌───────────┐ ┌─────────────┐ │   │
│  │  │ 属性     │ │ 命令      │ │ 状态管理     │ │   │
│  │  │ 绑定     │ │ 逻辑      │ │ 导航        │ │   │
│  │  └──────────┘ └───────────┘ └─────────────┘ │   │
│  └──────────────────┬───────────────────────────┘   │
│                     │                               │
├─────────────────────┼───────────────────────────────┤
│                     ▼                               │
│                   Service / Model 层                 │
│  ┌──────────────┐ ┌──────────────┐ ┌─────────────┐ │
│  │ConfigurationStore│PriceCalculator│AppThemeService│ │
│  │ AppPaths     │ │ ProxyHost    │ │ I18nService │ │
│  └──────────────┘ └──────────────┘ └─────────────┘ │
└─────────────────────────────────────────────────────┘
```

### 36.2.2 各层的职责和边界

| 层 | 职责 | 不该做的事 | 示例 |
|---|------|-----------|------|
| **View** | 布局、样式、动画、用户交互 | 业务逻辑、数据处理 | `MainWindow.axaml` |
| **ViewModel** | 状态管理、命令处理、页面导航 | 直接操作 UI 控件、文件 I/O | `MainWindowViewModel.cs` |
| **Service** | 业务逻辑、外部交互、数据持久化 | 持有 UI 引用 | `ConfigurationStore.cs` |
| **Model** | 纯数据结构、枚举、常量 | 任何逻辑 | `AppConfig.cs` |

### 36.2.3 依赖方向规则

**核心原则：依赖只能向下，不能向上。**

```csharp
// 正确：ViewModel 引用 Service
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ConfigurationStore _store;  // Service 层

    public MainWindowViewModel()
    {
        _paths = new AppPaths();
        _store = new ConfigurationStore(_paths);
        _config = _store.LoadConfig();
    }
}

// 错误：Service 引用 ViewModel
public class ConfigurationStore
{
    private MainWindowViewModel _vm;  // 禁止！向上依赖
}
```

```
依赖方向图：

    View ──────▶ ViewModel ──────▶ Service ──────▶ Model
      │              │                │
      │              │                │
      ▼              ▼                ▼
   ViewModelBase  AppConfig      AppPaths
   (基类)         (数据类)       (路径配置)
```

### 36.2.4 CodexSwitch 的分层实践

CodexSwitch 的实际分层结构：

```
CodexSwitch/
├── Views/                    # View 层
│   ├── MainWindow.axaml
│   ├── ProvidersPage.axaml
│   ├── SettingsPage.axaml
│   └── UsagePage.axaml
├── ViewModels/               # ViewModel 层
│   ├── ViewModelBase.cs      # 所有 ViewModel 的基类
│   └── MainWindowViewModel.cs
├── Services/                 # Service 层
│   ├── AppPaths.cs           # 路径管理
│   ├── AppThemeService.cs    # 主题切换
│   ├── ConfigurationStore.cs # 配置持久化
│   ├── PriceCalculator.cs    # 价格计算
│   ├── I18nService.cs        # 国际化
│   └── ProxyHostService.cs   # 代理服务
├── Models/                   # Model 层
│   ├── AppConfig.cs
│   └── ProviderConfig.cs
├── Proxy/                    # 代理协议层
│   ├── IProviderProtocolAdapter.cs
│   ├── OpenAiResponsesAdapter.cs
│   └── AnthropicMessagesAdapter.cs
└── Controls/                 # 自定义控件
```

**CodexSwitch 的 ViewModelBase 设计：**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace CodexSwitch.ViewModels;

// 极简基类——所有 ViewModel 的根
// 继承 ObservableObject，自动实现 INotifyPropertyChanged
public abstract class ViewModelBase : ObservableObject
{
}
```

这个基类虽然只有两行，但它确立了一个重要的架构决策：使用 CommunityToolkit.Mvvm 而不是 ReactiveUI。CommunityToolkit.Mvvm 通过源代码生成器（Source Generator）在编译时生成属性通知代码，性能优于反射。

## 36.3 依赖注入（DI）深入

### 36.3.1 Microsoft.Extensions.DependencyInjection 详解

依赖注入是大型应用架构的核心支柱。它解决了"谁来创建对象"和"谁来管理对象生命周期"的问题。

**没有 DI 的世界：**

```csharp
// 紧耦合——ViewModel 直接创建 Service
public class MainWindowViewModel
{
    private readonly ConfigurationStore _store;

    public MainWindowViewModel()
    {
        // 直接 new，耦合了具体实现
        var paths = new AppPaths();
        _store = new ConfigurationStore(paths);
    }
}

// 问题：
// 1. 测试时无法替换 _store 为 Mock
// 2. 如果 ConfigurationStore 的构造函数变了，所有使用它的地方都要改
// 3. 无法控制生命周期（每次 new 一个新实例还是复用？）
```

**使用 DI 的世界：**

```csharp
// 松耦合——依赖接口，不依赖实现
public class MainWindowViewModel
{
    private readonly IConfigurationStore _store;

    // 通过构造函数注入，不关心谁创建、怎么创建
    public MainWindowViewModel(IConfigurationStore store)
    {
        _store = store;
    }
}

// 在 Composition Root 注册
var services = new ServiceCollection();
services.AddSingleton<AppPaths>();
services.AddSingleton<IConfigurationStore, ConfigurationStore>();
services.AddTransient<MainWindowViewModel>();

var provider = services.BuildServiceProvider();
var vm = provider.GetRequiredService<MainWindowViewModel>();
```

### 36.3.2 服务生命周期

| 生命周期 | 说明 | 适用场景 | 注意事项 |
|---------|------|---------|---------|
| **Transient** | 每次解析创建新实例 | ViewModel、轻量对象 | 大量创建可能增加 GC 压力 |
| **Singleton** | 整个应用只有一个实例 | 配置、服务、缓存 | 注意线程安全 |
| **Scoped** | 每个作用域一个实例 | Web 请求上下文 | Avalonia 中较少使用 |

```csharp
var services = new ServiceCollection();

// Singleton：整个应用生命周期只有一个实例
services.AddSingleton<AppPaths>();
services.AddSingleton<ConfigurationStore>();

// Transient：每次请求都创建新实例
services.AddTransient<MainWindowViewModel>();

// Scoped：每个作用域一个实例（Avalonia 中不常用）
services.AddScoped<UserSession>();

var provider = services.BuildServiceProvider();

// Singleton 始终返回同一个实例
var paths1 = provider.GetRequiredService<AppPaths>();
var paths2 = provider.GetRequiredService<AppPaths>();
Console.WriteLine(ReferenceEquals(paths1, paths2)); // True

// Transient 每次返回新实例
var vm1 = provider.GetRequiredService<MainWindowViewModel>();
var vm2 = provider.GetRequiredService<MainWindowViewModel>();
Console.WriteLine(ReferenceEquals(vm1, vm2)); // False
```

### 36.3.3 服务注册最佳实践

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodexSwitchServices(this IServiceCollection services)
    {
        // 基础设施
        services.AddSingleton<AppPaths>();
        services.AddSingleton<ConfigurationStore>();

        // 业务服务
        services.AddSingleton<PriceCalculator>(sp =>
        {
            var store = sp.GetRequiredService<ConfigurationStore>();
            var pricing = store.LoadPricing();
            return new PriceCalculator(pricing);
        });

        services.AddSingleton<I18nService>();
        services.AddSingleton<AppThemeService>();

        // 代理服务
        services.AddSingleton<ProxyHostService>();
        services.AddSingleton<ProviderRoutingResolver>();

        // 协议适配器（Keyed Services）
        services.AddKeyedSingleton<IProviderProtocolAdapter, OpenAiResponsesAdapter>(
            ProviderProtocol.OpenAiResponses);
        services.AddKeyedSingleton<IProviderProtocolAdapter, OpenAiChatAdapter>(
            ProviderProtocol.OpenAiChat);
        services.AddKeyedSingleton<IProviderProtocolAdapter, AnthropicMessagesAdapter>(
            ProviderProtocol.AnthropicMessages);

        // ViewModel
        services.AddTransient<MainWindowViewModel>();

        return services;
    }
}
```

### 36.3.4 服务解析和工厂模式

```csharp
// 方式一：构造函数注入（推荐）
public class MainWindowViewModel
{
    private readonly ConfigurationStore _store;
    private readonly PriceCalculator _calculator;

    public MainWindowViewModel(ConfigurationStore store, PriceCalculator calculator)
    {
        _store = store;
        _calculator = calculator;
    }
}

// 方式二：工厂模式——延迟创建或条件创建
public interface IViewModelFactory<T> where T : ViewModelBase
{
    T Create();
}

public class ViewModelFactory<T> : IViewModelFactory<T> where T : ViewModelBase
{
    private readonly IServiceProvider _provider;

    public ViewModelFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public T Create() => ActivatorUtilities.CreateInstance<T>(_provider);
}

// 注册
services.AddSingleton(typeof(IViewModelFactory<>), typeof(ViewModelFactory<>));
```

### 36.3.5 Keyed Services（.NET 8+）

```csharp
// 注册 Keyed Services
services.AddKeyedSingleton<IProviderProtocolAdapter, OpenAiResponsesAdapter>(
    "openai-responses");
services.AddKeyedSingleton<IProviderProtocolAdapter, OpenAiChatAdapter>(
    "openai-chat");
services.AddKeyedSingleton<IProviderProtocolAdapter, AnthropicMessagesAdapter>(
    "anthropic");

// 解析 Keyed Services
public class ProviderRoutingResolver
{
    private readonly IServiceProvider _provider;

    public ProviderRoutingResolver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IProviderProtocolAdapter Resolve(ProviderProtocol protocol)
    {
        return protocol switch
        {
            ProviderProtocol.OpenAiResponses =>
                _provider.GetRequiredKeyedService<IProviderProtocolAdapter>("openai-responses"),
            ProviderProtocol.OpenAiChat =>
                _provider.GetRequiredKeyedService<IProviderProtocolAdapter>("openai-chat"),
            ProviderProtocol.AnthropicMessages =>
                _provider.GetRequiredKeyedService<IProviderProtocolAdapter>("anthropic"),
            _ => throw new NotSupportedException($"Unsupported protocol: {protocol}")
        };
    }
}
```

### 36.3.6 服务作用域和内存泄漏

```csharp
// 危险：Singleton 持有 Transient 的引用，导致 Transient 永远不会被释放
public class BadSingleton
{
    private readonly TransientService _service; // 永远不会被 GC

    public BadSingleton(TransientService service)
    {
        _service = service;
    }
}

// 正确：使用 IServiceScopeFactory 创建作用域
public class GoodSingleton
{
    private readonly IServiceScopeFactory _scopeFactory;

    public GoodSingleton(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void DoWork()
    {
        using var scope = _scopeFactory.CreateScope();
        var transient = scope.ServiceProvider.GetRequiredService<TransientService>();
        // transient 在 scope 释放时被清理
    }
}
```

## 36.4 服务层设计

### 36.4.1 服务接口设计原则

```csharp
// 原则一：接口隔离（ISP）——不要胖接口
// 错误：一个接口做太多事
public interface IBadService
{
    void LoadConfig();
    void SaveConfig();
    void LoadPricing();
    void SavePricing();
    void LoadUsage();
    void SaveUsage();
}

// 正确：拆分为小接口
public interface IConfigStore
{
    AppConfig LoadConfig();
    void SaveConfig(AppConfig config);
}

public interface IPricingStore
{
    ModelPricingCatalog LoadPricing();
    void SavePricing(ModelPricingCatalog catalog);
}

// 原则二：返回值不要暴露内部状态
// 错误：返回内部可变集合
public List<ProviderConfig> GetProviders() => _providers;

// 正确：返回只读副本
public IReadOnlyList<ProviderConfig> GetProviders() => _providers.AsReadOnly();
```

### 36.4.2 服务粒度控制

```csharp
// 粒度太细：每个属性一个服务
public interface IThemeNameService { string GetThemeName(); }
public interface IThemeColorService { Color GetColor(); }
public interface IThemeFontService { string GetFont(); }

// 粒度太粗：一个 God Service 做所有事
public interface IAppService
{
    void SetTheme(string theme);
    void SetLanguage(string lang);
    void SetProxy(ProxySettings settings);
    // ... 几十个方法
}

// 合适的粒度：按领域划分
public static class AppThemeService  // 主题领域
{
    public static void Apply(string? theme) { }
    public static string Normalize(string? theme) { }
}

public class I18nService  // 国际化领域
{
    public string Translate(string key) { }
    public void SetLanguage(string lang) { }
}
```

### 36.4.3 服务间通信

```csharp
// 方式一：事件（适合松耦合的通知）
public class ProxyHostService
{
    public event EventHandler<ProxyStateChangedEventArgs>? StateChanged;

    private void OnStateChanged(ProxyState newState)
    {
        StateChanged?.Invoke(this, new ProxyStateChangedEventArgs(newState));
    }
}

// 方式二：回调（适合一对一的通知）
public class UpdateCheckService
{
    private Action<UpdateCheckResult>? _onResult;

    public void CheckAsync(Action<UpdateCheckResult> onResult)
    {
        _onResult = onResult;
        // ...
    }
}

// 方式三：Messenger（适合跨 ViewModel 通信）
// 使用 CommunityToolkit.Mvvm 的 IMessenger
public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        Messenger.Register<ProviderChangedMessage>(this, (r, m) =>
        {
            // 处理消息
            RefreshProviderList();
        });
    }
}

// 发送消息
Messenger.Send(new ProviderChangedMessage(providerId));
```

### 36.4.4 服务初始化顺序

```csharp
// 在 App.axaml.cs 中控制初始化顺序
public override void OnFrameworkInitializationCompleted()
{
    // 步骤 1：配置 Bootstrap（最优先）
    ApplyClaudeBootstrapConfig();

    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        // 步骤 2：判断启动模式
        var startHidden = StartupLaunchOptions.ShouldStartHidden(
            Environment.GetCommandLineArgs().Skip(1));

        // 步骤 3：平台特定配置
        MacDockIconService.ConfigureForWindowVisibility(!startHidden);

        // 步骤 4：创建核心 ViewModel（会触发 Service 初始化）
        _viewModel = new MainWindowViewModel();

        // 步骤 5：创建托盘菜单（依赖 ViewModel）
        _trayMenuController = new TrayMenuController(
            this, desktop, _viewModel, ShowMainWindow, LoadTrayIcon());

        // 步骤 6：显示主窗口（根据启动模式决定）
        if (!startHidden)
            ShowMainWindow();

        // 步骤 7：注册关闭处理
        desktop.ShutdownRequested += async (_, _) =>
        {
            _trayMenuController?.Dispose();
            _trayMenuController = null;
            CloseMainWindow();
            if (_viewModel is not null)
                await _viewModel.DisposeAsync();
            _viewModel = null;
        };
    }

    base.OnFrameworkInitializationCompleted();
}
```

### 36.4.5 CodexSwitch 的服务设计

CodexSwitch 使用了三种不同的服务设计模式：

```csharp
// 模式一：静态服务（AppThemeService）
// 无状态，全局可访问，适合工具类
public static class AppThemeService
{
    private static string _theme = "system";

    public static void Apply(string? theme)
    {
        _theme = Normalize(theme);
        // ...
    }
}

// 模式二：实例服务（ConfigurationStore）
// 有状态，需要注入，适合有依赖的服务
public sealed class ConfigurationStore
{
    private readonly AppPaths _paths;  // 依赖注入

    public ConfigurationStore(AppPaths paths)
    {
        _paths = paths;
    }

    public AppConfig LoadConfig() { }
    public void SaveConfig(AppConfig config) { }
}

// 模式三：无状态计算器（PriceCalculator）
// 接收配置，输出计算结果，适合纯函数式服务
public sealed class PriceCalculator
{
    private readonly ModelPricingCatalog _catalog;

    public PriceCalculator(ModelPricingCatalog catalog)
    {
        _catalog = catalog;
    }

    public CostBreakdown Calculate(string model, UsageTokens usage,
        ProviderCostSettings settings)
    {
        var rule = FindRule(model);
        if (rule is null)
            return new CostBreakdown(0m, 0m, 0m, settings.Multiplier);

        var baseMultiplier = settings.FastMode ? ResolveFastMultiplier(model) : 1m;
        var multiplier = settings.Multiplier * baseMultiplier;

        return new CostBreakdown(
            CalculateTieredCost(usage.InputTokens, rule.Input),
            CalculateTieredCost(usage.CachedInputTokens, rule.CachedInput),
            CalculateTieredCost(usage.CacheCreationInputTokens, rule.CacheCreationInput),
            CalculateTieredCost(usage.OutputTokens, rule.Output),
            multiplier);
    }
}
```

## 36.5 配置管理

### 36.5.1 配置文件格式选择

| 格式 | 优点 | 缺点 | 适用场景 |
|------|------|------|---------|
| **JSON** | 生态好、C# 原生支持、AOT 友好 | 不支持注释 | Avalonia 应用首选 |
| **TOML** | 支持注释、可读性好 | 生态较弱 | Codex/Claude Code 配置 |
| **YAML** | 人类友好、支持注释 | 缩进敏感、解析复杂 | CI/CD 配置 |
| **INI** | 极简、古老 | 不支持嵌套结构 | 简单键值配置 |

CodexSwitch 选择 JSON 作为主要配置格式，因为它与 .NET 的 `System.Text.Json` 完美配合，且支持 AOT 编译。

### 36.5.2 强类型配置设计

```csharp
// 配置类设计——强类型，避免魔法字符串
public sealed class AppConfig
{
    public AppUiSettings Ui { get; set; } = new();
    public ProxySettings Proxy { get; set; } = new();
    public NetworkSettings Network { get; set; } = new();
    public ResilienceSettings Resilience { get; set; } = new();
    public Collection<ProviderConfig> Providers { get; set; } = [];
    public string ActiveCodexProviderId { get; set; } = "";
    public string ActiveClaudeCodeProviderId { get; set; } = "";
}

// 子配置类——每个领域独立
public sealed class AppUiSettings
{
    public string Theme { get; set; } = "system";
    public string Language { get; set; } = "zh-CN";
}

public sealed class ProxySettings
{
    public bool Enabled { get; set; } = true;
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 12785;
}
```

### 36.5.3 配置热更新

```csharp
// 文件监听实现配置热更新
public class ConfigWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly ConfigurationStore _store;
    private readonly Action<AppConfig> _onChanged;
    private DateTime _lastWrite = DateTime.MinValue;

    public ConfigWatcher(string configPath, ConfigurationStore store,
        Action<AppConfig> onChanged)
    {
        _store = store;
        _onChanged = onChanged;

        _watcher = new FileSystemWatcher(Path.GetDirectoryName(configPath)!)
        {
            Filter = Path.GetFileName(configPath),
            NotifyFilter = NotifyFilters.LastWrite
        };
        _watcher.Changed += OnFileChanged;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // 防抖：避免重复触发
        var now = DateTime.UtcNow;
        if ((now - _lastWrite).TotalMilliseconds < 500)
            return;
        _lastWrite = now;

        // 重新加载配置
        try
        {
            var config = _store.LoadConfig();
            _onChanged(config);
        }
        catch (IOException) { /* 文件正在写入，忽略 */ }
    }

    public void Dispose() => _watcher.Dispose();
}
```

### 36.5.4 配置版本迁移

```csharp
// CodexSwitch 的配置迁移实践
public sealed class ConfigurationStore
{
    public static void EnsureValidDefaults(AppConfig config)
    {
        // 确保所有子对象不为 null
        config.Ui ??= new AppUiSettings();
        config.Proxy ??= new ProxySettings();
        config.Network ??= new NetworkSettings();
        config.Resilience ??= new ResilienceSettings();
        config.Providers ??= [];

        // 规范化值
        config.Ui.Theme = AppThemeService.Normalize(config.Ui.Theme);
        if (string.IsNullOrWhiteSpace(config.Ui.Language))
            config.Ui.Language = "zh-CN";

        // 确保默认 Provider 存在
        if (config.Providers.Count == 0)
            SeedDefaultProviders(config);

        // 迁移旧版本数据
        MigrateBuiltInProviders(config);
        EnsureRequiredBuiltIns(config);
        EnsureProviderClientSupport(config);
        EnsureProviderCodexSettings(config);
        EnsureProviderClaudeCodeSettings(config);
        EnsureProviderModelConversions(config);
        EnsureProviderUsageQueries(config);

        // 确保活动 Provider 有效
        EnsureActiveProvider(config, ClientAppKind.Codex);
        EnsureActiveProvider(config, ClientAppKind.ClaudeCode);
    }
}
```

### 36.5.5 配置加密

```csharp
// API Key 等敏感配置的加密存储
public static class SecureConfigStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CodexSwitch_v1");

    public static string Protect(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(bytes, Entropy,
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(string protectedText)
    {
        var protectedBytes = Convert.FromBase64String(protectedText);
        var bytes = ProtectedData.Unprotect(protectedBytes, Entropy,
            DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}

// 使用示例
public void SaveApiKey(string providerId, string apiKey)
{
    var config = _store.LoadConfig();
    var provider = config.Providers.First(p => p.Id == providerId);
    provider.ApiKey = SecureConfigStore.Protect(apiKey);
    _store.SaveConfig(config);
}
```

### 36.5.6 CodexSwitch 的原子写入

```csharp
// CodexSwitch 的配置保存——原子写入，防止损坏
private static void SaveJsonAtomically<T>(
    string path,
    T value,
    JsonTypeInfo<T> typeInfo)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

    // 写入临时文件
    var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
    using (var stream = File.Create(tempPath))
    {
        JsonSerializer.Serialize(stream, value, typeInfo);
    }

    // 原子替换（重试机制应对文件锁定）
    ReplaceFileWithRetry(tempPath, path);
}

private static void ReplaceFileWithRetry(string tempPath, string path)
{
    for (var attempt = 0; ; attempt++)
    {
        try
        {
            File.Move(tempPath, path, overwrite: true);
            return;
        }
        catch (UnauthorizedAccessException) when (attempt < 3)
        {
            Thread.Sleep(25 * (attempt + 1));
        }
        catch (IOException) when (attempt < 3)
        {
            Thread.Sleep(25 * (attempt + 1));
        }
    }
}
```

## 36.6 状态管理

### 36.6.1 全局状态 vs 局部状态

```csharp
// 全局状态：整个应用共享
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private AppConfig _config;           // 全局配置

    [ObservableProperty]
    private string _currentPage = "Home"; // 当前页面（影响导航）

    [ObservableProperty]
    private ClientAppKind _selectedClientApp = ClientAppKind.Codex; // 全局选择
}

// 局部状态：只在某个页面/控件内使用
public partial class ProvidersPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isEditing;              // 编辑状态（仅该页面）

    [ObservableProperty]
    private string _searchText = "";      // 搜索框文本（仅该页面）
}
```

### 36.6.2 状态提升模式

当多个子组件需要共享状态时，将状态提升到共同的父级 ViewModel：

```csharp
// 状态在 MainWindowViewModel 中管理，子页面通过 DataContext 访问
public partial class MainWindowViewModel : ViewModelBase
{
    // 提升的状态：Provider 列表（多个页面都需要）
    [ObservableProperty]
    private ObservableCollection<ProviderConfig> _providers = [];

    // 提升的状态：当前编辑的 Provider（编辑页和列表页都需要）
    [ObservableProperty]
    private string? _editingProviderId;
}
```

### 36.6.3 状态持久化

```csharp
// 状态持久化策略：关键状态自动保存
public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly ConfigurationStore _store;
    private AppConfig _config;

    // 每次修改 Provider 后自动保存
    [RelayCommand]
    private void SaveProviderSettings()
    {
        // 更新配置
        var provider = FindProvider(_editingProviderId);
        provider.BaseUrl = _selectedBaseUrl;
        provider.ApiKey = _selectedApiKey;

        // 立即持久化
        _store.SaveConfig(_config);
    }

    // 应用关闭时的清理
    public async ValueTask DisposeAsync()
    {
        // 确保配置保存
        _store.SaveConfig(_config);

        // 停止代理服务
        if (_proxyHostService is not null)
            await _proxyHostService.StopAsync();
    }
}
```

### 36.6.4 状态同步（多窗口）

```csharp
// 使用 Messenger 实现多窗口状态同步
public partial class SettingsWindowViewModel : ViewModelBase
{
    public SettingsWindowViewModel()
    {
        // 监听主窗口的配置变更
        Messenger.Register<ConfigChangedMessage>(this, (r, m) =>
        {
            // 同步最新配置到设置窗口
            RefreshSettings(m.NewConfig);
        });
    }

    [RelayCommand]
    private void ApplyTheme(string theme)
    {
        AppThemeService.Apply(theme);

        // 通知所有窗口主题已变更
        Messenger.Send(new ThemeChangedMessage(theme));
    }
}
```

## 36.7 事件和消息系统

### 36.7.1 弱事件模式

```csharp
// 标准事件的问题：订阅者忘记取消订阅会导致内存泄漏
public class BadExample
{
    public BadExample(ProxyHostService proxy)
    {
        // 如果 this 被 GC 了，但 proxy 还持有引用
        // this 就永远不会被 GC（内存泄漏）
        proxy.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e) { }
}

// 弱事件模式：不阻止 GC
public class GoodExample
{
    public GoodExample(ProxyHostService proxy)
    {
        WeakEventManager<ProxyHostService, EventArgs>.AddHandler(
            proxy, nameof(proxy.StateChanged), OnStateChanged);
    }

    private void OnStateChanged(object? sender, EventArgs e) { }
}
```

### 36.7.2 CommunityToolkit.Mvvm Messenger

```csharp
// 定义消息
public record ProviderChangedMessage(string ProviderId);
public record ThemeChangedMessage(string Theme);

// 发送方
public partial class MainWindowViewModel : ViewModelBase
{
    [RelayCommand]
    private void SwitchProvider(string providerId)
    {
        _config.ActiveCodexProviderId = providerId;
        _store.SaveConfig(_config);

        // 广播消息
        Messenger.Send(new ProviderChangedMessage(providerId));
    }
}

// 接收方
public partial class TrayMenuViewModel : ViewModelBase
{
    public TrayMenuViewModel()
    {
        Messenger.Register<ProviderChangedMessage>(this, (r, m) =>
        {
            // 更新托盘菜单中的 Provider 列表
            RefreshTrayMenu(m.ProviderId);
        });
    }
}
```

### 36.7.3 事件去抖和节流

```csharp
// 去抖（Debounce）：等待一段时间没有新事件后才执行
public static class EventDebouncer
{
    private static CancellationTokenSource? _cts;

    public static void Debounce(Action action, int delayMs = 300)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Delay(delayMs, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
                action();
        }, TaskScheduler.Default);
    }
}

// 使用场景：搜索框输入
[ObservableProperty]
private string _searchText = "";

partial void OnSearchTextChanged(string value)
{
    EventDebouncer.Debounce(() =>
    {
        // 300ms 内没有新输入才执行搜索
        FilterProviders(value);
    }, 300);
}
```

## 36.8 异步编程最佳实践

### 36.8.1 async/await 深入

```csharp
// UI 线程的异步操作：不阻塞 UI
[RelayCommand]
private async Task RefreshUsageAsync(CancellationToken ct)
{
    try
    {
        IsUsageRefreshing = true;

        // 异步等待，UI 线程不会被阻塞
        var result = await _providerUsageQueryService.QueryAsync(
            _selectedProviderId, ct);

        // 回到 UI 线程更新属性（自动由 SynchronizationContext 处理）
        UpdateUsageDisplay(result);
    }
    catch (OperationCanceledException)
    {
        // 用户取消，静默处理
    }
    catch (Exception ex)
    {
        ProxyStatus = $"Error: {ex.Message}";
    }
    finally
    {
        IsUsageRefreshing = false;
    }
}
```

### 36.8.2 Task vs ValueTask

```csharp
// Task：每次都分配堆对象
public async Task<string> GetDataAsync()
{
    // 即使结果已经缓存，也会分配 Task 对象
    if (_cache.TryGetValue("key", out var cached))
        return cached;

    return await FetchFromNetworkAsync();
}

// ValueTask：避免已完成情况下的分配
public async ValueTask<string> GetDataAsync()
{
    // 如果结果已缓存，不分配 Task 对象
    if (_cache.TryGetValue("key", out var cached))
        return cached;

    return await FetchFromNetworkAsync();
}

// 注意：ValueTask 不能多次 await，不能在 using 中使用
// 一般建议：优先使用 Task，只在性能敏感的热路径使用 ValueTask
```

### 36.8.3 CancellationToken 使用

```csharp
// 在 ViewModel 中管理 CancellationTokenSource
public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private CancellationTokenSource? _usageDashboardRefreshCts;

    [RelayCommand]
    private async Task RefreshUsageDashboardAsync()
    {
        // 取消之前的请求
        _usageDashboardRefreshCts?.Cancel();
        _usageDashboardRefreshCts = new CancellationTokenSource();
        var ct = _usageDashboardRefreshCts.Token;

        try
        {
            IsUsageRefreshing = true;

            // 传递 CancellationToken，支持取消
            var snapshot = await _usageLogReader.ReadSnapshotAsync(
                _usageTimeRange, ct);

            ct.ThrowIfCancellationRequested();

            // 更新 UI
            UpdateUsageDashboard(snapshot);
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsUsageRefreshing = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _usageDashboardRefreshCts?.Cancel();
        _usageDashboardRefreshCts?.Dispose();
    }
}
```

### 36.8.4 Dispatcher.UIThread 使用

```csharp
// 从后台线程更新 UI
public async Task ProcessDataInBackgroundAsync()
{
    await Task.Run(() =>
    {
        // 在后台线程执行耗时操作
        var result = HeavyComputation();

        // 回到 UI 线程更新属性
        Dispatcher.UIThread.Post(() =>
        {
            ResultText = result;
            IsProcessing = false;
        });
    });
}

// 使用 InvokeAsync 等待 UI 线程操作完成
public async Task UpdateUIAndWaitAsync()
{
    await Dispatcher.UIThread.InvokeAsync(() =>
    {
        // 确保在 UI 线程执行
        SomeControl.Items.Add(newItem);
    });
}
```

## 36.9 性能优化：内存

### 36.9.1 对象池模式

```csharp
// 对象池：复用对象，减少 GC 压力
public class ObjectPool<T> where T : class
{
    private readonly ConcurrentBag<T> _objects = new();
    private readonly Func<T> _factory;
    private readonly Action<T>? _reset;

    public ObjectPool(Func<T> factory, Action<T>? reset = null, int initialSize = 0)
    {
        _factory = factory;
        _reset = reset;

        for (int i = 0; i < initialSize; i++)
            _objects.Add(factory());
    }

    public T Rent()
    {
        return _objects.TryTake(out var item) ? item : _factory();
    }

    public void Return(T item)
    {
        _reset?.Invoke(item);
        _objects.Add(item);
    }
}

// 使用示例：复用 StringBuilder
var sbPool = new ObjectPool<StringBuilder>(
    () => new StringBuilder(256),
    sb => sb.Clear());

var sb = sbPool.Rent();
try
{
    sb.Append("Hello");
    // ...
}
finally
{
    sbPool.Return(sb);
}
```

### 36.9.2 大对象堆（LOH）优化

```csharp
// LOH 阈值：85,000 字节
// 超过此大小的对象会被分配到 LOH，GC 回收频率低

// 问题：频繁创建大数组
public void ProcessLargeData()
{
    // 每次都分配在 LOH
    var buffer = new byte[100_000];
    // ...
}

// 优化：使用 ArrayPool 复用大数组
public void ProcessLargeDataOptimized()
{
    var pool = ArrayPool<byte>.Shared;
    var buffer = pool.Rent(100_000);
    try
    {
        // 使用 buffer
    }
    finally
    {
        pool.Return(buffer);
    }
}
```

### 36.9.3 IDisposable 和资源释放

```csharp
// 正确的资源释放模式
public sealed class ProxyHostService : IDisposable
{
    private HttpClient? _httpClient;
    private WebApplication? _app;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _httpClient?.Dispose();
        _app?.DisposeAsync().AsTask().Wait();
    }
}

// 异步释放模式
public sealed class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        _usageDashboardRefreshCts?.Cancel();
        _usageDashboardRefreshCts?.Dispose();

        if (_proxyHostService is not null)
            _proxyHostService.Dispose();

        _sharedHttpClient?.Dispose();
    }
}
```

### 36.9.4 内存泄漏检测

```csharp
// 常见内存泄漏模式及修复

// 泄漏一：事件未取消订阅
public class LeakyViewModel
{
    public LeakyViewModel(ProxyHostService proxy)
    {
        proxy.StateChanged += OnStateChanged; // 泄漏！
    }
}

// 修复：实现 IDisposable 并取消订阅
public class FixedViewModel : IDisposable
{
    private readonly ProxyHostService _proxy;

    public FixedViewModel(ProxyHostService proxy)
    {
        _proxy = proxy;
        _proxy.StateChanged += OnStateChanged;
    }

    public void Dispose()
    {
        _proxy.StateChanged -= OnStateChanged;
    }
}

// 泄漏二：Timer 未停止
public class LeakyTimer
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public LeakyTimer()
    {
        _timer.Tick += OnTick;
        _timer.Start(); // 泄漏！Timer 持有引用
    }
}

// 泄漏三：静态集合不断增长
public class LeakyCache
{
    private static readonly List<byte[]> _cache = new(); // 永远增长！

    public static void Add(byte[] data) => _cache.Add(data);
}
```

### 36.9.5 CodexSwitch 的内存优化

```csharp
// CodexSwitch 的 RequestMemoryTrim——窗口关闭时主动压缩内存
public partial class App : Application
{
    private void ReleaseMainWindow(MainWindow mainWindow)
    {
        mainWindow.Closed -= OnMainWindowClosed;
        mainWindow.DataContext = null;

        if (ReferenceEquals(_mainWindow, mainWindow))
        {
            _mainWindow = null;
            MacDockIconService.ConfigureForWindowVisibility(false);

            // 主动请求内存回收
            RequestMemoryTrim();
        }
    }

    private static void RequestMemoryTrim()
    {
        // 强制 GC，压缩大对象堆
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced,
            blocking: false, compacting: true);
    }
}
```

## 36.10 性能优化：渲染

### 36.10.1 减少视觉树深度

```xml
<!-- 不好的设计：深层嵌套 -->
<Border>
  <StackPanel>
    <Border>
      <StackPanel>
        <Border>
          <TextBlock Text="Hello" />
        </Border>
      </StackPanel>
    </Border>
  </StackPanel>
</Border>

<!-- 好的设计：扁平化 -->
<Border Padding="8">
  <TextBlock Text="Hello" />
</Border>

<!-- 量化对比 -->
<!-- 深层嵌套：8 个节点，4 次 Measure，4 次 Arrange -->
<!-- 扁平化：2 个节点，1 次 Measure，1 次 Arrange -->
<!-- 性能差异：嵌套越深，布局计算量呈线性增长 -->
```

### 36.10.2 虚拟化列表

```xml
<!-- 默认不虚拟化：所有项都创建控件 -->
<ListBox ItemsSource="{Binding LargeCollection}">
    <!-- 10000 项 = 10000 个控件 -->
</ListBox>

<!-- 启用虚拟化：只创建可见区域的控件 -->
<ListBox ItemsSource="{Binding LargeCollection}">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel />
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
</ListBox>

<!-- 自定义虚拟化：更大的缓存 -->
<ListBox ItemsSource="{Binding LargeCollection}">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel
                VirtualizationMode="Recycling"
                ScrollViewer.CanContentScroll="True" />
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
</ListBox>
```

### 36.10.3 冻结 Brush 和 Geometry

```csharp
// 冻结资源：标记为不可变，Skia 可以优化渲染
public static class FrozenResources
{
    // 冻结的 Brush 不会被修改，Skia 可以缓存其渲染状态
    public static readonly IBrush PrimaryBrush = new SolidColorBrush(Colors.Blue).Frozen();
    public static readonly IBrush SecondaryBrush = new SolidColorBrush(Colors.Gray).Frozen();

    // 使用 Frozen() 扩展方法
    public static T Frozen<T>(this T resource) where T : AvaloniaObject
    {
        if (resource is IFreezable freezable)
            freezable.Freeze();
        return resource;
    }
}

// 在 XAML 中：Avalonia 的 StaticResource 默认就是冻结的
<!-- 这些资源在加载时就被冻结 -->
<SolidColorBrush x:Key="PrimaryBrush" Color="#2563EB" />
```

### 36.10.4 图片懒加载

```xml
<!-- 图片懒加载：只在进入可视区域时加载 -->
<Image>
    <Image.Source>
        <Bitmap DecodePixelWidth="200"
                UriSource="{Binding ImageUrl}" />
    </Image.Source>
</Image>
```

```csharp
// 后台加载图片，不阻塞 UI
public async Task<Bitmap?> LoadImageAsync(string url)
{
    return await Task.Run(async () =>
    {
        using var client = new HttpClient();
        var bytes = await client.GetByteArrayAsync(url);
        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    });
}
```

## 36.11 性能优化：绑定

### 36.11.1 编译绑定 vs 反射绑定

```xml
<!-- 反射绑定：运行时通过反射查找属性，性能较差 -->
<TextBlock Text="{Binding UserName}" />

<!-- 编译绑定：编译时生成强类型代码，性能好 10-50 倍 -->
<Window x:Class="MyApp.MainWindow"
        x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding UserName}" />
</Window>

<!-- 编译绑定必须设置 x:DataType，否则回退到反射 -->
```

**性能对比数据：**

| 绑定方式 | 属性解析耗时 | 内存分配 | 适用场景 |
|---------|-------------|---------|---------|
| 反射绑定 | ~500ns | ~200 bytes | 快速原型 |
| 编译绑定 | ~10ns | ~0 bytes | 生产环境 |

### 36.11.2 避免深层绑定路径

```xml
<!-- 不好：深层绑定路径，每次中间属性变化都要重新解析 -->
<TextBlock Text="{Binding Provider.Settings.Advanced.Timeout}" />

<!-- 好：扁平化属性，直接绑定 -->
<TextBlock Text="{Binding ProviderTimeout}" />

<!-- ViewModel 中的扁平化 -->
public partial class MainWindowViewModel : ViewModelBase
{
    // 从深层对象提取为扁平属性
    [ObservableProperty]
    private int _providerTimeout;

    // 当 Provider 变化时同步
    partial void OnSelectedProviderChanged(ProviderConfig value)
    {
        ProviderTimeout = value?.Settings?.Advanced?.Timeout ?? 30;
    }
}
```

### 36.11.3 使用 OneTime 绑定

```xml
<!-- 不需要变化的绑定使用 OneTime -->
<TextBlock Text="{Binding AppVersion, Mode=OneTime}" />
<Image Source="{Binding AppIcon, Mode=OneTime}" />

<!-- 对比 -->
<!-- TwoWay 绑定：持续监听 PropertyChanged -->
<!-- OneTime 绑定：设置一次后不再监听，节省 CPU -->
```

### 36.11.4 集合绑定优化

```csharp
// 不好的方式：每次替换整个集合
public void UpdateProviders(List<ProviderConfig> newProviders)
{
    Providers = new ObservableCollection<ProviderConfig>(newProviders);
    // 整个列表重建，所有控件重新创建
}

// 好的方式：增量更新
public void UpdateProviders(List<ProviderConfig> newProviders)
{
    Providers.Clear();
    foreach (var p in newProviders)
        Providers.Add(p);
    // 控件复用，只更新变化的部分
}

// 最佳方式：使用 Diff 算法
public void UpdateProvidersOptimized(List<ProviderConfig> newProviders)
{
    // 只更新变化的项
    var toRemove = Providers.Where(p =>
        !newProviders.Any(np => np.Id == p.Id)).ToList();
    var toAdd = newProviders.Where(np =>
        !Providers.Any(p => p.Id == np.Id)).ToList();

    foreach (var p in toRemove) Providers.Remove(p);
    foreach (var p in toAdd) Providers.Add(p);
}
```

## 36.12 性能优化：启动

### 36.12.1 延迟初始化

```csharp
// Lazy<T> 延迟初始化
public class MainWindowViewModel : ViewModelBase
{
    // 只在第一次访问时创建
    private readonly Lazy<ProxyHostService> _proxyService = new(() =>
    {
        return new ProxyHostService(/* ... */);
    });

    // 第一次访问 _proxyService.Value 时才创建实例
    public ProxyHostService ProxyService => _proxyService.Value;
}

// 延迟加载页面
public partial class MainWindowViewModel : ViewModelBase
{
    private ProvidersPageViewModel? _providersPage;

    public ProvidersPageViewModel ProvidersPage =>
        _providersPage ??= new ProvidersPageViewModel(_config, _store);
}
```

### 36.12.2 Native AOT 加速

```xml
<!-- 在 .csproj 中启用 Native AOT -->
<PropertyGroup>
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>

<!-- 发布命令 -->
<!-- dotnet publish -c Release -r win-x64 -p:PublishAot=true -->
```

**AOT 启动性能对比：**

| 启动方式 | 冷启动时间 | 热启动时间 | 包大小 |
|---------|-----------|-----------|--------|
| Framework-dependent | ~2.5s | ~0.8s | ~5MB |
| Self-contained | ~1.5s | ~0.6s | ~80MB |
| Native AOT | ~0.3s | ~0.1s | ~30MB |

### 36.12.3 启动时间测量

```csharp
// 在 Program.cs 中测量启动时间
sealed class Program
{
    private static readonly Stopwatch StartupTimer = Stopwatch.StartNew();

    [STAThread]
    public static void Main(string[] args)
    {
        // 记录到 Main 的时间
        Console.WriteLine($"[Startup] Main reached: {StartupTimer.ElapsedMilliseconds}ms");

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }
}

// 在 App.axaml.cs 中测量初始化完成时间
public override void OnFrameworkInitializationCompleted()
{
    Console.WriteLine($"[Startup] Framework init: " +
        $"{Program.StartupTimer.ElapsedMilliseconds}ms");

    // ... 初始化代码

    base.OnFrameworkInitializationCompleted();

    Console.WriteLine($"[Startup] App ready: " +
        $"{Program.StartupTimer.ElapsedMilliseconds}ms");
}
```

## 36.13 性能优化：数据

### 36.13.1 分页加载

```csharp
// CodexSwitch 的使用日志分页
public partial class MainWindowViewModel : ViewModelBase
{
    private const int UsageLogPageSize = 10;

    [ObservableProperty]
    private int _usageLogPage = 1;

    [ObservableProperty]
    private bool _hasNextUsageLogPage;

    [RelayCommand]
    private async Task LoadUsageLogPageAsync(int page)
    {
        UsageLogPage = page;

        var snapshot = await _usageLogReader.ReadPageAsync(
            page, UsageLogPageSize, _usageTimeRange);

        HasNextUsageLogPage = snapshot.HasNextPage;

        // 只更新当前页数据，不重建整个集合
        UpdateUsageLogDisplay(snapshot.Rows);
    }
}
```

### 36.13.2 虚拟化数据源

```csharp
// 数据虚拟化：只加载可见区域的数据
public class VirtualizingCollection<T> : IList, INotifyCollectionChanged
{
    private readonly Dictionary<int, T> _cache = new();
    private readonly Func<int, int, IList<T>> _fetchPage;
    private readonly int _pageSize;
    private readonly int _totalCount;

    public object? this[int index]
    {
        get
        {
            if (!_cache.ContainsKey(index))
            {
                // 按需加载
                var page = index / _pageSize;
                var items = _fetchPage(page, _pageSize);
                for (int i = 0; i < items.Count; i++)
                    _cache[page * _pageSize + i] = items[i];
            }
            return _cache[index];
        }
    }

    public int Count => _totalCount;
}
```

### 36.13.3 数据缓存策略

```csharp
// 内存缓存 + 过期策略
public class TimedCache<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, CacheEntry<TValue>> _cache = new();
    private readonly TimeSpan _ttl;

    public TimedCache(TimeSpan ttl)
    {
        _ttl = ttl;
    }

    public TValue GetOrAdd(TKey key, Func<TValue> factory)
    {
        if (_cache.TryGetValue(key, out var entry) &&
            DateTime.UtcNow - entry.CreatedAt < _ttl)
        {
            return entry.Value;
        }

        var value = factory();
        _cache[key] = new CacheEntry<TValue>(value, DateTime.UtcNow);
        return value;
    }

    private record CacheEntry<T>(T Value, DateTime CreatedAt);
}

// 使用
private readonly TimedCache<string, ProviderUsageQueryResult> _usageCache = new(
    TimeSpan.FromMinutes(5));
```

## 36.14 可复用组件设计

### 36.14.1 自定义控件库设计

```csharp
// 控件库的项目结构
// CodexSwitchUI/
// ├── Controls/
// │   ├── Card.cs
// │   ├── Badge.cs
// │   ├── SegmentedControl.cs
// │   └── IconButton.cs
// ├── Themes/
// │   ├── CodexSwitchThemeManager.cs
// │   └── CodexSwitchThemeOptions.cs
// └── Styles/
//     ├── Card.axaml
//     └── Badge.axaml

// 控件基类设计
public class Card : ContentControl
{
    // 注册 Avalonia 属性
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<Card, CornerRadius>(nameof(CornerRadius),
            new CornerRadius(8));

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    // 命令属性
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<Card, ICommand?>(nameof(Command));

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
```

### 36.14.2 泛型模式

```csharp
// 泛型 ViewModel 基类：带分页功能
public abstract class PagedViewModelBase<TItem> : ViewModelBase
{
    private readonly ObservableCollection<TItem> _items = new();

    public ObservableCollection<TItem> Items => _items;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private bool _hasNextPage;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    public async Task LoadPageAsync(int page)
    {
        CurrentPage = page;
        IsLoading = true;

        try
        {
            var result = await FetchPageAsync(page, PageSize);
            HasNextPage = result.HasNextPage;

            Items.Clear();
            foreach (var item in result.Items)
                Items.Add(item);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected abstract Task<PageResult<TItem>> FetchPageAsync(int page, int pageSize);
}
```

## 36.15 设计模式在 Avalonia 中的应用

### 36.15.1 单例模式（服务层）

```csharp
// 通过 DI 实现的单例——比手动单例更安全
services.AddSingleton<ConfigurationStore>();

// 手动单例——简单但不利于测试
public sealed class AppThemeService
{
    private static string _theme = "system";

    public static void Apply(string? theme)
    {
        _theme = Normalize(theme);
        // ...
    }
}
```

### 36.15.2 工厂模式（控件创建）

```csharp
// ViewLocator——Avalonia 中的工厂模式
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        var name = data?.GetType().FullName!
            .Replace("ViewModel", "View");
        var type = Type.GetType(name);

        return type != null
            ? (Control)Activator.CreateInstance(type)!
            : new TextBlock { Text = $"View not found: {name}" };
    }

    public bool Match(object? data) => data is ViewModelBase;
}

// 协议适配器工厂——CodexSwitch 的设计
public class ProviderAdapterFactory
{
    private readonly IServiceProvider _provider;

    public IProviderProtocolAdapter Create(ProviderProtocol protocol)
    {
        return protocol switch
        {
            ProviderProtocol.OpenAiResponses =>
                _provider.GetRequiredService<OpenAiResponsesAdapter>(),
            ProviderProtocol.OpenAiChat =>
                _provider.GetRequiredService<OpenAiChatAdapter>(),
            ProviderProtocol.AnthropicMessages =>
                _provider.GetRequiredService<AnthropicMessagesAdapter>(),
            _ => throw new NotSupportedException()
        };
    }
}
```

### 36.15.3 观察者模式（事件系统）

```csharp
// Avalonia 的 PropertyChanged 就是观察者模式
public class ObservableConfig : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _theme = "system";
    public string Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(Theme)));
        }
    }
}

// Messenger 也是观察者模式的变体
public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        // 注册观察者
        Messenger.Register<ConfigChangedMessage>(this, (r, m) =>
        {
            OnConfigChanged(m.Config);
        });
    }
}
```

### 36.15.4 命令模式（ICommand）

```csharp
// CommunityToolkit.Mvvm 的 [RelayCommand] 就是命令模式
public partial class MainWindowViewModel : ViewModelBase
{
    // 自动生成 SwitchProviderCommand 属性
    [RelayCommand(CanExecute = nameof(CanSwitchProvider))]
    private void SwitchProvider(string providerId)
    {
        _config.ActiveCodexProviderId = providerId;
        _store.SaveConfig(_config);
    }

    private bool CanSwitchProvider(string providerId)
    {
        return !string.IsNullOrWhiteSpace(providerId) &&
            _config.Providers.Any(p => p.Id == providerId);
    }

    // 异步命令
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task RefreshUsageAsync(CancellationToken ct)
    {
        var result = await _usageQueryService.QueryAsync(ct);
        UpdateUsage(result);
    }
    // 自动生成 RefreshUsageCommand 和 CancelRefreshUsageCommand
}
```

### 36.15.5 策略模式（协议适配器）

```csharp
// 策略接口
public interface IProviderProtocolAdapter
{
    Task<ProxyResponse> SendRequestAsync(ProxyRequest request, CancellationToken ct);
}

// 策略实现
public class OpenAiResponsesAdapter : IProviderProtocolAdapter
{
    public async Task<ProxyResponse> SendRequestAsync(
        ProxyRequest request, CancellationToken ct)
    {
        // OpenAI Responses 协议的实现
        var payload = ResponsesPayloadBuilder.Build(request);
        return await SendUpstreamAsync(payload, ct);
    }
}

public class AnthropicMessagesAdapter : IProviderProtocolAdapter
{
    public async Task<ProxyResponse> SendRequestAsync(
        ProxyRequest request, CancellationToken ct)
    {
        // Anthropic Messages 协议的实现
        var payload = AnthropicPayloadBuilder.Build(request);
        return await SendUpstreamAsync(payload, ct);
    }
}

// 运行时切换策略
public class ProviderRoutingResolver
{
    public IProviderProtocolAdapter Resolve(ProviderConfig provider)
    {
        return provider.Protocol switch
        {
            ProviderProtocol.OpenAiResponses => _responsesAdapter,
            ProviderProtocol.OpenAiChat => _chatAdapter,
            ProviderProtocol.AnthropicMessages => _anthropicAdapter,
            _ => throw new NotSupportedException()
        };
    }
}
```

### 36.15.6 CodexSwitch 中的设计模式总结

| 设计模式 | 应用位置 | 作用 |
|---------|---------|------|
| **单例** | `AppThemeService`, `ConfigurationStore` | 全局唯一服务 |
| **工厂** | `ViewLocator`, `ProviderAdapterFactory` | 对象创建解耦 |
| **观察者** | `PropertyChanged`, `Messenger` | 事件通知 |
| **命令** | `[RelayCommand]` | UI 操作封装 |
| **策略** | `IProviderProtocolAdapter` | 可替换的算法 |
| **模板方法** | `ConfigurationStore.EnsureValidDefaults` | 定义算法骨架 |
| **构建器** | `ResponsesPayloadBuilder` | 复杂对象构建 |

## 36.16 测试策略

### 36.16.1 单元测试

```csharp
// ViewModel 单元测试
public class MainWindowViewModelTests
{
    [Fact]
    public void SwitchProvider_UpdatesActiveProvider()
    {
        // Arrange
        var config = new AppConfig
        {
            Providers =
            [
                new ProviderConfig { Id = "p1", Enabled = true, SupportsCodex = true },
                new ProviderConfig { Id = "p2", Enabled = true, SupportsCodex = true }
            ],
            ActiveCodexProviderId = "p1"
        };

        var vm = CreateViewModel(config);

        // Act
        vm.SwitchProviderCommand.Execute("p2");

        // Assert
        Assert.Equal("p2", config.ActiveCodexProviderId);
    }
}

// Service 单元测试
public class PriceCalculatorTests
{
    [Fact]
    public void CalculatesCachedTokens()
    {
        var catalog = new ModelPricingCatalog
        {
            BillingUnitTokens = 1_000_000,
            Models = [new ModelPricingRule
            {
                Id = "gpt-4o",
                Input = new TokenPriceTable { Tiers = [
                    new PricingTier { PricePerUnit = 2.50m }] },
                Output = new TokenPriceTable { Tiers = [
                    new PricingTier { PricePerUnit = 10.00m }] }
            }]
        };

        var calculator = new PriceCalculator(catalog);
        var usage = new UsageTokens(100_000, 50_000, 0, 20_000);
        var result = calculator.Calculate("gpt-4o", usage,
            new ProviderCostSettings());

        Assert.Equal(0.25m, result.InputCost);     // 100K * $2.50/1M
        Assert.Equal(0.20m, result.OutputCost);     // 20K * $10.00/1M
    }
}
```

### 36.16.2 集成测试

```csharp
// 配置集成测试
public class ConfigurationStoreTests : IDisposable
{
    private readonly string _tempDir;

    public ConfigurationStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsCorrectly()
    {
        var paths = new AppPaths(
            rootDirectory: _tempDir,
            codexDirectory: Path.Combine(_tempDir, ".codex"),
            claudeDirectory: Path.Combine(_tempDir, ".claude"));

        var store = new ConfigurationStore(paths);
        var config = store.LoadConfig();

        config.Ui.Theme = "dark";
        store.SaveConfig(config);

        var loaded = store.LoadConfig();
        Assert.Equal("dark", loaded.Ui.Theme);
    }

    public void Dispose() => Directory.Delete(_tempDir, true);
}
```

### 36.16.3 UI 测试

```csharp
// Avalonia.Headless UI 测试
public class MainWindowTests
{
    [Fact]
    public void ClickProvider_ShowsProviderDetails()
    {
        // 使用 Avalonia.Headless 进行 UI 测试
        var app = BuildTestApp();
        var window = app.MainWindow;
        var viewModel = (MainWindowViewModel)window.DataContext!;

        // 模拟点击
        viewModel.SelectedProviderId = "test-provider";

        // 验证
        Assert.Equal("test-provider", viewModel.SelectedProviderId);
        Assert.True(viewModel.IsProviderSelected);
    }
}
```

## 36.17 代码质量

### 36.17.1 静态分析

```xml
<!-- 在 .csproj 中启用 Roslyn Analyzers -->
<PropertyGroup>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisMode>recommended</AnalysisMode>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>

<!-- 推荐的 Analyzer 包 -->
<ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers"
                      Version="8.0.0" PrivateAssets="all" />
    <PackageReference Include="StyleCop.Analyzers"
                      Version="1.2.0-beta.556" PrivateAssets="all" />
</ItemGroup>
```

### 36.17.2 代码风格

```ini
# .editorconfig 示例
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.cs]
# 命名规则
dotnet_naming_rule.private_fields_should_be_underscore.severity = warning
dotnet_naming_rule.private_fields_should_be_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_underscore.style = underscore_prefix
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_style.underscore_prefix.required_prefix = _
dotnet_naming_style.underscore_prefix.capitalization = camel_case

# using 排序
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = true

# var 偏好
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion
```

## 36.18 实战：大型项目架构

### 36.18.1 项目结构设计

```
大型 Avalonia 应用推荐结构：

MyApp/
├── src/
│   ├── MyApp.App/              # 应用入口（启动、DI、配置）
│   │   ├── Program.cs
│   │   ├── App.axaml
│   │   └── App.axaml.cs
│   │
│   ├── MyApp.UI/               # UI 层（Views、Styles、Controls）
│   │   ├── Views/
│   │   ├── Controls/
│   │   ├── Styles/
│   │   └── Converters/
│   │
│   ├── MyApp.ViewModels/       # ViewModel 层
│   │   ├── MainWindowViewModel.cs
│   │   └── Pages/
│   │
│   ├── MyApp.Services/         # 服务层
│   │   ├── Configuration/
│   │   ├── Networking/
│   │   └── Storage/
│   │
│   ├── MyApp.Models/           # 数据模型
│   │   ├── AppConfig.cs
│   │   └── DTOs/
│   │
│   └── MyApp.Shared/           # 共享工具
│       ├── Extensions/
│       └── Helpers/
│
├── tests/
│   ├── MyApp.Tests.Unit/
│   └── MyApp.Tests.Integration/
│
└── MyApp.Components/           # 可复用组件库
    ├── Controls/
    └── Themes/
```

### 36.18.2 模块化设计

```csharp
// 模块接口
public interface IAppModule
{
    void ConfigureServices(IServiceCollection services);
    void Initialize(IServiceProvider provider);
}

// 功能模块实现
public class ProxyModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ProxyHostService>();
        services.AddSingleton<ProviderRoutingResolver>();
        services.AddKeyedSingleton<IProviderProtocolAdapter,
            OpenAiResponsesAdapter>("openai-responses");
    }

    public void Initialize(IServiceProvider provider)
    {
        // 模块初始化逻辑
    }
}

// 应用启动时组装模块
public class AppModuleLoader
{
    private readonly List<IAppModule> _modules = new()
    {
        new ProxyModule(),
        new I18nModule(),
        new ThemeModule(),
        new UpdateModule()
    };

    public void ConfigureServices(IServiceCollection services)
    {
        foreach (var module in _modules)
            module.ConfigureServices(services);
    }

    public void Initialize(IServiceProvider provider)
    {
        foreach (var module in _modules)
            module.Initialize(provider);
    }
}
```

### 36.18.3 CodexSwitch 的架构分析

CodexSwitch 采用了"中央 ViewModel + 服务层"的架构模式：

```
架构特点：
┌─────────────────────────────────────────┐
│         MainWindowViewModel             │
│    （中央协调者，3000+ 行代码）           │
│                                         │
│    管理：配置、导航、Provider、代理、     │
│          使用量、更新、OAuth、国际化      │
└────────┬────────────────┬───────────────┘
         │                │
         ▼                ▼
   ┌──────────┐    ┌──────────────┐
   │ Services │    │   Proxy/     │
   │ 配置存储  │    │ 协议适配器   │
   │ 价格计算  │    │ 路由解析     │
   │ 主题管理  │    │ 请求转发     │
   └──────────┘    └──────────────┘
```

**CodexSwitch 架构的优势：**

1. **集中管理**：所有状态在一个 ViewModel 中，避免了 ViewModel 间通信的复杂性
2. **服务解耦**：业务逻辑封装在 Service 中，ViewModel 只做协调
3. **协议适配**：通过 `IProviderProtocolAdapter` 接口支持多种协议
4. **原子配置**：`ConfigurationStore` 的原子写入确保配置不损坏
5. **内存管理**：窗口关闭时主动 GC，减少内存泄漏

**可改进的方向：**

1. **拆分 MainWindowViewModel**：3000+ 行的 ViewModel 可以按功能拆分为子 ViewModel
2. **引入 DI 容器**：当前通过 `new` 创建服务，可以改用 DI 容器管理
3. **增加单元测试**：Service 层有良好的可测试性，可以增加测试覆盖

## Deep Dive

### Avalonia 的内存模型

Avalonia 使用引用计数和弱引用来管理视觉树：

```
视觉树的生命周期：

1. 创建：new Control()
2. 添加到树：parent.Children.Add(child)
   - child 增加对 parent 的强引用
   - parent 增加对 child 的强引用
3. 从树移除：parent.Children.Remove(child)
   - 双向引用解除
   - 如果没有其他引用，child 可被 GC
4. GC 回收：GC.Collect()
   - 无引用的对象被回收
   - IDisposable.Dispose() 被调用（如果实现了）
```

### 渲染管线优化

```
Avalonia 渲染管线：

1. 布局（Layout）
   ├── Measure：计算每个控件的期望大小
   └── Arrange：确定每个控件的最终位置和大小

2. 绘制（Render）
   ├── 构建绘制指令列表
   └── 通过 Skia 渲染到帧缓冲区

3. 合成（Composition）
   ├── 将多个层合成为最终图像
   └── 应用变换、剪裁、透明度

优化建议：
- 减少布局变化：频繁的 Measure/Arrange 是性能杀手
- 缓存绘制结果：不变的内容不要重绘
- 使用 DrawingGroup：将复杂绘制组合为一个单元
```

### GC 在 UI 应用中的行为

```
.NET GC 代际模型：

Gen 0（短命对象）
├── 临时变量、字符串拼接
├── 回收频率：最频繁
└── 回收速度：最快

Gen 1（中等寿命）
├── 缓存对象、短生命周期的集合
├── 回收频率：中等
└── 回收速度：中等

Gen 2（长寿命）
├── Singleton 服务、长期缓存
├── 回收频率：最低
└── 回收速度：最慢

LOH（大对象堆，>= 85KB）
├── 大数组、Bitmap
├── 回收频率：与 Gen 2 相同
└── 不会被压缩（除非显式请求）

UI 应用的 GC 优化：
1. 减少 Gen 0/1 的分配（减少 UI 卡顿）
2. 使用对象池复用对象
3. 避免在渲染循环中分配
4. 使用 ArrayPool 复用大数组
5. 主动调用 GC.Collect(compacting: true) 压缩 LOH
```

## Cross References

- **第 1 章 Avalonia 框架概览**：了解 Avalonia 的整体架构和设计理念
- **第 2 章 项目结构**：深入理解 Avalonia 项目的标准组织方式
- **第 6 章 MVVM 模式实战**：MVVM 的详细实现，与本章的架构设计互补
- **第 7 章 样式与主题**：`AppThemeService` 的主题系统实现
- **第 9 章 自定义控件**：可复用组件设计的控件层实现
- **第 15 章 编译绑定**：绑定性能优化的核心技术
- **第 22 章 属性系统**：Avalonia 属性系统对性能的影响

## Common Pitfalls

### 1. God ViewModel

```csharp
// 错误：一个 ViewModel 做所有事
public class MainWindowViewModel : ViewModelBase
{
    // 3000+ 行，几十个属性，几十个命令
    // 难以维护、难以测试
}

// 修复：按功能拆分
public class MainWindowViewModel : ViewModelBase
{
    public ProvidersViewModel Providers { get; }
    public SettingsViewModel Settings { get; }
    public UsageViewModel Usage { get; }
}
```

### 2. 服务层持有 UI 引用

```csharp
// 错误：Service 引用 ViewModel 或控件
public class ProxyHostService
{
    private MainWindowViewModel _vm; // 禁止！

    public void UpdateUI()
    {
        _vm.ProxyStatus = "Running"; // UI 耦合
    }
}

// 正确：通过事件或消息通知
public class ProxyHostService
{
    public event EventHandler<string>? StatusChanged;

    private void UpdateStatus(string status)
    {
        StatusChanged?.Invoke(this, status);
    }
}
```

### 3. 忘记取消订阅

```csharp
// 错误：事件订阅导致内存泄漏
public class LeakyPage : UserControl
{
    public LeakyPage()
    {
        ViewModel.PropertyChanged += OnPropertyChanged;
        // 页面关闭后，ViewModel 仍然持有 LeakyPage 的引用
    }
}

// 正确：取消订阅
public class SafePage : UserControl, IDisposable
{
    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnPropertyChanged;
    }
}
```

### 4. UI 线程阻塞

```csharp
// 错误：在 UI 线程执行耗时操作
public void LoadData()
{
    var data = File.ReadAllText("large-file.json"); // 阻塞 UI！
    ProcessData(data); // 耗时计算，阻塞 UI！
}

// 正确：异步执行
public async Task LoadDataAsync()
{
    var data = await File.ReadAllTextAsync("large-file.json");
    await Task.Run(() => ProcessData(data));
}
```

### 5. 频繁的 PropertyChanged

```csharp
// 错误：循环中频繁触发通知
public void UpdateItems()
{
    for (int i = 0; i < 1000; i++)
    {
        Items.Add(newItem); // 每次 Add 都触发 CollectionChanged
    }
}

// 正确：批量更新
public void UpdateItems()
{
    // 暂停通知
    Items.RaiseListChangedEvents = false;
    for (int i = 0; i < 1000; i++)
    {
        Items.Add(newItem);
    }
    Items.RaiseListChangedEvents = true;
    Items.ResetBindings();
}
```

### 6. 编译绑定遗漏 x:DataType

```xml
<!-- 错误：忘记设置 x:DataType，回退到反射绑定 -->
<Window x:Class="MyApp.MainWindow">
    <TextBlock Text="{Binding UserName}" />
</Window>

<!-- 正确：设置 x:DataType 启用编译绑定 -->
<Window x:Class="MyApp.MainWindow"
        x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding UserName}" />
</Window>
```

### 7. 不使用虚拟化列表

```xml
<!-- 错误：大列表不虚拟化 -->
<ListBox ItemsSource="{Binding LargeCollection}">
    <!-- 10000 项全部创建控件 -->
</ListBox>

<!-- 正确：启用虚拟化 -->
<ListBox ItemsSource="{Binding LargeCollection}">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel />
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
</ListBox>
```

### 8. 配置保存不原子

```csharp
// 错误：直接写入目标文件
public void SaveConfig(AppConfig config)
{
    var json = JsonSerializer.Serialize(config);
    File.WriteAllText(_path, json); // 如果中途崩溃，配置损坏
}

// 正确：原子写入
public void SaveConfig(AppConfig config)
{
    var tempPath = _path + ".tmp";
    File.WriteAllText(tempPath, JsonSerializer.Serialize(config));
    File.Move(tempPath, _path, overwrite: true);
}
```

### 9. Singleton 中创建 Transient

```csharp
// 错误：Singleton 持有 Transient 引用
public class BadSingleton
{
    private readonly TransientService _service; // 永远不会释放

    public BadSingleton(TransientService service)
    {
        _service = service;
    }
}

// 正确：使用作用域
public class GoodSingleton
{
    private readonly IServiceScopeFactory _scopeFactory;

    public void DoWork()
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TransientService>();
    }
}
```

### 10. 异步方法中的同步等待

```csharp
// 错误：.Result 或 .Wait() 导致死锁
public void BadMethod()
{
    var result = GetDataAsync().Result; // 死锁风险！
}

// 正确：全程 async/await
public async Task GoodMethodAsync()
{
    var result = await GetDataAsync();
}
```

### 11. 不处理 CancellationToken

```csharp
// 错误：忽略取消请求
public async Task ProcessAsync(CancellationToken ct)
{
    while (true)
    {
        await DoWorkAsync(); // 用户取消了还在继续
    }
}

// 正确：检查取消
public async Task ProcessAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        await DoWorkAsync(ct);
    }
    ct.ThrowIfCancellationRequested();
}
```

### 12. 过度使用 static

```csharp
// 错误：所有工具方法都是 static
public static class AppHelper
{
    public static void DoThis() { }
    public static void DoThat() { }
    // 难以 Mock，难以测试
}

// 正确：通过接口注入
public interface IAppHelper
{
    void DoThis();
    void DoThat();
}

public class AppHelper : IAppHelper
{
    public void DoThis() { }
    public void DoThat() { }
}
```

## Try It Yourself

### 练习 1：实现一个简单的 DI 容器

创建一个简化版的 DI 容器，支持 Singleton 和 Transient 生命周期。

```csharp
// 你的任务：实现 SimpleContainer
public class SimpleContainer
{
    // 注册 Singleton
    public void RegisterSingleton<TInterface, TImplementation>()
        where TImplementation : TInterface;

    // 注册 Transient
    public void RegisterTransient<TInterface, TImplementation>()
        where TImplementation : TInterface;

    // 解析服务
    public T Resolve<T>();
}
```

### 练习 2：实现原子配置保存

修改以下代码，使其支持原子写入：

```csharp
public class SimpleConfigStore
{
    private readonly string _path;

    public void Save(MyConfig config)
    {
        // 你的任务：实现原子写入
        // 要求：写入临时文件后原子替换
        var json = JsonSerializer.Serialize(config);
        File.WriteAllText(_path, json);
    }
}
```

### 练习 3：实现事件去抖器

创建一个 `Debouncer` 类，在连续触发事件时只执行最后一次：

```csharp
public class Debouncer
{
    // 你的任务：实现去抖逻辑
    // 要求：在 delayMs 内没有新调用时才执行 action
    public void Debounce(Action action, int delayMs) { }
}
```

### 练习 4：实现对象池

创建一个泛型对象池：

```csharp
public class ObjectPool<T> where T : class
{
    // 你的任务：实现 Rent/Return 机制
    public T Rent() { }
    public void Return(T item) { }
}
```

### 练习 5：优化绑定性能

将以下反射绑定改为编译绑定：

```xml
<!-- 修改前 -->
<Window>
    <TextBlock Text="{Binding UserName}" />
    <TextBlock Text="{Binding UserEmail}" />
    <ListBox ItemsSource="{Binding Items}">
        <ListBox.ItemTemplate>
            <DataTemplate>
                <TextBlock Text="{Binding Name}" />
            </DataTemplate>
        </ListBox.ItemTemplate>
    </ListBox>
</Window>
```

### 练习 6：实现缓存策略

创建一个带 TTL（过期时间）的缓存：

```csharp
public class TtlCache<TKey, TValue>
{
    // 你的任务：实现带过期时间的缓存
    // 要求：GetOrAdd 方法，过期后自动重新获取
    public TValue GetOrAdd(TKey key, Func<TValue> factory) { }
}
```

### 练习 7：实现弱事件管理器

创建一个弱事件管理器，防止事件订阅导致的内存泄漏：

```csharp
public class WeakEventManager<TEventArgs>
{
    // 你的任务：使用 WeakReference 存储订阅者
    public void AddHandler(object source, EventHandler<TEventArgs> handler) { }
    public void RemoveHandler(object source, EventHandler<TEventArgs> handler) { }
}
```

### 练习 8：性能基准测试

使用 BenchmarkDotNet 测试编译绑定 vs 反射绑定的性能差异：

```csharp
[MemoryDiagnoser]
public class BindingBenchmarks
{
    // 你的任务：实现基准测试
    // 比较反射绑定和编译绑定的属性访问速度
    [Benchmark]
    public void ReflectionBinding() { }

    [Benchmark]
    public void CompiledBinding() { }
}
```

### 练习 9：实现分页数据源

创建一个支持分页的虚拟化数据源：

```csharp
public class PagedDataSource<T> : IList, INotifyCollectionChanged
{
    private readonly Func<int, int, Task<IList<T>>> _fetchPage;
    private readonly int _pageSize;

    // 你的任务：实现按需加载
    // 要求：只在访问时加载对应页的数据
    public object? this[int index]
    {
        get { /* 按需加载 */ }
    }
}
```

### 练习 10：模块化架构

将一个简单的 Avalonia 应用重构为模块化架构：

```
原始结构：
MyApp/
├── MainWindowViewModel.cs  (2000 行)
├── MainWindow.axaml
└── Services.cs

目标结构：
MyApp/
├── Modules/
│   ├── Home/
│   │   ├── HomeViewModel.cs
│   │   └── HomePage.axaml
│   ├── Settings/
│   │   ├── SettingsViewModel.cs
│   │   └── SettingsPage.axaml
│   └── About/
│       ├── AboutViewModel.cs
│       └── AboutPage.axaml
├── Shell/
│   ├── MainWindowViewModel.cs (只负责导航)
│   └── MainWindow.axaml
└── Services/
    ├── IModule.cs
    └── ModuleLoader.cs
```
