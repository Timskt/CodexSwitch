# 22. Avalonia 属性系统

> **写给零基础的你**：属性系统是 Avalonia 的"地基"。你平时用的绑定、样式、动画，都是建立在这个地基上的。就像手机的操作系统——你用的是 App，但 App 能运行是因为有操作系统在底层支撑。

> **小白提示**：本章偏底层，初学者可以先跳过，等你对 Avalonia 有一定了解后再回来看。但如果你想做自定义控件，这章是必读的。

## 22.1 概述

Avalonia 的属性系统是整个框架的核心基础设施，它支撑着样式系统、数据绑定、动画、属性继承等关键功能。与 WPF 的依赖属性类似，Avalonia 的属性系统提供了高效的属性存储、变更通知、值优先级管理等能力。

**为什么需要学习属性系统：**
- 理解属性系统是掌握 Avalonia 开发的基础
- 自定义控件开发必须使用属性系统
- 性能优化需要了解属性的存储和访问机制
- 调试绑定问题需要理解属性值的来源和优先级

**应用场景：**
- 创建自定义控件的可绑定属性
- 实现样式系统的属性覆盖
- 开发动画系统
- 实现属性继承机制

## 22.2 属性系统的层次结构

> **小白提示**：Avalonia 有三种属性，就像三种不同功能的"盒子"：
> - **StyledProperty** = 智能盒子（支持样式、绑定、动画，功能最全，最常用）
> - **DirectProperty** = 快速盒子（更快，但不支持样式覆盖，适合内部使用）
> - **AttachedProperty** = 共享盒子（可以"借"给任何控件使用，比如 Grid.Row）

```
AvaloniaProperty (基类，所有属性的祖先)
├── StyledProperty<T>              — 智能属性（支持样式、绑定、动画，最常用）
├── DirectProperty<TOwner, TValue> — 直接属性（更快，但不支持样式覆盖）
└── AttachedProperty<T>            — 附加属性（可以添加到任何控件上）
```

### 22.2.1 AvaloniaProperty 基类

```csharp
// AvaloniaProperty 是所有属性的基类
public abstract class AvaloniaProperty
{
    // 属性标识符
    public int Id { get; }
    public string Name { get; }
    public Type OwnerType { get; }
    public Type PropertyType { get; }
    public AvaloniaProperty? Inherits { get; }

    // 属性值操作
    public abstract object? GetValue(AvaloniaObject target);
    public abstract void SetValue(AvaloniaObject target, object? value);
    public abstract void ClearValue(AvaloniaObject target);
    public abstract bool IsSet(AvaloniaObject target);

    // 属性值来源
    public abstract BindingPriority GetValueSource(AvaloniaObject target);
}
```

## 22.3 StyledProperty 详解

> **小白提示：什么是 StyledProperty？**  普通的 C# 属性就像一个"普通的盒子"，你放什么进去就是什么。StyledProperty 就像一个"智能盒子"——它不仅能存值，还能被样式覆盖、被数据绑定驱动、被动画控制。它是 Avalonia 能实现样式系统和数据绑定的基础。

StyledProperty 是最常用的属性类型，支持样式、绑定、动画等完整功能。

### 22.3.1 注册 StyledProperty

```csharp
// 基本注册
// Register<控件类型, 属性值类型>(属性名)
public static readonly StyledProperty<bool> IsSelectedProperty =
    AvaloniaProperty.Register<CsSegmentedButton, bool>(nameof(IsSelected));

// 带默认值
public static readonly StyledProperty<double> FontSizeProperty =
    AvaloniaProperty.Register<CsMyControl, double>(nameof(FontSize), 15d);

// 带验证器
public static readonly StyledProperty<int> MaxItemsProperty =
    AvaloniaProperty.Register<CsMyControl, int>(nameof(MaxItems),
        validate: value => value > 0);

// 带默认值和验证器
public static readonly StyledProperty<double> OpacityProperty =
    AvaloniaProperty.Register<CsMyControl, double>(nameof(Opacity), 1.0,
        validate: value => value >= 0.0 && value <= 1.0);
```

### 22.3.2 属性变更通知

