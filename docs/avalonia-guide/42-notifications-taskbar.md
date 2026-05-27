# 36. 通知系统与任务栏集成

> **写给零基础的你**：你一定见过桌面右下角弹出的通知——"有新邮件"、"下载完成"。这些就是系统通知。任务栏集成就是让软件在 Windows 任务栏上显示进度条、小图标等额外信息。本章教你如何实现这些功能。

## 36.1 概述

本章涵盖 Avalonia 应用的通知与任务栏集成：

- **应用内通知**：Avalonia 内置的通知管理器
- **系统原生通知**：Windows Toast、macOS Notification Center、Linux Desktop Notifications
- **任务栏进度条**：Windows 任务栏上的进度指示
- **任务栏缩略图**：自定义窗口预览
- **气泡提示（ToolTip）**：高级用法

## 36.2 应用内通知

### 36.2.1 WindowNotificationManager

Avalonia 内置了 `WindowNotificationManager`，在应用窗口内部显示通知：

```csharp
// 在窗口中初始化
public partial class MainWindow : Window
{
    private WindowNotificationManager _notificationManager;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _notificationManager = new WindowNotificationManager(this)
        {
            MaxItems = 3,
            Position = NotificationPosition.TopRight
        };
    }

    // 显示通知
    public void ShowNotification(string title, string message)
    {
        _notificationManager.Show(new Notification(title, message,
            NotificationType.Information, TimeSpan.FromSeconds(3)));
    }
}
```

### 36.2.2 通知类型

```csharp
// 四种通知类型
_notificationManager.Show(new Notification("成功", "操作已完成", NotificationType.Success));
_notificationManager.Show(new Notification("信息", "这是一条信息", NotificationType.Information));
_notificationManager.Show(new Notification("警告", "请注意", NotificationType.Warning));
_notificationManager.Show(new Notification("错误", "操作失败", NotificationType.Error));
```

### 36.2.3 自定义通知样式

```xml
<!-- 在 App.axaml 中自定义通知样式 -->
<Application.Styles>
    <Style Selector="NotificationPresenter">
        <Setter Property="Width" Value="350" />
    </Style>
</Application.Styles>
```

## 36.3 系统原生通知

### 36.3.1 跨平台通知接口

```csharp
public interface INativeNotificationService
{
    void Show(string title, string body, string? imageUrl = null);
    bool IsSupported { get; }
}
```

### 36.3.2 Windows Toast 通知

```csharp
// 使用 Microsoft.Toolkit.Uwp.Notifications
// NuGet: Microsoft.Toolkit.Uwp.Notifications

using Microsoft.Toolkit.Uwp.Notifications;

public class WindowsNotificationService : INativeNotificationService
{
    public bool IsSupported => OperatingSystem.IsWindows();

    public void Show(string title, string body, string? imageUrl = null)
    {
        var builder = new ToastContentBuilder()
            .AddToastActivationInfo("action=open", ToastActivationType.Foreground)
            .AddText(title)
            .AddText(body);

        if (imageUrl != null)
        {
            builder.AddInlineImage(new Uri(imageUrl));
        }

        builder.Show(); // 显示 Toast 通知
    }
}
```

**带按钮的交互式通知：**

```csharp
public void ShowWithActions(string title, string body)
{
    new ToastContentBuilder()
        .AddText(title)
        .AddText(body)
        .AddButton(new ToastButton()
            .SetContent("查看")
            .AddArgument("action", "view"))
        .AddButton(new ToastButton()
            .SetContent("忽略")
            .AddArgument("action", "dismiss"))
        .Show();
}
```

### 36.3.3 macOS 通知

```csharp
// macOS 通知需要通过 Objective-C 运行时互操作
// 或使用社区库如 DesktopNotifications

// 使用 DesktopNotifications 库（跨平台）
// NuGet: DesktopNotifications
using DesktopNotifications;

public class MacNotificationService : INativeNotificationService
{
    private readonly INotificationManager _manager;

    public MacNotificationService()
    {
        // DesktopNotifications 提供跨平台抽象
        _manager = new DesktopNotifications.Apple.MacNotificationManager();
    }

    public bool IsSupported => OperatingSystem.IsMacOS();

    public void Show(string title, string body, string? imageUrl = null)
    {
        var notification = new Notification
        {
            Title = title,
            Body = body
        };
        _manager.ShowNotification(notification);
    }
}
```

### 36.3.4 Linux Desktop Notifications

```csharp
// Linux 使用 D-Bus org.freedesktop.Notifications
// 推荐使用 DesktopNotifications 库

public class LinuxNotificationService : INativeNotificationService
{
    public bool IsSupported => OperatingSystem.IsLinux();

    public void Show(string title, string body, string? imageUrl = null)
    {
        // 通过 D-Bus 发送通知
        // 或使用 DesktopNotifications.Linux 实现
    }
}
```

