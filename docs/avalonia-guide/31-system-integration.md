# 31. 系统集成 -- 文件关联、协议处理与全局快捷键

> **写给零基础的你**：系统集成就是让你的软件和操作系统"深度配合"。比如双击一个 `.codexswitch` 文件自动用你的软件打开，或者在浏览器里点击 `codexswitch://open` 链接直接跳转到你的应用。这些都是"系统级"的能力，让你的软件更像一个"原生"应用。

## 31.1 概述

构建重量级桌面应用不仅需要漂亮的 UI，还需要与操作系统深度集成。本章涵盖以下系统集成能力：

- **文件关联**：双击特定扩展名的文件，自动用你的应用打开
- **自定义协议处理器**：通过 `myapp://` URI 跳转到应用
- **Windows Jump List**：任务栏右键菜单自定义
- **开机自启动**：Windows/macOS/Linux 三平台实现
- **全局快捷键**：应用不在前台时也能响应快捷键
- **剪切板高级操作**：富文本、图片、文件列表
- **文件系统监听**：FileSystemWatcher 实时监控文件变化

学完本章后，你将能够为 Avalonia 应用添加完整的系统级集成能力。

## 31.2 文件关联

### 31.2.1 什么是文件关联

文件关联就是告诉操作系统："当用户双击 `.xxx` 文件时，请用我的程序打开它"。比如双击 `.pdf` 用 Adobe Reader 打开，双击 `.docx` 用 Word 打开。

### 31.2.2 Windows 文件关联

在 Windows 上，文件关联通过注册表实现。有两种方式：

**方式一：Inno Setup / NSIS 安装程序（推荐）**

安装程序会在安装时自动写入注册表：

```ini
; Inno Setup 示例
[Registry]
; 建立文件类型
Root: HKCR; Subkey: ".myapp"; ValueType: string; ValueName: ""; ValueData: "MyAppFile"; Flags: uninsdeletevalue
Root: HKCR; Subkey: "MyAppFile"; ValueType: string; ValueName: ""; ValueData: "My App Document"; Flags: uninsdeletekey
Root: HKCR; Subkey: "MyAppFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\MyApp.exe,0"
Root: HKCR; Subkey: "MyAppFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\MyApp.exe"" ""%1"""
```

**方式二：应用内注册（运行时）**

```csharp
using Microsoft.Win32;

public static class FileAssociation
{
    /// <summary>
    /// 注册文件关联（需要管理员权限或当前用户范围）
    /// </summary>
    public static void Register(string extension, string progId, string description, string exePath)
    {
        // 注册扩展名 -> ProgID 映射
        using (var extKey = Registry.ClassesRoot.CreateSubKey(extension))
        {
            extKey.SetValue("", progId);
        }

        // 注册 ProgID -> 应用信息
        using (var progIdKey = Registry.ClassesRoot.CreateSubKey(progId))
        {
            progIdKey.SetValue("", description);

            // 设置默认图标
            using (var iconKey = progIdKey.CreateSubKey("DefaultIcon"))
            {
                iconKey.SetValue("", $"{exePath},0");
            }

            // 设置打开命令
            using (var commandKey = progIdKey.CreateSubKey(@"shell\open\command"))
            {
                commandKey.SetValue("", $"\"{exePath}\" \"%1\"");
            }
        }

        // 通知系统刷新文件关联
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
```

**在 Avalonia 应用中接收文件打开事件：**

```csharp
// Program.cs
public static void Main(string[] args)
{
    // args[0] 可能是用户双击的文件路径
    var filePath = args.Length > 0 ? args[0] : null;

    BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
}

// App.axaml.cs
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var args = desktop.Args;
        var openFile = args?.Length > 0 ? args[0] : null;

        var mainWindow = new MainWindow();
        if (openFile != null)
        {
            // 在 ViewModel 中处理打开的文件
            (mainWindow.DataContext as MainWindowViewModel)?.OpenFile(openFile);
        }
        desktop.MainWindow = mainWindow;
    }
    base.OnFrameworkInitializationCompleted();
}
```