```csharp
// 方式 1：静态类处理器（推荐）
static CsSegmentedButton()
{
    IsSelectedProperty.Changed.AddClassHandler<CsSegmentedButton>((button, args) =>
    {
        var oldValue = (bool)args.OldValue!;
        var newValue = (bool)args.NewValue!;

        // 更新伪类
        button.PseudoClasses.Set(":selected", newValue);

        // 触发视觉更新
        button.InvalidateVisual();

        // 自定义逻辑
        button.OnSelectionChanged(oldValue, newValue);
    });
}

// 方式 2：实例方法重写
protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    base.OnPropertyChanged(change);

    if (change.Property == IsSelectedProperty)
    {
        var newValue = change.NewValue as bool? ?? false;
        PseudoClasses.Set(":selected", newValue);
    }
    else if (change.Property == FontSizeProperty)
    {
        InvalidateMeasure();
    }
}

// 方式 3：partial method（CommunityToolkit.Mvvm）
[ObservableProperty]
private string _name;

partial void OnNameChanged(string value)
{
    // 属性变更后的逻辑
    Debug.WriteLine($"Name changed to: {value}");
}

partial void OnNameChanging(string value)
{
    // 属性变更前的验证
    if (string.IsNullOrEmpty(value))
        throw new ArgumentException("Name cannot be empty");
}
```

### 22.3.3 属性包装器

```csharp
// 为 StyledProperty 提供类型安全的包装器
public bool IsSelected
{
    get => GetValue(IsSelectedProperty);
    set => SetValue(IsSelectedProperty, value);
}

public double FontSize
{
    get => GetValue(FontSizeProperty);
    set => SetValue(FontSizeProperty, value);
}

public int MaxItems
{
    get => GetValue(MaxItemsProperty);
    set => SetValue(MaxItemsProperty, value);
}
```

### 22.3.4 StyledProperty 的完整示例

```csharp
public class CsRollingNumber : Control
{
    // 注册属性
    public static readonly StyledProperty<long> ValueProperty =
        AvaloniaProperty.Register<CsRollingNumber, long>(nameof(Value));

    public static readonly StyledProperty<bool> UseCompactFormatProperty =
        AvaloniaProperty.Register<CsRollingNumber, bool>(nameof(UseCompactFormat));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<CsRollingNumber, double>(nameof(FontSize), 15d);

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        AvaloniaProperty.Register<CsRollingNumber, FontWeight>(nameof(FontWeight), FontWeight.SemiBold);

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<CsRollingNumber, IBrush?>(nameof(Foreground), Brushes.White);

    // 静态构造函数中注册变更处理器
    static CsRollingNumber()
    {
        // 声明影响布局的属性
        AffectsMeasure<CsRollingNumber>(
            ValueProperty,
            UseCompactFormatProperty,
            FontSizeProperty,
            FontWeightProperty);

        // 声明影响渲染的属性
        AffectsRender<CsRollingNumber>(
            ValueProperty,
            UseCompactFormatProperty,
            FontSizeProperty,
            FontWeightProperty,
            ForegroundProperty);

        // 注册变更处理器
        ValueProperty.Changed.AddClassHandler<CsRollingNumber>((number, args) =>
        {
            if (args.NewValue is long value)
                number.OnValueChanged(value);
        });
    }

    // 属性包装器
    public long Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool UseCompactFormat
    {
        get => GetValue(UseCompactFormatProperty);
        set => SetValue(UseCompactFormatProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    // 变更处理逻辑
    private void OnValueChanged(long newValue)
    {
        if (!_hasValue)
        {
            SetImmediateValue(newValue);
            return;
        }

        if (newValue <= _displayValue)
        {
            SetImmediateValue(newValue);
            return;
        }

        _startValue = _displayValue;
        _targetValue = newValue;
        _animationStartedAt = DateTimeOffset.UtcNow;
        _animationDuration = ResolveDuration(newValue - _startValue);
        StartAnimation();
    }
}
```

## 22.4 DirectProperty 详解

DirectProperty 提供更快的属性访问，但不支持样式系统。

### 22.4.1 注册 DirectProperty

```csharp
// 基本注册
public static readonly DirectProperty<CsMyControl, string?> TextProperty =
    AvaloniaProperty.RegisterDirect<CsMyControl, string?>(
        nameof(Text),
        o => o.Text,
        (o, v) => o.Text = v);

// 带默认值
public static readonly DirectProperty<CsMyControl, int> CountProperty =
    AvaloniaProperty.RegisterDirect<CsMyControl, int>(
        nameof(Count),
        o => o.Count,
        (o, v) => o.Count = v,
        unsetValue: 0);

// 带变更通知
public static readonly DirectProperty<CsMyControl, string?> TextProperty =
    AvaloniaProperty.RegisterDirect<CsMyControl, string?>(
        nameof(Text),
        o => o.Text,
        (o, v) => o.Text = v,
        enableDataBinding: true);
```

