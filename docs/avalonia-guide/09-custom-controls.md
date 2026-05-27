# 9. 自定义控件开发

CodexSwitch 包含 45+ 自定义控件，从简单的样式包装器到复杂的自定义渲染控件。

## 9.1 控件类型层次

```
Control                    # 最基础的控件
├── TemplatedControl       # 支持模板的控件
│   ├── ContentControl     # 单内容容器
│   │   ├── Window
│   │   ├── UserControl
│   │   └── CsDialog
│   ├── HeaderedContentControl
│   ├── ItemsControl       # 集合容器
│   │   ├── ListBox
│   │   └── ComboBox
│   └── RangeBase
├── InputElement           # 支持输入的控件
│   ├── Control
│   │   ├── TextBox
│   │   └── Button
│   │       └── CsSegmentedButton
│   └── ScrollViewer
└── Visual                 # 可视化基类
    └── Layoutable
        └── Control
```

## 9.2 样式包装器控件（最简模式）

大多数 CodexSwitch 控件是"样式包装器"——不添加逻辑，只为样式选择器提供唯一类型名：

```csharp
// CsBadge.cs - 仅用于在 AXAML 中写 Style Selector="ui|CsBadge"
public class CsBadge : ContentControl { }

// CsCard.cs
public class CsCard : ContentControl { }

// CsSection.cs - 带自定义属性的样式包装器
public class CsSection : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<CsSection, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<CsSection, string?>(nameof(Description));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}
```

### 为什么需要包装器？

直接对 `ContentControl` 写样式会影响所有 ContentControl。包装器让你精确控制：

```xml
<!-- 只影响 CsBadge，不影响其他 ContentControl -->
<Style Selector="ui|CsBadge">
    <Setter Property="Template">
        <ControlTemplate>
            <Border Background="{StaticResource CsPrimaryBrush}"
                    CornerRadius="4"
                    Padding="6,2">
                <ContentPresenter/>
            </Border>
        </ControlTemplate>
    </Setter>
</Style>
```

## 9.3 StyledProperty 详解

`StyledProperty` 是 Avalonia 的属性系统核心：

```csharp
public sealed class CsSegmentedButton : Button
{
    // 1. 注册 StyledProperty
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<CsSegmentedButton, bool>(nameof(IsSelected));

    // 2. 属性变更处理器（静态构造函数中注册）
    static CsSegmentedButton()
    {
        IsSelectedProperty.Changed.AddClassHandler<CsSegmentedButton>((button, args) =>
        {
            // 3. 当属性变更时，更新伪类
            button.PseudoClasses.Set(":selected", args.NewValue is true);
        });
    }

    // 4. CLR 属性包装器
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
}
```

### StyledProperty vs DirectProperty

| 特性 | StyledProperty | DirectProperty |
|------|---------------|----------------|
| 样式支持 | 是 | 否 |
| 继承 | 支持 | 不支持 |
| 动画 | 支持 | 不支持 |
| 性能 | 稍慢（走属性系统） | 更快（直接字段访问） |
| 用途 | UI 可绑定属性 | 内部状态 |

### 属性选项

```csharp
// 带默认值
public static readonly StyledProperty<double> FontSizeProperty =
    AvaloniaProperty.Register<CsRollingNumber, double>(nameof(FontSize), 15d);

// 带验证
public static readonly StyledProperty<int> MaxItemsProperty =
    AvaloniaProperty.Register<CsMyControl, int>(nameof(MaxItems),
        validate: value => value > 0);

// 带默认值和继承
public static readonly StyledProperty<FontFamily> FontFamilyProperty =
    TextBlock.FontFamilyProperty.AddOwner<CsMyControl>();
```

## 9.4 AffectsMeasure 与 AffectsRender

告诉属性系统哪些属性变更需要重新测量或重绘：

```csharp
static CsRollingNumber()
{
    // 这些属性变更时，需要重新测量布局
    AffectsMeasure<CsRollingNumber>(
        ValueProperty,
        UseCompactFormatProperty,
        FontSizeProperty,
        FontWeightProperty);

    // 这些属性变更时，需要重绘（但不需要重新测量）
    AffectsRender<CsRollingNumber>(
        ValueProperty,
        UseCompactFormatProperty,
        FontSizeProperty,
        FontWeightProperty,
        ForegroundProperty);
}
```