### 31.2.3 macOS 文件关联

在 macOS 上，文件关联通过 `Info.plist` 配置：

```xml
<!-- Info.plist -->
<key>CFBundleDocumentTypes</key>
<array>
    <dict>
        <key>CFBundleTypeName</key>
        <string>My App Document</string>
        <key>CFBundleTypeExtensions</key>
        <array>
            <string>myapp</string>
        </array>
        <key>CFBundleTypeRole</key>
        <string>Editor</string>
        <key>CFBundleTypeIconFile</key>
        <string>myapp-icon</string>
    </dict>
</array>
```

### 31.2.4 Linux 文件关联

在 Linux 上，创建 `.desktop` 文件并注册 MIME 类型：

```ini
# ~/.local/share/applications/myapp.desktop
[Desktop Entry]
Name=My App
Exec=/path/to/myapp %F
Type=Application
MimeType=application/x-myapp;
Icon=myapp
```

```xml
<!-- ~/.local/share/mime/packages/myapp.xml -->
<?xml version="1.0" encoding="UTF-8"?>
<mime-info xmlns="http://www.freedesktop.org/standards/shared-mime-info">
    <mime-type type="application/x-myapp">
        <comment>My App Document</comment>
        <glob pattern="*.myapp"/>
    </mime-type>
</mime-info>
```

然后运行 `update-mime-database ~/.local/share/mime` 更新。

## 31.3 自定义协议处理器

### 31.3.1 什么是协议处理器

协议处理器让你的应用能响应 `myapp://` 开头的链接。当用户在浏览器中点击 `myapp://open/project/123` 时，操作系统会自动启动你的应用并传递这个 URL。常见的例子：`slack://`、`vscode://`、`zoommtg://`。

### 31.3.2 Windows 注册

```csharp
public static void RegisterProtocol(string protocol, string exePath)
{
    using (var key = Registry.ClassesRoot.CreateSubKey(protocol))
    {
        key.SetValue("", $"URL:{protocol} Protocol");
        key.SetValue("URL Protocol", "");

        using (var iconKey = key.CreateSubKey("DefaultIcon"))
        {
            iconKey.SetValue("", $"{exePath},0");
        }

        using (var commandKey = key.CreateSubKey(@"shell\open\command"))
        {
            commandKey.SetValue("", $"\"{exePath}\" \"%1\"");
        }
    }
}
```

### 31.3.3 macOS 注册

```xml
<!-- Info.plist -->
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>myapp</string>
        </array>
        <key>CFBundleURLName</key>
        <string>com.mycompany.myapp</string>
    </dict>
</array>
```

### 31.3.4 Linux 注册

```ini
# ~/.local/share/applications/myapp.desktop
[Desktop Entry]
Name=My App
Exec=/path/to/myapp %u
Type=Application
MimeType=x-scheme-handler/myapp;
```

### 31.3.5 Avalonia 中处理协议 URI

```csharp
// App.axaml.cs - 处理协议激活
public class App : Application
{
    // 单实例场景下的 URI 处理
    protected override void OnApplicationStartup(ApplicationStartupEventArgs e)
    {
        // 检查命令行参数中是否有协议 URI
        var args = Environment.GetCommandLineArgs();
        foreach (var arg in args)
        {
            if (arg.StartsWith("myapp://"))
            {
                HandleProtocolUri(arg);
            }
        }
    }

    private void HandleProtocolUri(string uri)
    {
        // 解析 URI: myapp://open/project/123
        var parsed = new Uri(uri);
        var host = parsed.Host;     // "open"
        var path = parsed.AbsolutePath; // "/project/123"

        // 根据 URI 执行相应操作
        Dispatcher.UIThread.Post(() =>
        {
            var vm = MainWindow?.DataContext as MainWindowViewModel;
            vm?.NavigateToResource(host, path);
        });
    }
}
```

