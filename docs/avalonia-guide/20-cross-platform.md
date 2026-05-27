# 20. 跨平台适配

> **写给零基础的你**：跨平台就是"一份代码，到处运行"。但不同的操作系统（Windows、Mac、Linux）有些地方不一样，比如 Mac 的菜单栏在屏幕顶部，Windows 的在窗口里面。本章教你处理这些"平台差异"。

## 20.1 概述

Avalonia 的核心优势之一是跨平台能力——一套代码可以运行在 Windows、macOS、Linux 上。然而，不同平台之间存在着显著的差异，包括文件系统路径、窗口管理行为、系统托盘、字体渲染、DPI 缩放等方面。编写真正的跨平台应用需要理解这些差异并做出适当的适配。

在 CodexSwitch 中，跨平台适配体现在：
- **窗口管理**：macOS Dock 图标隐藏/显示、Windows 任务栏行为
- **文件路径**：不同平台的配置文件存储路径
- **字体配置**：统一的字体回退链
- **Native AOT**：每个平台独立编译
- **系统托盘**：不同平台的托盘图标行为差异

本章将详细讲解 Avalonia 跨平台开发中的所有关键差异和适配策略。

## 20.2 平台检测

### 20.2.1 OperatingSystem 类

> **小白提示**：平台检测就像"看天气穿衣服"。程序运行时先检查"我现在在哪个操作系统上"，然后根据不同的系统做不同的事情。比如在 Mac 上隐藏 Dock 图标，在 Windows 上隐藏任务栏图标。

.NET 提供了 `OperatingSystem` 静态类来检测当前运行的操作系统：

```csharp
// 操作系统检测
if (OperatingSystem.IsWindows())     { /* Windows */ }
if (OperatingSystem.IsMacOS())       { /* macOS */ }
if (OperatingSystem.IsLinux())       { /* Linux */ }
if (OperatingSystem.IsIOS())         { /* iOS */ }
if (OperatingSystem.IsAndroid())     { /* Android */ }
if (OperatingSystem.IsBrowser())     { /* WebAssembly */ }

// 版本检测
if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
{
    // Windows 10 2004 或更高版本
}

if (OperatingSystem.IsMacOSVersionAtLeast(13, 0))
{
    // macOS Ventura 或更高版本
}

if (OperatingSystem.IsLinux() && OperatingSystem.IsVersionAtLeast(5, 0))
{
    // Linux 内核 5.0+
}
```

### 20.2.2 RuntimeInformation 类

```csharp
using System.Runtime.InteropServices;

// 运行时架构
var arch = RuntimeInformation.ProcessArchitecture;
// Architecture.X64, Architecture.Arm64, etc.

// OS 描述
var osDescription = RuntimeInformation.OSDescription;
// "Darwin 23.0.0" (macOS), "Microsoft Windows 10.0.19045" (Windows)

// 运行时标识符
var rid = RuntimeInformation.RuntimeIdentifier;
// "osx-arm64", "win-x64", "linux-x64"

// 是否为 64 位进程
bool is64Bit = Environment.Is64BitProcess;
```

### 20.2.3 平台检测的时机

```csharp
// 在 Program.cs 中尽早检测
public static void Main(string[] args)
{
    // 启动时的平台特定逻辑
    if (OperatingSystem.IsMacOS())
    {
        // macOS 特定的初始化
    }

    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}

// 在 App.axaml.cs 中检测
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (OperatingSystem.IsMacOS())
        {
            // macOS 特定的窗口配置
        }
    }
}

// 在运行时动态检测
public void OnAction()
{
    if (OperatingSystem.IsWindows())
    {
        // Windows 特定行为
    }
    else if (OperatingSystem.IsMacOS())
    {
        // macOS 特定行为
    }
}
```

## 20.3 文件路径差异

### 20.3.1 各平台的标准路径

```csharp
// 应用数据目录
var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
// Windows: C:\Users\{user}\AppData\Roaming
// macOS:   /Users/{user}/.config  (或 ~/Library/Application Support)
// Linux:   /home/{user}/.config

// 本地应用数据
var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
// Windows: C:\Users\{user}\AppData\Local
// macOS:   /Users/{user}/.local/share
// Linux:   /home/{user}/.local/share

// 用户主目录
var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
// Windows: C:\Users\{user}
// macOS:   /Users/{user}
// Linux:   /home/{user}

// 程序文件目录
var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
// Windows: C:\Program Files
// macOS:   /Applications
// Linux:   /usr (通常)
```

### 20.3.2 CodexSwitch 的 AppPaths 实现