### 22.4.2 使用 SetAndRaise

```csharp
private string? _text;
public string? Text
{
    get => _text;
    set => SetAndRaise(TextProperty, ref _text, value);
}

private int _count;
public int Count
{
    get => _count;
    set => SetAndRaise(CountProperty, ref _count, value);
}

// SetAndRaise 的作用：
// 1. 检查值是否真的改变了
// 2. 如果改变了，更新 backing field
// 3. 触发 PropertyChanged 事件
// 4. 通知所有绑定
```

### 22.4.3 StyledProperty vs DirectProperty 对比

| 特性 | StyledProperty | DirectProperty |
|------|---------------|----------------|
| 样式支持 | 是 | 否 |
| 绑定支持 | 是 | 是 |
| 动画支持 | 是 | 否 |
| 继承支持 | 是 | 否 |
| 性能 | 稍慢（需要查找值源） | 更快（直接访问字段） |
| 内存占用 | 更多（存储在稀疏数组） | 更少（存储在字段中） |
| 用途 | UI 属性、需要样式覆盖 | 内部状态、高频访问 |

### 22.4.4 何时使用 DirectProperty

```csharp
// 使用 DirectProperty 的场景：
// 1. 高频访问的属性（如动画中的值）
// 2. 不需要样式覆盖的内部状态
// 3. 性能关键路径

// 示例：动画值
public static readonly DirectProperty<AnimationControl, double> ProgressProperty =
    AvaloniaProperty.RegisterDirect<AnimationControl, double>(
        nameof(Progress),
        o => o.Progress,
        (o, v) => o.Progress = v);

private double _progress;
public double Progress
{
    get => _progress;
    set => SetAndRaise(ProgressProperty, ref _progress, value);
}
```

## 22.5 AttachedProperty 详解

AttachedProperty 可以附加到任何控件上，常用于布局系统。

### 22.5.1 注册 AttachedProperty

```csharp
// 基本注册
public static class GridEx
{
    public static readonly AttachedProperty<int> ColumnSpanProperty =
        AvaloniaProperty.RegisterAttached<Grid, Control, int>("ColumnSpan");

    // 静态 getter/setter
    public static void SetColumnSpan(Control control, int value)
    {
        control.SetValue(ColumnSpanProperty, value);
    }

    public static int GetColumnSpan(Control control)
    {
        return control.GetValue(ColumnSpanProperty);
    }
}

// 带默认值
public static class DockPanel
{
    public static readonly AttachedProperty<Dock> DockProperty =
        AvaloniaProperty.RegisterAttached<DockPanel, Control, Dock>("Dock", Dock.Left);

    public static void SetDock(Control control, Dock value)
    {
        control.SetValue(DockProperty, value);
    }

    public static Dock GetDock(Control control)
    {
        return control.GetValue(DockProperty);
    }
}
```

### 22.5.2 内置附加属性

```xml
<!-- Grid 的附加属性 -->
<Grid>
    <Border Grid.Row="0" Grid.Column="0" Grid.RowSpan="2" Grid.ColumnSpan="2"/>
    <Button Grid.Row="1" Grid.Column="1"/>
</Grid>

<!-- DockPanel 的附加属性 -->
<DockPanel>
    <Border DockPanel.Dock="Top" Height="50"/>
    <Border DockPanel.Dock="Left" Width="100"/>
    <TextBlock DockPanel.Dock="Bottom"/>
    <ContentControl/>  <!-- 填充剩余空间 -->
</DockPanel>

<!-- Canvas 的附加属性 -->
<Canvas>
    <Button Canvas.Left="10" Canvas.Top="20"/>
    <TextBlock Canvas.Left="50" Canvas.Top="50"/>
</Canvas>

<!-- ToolTip 的附加属性 -->
<Button ToolTip.Tip="Click me!"/>
```

### 22.5.3 自定义附加属性

