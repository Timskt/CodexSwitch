# 12. 多窗口与系统托盘

## 12.1 系统托盘

```csharp
public class TrayMenuController : IDisposable
{
    private readonly TrayIcon _trayIcon;

    public TrayMenuController(Application app, IClassicDesktopStyleApplicationLifetime desktop, ...)
    {
        _trayIcon = new TrayIcon
        {
            Icon = icon,
            ToolTipText = "CodexSwitch",
            IsVisible = true
        };

        var menu = new NativeMenu();
        var showItem = new NativeMenuItem("Show Window");
        showItem.Click += (_, _) => showMainWindow();
        menu.Add(showItem);

        var quitItem = new NativeMenuItem("Quit");
        quitItem.Click += (_, _) => desktop.Shutdown();
        menu.Add(quitItem);

        _trayIcon.Menu = menu;
        _trayIcon.Clicked += (_, _) => showMainWindow();
    }
}
```

## 12.2 启动到托盘

```csharp
// --start-minimized 参数
var startHidden = StartupLaunchOptions.ShouldStartHidden(Environment.GetCommandLineArgs());
MacDockIconService.ConfigureForWindowVisibility(!startHidden);

if (!startHidden)
    ShowMainWindow();
```

## 12.3 关闭到托盘

```csharp
desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
// 关闭窗口不会退出应用
```

## 12.4 多窗口管理

```csharp
// 子窗口定位
private void PositionDetailsWindow()
{
    var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
    var workingArea = screen.WorkingArea;

    // 屏幕边界检测
    x = Math.Clamp(x, minX, maxX);
    y = Math.Clamp(y, minY, maxY);

    _detailsWindow.Position = new PixelPoint(x, y);
}
```

## 12.5 悬浮窗交互

```csharp
// 悬停展开/收起
private void OnPointerEntered(object? sender, PointerEventArgs e)
{
    ExpandDetails();
}

private void OnPointerExited(object? sender, PointerEventArgs e)
{
    ScheduleCollapse();  // 延迟收起
}
```

## 12.6 拖拽移动

```csharp
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    if (e.ClickCount >= 2)
    {
        OpenMainWindowRequested?.Invoke(this, EventArgs.Empty);
        return;
    }

    _isDragging = true;
    BeginMoveDrag(e);  // Avalonia 内置窗口拖拽
}
```

## 12.7 深入：窗口所有权（Window Ownership）

### 窗口所有权概念

窗口所有权定义了窗口之间的父子关系：

- **所有者窗口（Owner）**：父窗口
- **被所有者窗口（Owned）**：子窗口

```csharp
// 设置窗口所有权
var childWindow = new ChildWindow();
childWindow.Show(mainWindow);  // mainWindow 是所有者
```

### 所有权的影响

1. **生命周期**：关闭所有者窗口时，所有被拥有的窗口也会关闭
2. **Z 顺序**：被拥有的窗口始终在所有者窗口之上
3. **最小化**：最小化所有者窗口时，被拥有的窗口也会最小化
4. **任务栏**：被拥有的窗口通常不在任务栏显示独立图标

### 获取和设置所有权

```csharp
// 获取窗口的所有者
var owner = myWindow.Owner;

// 设置窗口的所有者
myWindow.Owner = mainWindow;

// 获取窗口拥有的所有窗口
var ownedWindows = mainWindow.OwnedWindows;
```

### 所有权链

```
MainWindow (顶级窗口)
├── SettingsWindow (Owned by MainWindow)
│   └── AdvancedSettingsWindow (Owned by SettingsWindow)
└── AboutWindow (Owned by MainWindow)
```

## 12.8 深入：模态 vs 非模态窗口

### 模态窗口（Modal）

模态窗口会阻止用户与所有者窗口交互：

```csharp
// 显示模态对话框
var dialog = new ConfirmationDialog();
var result = await dialog.ShowDialog<bool>(mainWindow);

if (result)
{
    // 用户点击了确认
}
```

特点：
- 阻塞代码执行（使用 async/await）
- 阻止与所有者窗口交互
- 返回结果值
- 适合需要用户确认的操作

### 非模态窗口（Modeless）

非模态窗口允许用户同时与多个窗口交互：