## 9.5 模板部件 (Template Parts)

当控件需要从模板中查找特定元素时使用 `OnApplyTemplate`：

```csharp
public class CsSegmentedControl : ContentControl
{
    private Border? _selectedPill;
    private Control? _selectionLayer;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // 从模板中查找命名元素
        _selectedPill = e.NameScope.Find<Border>("PART_SelectedPill");
        _selectionLayer = e.NameScope.Find<Control>("PART_SelectionLayer");

        EnsurePillTransform();
        // 使用 Dispatcher.UIThread.Post 确保在布局完成后执行
        Dispatcher.UIThread.Post(() => UpdateSelectionPill(animate: false), DispatcherPriority.Loaded);
    }
}
```

对应的 AXAML 模板：
```xml
<ControlTemplate>
    <Grid x:Name="PART_SelectionLayer">     <!-- 在代码中查找 -->
        <Border x:Name="PART_SelectedPill"> <!-- 在代码中查找 -->
            <Border.RenderTransform>
                <TranslateTransform/>
            </Border.RenderTransform>
        </Border>
        <ContentPresenter/>
    </Grid>
</ControlTemplate>
```

## 9.6 PseudoClasses（伪类）

伪类让你在 AXAML 中用 `:pseudo-class` 语法选择控件状态：

```csharp
[PseudoClasses(":selected", ":dragging", ":expanded")]
public class CsSegmentedButton : Button
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<CsSegmentedButton, bool>(nameof(IsSelected));

    static CsSegmentedButton()
    {
        IsSelectedProperty.Changed.AddClassHandler<CsSegmentedButton>((button, args) =>
        {
            button.PseudoClasses.Set(":selected", args.NewValue is true);
        });
    }
}
```

在样式中使用：
```xml
<!-- 选中状态 -->
<Style Selector="ui|CsSegmentedButton:selected">
    <Setter Property="Foreground" Value="{StaticResource CsForegroundBrush}"/>
</Style>

<!-- 选中且悬停 -->
<Style Selector="ui|CsSegmentedButton:selected:pointerover">
    <Setter Property="Foreground" Value="{StaticResource CsForegroundBrush}"/>
</Style>
```

### 内置伪类

| 伪类 | 说明 |
|------|------|
| `:pointerover` | 鼠标悬停 |
| `:pressed` | 按下 |
| `:focus` | 聚焦 |
| `:focus-visible` | 键盘聚焦可见 |
| `:disabled` | 禁用 |
| `:checked` | 选中（CheckBox） |
| `:selected` | 选中（自定义） |

## 9.7 StyleKeyOverride

当控件需要复用另一个控件的样式时：

```csharp
// CsInput 继承 TextBox，但用 "TextBox" 的样式
public class CsInput : TextBox
{
    // 这样 Style Selector="TextBox" 也会匹配 CsInput
    protected override Type StyleKeyOverride => typeof(TextBox);
}

// CsSwitch 继承 CheckBox
public class CsSwitch : CheckBox
{
    protected override Type StyleKeyOverride => typeof(CheckBox);
}
```

这让你可以为 `TextBox` 写通用样式，`CsInput` 自动继承。

## 9.8 自定义视觉树遍历

CodexSwitch 大量使用视觉树遍历来查找特定控件：

```csharp
// 向下查找所有后代
var buttons = this.GetVisualDescendants()
    .OfType<CsSegmentedButton>()
    .ToList();

// 向上查找祖先
Control? FindProviderRow(Control source)
{
    if (source.Classes.Contains("provider-list-row"))
        return source;

    return source.GetVisualAncestors()
        .OfType<Control>()
        .FirstOrDefault(control => control.Classes.Contains("provider-list-row"));
}

// 坐标转换
var topLeft = selected.TranslatePoint(new Point(0, 0), _selectionLayer);
```

### 常用遍历方法

| 方法 | 方向 | 返回 |
|------|------|------|
| `GetVisualChildren()` | 直接子元素 | `IEnumerable<Visual>` |
| `GetVisualDescendants()` | 所有后代 | `IEnumerable<Visual>` |
| `GetVisualAncestors()` | 所有祖先 | `IEnumerable<Visual>` |
| `GetVisualParent()` | 直接父元素 | `Visual?` |
| `GetLogicalDescendants()` | 逻辑后代 | `IEnumerable<ILogical>` |