```csharp
// 示例：为控件添加动画延迟
public static class AnimationHelper
{
    public static readonly AttachedProperty<double> DelayProperty =
        AvaloniaProperty.RegisterAttached<AnimationHelper, Control, double>("Delay", 0.0);

    public static void SetDelay(Control control, double value)
    {
        control.SetValue(DelayProperty, value);
    }

    public static double GetDelay(Control control)
    {
        return control.GetValue(DelayProperty);
    }

    // 可以添加变更处理器
    static AnimationHelper()
    {
        DelayProperty.Changed.AddClassHandler<Control>((control, args) =>
        {
            var delay = (double)args.NewValue!;
            // 应用动画延迟
            ApplyAnimationDelay(control, delay);
        });
    }
}
```

```xml
<!-- 使用自定义附加属性 -->
<Button local:AnimationHelper.Delay="0.5" Content="Animate me"/>
<Button local:AnimationHelper.Delay="1.0" Content="Animate me later"/>
```

## 22.6 属性优先级系统

### 22.6.1 优先级层次

Avalonia 属性值的优先级（从高到低）：

```
1. Animation（动画值）          — 最高优先级
2. Local（本地值）              — 直接设置的值
3. Style Trigger（样式触发器）  — 样式中的触发条件
4. Style（样式）                — 普通样式
5. Template（模板）             — ControlTemplate 中的值
6. Inherited（继承）            — 从父元素继承
7. Default（默认值）            — 注册时的默认值
```

### 22.6.2 查看属性值来源

```csharp
// 获取属性值的来源
var source = control.GetValueSource(MyProperty);

// 可能的来源：
switch (source)
{
    case BindingPriority.UnsetValue:
        Debug.WriteLine("属性未设置");
        break;
    case BindingPriority.LocalValue:
        Debug.WriteLine("本地值（直接设置）");
        break;
    case BindingPriority.Style:
        Debug.WriteLine("来自样式");
        break;
    case BindingPriority.Template:
        Debug.WriteLine("来自模板");
        break;
    case BindingPriority.Inherited:
        Debug.WriteLine("继承自父元素");
        break;
    case BindingPriority.Animation:
        Debug.WriteLine("来自动画");
        break;
}
```

### 22.6.3 优先级示例

```csharp
// 示例：理解优先级
var button = new Button();

// 1. 默认值
Debug.WriteLine(button.GetValue(Button.BackgroundProperty));  // 默认值

// 2. 样式值
// 在 AXAML 中：<Style Selector="Button"><Setter Property="Background" Value="Red"/></Style>
Debug.WriteLine(button.GetValue(Button.BackgroundProperty));  // Red

// 3. 本地值（最高优先级）
button.Background = Brushes.Blue;
Debug.WriteLine(button.GetValue(Button.BackgroundProperty));  // Blue

// 4. 动画值（最高优先级）
// 动画运行时，动画值会覆盖本地值
```

### 22.6.4 清除属性值

```csharp
// 清除本地值，恢复到样式值或默认值
button.ClearValue(Button.BackgroundProperty);

// 检查是否有本地值
var hasLocalValue = button.IsSet(Button.BackgroundProperty);
Debug.WriteLine($"Has local value: {hasLocalValue}");

// 获取所有设置的属性
var properties = button.GetSetProperties();
foreach (var prop in properties)
{
    Debug.WriteLine($"{prop.Name}: {button.GetValue(prop)}");
}
```

## 22.7 属性继承

### 22.7.1 可继承的属性

```csharp
// 可继承的属性会从父元素传播到子元素
// 常见的可继承属性：
// - FontFamily
// - FontSize
// - FontWeight
// - Foreground
// - FlowDirection
// - TextElement.FontSize
// - TextElement.FontWeight
```

### 22.7.2 创建可继承属性

```csharp
// 方式 1：使用 AddOwner 继承已有属性
public static readonly StyledProperty<FontFamily> FontFamilyProperty =
    TextBlock.FontFamilyProperty.AddOwner<CsMyControl>();

// 方式 2：创建新的可继承属性
public static readonly StyledProperty<string> ThemeProperty =
    AvaloniaProperty.Register<CsMyControl, string>(
        nameof(Theme),
        defaultValue: "default",
        inherits: true);  // 关键：设置 inherits: true
```

### 22.7.3 属性继承示例

```xml
<!-- 在父元素设置 FontFamily，子元素会继承 -->
<Window FontFamily="avares://App/Fonts/CustomFont#Custom Font">
    <StackPanel>
        <TextBlock Text="This uses Custom Font"/>  <!-- 继承 -->
        <Button Content="This also uses Custom Font"/>  <!-- 继承 -->

        <!-- 子元素可以覆盖继承的值 -->
        <TextBlock Text="This uses Arial" FontFamily="Arial"/>
    </StackPanel>
</Window>
```

