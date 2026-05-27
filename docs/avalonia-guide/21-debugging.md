# 21. 调试与诊断

> **写给零基础的你**：调试（Debug）就是"找虫子"。程序里的错误叫 Bug（虫子），找错误的过程叫 Debug（除虫）。本章教你用各种工具来"除虫"——看哪里出错了、为什么出错、怎么修。

## 21.1 概述

调试是软件开发中不可或缺的环节。Avalonia 作为跨平台 UI 框架，提供了丰富的调试和诊断工具，帮助开发者快速定位问题。本章将深入讲解 Avalonia 的调试体系，包括 DevTools、日志系统、绑定诊断、性能分析等核心技术。

**为什么需要学习调试技术：**
- Avalonia 的渲染管线比传统 WinForms 更复杂，需要专门的调试手段
- 数据绑定错误在编译时无法发现，只能在运行时捕获
- 跨平台开发时，不同平台的渲染行为可能有差异
- 性能优化需要精确的测量工具

**应用场景：**
- 开发阶段：快速发现 UI 布局问题、绑定错误
- 测试阶段：性能分析、内存泄漏检测
- 生产环境：错误日志收集、运行时诊断

## 21.2 Avalonia DevTools 详解

### 21.2.1 启动 DevTools

> **小白提示**：DevTools 就像"X 光机"。它能让你看到程序内部的结构——哪些控件在哪个位置、绑定了什么数据、样式是怎么应用的。就像医生用 X 光看人体内部一样，你用 DevTools 看程序内部。

DevTools 是 Avalonia 内置的运行时调试工具，类似于浏览器的开发者工具（按 F12 打开的那个）。

```csharp
// 方式 1：在 App.axaml.cs 中配置
public override void OnFrameworkInitializationCompleted()
{
#if DEBUG
    // 附加 DevTools，按 F12 打开
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.MainWindow = new MainWindow();
        AttachDevTools(desktop.MainWindow);
    }
#endif
}

// 方式 2：使用扩展方法（推荐）
#if DEBUG
this.AttachDevTools();
#endif

// 方式 3：在 Program.cs 中全局启用
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
#if DEBUG
        .With(new Avalonia.Diagnostics.DiagnosticsOptions
        {
            EnableDeveloperTools = true,
            ShowDevTools = true  // 启动时自动打开
        })
#endif
        ;
```

### 21.2.2 DevTools 功能面板

DevTools 提供以下核心功能面板：

| 面板 | 功能 | 使用场景 |
|------|------|----------|
| **Visual Tree** | 可视化控件层次结构 | 检查控件是否正确嵌套 |
| **Properties** | 实时查看/修改控件属性 | 调试布局、样式问题 |
| **Events** | 查看事件路由和处理 | 调试点击、键盘事件 |
| **Binding** | 查看绑定状态和错误 | 调试数据绑定 |
| **Styles** | 查看应用的样式规则 | 调试样式优先级 |
| **Layout** | 查看 Margin、Padding、Bounds | 调试布局问题 |
| **Console** | 执行表达式、查看输出 | 快速测试代码 |
| **FPS** | 帧率监视器 | 性能分析 |

### 21.2.3 视觉树检查

在 Visual Tree 面板中，你可以：

1. **展开/折叠控件层次**：点击节点展开，查看子控件
2. **选中控件**：点击节点，右侧面板显示该控件的属性
3. **高亮显示**：选中控件后，UI 中对应区域会高亮
4. **过滤**：按类型、名称过滤控件

```csharp
// 在代码中获取视觉树信息
var descendants = this.GetVisualDescendants().ToList();
Debug.WriteLine($"视觉树中共有 {descendants.Count} 个控件");

// 输出特定控件的信息
foreach (var control in descendants.OfType<Button>())
{
    Debug.WriteLine($"Button: {control.Name}, Bounds: {control.Bounds}, " +
                    $"IsVisible: {control.IsVisible}");
}
```

### 21.2.4 属性检查与修改

Properties 面板允许运行时修改属性：