### 36.3.5 统一通知服务

```csharp
public class NotificationService : INativeNotificationService
{
    private readonly INativeNotificationService _inner;

    public NotificationService()
    {
        _inner = OperatingSystem.IsWindows() ? new WindowsNotificationService()
               : OperatingSystem.IsMacOS()   ? new MacNotificationService()
               : new LinuxNotificationService();
    }

    public bool IsSupported => _inner.IsSupported;

    public void Show(string title, string body, string? imageUrl = null)
    {
        _inner.Show(title, body, imageUrl);
    }
}
```

## 36.4 任务栏进度条

### 36.4.1 Windows 任务栏进度

```csharp
// 使用 ITaskbarList3 COM 接口
using System.Runtime.InteropServices;

public class TaskbarProgress
{
    [ComImport]
    [Guid("56fdf344-fd6d-11d0-958a-006097c9a090")]
    [ClassInterface(ClassInterfaceType.None)]
    private class TaskbarList { }

    [ComImport]
    [Guid("c43dc798-95d1-4bea-9030-bb99e2983a1a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList3
    {
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActiveAlt(IntPtr hwnd);
        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
        void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
        void SetProgressState(IntPtr hwnd, TaskbarProgressState tbpFlags);
    }

    public enum TaskbarProgressState
    {
        NoProgress = 0,
        Indeterminate = 0x1,
        Normal = 0x2,
        Error = 0x4,
        Paused = 0x8
    }

    private static ITaskbarList3? _taskbarList;

    private static ITaskbarList3 GetTaskbarList()
    {
        _taskbarList ??= (ITaskbarList3)new TaskbarList();
        return _taskbarList;
    }

    public static void SetProgress(IntPtr hwnd, TaskbarProgressState state,
        ulong completed = 0, ulong total = 100)
    {
        if (!OperatingSystem.IsWindows()) return;

        var taskbar = GetTaskbarList();
        taskbar.SetProgressState(hwnd, state);
        if (state == TaskbarProgressState.Normal ||
            state == TaskbarProgressState.Paused ||
            state == TaskbarProgressState.Error)
        {
            taskbar.SetProgressValue(hwnd, completed, total);
        }
    }
}
```

### 36.4.2 在 Avalonia 中使用

```csharp
public partial class MainWindow : Window
{
    private IntPtr _hwnd;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (OperatingSystem.IsWindows())
        {
            _hwnd = this.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        }
    }

    /// <summary>
    /// 更新任务栏进度（0-100）
    /// </summary>
    public void SetTaskbarProgress(int percentage)
    {
        TaskbarProgress.SetProgress(_hwnd,
            TaskbarProgressState.Normal,
            (ulong)percentage, 100);
    }

    /// <summary>
    /// 显示不确定进度（动画）
    /// </summary>
    public void SetTaskbarIndeterminate()
    {
        TaskbarProgress.SetProgress(_hwnd, TaskbarProgressState.Indeterminate);
    }

    /// <summary>
    /// 显示错误状态
    /// </summary>
    public void SetTaskbarError()
    {
        TaskbarProgress.SetProgress(_hwnd, TaskbarProgressState.Error);
    }

    /// <summary>
    /// 清除进度
    /// </summary>
    public void ClearTaskbarProgress()
    {
        TaskbarProgress.SetProgress(_hwnd, TaskbarProgressState.NoProgress);
    }
}
```

### 36.4.3 绑定到 ViewModel

```csharp
public partial class DownloadViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _downloadProgress;

    [ObservableProperty]
    private bool _isDownloading;

    partial void OnDownloadProgressChanged(int value)
    {
        // 通过消息通知主窗口更新任务栏
        Messenger.Send(new TaskbarProgressMessage(value));
    }
}

// 主窗口接收消息
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Messenger.Register<TaskbarProgressMessage>(this, (r, m) =>
        {
            SetTaskbarProgress(m.Value);
        });
    }
}
```

## 36.5 任务栏缩略图按钮

### 36.5.1 自定义缩略图工具栏

Windows 7+ 支持在任务栏缩略图上添加自定义按钮：

```csharp
// 使用 IThumbnailToolbar 接口
// 这需要较复杂的 COM 互操作
// 推荐使用封装库或简化实现
```

### 36.5.2 任务栏图标叠加