```csharp
public static class AppPaths
{
    // 应用数据根目录
    public static string AppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CodexSwitch");

    // 配置文件
    public static string ConfigFilePath { get; } = Path.Combine(
        AppDataDirectory, "config.json");

    // 使用量日志目录
    public static string UsageLogDirectory { get; } = Path.Combine(
        AppDataDirectory, "usage");

    // 定价目录
    public static string PricingCatalogPath { get; } = Path.Combine(
        AppDataDirectory, "pricing.json");

    // 静态构造函数：确保目录存在
    static AppPaths()
    {
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(UsageLogDirectory);
    }
}
```

### 20.3.3 路径处理最佳实践

```csharp
// 好的做法：使用 Path.Combine
var path = Path.Combine("folder", "subfolder", "file.txt");
// 自动使用平台的路径分隔符

// 好的做法：使用 Path.Join (.NET Core 3.0+)
var path = Path.Join("folder", "subfolder", "file.txt");

// 不好的做法：硬编码路径分隔符
var path = "folder\\subfolder\\file.txt";  // 只在 Windows 工作
var path = "folder/subfolder/file.txt";    // 在大多数平台工作，但不是最佳

// 不好的做法：字符串拼接
var path = baseDir + "/" + fileName;  // 不处理根路径
```

### 20.3.4 文件名大小写差异

```csharp
// Windows 和 macOS 默认不区分大小写
// Linux 区分大小写

// 不好的做法：
File.Exists("Config.json")  // Linux 上找不到 "config.json"

// 好的做法：始终使用一致的大小写
File.Exists("config.json")

// 跨平台的文件查找
public static string? FindFileCaseInsensitive(string directory, string fileName)
{
    if (!OperatingSystem.IsLinux())
    {
        // 在 Windows 和 macOS 上直接使用
        var path = Path.Combine(directory, fileName);
        return File.Exists(path) ? path : null;
    }

    // Linux 上需要手动查找
    var files = Directory.GetFiles(directory);
    return files.FirstOrDefault(f =>
        string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));
}
```

### 20.3.5 路径长度限制

```csharp
// Windows 传统限制：260 字符（MAX_PATH）
// Windows 长路径支持：32,767 字符（需要 \\?\ 前缀或注册表启用）
// macOS：1024 字符
// Linux：4096 字符

// 安全的路径长度检查
public static bool IsPathTooLong(string path)
{
    if (OperatingSystem.IsWindows())
        return path.Length > 250; // 留一些余量
    if (OperatingSystem.IsMacOS())
        return path.Length > 1000;
    return path.Length > 4000; // Linux
}
```

## 20.4 窗口管理差异

### 20.4.1 macOS Dock 图标控制

CodexSwitch 使用 `MacDockIconService` 控制 macOS Dock 图标的可见性：

```csharp
public static class MacDockIconService
{
    public static void ConfigureForWindowVisibility(bool visible)
    {
        if (!OperatingSystem.IsMacOS()) return;

        // 使用 NSApplication API 控制 Dock 图标
        // visible=true:  显示 Dock 图标（应用在前台）
        // visible=false: 隐藏 Dock 图标（应用在后台/最小化）
        // 这允许应用在后台运行但不在 Dock 中显示
    }
}
```

在 CodexSwitch 的 `App.axaml.cs` 中使用：

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var startHidden = StartupLaunchOptions.ShouldStartHidden(args);
        MacDockIconService.ConfigureForWindowVisibility(!startHidden);
        // ...
    }
}

private void ShowMainWindow()
{
    MacDockIconService.ConfigureForWindowVisibility(true);
    // 显示窗口
}

private void ReleaseMainWindow(MainWindow mainWindow)
{
    MacDockIconService.ConfigureForWindowVisibility(false);
    // 隐藏窗口但不退出
}
```

### 20.4.2 Windows 任务栏

```csharp
// Windows 任务栏进度条
if (OperatingSystem.IsWindows())
{
    // 使用 ITaskbarList3 接口
    // 可以在任务栏图标上显示进度条
    // Avalonia 暂未直接提供此 API
}

// Windows 任务栏闪烁
if (OperatingSystem.IsWindows())
{
    // FlashWindowEx API 可以让任务栏图标闪烁
    // 用于通知用户注意
}
```

### 20.4.3 Linux 系统托盘

```csharp
// Linux 系统托盘的行为取决于桌面环境
// GNOME: 使用 AppIndicator 或 SNI
// KDE: 使用 SNI (StatusNotifierItem)
// XFCE: 使用旧版 System Tray