```csharp
// 显示非模态窗口
var detailsWindow = new DetailsWindow();
detailsWindow.Show();  // 不阻塞代码执行
```

特点：
- 不阻塞代码执行
- 允许与所有者窗口交互
- 不返回结果值
- 适合工具窗口、辅助窗口

### 何时使用哪种

| 场景 | 推荐使用 |
|------|---------|
| 需要用户确认 | 模态窗口 |
| 错误消息 | 模态窗口 |
| 工具窗口 | 非模态窗口 |
| 辅助窗口 | 非模态窗口 |
| 需要返回值 | 模态窗口 |
| 长时间操作 | 非模态窗口 |

## 12.9 深入：窗口外观定制

### 窗口边框样式（WindowChrome）

```xml
<Window>
    <Window.Chrome>
        <WindowChrome CaptionHeight="32"
                      CornerRadius="0"
                      GlassFrameThickness="0"
                      ResizeBorderThickness="8"
                      UseAeroCaptionButtons="True"/>
    </Window.Chrome>

    <!-- 自定义标题栏 -->
    <DockPanel>
        <Border DockPanel.Dock="Top"
                Background="{StaticResource CsPrimaryBrush}"
                Height="32">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="My App" VerticalAlignment="Center"/>
                <Button Content="Minimize" Command="{Binding MinimizeCommand}"/>
                <Button Content="Maximize" Command="{Binding MaximizeCommand}"/>
                <Button Content="Close" Command="{Binding CloseCommand}"/>
            </StackPanel>
        </Border>
        <ContentPresenter/>
    </DockPanel>
</Window>
```

### 无边框窗口

```xml
<Window WindowStartupLocation="CenterScreen"
        WindowState="Normal"
        ExtendClientAreaToDecorationsHint="True"
        ExtendClientAreaChromeHints="NoChrome"
        ExtendClientAreaTitleBarHeightHint="-1">
    <!-- 完全自定义的窗口外观 -->
    <Border Background="{StaticResource CsBackgroundBrush}"
            CornerRadius="10">
        <StackPanel>
            <!-- 自定义标题栏 -->
            <DockPanel>
                <TextBlock Text="My App" DockPanel.Dock="Left"/>
                <Button Content="X" DockPanel.Dock="Right"
                        Command="{Binding CloseCommand}"/>
            </DockPanel>
            <!-- 窗口内容 -->
            <ContentPresenter/>
        </StackPanel>
    </Border>
</Window>
```

### 透明窗口

```xml
<Window Background="Transparent"
        TransparencyLevelHint="Transparent"
        ExtendClientAreaToDecorationsHint="True"
        ExtendClientAreaChromeHints="NoChrome">
    <!-- 透明背景的窗口 -->
    <Border Background="#80000000" CornerRadius="10">
        <TextBlock Text="Transparent Window"/>
    </Border>
</Window>
```

### 圆角窗口

```xml
<Window ExtendClientAreaToDecorationsHint="True"
        ExtendClientAreaChromeHints="NoChrome"
        ExtendClientAreaTitleBarHeightHint="-1">
    <Border CornerRadius="15"
            Background="{StaticResource CsBackgroundBrush}"
            ClipToBounds="True">
        <Border.Effect>
            <DropShadowEffect BlurRadius="10" Opacity="0.3"/>
        </Border.Effect>
        <StackPanel>
            <!-- 窗口内容 -->
        </StackPanel>
    </Border>
</Window>
```

## 12.10 深入：覆盖窗口和弹出窗口

### 覆盖窗口（Overlay Window）

覆盖窗口显示在其他窗口之上：

```csharp
public class OverlayWindow : Window
{
    public OverlayWindow()
    {
        // 设置为覆盖窗口
        Topmost = true;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.Manual;

        // 设置位置
        var screen = Screens.Primary;
        if (screen != null)
        {
            var workingArea = screen.WorkingArea;
            Position = new PixelPoint(
                workingArea.X + workingArea.Width - (int)Width - 20,
                workingArea.Y + workingArea.Height - (int)Height - 20);
        }
    }
}
```

### 弹出窗口（Popup）

