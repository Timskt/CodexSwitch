# 32. 窗口特效 -- 透明、毛玻璃、圆角与阴影

> **写给零基础的你**：你可能注意到了，Windows 11 的设置窗口有半透明的毛玻璃效果，macOS 的窗口有漂亮的圆角和阴影。这些都是"窗口特效"。本章教你如何在 Avalonia 应用中实现这些效果，让你的软件看起来更现代、更专业。

## 32.1 概述

窗口特效是现代桌面应用的视觉标志。本章涵盖：

- **窗口透明**：让窗口背景完全透明
- **毛玻璃效果**：Windows 的 Acrylic/Mica、macOS 的 NSVisualEffect
- **圆角窗口**：无边框圆角窗口实现
- **窗口阴影**：自定义阴影效果
- **穿透点击**：透明区域可以点击穿透
- **窗口吸附**：Windows Snap Layouts 支持

学完本章后，你将能够创建媲美原生应用的窗口视觉效果。

## 32.2 窗口透明基础

### 32.2.1 TransparencyLevelHint

Avalonia 通过 `TransparencyLevelHint` 属性控制窗口透明度：

```xml
<Window xmlns="https://github.com/avaloniaui"
        TransparencyLevelHint="Transparent"
        Background="Transparent"
        ExtendClientAreaToDecorationsHint="True"
        ExtendClientAreaTitleBarHeightHint="-1">
    <!-- 窗口内容 -->
</Window>
```

**可用的透明度级别：**

| 级别 | 说明 | 平台支持 |
|------|------|---------|
| `None` | 不透明（默认） | 全平台 |
| `Transparent` | 完全透明 | 全平台 |
| `Blur` | 背景模糊 | Windows 10+ |
| `AcrylicBlur` | 亚克力模糊 | Windows 10 1803+ |
| `Mica` | Mica 材质 | Windows 11 |
| `MicaAlt` | Mica 替代色 | Windows 11 |

### 32.2.2 完全透明窗口

```xml
<Window x:Class="MyApp.TransparentWindow"
        TransparencyLevelHint="Transparent"
        Background="Transparent"
        ExtendClientAreaToDecorationsHint="True"
        SystemDecorations="None"
        SizeToContent="WidthAndContent"
        WindowStartupLocation="CenterScreen">
    <Border CornerRadius="12" Background="#E0202020" Padding="20">
        <TextBlock Text="这是一个透明窗口" Foreground="White" />
    </Border>
</Window>
```

### 32.2.3 检测实际透明度支持

请求的透明度级别不一定被系统支持，需要检测实际结果：

```csharp
public partial class TransparentWindow : Window
{
    public TransparentWindow()
    {
        InitializeComponent();

        // 监听实际透明度级别变化
        this.GetObservable(TransparencyHintProperty)
            .Subscribe(level =>
            {
                Console.WriteLine($"实际透明度: {level}");
            });
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // ActualTransparencyLevel 反映系统实际支持的级别
        var actual = this.ActualTransparencyLevel;
        if (actual == WindowTransparencyLevel.None)
        {
            // 系统不支持透明，降级为普通窗口
            this.Background = new SolidColorBrush(Colors.White);
        }
    }
}
```

## 32.3 毛玻璃效果

### 32.3.1 Windows Acrylic 效果

```xml
<Window TransparencyLevelHint="AcrylicBlur"
        Background="Transparent"
        ExtendClientAreaToDecorationsHint="True">
    <Border Background="#80FFFFFF" CornerRadius="8">
        <StackPanel Margin="20">
            <TextBlock Text="Acrylic 毛玻璃效果" FontSize="20" />
            <TextBlock Text="背景会显示桌面内容的模糊版本" Opacity="0.7" />
        </StackPanel>
    </Border>
</Window>
```

### 32.3.2 Windows Mica 效果

Mica 是 Windows 11 引入的新材质，它采样桌面壁纸的颜色作为窗口背景，比 Acrylic 更节能：

