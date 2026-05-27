# 2. 项目结构与启动流程

> **写给零基础的你**：这一章会介绍一个 Avalonia 项目里都有哪些文件，每个文件是干什么的。就像你搬进一个新房子，先要知道哪个房间是卧室、哪个是厨房。

## 2.1 概述

理解 Avalonia 项目的文件结构和启动流程是开发高质量应用的基础。本章将深入讲解 `.csproj` 的每个重要属性、`Program.cs` 的启动流程、`App.axaml` 和 `App.axaml.cs` 的生命周期等核心知识点。

学完本章后，你将能够：
- 理解 Avalonia 项目里每个文件的作用
- 理解项目配置文件（.csproj）的含义
- 理解程序是怎么启动的
- 理解应用程序的生命周期（从启动到关闭的过程）

## 2.2 核心概念

### 2.2.1 项目文件 (.csproj) 详解

> **什么是 .csproj 文件？** 这是 C# 项目的"身份证"。它告诉编译器：这个项目用什么版本的 .NET、需要哪些依赖包、输出什么类型的文件等。`.csproj` 是 "C# Project" 的缩写。

> **什么是 XML？** 你看到的 `<xxx>内容</xxx>` 这种格式就是 XML。它是一种标记语言，用标签来组织信息。就像 HTML 用 `<h1>` 表示标题一样，XML 用自定义标签来描述数据。你不需要精通 XML，只需要知道 `<标签名>值</标签名>` 这个基本格式就行。

CodexSwitch 的项目文件展示了 Avalonia 应用的典型配置：

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ApplicationManifest>app.manifest</ApplicationManifest>
        <ApplicationIcon>Assets\favicon.ico</ApplicationIcon>
        <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
        <PublishAot>true</PublishAot>
        <TrimMode>full</TrimMode>
        <InvariantGlobalization>false</InvariantGlobalization>
    </PropertyGroup>
    <!-- ... -->
</Project>
```

#### OutputType 属性

`OutputType` 决定了编译输出的类型：

| 值 | 说明 | 适用场景 |
|---|------|---------|
| `WinExe` | Windows 应用程序（无控制台窗口） | 桌面 GUI 应用 |
| `Exe` | 控制台应用程序（有控制台窗口） | CLI 工具、调试阶段 |
| `Library` | 类库（DLL） | 组件库、插件 |

对于 Avalonia 桌面应用，应使用 `WinExe`。如果你在开发阶段需要查看控制台输出，可以临时改为 `Exe`。

#### TargetFramework 属性

`TargetFramework` 指定目标框架：

| 值 | 说明 | Avalonia 支持 |
|---|------|--------------|
| `net10.0` | .NET 10 | Avalonia 12+ |
| `net9.0` | .NET 9 | Avalonia 11.2+ |
| `net8.0` | .NET 8 (LTS) | Avalonia 11.0+ |
| `net6.0` | .NET 6 (LTS) | Avalonia 0.10.x |

CodexSwitch 使用 `net10.0`，因为项目使用了 .NET 10 的新特性（如 `FrameworkReference` 引用 ASP.NET Core）。

#### Nullable 属性

`Nullable` 启用可空引用类型检查：

```xml
<Nullable>enable</Nullable>   <!-- 启用，推荐 -->
<Nullable>disable</Nullable>  <!-- 禁用 -->
<Nullable>warnings</Nullable> <!-- 只警告不报错 -->
```

启用后，编译器会检查可能的空引用异常：

```csharp
string? name = null;  // 可空类型
string name = null;   // 编译警告：可能的空引用