```
// 在 DevTools 中修改属性
// 1. 选中控件
// 2. 在 Properties 面板找到属性
// 3. 直接修改值
// 4. UI 立即更新

// 属性分类：
// - Local Value: 直接设置的值
// - Style Value: 样式设置的值
// - Default Value: 默认值
// - Inherited Value: 继承的值
```

### 21.2.5 绑定诊断面板

Binding 面板是调试数据绑定的利器：

```
// 绑定状态类型：
// - Active: 绑定正常工作
// - Error: 绑定出错（属性不存在、类型不匹配等）
// - Inactive: 绑定未激活

// 常见绑定错误：
// 1. 属性名拼写错误
// 2. 类型不匹配
// 3. DataContext 为 null
// 4. 路径错误（多级属性访问）
```

## 21.3 日志系统详解

### 21.3.1 LogToTrace 配置

Avalonia 使用 Serilog 作为日志框架，通过 `LogToTrace` 输出到调试窗口：

```csharp
// Program.cs 中配置日志级别
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace(LogEventLevel.Information)  // 推荐级别
        ;

// 日志级别说明：
// LogEventLevel.Verbose    - 最详细，包含所有内部日志
// LogEventLevel.Debug      - 调试信息，开发阶段使用
// LogEventLevel.Information - 一般信息，推荐级别
// LogEventLevel.Warning    - 警告，生产环境推荐
// LogEventLevel.Error      - 错误
// LogEventLevel.Fatal      - 致命错误
```

### 21.3.2 自定义日志输出

```csharp
// 方式 1：使用 TraceSource
var traceSource = new TraceSource("MyApp");
traceSource.Switch.Level = SourceLevels.All;
traceSource.Listeners.Add(new ConsoleTraceListener());

// 方式 2：使用 ILogger（ASP.NET Core 风格）
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);
});

// 方式 3：自定义日志接收器
public class FileLogSink : ILogSink
{
    private readonly StreamWriter _writer;

    public FileLogSink(string path)
    {
        _writer = new StreamWriter(path, append: true);
    }

    public void Log(LogEventLevel level, string area, object? source, string message)
    {
        _writer.WriteLine($"[{level}] [{area}] {message}");
        _writer.Flush();
    }
}

// 在 AppBuilder 中注册
.Log(new FileLogSink("app.log"))
```

### 21.3.3 日志输出示例

```csharp
// 启用详细日志后，输出示例：
[Information] Avalonia: Application started
[Debug] Avalonia: Layout pass completed in 2.3ms
[Debug] Avalonia: Render pass completed in 1.8ms
[Warning] Binding: Error in binding to 'TextBlock.Text': 'Name' property not found on 'MyViewModel'
[Information] Avalonia: Window resized to 1200x800

// 绑定错误日志示例：
[Warning] Binding: Binding error: Path 'User.Name' on 'MainWindow' - property 'User' not found
[Error] Binding: Cannot convert 'System.Int32' to 'System.String' for binding to 'TextBlock.Text'
```

## 21.4 绑定错误调试

### 21.4.1 绑定错误类型

```csharp
// 类型 1：属性不存在
// AXAML: <TextBlock Text="{Binding NonExistentProperty}"/>
// 错误: [Binding] 'NonExistentProperty' property not found on 'MyViewModel'

// 类型 2：类型不匹配
// AXAML: <TextBlock Text="{Binding IntegerValue}"/>
// 错误: [Binding] Cannot convert 'System.Int32' to 'System.String'

// 类型 3：路径错误
// AXAML: <TextBlock Text="{Binding User.Address.City}"/>
// 错误: [Binding] 'User' property not found on 'MainWindowViewModel'

// 类型 4：DataContext 为 null
// AXAML: <TextBlock Text="{Binding Name}"/>
// 错误: [Binding] DataContext is null
```

### 21.4.2 调试转换器