```xml
<Window TransparencyLevelHint="Mica"
        Background="Transparent"
        ExtendClientAreaToDecorationsHint="True">
    <!-- Mica 效果会自动应用到窗口背景 -->
    <Grid>
        <TextBlock Text="Mica 材质效果" Margin="20" />
    </Grid>
</Window>
```

**在代码中动态切换：**

```csharp
public void SetWindowMaterial(string material)
{
    this.TransparencyLevelHint = material switch
    {
        "Mica"        => WindowTransparencyLevel.Mica,
        "MicaAlt"     => WindowTransparencyLevel.MicaAlt,
        "Acrylic"     => WindowTransparencyLevel.AcrylicBlur,
        "Transparent" => WindowTransparencyLevel.Transparent,
        _             => WindowTransparencyLevel.None
    };
}
```

### 32.3.3 macOS 毛玻璃效果

macOS 的毛玻璃效果（Vibrancy）需要通过原生互操作实现：

```csharp
// macOS NSVisualEffectView 互操作
// 需要通过 Objective-C 运行时调用
if (OperatingSystem.IsMacOS())
{
    // Avalonia 的 ExtendClientAreaToDecorationsHint 配合透明
    // 可以获得类似的效果
    this.ExtendClientAreaToDecorationsHint = true;
    this.TransparencyLevelHint = WindowTransparencyLevel.Transparent;
    this.Background = Brushes.Transparent;
}
```

### 32.3.4 主题感知的背景色

毛玻璃效果需要根据亮色/暗色主题调整叠加色：

```csharp
public class GlassBackgroundService
{
    public static IBrush GetOverlayBrush(bool isDark, WindowTransparencyLevel level)
    {
        return (isDark, level) switch
        {
            (true, WindowTransparencyLevel.Mica)        => new SolidColorBrush(Color.FromArgb(180, 32, 32, 32)),
            (true, WindowTransparencyLevel.AcrylicBlur) => new SolidColorBrush(Color.FromArgb(120, 32, 32, 32)),
            (false, WindowTransparencyLevel.Mica)       => new SolidColorBrush(Color.FromArgb(180, 243, 243, 243)),
            (false, WindowTransparencyLevel.AcrylicBlur)=> new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
            _ => new SolidColorBrush(Colors.White)
        };
    }
}
```

## 32.4 圆角窗口

### 32.4.1 无边框圆角窗口

```xml
<Window x:Class="MyApp.RoundedWindow"
        SystemDecorations="None"
        TransparencyLevelHint="Transparent"
        Background="Transparent"
        ExtendClientAreaToDecorationsHint="True"
        WindowStartupLocation="CenterScreen"
        Width="800" Height="600">

    <Border CornerRadius="12"
            Background="{DynamicResource WindowBackgroundBrush}"
            BoxShadow="0 8 32 0 #40000000"
            ClipToBounds="True">

        <Grid RowDefinitions="Auto,*">
            <!-- 自定义标题栏 -->
            <Grid Grid.Row="0" Height="32" Background="Transparent"
                  PointerPressed="OnTitleBarPointerPressed">
                <TextBlock Text="My App" VerticalAlignment="Center" Margin="12,0" />
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,0,8,0">
                    <Button Content="—" Click="Minimize_Click" />
                    <Button Content="□" Click="Maximize_Click" />
                    <Button Content="×" Click="Close_Click" />
                </StackPanel>
            </Grid>

            <!-- 内容区域 -->
            <ContentPresenter Grid.Row="1" Content="{Binding Content}" />
        </Grid>
    </Border>
</Window>
```

### 32.4.2 自定义标题栏拖动

```csharp
public partial class RoundedWindow : Window
{
    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // 双击切换最大化
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                this.BeginMoveDrag(e);
            }
        }
    }

    private void Minimize_Click(object? sender, RoutedEventArgs e)
        => this.WindowState = WindowState.Minimized;

    private void Maximize_Click(object? sender, RoutedEventArgs e)
        => ToggleMaximize();

    private void ToggleMaximize()
    {
        this.WindowState = this.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
        => this.Close();
}
```

### 32.4.3 窗口调整大小（无边框）

无边框窗口需要自己处理边缘拖拽调整大小：