if (name is not null)
{
    Console.WriteLine(name.Length);  // 安全访问
}
```

#### AvaloniaUseCompiledBindingsByDefault 属性

这是 Avalonia 特有的属性，启用编译绑定：

```xml
<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
```

启用后：
- 所有绑定表达式在编译时验证
- 生成强类型的绑定代码
- 支持 Native AOT 编译
- 必须在每个 AXAML 文件中设置 `x:DataType`

#### PublishAot 和 TrimMode 属性

```xml
<PublishAot>true</PublishAot>
<TrimMode>full</TrimMode>
```

- `PublishAot`：启用 Native AOT 编译，生成无依赖的原生可执行文件
- `TrimMode=full`：完全裁剪未使用的代码，减小文件体积

**注意**：AOT 编译不支持动态反射，需要使用源代码生成器（如 CommunityToolkit.Mvvm）。

#### InvariantGlobalization 属性

```xml
<InvariantGlobalization>false</InvariantGlobalization>
```

- `true`：使用不变全球化（不支持本地化）
- `false`：支持本地化（i18n 必须）

**重要**：如果应用需要支持多语言，必须设置为 `false`。Native AOT 编译时默认为 `true`，需要显式设置为 `false`。

#### ApplicationManifest 和 ApplicationIcon

```xml
<ApplicationManifest>app.manifest</ApplicationManifest>
<ApplicationIcon>Assets\favicon.ico</ApplicationIcon>
```

- `ApplicationManifest`：Windows 应用清单文件，用于 DPI 感知、权限声明等
- `ApplicationIcon`：应用程序图标，显示在任务栏和文件管理器中

### 2.2.2 AvaloniaResource 资源嵌入

```xml
<ItemGroup>
    <AvaloniaResource Include="Assets\**" />
</ItemGroup>
```

`AvaloniaResource` 将文件嵌入程序集，通过 `avares://` URI 访问：

```csharp
// 在代码中加载资源
using var stream = AssetLoader.Open(new Uri("avares://CodexSwitch/Assets/favicon.ico"));
```

```xml
<!-- 在 AXAML 中引用资源 -->
<Image Source="avares://CodexSwitch/Assets/logo.png"/>
<StyleInclude Source="avares://CodexSwitch/Styles/CodexTheme.axaml"/>
```

**AvaloniaResource vs ContentResource vs None：**

| 类型 | URI | 访问方式 | 适用场景 |
|------|-----|---------|---------|
| AvaloniaResource | `avares://Assembly/Path` | `AssetLoader.Open()` | 嵌入资源（推荐） |
| ContentResource | 文件路径 | `File.OpenRead()` | 运行时可修改的文件 |
| None | 无 | 无 | 源代码文件 |

### 2.2.3 框架引用

```xml
<ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

CodexSwitch 引用了 ASP.NET Core 框架，因为它内嵌了 Kestrel 本地代理服务器。这是 .NET 项目引用框架组件的方式。

### 2.2.4 项目引用

```xml
<ItemGroup>
    <ProjectReference Include="..\CodexSwitchUI\src\CodexSwitchUI\CodexSwitchUI.csproj" />
    <ProjectReference Include="..\CodexSwitchUI\src\CodexSwitchUI.ECharts\CodexSwitchUI.ECharts.csproj" />
</ItemGroup>
```

CodexSwitch 引用了两个 UI 组件库（Git 子模块）：
- `CodexSwitchUI`：自定义控件库（侧边栏、卡片、按钮等）
- `CodexSwitchUI.ECharts`：ECharts 图表集成

### 2.2.5 NuGet 包引用

> **小白提示**：NuGet 包就像"现成的零件"。你不需要自己造轮子，直接从 NuGet（微软的包管理平台）下载别人做好的组件来用。就像你装修房子，不需要自己造门锁，直接买现成的装上就行。

```xml
<ItemGroup>
    <PackageReference Include="Avalonia" />              <!-- 核心框架 -->
    <PackageReference Include="Avalonia.Desktop" />      <!-- 桌面平台支持 -->
    <PackageReference Include="Avalonia.Themes.Fluent" /> <!-- Fluent 主题 -->
    <PackageReference Include="AvaloniaUI.DiagnosticsSupport">  <!-- 诊断工具 -->
        <IncludeAssets Condition="'$(Configuration)' != 'Debug'">None</IncludeAssets>
        <PrivateAssets Condition="'$(Configuration)' != 'Debug'">All</PrivateAssets>
    </PackageReference>
    <PackageReference Include="CommunityToolkit.Mvvm" />  <!-- MVVM 框架 -->
    <PackageReference Include="Lucide.Avalonia" />        <!-- 图标库 -->