### 22.7.4 属性继承的性能考虑

```csharp
// 属性继承有性能开销，因为需要沿树传播
// 最佳实践：
// 1. 只对真正需要继承的属性启用继承
// 2. 避免在深层嵌套中使用过多继承属性
// 3. 使用 Local 值覆盖继承值时，性能影响最小
```

## 22.8 AffectsMeasure、AffectsRender、AffectsParentMeasure

### 22.8.1 AffectsMeasure

```csharp
// AffectsMeasure 声明哪些属性变化时需要重新测量
static MyControl()
{
    // 这些属性变化时，控件需要重新测量
    AffectsMeasure<MyControl>(
        WidthProperty,
        HeightProperty,
        ContentProperty,
        PaddingProperty,
        MarginProperty);
}

// 什么时候需要 AffectsMeasure：
// - 属性影响控件的 DesiredSize
// - 属性影响控件的布局方式
// - 属性影响子控件的排列
```

### 22.8.2 AffectsRender

```csharp
// AffectsRender 声明哪些属性变化时需要重绘
static MyControl()
{
    // 这些属性变化时，控件需要重绘
    AffectsRender<MyControl>(
        ForegroundProperty,
        BackgroundProperty,
        BorderBrushProperty,
        BorderThicknessProperty);
}

// 什么时候需要 AffectsRender：
// - 属性影响控件的外观
// - 属性影响绘制的颜色、形状、大小
```

### 22.8.3 AffectsParentMeasure

> **注意**：`AffectsParentMeasure<T>` 和 `AffectsParentArrange<T>` 是 `Panel` 类的方法（不是 `Layoutable`），只能在继承自 `Panel` 的容器控件中使用。

```csharp
// AffectsParentMeasure 声明哪些属性变化时需要父控件重新测量
// 注意：此方法仅在 Panel 子类中可用
public class MyPanel : Panel
{
    static MyPanel()
    {
        // 这些属性变化时，父控件需要重新测量
        AffectsParentMeasure<MyPanel>(
            WidthProperty,
            HeightProperty,
            MarginProperty);
    }
}

// 什么时候需要 AffectsParentMeasure：
// - 属性变化会影响父控件的布局
// - 例如：子控件的 Margin 变化会影响父控件的排列
// - 典型场景：自定义面板中的子元素属性变化
```

### 22.8.4 完整示例

```csharp
public class CsRollingNumber : Control
{
    static CsRollingNumber()
    {
        // 声明影响布局的属性
        AffectsMeasure<CsRollingNumber>(
            ValueProperty,
            UseCompactFormatProperty,
            FontSizeProperty,
            FontWeightProperty);

        // 声明影响渲染的属性
        AffectsRender<CsRollingNumber>(
            ValueProperty,
            UseCompactFormatProperty,
            FontSizeProperty,
            FontWeightProperty,
            ForegroundProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // 测量逻辑
        var text = FormatValue((long)Math.Round(_displayValue), UseCompactFormat);
        var layout = CreateTextLayout(text);
        return new Size(
            Math.Ceiling(layout.Width),
            Math.Ceiling(layout.Height));
    }

    public override void Render(DrawingContext context)
    {
        // 渲染逻辑
        var text = FormatValue((long)Math.Round(_displayValue), UseCompactFormat);
        var layout = CreateTextLayout(text);
        var y = Math.Round((Bounds.Height - layout.Height) / 2d);
        layout.Draw(context, new Point(0, y));
    }
}
```

## 22.9 属性值的存储机制

### 22.9.1 稀疏数组存储

```csharp
// Avalonia 使用稀疏数组存储属性值
// 只存储有值的属性，节省内存

// 内部实现（简化）：
public class AvaloniaObject
{
    // 稀疏数组：只存储有值的属性
    private Dictionary<int, object?> _values = new();

    public object? GetValue(AvaloniaProperty property)
    {
        if (_values.TryGetValue(property.Id, out var value))
            return value;
        return property.GetDefaultValue();
    }

    public void SetValue(AvaloniaProperty property, object? value)
    {
        _values[property.Id] = value;
    }
}
```

### 22.9.2 值来源追踪

