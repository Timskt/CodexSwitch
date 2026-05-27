# 12. 多窗口与系统托盘

> **写给零基础的你**：系统托盘就是屏幕右下角（Windows）或顶部菜单栏（Mac）那个小图标区域。微信、QQ 缩小后都躲在那里面。多窗口就是一个软件可以弹出多个独立的窗口，比如浏览器可以打开多个标签页。

## 12.1 概述

多窗口管理和系统托盘是桌面应用的核心能力。学完本章，你将能够：

- 掌握 Window 控件的所有重要属性和生命周期
- 实现系统托盘图标和原生菜单
- 管理多个窗口的创建、显示、隐藏和销毁
- 实现屏幕感知的窗口定位（多显示器支持）
- 实现窗口状态持久化（位置、大小、状态恢复）
- 使用 NativeMenu 创建平台原生菜单
- 理解窗口所有权和生命周期管理

CodexSwitch 实现了完整的多窗口架构：主窗口、系统托盘、MiniStatusWindow（悬浮状态条）和 MiniStatusDetailsWindow（详情弹窗）。这些窗口之间有复杂的交互关系，是学习多窗口管理的绝佳案例。

## 12.2 核心概念

### 12.2.1 Window 的重要属性

> **小白提示**：窗口就像一个"画框"。WindowState 决定画框是正常大小、最小化（缩到任务栏）、最大化（占满屏幕）还是全屏（连任务栏都覆盖）。

**WindowState** — 窗口状态

```csharp
window.WindowState = WindowState.Normal;      // 正常大小（默认）
window.WindowState = WindowState.Minimized;   // 最小化（缩到任务栏）
window.WindowState = WindowState.Maximized;   // 最大化（占满屏幕，但还能看到任务栏）
window.WindowState = WindowState.FullScreen;  // 全屏（覆盖整个屏幕，包括任务栏）
```

```xml
<Window WindowState="{Binding WindowState}">
```

**WindowStartupLocation** — 启动位置

```csharp
window.WindowStartupLocation = WindowStartupLocation.CenterScreen;    // 屏幕中央
window.WindowStartupLocation = WindowStartupLocation.CenterOwner;    // 居中于 Owner
window.WindowStartupLocation = WindowStartupLocation.Manual;         // 手动设置 Position
```

**SizeToContent** — 尺寸自适应

```csharp
window.SizeToContent = SizeToContent.Width;           // 宽度自适应内容
window.SizeToContent = SizeToContent.Height;          // 高度自适应内容
window.SizeToContent = SizeToContent.WidthAndHeight;  // 宽高都自适应
window.SizeToContent = SizeToContent.Manual;          // 手动设置
```

CodexSwitch 的 MiniStatusWindow 使用 `SizeToContent = SizeToContent.Width` 让宽度自适应内容，高度固定为 34px。

**ShowInTaskbar** — 是否在任务栏显示

```csharp
window.ShowInTaskbar = false;  // 不在任务栏显示（适合弹窗、工具窗口）
window.ShowInTaskbar = true;   // 在任务栏显示（默认）
```

**其他重要属性**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Topmost` | `bool` | 是否始终在最前面 |
| `ShowActivated` | `bool` | 显示时是否激活（获取焦点） |
| `CanResize` | `bool` | 是否可调整大小 |
| `SystemDecorations` | `SystemDecorations` | 窗口装饰（标题栏、边框） |
| `ExtendClientAreaToDecorationsHint` | `bool` | 是否将客户区扩展到装饰区域 |
| `MinWidth/MaxWidth` | `double` | 最小/最大宽度 |
| `MinHeight/MaxHeight` | `double` | 最小/最大高度 |
| `Icon` | `WindowIcon` | 窗口图标 |
| `Title` | `string` | 窗口标题 |

### 12.2.2 窗口生命周期

```
创建 (new Window)
    -> Show(owner) 或 Show()
        -> 窗口变为可见
        -> 触发 Opened 事件
        -> 进入消息循环
    -> Hide()
        -> 窗口变为不可见但不销毁
        -> 可以再次 Show()
    -> Close()
        -> 触发 Closing 事件（可取消）
        -> 触发 Closed 事件
        -> 窗口销毁