```csharp
// 创建调试转换器，在绑定链中插入日志
public class DebugConverter : IValueConverter
{
    public static readonly DebugConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Debug.WriteLine($"[Binding Debug] Value: {value}, Type: {value?.GetType()?.Name}, " +
                       $"Target: {targetType.Name}, Parameter: {parameter}");
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Debug.WriteLine($"[Binding Debug] ConvertBack: {value}, Type: {value?.GetType()?.Name}");
        return value;
    }
}

// 在 AXAML 中使用
<TextBlock Text="{Binding Name, Converter={x:Static converters:DebugConverter.Instance}}"/>
```

### 21.4.3 绑定错误时中断

> **注意**：`Binding.Diagnostics` API 在 Avalonia 11.x 中引入，具体可用版本请参考官方文档。以下示例展示诊断绑定错误的思路。

```csharp
// 在 App.axaml.cs 中，绑定错误时自动中断
public override void OnFrameworkInitializationCompleted()
{
#if DEBUG
    // 方式 1：使用 BindingDiagnostic 事件
    Binding.Diagnostics.BindingDiagnostic += (sender, e) =>
    {
        if (e.Severity == BindingDiagnosticSeverity.Error)
        {
            Debug.WriteLine($"[Binding Error] {e.Message}");
            Debugger.Break();  // 自动中断到调试器
        }
    };

    // 方式 2：自定义诊断处理器
    Binding.Diagnostics.SetBindingDiagnosticHandler(new MyBindingDiagnosticHandler());
#endif
}

public class MyBindingDiagnosticHandler : IBindingDiagnosticHandler
{
    public void Handle(BindingDiagnostic diagnostic)
    {
        if (diagnostic.Severity == BindingDiagnosticSeverity.Error)
        {
            // 记录到文件
            File.AppendAllText("binding-errors.log",
                $"{DateTime.Now}: {diagnostic.Message}\n");

            // 在调试器中中断
            if (Debugger.IsAttached)
                Debugger.Break();
        }
    }
}
```

## 21.5 布局调试

### 21.5.1 查看布局信息

```csharp
// 方式 1：在 DevTools 中查看
// 选中控件 → Properties 面板 → Layout 分组
// 显示：Margin, Padding, Bounds, DesiredSize, ActualSize

// 方式 2：在代码中输出
private void DumpLayoutInfo(Control control)
{
    Debug.WriteLine($"Control: {control.GetType().Name}");
    Debug.WriteLine($"  Bounds: {control.Bounds}");
    Debug.WriteLine($"  DesiredSize: {control.DesiredSize}");
    Debug.WriteLine($"  Margin: {control.Margin}");
    Debug.WriteLine($"  Padding: {(control as Decorator)?.Padding}");
    Debug.WriteLine($"  IsVisible: {control.IsVisible}");
    Debug.WriteLine($"  Opacity: {control.Opacity}");
}

// 方式 3：监听布局更新
control.LayoutUpdated += (sender, args) =>
{
    Debug.WriteLine($"Layout updated: {control.Bounds}");
};
```

### 21.5.2 布局循环检测

```csharp
// 布局循环会导致性能问题，Avalonia 会输出警告：
// [Layout] Layout cycle detected! Measure passes: 101, Arrange passes: 101

// 常见原因：
// 1. 在 MeasureOverride 中修改影响布局的属性
protected override Size MeasureOverride(Size availableSize)
{
    // 错误示例：在测量中修改宽度
    Width = 100;  // 这会导致无限循环！
    return new Size(100, 100);
}

// 2. 在 ArrangeOverride 中修改影响布局的属性
protected override Size ArrangeOverride(Size finalSize)
{
    // 错误示例：在排列中修改边距
    Margin = new Thickness(10);  // 这会导致无限循环！
    return finalSize;
}

// 3. 绑定循环
// A.Width → B.Height → A.Width → ... 无限循环

// 解决方案：
// 1. 使用 SetAndRaise 避免重复赋值
// 2. 添加布局重入保护
// 3. 使用 Dispatcher.UIThread.Post 延迟更新
```

### 21.5.3 Bounds 可视化