// Avalonia 的 TrayIcon 在不同 Linux 桌面环境中的兼容性不同
var trayIcon = new TrayIcon
{
    Icon = new WindowIcon("Assets/tray-icon.png"),
    ToolTipText = "CodexSwitch",
    IsVisible = true
};
```

### 20.4.4 系统托盘的跨平台实现

```csharp
// CodexSwitch 的 TrayMenuController
public class TrayMenuController : IDisposable
{
    private readonly TrayIcon _trayIcon;

    public TrayMenuController(
        Application app,
        IClassicDesktopStyleApplicationLifetime desktop,
        MainWindowViewModel viewModel,
        Action showMainWindow)
    {
        _trayIcon = new TrayIcon
        {
            Icon = LoadTrayIcon(),
            ToolTipText = "CodexSwitch",
            IsVisible = true
        };

        // 构建托盘菜单
        var menu = new NativeMenu();

        var showItem = new NativeMenuItem("Show");
        showItem.Click += (_, _) => showMainWindow();
        menu.Add(showItem);

        var separator = new NativeMenuItemSeparator();
        menu.Add(separator);

        var quitItem = new NativeMenuItem("Quit");
        quitItem.Click += (_, _) => desktop.Shutdown();
        menu.Add(quitItem);

        _trayIcon.Menu = menu;
        _trayIcon.Clicked += (_, _) => showMainWindow();

        // 注册到应用
        TrayIcon.SetIcons(app, new[] { _trayIcon });
    }

    public void Dispose()
    {
        _trayIcon.IsVisible = false;
        _trayIcon.Dispose();
    }
}
```

### 20.4.5 窗口属性差异

```csharp
// macOS 窗口圆角
if (OperatingSystem.IsMacOS())
{
    // macOS 窗口默认有圆角
    // Avalonia 自动处理
}

// Windows 窗口边框
if (OperatingSystem.IsWindows())
{
    // Windows 11 有圆角窗口
    // Windows 10 是直角窗口
}

// Linux 窗口装饰
if (OperatingSystem.IsLinux())
{
    // 窗口装饰取决于窗口管理器
    // GNOME: CSD (Client-Side Decoration)
    // KDE: SSD (Server-Side Decoration)
}
```

## 20.5 字体差异与回退

### 20.5.1 字体配置

```csharp
// CodexSwitch 的字体配置
public static class AppFonts
{
    public const string DefaultFontFamily = "Inter, -apple-system, BlinkMacSystemFont, " +
        "Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif";
}

// 在 Program.cs 中配置
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new FontManagerOptions
        {
            DefaultFamilyName = AppFonts.DefaultFontFamily,
            FontFallbacks =
            [
                new FontFallback { FontFamily = new FontFamily(AppFonts.DefaultFontFamily) }
            ]
        })
        .LogToTrace();
```

### 20.5.2 各平台的默认字体

| 平台 | 系统字体 | 等宽字体 |
|------|---------|---------|
| Windows | Segoe UI | Cascadia Code, Consolas |
| macOS | -apple-system (San Francisco) | SF Mono, Menlo |
| Linux | varies (DejaVu Sans, Noto Sans) | DejaVu Sans Mono |

### 20.5.3 字体回退链

```csharp
// Avalonia 的字体回退机制：
// 1. 尝试使用指定的字体
// 2. 如果不存在，使用 FontFallbacks 中的第一个
// 3. 如果仍不存在，使用平台默认字体
// 4. 如果字符不在当前字体中，使用系统字体回退

// 在 XAML 中指定字体
<TextBlock FontFamily="Segoe UI, -apple-system, sans-serif"/>

// 使用系统字体
<TextBlock FontFamily="{x:Null}"/>  // 使用默认字体
```

### 20.5.4 字体大小差异

```xml
<!-- 不同平台的字体渲染可能有不同的视觉大小 -->
<!-- macOS 的字体通常看起来比 Windows 大 -->
<!-- 使用相对单位可以减少差异 -->

<!-- 好的做法：使用 ThemeResource 定义统一的字体大小 -->
<Style Selector="TextBlock.body">
    <Setter Property="FontSize" Value="14"/>
</Style>
<Style Selector="TextBlock.caption">
    <Setter Property="FontSize" Value="12"/>
</Style>
```

## 20.6 DPI 与缩放

### 20.6.1 DPI 基础

```csharp
// 获取当前 DPI 缩放比例
var scaling = this.RenderScaling; // Window 的属性
// Windows: 1.0 (100%), 1.25 (125%), 1.5 (150%), 2.0 (200%)
// macOS: 2.0 (Retina), 1.0 (非 Retina)
// Linux: 通常 1.0，HiDPI 配置下可能更高

// 获取屏幕信息
var screens = this.Screens;
var primary = screens.Primary;
var dpi = primary?.Scaling ?? 1.0;
```

### 20.6.2 处理高 DPI

```csharp
// Avalonia 自动处理 DPI 缩放
// 但某些场景需要手动处理：