```

`Hide` vs `Close`：`Hide` 保留窗口实例，`Show` 可以重新显示；`Close` 销毁窗口，再次使用需要重新创建。

CodexSwitch 的详情窗口使用 `Hide()` 而非 `Close()` 保留实例：

```csharp
private void HideDetailsWindow()
{
    if (_detailsWindow?.IsVisible == true)
        _detailsWindow.Hide();  // 不销毁，下次 Show 可以复用
}
```

### 12.2.3 ShutdownMode

| 模式 | 行为 |
|------|------|
| `OnLastWindowClose` | 关闭最后一个窗口时退出应用（默认） |
| `OnMainWindowClose` | 关闭主窗口时退出应用 |
| `OnExplicitShutdown` | 只有调用 `Shutdown()` 才退出 |

CodexSwitch 使用 `OnExplicitShutdown`，因为主窗口关闭后托盘仍在运行：

```csharp
desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
```

### 12.2.4 窗口所有权

```csharp
// 创建子窗口并指定 owner
var childWindow = new SettingsWindow();
childWindow.Show(mainWindow);  // mainWindow 是 owner
```

指定 owner 后：
- 子窗口始终在 owner 窗口之上
- owner 最小化时子窗口也最小化
- owner 关闭时子窗口也关闭
- 子窗口可以使用 owner 的坐标系定位

## 12.3 进阶用法

### 12.3.1 系统托盘（TrayIcon）

```csharp
public class TrayMenuController : IDisposable
{
    private readonly TrayIcon _trayIcon;

    public TrayMenuController(
        Application app,
        IClassicDesktopStyleApplicationLifetime desktop,
        WindowIcon icon,
        Action showMainWindow)
    {
        _trayIcon = new TrayIcon
        {
            Icon = icon,
            ToolTipText = "CodexSwitch",
            IsVisible = true
        };

        // 构建原生菜单
        var menu = new NativeMenu();

        var showItem = new NativeMenuItem("Show Window");
        showItem.Click += (_, _) => showMainWindow();
        menu.Add(showItem);

        menu.Add(new NativeMenuItemSeparator());

        var toggleStatusItem = new NativeMenuItem("Toggle Status Bar");
        toggleStatusItem.Click += (_, _) => ToggleStatusBar();
        menu.Add(toggleStatusItem);

        menu.Add(new NativeMenuItemSeparator());

        var quitItem = new NativeMenuItem("Quit");
        quitItem.Click += (_, _) => desktop.Shutdown();
        menu.Add(quitItem);

        _trayIcon.Menu = menu;

        // 单击托盘图标显示主窗口
        _trayIcon.Clicked += (_, _) => showMainWindow();
    }

    public void Dispose()
    {
        _trayIcon.IsVisible = false;
        _trayIcon.Dispose();
    }
}
```

`TrayIcon` 是 Avalonia 对系统托盘图标的抽象。`NativeMenu` 使用平台原生菜单样式，在 Windows、macOS 和 Linux 上外观各异但行为一致。

### 12.3.2 NativeMenu 完整用法

`NativeMenu` 可以包含多种元素：

```csharp
var menu = new NativeMenu();

// 普通菜单项
var openItem = new NativeMenuItem("Open");
openItem.Click += (_, _) => OpenFile();
openItem.Gesture = new KeyGesture(Key.O, KeyModifiers.Control);  // 快捷键
menu.Add(openItem);

// 带子菜单的菜单项
var recentItem = new NativeMenuItem("Recent Files");
recentItem.Menu = new NativeMenu();
recentItem.Menu.Add(new NativeMenuItem("file1.txt"));
recentItem.Menu.Add(new NativeMenuItem("file2.txt"));
menu.Add(recentItem);

// 分隔线
menu.Add(new NativeMenuItemSeparator());

// 带图标的菜单项（macOS 上的 Template Image）
var prefsItem = new NativeMenuItem("Preferences");
prefsItem.Gesture = new KeyGesture(Key.OemComma, KeyModifiers.Control);
menu.Add(prefsItem);