**单实例应用的协议 URI 转发：**

```csharp
// 使用 Avalonia 的内置单实例支持
// Program.cs
public static void Main(string[] args)
{
    if (args.Contains("--secondary"))
    {
        // 作为第二个实例启动，通过 IPC 发送 URI 给主实例
        var uri = args.FirstOrDefault(a => a.StartsWith("myapp://"));
        if (uri != null)
        {
            SendUriToPrimaryInstance(uri);
        }
        return;
    }

    BuildAvaloniaApp()
        .With(new Win32PlatformOptions { })
        .StartWithClassicDesktopLifetime(args);
}
```

## 31.4 Windows Jump List

### 31.4.1 什么是 Jump List

Jump List 是 Windows 7+ 任务栏右键菜单中的自定义列表。比如 VS Code 的 Jump List 会显示"最近打开的项目"，Chrome 会显示"新窗口"、"新的无痕窗口"等。

### 31.4.2 Avalonia 中使用 NativeMenu 模拟

Avalonia 原生不直接暴露 Windows Jump List API，但可以通过 `TrayIcon` 的 `NativeMenu` 或平台原生 API 实现：

```csharp
// 使用 P/Invoke 访问 Windows Jump List
using System.Runtime.InteropServices;

public static class JumpListHelper
{
    // 通过 COM 接口 ICustomDestinationList 实现
    // 这需要较复杂的 COM 互操作，通常建议使用封装库

    /// <summary>
    /// 简易方案：通过 NativeMenu 实现类似效果
    /// </summary>
    public static void SetupTrayJumpList(TrayIcon trayIcon)
    {
        var menu = new NativeMenu();

        var recentItem = new NativeMenuItem("最近项目");
        var recentSubmenu = new NativeMenu();
        recentSubmenu.Add(new NativeMenuItem("项目 A"));
        recentSubmenu.Add(new NativeMenuItem("项目 B"));
        recentItem.Menu = recentSubmenu;
        menu.Add(recentItem);

        menu.Add(new NativeMenuItemSeparator());
        menu.Add(new NativeMenuItem("新建项目"));
        menu.Add(new NativeMenuItem("设置"));

        trayIcon.Menu = menu;
    }
}
```

### 31.4.3 使用 Windows API 实现真正的 Jump List

对于需要真正 Windows Jump List 的场景，可以使用 `WindowsAPICodePack` 或直接 P/Invoke：

```csharp
// 使用 Microsoft.WindowsAPICodePack.Shell
// NuGet: Microsoft-WindowsAPICodePack-Shell

using Microsoft.WindowsAPICodePack.Taskbar;

public static class RealJumpList
{
    public static void Configure()
    {
        var jumpList = JumpList.CreateJumpList();

        // 添加用户任务（固定操作）
        jumpList.AddUserTasks(new JumpListLink("myapp://new", "新建项目")
        {
            IconReference = new IconReference("myapp.exe", 0)
        });

        jumpList.AddUserTasks(new JumpListSeparator());

        jumpList.AddUserTasks(new JumpListLink("myapp://settings", "设置")
        {
            IconReference = new IconReference("myapp.exe", 1)
        });

        // 添加最近项目
        jumpList.KnownCategoryToDisplay = JumpListKnownCategoryType.Recent;

        jumpList.Refresh();
    }
}
```

## 31.5 开机自启动

### 31.5.1 三平台实现策略

开机自启动没有跨平台统一 API，需要针对每个平台单独实现。

**接口定义：**

```csharp
public interface IStartupManager
{
    bool IsStartupEnabled { get; }
    void EnableStartup(string appName, string exePath);
    void DisableStartup(string appName);
}
```

**Windows 实现：**

```csharp
using Microsoft.Win32;

public class WindowsStartupManager : IStartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsStartupEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue("MyApp") != null;
        }
    }

    public void EnableStartup(string appName, string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.SetValue(appName, $"\"{exePath}\" --minimized");
    }

    public void DisableStartup(string appName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.DeleteValue(appName, false);
    }
}
```

