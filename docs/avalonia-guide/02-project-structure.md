# 2. 项目结构与启动流程

## 2.1 Avalonia 项目文件 (.csproj)

CodexSwitch 的项目文件展示了 Avalonia 应用的典型配置：

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>

        <!-- 编译绑定：编译时检查绑定表达式，而非运行时 -->
        <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>

        <!-- Native AOT 发布支持 -->
        <PublishAot>true</PublishAot>
        <TrimMode>full</TrimMode>

        <!-- 保留全球化支持（i18n 需要） -->
        <InvariantGlobalization>false</InvariantGlobalization>
    </PropertyGroup>

    <!-- 将 Assets 目录下所有文件作为 Avalonia 资源嵌入 -->
    <ItemGroup>
        <AvaloniaResource Include="Assets\**" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Avalonia" />
        <PackageReference Include="Avalonia.Desktop" />
        <PackageReference Include="Avalonia.Themes.Fluent" />
        <PackageReference Include="CommunityToolkit.Mvvm" />
        <PackageReference Include="Lucide.Avalonia" />
    </ItemGroup>
</Project>
```

### 关键知识点

- **`AvaloniaUseCompiledBindingsByDefault`**: 启用编译时绑定检查，在编译期就能发现绑定错误，而非运行时
- **`AvaloniaResource`**: 将文件嵌入程序集，通过 `avares://` URI 访问
- **`PublishAot` + `TrimMode=full`**: 启用 Native AOT 编译，生成无依赖的原生可执行文件
- **`InvariantGlobalization=false`**: 必须设为 false，否则 i18n 和数字格式化会出问题

### NuGet 包说明

| 包 | 作用 |
|---|---|
| `Avalonia` | 核心框架 |
| `Avalonia.Desktop` | 桌面平台支持 (Windows/macOS/Linux) |
| `Avalonia.Themes.Fluent` | Fluent 主题（可选，本项目用自定义主题） |
| `CommunityToolkit.Mvvm` | MVVM 框架，提供源代码生成器 |
| `Lucide.Avalonia` | Lucide 图标的 Avalonia 集成 |

### 项目属性详解

**OutputType**：
- `WinExe`：Windows 应用程序（无控制台窗口）
- `Exe`：控制台应用程序（有控制台窗口）
- `Library`：类库（DLL）

**TargetFramework**：
- `net10.0`：.NET 10 目标框架
- `net8.0`：.NET 8 目标框架（LTS 版本）
- `net6.0`：.NET 6 目标框架（LTS 版本，但 Avalonia 12+ 需要 .NET 8+）

**Nullable**：
- `enable`：启用可空引用类型检查
- `disable`：禁用可空引用类型检查
- `warnings`：只显示警告，不报错

**PublishAot**：
- `true`：启用 Native AOT 编译
- `false`：禁用 Native AOT 编译（默认）

**TrimMode**：
- `full`：完全裁剪未使用的代码
- `partial`：部分裁剪（更安全，但文件更大）
- `link`：链接模式（传统方式）

**InvariantGlobalization**：
- `true`：使用不变全球化（不支持本地化）
- `false`：支持本地化（i18n 必需）

## 2.2 入口点 Program.cs

```csharp
sealed class Program
{
    [STAThread]  // 必须标记为 STA 线程
    public static void Main(string[] args)
    {
        // 支持 CLI 参数：直接执行配置写入，不启动 UI
        if (StartupLaunchOptions.ShouldBootstrapClaudeConfig(args))
        {
            ClaudeBootstrapConfigWriter.TryApplyForCurrentUser();
            return;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // 这个方法也被 Avalonia 设计器使用
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()  // 自动检测平台 (Win/X11/macOS)
            .With(new FontManagerOptions
            {
                DefaultFamilyName = AppFonts.DefaultFontFamily,
                FontFallbacks =
                [
                    new FontFallback { FontFamily = new FontFamily(AppFonts.DefaultFontFamily) }
                ]
            })
            .LogToTrace();  // 将 Avalonia 日志输出到 Trace
}
```

### 知识点

1. **`[STAThread]`**: Avalonia 要求主线程是 STA（单线程公寓），这与 WPF 一致
2. **`AppBuilder.Configure<App>()`**: 泛型参数指定 Application 类型
3. **`UsePlatformDetect()`**: 自动选择当前平台的渲染后端
4. **`FontManagerOptions`**: 配置全局默认字体，包括字体回退链
5. **`LogToTrace()`**: 开发阶段调试用，将内部日志输出到调试窗口

### AppBuilder 配置详解

`AppBuilder` 是 Avalonia 的启动配置器，它提供了流畅的 API 来配置应用：