```csharp
// 每个属性值都有来源信息
public struct PropertyEntry
{
    public object? Value { get; }
    public BindingPriority Priority { get; }
    public object? Source { get; }  // 来源对象（如 Style、Animation）
}

// 获取属性值的来源
var source = control.GetValueSource(MyProperty);

// 获取所有设置的属性及其来源
var entries = control.GetLocalValueEnumerator();
while (entries.MoveNext())
{
    var entry = entries.Current;
    Debug.WriteLine($"{entry.Property.Name}: {entry.Value} (Priority: {entry.Priority})");
}
```

### 22.9.3 内存优化

```csharp
// Avalonia 的属性系统经过优化：
// 1. 使用 int 作为属性 ID，而非字符串
// 2. 稀疏数组只存储有值的属性
// 3. 默认值不存储在对象中
// 4. 使用对象池减少 GC 压力

// 内存使用示例：
// 一个有 100 个属性的控件，只有 10 个有值
// 稀疏数组只存储 10 个条目，而非 100 个
```

## 22.10 AvaloniaProperty 的类型系统

### 22.10.1 类型转换

```csharp
// Avalonia 支持类型转换器
[TypeConverter(typeof(ColorConverter))]
public struct Color { ... }

// 在 AXAML 中使用
<Border Background="Red"/>  <!-- 字符串 "Red" 自动转换为 Color -->

// 自定义类型转换器
public class MyTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo culture, object value)
    {
        if (value is string str)
        {
            return new MyType(str);
        }
        throw new ArgumentException($"Cannot convert {value} to MyType");
    }
}
```

### 22.10.2 类型验证

```csharp
// 注册时添加验证器
public static readonly StyledProperty<int> MaxItemsProperty =
    AvaloniaProperty.Register<CsMyControl, int>(nameof(MaxItems),
        validate: value => value > 0);

// 验证器会抛出异常
try
{
    control.SetValue(MaxItemsProperty, -1);  // 抛出 ArgumentException
}
catch (ArgumentException ex)
{
    Debug.WriteLine($"Validation failed: {ex.Message}");
}

// 复杂验证器
public static readonly StyledProperty<string> EmailProperty =
    AvaloniaProperty.Register<CsMyControl, string>(nameof(Email),
        validate: value =>
        {
            if (string.IsNullOrEmpty(value))
                return true;  // 允许空值
            return value.Contains("@") && value.Contains(".");
        });
```

### 22.10.3 泛型属性

```csharp
// Avalonia 支持泛型属性
public static readonly StyledProperty<T> ValueProperty<T>(string name, T defaultValue = default)
{
    return AvaloniaProperty.Register<CsMyControl, T>(name, defaultValue);
}

// 使用泛型属性
public static readonly StyledProperty<int> IntValueProperty = ValueProperty<int>("IntValue");
public static readonly StyledProperty<string> StringValueProperty = ValueProperty<string>("StringValue");
```

## 22.11 CodexSwitch 实战：属性系统应用

### 22.11.1 CsRollingNumber 的属性设计

```csharp
// CsRollingNumber.cs 中的属性设计展示：
// 1. 使用 StyledProperty 支持样式覆盖
// 2. 使用 AffectsMeasure/AffectsRender 优化性能
// 3. 使用变更处理器触发动画

public sealed class CsRollingNumber : Control
{
    // 属性注册
    public static readonly StyledProperty<long> ValueProperty =
        AvaloniaProperty.Register<CsRollingNumber, long>(nameof(Value));

    public static readonly StyledProperty<bool> UseCompactFormatProperty =
        AvaloniaProperty.Register<CsRollingNumber, bool>(nameof(UseCompactFormat));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<CsRollingNumber, double>(nameof(FontSize), 15d);

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        AvaloniaProperty.Register<CsRollingNumber, FontWeight>(nameof(FontWeight), FontWeight.SemiBold);

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<CsRollingNumber, IBrush?>(nameof(Foreground), Brushes.White);

    // 静态构造函数
    static CsRollingNumber()
    {
        // 声明影响布局的属性
        AffectsMeasure<CsRollingNumber>(
            ValueProperty,
            UseCompactFormatProperty,
            FontSizeProperty,
            FontWeightProperty);

        // 声明影响渲染的属性
        AffectsRender<CsRollingNumber>(
            ValueProperty,
            UseCompactFormatProperty,
            FontSizeProperty,
            FontWeightProperty,
            ForegroundProperty);

        // 注册变更处理器
        ValueProperty.Changed.AddClassHandler<CsRollingNumber>((number, args) =>
        {
            if (args.NewValue is long value)
                number.OnValueChanged(value);
        });
    }

    // 属性包装器
    public long Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    // ... 其他属性包装器
}
```