**macOS 实现（Launch Agent）：**

```csharp
public class MacStartupManager : IStartupManager
{
    private string PlistPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "LaunchAgents", "com.myapp.autostart.plist");

    public bool IsStartupEnabled => File.Exists(PlistPath);

    public void EnableStartup(string appName, string exePath)
    {
        var plist = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>Label</key>
    <string>com.myapp.autostart</string>
    <key>ProgramArguments</key>
    <array>
        <string>{exePath}</string>
        <string>--minimized</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <false/>
</dict>
</plist>";

        var dir = Path.GetDirectoryName(PlistPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(PlistPath, plist);
    }

    public void DisableStartup(string appName)
    {
        if (File.Exists(PlistPath))
            File.Delete(PlistPath);
    }
}
```

**Linux 实现（.desktop 文件）：**

```csharp
public class LinuxStartupManager : IStartupManager
{
    private string DesktopFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "autostart", "myapp.desktop");

    public bool IsStartupEnabled => File.Exists(DesktopFilePath);

    public void EnableStartup(string appName, string exePath)
    {
        var content = $@"[Desktop Entry]
Type=Application
Name={appName}
Exec={exePath} --minimized
X-GNOME-Autostart-enabled=true
Hidden=false";

        var dir = Path.GetDirectoryName(DesktopFilePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(DesktopFilePath, content);
    }

    public void DisableStartup(string appName)
    {
        if (File.Exists(DesktopFilePath))
            File.Delete(DesktopFilePath);
    }
}
```

**平台工厂：**

```csharp
public static class StartupManagerFactory
{
    public static IStartupManager Create()
    {
        if (OperatingSystem.IsWindows()) return new WindowsStartupManager();
        if (OperatingSystem.IsMacOS())   return new MacStartupManager();
        if (OperatingSystem.IsLinux())   return new LinuxStartupManager();
        throw new PlatformNotSupportedException();
    }
}
```

## 31.6 全局快捷键

### 31.6.1 应用内快捷键 vs 全局快捷键

- **应用内快捷键**：只有应用窗口在前台时才生效。Avalonia 的 `KeyBinding` 和 `HotKeyManager` 属于此类。
- **全局快捷键**：即使应用在后台、最小化，甚至没有窗口时也能响应。需要操作系统级别的 API。

### 31.6.2 Avalonia 应用内快捷键

```xml
<!-- 在 AXAML 中定义 KeyBinding -->
<Window.KeyBindings>
    <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}" />
    <KeyBinding Gesture="Ctrl+Shift+N" Command="{Binding NewProjectCommand}" />
    <KeyBinding Gesture="F5" Command="{Binding RefreshCommand}" />
</Window.KeyBindings>
```

```csharp
// 使用 HotKeyManager 注册（应用内生效）
HotKeyManager.SetHotKey(saveButton, new KeyGesture(Key.S, KeyModifiers.Control));
```

### 31.6.3 全局快捷键（平台原生）

**Windows 实现（RegisterHotKey）：**

```csharp
using System.Runtime.InteropServices;

public class WindowsGlobalHotkey : IDisposable
{
    private readonly int _id;
    private readonly IntPtr _hwnd;
    private readonly Action _callback;
    private bool _registered;

    // Win32 API
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const int WM_HOTKEY = 0x0312;

    public WindowsGlobalHotkey(IntPtr hwnd, int id, KeyModifiers modifiers, Key key, Action callback)
    {
        _hwnd = hwnd;
        _id = id;
        _callback = callback;

        uint modFlags = 0;
        if (modifiers.HasFlag(KeyModifiers.Alt))     modFlags |= MOD_ALT;
        if (modifiers.HasFlag(KeyModifiers.Control))  modFlags |= MOD_CONTROL;
        if (modifiers.HasFlag(KeyModifiers.Shift))    modFlags |= MOD_SHIFT;
        if (modifiers.HasFlag(KeyModifiers.Meta))     modFlags |= MOD_WIN;

        _registered = RegisterHotKey(hwnd, id, modFlags, (uint)KeyInterop.VirtualKeyFromKey(key));
    }

    /// <summary>
    /// 在窗口的 WndProc 中调用此方法来分发全局快捷键消息
    /// </summary>
    public static void ProcessWndProcMessage(IntPtr hwnd, uint msg, IntPtr wParam, Action<int> onHotkey)
    {
        if (msg == WM_HOTKEY)
        {
            onHotkey?.Invoke(wParam.ToInt32());
        }
    }

    public void Dispose()
    {
        if (_registered)
        {
            UnregisterHotKey(_hwnd, _id);
            _registered = false;
        }
    }
}
```