```csharp
// 在任务栏图标上显示小标记（如未读消息数）
public static class TaskbarOverlay
{
    [DllImport("user32.dll")]
    private static extern int SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, IntPtr description);

    public static void SetOverlay(IntPtr hwnd, string? iconPath)
    {
        if (!OperatingSystem.IsWindows()) return;

        if (iconPath == null)
        {
            SetOverlayIcon(hwnd, IntPtr.Zero, IntPtr.Zero);
            return;
        }

        // 加载图标并设置
        // 实际实现需要处理图标资源
    }
}
```

## 36.6 高级 ToolTip

### 36.6.1 自定义 ToolTip 内容

```xml
<Button Content="Hover me">
    <Button.ToolTip>
        <ToolTip>
            <StackPanel Width="300">
                <TextBlock Text="高级提示" FontWeight="Bold" FontSize="14" />
                <Separator Margin="0,4" />
                <TextBlock Text="这里可以放置任意复杂的内容" TextWrapping="Wrap" />
                <Image Source="/Assets/help-icon.png" Width="50" Height="50"
                       HorizontalAlignment="Left" Margin="0,4,0,0" />
            </StackPanel>
        </ToolTip>
    </Button.ToolTip>
</Button>
```

### 36.6.2 ToolTip 位置和延迟

```xml
<Button Content="Custom ToolTip"
        ToolTip.Tip="Hello"
        ToolTip.Placement="Right"
        ToolTip.HorizontalOffset="10"
        ToolTip.VerticalOffset="0"
        ToolTip.ShowDelay="500" />
```

### 36.6.3 程序控制 ToolTip

```csharp
// 动态显示 ToolTip
var toolTip = new ToolTip
{
    Content = new TextBlock { Text = "动态内容" },
    Placement = PlacementMode.Right,
    HorizontalOffset = 10
};

ToolTip.SetTip(button, toolTip);
toolTip.IsOpen = true;

// 延迟关闭
DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(3) };
timer.Tick += (s, e) => { toolTip.IsOpen = false; timer.Stop(); };
timer.Start();
```

## 36.7 Deep Dive: 通知系统架构

### 36.7.1 通知队列管理

```csharp
public class NotificationQueue
{
    private readonly Queue<NotificationItem> _queue = new();
    private readonly int _maxVisible;
    private readonly List<NotificationItem> _visible = new();

    public NotificationQueue(int maxVisible = 3)
    {
        _maxVisible = maxVisible;
    }

    public void Enqueue(string title, string body, NotificationType type)
    {
        var item = new NotificationItem(title, body, type);

        if (_visible.Count < _maxVisible)
        {
            ShowNotification(item);
        }
        else
        {
            _queue.Enqueue(item);
        }
    }

    private void ShowNotification(NotificationItem item)
    {
        _visible.Add(item);
        item.Dismissed += OnNotificationDismissed;
        // 显示通知...
    }

    private void OnNotificationDismissed(NotificationItem item)
    {
        _visible.Remove(item);
        item.Dismissed -= OnNotificationDismissed;

        if (_queue.Count > 0)
        {
            ShowNotification(_queue.Dequeue());
        }
    }
}
```

### 36.7.2 通知持久化

```csharp
public class NotificationHistory
{
    private readonly List<NotificationRecord> _history = new();
    private readonly string _storagePath;

    public void Add(string title, string body)
    {
        _history.Add(new NotificationRecord
        {
            Title = title,
            Body = body,
            Timestamp = DateTime.UtcNow,
            IsRead = false
        });
        Save();
    }

    public IReadOnlyList<NotificationRecord> GetAll() => _history.AsReadOnly();

    public int UnreadCount => _history.Count(n => !n.IsRead);

    private void Save()
    {
        var json = JsonSerializer.Serialize(_history);
        File.WriteAllText(_storagePath, json);
    }
}
```

## 36.8 Cross References

- **第 12 章**：多窗口与系统托盘（TrayIcon 与通知配合）
- **第 17 章**：对话框与弹出层（通知的 UI 层级）
- **第 10 章**：动画与过渡（通知出现/消失动画）

## 36.9 Common Pitfalls

1. **Toast 通知不显示**：Windows 上需要应用有 AppUserModelID
2. **通知权限**：macOS 和某些 Linux 桌面需要用户授权通知权限
3. **任务栏进度不刷新**：确保在 UI 线程更新
4. **通知过多**：不要频繁弹通知，会影响用户体验
5. **跨平台差异**：不同平台的通知外观和行为差异很大
6. **ToolTip 性能**：复杂 ToolTip 内容可能导致渲染卡顿

## 36.10 Try It Yourself

1. 实现一个带四种类型的 Toast 通知系统
2. 为文件下载功能添加任务栏进度条
3. 创建通知中心面板，支持查看历史通知
4. 实现带交互按钮的系统通知