// 1. 自定义渲染
protected override void Render(DrawingContext context)
{
    var scaling = VisualRoot?.RenderScaling ?? 1.0;
    // 在自定义渲染中考虑缩放
}

// 2. 图片资源
// 提供多种分辨率的图片
// 1x: icon.png (24x24)
// 2x: icon@2x.png (48x48)
```

### 20.6.3 macOS Retina 显示

```csharp
// macOS 的 Retina 显示有 2x 缩放
// Avalonia 自动处理，但需要注意：

// 1. 像素完美的渲染
// 使用整数坐标，避免子像素渲染
Canvas.SetLeft(rect, 100);  // 好
Canvas.SetLeft(rect, 100.5); // 可能模糊

// 2. 图片资源
// 提供 @2x 版本的图片
<Image Source="/Assets/icon.png"/>        <!-- 1x -->
<!-- Avalonia 会自动查找 icon@2x.png -->
```

### 20.6.4 Windows 显示缩放

```csharp
// Windows 10/11 支持每显示器 DPI 缩放
// 当窗口在不同 DPI 的显示器间移动时

// 监听 DPI 变化
protected override void OnDpiChanged(RoutedEventArgs e)
{
    // 窗口移动到不同 DPI 的显示器时触发
    // 可以在这里调整布局
}

// 确保自定义控件正确响应 DPI
public class CustomControl : Control
{
    protected override void Render(DrawingContext context)
    {
        var scaling = VisualRoot?.RenderScaling ?? 1.0;

        // 使用物理像素而非逻辑像素
        var physicalWidth = Bounds.Width * scaling;
        var physicalHeight = Bounds.Height * scaling;
    }
}
```

## 20.7 平台特定代码

### 20.7.1 条件编译

```csharp
// 使用预处理器指令
#if WINDOWS
    // Windows 特定代码
    WindowsSpecificMethod();
#elif MACOS
    // macOS 特定代码
    MacSpecificMethod();
#elif LINUX
    // Linux 特定代码
    LinuxSpecificMethod();
#endif
```

```xml
<!-- 在 .csproj 中定义条件编译符号 -->
<PropertyGroup Condition="$([MSBuild]::IsOSPlatform('Windows'))">
    <DefineConstants>$(DefineConstants);WINDOWS</DefineConstants>
</PropertyGroup>
<PropertyGroup Condition="$([MSBuild]::IsOSPlatform('OSX'))">
    <DefineConstants>$(DefineConstants);MACOS</DefineConstants>
</PropertyGroup>
<PropertyGroup Condition="$([MSBuild]::IsOSPlatform('Linux'))">
    <DefineConstants>$(DefineConstants);LINUX</DefineConstants>
</PropertyGroup>
```

### 20.7.2 运行时平台检测（推荐）

```csharp
// 推荐使用运行时检测而非条件编译
// 因为条件编译需要为每个平台单独编译

public static class PlatformHelper
{
    public static string GetConfigDirectory()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CodexSwitch");

        if (OperatingSystem.IsMacOS())
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "CodexSwitch");

        // Linux
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "codexswitch");
    }

    public static void OpenUrl(string url)
    {
        if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        else if (OperatingSystem.IsMacOS())
            Process.Start("open", url);
        else if (OperatingSystem.IsLinux())
            Process.Start("xdg-open", url);
    }

    public static void OpenFolder(string path)
    {
        if (OperatingSystem.IsWindows())
            Process.Start("explorer.exe", path);
        else if (OperatingSystem.IsMacOS())
            Process.Start("open", path);
        else if (OperatingSystem.IsLinux())
            Process.Start("xdg-open", path);
    }

    public static void RevealFileInExplorer(string filePath)
    {
        if (OperatingSystem.IsWindows())
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        else if (OperatingSystem.IsMacOS())
            Process.Start("open", $"-R \"{filePath}\"");
        else if (OperatingSystem.IsLinux())
        {
            var dir = Path.GetDirectoryName(filePath);
            Process.Start("xdg-open", dir);
        }
    }
}
```

### 20.7.3 平台特定服务注册

```csharp
// 使用依赖注入注册平台特定实现
public interface IStartupRegistration
{
    bool IsRegistered();
    void Register();
    void Unregister();
}

public static class PlatformServices
{
    public static void RegisterPlatformServices(IServiceCollection services)
    {
        if (OperatingSystem.IsWindows())
            services.AddSingleton<IStartupRegistration, WindowsStartupRegistration>();
        else if (OperatingSystem.IsMacOS())
            services.AddSingleton<IStartupRegistration, MacStartupRegistration>();
        else if (OperatingSystem.IsLinux())
            services.AddSingleton<IStartupRegistration, LinuxStartupRegistration>();
    }
}