</ItemGroup>
```

**包说明：**

| 包 | 作用 | 类比 |
|---|------|------|
| `Avalonia` | 核心框架，包含控件、布局、绑定等 | 房子的地基和框架 |
| `Avalonia.Desktop` | 桌面平台支持（Windows/macOS/Linux） | 门和窗户（让房子能住人） |
| `Avalonia.Themes.Fluent` | Fluent 主题（可选） | 装修风格 |
| `AvaloniaUI.DiagnosticsSupport` | 诊断工具（仅 Debug 配置） | 维修工具（只在装修时用） |
| `CommunityToolkit.Mvvm` | MVVM 框架，提供源代码生成器 | 自动化工具（帮你省力） |
| `Lucide.Avalonia` | Lucide 图标的 Avalonia 集成 | 图标素材库 |

**条件包引用**：`AvaloniaUI.DiagnosticsSupport` 只在 Debug 配置下包含，Release 配置下完全排除。这避免了诊断工具影响发布版本的性能。就像装修时用的脚手架，装修完了就拆掉。

## 2.3 进阶用法

### 2.3.1 AppBuilder 配置详解

`AppBuilder` 是 Avalonia 的启动配置器，提供了流畅的 API 来配置应用：

```csharp
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new FontManagerOptions
        {
            DefaultFamilyName = AppFonts.DefaultFontFamily,
            FontFallbacks = [
                new FontFallback { FontFamily = new FontFamily(AppFonts.DefaultFontFamily) }
            ]
        })
        .LogToTrace();
```

**AppBuilder 的所有配置方法：**

```csharp
AppBuilder.Configure<App>()
    // 平台配置
    .UsePlatformDetect()  // 自动检测平台

    // 字体配置
    .With(new FontManagerOptions
    {
        DefaultFamilyName = "Inter",
        FontFallbacks = [
            new FontFallback { FontFamily = new FontFamily("Arial") }
        ]
    })

    // 日志配置
    .LogToTrace()  // 输出到 Trace
    .LogToTrace(LogEventLevel.Debug)  // 指定日志级别

    // Windows 渲染配置
    .With(new Win32PlatformOptions
    {
        RenderingMode = [
            Win32RenderingMode.AngleEgl,
            Win32RenderingMode.Wgl
        ]
    })

    // X11 配置（Linux）
    .With(new X11PlatformOptions
    {
        RenderingMode = [
            X11RenderingMode.Glx,
            X11RenderingMode.Egl
        ]
    })

    // macOS 配置
    .With(new AvaloniaNativePlatformOptions
    {
        RenderingMode = [
            AvaloniaNativeRenderingMode.Metal,
            AvaloniaNativeRenderingMode.OpenGl
        ]
    });
```

### 2.3.2 Program.cs 启动流程详解

```csharp
sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 1. CLI 模式：直接执行配置写入，不启动 UI
        if (StartupLaunchOptions.ShouldBootstrapClaudeConfig(args))
        {
            ClaudeBootstrapConfigWriter.TryApplyForCurrentUser();
            return;
        }

        // 2. GUI 模式：启动 Avalonia 应用
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new FontManagerOptions { /* ... */ })
            .LogToTrace();
}
```

**启动流程：**

```
Main(args)
    ├── CLI 模式？
    │   └── 是 → ClaudeBootstrapConfigWriter.TryApplyForCurrentUser() → return
    └── GUI 模式
        └── BuildAvaloniaApp()
            └── AppBuilder.Configure<App>()
                └── UsePlatformDetect()
                └── FontManagerOptions
                └── LogToTrace()
            └── StartWithClassicDesktopLifetime(args)
                └── App.Initialize()
                └── App.OnFrameworkInitializationCompleted()
                └── 事件循环开始
```

**`[STAThread]` 的含义**：

STA（Single-Threaded Apartment）是 Windows COM 线程模型。Avalonia 要求主线程是 STA，因为：
- UI 操作必须在单线程上执行
- 避免多线程竞争 UI 资源
- 与 Windows API 兼容

**`StartWithClassicDesktopLifetime` 的作用**：

这个方法会：
1. 创建 `IClassicDesktopStyleApplicationLifetime` 实例
2. 调用 `App.Initialize()` 加载 AXAML
3. 调用 `App.OnFrameworkInitializationCompleted()` 创建窗口
4. 进入事件循环（消息泵）
5. 等待 `Shutdown()` 被调用
6. 清理资源并退出

### 2.3.3 Application 生命周期

```csharp
public partial class App : Application
{
    private TrayMenuController? _trayMenuController;
    private MainWindowViewModel? _viewModel;
    private MainWindow? _mainWindow;