```csharp
// 在 DevTools 中，选中控件后会显示 Bounds 矩形
// Bounds 包含：
// - X, Y: 相对于父控件的位置
// - Width, Height: 控件的实际尺寸

// 代码中获取 Bounds
var bounds = control.Bounds;
Debug.WriteLine($"位置: ({bounds.X}, {bounds.Y}), 尺寸: {bounds.Width}x{bounds.Height}");

// 获取屏幕坐标
var screenPoint = control.PointToScreen(new Point(0, 0));
Debug.WriteLine($"屏幕位置: ({screenPoint.X}, {screenPoint.Y})");
```

## 21.6 性能分析

### 21.6.1 帧率监控

```csharp
// 方式 1：使用 DevTools 的 FPS 监视器
// 按 F12 打开 DevTools → 切换到 FPS 面板

// 方式 2：代码中监控帧率
public class FpsMonitor
{
    private int _frameCount;
    private Stopwatch _stopwatch = new();
    private double _fps;

    public void Start()
    {
        _stopwatch.Start();
        CompositionTarget.Rendering += OnRendering;
    }

    public void Stop()
    {
        _stopwatch.Stop();
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        _frameCount++;
        if (_stopwatch.ElapsedMilliseconds >= 1000)
        {
            _fps = _frameCount * 1000.0 / _stopwatch.ElapsedMilliseconds;
            Debug.WriteLine($"FPS: {_fps:F1}");
            _frameCount = 0;
            _stopwatch.Restart();
        }
    }
}
```

### 21.6.2 渲染时间测量

```csharp
// 测量渲染帧时间
public class RenderTimeMeasurer
{
    private Stopwatch _stopwatch = new();
    private long _lastTick;

    public void Start()
    {
        CompositionTarget.Rendering += OnRendering;
        _lastTick = Stopwatch.GetTimestamp();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var currentTick = Stopwatch.GetTimestamp();
        var elapsed = (currentTick - _lastTick) * 1000.0 / Stopwatch.Frequency;
        _lastTick = currentTick;

        if (elapsed > 16.67)  // 超过 60fps
        {
            Debug.WriteLine($"Frame time: {elapsed:F2}ms (jank detected)");
        }
    }
}

// 测量特定操作的耗时
var sw = Stopwatch.StartNew();
// ... 执行操作
sw.Stop();
Debug.WriteLine($"操作耗时: {sw.ElapsedMilliseconds}ms");

// 测量布局耗时
control.LayoutUpdated += (sender, args) =>
{
    Debug.WriteLine($"Layout completed at {DateTime.Now:HH:mm:ss.fff}");
};
```

### 21.6.3 内存分析

```csharp
// 内存使用监控
public class MemoryMonitor
{
    private Timer _timer;

    public void Start()
    {
        _timer = new Timer(OnTimerTick, null, 0, 5000);  // 每 5 秒
    }

    private void OnTimerTick(object? state)
    {
        var memory = GC.GetTotalMemory(false);
        Debug.WriteLine($"内存使用: {memory / 1024 / 1024:F2} MB");
        Debug.WriteLine($"GC 次数: Gen0={GC.CollectionCount(0)}, " +
                       $"Gen1={GC.CollectionCount(1)}, Gen2={GC.CollectionCount(2)}");
    }
}

// 强制 GC 并检查
GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: false, compacting: true);
GC.WaitForPendingFinalizers();
var memory = GC.GetTotalMemory(true);
Debug.WriteLine($"GC 后内存: {memory / 1024 / 1024:F2} MB");
```

### 21.6.4 控件数量监控

```csharp
// 检查控件数量，发现内存泄漏
private void MonitorControlCount()
{
    var controls = this.GetVisualDescendants().ToList();
    Debug.WriteLine($"控件总数: {controls.Count}");

    // 按类型统计
    var groups = controls.GroupBy(c => c.GetType())
        .OrderByDescending(g => g.Count())
        .Take(10);

    foreach (var group in groups)
    {
        Debug.WriteLine($"  {group.Key.Name}: {group.Count()}");
    }
}

// 检查是否有控件未被释放
private void CheckForLeaks()
{
    var before = GC.GetTotalMemory(false);
    // 执行一些操作
    var after = GC.GetTotalMemory(false);
    var diff = after - before;
    if (diff > 1024 * 1024)  // 超过 1MB
    {
        Debug.WriteLine($"可能的内存泄漏: {diff / 1024}KB");
    }
}
```