## 9.9 深入：控件生命周期详解

### 完整生命周期

Avalonia 控件的生命周期分为几个关键阶段：

```
构造函数
    ↓
属性初始化（默认值）
    ↓
添加到可视化树（OnAttachedToVisualTree）
    ↓
首次测量（MeasureOverride）
    ↓
首次排列（ArrangeOverride）
    ↓
模板应用（OnApplyTemplate）
    ↓
首次渲染（Render）
    ↓
... 运行期间循环测量/排列/渲染 ...
    ↓
从可视化树移除（OnDetachedFromVisualTree）
    ↓
销毁（Dispose，如果实现了 IDisposable）
```

### 各阶段详解

**1. 构造函数**

```csharp
public class CsRollingNumber : Control
{
    public CsRollingNumber()
    {
        // 设置默认属性值
        ClipToBounds = true;

        // 注意：不要在这里访问模板部件
        // 模板还没有应用
    }
}
```

**2. OnAttachedToVisualTree**

```csharp
protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnAttachedToVisualTree(e);

    // 此时控件已经添加到可视化树
    // 可以安全地访问 Parent、VisualRoot 等
    _isAttached = true;

    // 启动动画、订阅事件等
    if (!_hasValue)
        SetImmediateValue(Value);
}
```

**3. OnApplyTemplate**

```csharp
protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
{
    base.OnApplyTemplate(e);

    // 此时模板已经应用，可以查找模板部件
    _selectedPill = e.NameScope.Find<Border>("PART_SelectedPill");
    _selectionLayer = e.NameScope.Find<Control>("PART_SelectionLayer");

    // 使用 Dispatcher.UIThread.Post 确保在布局完成后执行
    Dispatcher.UIThread.Post(() => UpdateSelectionPill(animate: false), DispatcherPriority.Loaded);
}
```

**4. MeasureOverride 和 ArrangeOverride**

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    // 测量逻辑：计算控件需要的尺寸
    var text = FormatValue(Value, UseCompactFormat);
    var layout = CreateTextLayout(text);
    return new Size(Math.Ceiling(layout.Width), Math.Ceiling(layout.Height));
}