    // 阶段 1：初始化
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        // 加载 App.axaml 中定义的样式和资源
        // 此时还没有窗口，不能创建 UI 元素
    }

    // 阶段 2：框架初始化完成
    public override void OnFrameworkInitializationCompleted()
    {
        ApplyClaudeBootstrapConfig();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 判断是否以隐藏模式启动
            var startHidden = StartupLaunchOptions.ShouldStartHidden(
                Environment.GetCommandLineArgs().Skip(1));

            // macOS Dock 图标控制
            MacDockIconService.ConfigureForWindowVisibility(!startHidden);

            // 创建 ViewModel（所有业务逻辑的入口）
            _viewModel = new MainWindowViewModel();

            // 创建系统托盘控制器
            _trayMenuController = new TrayMenuController(
                this, desktop, _viewModel, ShowMainWindow, LoadTrayIcon());

            // 显示主窗口（除非以隐藏模式启动）
            if (!startHidden)
                ShowMainWindow();

            // 注册关闭事件
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
}
```

**生命周期流程：**

```
Main() → AppBuilder.Build() → App.Initialize()
    → [加载 AXAML 样式/资源]
    → App.OnFrameworkInitializationCompleted()
        → [创建 ViewModel]
        → [创建窗口并显示]
        → [事件循环开始]
    → ShutdownRequested
        → [清理资源]
        → [退出]
```

### 2.3.4 窗口管理模式

CodexSwitch 采用了"延迟窗口创建"模式：

```csharp
private void ShowMainWindow()
{
    if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
        _viewModel is null)
    {
        return;
    }

    MacDockIconService.ConfigureForWindowVisibility(true);

    var mainWindow = _mainWindow;
    if (mainWindow is null)
    {
        // 延迟创建：首次显示时才创建窗口
        mainWindow = new MainWindow
        {
            DataContext = _viewModel
        };
        mainWindow.Closed += OnMainWindowClosed;
        _mainWindow = mainWindow;
        desktop.MainWindow = mainWindow;
    }

    if (!mainWindow.IsVisible)
        mainWindow.Show();

    if (mainWindow.WindowState == WindowState.Minimized)
        mainWindow.WindowState = WindowState.Normal;

    mainWindow.Activate();
}

private void ReleaseMainWindow(MainWindow mainWindow)
{
    mainWindow.Closed -= OnMainWindowClosed;
    mainWindow.DataContext = null;

    if (ReferenceEquals(_mainWindow, mainWindow))
    {
        _mainWindow = null;
        MacDockIconService.ConfigureForWindowVisibility(false);
        RequestMemoryTrim();
    }

    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
        ReferenceEquals(desktop.MainWindow, mainWindow))
    {
        desktop.MainWindow = null;
    }
}
```

**关键设计决策：**

1. **延迟窗口创建**：`ShowMainWindow()` 按需创建窗口，而非在构造函数中创建。这支持"启动到托盘"功能。

2. **窗口关闭不退出应用**：关闭窗口只是隐藏窗口，应用继续在托盘中运行。只有调用 `Shutdown()` 才会退出。

3. **内存管理**：窗口关闭时释放 DataContext 和事件订阅，请求内存回收。

## 2.4 组件详解大全

### 2.4.1 AvaloniaXamlLoader

`AvaloniaXamlLoader` 是 Avalonia 的 XAML 加载器，负责解析 AXAML 文件并构建视觉树。

```csharp
// 基本用法
AvaloniaXamlLoader.Load(this);

// 加载指定 URI 的 AXAML
AvaloniaXamlLoader.Load(this, new Uri("avares://MyApp/Views/MainWindow.axaml"));
```

**AvaloniaXamlLoader.Load 的作用：**

1. 解析 AXAML 文件
2. 创建控件实例
3. 设置属性值
4. 注册事件处理器
5. 应用样式和资源
6. 构建视觉树

### 2.4.2 AssetLoader

`AssetLoader` 用于加载嵌入的 Avalonia 资源：

```csharp
// 加载资源流
using var stream = AssetLoader.Open(new Uri("avares://CodexSwitch/Assets/favicon.ico"));

// 检查资源是否存在
bool exists = AssetLoader.Exists(new Uri("avares://CodexSwitch/Assets/logo.png"));