**与 Avalonia 窗口集成：**

```csharp
// 在 MainWindow 中集成
public partial class MainWindow : Window
{
    private WindowsGlobalHotkey? _globalHotkey;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (OperatingSystem.IsWindows())
        {
            var handle = this.TryGetPlatformHandle();
            if (handle != null)
            {
                // 注册 Ctrl+Shift+M 全局快捷键
                _globalHotkey = new WindowsGlobalHotkey(
                    handle.Handle, 1,
                    KeyModifiers.Control | KeyModifiers.Shift,
                    Key.M,
                    () => Dispatcher.UIThread.Post(() =>
                    {
                        this.Show();
                        this.WindowState = WindowState.Normal;
                        this.Activate();
                    }));
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _globalHotkey?.Dispose();
        base.OnClosed(e);
    }
}
```

**macOS 实现（CGEventTap）：**

```csharp
using System.Runtime.InteropServices;

public class MacGlobalHotkey
{
    // macOS 需要通过 CGEventTap 或 NSEvent addGlobalMonitorForEvents
    // 这通常需要使用 Objective-C 运行时互操作

    // 简化示例：使用 Carbon API（已废弃但仍然可用）
    [DllImport("/System/Library/Frameworks/Carbon.framework/Carbon")]
    private static extern int RegisterEventHotKey(
        uint keyCode, uint modifiers, IntPtr hotKeyId,
        IntPtr target, uint options, out IntPtr reference);

    // 更现代的方式是使用 SharpHook 或类似库
}

// 推荐使用 SharpHook 库（跨平台）
// NuGet: SharpHook
// 提供跨平台的全局键盘/鼠标钩子
```

**推荐使用 SharpHook 库（跨平台）：**

```csharp
// NuGet: SharpHook
using SharpHook;

public class GlobalHotkeyService : IDisposable
{
    private readonly SimpleGlobalHook _hook = new();
    private readonly Dictionary<(KeyCode, uint), Action> _hotkeys = new();

    public GlobalHotkeyService()
    {
        _hook.KeyPressed += OnKeyPressed;
    }

    public async Task StartAsync() => await _hook.RunAsync();

    public void Register(KeyCode key, uint modifiers, Action callback)
    {
        _hotkeys[(key, modifiers)] = callback;
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        var modFlags = (uint)e.RawEvent.Mask;
        if (_hotkeys.TryGetValue((e.Data.KeyCode, modFlags), out var callback))
        {
            Dispatcher.UIThread.Post(callback);
        }
    }

    public void Dispose() => _hook.Dispose();
}
```

## 31.7 剪切板高级操作

### 31.7.1 Avalonia 剪切板基础

```csharp
// 获取剪切板引用
var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
if (clipboard == null) return;

// 文本操作
await clipboard.SetTextAsync("Hello World");
var text = await clipboard.GetTextAsync();
```

### 31.7.2 富文本（HTML）操作

```csharp
// 复制 HTML 内容
var html = "<h1>标题</h1><p>这是<strong>加粗</strong>文本</p>";
var dataObject = new DataObject();
dataObject.Set(DataFormats.Html, html);
await clipboard.SetDataObjectAsync(dataObject);

// 读取 HTML
var htmlContent = await clipboard.GetDataAsync(DataFormats.Html) as string;
```