protected override Size ArrangeOverride(Size finalSize)
{
    // 排列逻辑：在给定的尺寸内排列子元素
    // 通常调用 base.ArrangeOverride(finalSize)
    return base.ArrangeOverride(finalSize);
}
```

**5. OnDetachedFromVisualTree**

```csharp
protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnDetachedFromVisualTree(e);

    // 清理资源
    _isAttached = false;
    StopAnimation();  // 必须停止定时器

    // 取消事件订阅
    foreach (var button in _trackedButtons)
        button.PropertyChanged -= OnSegmentedButtonPropertyChanged;
    _trackedButtons.Clear();
}
```

### 生命周期顺序验证

```csharp
public class LifecycleTestControl : Control
{
    public LifecycleTestControl()
    {
        Debug.WriteLine("1. Constructor");
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Debug.WriteLine("2. OnAttachedToVisualTree");
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Debug.WriteLine("3. MeasureOverride");
        return base.MeasureOverride(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Debug.WriteLine("4. ArrangeOverride");
        return base.ArrangeOverride(finalSize);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Debug.WriteLine("5. OnApplyTemplate");
    }

    public override void Render(DrawingContext context)
    {
        Debug.WriteLine("6. Render");
        base.Render(context);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Debug.WriteLine("7. OnDetachedFromVisualTree");
    }
}
```

## 9.10 深入：自定义测量与排列

### MeasureOverride 详解

`MeasureOverride` 负责计算控件的期望尺寸：

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    // availableSize 是父控件提供的可用空间
    // 可能是无限大（double.PositiveInfinity）

    // 1. 测量子元素
    double totalWidth = 0;
    double totalHeight = 0;

    foreach (var child in GetVisualChildren().OfType<Control>())
    {
        child.Measure(availableSize);
        totalWidth = Math.Max(totalWidth, child.DesiredSize.Width);
        totalHeight += child.DesiredSize.Height;
    }

    // 2. 添加内边距
    totalWidth += Padding.Left + Padding.Right;
    totalHeight += Padding.Top + Padding.Bottom;

    // 3. 返回期望尺寸
    return new Size(totalWidth, totalHeight);
}
```

### ArrangeOverride 详解

`ArrangeOverride` 负责在给定尺寸内排列子元素：

```csharp
protected override Size ArrangeOverride(Size finalSize)
{
    // finalSize 是父控件分配的实际空间

    double y = Padding.Top;
    double availableWidth = finalSize.Width - Padding.Left - Padding.Right;

    foreach (var child in GetVisualChildren().OfType<Control>())
    {
        var childHeight = child.DesiredSize.Height;
        child.Arrange(new Rect(Padding.Left, y, availableWidth, childHeight));
        y += childHeight;
    }

    return finalSize;
}
```

### 自定义面板示例

```csharp
public class WrapPanel : Panel
{
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<WrapPanel, double>(nameof(Spacing));

    protected override Size MeasureOverride(Size availableSize)
    {
        double lineWidth = 0;
        double lineHeight = 0;
        double totalHeight = 0;
        double maxWidth = 0;

        foreach (var child in Children)
        {
            child.Measure(availableSize);

            if (lineWidth + child.DesiredSize.Width > availableSize.Width)
            {
                // 换行
                maxWidth = Math.Max(maxWidth, lineWidth);
                totalHeight += lineHeight + Spacing;
                lineWidth = child.DesiredSize.Width;
                lineHeight = child.DesiredSize.Height;
            }
            else
            {
                lineWidth += child.DesiredSize.Width + Spacing;
                lineHeight = Math.Max(lineHeight, child.DesiredSize.Height);
            }
        }

        maxWidth = Math.Max(maxWidth, lineWidth);
        totalHeight += lineHeight;

        return new Size(maxWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = 0;
        double y = 0;
        double lineHeight = 0;

        foreach (var child in Children)
        {
            if (x + child.DesiredSize.Width > finalSize.Width)
            {
                x = 0;
                y += lineHeight + Spacing;
                lineHeight = 0;
            }

            child.Arrange(new Rect(x, y, child.DesiredSize.Width, child.DesiredSize.Height));
            x += child.DesiredSize.Width + Spacing;
            lineHeight = Math.Max(lineHeight, child.DesiredSize.Height);
        }

        return finalSize;
    }
}
```

## 9.11 深入：自定义命中测试

### 命中测试原理

当用户点击控件时，Avalonia 通过命中测试确定被点击的元素：

```
用户点击坐标 (x, y)
    ↓
从根元素开始，递归检查每个控件
    ↓
检查点是否在控件的边界内
    ↓
检查控件是否可命中测试（IsHitTestVisible）
    ↓
检查控件的命中测试几何形状
    ↓
返回最具体的命中元素
```

### 自定义命中测试几何形状

默认情况下，命中测试使用控件的边界矩形。对于不规则形状，可以自定义：

```csharp
public class CircleControl : Control
{
    public override bool HitTest(Point point)
    {
        // 计算点到圆心的距离
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var radius = Math.Min(Bounds.Width, Bounds.Height) / 2;
        var distance = Math.Sqrt(
            Math.Pow(point.X - center.X, 2) +
            Math.Pow(point.Y - center.Y, 2));

        // 只有在圆内才算命中
        return distance <= radius;
    }

    protected override Geometry? CreateHitTestGeometry()
    {
        // 创建圆形几何形状用于命中测试
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var radius = Math.Min(Bounds.Width, Bounds.Height) / 2;
        return new EllipseGeometry(new Rect(
            center.X - radius, center.Y - radius,
            radius * 2, radius * 2));
    }
}
```

### 命中测试与事件冒泡

```csharp
public class CustomControl : Control
{
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // 检查点击位置是否在特定区域
        var point = e.GetPosition(this);
        if (IsInSpecialArea(point))
        {
            // 处理特殊区域的点击
            HandleSpecialClick(point);
            e.Handled = true;  // 阻止事件继续冒泡
            return;
        }

        // 默认处理
        base.OnPointerPressed(e);
    }

    private bool IsInSpecialArea(Point point)
    {
        // 自定义区域检测逻辑
        return point.X < 50 && point.Y < 50;
    }
}
```

## 9.12 深入：控件模板 vs 用户控件

### 控件模板（TemplatedControl）

```csharp
// 定义控件模板
public class CsCard : ContentControl
{
    // 模板在样式中定义
    // 通过 Style Selector="ui|CsCard" 设置 Template
}

// 在样式中定义模板
<Style Selector="ui|CsCard">
    <Setter Property="Template">
        <ControlTemplate>
            <Border Background="{TemplateBinding Background}"
                    CornerRadius="{StaticResource CsRadiusMd}"
                    Padding="{TemplateBinding Padding}">
                <ContentPresenter/>
            </Border>
        </ControlTemplate>
    </Setter>
</Style>
```

特点：
- 模板与逻辑分离
- 可以通过样式重新定义模板
- 支持模板部件（PART_*）
- 适合可复用的控件库

### 用户控件（UserControl）

```xml
<!-- MyUserControl.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="MyApp.MyUserControl">
    <StackPanel>
        <TextBlock Text="{Binding Title}"/>
        <Button Content="Click Me" Command="{Binding ClickCommand}"/>
    </StackPanel>
</UserControl>
```

```csharp
// MyUserControl.axaml.cs
public partial class MyUserControl : UserControl
{
    public MyUserControl()
    {
        InitializeComponent();
    }
}
```

特点：
- 视图和代码后置文件
- 模板固定，不能通过样式重新定义
- 适合特定场景的 UI 组合
- 更接近 WPF 的 UserControl

### 何时使用哪种

| 场景 | 推荐使用 |
|------|---------|
| 可复用的控件库 | TemplatedControl |
| 需要样式定制 | TemplatedControl |
| 特定页面的 UI 组合 | UserControl |
| 快速原型开发 | UserControl |
| 需要模板部件 | TemplatedControl |
| 不需要样式定制 | UserControl |

## 9.13 深入：Lookless 控件设计

### 什么是 Lookless 控件

Lookless（无外观）控件是将逻辑与外观完全分离的控件：

- **逻辑**：在 C# 代码中定义（属性、命令、事件）
- **外观**：在 AXAML 样式中定义（模板、动画、视觉效果）

```csharp
// Lookless 控件：只定义逻辑
public class CsSegmentedButton : Button
{
    // 属性定义
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<CsSegmentedButton, bool>(nameof(IsSelected));

    // 伪类定义
    [PseudoClasses(":selected")]

    // 属性变更处理
    static CsSegmentedButton()
    {
        IsSelectedProperty.Changed.AddClassHandler<CsSegmentedButton>((button, args) =>
        {
            button.PseudoClasses.Set(":selected", args.NewValue is true);
        });
    }

    // CLR 属性包装器
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    // 注意：没有模板定义，没有视觉逻辑
}
```

```xml
<!-- 外观在样式中定义 -->
<Style Selector="ui|CsSegmentedButton">
    <Setter Property="Template">
        <ControlTemplate>
            <Border Background="{TemplateBinding Background}"
                    CornerRadius="{StaticResource CsRadiusSm}">
                <ContentPresenter/>
            </Border>
        </ControlTemplate>
    </Setter>
</Style>

<!-- 不同状态的外观 -->
<Style Selector="ui|CsSegmentedButton:selected">
    <Setter Property="Background" Value="{StaticResource CsSegmentedPillBrush}"/>
</Style>
```

### Lookless 控件设计模式

**1. 属性驱动**

```csharp
public class CsBadge : ContentControl
{
    // 通过属性控制外观
    public static readonly StyledProperty<string?> VariantProperty =
        AvaloniaProperty.Register<CsBadge, string?>(nameof(Variant));

    public string? Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }
}
```

```xml
<!-- 根据属性值应用不同样式 -->
<Style Selector="ui|CsBadge[Variant=success]">
    <Setter Property="Background" Value="{StaticResource CsSuccessBrush}"/>
</Style>
<Style Selector="ui|CsBadge[Variant=destructive]">
    <Setter Property="Background" Value="{StaticResource CsDestructiveBrush}"/>
</Style>
```

**2. 类驱动**

```csharp
public class CsButton : Button
{
    // 不需要额外代码，使用 Classes 属性
}
```

```xml
<!-- 根据类名应用不同样式 -->
<Style Selector="ui|CsButton.primary">
    <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
</Style>
<Style Selector="ui|CsButton.destructive">
    <Setter Property="Background" Value="{StaticResource CsDestructiveBrush}"/>
</Style>
```

**3. 状态驱动**

```csharp
public class CsSwitch : CheckBox
{
    // 使用伪类驱动状态
    // Avalonia 自动为 CheckBox 添加 :checked 伪类
}
```

```xml
<!-- 根据伪类状态应用不同样式 -->
<Style Selector="ui|CsSwitch">
    <Setter Property="Template">
        <ControlTemplate>
            <Border x:Name="PART_SwitchRoot">
                <Border x:Name="PART_SwitchThumb"/>
            </Border>
        </ControlTemplate>
    </Setter>
</Style>
<Style Selector="ui|CsSwitch:checked /template/ Border#PART_SwitchRoot">
    <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
</Style>
```

## 9.14 深入：自定义控件测试

### 单元测试控件逻辑

```csharp
public class CsSegmentedButtonTests
{
    [Fact]
    public void IsSelected_SetsPseudoClass()
    {
        var button = new CsSegmentedButton();

        button.IsSelected = true;

        Assert.True(button.Classes.Contains(":selected"));
    }

    [Fact]
    public void IsSelected_False_RemovesPseudoClass()
    {
        var button = new CsSegmentedButton();
        button.IsSelected = true;

        button.IsSelected = false;

        Assert.False(button.Classes.Contains(":selected"));
    }
}
```

### 集成测试控件渲染

```csharp
public class CsRollingNumberTests
{
    [Fact]
    public void Render_DisplaysCorrectValue()
    {
        var control = new CsRollingNumber { Value = 12345 };

        // 创建测试窗口
        var window = new Window
        {
            Content = control,
            Width = 200,
            Height = 50
        };

        // 显示窗口并等待布局完成
        window.Show();

        // 验证渲染结果
        // 注意：实际测试中可能需要截图比较或使用测试框架
    }
}
```

### 测试控件模板

```csharp
public class CsDialogTests
{
    [Fact]
    public void OnApplyTemplate_FindsTemplateParts()
    {
        var dialog = new CsDialog();

        // 应用模板
        var template = new ControlTemplate((_, _) =>
        {
            return new Border
            {
                Name = "PART_DialogRoot",
                Child = new ContentPresenter()
            };
        });

        dialog.Template = template;
        dialog.ApplyTemplate();

        // 验证模板部件
        var root = dialog.GetTemplateChildren()
            .FirstOrDefault(c => c.Name == "PART_DialogRoot");
        Assert.NotNull(root);
    }
}
```

### 测试控件属性

```csharp
public class CsSectionTests
{
    [Fact]
    public void Title_DefaultValueIsNull()
    {
        var section = new CsSection();
        Assert.Null(section.Title);
    }

    [Fact]
    public void Title_SetValue_RaisesPropertyChanged()
    {
        var section = new CsSection();
        var raised = false;
        section.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CsSection.Title))
                raised = true;
        };

        section.Title = "Test";

        Assert.True(raised);
        Assert.Equal("Test", section.Title);
    }
}
```

## 9.15 跨引用

- **样式系统**：控件模板在样式中定义，参见 [第 7 章](07-styling-theming.md)
- **数据模板**：ItemsControl 和 ContentControl 的模板机制参见 [第 8 章](08-data-templates.md)
- **动画过渡**：控件状态变化时的动画参见 [第 10 章](10-animation-transitions.md)
- **属性系统**：StyledProperty 和 DirectProperty 的详细机制参见 [第 22 章](22-property-system.md)
- **可视化树**：控件树的结构和遍历参见 [第 23 章](23-visual-logical-tree.md)

## 9.16 常见陷阱

### 陷阱 1：在构造函数中访问模板部件

```csharp
// 问题：模板还没有应用，无法访问模板部件
public class CsSegmentedControl : ContentControl
{
    public CsSegmentedControl()
    {
        _selectedPill = this.FindControl<Border>("PART_SelectedPill");  // 错误
    }
}

// 解决：在 OnApplyTemplate 中访问
protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
{
    base.OnApplyTemplate(e);
    _selectedPill = e.NameScope.Find<Border>("PART_SelectedPill");  // 正确
}
```

### 陷阱 2：忘记清理资源

```csharp
// 问题：没有在 OnDetachedFromVisualTree 中停止定时器
protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnDetachedFromVisualTree(e);
    // 忘记停止 _animationTimer
}

// 解决：总是清理资源
protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnDetachedFromVisualTree(e);
    StopAnimation();  // 停止定时器
    _animationTimer = null;
}
```

### 陷阱 3：StyledProperty 默认值类型错误

```csharp
// 问题：默认值类型与属性类型不匹配
public static readonly StyledProperty<int> MaxItemsProperty =
    AvaloniaProperty.Register<CsMyControl, int>(nameof(MaxItems), 10.5);  // 错误

// 解决：确保类型匹配
public static readonly StyledProperty<int> MaxItemsProperty =
    AvaloniaProperty.Register<CsMyControl, int>(nameof(MaxItems), 10);  // 正确
```

### 陷阱 4：模板部件命名约定

```xml
<!-- 问题：模板部件没有使用 PART_ 前缀 -->
<ControlTemplate>
    <Border x:Name="SelectedPill"/>  <!-- 不规范 -->
</ControlTemplate>

<!-- 正确：使用 PART_ 前缀 -->
<ControlTemplate>
    <Border x:Name="PART_SelectedPill"/>  <!-- 规范 -->
</ControlTemplate>
```

### 陷阱 5：在 MeasureOverride 中调用 InvalidateMeasure

```csharp
// 问题：在测量过程中触发新的测量，导致无限循环
protected override Size MeasureOverride(Size availableSize)
{
    InvalidateMeasure();  // 错误：会导致无限循环
    return new Size(100, 100);
}

// 解决：避免在测量过程中触发布局
protected override Size MeasureOverride(Size availableSize)
{
    // 只计算尺寸，不要触发布局
    return new Size(100, 100);
}
```

## 9.17 动手练习

### 练习 1：创建自定义控件

在 CodexSwitch 中创建一个新的自定义控件 `CsProgressRing`：

```csharp
// 1. 定义控件类
public class CsProgressRing : Control
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<CsProgressRing, double>(nameof(Progress), 0d);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<CsProgressRing, double>(nameof(StrokeThickness), 4d);

    static CsProgressRing()
    {
        AffectsRender<CsProgressRing>(ProgressProperty, StrokeThicknessProperty);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = Math.Min(availableSize.Width, availableSize.Height);
        return new Size(size, size);
    }

    public override void Render(DrawingContext context)
    {
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var radius = Math.Min(Bounds.Width, Bounds.Height) / 2 - StrokeThickness / 2;
        var angle = Progress * 360;

        // 绘制背景圆环
        context.DrawEllipse(null, new Pen(Brushes.Gray, StrokeThickness),
            center, radius, radius);

        // 绘制进度圆弧
        if (Progress > 0)
        {
            var geometry = new ArcGeometry(center, radius, 0, angle);
            context.DrawGeometry(null, new Pen(Foreground, StrokeThickness), geometry);
        }
    }
}
```

### 练习 2：添加样式

在 `Styles/Components/` 中为 `CsProgressRing` 创建样式：

```xml
<Style Selector="ui|CsProgressRing">
    <Setter Property="Foreground" Value="{StaticResource CsPrimaryBrush}"/>
    <Setter Property="Width" Value="40"/>
    <Setter Property="Height" Value="40"/>
</Style>
```

### 练习 3：测试控件

为 `CsProgressRing` 编写单元测试：

```csharp
public class CsProgressRingTests
{
    [Fact]
    public void Progress_DefaultValue_IsZero()
    {
        var ring = new CsProgressRing();
        Assert.Equal(0d, ring.Progress);
    }

    [Fact]
    public void Progress_SetValue_RaisesPropertyChanged()
    {
        var ring = new CsProgressRing();
        var raised = false;
        ring.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CsProgressRing.Progress))
                raised = true;
        };

        ring.Progress = 0.5;

        Assert.True(raised);
        Assert.Equal(0.5, ring.Progress);
    }

    [Fact]
    public void MeasureOverride_ReturnsSquareSize()
    {
        var ring = new CsProgressRing();
        var size = ring.MeasureOverride(new Size(100, 200));
        Assert.Equal(100, size.Width);
        Assert.Equal(100, size.Height);
    }
}
```

### 练习 4：集成到页面

在 CodexSwitch 的某个页面中使用 `CsProgressRing`：

```xml
<StackPanel>
    <ui:CsProgressRing Progress="{Binding UploadProgress}"/>
    <TextBlock Text="{Binding UploadProgress, StringFormat='{}{0:P0}'}"/>
</StackPanel>
```