// 禁用的菜单项
var disabledItem = new NativeMenuItem("Disabled Action");
disabledItem.IsEnabled = false;
menu.Add(disabledItem);
```

**NativeMenu 的平台差异**

| 特性 | Windows | macOS | Linux |
|------|---------|-------|-------|
| 托盘菜单 | 右键显示 | 右键显示 | 右键显示 |
| 快捷键显示 | 显示在菜单项后 | 显示在菜单项后 | 不显示 |
| 图标支持 | 不支持 | 支持 Template Image | 不支持 |
| 分隔线 | 显示 | 显示 | 显示 |

### 12.3.3 启动到托盘

```csharp
// 通过命令行参数 --start-minimized 启动时隐藏主窗口
var startHidden = StartupLaunchOptions.ShouldStartHidden(Environment.GetCommandLineArgs());

// macOS 上额外控制 Dock 图标可见性
MacDockIconService.ConfigureForWindowVisibility(!startHidden);

if (!startHidden)
    ShowMainWindow();
```

macOS 上调用 `MacDockIconService` 控制 Dock 图标可见性——隐藏窗口时也隐藏 Dock 图标，实现真正的"后台运行"。

### 12.3.4 窗口动画

```csharp
// 显示时的淡入动画
window.Opacity = 0;
window.Show();
// 使用 DoubleTransition 实现淡入
var transition = new DoubleTransition
{
    Property = Window.OpacityProperty,
    Duration = TimeSpan.FromMilliseconds(200)
};
window.Transitions = [transition];
window.Opacity = 1;
```

**自定义窗口打开动画**

```csharp
public class AnimatedWindow : Window
{
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        // 从点击位置扩散
        AnimateOpen();
    }

    private async void AnimateOpen()
    {
        var transform = new ScaleTransform(0.8, 0.8);
        RenderTransform = transform;
        RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        Opacity = 0;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = {
                    new Setter(ScaleTransform.ScaleXProperty, 0.8),
                    new Setter(ScaleTransform.ScaleYProperty, 0.8),
                    new Setter(OpacityProperty, 0d)
                }},
                new KeyFrame { Cue = new Cue(1), Setters = {
                    new Setter(ScaleTransform.ScaleXProperty, 1.0),
                    new Setter(ScaleTransform.ScaleYProperty, 1.0),
                    new Setter(OpacityProperty, 1d)
                }}
            }
        };
        await animation.RunAsync(this, CancellationToken.None);
    }
}
```

## 12.4 组件详解大全

### 12.4.1 Screens API — 屏幕管理

```csharp
// 获取所有屏幕
var allScreens = window.Screens.All;

// 获取主屏幕
var primary = window.Screens.Primary;

// 获取窗口所在的屏幕
var currentScreen = window.Screens.ScreenFromWindow(window);

// 获取鼠标所在的屏幕
var mouseScreen = window.Screens.ScreenFromPoint(mousePosition);
```

**Screen 对象**

```csharp
var screen = window.Screens.Primary;

// 工作区域（排除任务栏/Dock）
var workingArea = screen.WorkingArea;  // PixelRect { X, Y, Width, Height }

// 整个屏幕区域
var bounds = screen.Bounds;

// 缩放比例
var scaling = screen.Scaling;  // 1.0 = 100%, 2.0 = 200% (Retina)
```

**多显示器定位**

```csharp
// 将窗口定位到特定屏幕的中央
public static void CenterOnScreen(Window window, Screen screen)
{
    var workingArea = screen.WorkingArea;
    var x = workingArea.X + (workingArea.Width - (int)window.Width) / 2;
    var y = workingArea.Y + (workingArea.Height - (int)window.Height) / 2;
    window.Position = new PixelPoint(x, y);
}
```

### 12.4.2 窗口状态持久化

```csharp
public class WindowPositionManager
{
    private const string ConfigKey = "window.position";

    public static void Save(Window window, string windowId)
    {
        var config = new WindowPositionConfig
        {
            X = window.Position.X,
            Y = window.Position.Y,
            Width = window.Width,
            Height = window.Height,
            State = window.WindowState,
            ScreenBounds = window.Screens.ScreenFromWindow(window)?.Bounds
        };
        SaveConfig(windowId, config);
    }