// Windows 启动注册
public class WindowsStartupRegistration : IStartupRegistration
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "CodexSwitch";

    public bool IsRegistered()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(AppName) != null;
    }

    public void Register()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.SetValue(AppName, $"\"{Environment.ProcessPath}\" --start-hidden");
    }

    public void Unregister()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.DeleteValue(AppName, false);
    }
}

// macOS 启动注册
public class MacStartupRegistration : IStartupRegistration
{
    // macOS 使用 Login Items
    // 可以通过 SMLoginItemSetEnabled 或 Launch Agent plist 实现
    // ...
}

// Linux 启动注册
public class LinuxStartupRegistration : IStartupRegistration
{
    // Linux 使用 ~/.config/autostart/ 下的 .desktop 文件
    // ...
}
```

## 20.8 Native AOT 跨平台

### 20.8.1 各平台发布命令

```bash
# Windows x64
dotnet publish -r win-x64 --self-contained true -p:PublishAot=true

# Windows ARM64
dotnet publish -r win-arm64 --self-contained true -p:PublishAot=true

# macOS Intel
dotnet publish -r osx-x64 --self-contained true -p:PublishAot=true

# macOS Apple Silicon (M1/M2/M3)
dotnet publish -r osx-arm64 --self-contained true -p:PublishAot=true

# macOS 通用二进制（需要分别编译后合并）
# dotnet publish -r osx-x64 ...
# dotnet publish -r osx-arm64 ...
# lipo -create osx-x64/binary osx-arm64/binary -output osx-universal/binary

# Linux x64
dotnet publish -r linux-x64 --self-contained true -p:PublishAot=true

# Linux ARM64
dotnet publish -r linux-arm64 --self-contained true -p:PublishAot=true
```

### 20.8.2 Runtime Identifier (RID)

```
{os}-{arch}

os:   win, linux, osx, ios, android, browser, tvos, watchos, freebsd
arch: x86, x64, arm, arm64, s390x, ppc64le, loongarch64, riscv64

常用 RID：
win-x64        Windows 64-bit Intel/AMD
win-arm64      Windows 64-bit ARM (Surface Pro X 等)
osx-x64        macOS Intel
osx-arm64      macOS Apple Silicon
linux-x64      Linux 64-bit Intel/AMD
linux-arm64    Linux 64-bit ARM (Raspberry Pi 4 等)
```

### 20.8.3 AOT 的平台差异

```csharp
// AOT 编译的注意事项：

// 1. 反射限制
// AOT 不支持完整的反射，某些动态特性可能不工作
// CommunityToolkit.Mvvm 的源代码生成器避免了反射

// 2. 动态加载程序集
// AOT 不支持 Assembly.LoadFrom()
// 需要使用插件模型或预编译

// 3. 序列化
// System.Text.Json 需要源代码生成器
[JsonSerializable(typeof(AppConfig))]
internal partial class AppJsonContext : JsonSerializerContext { }

// 4. 体积差异
// 不同平台的 AOT 产物大小不同
// win-x64: 通常较大
// linux-x64: 中等
// osx-arm64: 较小
```

### 20.8.4 CodexSwitch 的 CI/CD 发布

```yaml
# .github/workflows/release.yml
jobs:
  publish:
    strategy:
      matrix:
        include:
          - rid: win-x64
            os: windows-latest
          - rid: win-arm64
            os: windows-latest
          - rid: osx-x64
            os: macos-latest
          - rid: osx-arm64
            os: macos-latest
          - rid: linux-x64
            os: ubuntu-latest

    runs-on: ${{ matrix.os }}

    steps:
      - uses: actions/checkout@v4

      - name: Publish
        run: >
          dotnet publish CodexSwitch/CodexSwitch.csproj
          -c Release
          -r ${{ matrix.rid }}
          --self-contained true
          -p:PublishAot=true

      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: codexswitch-${{ matrix.rid }}
          path: CodexSwitch/bin/Release/net10.0/${{ matrix.rid }}/publish/
```

## 20.9 环境变量差异

### 20.9.1 各平台的环境变量

```csharp
// 获取环境变量
var home = Environment.GetEnvironmentVariable("HOME");    // macOS/Linux
var home = Environment.GetEnvironmentVariable("USERPROFILE"); // Windows

// 跨平台获取用户主目录
var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

// PATH 变量
var path = Environment.GetEnvironmentVariable("PATH");
// Windows: 分号分隔 (;)
// macOS/Linux: 冒号分隔 (:)