```csharp
// 方案 1：使用 ExtendClientAreaToDecorationsHint 保留系统调整大小能力
// 这是最简单的方案，系统会自动处理边缘拖拽

// 方案 2：手动实现（完全自定义）
public partial class RoundedWindow : Window
{
    private const int ResizeBorder = 8; // 边缘可拖拽区域宽度

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (this.WindowState == WindowState.Maximized) return;

        var pos = e.GetPosition(this);
        var size = this.Bounds.Size;

        var edge = GetResizeEdge(pos, size);
        this.Cursor = edge switch
        {
            WindowEdge.NorthWest => new Cursor(StandardCursorType.TopLeftCorner),
            WindowEdge.NorthEast => new Cursor(StandardCursorType.TopRightCorner),
            WindowEdge.SouthWest => new Cursor(StandardCursorType.BottomLeftCorner),
            WindowEdge.SouthEast => new Cursor(StandardCursorType.BottomRightCorner),
            WindowEdge.North     => new Cursor(StandardCursorType.TopSide),
            WindowEdge.South     => new Cursor(StandardCursorType.BottomSide),
            WindowEdge.West      => new Cursor(StandardCursorType.LeftSide),
            WindowEdge.East      => new Cursor(StandardCursorType.RightSide),
            _ => Cursor.Default
        };
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (this.WindowState == WindowState.Maximized) return;

        var pos = e.GetPosition(this);
        var edge = GetResizeEdge(pos, this.Bounds.Size);
        if (edge.HasValue)
        {
            this.BeginResizeDrag(edge.Value, e);
        }
    }

    private WindowEdge? GetResizeEdge(Point pos, Size size)
    {
        var left   = pos.X < ResizeBorder;
        var right  = pos.X > size.Width - ResizeBorder;
        var top    = pos.Y < ResizeBorder;
        var bottom = pos.Y > size.Height - ResizeBorder;

        if (top && left)   return WindowEdge.NorthWest;
        if (top && right)  return WindowEdge.NorthEast;
        if (bottom && left) return WindowEdge.SouthWest;
        if (bottom && right) return WindowEdge.SouthEast;
        if (top)    return WindowEdge.North;
        if (bottom) return WindowEdge.South;
        if (left)   return WindowEdge.West;
        if (right)  return WindowEdge.East;
        return null;
    }
}
```

## 32.5 窗口阴影

### 32.5.1 BoxShadow 实现阴影

```xml
<!-- 无边框窗口的阴影效果 -->
<Border CornerRadius="12"
        Background="#2D2D2D"
        BoxShadow="0 4 12 0 #40000000, 0 1 3 0 #20000000"
        Margin="16">
    <!-- BoxShadow 参数: offsetX offsetY blur spread color -->
    <!-- 多层阴影实现更自然的效果 -->
    <StackPanel Margin="20">
        <TextBlock Text="带阴影的圆角窗口" Foreground="White" />
    </StackPanel>
</Border>
```

### 32.5.2 动态阴影（根据窗口状态）

```xml
<Border CornerRadius="12" Background="#2D2D2D">
    <Border.Styles>
        <Style Selector="Border">
            <Setter Property="BoxShadow" Value="0 4 12 0 #40000000" />
        </Style>
        <!-- 窗口最大化时移除阴影（节省空间） -->
    </Border.Styles>
</Border>
```

```csharp
// 在 ViewModel 中根据窗口状态切换
[ObservableProperty]
private BoxShadows _windowShadow = new(BoxShadow.Parse("0 4 12 0 #40000000"));

partial void OnWindowStateChanged(WindowState value)
{
    WindowShadow = value == WindowState.Maximized
        ? BoxShadows.None
        : new BoxShadows(BoxShadow.Parse("0 4 12 0 #40000000"));
}
```

## 32.6 穿透点击

### 32.6.1 控件级穿透

```xml
<!-- IsHitTestVisible="False" 让控件不响应鼠标事件 -->
<Canvas>
    <Rectangle Width="100" Height="100" Fill="Red" />
    <!-- 这个矩形不会阻挡下面的控件接收点击 -->
    <Rectangle Width="50" Height="50" Fill="Blue" Opacity="0.5"
               IsHitTestVisible="False" />
</Canvas>
```