### 31.7.3 图片操作

```csharp
// 复制图片到剪切板
public async Task CopyBitmapToClipboard(Bitmap bitmap)
{
    using var stream = new MemoryStream();
    bitmap.Save(stream);
    stream.Position = 0;

    var dataObject = new DataObject();
    dataObject.Set("image/png", stream.ToArray());
    await clipboard.SetDataObjectAsync(dataObject);
}

// 从剪切板粘贴图片
public async Task<Bitmap?> PasteBitmapFromClipboard()
{
    var data = await clipboard.GetDataAsync("image/png");
    if (data is byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }
    return null;
}
```

### 31.7.4 文件列表操作

```csharp
// 复制文件路径列表
public async Task CopyFilesToClipboard(IEnumerable<string> filePaths)
{
    var dataObject = new DataObject();
    dataObject.Set(DataFormats.Files, filePaths);
    await clipboard.SetDataObjectAsync(dataObject);
}

// 读取剪切板中的文件列表
public async Task<IReadOnlyList<string>?> GetFilesFromClipboard()
{
    var data = await clipboard.GetDataAsync(DataFormats.Files);
    if (data is IEnumerable<string> files)
        return files.ToList().AsReadOnly();
    return null;
}
```

### 31.7.5 监听剪切板变化

```csharp
// 定时轮询方式（跨平台兼容）
public class ClipboardMonitor : IDisposable
{
    private readonly DispatcherTimer _timer;
    private string? _lastText;

    public event Action<string>? ClipboardChanged;

    public ClipboardMonitor(TimeSpan interval)
    {
        _timer = new DispatcherTimer { Interval = interval };
        _timer.Tick += CheckClipboard;
    }

    private void CheckClipboard(object? sender, EventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null)?.Clipboard;

        // 注意：剪切板操作必须在 UI 线程
        // 实际实现中需要更复杂的异步处理
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();
    public void Dispose() => _timer.Stop();
}
```

## 31.8 文件系统监听

### 31.8.1 FileSystemWatcher 基础

`System.IO.FileSystemWatcher` 是 .NET 内置的文件系统监控类，跨平台可用：

```csharp
public class FileWatcherService : IDisposable
{
    private readonly FileSystemWatcher _watcher;

    public event Action<string, WatcherChangeTypes>? FileChanged;

    public FileWatcherService(string path, string filter = "*.*")
    {
        _watcher = new FileSystemWatcher(path, filter)
        {
            NotifyFilter = NotifyFilters.LastWrite
                         | NotifyFilters.FileName
                         | NotifyFilters.DirectoryName
                         | NotifyFilters.Size,
            EnableRaisingEvents = false,
            IncludeSubdirectories = true
        };

        _watcher.Created += OnFileEvent;
        _watcher.Changed += OnFileEvent;
        _watcher.Deleted += OnFileEvent;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        // 注意：FileSystemWatcher 在后台线程触发事件
        // 需要 Dispatcher.UIThread 切换到 UI 线程
        Dispatcher.UIThread.Post(() =>
        {
            FileChanged?.Invoke(e.FullPath, e.ChangeType);
        });
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            FileChanged?.Invoke(e.FullPath, WatcherChangeTypes.Renamed);
        });
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        // FileSystemWatcher 的内部缓冲区溢出时会触发此事件
        // 通常是因为短时间内文件变化太多
        System.Diagnostics.Debug.WriteLine($"FileWatcher error: {e.GetException().Message}");
    }

    public void Start() => _watcher.EnableRaisingEvents = true;
    public void Stop() => _watcher.EnableRaisingEvents = false;

    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
    }
}
```

### 31.8.2 防抖处理

FileSystemWatcher 可能在短时间内触发大量事件（比如编辑器保存文件时会触发多次），需要防抖：