## 21.7 诊断工具包

### 21.7.1 AvaloniaUI.DiagnosticsSupport

```xml
<!-- 在 .csproj 中添加诊断支持 -->
<PackageReference Include="AvaloniaUI.DiagnosticsSupport">
    <IncludeAssets Condition="'$(Configuration)' != 'Debug'">None</IncludeAssets>
    <PrivateAssets Condition="'$(Configuration)' != 'Debug'">All</PrivateAssets>
</PackageReference>
```

### 21.7.2 诊断工具功能

```csharp
// 诊断工具提供的功能：
// 1. 实时视觉树查看
// 2. 属性编辑器（运行时修改属性）
// 3. 布局调试（查看 Margin、Padding、Bounds）
// 4. 绑定日志（查看绑定错误和警告）
// 5. 样式检查（查看应用的样式和选择器）
// 6. 性能监视（FPS、渲染时间）

// 在代码中配置诊断选项
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new Avalonia.Diagnostics.DiagnosticsOptions
        {
            EnableDeveloperTools = true,
            ShowDevTools = false,  // 不自动打开
            EnableFramesPerSecondCounter = true,  // 启用 FPS 计数器
            EnablePointerOverlays = false  // 禁用指针覆盖层
        })
        ;
```

### 21.7.3 运行时诊断 API

```csharp
// 获取当前帧率
var fps = CompositionTarget.FrameCount;

// 监听每帧渲染事件（用于性能分析）
CompositionTarget.Rendering += (sender, e) =>
{
    // 每帧触发，可用于测量帧率
    Debug.WriteLine($"渲染时间: {e.RenderingTime}");
};
```

## 21.8 CodexSwitch 实战：调试技术应用

### 21.8.1 主题切换调试

来自 `AppThemeService.cs` 的实战代码，展示如何调试主题切换：

```csharp
// AppThemeService.cs 中的主题切换逻辑
public static void Apply(string? theme)
{
    var app = Application.Current;
    if (app is null)
        return;

    _theme = Normalize(theme);

    // 调试：输出主题切换信息
    Debug.WriteLine($"[Theme] Switching to: {_theme}");

    app.RequestedThemeVariant = _theme switch
    {
        "light" => ThemeVariant.Light,
        "dark" => ThemeVariant.Dark,
        _ => ThemeVariant.Default
    };

    EnsureSystemThemeListener(app);
    ApplyComponentLibraryTheme(app);
    ApplyBrushes(app);

    Debug.WriteLine($"[Theme] Applied. Actual theme: {app.ActualThemeVariant}");
}

// 调试主题资源应用
private static void ApplyBrush(Application app, string key, string colorText)
{
    var color = Color.Parse(colorText);

    // 调试：检查资源是否存在
    if (app.TryGetResource(key, app.ActualThemeVariant, out var resource))
    {
        Debug.WriteLine($"[Theme] Found resource: {key}, Type: {resource?.GetType().Name}");
        if (resource is SolidColorBrush brush)
        {
            Debug.WriteLine($"[Theme] Updating brush color: {brush.Color} -> {color}");
            brush.Color = color;
            return;
        }
    }
    else
    {
        Debug.WriteLine($"[Theme] Resource not found: {key}, creating new");
    }

    app.Resources[key] = new SolidColorBrush(color);
}
```

### 21.8.2 自定义控件调试

来自 `CsRollingNumber.cs` 的调试技巧：

```csharp
// CsRollingNumber.cs 中的动画调试
private void OnAnimationTick(object? sender, EventArgs e)
{
    var progress = GetAnimationProgress();

    // 调试：输出动画进度
    Debug.WriteLine($"[Animation] Progress: {progress:F3}, " +
                   $"Display: {_displayValue:F1}, Target: {_targetValue}");

    if (progress >= 1d)
    {
        _displayValue = _targetValue;
        StopAnimation();
        Debug.WriteLine("[Animation] Completed");
    }
    else
    {
        _displayValue = _startValue + (_targetValue - _startValue) * EaseOutCubic(progress);
    }

    InvalidateMeasure();
    InvalidateVisual();
}

// 调试渲染
public override void Render(DrawingContext context)
{
    base.Render(context);

    var text = FormatValue((long)Math.Round(_displayValue), UseCompactFormat);

    #if DEBUG
    // 调试：输出渲染信息
    if (_animationTimer is not null)
    {
        Debug.WriteLine($"[Render] Text: {text}, Bounds: {Bounds}");
    }
    #endif

    // ... 渲染逻辑
}
```