### 32.6.2 窗口级穿透（Windows）

让整个窗口或窗口的透明区域可以点击穿透：

```csharp
// Windows: 使用 WS_EX_TRANSPARENT 和 WS_EX_LAYERED
using System.Runtime.InteropServices;

public class ClickThroughHelper
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    /// <summary>
    /// 让窗口变为点击穿透
    /// </summary>
    public static void SetClickThrough(IntPtr hwnd, bool enable)
    {
        var style = GetWindowLong(hwnd, GWL_EXSTYLE);
        if (enable)
        {
            style |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
        }
        else
        {
            style &= ~WS_EX_TRANSPARENT;
        }
        SetWindowLong(hwnd, GWL_EXSTYLE, style);
    }
}
```

**应用场景**：悬浮状态条、屏幕标注工具、游戏叠加层。

## 32.7 窗口吸附与 Snap Layouts

### 32.7.1 Windows 11 Snap Layouts

Windows 11 的 Snap Layouts 功能需要系统原生标题栏支持。在 Avalonia 中，使用 `SystemDecorations="Full"` 保留原生标题栏即可自动支持：

```xml
<!-- 保留原生标题栏以支持 Snap Layouts -->
<Window SystemDecorations="Full"
        ExtendClientAreaToDecorationsHint="True"
        ExtendClientAreaTitleBarHeightHint="32">
    <!-- 自定义内容覆盖在原生标题栏上方 -->
    <!-- 同时保留 Snap Layouts 功能 -->
</Window>
```

### 32.7.2 自定义窗口吸附

```csharp
public class WindowSnapHelper
{
    /// <summary>
    /// 将窗口吸附到屏幕指定位置（类似 Windows 的 Win+方向键）
    /// </summary>
    public static void SnapTo(Window window, SnapPosition position)
    {
        var screen = window.Screens.ScreenFromWindow(window);
        if (screen == null) return;

        var workArea = screen.WorkingArea;
        var dpi = screen.Scaling;

        window.WindowState = WindowState.Normal;

        switch (position)
        {
            case SnapPosition.Left:
                window.Position = new PixelPoint((int)workArea.X, (int)workArea.Y);
                window.Width = workArea.Width / 2 / dpi;
                window.Height = workArea.Height / dpi;
                break;

            case SnapPosition.Right:
                window.Position = new PixelPoint(
                    (int)(workArea.X + workArea.Width / 2), (int)workArea.Y);
                window.Width = workArea.Width / 2 / dpi;
                window.Height = workArea.Height / dpi;
                break;

            case SnapPosition.TopLeft:
                window.Position = new PixelPoint((int)workArea.X, (int)workArea.Y);
                window.Width = workArea.Width / 2 / dpi;
                window.Height = workArea.Height / 2 / dpi;
                break;

            // ... 其他位置
        }
    }

    public enum SnapPosition
    {
        Left, Right, TopLeft, TopRight, BottomLeft, BottomRight, Top, Bottom
    }
}
```

## 32.8 多显示器管理

### 32.8.1 获取屏幕信息

```csharp
// 获取所有屏幕
var screens = this.Screens.All;
foreach (var screen in screens)
{
    Console.WriteLine($"屏幕: {screen.Name}");
    Console.WriteLine($"  工作区: {screen.WorkingArea}");
    Console.WriteLine($"  缩放: {screen.Scaling}");
    Console.WriteLine($"  主屏幕: {screen.IsPrimary}");
}

// 获取当前窗口所在的屏幕
var currentScreen = this.Screens.ScreenFromWindow(this);

// 获取鼠标所在的屏幕
var mousePos = this.PointToClient(MouseDevice.Instance.GetPosition(this));
var mouseScreen = this.Screens.ScreenFromPoint(mousePos);
```

### 32.8.2 窗口跨显示器移动