    public static void Restore(Window window, string windowId)
    {
        var config = LoadConfig(windowId);
        if (config is null) return;

        // 验证位置是否仍然有效（屏幕可能已断开）
        var screen = window.Screens.All.FirstOrDefault(s =>
            s.Bounds.Contains(new PixelPoint(config.X, config.Y)))
            ?? window.Screens.Primary;

        if (screen is null) return;

        var workingArea = screen.WorkingArea;

        // 限制在屏幕范围内
        window.Position = new PixelPoint(
            Math.Clamp(config.X, workingArea.X,
                workingArea.X + workingArea.Width - (int)config.Width),
            Math.Clamp(config.Y, workingArea.Y,
                workingArea.Y + workingArea.Height - (int)config.Height));

        window.Width = Math.Max(window.MinWidth, Math.Min(config.Width, window.MaxWidth));
        window.Height = Math.Max(window.MinHeight, Math.Min(config.Height, window.MaxHeight));

        if (config.State == WindowState.Maximized)
            window.WindowState = WindowState.Maximized;
    }
}
```

### 12.4.3 无边框窗口

```xml
<Window SystemDecorations="None"
        ExtendClientAreaToDecorationsHint="True"
        Background="Transparent">
    <Border Background="{StaticResource CsBackgroundBrush}"
            CornerRadius="8"
            ClipToBounds="True">
        <!-- 自定义标题栏 -->
        <DockPanel>
            <Border DockPanel.Dock="Top" Height="32"
                    Background="{StaticResource CsPrimaryBrush}"
                    PointerPressed="OnTitleBarPointerPressed">
                <StackPanel Orientation="Horizontal" Margin="8,0">
                    <TextBlock Text="CodexSwitch" VerticalAlignment="Center"/>
                    <Button Content="X" HorizontalAlignment="Right"
                            Click="OnCloseClick"/>
                </StackPanel>
            </Border>
            <!-- 内容区域 -->
            <ContentPresenter/>
        </DockPanel>
    </Border>
</Window>
```

```csharp
// 拖拽移动
private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
{
    if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
    {
        BeginMoveDrag(e);
    }
}

// 关闭
private void OnCloseClick(object? sender, RoutedEventArgs e)
{
    Close();
}
```

## 12.5 CodexSwitch 实战

### 12.5.1 MiniStatusWindow 完整解析

MiniStatusWindow 是一个 34px 高的纤细悬浮条，常驻在屏幕边缘。

**初始化配置**

```csharp
public MiniStatusWindow()
{
    InitializeComponent();
    SizeToContent = SizeToContent.Width;    // 宽度自适应内容
    MinWidth = CollapsedMinWidth;           // 170px
    MaxWidth = CollapsedMaxWidth;           // 360px
    Height = CollapsedHeight;               // 34px

    _collapseTimer = new DispatcherTimer
    {
        Interval = CollapseDelay            // 220ms
    };
    _collapseTimer.Tick += OnCollapseTimerTick;
}
```

**悬停展开与延迟收起的状态机**

```
指针进入 -> ExpandDetails() -> 显示详情窗口
指针退出 -> ScheduleCollapse() -> 启动 220ms 定时器
定时器到期 -> 检查指针是否仍在 -> CollapseDetails()
```

```csharp
private void OnPointerEntered(object? sender, PointerEventArgs e)
{
    if (_isDragging || _suppressHoverUntilExit)
        return;
    ExpandDetails();
}

private void OnPointerExited(object? sender, PointerEventArgs e)
{
    if (_isDragging) return;
    _suppressHoverUntilExit = false;
    ScheduleCollapse();
}

