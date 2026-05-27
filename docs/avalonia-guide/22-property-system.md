# 22. Avalonia 属性系统

## 22.1 属性系统概览

Avalonia 的属性系统是整个框架的核心，它支持：
- 样式系统
- 数据绑定
- 动画
- 属性继承
- 默认值
- 变更通知
- 验证

### 属性系统的层次

```
AvaloniaProperty (基类)
├── StyledProperty      — 支持样式、绑定、动画
├── DirectProperty      — 直接属性，更快但不支持样式
└── AttachedProperty    — 附加属性，可添加到任何控件
```

## 22.2 StyledProperty

最常见的属性类型，支持样式、绑定、动画：

```csharp
public static readonly StyledProperty<bool> IsSelectedProperty =
    AvaloniaProperty.Register<CsSegmentedButton, bool>(nameof(IsSelected));

// 带默认值
public static readonly StyledProperty<double> FontSizeProperty =
    AvaloniaProperty.Register<CsMyControl, double>(nameof(FontSize), 15d);

// 带验证
public static readonly StyledProperty<int> MaxItemsProperty =
    AvaloniaProperty.Register<CsMyControl, int>(nameof(MaxItems),
        validate: value => value > 0);
```

### 变更通知

```csharp
static CsSegmentedButton()
{
    IsSelectedProperty.Changed.AddClassHandler<CsSegmentedButton>((button, args) =>
    {
        var oldValue = (bool)args.OldValue!;
        var newValue = (bool)args.NewValue!;
        button.PseudoClasses.Set(":selected", newValue);
    });
}
```

### 属性变更回调的三种方式

```csharp
// 方式 1：静态类处理器（推荐）
MyProperty.Changed.AddClassHandler<CsMyControl>((control, args) =>
{
    control.OnMyPropertyChanged(args);
});

// 方式 2：实例方法重写
protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    base.OnPropertyChanged(change);
    if (change.Property == MyProperty)
    {
        // 处理变更
    }
}

// 方式 3：partial method（CommunityToolkit.Mvvm）
[ObservableProperty]
private string _name;

partial void OnNameChanged(string value) { }
partial void OnNameChanging(string value) { }
```

## 22.3 DirectProperty

直接属性，性能更好，但不支持样式和动画：

```csharp
public static readonly DirectProperty<CsMyControl, string?> TextProperty =
    AvaloniaProperty.RegisterDirect<CsMyControl, string?>(
        nameof(Text),
        o => o.Text,
        (o, v) => o.Text = v);

private string? _text;
public string? Text
{
    get => _text;
    set => SetAndRaise(TextProperty, ref _text, value);
}
```

### StyledProperty vs DirectProperty

| 特性 | StyledProperty | DirectProperty |
|------|---------------|----------------|
| 样式支持 | 是 | 否 |
| 绑定支持 | 是 | 是 |
| 动画支持 | 是 | 否 |
| 继承支持 | 是 | 否 |
| 性能 | 稍慢 | 更快 |
| 内存占用 | 更多 | 更少 |
| 用途 | UI 属性 | 内部状态 |

## 22.4 AttachedProperty

附加属性，可以附加到任何控件上：

```csharp
public static class GridEx
{
    public static readonly AttachedProperty<int> ColumnSpanProperty =
        AvaloniaProperty.RegisterAttached<Grid, Control, int>("ColumnSpan");

    public static void SetColumnSpan(Control control, int value)
    {
        control.SetValue(ColumnSpanProperty, value);
    }

    public static int GetColumnSpan(Control control)
    {
        return control.GetValue(ColumnSpanProperty);
    }
}
```

```xml
<Grid>
    <Border local:GridEx.ColumnSpan="2"/>
</Grid>
```

### 内置附加属性

```xml
<!-- Grid 的附加属性 -->
<Border Grid.Row="1" Grid.Column="2" Grid.RowSpan="2"/>

<!-- DockPanel 的附加属性 -->
<Border DockPanel.Dock="Top"/>

<!-- Canvas 的附加属性 -->
<Border Canvas.Left="10" Canvas.Top="20"/>
```

## 22.5 属性优先级

Avalonia 属性值的优先级（从高到低）：

1. **动画** — 最高优先级
2. **本地值** — 直接设置的值
3. **样式触发器** — 样式中的触发条件
4. **样式** — 普通样式
5. **模板** — ControlTemplate 中的值
6. **继承** — 从父元素继承
7. **默认值** — 注册时的默认值

```csharp
// 获取属性值的来源
var source = control.GetValueSource(MyProperty);
// 返回：Local, Style, Template, Inherited, Unset
```

## 22.6 属性继承

```csharp
// 从父元素继承属性值
public static readonly StyledProperty<FontFamily> FontFamilyProperty =
    TextBlock.FontFamilyProperty.AddOwner<CsMyControl>();
```

这使得在父元素上设置 `FontFamily` 会自动传播到所有子元素。

### 可继承的属性

- `FontFamily`
- `FontSize`
- `FontWeight`
- `Foreground`
- `FlowDirection`

## 22.7 AffectsMeasure 和 AffectsRender

```csharp
static CsMyControl()
{
    // 这些属性变化时需要重新测量
    AffectsMeasure<CsMyControl>(WidthProperty, HeightProperty, ContentProperty);

    // 这些属性变化时需要重绘
    AffectsRender<CsMyControl>(ForegroundProperty, BackgroundProperty);
}
```

### 何时使用哪个

| 方法 | 用途 | 性能影响 |
|------|------|---------|
| `AffectsMeasure` | 尺寸相关属性 | 高（触发布局重算） |
| `AffectsRender` | 外观相关属性 | 中（触发重绘） |
| 都不调用 | 不影响布局或外观 | 无 |

---

## Deep Dive：属性值的存储机制

Avalonia 的属性系统使用高效的存储机制：

```csharp
// 属性值存储在稀疏数组中
// 只存储有值的属性，节省内存

// 获取属性值
var value = control.GetValue(MyProperty);

// 设置属性值
control.SetValue(MyProperty, value);

// 清除属性值（恢复默认）
control.ClearValue(MyProperty);

// 检查是否有本地值
var hasLocalValue = control.IsSet(MyProperty);
```

### 属性值的来源追踪

```csharp
// 获取属性值的来源
var source = control.GetValueSource(MyProperty);

// 可能的来源：
// - Unset: 未设置
// - Local: 直接设置
// - Style: 来自样式
// - Template: 来自模板
// - Inherited: 继承自父元素
// - Animation: 来自动画
```

## Cross References

- [第 9 章 自定义控件开发](09-custom-controls.md) — StyledProperty 的使用
- [第 7 章 样式与主题系统](07-styling-theming.md) — 样式与属性的关系
- [第 10 章 动画与过渡效果](10-animation-transitions.md) — 动画与属性的关系

## Common Pitfalls

1. **忘记调用 AffectsMeasure/AffectsRender**: 属性变化不会触发重绘或重排
2. **在属性变更回调中修改其他属性**: 可能导致布局循环
3. **使用 DirectProperty 但需要样式支持**: DirectProperty 不支持样式

## Try It Yourself

1. 在 CodexSwitch 中找到 `CsRollingNumber`，研究它的 `StyledProperty` 定义
2. 创建一个自定义附加属性，为控件添加额外功能
3. 尝试使用 `GetValueSource` 检查属性值的来源