```csharp
public class DebouncedFileWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly DispatcherTimer _debounceTimer;
    private readonly HashSet<string> _pendingChanges = new();
    private readonly TimeSpan _debounceInterval;

    public event Action<IReadOnlyCollection<string>>? FilesChanged;

    public DebouncedFileWatcher(string path, TimeSpan? debounceInterval = null)
    {
        _debounceInterval = debounceInterval ?? TimeSpan.FromMilliseconds(500);
        _watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = false
        };

        _watcher.Changed += OnChange;
        _watcher.Created += OnChange;
        _watcher.Deleted += OnChange;

        _debounceTimer = new DispatcherTimer { Interval = _debounceInterval };
        _debounceTimer.Tick += OnDebounceElapsed;
    }

    private void OnChange(object sender, FileSystemEventArgs e)
    {
        lock (_pendingChanges)
        {
            _pendingChanges.Add(e.FullPath);
        }

        // 重置防抖计时器
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnDebounceElapsed(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();

        List<string> changes;
        lock (_pendingChanges)
        {
            changes = _pendingChanges.ToList();
            _pendingChanges.Clear();
        }

        if (changes.Count > 0)
        {
            Dispatcher.UIThread.Post(() => FilesChanged?.Invoke(changes));
        }
    }

    public void Start() => _watcher.EnableRaisingEvents = true;
    public void Stop() { _watcher.EnableRaisingEvents = false; _debounceTimer.Stop(); }
    public void Dispose() { Stop(); _watcher.Dispose(); }
}
```

### 31.8.3 跨平台注意事项

| 平台 | 底层机制 | 注意事项 |
|------|---------|---------|
| Windows | `ReadDirectoryChangesW` | 速度快，但网络驱动器可能不可靠 |
| macOS | `FSEvents` | 支持批量事件，精度略低 |
| Linux | `inotify` | 有最大 watch 数量限制（默认 8192） |

## 31.9 Deep Dive: 平台原生互操作模式

### 31.9.1 P/Invoke 模式

```csharp
// 新式源生成 P/Invoke (.NET 7+)
using System.Runtime.InteropServices;

public static partial class NativeMethods
{
    [LibraryImport("user32.dll")]
    public static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int GetWindowText(IntPtr hWnd, Span<char> text, int count);
}
```

### 31.9.2 条件编译模式

```csharp
public class PlatformService
{
    public string GetNativeConfigPath()
    {
#if WINDOWS
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyApp");
#elif MACOS
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "MyApp");
#else
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "myapp");
#endif
    }
}
```

## 31.10 Cross References

- **第 12 章**：系统托盘基础（本章扩展了 NativeMenu 和协议处理）
- **第 16 章**：键盘输入事件（本章扩展了全局快捷键）
- **第 20 章**：跨平台适配（本章的每个系统集成点都有跨平台差异）
- **第 25 章**：ASP.NET Core 集成（协议处理器可以与本地 HTTP 服务器配合）

## 31.11 Common Pitfalls

1. **权限问题**：Windows 注册表写入可能需要管理员权限；macOS Launch Agent 需要正确的文件权限
2. **全局快捷键冲突**：不要注册常见的快捷键组合（如 Ctrl+C），会与其他应用冲突
3. **FileSystemWatcher 缓冲区溢出**：短时间内大量文件变化会导致事件丢失，需要做好防抖和错误处理
4. **剪切板线程安全**：Avalonia 的剪切板操作必须在 UI 线程执行
5. **协议处理器注册时机**：通常在安装时注册，而不是应用启动时
6. **单实例与协议 URI**：需要确保第二个实例能将 URI 传递给主实例

## 31.12 Try It Yourself

1. 为你的 Avalonia 应用注册一个自定义协议 `myapp://`，并在浏览器中测试跳转
2. 实现一个跨平台的"开机自启动"开关，绑定到 Settings 页面的 ToggleButton
3. 使用 FileSystemWatcher 实现一个简单的文件变更监控面板
4. 实现剪切板历史记录功能（定时轮询剪切板内容并保存到列表）