private void OnCollapseTimerTick(object? sender, EventArgs e)
{
    _collapseTimer.Stop();
    if (_isDragging || IsPointerOver || _detailsWindow?.IsPointerOver == true)
        return;
    CollapseDetails();
}
```

220ms 的延迟让鼠标从主窗口移到详情窗口时不会闪烁收起。

**_suppressHoverUntilExit 机制**

拖拽释放后 `IsPointerOver` 可能为 true（鼠标还在窗口上），但不希望立即展开。`_suppressHoverUntilExit` 标志在 `OnPointerExited` 时清除，确保只有鼠标真正离开再进入时才触发展开。

### 12.5.2 多窗口管理

**详情窗口的创建与复用**

```csharp
private MiniStatusDetailsWindow CreateDetailsWindow(MainWindowViewModel viewModel)
{
    var window = new MiniStatusDetailsWindow(viewModel)
    {
        ShowActivated = false  // 不抢焦点
    };
    window.PointerEntered += OnDetailsPointerEntered;
    window.PointerExited += OnDetailsPointerExited;
    window.Closed += (_, _) =>
    {
        window.PointerEntered -= OnDetailsPointerEntered;
        window.PointerExited -= OnDetailsPointerExited;
        if (ReferenceEquals(_detailsWindow, window))
            _detailsWindow = null;
    };
    return window;
}
```

`ShowActivated = false` 确保详情窗口显示时不抢走主窗口或悬浮条的焦点。

### 12.5.3 屏幕感知定位

```csharp
private void PositionDetailsWindow()
{
    var detailHeight = GetDetailHeight();
    var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;

    if (screen is not null)
        detailHeight = Math.Min(detailHeight, GetMaxDetailHeight(screen.WorkingArea));

    _detailsWindow.Width = DetailsWindowWidth;    // 380px
    _detailsWindow.Height = detailHeight;

    // 默认显示在悬浮条上方
    var preferredY = (int)Math.Round(Position.Y - detailHeight - DetailsGap);
    var x = Position.X;
    var y = preferredY;

    if (screen is not null)
    {
        var workingArea = screen.WorkingArea;
        var minX = workingArea.X + PlacementMargin;
        var maxX = workingArea.X + workingArea.Width - (int)DetailsWindowWidth - PlacementMargin;
        var minY = workingArea.Y + PlacementMargin;
        var maxY = workingArea.Y + workingArea.Height - (int)detailHeight - PlacementMargin;
        var fallbackBelowY = (int)Math.Round(Position.Y + Bounds.Height + DetailsGap);

        // 水平：限制在屏幕范围内
        x = Math.Clamp(x, minX, Math.Max(minX, maxX));

        // 垂直：优先上方，空间不足时放到下方
        y = preferredY >= minY
            ? preferredY
            : fallbackBelowY <= maxY
                ? fallbackBelowY
                : Math.Clamp(preferredY, minY, Math.Max(minY, maxY));
    }

    _detailsWindow.Position = new PixelPoint(x, y);
}
```

关键逻辑：优先在悬浮条上方显示详情，如果上方空间不足则放到下方，如果下方也不够则限制在屏幕边界内。

### 12.5.4 拖拽移动与位置保存

```csharp
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    var point = e.GetCurrentPoint(this);
    if (!point.Properties.IsLeftButtonPressed) return;

    // 双击打开主窗口
    if (e.ClickCount >= 2)
    {
        OpenMainWindowRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
        return;
    }

    _isDragging = true;
    _suppressHoverUntilExit = false;
    CollapseDetails();       // 拖拽时收起详情
    BeginMoveDrag(e);        // Avalonia 内置窗口拖拽
    e.Handled = true;
}

private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
{
    _isDragging = false;
    _suppressHoverUntilExit = IsPointerOver;
    _viewModel?.SaveMiniStatusPosition(Position.X, Position.Y);  // 保存位置
}
```

`BeginMoveDrag` 是 Avalonia 内置的窗口拖拽方法，调用后系统接管鼠标移动，窗口跟随指针。

### 12.5.5 窗口关闭时的清理

```csharp
// 在构造函数中注册
Closed += (_, _) =>
{
    _collapseTimer.Stop();
    _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    CloseDetailsWindow();
};

private void CloseDetailsWindow()
{
    if (_detailsWindow is null) return;

    _detailsWindow.PointerEntered -= OnDetailsPointerEntered;
    _detailsWindow.PointerExited -= OnDetailsPointerExited;
    _detailsWindow.Close();
    _detailsWindow = null;
}
```

## 12.6 举一反三

### 12.6.1 与 WPF 窗口管理的差异

| 特性 | WPF | Avalonia |
|------|-----|----------|
| 窗口类 | `System.Windows.Window` | `Avalonia.Controls.Window` |
| 托盘图标 | `System.Windows.Forms.NotifyIcon` | `TrayIcon` |
| 原生菜单 | `ContextMenu` | `NativeMenu` |
| 屏幕信息 | `System.Windows.Forms.Screen` | `Screens` API |
| DPI 感知 | `VisualTreeHelper.GetDpi()` | `Screens.Scaling` |
| 窗口拖拽 | `DragMove()` | `BeginMoveDrag(e)` |

### 12.6.2 跨平台窗口差异

| 特性 | Windows | macOS | Linux |
|------|---------|-------|-------|
| 系统装饰 | 标准标题栏 | 红绿灯按钮 | 依赖 WM |
| 无边框窗口 | `SystemDecorations="None"` | 同左 | 同左 |
| 窗口圆角 | 需自绘 | 系统圆角 | 依赖 WM |
| Dock 图标 | 任务栏图标 | Dock 图标 | 任务栏图标 |
| 全屏 | 标准全屏 | 原生全屏 | 依赖 WM |
| 窗口阴影 | 需自绘 | 系统阴影 | 依赖 WM |

## 12.7 最佳实践与设计模式

### 12.7.1 窗口生命周期管理

```csharp
// 好：使用 Hide/Show 复用窗口
_detailsWindow ??= CreateDetailsWindow(viewModel);
_detailsWindow.Show(this);
// ...
_detailsWindow.Hide();  // 保留实例