### 21.8.3 视觉树遍历调试

来自 `CsSegmentedControl.cs` 的视觉树调试：

```csharp
// CsSegmentedControl.cs 中的视觉树遍历
private void TrackSegmentedButtons()
{
    var buttons = this.GetVisualDescendants()
        .OfType<CsSegmentedButton>()
        .Concat(this.GetLogicalDescendants().OfType<CsSegmentedButton>())
        .ToHashSet();

    // 调试：输出找到的按钮
    Debug.WriteLine($"[VisualTree] Found {buttons.Count} segmented buttons");
    foreach (var button in buttons)
    {
        Debug.WriteLine($"  Button: {button.Name}, " +
                       $"IsSelected: {button.IsSelected}, " +
                       $"Bounds: {button.Bounds}");
    }

    // ... 后续逻辑
}

// 调试坐标转换
private void UpdateSelectionPill(bool animate)
{
    // ...
    var topLeft = selected.TranslatePoint(new Point(0, 0), _selectionLayer);
    if (topLeft is null)
    {
        Debug.WriteLine("[VisualTree] TranslatePoint returned null");
        return;
    }

    Debug.WriteLine($"[VisualTree] Pill position: ({topLeft.Value.X}, {topLeft.Value.Y})");
    // ...
}
```

## 21.9 常见运行时错误与解决方案

### 21.9.1 绑定错误

```csharp
// 错误 1：属性不存在
// 症状：输出窗口显示 [Binding] 'PropertyName' not found
// 解决：检查属性名拼写，确保 DataContext 正确设置

// 错误 2：类型转换失败
// 症状：[Binding] Cannot convert type
// 解决：使用 IValueConverter 或修改属性类型

// 错误 3：空引用异常
// 症状：NullReferenceException 在绑定路径中
// 解决：使用空条件运算符或检查 DataContext
```

### 21.9.2 布局错误

```csharp
// 错误 1：布局循环
// 症状：UI 卡顿，输出 Layout cycle detected
// 解决：避免在 MeasureOverride/ArrangeOverride 中修改属性

// 错误 2：无限尺寸
// 症状：控件尺寸无限增长
// 解决：设置 MaxWidth/MaxHeight 或使用 ScrollViewer

// 错误 3：控件不显示
// 症状：控件存在但不可见
// 解决：检查 IsVisible、Opacity、Bounds
```

### 21.9.3 渲染错误

```csharp
// 错误 1：渲染异常
// 症状：输出窗口显示渲染错误
// 解决：检查 Render 方法中的绘图代码

// 错误 2：性能问题
// 症状：FPS 低，UI 卡顿
// 解决：减少布局重算，使用缓存，避免过度绘制

// 错误 3：资源未找到
// 症状：[Resource] Resource not found
// 解决：检查资源键拼写，确保资源已加载
```

## 21.10 最佳实践

### 21.10.1 开发阶段

```csharp
// 1. 始终启用详细日志
.LogToTrace(LogEventLevel.Debug)

// 2. 使用条件编译
#if DEBUG
this.AttachDevTools();
#endif

// 3. 使用调试转换器排查绑定问题

// 4. 定期检查控件数量，防止内存泄漏
```

### 21.10.2 测试阶段

```csharp
// 1. 使用性能分析工具测量帧率
// 2. 检查内存使用趋势
// 3. 测试边界条件（大量数据、快速操作）
// 4. 在不同平台上测试
```

### 21.10.3 生产环境