```xml
<Border>
    <Border.ContextMenu>
        <ContextMenu>
            <MenuItem Header="Copy" Command="{Binding CopyCommand}"/>
            <MenuItem Header="Paste" Command="{Binding PasteCommand}"/>
        </ContextMenu>
    </Border.ContextMenu>
</Border>

<!-- 自定义弹出窗口 -->
<Popup IsOpen="{Binding IsPopupOpen}"
       PlacementTarget="{Binding #AnchorElement}"
       Placement="Bottom">
    <Border Background="White" Padding="10">
        <TextBlock Text="Popup Content"/>
    </Border>
</Popup>
```

### 工具提示

```xml
<Button Content="Hover Me">
    <ToolTip.Tip>
        <StackPanel>
            <TextBlock Text="Tool Tip Title" FontWeight="Bold"/>
            <TextBlock Text="Tool tip description"/>
        </StackPanel>
    </ToolTip.Tip>
</Button>
```

## 12.11 深入：屏幕管理

### 获取屏幕信息

```csharp
// 获取所有屏幕
var screens = Screens.All;

// 获取主屏幕
var primary = Screens.Primary;

// 获取窗口所在的屏幕
var screen = Screens.ScreenFromWindow(this);

// 屏幕信息
foreach (var screen in screens)
{
    Console.WriteLine($"Name: {screen.Name}");
    Console.WriteLine($"Bounds: {screen.Bounds}");
    Console.WriteLine($"WorkingArea: {screen.WorkingArea}");
    Console.WriteLine($"Scaling: {screen.Scaling}");
    Console.WriteLine($"IsPrimary: {screen.IsPrimary}");
}
```

### 多显示器支持

```csharp
// 在特定显示器上显示窗口
public void ShowOnScreen(Window window, Screen targetScreen)
{
    var workingArea = targetScreen.WorkingArea;
    window.Position = new PixelPoint(
        workingArea.X + (workingArea.Width - (int)window.Width) / 2,
        workingArea.Y + (workingArea.Height - (int)window.Height) / 2);
    window.Show();
}

// 跨显示器移动窗口
public void MoveToNextScreen(Window window)
{
    var currentScreen = Screens.ScreenFromWindow(window);
    var allScreens = Screens.All;
    var currentIndex = allScreens.IndexOf(currentScreen);
    var nextIndex = (currentIndex + 1) % allScreens.Count;
    var nextScreen = allScreens[nextIndex];

    ShowOnScreen(window, nextScreen);
}
```

### 屏幕边界检测

CodexSwitch 的 `PositionDetailsWindow` 展示了如何确保子窗口不超出屏幕边界：

```csharp
private void PositionDetailsWindow()
{
    var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
    var workingArea = screen.WorkingArea;

    // 屏幕边界检测
    x = Math.Clamp(x, minX, maxX);
    y = Math.Clamp(y, minY, maxY);

    _detailsWindow.Position = new PixelPoint(x, y);
}
```

## 12.12 深入：窗口状态持久化

### 保存窗口状态

```csharp
public class WindowStatePersistence
{
    private const string StateFile = "window-state.json";

    public void SaveState(Window window)
    {
        var state = new WindowState
        {
            X = window.Position.X,
            Y = window.Position.Y,
            Width = window.Width,
            Height = window.Height,
            WindowState_ = window.WindowState,
            IsMaximized = window.WindowState == WindowState.Maximized
        };

        var json = JsonSerializer.Serialize(state);
        File.WriteAllText(StateFile, json);
    }

    public WindowState? LoadState()
    {
        if (!File.Exists(StateFile))
            return null;

        var json = File.ReadAllText(StateFile);
        return JsonSerializer.Deserialize<WindowState>(json);
    }
}
```

### 恢复窗口状态

```csharp
protected override void OnOpened(EventArgs e)
{
    base.OnOpened(e);

    var state = _persistence.LoadState();
    if (state != null)
    {
        var screen = Screens.Primary;
        if (screen != null)
        {
            var workingArea = screen.WorkingArea;
            if (state.X >= workingArea.X &&
                state.X + state.Width <= workingArea.X + workingArea.Width &&
                state.Y >= workingArea.Y &&
                state.Y + state.Height <= workingArea.Y + workingArea.Height)
            {
                Position = new PixelPoint(state.X, state.Y);
                Width = state.Width;
                Height = state.Height;
            }
        }

        if (state.IsMaximized)
            WindowState = WindowState.Maximized;
    }
}

protected override void OnClosing(WindowClosingEventArgs e)
{
    base.OnClosing(e);
    _persistence.SaveState(this);
}
```