```csharp
AppBuilder.Configure<App>()
    // 平台配置
    .UsePlatformDetect()
    
    // 字体配置
    .With(new FontManagerOptions
    {
        DefaultFamilyName = "Inter",
        FontFallbacks = new[] { new FontFallback { FontFamily = new FontFamily("Arial") } }
    })
    
    // 日志配置
    .LogToTrace()
    
    // 渲染配置
    .With(new Win32PlatformOptions
    {
        RenderingMode = new[] { Win32RenderingMode.AngleEgl, Win32RenderingMode.Wgl }
    })
    
    // X11 配置（Linux）
    .With(new X11PlatformOptions
    {
        RenderingMode = new[] { X11RenderingMode.Glx, X11RenderingMode.Egl }
    })
    
    // macOS 配置
    .With(new AvaloniaNativePlatformOptions
    {
        RenderingMode = new[] { AvaloniaNativeRenderingMode.Metal, AvaloniaNativeRenderingMode.OpenGl }
    });
```

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
```

## 2.3 Application 生命周期 App.axaml.cs

```csharp
public partial class App : Application
{
    private TrayMenuController? _trayMenuController;
    private MainWindowViewModel? _viewModel;
    private MainWindow? _mainWindow;

    public override void Initialize()
    {
        // 加载 AXAML 中定义的样式和资源
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 1. 判断是否以隐藏模式启动（系统托盘）
            var startHidden = StartupLaunchOptions.ShouldStartHidden(...);

            // 2. macOS Dock 图标控制
            MacDockIconService.ConfigureForWindowVisibility(!startHidden);

            // 3. 创建 ViewModel（所有业务逻辑的入口）
            _viewModel = new MainWindowViewModel();

            // 4. 创建系统托盘控制器
            _trayMenuController = new TrayMenuController(
                this, desktop, _viewModel, ShowMainWindow, LoadTrayIcon());

            // 5. 显示主窗口（除非以隐藏模式启动）
            if (!startHidden)
                ShowMainWindow();

            // 6. 注册关闭事件
            desktop.ShutdownRequested += async (_, _) =>
            {
                _trayMenuController?.Dispose();
                if (_viewModel is not null)
                    await _viewModel.DisposeAsync();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

### 生命周期流程

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

### 关键设计决策

1. **延迟窗口创建**: `ShowMainWindow()` 按需创建窗口，而非在构造函数中创建。这支持"启动到托盘"功能
2. **ShutdownMode.OnExplicitShutdown**: 关闭窗口不会退出应用，必须显式调用 Shutdown。这是托盘应用的标准模式
3. **IAsyncDisposable**: ViewModel 实现异步释放，因为代理服务器需要异步关闭

### ApplicationLifetime 类型

Avalonia 支持多种应用生命周期：

**IClassicDesktopStyleApplicationLifetime**：
- 桌面应用的标准生命周期
- 支持多窗口
- 支持系统托盘
- 支持关闭请求拦截

**ISingleViewApplicationLifetime**：
- 单视图应用的生命周期
- 适用于移动平台和 WebAssembly
- 只有一个主视图

**IClassicDesktopStyleApplicationLifetime 的关键属性**：
- `MainWindow`：主窗口实例
- `ShutdownMode`：关闭模式（OnLastWindowClose, OnMainWindowClose, OnExplicitShutdown）
- `Args`：命令行参数
- `ExitCode`：退出代码

### 资源加载时机

AvaloniaXamlLoader.Load(this) 在 `Initialize()` 中调用，它会：

1. 解析 App.axaml 文件
2. 加载所有 StyleInclude
3. 注册样式和资源
4. 应用主题

这个过程在 `OnFrameworkInitializationCompleted()` 之前完成，确保所有资源在窗口创建前可用。

## 2.4 Avalonia 资源系统 (avares://)

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

### `avares://` vs 嵌入资源

| 方式 | URI | 访问方式 |
|------|-----|----------|
| AvaloniaResource | `avares://Assembly/Path` | `AssetLoader.Open()` |
| 嵌入资源 | `resource://Assembly/Path` | `Assembly.GetManifestResourceStream()` |

`AvaloniaResource` 是 Avalonia 推荐的方式，支持 XAML 中的直接引用。

### AvaloniaResource 工作原理

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

### 资源组织最佳实践

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

### 资源类型

Avalonia 支持多种资源类型：

**AvaloniaResource**：
- 嵌入到程序集中的文件
- 通过 `avares://` URI 访问
- 支持 XAML 中的直接引用

**ContentResource**：
- 作为内容文件复制到输出目录
- 通过文件路径访问
- 适用于需要运行时修改的文件

**None**：
- 不嵌入，不复制
- 仅作为项目文件存在
- 适用于源代码文件

### 资源缓存

Avalonia 会缓存已加载的资源，避免重复加载：

```csharp
// 第一次加载：从程序集提取
using var stream1 = AssetLoader.Open(new Uri("avares://CodexSwitch/Assets/logo.png"));

// 第二次加载：从缓存返回
using var stream2 = AssetLoader.Open(new Uri("avares://CodexSwitch/Assets/logo.png"));
```

注意：每次调用 `AssetLoader.Open()` 都会返回一个新的 Stream 实例，需要手动释放。

## 2.5 中央包管理

CodexSwitch 使用 `Directory.Packages.props` 实现中央包版本管理：

```xml
<Project>
    <PropertyGroup>
        <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    </PropertyGroup>
    <ItemGroup>
        <PackageVersion Include="Avalonia" Version="12.0.3" />
        <PackageVersion Include="Avalonia.Desktop" Version="12.0.3" />
        <!-- ... -->
    </ItemGroup>
</Project>
```

这样所有子项目引用包时不需要指定版本号，版本由中央文件统一管理。

### 为什么使用中央包管理

1. **版本一致性**：确保所有项目使用相同版本的包
2. **简化更新**：只需修改一个文件即可更新所有项目
3. **减少冲突**：避免不同项目使用不同版本的包
4. **安全审计**：更容易审查依赖项

### Directory.Build.props

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

### 项目引用

CodexSwitch 引用了两个 UI 组件库：

```xml
<ProjectReference Include="..\CodexSwitchUI\src\CodexSwitchUI\CodexSwitchUI.csproj" />
<ProjectReference Include="..\CodexSwitchUI\src\CodexSwitchUI.ECharts\CodexSwitchUI.ECharts.csproj" />
```

这些是 Git 子模块，提供了自定义控件和 ECharts 图表集成。

## 2.6 AXAML 编译流程

AXAML 文件在编译时会经历以下流程：

1. **XAML 解析**：将 AXAML 文件解析为 XAML 对象树
2. **代码生成**：生成 C# 代码（InitializeComponent 方法）
3. **编译**：将生成的代码编译为 IL
4. **AOT 编译**（可选）：将 IL 编译为原生代码

### XAML 编译器（XamlCompiler）

Avalonia 的 XAML 编译器会：

1. 解析 AXAML 文件
2. 验证命名空间和类型
3. 生成 InitializeComponent 方法
4. 处理绑定表达式（如果启用编译绑定）
5. 生成资源加载代码

### 代码生成示例

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
        
        // 编译绑定代码
        var textBlock = this.FindControl<TextBlock>("textBlock");
        textBlock.Bind(TextBlock.TextProperty, 
            new Binding("Name") { Source = DataContext });
    }
}
```

### 构建错误排查

常见构建错误及解决方案：

**错误：找不到类型**
```
Error AXAML: Cannot find type 'MyControl'
```
解决方案：检查命名空间声明和程序集引用

**错误：绑定路径无效**
```
Error AXAML: Cannot find property 'Name' on type 'MyViewModel'
```
解决方案：检查 `x:DataType` 和绑定路径

**错误：资源未找到**
```
Error AXAML: Cannot find resource 'MyBrush'
```
解决方案：检查资源定义和 `avares://` URI

---

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

### 项目模板

Avalonia 提供了多种项目模板：

**avalonia.app**：
- 基本的 Avalonia 应用
- 包含 Program.cs、App.axaml、MainWindow.axaml
- 适合快速开始

**avalonia.mvvm**：
- MVVM 架构的 Avalonia 应用
- 包含 ViewModelBase、MainWindowViewModel
- 适合企业应用

**avalonia.lib**：
- Avalonia 类库
- 可以包含自定义控件和样式
- 适合组件库

**avalonia.xplat**：
- 跨平台应用
- 包含多个平台项目
- 适合需要平台特定代码的应用

---

## Cross References

- **[第 1 章：Avalonia 概览](01-avalonia-overview.md)** — 了解 Avalonia 的整体架构
- **[第 3 章：AXAML 基础](03-axaml-fundamentals.md)** — 学习 AXAML 语法和编译
- **[第 6 章：MVVM 模式](06-mvvm-pattern.md)** — 理解 ViewModel 和服务层
- **[第 22 章：属性系统](22-property-system.md)** — 深入了解 AvaloniaProperty
- **[第 24 章：资源系统](24-resource-system.md)** — 理解资源加载和管理
- **[第 25 章：ASP.NET 集成](25-aspnet-integration.md)** — 了解 CodexSwitch 的代理服务

---

## Common Pitfalls

### 1. 忘记设置 OutputType

**问题**：创建 Avalonia 应用时忘记设置 `OutputType` 为 `WinExe`。

```xml
<!-- 错误：默认是 Exe，会显示控制台窗口 -->
<OutputType>Exe</OutputType>

<!-- 正确：WinExe 不会显示控制台窗口 -->
<OutputType>WinExe</OutputType>
```

### 2. AvaloniaResource 路径错误

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
    // ...
}
```

### 5. 忘记释放资源

**问题**：打开 AssetLoader 流后忘记释放。

```csharp
// 错误：没有释放流
var stream = AssetLoader.Open(new Uri("avares://CodexSwitch/Assets/logo.png"));
// 使用 stream...
// stream 没有被释放

// 正确：使用 using 语句
using var stream = AssetLoader.Open(new Uri("avares://CodexSwitch/Assets/logo.png"));
// 使用 stream...
// stream 会在作用域结束时自动释放
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

---

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

3. 比较模板项目与 CodexSwitch 的差异：
   - 项目配置
   - 文件结构
   - 代码组织

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

3. 尝试修改任务，输出更多信息：
   - 资源文件大小
   - 资源类型
   - 嵌入状态

4. 研究 Avalonia 的 MSBuild 任务源码：
   ```bash
   find ~/.nuget/packages/avalonia.build.tasks -name "*.targets"
   ```
