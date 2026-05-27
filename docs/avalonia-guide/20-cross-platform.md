# 20. 跨平台适配

## 20.1 平台检测

```csharp
if (OperatingSystem.IsWindows()) { }
if (OperatingSystem.IsMacOS()) { }
if (OperatingSystem.IsLinux()) { }
if (OperatingSystem.IsIOS()) { }
if (OperatingSystem.IsAndroid()) { }
if (OperatingSystem.IsBrowser()) { }  // WebAssembly
```

## 20.2 macOS 特殊处理

### Dock 图标控制

```csharp
public static class MacDockIconService
{
    public static void ConfigureForWindowVisibility(bool visible)
    {
        if (!OperatingSystem.IsMacOS()) return;

        // 使用 NSApplication API 控制 Dock 图标
        // visible=true: 显示 Dock 图标
        // visible=false: 隐藏 Dock 图标（应用仍在运行）
    }
}
```

### macOS 菜单栏

```csharp
// macOS 的菜单栏在屏幕顶部，不在窗口内
if (OperatingSystem.IsMacOS())
{
    var menu = new NativeMenu();
    var appMenu = new NativeMenuItem("CodexSwitch");
    appMenu.Menu = new NativeMenu();
    appMenu.Menu.Add(new NativeMenuItem("Quit", (_, _) => app.Shutdown()));
    menu.Add(appMenu);
    NativeMenu.SetMenu(app, menu);
}
```

## 20.3 平台特定样式

```xml
<!-- 在 AXAML 中无法直接做平台判断，但可以通过代码设置 -->
<Style Selector="Window">
    <Style.Resources>
        <!-- 可以通过代码动态添加平台特定资源 -->
    </Style.Resources>
</Style>
```

```csharp
// 在代码中设置平台特定值
if (OperatingSystem.IsMacOS())
{
    Application.Current.Resources["WindowCornerRadius"] = new CornerRadius(10);
    Application.Current.Resources["TitleBarHeight"] = 28.0;
}
else if (OperatingSystem.IsWindows())
{
    Application.Current.Resources["WindowCornerRadius"] = new CornerRadius(0);
    Application.Current.Resources["TitleBarHeight"] = 32.0;
}
```

## 20.4 路径处理

```csharp
// 跨平台路径
var configPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "CodexSwitch",
    "config.json");

// 各平台的 ApplicationData 路径：
// Windows: C:\Users\{user}\AppData\Roaming
// macOS: /Users/{user}/.config (或 ~/Library/Application Support)
// Linux: /home/{user}/.config
```

### AppPaths 服务

```csharp
public static class AppPaths
{
    public static string AppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CodexSwitch");

    public static string ConfigFilePath { get; } = Path.Combine(AppDataDirectory, "config.json");
    public static string UsageLogDirectory { get; } = Path.Combine(AppDataDirectory, "usage");
    public static string PricingCatalogPath { get; } = Path.Combine(AppDataDirectory, "pricing.json");

    static AppPaths()
    {
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(UsageLogDirectory);
    }
}
```

## 20.5 文件系统差异

| 特性 | Windows | macOS | Linux |
|------|---------|-------|-------|
| 路径分隔符 | `\` | `/` | `/` |
| 文件名大小写 | 不敏感 | 默认不敏感 | 敏感 |
| 符号链接 | 支持 | 支持 | 支持 |
| 最大路径长度 | 260 (传统) / 32K (新) | 1024 | 4096 |

```csharp
// 使用 Path.Combine 确保跨平台兼容
var path = Path.Combine("folder", "subfolder", "file.txt");

// 不要使用硬编码的路径分隔符
var path = "folder\\subfolder\\file.txt";  // ❌ Windows only
var path = "folder/subfolder/file.txt";    // ✅ 跨平台
```

## 20.6 Native AOT 平台

```bash
# Windows x64
dotnet publish -r win-x64 --self-contained true -p:PublishAot=true

# Windows ARM64
dotnet publish -r win-arm64 --self-contained true -p:PublishAot=true

# macOS Intel
dotnet publish -r osx-x64 --self-contained true -p:PublishAot=true

# macOS Apple Silicon
dotnet publish -r osx-arm64 --self-contained true -p:PublishAot=true

# Linux x64
dotnet publish -r linux-x64 --self-contained true -p:PublishAot=true

# Linux ARM64
dotnet publish -r linux-arm64 --self-contained true -p:PublishAot=true
```

### Runtime Identifier (RID) 格式

```
{os}-{arch}

os: win, linux, osx, ios, android, browser
arch: x64, x86, arm, arm64

示例：
win-x64      Windows 64-bit Intel/AMD
osx-arm64    macOS Apple Silicon
linux-x64    Linux 64-bit
```

## 20.7 平台特定 API

```csharp
// 启动时注册平台特定服务
if (OperatingSystem.IsWindows())
{
    services.AddSingleton<IStartupService, WindowsStartupService>();
}
else if (OperatingSystem.IsMacOS())
{
    services.AddSingleton<IStartupService, MacStartupService>();
}
else if (OperatingSystem.IsLinux())
{
    services.AddSingleton<IStartupService, LinuxStartupService>();
}
```

---

## Deep Dive：Avalonia 的平台抽象层

Avalonia 通过平台抽象层 (Platform Abstraction Layer) 实现跨平台：

```
Application Code
    ↓
Avalonia Core (平台无关)
    ↓
Platform Backend (平台特定)
    ├── Win32 Backend
    ├── X11 Backend
    ├── macOS Backend (NativeAot)
    ├── iOS Backend
    ├── Android Backend
    └── WebAssembly Backend
```

每个后端实现了：
- 窗口管理
- 输入处理
- 渲染输出
- 剪贴板
- 文件对话框
- 系统主题检测

## Cross References

- [第 2 章 项目结构与启动流程](02-project-structure.md) — 项目配置和平台检测
- [第 15 章 编译绑定与 AOT 发布](15-compiled-bindings.md) — AOT 发布命令
- [第 21 章 调试与诊断](21-debugging.md) — 跨平台调试

## Common Pitfalls

1. **硬编码路径**: 使用 `Path.Combine` 而非字符串拼接
2. **忽略大小写差异**: Linux 文件名大小写敏感
3. **假设特定 API 存在**: 某些 API 只在特定平台可用

## Try It Yourself

1. 在 CodexSwitch 中找到 `MacDockIconService`，研究它的实现
2. 尝试为 Windows 添加一个启动注册服务
3. 测试应用在不同平台上的行为差异