// 差：每次创建新窗口（浪费资源）
var window = new DetailsWindow(viewModel);
window.Show();
// window.Close() 后实例销毁
```

### 12.7.2 窗口位置安全检查

```csharp
// 确保窗口在可见屏幕范围内
var screen = window.Screens.All.FirstOrDefault(s =>
    s.Bounds.Contains(window.Position))
    ?? window.Screens.Primary;

if (screen is null) return;

var workingArea = screen.WorkingArea;
window.Position = new PixelPoint(
    Math.Clamp(window.Position.X, workingArea.X,
        workingArea.X + workingArea.Width - (int)window.Width),
    Math.Clamp(window.Position.Y, workingArea.Y,
        workingArea.Y + workingArea.Height - (int)window.Height));
```

### 12.7.3 事件订阅与清理

```csharp
// 好：在 Closed 中取消订阅
window.Closed += (_, _) =>
{
    window.PointerEntered -= OnPointerEntered;
    window.PointerExited -= OnPointerExited;
};
```

## Deep Dive：内部原理

### 窗口与操作系统的关系

```
Avalonia Window
    -> IWindowImpl（平台实现）
        -> Win32: HWND + WndProc
        -> macOS: NSWindow + NSWindowDelegate
        -> X11: XWindow + XEvent
```

每个平台的窗口系统差异被 `IWindowImpl` 抽象层屏蔽。`BeginMoveDrag`、`Position`、`Screens` 等 API 在每个平台上有不同的底层实现。

### PixelPoint vs 设备无关坐标

Avalonia 的窗口位置使用 `PixelPoint`（物理像素），不是设备无关单位。在 200% DPI 的显示器上，1 逻辑像素 = 2 物理像素。`Position` 属性直接读写窗口在屏幕上的绝对位置。

```
逻辑坐标 (DIP)          物理坐标 (Pixel)
(100, 100) @ 200% DPI -> (200, 200)
(100, 100) @ 100% DPI -> (100, 100)
(100, 100) @ 150% DPI -> (150, 150)
```

## Cross References

- **Avalonia 概览**：Avalonia 的窗口系统与 WPF 的差异，参见 [第 1 章](01-avalonia-overview.md)
- **输入事件**：Pointer 事件处理，参见 [第 16 章](16-input-events.md)
- **动画**：窗口打开/关闭动画，参见 [第 10 章](10-animation-transitions.md)
- **跨平台**：macOS Dock 集成的平台差异，参见 [第 20 章](20-cross-platform.md)
- **命令系统**：托盘菜单与 MVVM 命令的集成，参见 [第 18 章](18-commands.md)

## Common Pitfalls

### 陷阱 1：忘记设置 ShutdownMode

```csharp
// 错误：默认模式下关闭窗口会退出应用，托盘功能失效
// 正确：
desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
```

### 陷阱 2：窗口关闭后未取消事件订阅

```csharp
// 错误：
_detailsWindow.PointerEntered += OnDetailsPointerEntered;
// _detailsWindow 关闭后事件仍持有引用