### 22.11.2 CsSegmentedControl 的属性使用

```csharp
// CsSegmentedControl.cs 中的属性使用：
// 使用视觉树遍历查找子控件的属性

private void TrackSegmentedButtons()
{
    var buttons = this.GetVisualDescendants()
        .OfType<CsSegmentedButton>()
        .Concat(this.GetLogicalDescendants().OfType<CsSegmentedButton>())
        .ToHashSet();

    // 监听子控件的属性变更
    foreach (var button in buttons)
    {
        if (!_trackedButtons.Add(button))
            continue;

        button.PropertyChanged += OnSegmentedButtonPropertyChanged;
    }
}

private void OnSegmentedButtonPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
{
    // 响应子控件的属性变更
    if (e.Property == CsSegmentedButton.IsSelectedProperty ||
        e.Property == BoundsProperty ||
        e.Property == IsVisibleProperty)
    {
        Dispatcher.UIThread.Post(() => UpdateSelectionPill(animate: true), DispatcherPriority.Render);
    }
}
```

## 22.12 最佳实践

### 22.12.1 属性注册最佳实践

```csharp
// 1. 使用 nameof 确保属性名一致
public static readonly StyledProperty<bool> IsSelectedProperty =
    AvaloniaProperty.Register<CsMyControl, bool>(nameof(IsSelected));

// 2. 为属性提供合理的默认值
public static readonly StyledProperty<double> FontSizeProperty =
    AvaloniaProperty.Register<CsMyControl, double>(nameof(FontSize), 15d);

// 3. 添加验证器确保值有效
public static readonly StyledProperty<int> MaxItemsProperty =
    AvaloniaProperty.Register<CsMyControl, int>(nameof(MaxItems),
        validate: value => value > 0);

// 4. 在静态构造函数中注册变更处理器
static CsMyControl()
{
    IsSelectedProperty.Changed.AddClassHandler<CsMyControl>((control, args) =>
    {
        // 处理变更
    });
}
```

### 22.12.2 性能优化最佳实践

```csharp
// 1. 使用 AffectsMeasure/AffectsRender 精确控制
static CsMyControl()
{
    AffectsMeasure<CsMyControl>(WidthProperty, HeightProperty);
    AffectsRender<CsMyControl>(ForegroundProperty);
}

// 2. 对高频访问的属性使用 DirectProperty
public static readonly DirectProperty<CsMyControl, double> ProgressProperty =
    AvaloniaProperty.RegisterDirect<CsMyControl, double>(
        nameof(Progress),
        o => o.Progress,
        (o, v) => o.Progress = v);

// 3. 避免在属性变更处理器中执行耗时操作
ValueProperty.Changed.AddClassHandler<CsMyControl>((control, args) =>
{
    // 错误示例：在变更处理器中执行耗时操作
    // Thread.Sleep(100);  // 不要这样做

    // 正确示例：使用异步操作
    Dispatcher.UIThread.Post(() => control.UpdateUI(), DispatcherPriority.Background);
});
```

### 22.12.3 调试最佳实践

```csharp
// 1. 使用 GetValueSource 检查属性值来源
var source = control.GetValueSource(MyProperty);
Debug.WriteLine($"Property source: {source}");

// 2. 使用 GetLocalValueEnumerator 检查所有设置的属性
var enumerator = control.GetLocalValueEnumerator();
while (enumerator.MoveNext())
{
    var entry = enumerator.Current;
    Debug.WriteLine($"{entry.Property.Name}: {entry.Value}");
}

// 3. 使用条件编译输出调试信息
#if DEBUG
ValueProperty.Changed.AddClassHandler<CsMyControl>((control, args) =>
{
    Debug.WriteLine($"Value changed: {args.OldValue} -> {args.NewValue}");
});
#endif
```

---

## Deep Dive：属性系统的内部实现

### 属性注册表

```csharp
// AvaloniaProperty 使用全局注册表
public class AvaloniaPropertyRegistry
{
    private readonly Dictionary<Type, Dictionary<string, AvaloniaProperty>> _registered = new();

    public AvaloniaProperty Register(Type ownerType, AvaloniaProperty property)
    {
        if (!_registered.ContainsKey(ownerType))
            _registered[ownerType] = new Dictionary<string, AvaloniaProperty>();

        _registered[ownerType][property.Name] = property;
        return property;
    }
}
```