// 获取资源信息
var info = AssetLoader.GetAsset(new Uri("avares://CodexSwitch/Assets/favicon.ico"));
```

**注意**：每次调用 `AssetLoader.Open()` 都会返回一个新的 Stream 实例，需要手动释放。

### 2.4.3 ApplicationLifetime

Avalonia 支持多种应用生命周期：

```csharp
// 桌面应用生命周期
if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
{
    desktop.MainWindow = mainWindow;
    desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
    desktop.ShutdownRequested += (_, _) => { /* 清理 */ };
}

// 单视图应用生命周期（移动平台、WebAssembly）
if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
{
    singleView.MainView = mainView;
}
```

**IClassicDesktopStyleApplicationLifetime 的关键属性和方法：**

| 成员 | 说明 |
|------|------|
| `MainWindow` | 主窗口实例 |
| `ShutdownMode` | 关闭模式 |
| `Args` | 命令行参数 |
| `ExitCode` | 退出代码 |
| `Shutdown(exitCode)` | 请求关闭应用 |

**ShutdownMode 枚举：**

| 值 | 说明 |
|---|------|
| `OnLastWindowClose` | 关闭最后一个窗口时退出（默认） |
| `OnMainWindowClose` | 关闭主窗口时退出 |
| `OnExplicitShutdown` | 必须显式调用 Shutdown() 才退出 |

CodexSwitch 使用 `OnExplicitShutdown`，因为关闭窗口不应退出应用（应用在托盘中继续运行）。

## 2.5 CodexSwitch 实战

### 2.5.1 中央包管理

CodexSwitch 使用 `Directory.Packages.props` 实现中央包版本管理：

```xml
<Project>
    <PropertyGroup>
        <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    </PropertyGroup>
    <ItemGroup>
        <PackageVersion Include="Avalonia" Version="12.0.3" />
        <PackageVersion Include="Avalonia.Desktop" Version="12.0.3" />
        <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.2" />
        <!-- ... -->
    </ItemGroup>
</Project>
```

**为什么使用中央包管理：**

1. **版本一致性**：确保所有项目使用相同版本的包
2. **简化更新**：只需修改一个文件即可更新所有项目
3. **减少冲突**：避免不同项目使用不同版本的包
4. **安全审计**：更容易审查依赖项

### 2.5.2 Directory.Build.props

CodexSwitch 使用 `Directory.Build.props` 来共享项目配置：

```xml
<Project>
    <PropertyGroup>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>
</Project>
```

这个文件会自动应用到所有子项目，避免重复配置。

### 2.5.3 AXAML 编译流程

AXAML 文件在编译时会经历以下流程：

```
.axaml 文件
    ↓
MSBuild 任务 (AvaloniaXamlIlTask)
    ↓
XamlX 解析器 (解析 XML → XAML AST)
    ↓
类型检查 (验证属性名、类型、绑定路径)
    ↓
代码生成 (生成 C# IL 代码)
    ↓
编译到程序集 (.dll)
```

**编译产物：**

对于以下 AXAML：
```xml
<Window x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding Name}"/>
</Window>
```

生成的代码类似：
```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
```

## 2.6 举一反三

### 2.6.1 Avalonia 资源系统 (avares://)

Avalonia 使用自定义 URI 方案访问嵌入资源：

```
avares://程序集名/路径/到/资源
```

示例：
```csharp
// 加载嵌入的图标
using var stream = AssetLoader.Open(new Uri("avares://CodexSwitch/Assets/favicon.ico"));

// 在 AXAML 中引用样式
<StyleInclude Source="avares://CodexSwitch/Styles/CodexTheme.axaml"/>

// 在 AXAML 中引用组件库样式
<StyleInclude Source="avares://CodexSwitchUI.ECharts/Themes/UsageTrendChart.axaml"/>
```

**AvaloniaResource 工作原理：**

当你在 csproj 中添加：
```xml
<AvaloniaResource Include="Assets\**" />
```

Avalonia 的 MSBuild 任务会：
1. 扫描匹配的文件
2. 将它们嵌入到程序集中
3. 生成资源清单
4. 注册 `avares://` URI 处理器

在运行时，`AssetLoader.Open()` 会：
1. 解析 `avares://` URI
2. 查找目标程序集
3. 从程序集中提取资源流
4. 返回 Stream 对象

### 2.6.2 资源组织最佳实践