```csharp
// 1. 使用 Warning 或 Error 日志级别
.LogToTrace(LogEventLevel.Warning)

// 2. 记录错误到文件
// 3. 监控内存使用
// 4. 收集崩溃报告
```

---

## Deep Dive：Avalonia 渲染管线调试

### 渲染帧分析

```
Avalonia 渲染管线：
1. Input（输入处理）
2. Layout（布局计算）
   - Measure: 自底向上计算所需空间
   - Arrange: 自顶向下分配空间
3. Render（渲染）
   - 遍历视觉树
   - 调用每个控件的 Render 方法
4. Composite（合成）
   - 将渲染结果输出到屏幕

// 每帧的详细时间：
[Render] Frame 1234:
  Input: 0.1ms
  Measure: 0.5ms
  Arrange: 0.3ms
  Render: 2.1ms
  Composite: 0.2ms
  Total: 3.2ms
```

### 渲染优化技巧

```csharp
// 1. 减少布局重算
// 使用 AffectsMeasure/AffectsRender 精确控制
static MyControl()
{
    AffectsMeasure<MyControl>(WidthProperty, HeightProperty);
    AffectsRender<MyControl>(ForegroundProperty);
}

// 2. 使用缓存
private FormattedText? _cachedText;
private string? _lastText;

public override void Render(DrawingContext context)
{
    if (_cachedText is null || _lastText != Text)
    {
        _cachedText = new FormattedText(Text, ...);
        _lastText = Text;
    }
    context.DrawText(_cachedText, new Point(0, 0));
}

// 3. 避免过度绘制
// 使用 ClipToBounds 限制绘制区域
// 使用 OpacityMask 实现复杂效果
```

## Cross References

- [第 15 章 编译绑定与 AOT 发布](15-compiled-bindings.md) — 绑定和性能优化
- [第 22 章 Avalonia 属性系统](22-property-system.md) — 属性变更机制
- [第 23 章 视觉树与逻辑树](23-visual-logical-tree.md) — 树结构
- [第 24 章 资源系统](24-resource-system.md) — 资源查找与调试
- [第 10 章 动画与过渡效果](10-animation-transitions.md) — 动画性能分析

## Common Pitfalls

1. **不在 Release 模式下测试**: 某些问题只在 Release 模式下出现（如 AOT 兼容性），务必在 Release 模式下测试
2. **忽略绑定警告**: 绑定警告可能导致性能问题，应尽早修复
3. **过度使用 Debug.WriteLine**: 生产代码中不要保留调试输出，使用条件编译
4. **忘记释放事件订阅**: 事件订阅会导致内存泄漏，确保在 Dispose 中取消订阅
5. **在 UI 线程执行耗时操作**: 会导致 UI 卡顿，使用异步操作
6. **不检查 DataContext**: 绑定时 DataContext 可能为 null，使用空条件运算符
7. **布局循环未检测**: 在 MeasureOverride/ArrangeOverride 中修改属性会导致无限循环
8. **内存泄漏未发现**: 定期检查控件数量和内存使用
9. **跨平台差异未测试**: 不同平台的渲染行为可能有差异
10. **性能瓶颈未识别**: 使用性能分析工具定位瓶颈

## Try It Yourself

1. **基础练习**: 在 CodexSwitch 中启用 `LogToTrace(LogEventLevel.Debug)`，观察日志输出，记录绑定错误

2. **DevTools 练习**: 按 F12 打开 DevTools，检查 MainWindow 的视觉树结构，找到 5 个关键控件并记录其属性

3. **绑定调试**: 故意拼错一个绑定属性名，观察错误日志，使用调试转换器定位问题

4. **性能分析**: 使用 FPS 监视器测量滚动列表的帧率，优化到 60fps

5. **内存检查**: 创建一个包含 1000 个控件的页面，监控内存使用，找出内存泄漏

6. **布局调试**: 创建一个复杂的 Grid 布局，使用 DevTools 检查每个控件的 Bounds

7. **渲染优化**: 实现一个自定义控件，使用 AffectsRender 优化重绘性能

8. **综合项目**: 实现一个简单的性能监控面板，显示 FPS、内存使用、控件数量