### 属性值查找算法

```csharp
// 属性值查找的简化算法
object? FindValue(AvaloniaObject obj, AvaloniaProperty property)
{
    // 1. 检查动画值
    if (AnimationStore.TryGetValue(obj, property, out var animValue))
        return animValue;

    // 2. 检查本地值
    if (obj.TryGetLocalValue(property, out var localValue))
        return localValue;

    // 3. 检查样式值
    if (StyleStore.TryGetValue(obj, property, out var styleValue))
        return styleValue;

    // 4. 检查模板值
    if (TemplateStore.TryGetValue(obj, property, out var templateValue))
        return templateValue;

    // 5. 检查继承值
    if (property.Inherits && TryGetInheritedValue(obj, property, out var inheritedValue))
        return inheritedValue;

    // 6. 返回默认值
    return property.GetDefaultValue();
}
```

### 属性变更通知机制

```csharp
// 属性变更通知的内部实现
void NotifyPropertyChanged(AvaloniaObject obj, AvaloniaProperty property, object? oldValue, object? newValue)
{
    // 1. 触发类处理器
    if (_classHandlers.TryGetValue(property, out var handlers))
    {
        foreach (var handler in handlers)
            handler(obj, new AvaloniaPropertyChangedEventArgs(property, oldValue, newValue));
    }

    // 2. 触发实例处理器
    obj.OnPropertyChanged(new AvaloniaPropertyChangedEventArgs(property, oldValue, newValue));

    // 3. 通知绑定系统
    BindingSystem.NotifyPropertyChanged(obj, property, newValue);

    // 4. 通知样式系统
    StyleSystem.NotifyPropertyChanged(obj, property, newValue);
}
```

## Cross References

- [第 9 章 自定义控件开发](09-custom-controls.md) — StyledProperty 的使用
- [第 7 章 样式与主题系统](07-styling-theming.md) — 样式与属性的关系
- [第 10 章 动画与过渡效果](10-animation-transitions.md) — 动画与属性的关系
- [第 15 章 编译绑定与 AOT 发布](15-compiled-bindings.md) — 绑定系统
- [第 21 章 调试与诊断](21-debugging.md) — 属性调试技术
- [第 23 章 视觉树与逻辑树](23-visual-logical-tree.md) — 属性继承与树结构

## Common Pitfalls

1. **忘记调用 AffectsMeasure/AffectsRender**: 属性变化不会触发重绘或重排，导致 UI 不更新
2. **在属性变更回调中修改其他属性**: 可能导致布局循环或无限递归
3. **使用 DirectProperty 但需要样式支持**: DirectProperty 不支持样式覆盖
4. **属性注册时忘记设置默认值**: 导致属性值为 null 或类型默认值
5. **验证器抛出异常导致应用崩溃**: 验证器应返回 false 而非抛出异常
6. **属性继承导致意外行为**: 某些属性继承可能导致子控件样式异常
7. **属性值类型不匹配**: 设置错误类型的值会导致运行时错误
8. **忘记释放属性变更订阅**: 属性变更订阅会导致内存泄漏
9. **在属性变更处理器中执行耗时操作**: 会阻塞 UI 线程
10. **属性名拼写错误**: 编译时不会报错，运行时才会发现

## Try It Yourself

1. **基础练习**: 在 CodexSwitch 中找到 `CsRollingNumber`，研究它的 5 个 StyledProperty 的定义和使用

2. **自定义属性**: 创建一个自定义控件，包含 3 个 StyledProperty 和 1 个 DirectProperty，比较它们的性能

3. **附加属性**: 创建一个自定义附加属性，为控件添加动画延迟功能

4. **属性继承**: 创建一个可继承的属性，在父元素设置后观察子元素的继承行为

5. **属性优先级**: 创建一个页面，演示属性优先级（本地值 > 样式值 > 默认值）

6. **性能测试**: 比较 StyledProperty 和 DirectProperty 的访问性能

7. **调试练习**: 使用 GetValueSource 和 GetLocalValueEnumerator 检查属性值来源

8. **综合项目**: 实现一个主题系统，使用属性继承和 DynamicResource 实现运行时主题切换
