# 21. 调试与诊断

## 21.1 AvaloniaUI.DiagnosticsSupport

这是 Avalonia 官方的诊断工具包，只在 Debug 模式下包含：

```xml
<PackageReference Include="AvaloniaUI.DiagnosticsSupport">
    <IncludeAssets Condition="'$(Configuration)' != 'Debug'">None</IncludeAssets>
    <PrivateAssets Condition="'$(Configuration)' != 'Debug'">All</PrivateAssets>
</PackageReference>
```

### 诊断工具功能

运行时按 **F12** 打开诊断工具，提供：

| 功能 | 说明 |
|------|------|
| 视觉树检查 | 实时查看控件层次结构 |
| 属性编辑器 | 运行时修改控件属性 |
| 布局调试 | 查看 Margin、Padding、Bounds |
| 绑定日志 | 查看绑定错误和警告 |
| 样式检查 | 查看应用的样式和选择器 |
| 性能监视 | FPS、渲染时间 |

## 21.2 日志输出

```csharp
// Program.cs - 将 Avalonia 内部日志输出到 Trace
.LogToTrace()

// 日志级别控制
.LogToTrace(LogEventLevel.Debug)  // 详细日志
.LogToTrace(LogEventLevel.Warning)  // 只显示警告和错误
```

### 绑定错误日志

绑定错误会输出到调试窗口：

```
[Binding] Error in binding to 'TextBlock.Text': 'PropertyName' property not found on 'MyViewModel'
```

## 21.3 调试绑定

### 方法 1：查看输出窗口

在 Visual Studio 或 Rider 的 Output 窗口中查看绑定错误。

### 方法 2：使用诊断工具

按 F12 打开诊断工具，切换到 "Binding" 标签页查看所有绑定状态。

### 方法 3：添加调试转换器

```csharp
public class DebugConverter : IValueConverter
{
    public static readonly DebugConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Debug.WriteLine($"[Binding] Value: {value}, Type: {value?.GetType()}, Target: {targetType}");
        return value;
    }
}
```

```xml
<TextBlock Text="{Binding Name, Converter={x:Static converters:DebugConverter.Instance}}"/>
```

## 21.4 性能分析

### 渲染性能

```csharp
// 测量操作耗时
var sw = Stopwatch.StartNew();
// ... 操作
sw.Stop();
Trace.WriteLine($"操作耗时: {sw.ElapsedMilliseconds}ms");
```

### 内存分析

```csharp
// 强制 GC
GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: false, compacting: true);

// 检查内存使用
var memory = GC.GetTotalMemory(true);
Debug.WriteLine($"内存使用: {memory / 1024 / 1024}MB");

// 检查对象数量
var gen0 = GC.CollectionCount(0);
var gen1 = GC.CollectionCount(1);
var gen2 = GC.CollectionCount(2);
Debug.WriteLine($"GC 次数: Gen0={gen0}, Gen1={gen1}, Gen2={gen2}");
```

## 21.5 视觉树调试

```csharp
// 遍历视觉树并输出
private void DumpVisualTree(Visual visual, int depth = 0)
{
    var indent = new string(' ', depth * 2);
    Debug.WriteLine($"{indent}{visual.GetType().Name} - {visual.Bounds} - Visible: {visual.IsVisible}");

    foreach (var child in visual.GetVisualChildren())
    {
        DumpVisualTree(child, depth + 1);
    }
}

// 使用
DumpVisualTree(this);
```

### 查找泄漏的控件

```csharp
// 检查是否有控件未被释放
var controls = this.GetVisualDescendants().ToList();
Debug.WriteLine($"控件总数: {controls.Count}");

// 检查特定类型
var buttons = controls.OfType<Button>().ToList();
Debug.WriteLine($"按钮数量: {buttons.Count}");
```

## 21.6 断点调试技巧

### 在绑定错误时中断

```csharp
// 在 App.axaml.cs 中
public override void OnFrameworkInitializationCompleted()
{
#if DEBUG
    // 绑定错误时中断
    Binding.Diagnostics.BindingDiagnostic += (sender, e) =>
    {
        if (e.Severity == BindingDiagnosticSeverity.Error)
            Debugger.Break();
    };
#endif
}
```

### 在属性变更时中断

```csharp
// 在 ViewModel 中
[ObservableProperty]
private string _name;

partial void OnNameChanged(string value)
{
#if DEBUG
    Debug.WriteLine($"Name changed to: {value}");
    // Debugger.Break();  // 取消注释以在每次变更时中断
#endif
}
```

---

## Deep Dive：Avalonia 的渲染管线调试

### 渲染帧分析

```csharp
// 启用渲染统计
LogToTrace(LogEventLevel.Information);

// 输出示例：
// [Render] Frame 1234: Measure=0.5ms, Arrange=0.3ms, Render=2.1ms, Total=2.9ms
```

### 布局循环检测

如果看到 "Layout cycle detected" 警告，说明布局在无限循环：

```
[Layout] Layout cycle detected! Measure passes: 101, Arrange passes: 101
```

这通常是因为：
1. 在 `MeasureOverride` 中修改了影响布局的属性
2. 在 `ArrangeOverride` 中修改了影响布局的属性
3. 绑定循环导致布局不断失效

## Cross References

- [第 15 章 编译绑定与 AOT 发布](15-compiled-bindings.md) — 绑定和性能优化
- [第 22 章 Avalonia 属性系统](22-property-system.md) — 属性变更机制
- [第 23 章 视觉树与逻辑树](23-visual-logical-tree.md) — 树结构

## Common Pitfalls

1. **不在 Release 模式下测试**: 某些问题只在 Release 模式下出现（如 AOT 兼容性）
2. **忽略绑定警告**: 绑定警告可能导致性能问题
3. **过度使用 Debug.WriteLine**: 生产代码中不要保留调试输出

## Try It Yourself

1. 在 CodexSwitch 中启用 `LogToTrace(LogEventLevel.Debug)`，观察日志输出
2. 使用诊断工具检查 MainWindow 的视觉树结构
3. 故意拼错一个绑定属性名，观察错误日志