## 12.13 跨引用

- **样式系统**：窗口样式参见 [第 7 章](07-styling-theming.md)
- **数据绑定**：窗口数据绑定参见 [第 5 章](05-data-binding.md)
- **国际化**：窗口标题和菜单的本地化参见 [第 11 章](11-i18n.md)
- **拖拽交互**：窗口拖拽参见 [第 13 章](13-drag-drop.md)

## 12.14 常见陷阱

### 陷阱 1：忘记设置 ShutdownMode

```csharp
// 问题：关闭窗口时应用退出
// 默认 ShutdownMode 是 OnLastWindowClose

// 解决：设置 ShutdownMode
desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
```

CodexSwitch 的 `TrayMenuController` 在构造函数中设置了 `ShutdownMode.OnExplicitShutdown`，确保关闭窗口不会退出应用。

### 陷阱 2：窗口所有权导致的内存泄漏

```csharp
// 问题：没有正确清理拥有的窗口
public void ShowChildWindow()
{
    var child = new ChildWindow();
    child.Owner = mainWindow;
    child.Show();
    // 忘记处理 child 的关闭
}

// 解决：监听窗口关闭事件
public void ShowChildWindow()
{
    var child = new ChildWindow();
    child.Owner = mainWindow;
    child.Closed += (_, _) =>
    {
        // 清理资源
    };
    child.Show();
}
```

### 陷阱 3：模态对话框阻塞 UI 线程

```csharp
// 问题：在 UI 线程上同步等待模态对话框
var result = dialog.ShowDialog<bool>(mainWindow).Result;  // 死锁！

// 解决：使用 async/await
var result = await dialog.ShowDialog<bool>(mainWindow);
```

### 陷阱 4：屏幕边界检测遗漏

```csharp
// 问题：没有考虑任务栏高度
var screen = Screens.Primary;
window.Position = new PixelPoint(
    screen.Bounds.Width - (int)window.Width,
    screen.Bounds.Height - (int)window.Height);  // 可能被任务栏遮挡

// 解决：使用 WorkingArea
window.Position = new PixelPoint(
    screen.WorkingArea.X + screen.WorkingArea.Width - (int)window.Width,
    screen.WorkingArea.Y + screen.WorkingArea.Height - (int)window.Height);
```

### 陷阱 5：托盘图标不显示

```csharp
// 问题：忘记设置 TrayIcon
var trayIcon = new TrayIcon { Icon = icon, IsVisible = true };
// 忘记添加到 TrayIcons 集合

// 解决：添加到 TrayIcons
TrayIcon.SetIcons(app, new TrayIcons { trayIcon });
```

## 12.15 动手练习

### 练习 1：创建设置窗口

在 CodexSwitch 中创建一个模态设置窗口：

```csharp
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    public async Task<bool> ShowDialogAsync(Window owner)
    {
        return await ShowDialog<bool>(owner);
    }
}
```

### 练习 2：实现窗口状态持久化

为 CodexSwitch 的主窗口添加状态持久化：

1. 保存窗口位置、大小、状态
2. 启动时恢复窗口状态
3. 处理多显示器情况

### 练习 3：扩展 TrayMenuController

为 `TrayMenuController` 添加以下功能：

1. 显示当前代理状态
2. 添加快速切换提供商的子菜单
3. 添加打开设置的菜单项

### 练习 4：多显示器支持

实现一个功能，将窗口移动到下一个显示器：

```csharp
public void MoveToNextScreen()
{
    var currentScreen = Screens.ScreenFromWindow(this);
    var allScreens = Screens.All;
    var currentIndex = allScreens.IndexOf(currentScreen);
    var nextIndex = (currentIndex + 1) % allScreens.Count;
    var nextScreen = allScreens[nextIndex];

    var workingArea = nextScreen.WorkingArea;
    Position = new PixelPoint(
        workingArea.X + (workingArea.Width - (int)Width) / 2,
        workingArea.Y + (workingArea.Height - (int)Height) / 2);
}
```