// 临时目录
var temp = Path.GetTempDirectory();
// Windows: C:\Users\{user}\AppData\Local\Temp
// macOS: /var/folders/xx/xxx/T/
// Linux: /tmp
```

### 20.9.2 环境变量最佳实践

```csharp
// 好的做法：使用 Environment.GetFolderPath
var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

// 不好的做法：硬编码路径
var appData = @"C:\Users\{user}\AppData\Roaming"; // Windows only

// 好的做法：使用 Path.Combine
var configPath = Path.Combine(appData, "CodexSwitch", "config.json");

// 不好的做法：字符串拼接
var configPath = appData + "/CodexSwitch/config.json"; // 不处理根路径
```

## 20.10 进程与命令行差异

### 20.10.1 启动外部进程

```csharp
public static class ProcessHelper
{
    public static void OpenUrl(string url)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", url);
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", url);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }

    public static void OpenTerminal(string? workingDirectory = null)
    {
        if (OperatingSystem.IsWindows())
        {
            var psi = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = true,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
            };
            Process.Start(psi);
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process.Start("open", "-a Terminal");
        }
        else if (OperatingSystem.IsLinux())
        {
            // 尝试常见的终端模拟器
            var terminals = new[] { "gnome-terminal", "konsole", "xfce4-terminal", "xterm" };
            foreach (var terminal in terminals)
            {
                try
                {
                    Process.Start(terminal);
                    return;
                }
                catch { }
            }
        }
    }
}
```

### 20.10.2 命令行参数差异

```csharp
// Windows: 使用 / 或 - 作为前缀
// myapp.exe /start-hidden
// myapp.exe --start-hidden

// macOS/Linux: 使用 - 或 -- 作为前缀
// ./myapp --start-hidden
// ./myapp -h

// CodexSwitch 统一使用 -- 前缀
public static class StartupLaunchOptions
{
    public static bool ShouldStartHidden(IEnumerable<string> args)
    {
        return args.Any(a =>
            string.Equals(a, "--start-hidden", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase));
    }
}
```

## 20.11 剪贴板与拖放差异

### 20.11.1 剪贴板

```csharp
// Avalonia 的剪贴板 API 是跨平台的
var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

// 复制
await clipboard.SetTextAsync("Hello");

// 粘贴
var text = await clipboard.GetTextAsync();

// 检查是否有文本
var formats = await clipboard.GetFormatsAsync();
var hasText = formats.Contains("text/plain");
```

### 20.11.2 文件拖放

```csharp
// 文件拖放在不同平台上的行为：
// Windows: 支持完整的 OLE 拖放
// macOS: 支持 Cocoa 拖放
// Linux: 支持 XDND 协议

// Avalonia 统一了拖放 API，但有些细微差异：
// 1. 拖放图标可能不同
// 2. 拖放效果的视觉反馈可能不同
// 3. 某些 Linux 桌面环境可能不完全支持
```

## 20.12 系统主题检测

### 20.12.1 检测系统主题

```csharp
// 检测系统是否使用深色模式
var themeVariant = Application.Current?.ActualThemeVariant;
bool isDark = themeVariant == ThemeVariant.Dark;