```csharp
public void MoveToNextScreen()
{
    var currentScreen = this.Screens.ScreenFromWindow(this);
    var allScreens = this.Screens.All.ToList();
    var currentIndex = allScreens.IndexOf(currentScreen);
    var nextIndex = (currentIndex + 1) % allScreens.Count;

    var nextScreen = allScreens[nextIndex];
    var targetArea = nextScreen.WorkingArea;

    // 保持窗口在屏幕中的相对位置
    var relativeX = (this.Position.X - currentScreen!.WorkingArea.X)
                     / currentScreen.WorkingArea.Width;
    var relativeY = (this.Position.Y - currentScreen.WorkingArea.Y)
                     / currentScreen.WorkingArea.Height;

    this.Position = new PixelPoint(
        (int)(targetArea.X + targetArea.Width * relativeX),
        (int)(targetArea.Y + targetArea.Height * relativeY));
}
```

### 32.8.3 窗口状态持久化

```csharp
public class WindowStatePersistence
{
    public record WindowPlacement(
        int X, int Y, int Width, int Height,
        WindowState State, string? ScreenName);

    public static void Save(Window window, string configPath)
    {
        var placement = new WindowPlacement(
            window.Position.X, window.Position.Y,
            (int)window.Width, (int)window.Height,
            window.WindowState,
            window.Screens.ScreenFromWindow(window)?.Name);

        var json = JsonSerializer.Serialize(placement);
        File.WriteAllText(configPath, json);
    }

    public static void Restore(Window window, string configPath)
    {
        if (!File.Exists(configPath)) return;

        var json = File.ReadAllText(configPath);
        var placement = JsonSerializer.Deserialize<WindowPlacement>(json);
        if (placement == null) return;

        // 检查保存的屏幕是否仍然存在
        var targetScreen = window.Screens.All
            .FirstOrDefault(s => s.Name == placement.ScreenName);

        if (targetScreen != null)
        {
            window.Position = new PixelPoint(placement.X, placement.Y);
            window.Width = placement.Width;
            window.Height = placement.Height;
            window.WindowState = placement.State;
        }
        else
        {
            // 屏幕已断开，居中于主屏幕
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}
```

## 32.9 Deep Dive: 透明窗口的渲染管线

当窗口设置为透明时，Avalonia 的渲染管线会发生以下变化：

1. **合成器模式**：操作系统使用 DWM（Desktop Window Manager）合成窗口
2. **Alpha 通道**：每个像素的 Alpha 通道决定透明度
3. **性能影响**：透明窗口比不透明窗口消耗更多 GPU 资源
4. **层级关系**：透明窗口的 Z-order 处理更复杂

**性能优化建议：**
- 只在需要时启用透明效果
- 避免在透明窗口上进行大量动画
- 使用 `ClipToBounds="True"` 减少需要混合的区域
- 在不需要透明时切换为不透明模式

## 32.10 Cross References

- **第 7 章**：样式与主题系统（毛玻璃效果需要配合主题切换）
- **第 10 章**：动画与过渡（窗口打开/关闭动画）
- **第 12 章**：多窗口与系统托盘（悬浮窗通常使用透明窗口）
- **第 20 章**：跨平台适配（不同平台的透明效果差异）

## 32.11 Common Pitfalls

1. **透明度不生效**：某些 Linux 窗口管理器不支持透明；需要检查 `ActualTransparencyLevel`
2. **圆角裁切**：`ClipToBounds="True"` 必须设置在正确的 Border 上
3. **阴影被裁切**：确保 Border 有足够的 Margin 来显示阴影
4. **无边框窗口无法调整大小**：需要手动实现边缘拖拽或使用 `ExtendClientAreaToDecorationsHint`
5. **Mica 在 Windows 10 上不工作**：Mica 是 Windows 11 专属，需要降级方案
6. **穿透点击导致无法操作窗口**：穿透区域需要精心设计，确保可交互部分仍然可以点击

## 32.12 Try It Yourself

1. 创建一个带圆角和阴影的无边框窗口，实现自定义标题栏拖动和调整大小
2. 实现一个窗口材质切换器，支持在 Mica/Acrylic/透明/不透明之间切换
3. 创建一个悬浮状态条窗口，使用穿透点击和透明效果
4. 实现窗口位置记忆功能，重启应用后恢复到上次的位置和大小