```
CodexSwitch/
├── Assets/
│   ├── favicon.ico
│   ├── images/
│   │   ├── logo.png
│   │   └── icons/
│   │       ├── home.svg
│   │       └── settings.svg
│   └── i18n/
│       ├── en-US.json
│       └── zh-CN.json
├── Styles/
│   ├── CodexTheme.axaml
│   └── Components/
│       ├── Button.axaml
│       └── Input.axaml
└── Views/
    ├── MainWindow.axaml
    └── Pages/
        ├── HomePage.axaml
        └── SettingsPage.axaml
```

## 2.7 最佳实践与设计模式

### 2.7.1 项目结构最佳实践

1. **使用中央包管理**：通过 `Directory.Packages.props` 统一管理包版本
2. **共享项目配置**：通过 `Directory.Build.props` 共享通用配置
3. **启用编译绑定**：设置 `AvaloniaUseCompiledBindingsByDefault=true`
4. **启用可空检查**：设置 `Nullable=enable`
5. **合理组织资源**：按类型和功能组织 Assets 目录

### 2.7.2 启动流程最佳实践

1. **CLI 模式优先**：支持命令行参数直接执行操作，不启动 UI
2. **延迟窗口创建**：支持"启动到托盘"功能
3. **正确释放资源**：在 `ShutdownRequested` 中清理所有资源
4. **异步释放**：使用 `IAsyncDisposable` 处理需要异步关闭的资源

## Deep Dive

### Avalonia 的构建管道

Avalonia 使用 MSBuild 任务来处理 AXAML 文件：

1. **AvaloniaResourceGenerator**：处理 AvaloniaResource 项目
2. **XamlCompilerTask**：编译 AXAML 文件
3. **GenerateAvaloniaXamlTask**：生成 XAML 相关代码

这些任务在 `Avalonia.Build.Tasks` NuGet 包中定义。

### 程序集资源清单

Avalonia 会生成一个资源清单文件（`*.AvaloniaResource.manifest`），包含：

```json
{
  "avares://CodexSwitch/Assets/favicon.ico": {
    "offset": 0,
    "length": 12345
  },
  "avares://CodexSwitch/Styles/CodexTheme.axaml": {
    "offset": 12345,
    "length": 67890
  }
}
```

这个清单在运行时用于快速定位资源。

### AOT 编译与裁剪

Native AOT 编译会：

1. 将 IL 编译为原生代码
2. 裁剪未使用的代码
3. 生成独立的可执行文件

对于 Avalonia 应用，AOT 编译需要注意：

1. **反射**：AOT 不支持动态反射，需要使用源代码生成器
2. **序列化**：需要使用 AOT 兼容的序列化器
3. **动态加载**：不支持运行时加载程序集

### 线程模型

Avalonia 使用单线程 UI 模型：

- **UI 线程**：处理用户输入、布局、渲染
- **后台线程**：处理数据加载、网络请求等
- **Dispatcher**：用于在 UI 线程上执行操作

```csharp
// 在 UI 线程上执行操作
Dispatcher.UIThread.Post(() => {
    // UI 操作
});

// 在 UI 线程上执行操作（异步）
await Dispatcher.UIThread.InvokeAsync(() => {
    // UI 操作
});

// 指定优先级
Dispatcher.UIThread.Post(() => {
    // UI 操作
}, DispatcherPriority.Render);
```

## Cross References

- **[第 1 章：Avalonia 概览](01-avalonia-overview.md)** — 了解 Avalonia 的整体架构
- **[第 3 章：AXAML 基础](03-axaml-fundamentals.md)** — 学习 AXAML 语法和编译
- **[第 6 章：MVVM 模式](06-mvvm-pattern.md)** — 理解 ViewModel 和服务层
- **[第 7 章：样式与主题](07-styling-theming.md)** — 了解样式系统中的资源引用

## Common Pitfalls

### 1. 忘记设置 OutputType

**问题**：创建 Avalonia 应用时忘记设置 `OutputType` 为 `WinExe`。

```xml
<!-- 错误：默认是 Exe，会显示控制台窗口 -->
<OutputType>Exe</OutputType>

<!-- 正确：WinExe 不会显示控制台窗口 -->
<OutputType>WinExe</OutputType>
```

### 2. AvaloniaResource 路径大小写错误

**问题**：AvaloniaResource 路径大小写敏感。

```xml
<!-- 错误：路径大小写不匹配 -->
<AvaloniaResource Include="assets\**" />

<!-- 正确：路径大小写匹配 -->
<AvaloniaResource Include="Assets\**" />
```