// 监听主题变化
Application.Current?.ActualThemeVariantChanged += (s, e) =>
{
    var isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
    UpdateTheme(isDark);
};
```

### 20.12.2 各平台的主题检测

```csharp
// 平台特定的主题检测
public static bool IsSystemDarkMode()
{
    if (OperatingSystem.IsWindows())
    {
        // 读取注册表
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        var value = key?.GetValue("AppsUseLightTheme");
        return value is int v && v == 0;
    }

    if (OperatingSystem.IsMacOS())
    {
        // 使用 NSUserDefaults
        // defaults read -g AppleInterfaceStyle -> "Dark"
        try
        {
            var process = Process.Start(new ProcessStartInfo("defaults")
            {
                Arguments = "read -g AppleInterfaceStyle",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            var output = process?.StandardOutput.ReadToEnd();
            return output?.Trim() == "Dark";
        }
        catch { return false; }
    }

    if (OperatingSystem.IsLinux())
    {
        // GNOME: gsettings get org.gnome.desktop.interface color-scheme
        // KDE: 检查 kdeglobals
        return false; // 默认浅色
    }

    return false;
}
```

## 20.13 CodexSwitch 实战

### 20.13.1 Program.cs 中的跨平台配置

```csharp
sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 启动引导模式：非 GUI 操作
        if (StartupLaunchOptions.ShouldBootstrapClaudeConfig(args))
        {
            ClaudeBootstrapConfigWriter.TryApplyForCurrentUser();
            return;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()  // 自动检测平台
            .With(new FontManagerOptions
            {
                DefaultFamilyName = AppFonts.DefaultFontFamily,
                FontFallbacks =
                [
                    new FontFallback
                    {
                        FontFamily = new FontFamily(AppFonts.DefaultFontFamily)
                    }
                ]
            })
            .LogToTrace();
}
```

### 20.13.2 App.axaml.cs 中的平台适配

```csharp
public override void OnFrameworkInitializationCompleted()
{
    // 应用引导配置
    ApplyClaudeBootstrapConfig();

    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        // 检测是否应该隐藏启动
        var startHidden = StartupLaunchOptions.ShouldStartHidden(
            Environment.GetCommandLineArgs().Skip(1));

        // macOS Dock 图标控制
        MacDockIconService.ConfigureForWindowVisibility(!startHidden);

        // 创建 ViewModel
        _viewModel = new MainWindowViewModel();

        // 创建系统托盘
        _trayMenuController = new TrayMenuController(
            this, desktop, _viewModel, ShowMainWindow, LoadTrayIcon());

        // 显示主窗口
        if (!startHidden)
            ShowMainWindow();

        // 注册关闭处理
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

### 20.13.3 窗口生命周期管理

```csharp
private void ShowMainWindow()
{
    // macOS: 显示 Dock 图标
    MacDockIconService.ConfigureForWindowVisibility(true);

    var mainWindow = _mainWindow;
    if (mainWindow is null)
    {
        mainWindow = new MainWindow { DataContext = _viewModel };
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
        // macOS: 隐藏 Dock 图标
        MacDockIconService.ConfigureForWindowVisibility(false);
        // 释放内存
        RequestMemoryTrim();
    }
}

private static void RequestMemoryTrim()
{
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced,
        blocking: false, compacting: true);
}
```

## 20.14 最佳实践

### 跨平台开发原则

1. **优先使用运行时检测**：`OperatingSystem.IsXxx()` 比条件编译更灵活
2. **使用标准路径**：`Environment.GetFolderPath` 和 `Path.Combine`
3. **统一 UI 语言**：使用国际化（i18n）而非硬编码文本
4. **测试所有平台**：在 Windows、macOS、Linux 上都进行测试
5. **处理平台异常**：某些 API 在特定平台可能抛出异常

### 常见跨平台陷阱

```
路径分隔符    → Path.Combine
文件名大小写  → 统一使用小写
换行符       → Environment.NewLine
编码         → UTF-8（无 BOM）
行尾         → 自动处理
```

### 平台特定 UI 策略

```csharp
// 策略一：统一 UI，接受差异
// 不做平台特定调整，接受不同平台上的细微差异

// 策略二：平台特定样式
if (OperatingSystem.IsMacOS())
{
    Application.Current.Resources["WindowCornerRadius"] = new CornerRadius(10);
    Application.Current.Resources["ControlHeight"] = 28.0;
}
else if (OperatingSystem.IsWindows())
{
    Application.Current.Resources["WindowCornerRadius"] = new CornerRadius(4);
    Application.Current.Resources["ControlHeight"] = 32.0;
}

// 策略三：平台特定控件
if (OperatingSystem.IsMacOS())
{
    // 使用 macOS 原生菜单栏
}
else
{
    // 使用窗口内菜单栏
}
```

## 20.15 Deep Dive：Avalonia 的平台抽象层

### 20.15.1 架构概览

```
Application Code
    ↓
Avalonia Core (平台无关)
    ├── 动画系统
    ├── 数据绑定
    ├── 布局系统
    ├── 样式系统
    └── 命令系统
    ↓
Platform Backend (平台特定)
    ├── Win32 Backend
    │   ├── 窗口管理 (HWND)
    │   ├── 输入处理 (Win32 Message Loop)
    │   ├── 渲染 (DirectX/ANGLE/Skia)
    │   └── 系统集成 (注册表、COM)
    ├── X11 Backend
    │   ├── 窗口管理 (X Window)
    │   ├── 输入处理 (XInput2)
    │   ├── 渲染 (Skia)
    │   └── 系统集成 (D-Bus)
    ├── macOS Backend
    │   ├── 窗口管理 (NSWindow)
    │   ├── 输入处理 (NSEvent)
    │   ├── 渲染 (Metal/Skia)
    │   └── 系统集成 (NSApplication)
    ├── iOS Backend
    ├── Android Backend
    └── WebAssembly Backend
```

### 20.15.2 平台后端选择

```csharp
// Avalonia 通过 UsePlatformDetect() 自动选择后端
AppBuilder.Configure<App>()
    .UsePlatformDetect()

// 也可以手动指定
AppBuilder.Configure<App>()
    .UseWin32()      // Windows
    .UseX11()        // Linux/X11
    .UseAvaloniaNative() // macOS
    .UseSkia()       // 渲染后端
```

### 20.15.3 渲染后端

```csharp
// Avalonia 支持多种渲染后端：

// Skia（默认，跨平台）
// - 软件渲染
// - GPU 加速（通过 ANGLE 或原生 OpenGL）

// Direct2D（Windows）
// - Windows 10+ 原生
// - 高性能

// Metal（macOS）
// - macOS 原生 GPU API
// - 高性能

// 配置渲染后端
AppBuilder.Configure<App>()
    .With(new Win32PlatformOptions
    {
        RenderingMode = new[] { Win32RenderingMode.Wgl, Win32RenderingMode.Software },
        AllowEglInitialization = true
    })
    .With(new X11PlatformOptions
    {
        RenderingMode = new[] { X11RenderingMode.Glx, X11RenderingMode.Software },
        UseDBusMenu = true
    })
    .With(new AvaloniaNativePlatformOptions
    {
        RenderingMode = new[] { AvaloniaNativeRenderingMode.Metal, AvaloniaNativeRenderingMode.Software }
    });
```

## 20.16 Cross References

- [第 2 章 项目结构与启动流程](02-project-structure.md) -- 项目配置和平台检测
- [第 15 章 编译绑定与 AOT 发布](15-compiled-bindings.md) -- AOT 发布命令
- [第 21 章 调试与诊断](21-debugging.md) -- 跨平台调试
- [第 7 章 样式与主题系统](07-styling-theming.md) -- 系统主题检测
- [第 16 章 输入处理与事件系统](16-input-events.md) -- 平台特定的键盘修饰键

## 20.17 Common Pitfalls

1. **硬编码路径**：使用 `Path.Combine` 而非字符串拼接。`@"C:\Users\..."` 只在 Windows 工作。

2. **忽略文件名大小写差异**：Linux 文件名大小写敏感，Windows 和 macOS 默认不敏感。始终使用一致的大小写。

3. **假设特定 API 存在**：某些 API（如 Windows 注册表）只在特定平台可用。使用 `OperatingSystem.IsXxx()` 检查。

4. **忽略 DPI 差异**：macOS Retina 显示有 2x 缩放，Windows 可能有 125%/150% 缩放。自定义渲染时需要考虑。

5. **字体不可用**：指定的字体在某些平台可能不存在。提供字体回退链。

6. **换行符差异**：Windows 使用 `\r\n`，Unix 使用 `\n`。使用 `Environment.NewLine` 或在写入时统一。

7. **Native AOT 的反射限制**：AOT 不支持完整反射。使用源代码生成器（如 CommunityToolkit.Mvvm 的 `[ObservableProperty]`）。

8. **系统托盘行为差异**：不同 Linux 桌面环境的系统托盘实现不同。某些环境可能不支持或行为异常。

9. **路径长度限制**：Windows 传统限制 260 字符。长路径需要特殊处理或注册表启用。

10. **编码差异**：某些系统默认编码不是 UTF-8。始终显式指定编码 `new UTF8Encoding(false)`。

## 20.18 Try It Yourself

1. **平台检测练习**：创建一个应用，启动时显示当前操作系统信息（名称、版本、架构），使用 `OperatingSystem` 和 `RuntimeInformation`。

2. **文件路径练习**：实现一个 `PlatformPaths` 类，为 Windows、macOS、Linux 分别返回正确的应用数据目录，并编写单元测试。

3. **系统托盘练习**：创建一个最小化的系统托盘应用，支持"显示窗口"和"退出"两个菜单项。测试在不同平台上的行为。

4. **字体回退练习**：配置一个应用使用自定义字体，并提供完整的字体回退链。测试在字体不存在时的回退行为。

5. **平台特定服务练习**：实现一个 `IStartupRegistration` 接口，为 Windows（注册表）、macOS（Login Items）、Linux（.desktop 文件）分别实现。

6. **Native AOT 练习**：为一个简单的 Avalonia 应用创建 Native AOT 发布脚本，编译为 win-x64、osx-arm64、linux-x64 三个平台。

7. **DPI 处理练习**：创建一个自定义控件，正确处理高 DPI 显示。在 Windows 150% 缩放和 macOS Retina 上测试。

8. **综合练习**：在 CodexSwitch 中研究 `Program.cs`、`App.axaml.cs`、`MacDockIconService`、`AppPaths` 的实现，理解一个真实的跨平台应用如何处理平台差异。然后尝试为 Linux 添加一个启动注册服务。