// 正确：
window.Closed += (_, _) =>
{
    window.PointerEntered -= OnDetailsPointerEntered;
    if (ReferenceEquals(_detailsWindow, window))
        _detailsWindow = null;
};
```

### 陷阱 3：多窗口的 PointerEntered/Exited 不连贯

鼠标从窗口 A 移到窗口 B 时，A 收到 `PointerExited`，B 收到 `PointerEntered`。但如果两个窗口之间有间隙（像素级），可能出现短暂的"两个都退出"状态。使用定时器容错。

### 陷阱 4：屏幕坐标系差异

不同平台的 `PixelPoint` 坐标系不同。Avalonia 抽象了这个差异，但多显示器场景下 `WorkingArea` 的坐标仍然是像素坐标，需要使用 `Math.Clamp` 确保窗口不超出屏幕边界。

### 陷阱 5：BeginMoveDrag 的平台行为差异

`BeginMoveDrag` 在 macOS 上可能有轻微的延迟感（系统动画），在 Linux X11 上需要窗口管理器支持。如果拖拽不生效，检查窗口是否设置了 `SystemDecorations="None"`。

### 陷阱 6：ShowActivated=false 的平台差异

`ShowActivated = false` 在某些 Linux 窗口管理器上可能不生效——窗口仍然会获得焦点。

### 陷阱 7：托盘图标在 Linux 上不可见

某些 Linux 桌面环境（如 GNOME）不支持系统托盘图标。需要安装 `AppIndicator` 扩展或使用其他方案。

### 陷阱 8：窗口 Position 设置后未生效

在某些情况下，设置 `window.Position` 可能在窗口显示后才生效。使用 `Opened` 事件后设置位置更可靠。

### 陷阱 9：Close() 后访问窗口属性

```csharp
// 错误：
window.Close();
var pos = window.Position;  // 窗口已销毁，行为未定义

// 正确：
var pos = window.Position;  // 先读取
window.Close();              // 再关闭
```

### 陷阱 10：ScreenFromWindow 返回 null

当窗口尚未显示或已被关闭时，`Screens.ScreenFromWindow(window)` 可能返回 `null`。始终提供 `?? Screens.Primary` 回退。

### 陷阱 11：NativeMenu 在 Windows 上的渲染延迟

在 Windows 上，`NativeMenu` 的第一次渲染可能有轻微延迟。考虑在应用启动时预创建菜单。

## Try It Yourself

### 练习 1：添加托盘菜单项

在 `TrayMenuController` 中添加一个 `NativeMenuItem`，点击后切换悬浮条的显示/隐藏：

```csharp
var toggleItem = new NativeMenuItem("Toggle Status Bar");
toggleItem.Click += (_, _) =>
{
    if (miniStatusWindow.IsVisible)
        miniStatusWindow.Hide();
    else
        miniStatusWindow.Show();
};
menu.Add(toggleItem);
```

### 练习 2：实现窗口位置记忆

在窗口关闭时保存位置和大小，启动时恢复：

```csharp
// 保存
protected override void OnClosing(WindowClosingEventArgs e)
{
    WindowPositionManager.Save(this, "main-window");
    base.OnClosing(e);
}

// 恢复
protected override void OnOpened(EventArgs e)
{
    base.OnOpened(e);
    WindowPositionManager.Restore(this, "main-window");
}
```

### 练习 3：创建自定义无边框窗口

- 自绘标题栏和关闭/最大化/最小化按钮
- `BeginMoveDrag` 拖拽移动
- 窗口边缘调整大小
- 双击标题栏最大化/还原
- Windows 上支持 Snap Layout（Win11）

### 练习 4：实现全局热键激活

注册系统全局热键（如 `Ctrl+Shift+S`），按下时切换主窗口的显示/隐藏。

### 练习 5：实现窗口动画

为主窗口添加打开/关闭动画：
- 打开时从中心点放大 + 淡入
- 关闭时缩小 + 淡出

### 练习 6：实现多显示器窗口管理

创建一个工具，将窗口移动到指定显示器的中央：

```csharp
public static void MoveToScreen(Window window, Screen targetScreen)
{
    var workingArea = targetScreen.WorkingArea;
    window.Position = new PixelPoint(
        workingArea.X + (workingArea.Width - (int)window.Width) / 2,
        workingArea.Y + (workingArea.Height - (int)window.Height) / 2);
}
```

### 练习 7：实现窗口快照

在窗口关闭时截图保存，下次打开时显示快照作为过渡。

### 练习 8：创建系统信息浮窗

创建一个类似 MiniStatusWindow 的浮窗，显示 CPU/内存使用率，支持：
- 始终在最前面
- 透明背景
- 鼠标穿透（`IsHitTestVisible="False"`）
- 右键菜单切换显示内容