### 3. 忘记加载 AXAML

**问题**：在 App.axaml.cs 中忘记调用 `AvaloniaXamlLoader.Load(this)`。

```csharp
// 错误：没有加载 AXAML
public override void Initialize()
{
    // 样式和资源不会被加载
}

// 正确：加载 AXAML
public override void Initialize()
{
    AvaloniaXamlLoader.Load(this);
}
```

### 4. 在错误的生命周期阶段创建窗口

**问题**：在 `Initialize()` 中创建窗口，此时资源还未加载完成。

```csharp
// 错误：在 Initialize() 中创建窗口
public override void Initialize()
{
    AvaloniaXamlLoader.Load(this);
    var window = new MainWindow();  // 资源可能还未加载
}

// 正确：在 OnFrameworkInitializationCompleted() 中创建窗口
public override void OnFrameworkInitializationCompleted()
{
    var window = new MainWindow();
}
```

### 5. 忘记释放 AssetLoader 流

**问题**：打开 AssetLoader 流后忘记释放。

```csharp
// 错误：没有释放流
var stream = AssetLoader.Open(new Uri("avares://CodexSwitch/Assets/logo.png"));

// 正确：使用 using 语句
using var stream = AssetLoader.Open(new Uri("avares://CodexSwitch/Assets/logo.png"));
```

### 6. 混淆 AvaloniaResource 和 ContentResource

**问题**：将需要嵌入的资源设置为 ContentResource。

```xml
<!-- 错误：ContentResource 不会被嵌入 -->
<ContentResource Include="Assets\logo.png" />

<!-- 正确：AvaloniaResource 会被嵌入 -->
<AvaloniaResource Include="Assets\logo.png" />
```

### 7. 忘记设置 InvariantGlobalization

**问题**：启用 Native AOT 时忘记设置 `InvariantGlobalization`。

```xml
<!-- 错误：AOT 编译时 InvariantGlobalization 默认为 true -->
<PublishAot>true</PublishAot>

<!-- 正确：显式设置为 false 以支持 i18n -->
<PublishAot>true</PublishAot>
<InvariantGlobalization>false</InvariantGlobalization>
```

### 8. 在非 UI 线程访问 Application.Current

**问题**：`Application.Current` 只能在 UI 线程访问。

```csharp
// 错误：在后台线程访问
Task.Run(() => {
    var app = Application.Current;  // 可能为 null
});

// 正确：在 UI 线程访问
Dispatcher.UIThread.Post(() => {
    var app = Application.Current;
});
```

### 9. 忘记处理 ShutdownRequested

**问题**：没有在 `ShutdownRequested` 中清理资源，导致资源泄漏。

```csharp
// 错误：没有清理资源
desktop.ShutdownRequested += (_, _) => { };

// 正确：清理所有资源
desktop.ShutdownRequested += async (_, _) =>
{
    _trayMenuController?.Dispose();
    if (_viewModel is not null)
        await _viewModel.DisposeAsync();
};
```

### 10. 使用错误的 ShutdownMode

**问题**：关闭窗口时应用意外退出。

```csharp
// 问题：默认 OnLastWindowClose，关闭窗口就退出
desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

// 解决：使用 OnExplicitShutdown，手动控制退出
desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
```

### 11. 编译绑定缺少 x:DataType

**问题**：启用了全局编译绑定但没有设置 `x:DataType`，导致绑定编译失败。

```xml
<!-- 错误：缺少 x:DataType -->
<UserControl>
    <TextBlock Text="{Binding Name}"/>  <!-- 编译错误 -->
</UserControl>

<!-- 正确：设置 x:DataType -->
<UserControl x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding Name}"/>
</UserControl>
```

### 12. 资源缓存 Stream 泄漏

**问题**：多次调用 `AssetLoader.Open()` 但没有释放之前的 Stream。

```csharp
// 错误：没有释放之前的流
for (int i = 0; i < 100; i++)
{
    var stream = AssetLoader.Open(new Uri("avares://MyApp/Assets/logo.png"));
    // 使用 stream，但没有释放
}

// 正确：使用 using 语句
for (int i = 0; i < 100; i++)
{
    using var stream = AssetLoader.Open(new Uri("avares://MyApp/Assets/logo.png"));
    // 使用 stream
}
```

## Try It Yourself

### 练习 1：分析 CodexSwitch 的 csproj

1. 打开 `CodexSwitch/CodexSwitch.csproj`

2. 识别以下配置：
   - 目标框架
   - 编译绑定设置
   - AOT 配置
   - 资源定义
   - 包引用

3. 尝试修改配置：
   - 将 `OutputType` 改为 `Exe`，运行观察控制台窗口
   - 将 `AvaloniaUseCompiledBindingsByDefault` 改为 `false`，观察绑定行为变化
   - 添加新的 `AvaloniaResource`，测试资源加载

### 练习 2：创建项目模板

1. 使用 Avalonia 模板创建新项目：
   ```bash
   dotnet new avalonia.mvvm -n MyTemplateApp
   ```

2. 分析生成的项目结构：
   ```bash
   find MyTemplateApp -type f | sort
   ```

3. 比较模板项目与 CodexSwitch 的差异

4. 尝试将模板项目修改为类似 CodexSwitch 的结构

### 练习 3：调试资源加载

1. 在 `App.axaml.cs` 的 `Initialize()` 中添加断点

2. 运行项目，观察 `AvaloniaXamlLoader.Load(this)` 的执行

3. 在调试器中查看：
   - 加载了哪些资源
   - 资源的加载顺序
   - 资源的大小

4. 尝试添加一个不存在的资源，观察错误信息

### 练习 4：测试 AOT 编译

1. 发布 CodexSwitch 为 AOT：
   ```bash
   dotnet publish CodexSwitch/CodexSwitch.csproj -c Release -r osx-arm64 --self-contained true -p:PublishAot=true
   ```

2. 观察输出目录：
   ```bash
   ls -la CodexSwitch/bin/Release/net10.0/osx-arm64/publish/
   ```

3. 运行 AOT 编译的可执行文件

4. 比较 AOT 编译与普通编译的：
   - 文件大小
   - 启动速度
   - 内存占用

### 练习 5：管理 NuGet 包

1. 打开 `Directory.Packages.props`

2. 查看所有包的版本

3. 尝试更新一个包的版本：
   ```xml
   <PackageVersion Include="Avalonia" Version="12.0.4" />
   ```

4. 运行 `dotnet restore` 观察包更新

5. 运行项目测试兼容性

### 练习 6：组织资源文件

1. 创建一个新的资源目录结构：
   ```
   Assets/
   ├── images/
   │   ├── logo.png
   │   └── icons/
   │       ├── home.svg
   │       └── settings.svg
   └── i18n/
       ├── en-US.json
       └── zh-CN.json
   ```

2. 在 csproj 中添加 AvaloniaResource：
   ```xml
   <AvaloniaResource Include="Assets\**" />
   ```

3. 在 AXAML 中引用资源：
   ```xml
   <Image Source="avares://CodexSwitch/Assets/images/logo.png"/>
   ```

4. 在代码中加载资源：
   ```csharp
   using var stream = AssetLoader.Open(new Uri("avares://CodexSwitch/Assets/images/logo.png"));
   ```

### 练习 7：配置构建管道

1. 创建一个简单的 MSBuild 任务：
   ```xml
   <Target Name="LogAvaloniaResources" BeforeTargets="Build">
       <Message Text="Avalonia Resources:" Importance="high" />
       <Message Text="@(AvaloniaResource)" Importance="high" />
   </Target>
   ```

2. 运行构建，观察输出

3. 尝试修改任务，输出更多信息

### 练习 8：实现依赖注入

> **小白提示：什么是依赖注入（DI）？**  依赖注入就像"外卖服务"。你自己不做饭（不自己创建对象），而是告诉外卖平台（DI 容器）你要什么菜（接口），平台帮你找餐厅做好送来（注入实现）。好处是：你不需要知道餐厅在哪里、怎么做菜，你只管说"我要一份宫保鸡丁"。
>
> 在代码里，ViewModel 不自己 `new` 一个服务，而是通过构造函数"告诉框架我需要什么服务"，框架自动把服务实例传进来。

1. 安装 `Microsoft.Extensions.DependencyInjection`
2. 创建一个简单的服务接口和实现
3. 在 `App.axaml.cs` 中配置 DI 容器
4. 在 `MainWindowViewModel` 中注入服务
5. 运行项目验证 DI 工作正常
