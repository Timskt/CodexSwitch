# 9. 自定义控件开发

> **写给零基础的你**：Avalonia 自带了很多控件（按钮、文本框、列表等），但有时候你需要一个"世界上还没有"的控件。自定义控件就是自己造一个新的零件。就像乐高积木套装里没有飞机零件，你可以用现有零件拼一个出来，下次直接用。

> **UserControl vs TemplatedControl**：这是初学者最容易困惑的地方。简单来说：
> - **UserControl** = 你直接用现有零件拼一个固定造型（布局写死，适合一次性使用的界面片段）
> - **TemplatedControl** = 你设计一个新零件的"规格说明书"，别人可以按说明书制造，还能换颜色、换大小（支持模板和样式，适合通用控件）

## 9.1 概述

自定义控件是 Avalonia UI 开发的核心能力之一。学完本章，你将能够：

- 理解 Avalonia 控件类型的完整层次结构，知道何时继承哪个基类
- 熟练创建样式包装器控件、模板控件和自定义渲染控件
- 掌握 StyledProperty、PseudoClasses、ControlTemplate 等核心机制
- 自定义 ItemsControl、ContentControl、Decorator 等高级控件
- 使用 AdornerLayer 实现浮动装饰层
- 参考 CodexSwitch 的真实控件代码，理解工业级控件开发模式

CodexSwitch 包含 45+ 自定义控件，从简单的样式包装器（`CsBadge`）到复杂的自定义渲染控件（`CsRollingNumber`），再到带动画的模板控件（`CsSegmentedControl`）。本章将逐一拆解这些控件的设计思路和实现细节。

## 9.2 核心概念

### 9.2.1 控件类型层次

> **小白提示**：这个层次图就像"家族族谱"。`Control` 是老祖宗，所有的控件都是它的后代。每个后代继承了祖先的能力，还添加了自己的新能力。比如 `Button` 继承了 `Control` 的显示能力，还添加了"点击"能力。

```
Control                       # 最基础的控件（老祖宗，所有控件的基类）
├── TemplatedControl          # 支持 ControlTemplate 的控件（可以用"模板"定制外观）
│   ├── ContentControl        # 单内容容器（只能放一个子控件）
│   │   ├── Window            # 窗口
│   │   ├── UserControl       # 用户控件（组合现有控件）
│   │   └── CsDialog          # 自定义对话框
│   ├── HeaderedContentControl # 带标题的内容容器
│   │   ├── TabItem           # 标签页项
│   │   └── TreeViewItem      # 树节点
│   ├── ItemsControl          # 集合容器（可以放多个子控件）
│   │   ├── ListBox           # 列表
│   │   ├── ComboBox          # 下拉框
│   │   ├── TabControl        # 标签页
│   │   ├── TreeView          # 树形控件
│   │   └── DataGrid          # 数据表格
│   └── RangeBase             # 范围控件基类（有最小值、最大值、当前值）
│       ├── Slider            # 滑块
│       ├── ProgressBar       # 进度条
│       ├── ScrollBar         # 滚动条
│       └── NumericUpDown     # 数字输入框
├── InputElement              # 支持输入事件（鼠标、键盘等）
│   ├── TextBox               # 文本输入框
│   └── Button                # 按钮
│       └── ToggleButton
│           ├── CheckBox
│           └── RadioButton
└── Visual                    # 可视化基类
    └── Layoutable
        └── Control
```

### 9.2.2 UserControl vs 自定义控件（TemplatedControl）

这是初学者最常困惑的问题。两者的核心区别：

**UserControl（用户控件）**

```xml
<!-- MyWidget.axaml - 布局固定，不可被样式覆盖 -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="MyApp.MyWidget">
    <StackPanel>
        <TextBlock Text="{Binding Title}"/>
        <Button Content="Click" Command="{Binding ClickCommand}"/>
    </StackPanel>
</UserControl>
```

```csharp
public partial class MyWidget : UserControl
{
    public MyWidget()
    {
        InitializeComponent(); // 加载 AXAML 中定义的视觉树
    }
}
```

特点：
- AXAML 中直接定义视觉树，调用 `InitializeComponent()` 加载
- 模板固定，消费者无法通过 Style 重新定义外观
- 适合特定页面的 UI 组合（如设置面板、表单区域）
- 开发速度快，有设计器支持

**TemplatedControl（模板控件）**

```csharp
// CsCard.cs - 只定义逻辑，外观在 Style 中
public class CsCard : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<CsCard, string?>(nameof(Title));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}
```

```xml
<!-- 在 Styles/Components/Card.axaml 中定义外观 -->
<Style Selector="ui|CsCard">
    <Setter Property="Template">
        <ControlTemplate>
            <Border Background="{TemplateBinding Background}"
                    CornerRadius="{StaticResource CsRadiusMd}"
                    Padding="{TemplateBinding Padding}">
                <StackPanel>
                    <TextBlock Text="{TemplateBinding Title}"
                               FontWeight="SemiBold"/>
                    <ContentPresenter/>
                </StackPanel>
            </Border>
        </ControlTemplate>
    </Setter>
</Style>
```

特点：
- 逻辑和外观完全分离（Lookless）
- 外观通过 Style/ControlTemplate 定义，可被覆盖
- 支持模板部件（PART_*）
- 适合可复用的控件库

**选择决策表**

| 场景 | 推荐 | 原因 |
|------|------|------|
| 页面特定的 UI 组合 | UserControl | 快速开发，布局固定 |
| 可复用的通用控件 | TemplatedControl | 样式可覆盖，逻辑复用 |
| 需要多种外观变体 | TemplatedControl | 不同 Style 提供不同外观 |
| 需要模板部件交互 | TemplatedControl | 支持 PART_* 查找 |
| 快速原型 | UserControl | 最少代码，即时预览 |
| 控件库发布 | TemplatedControl | 消费者可自定义外观 |

### 9.2.3 StyledProperty 详解

`StyledProperty` 是 Avalonia 属性系统的核心。每个自定义控件属性都应该通过 `StyledProperty` 注册：

```csharp
public sealed class CsSegmentedButton : Button
{
    // 1. 静态注册 StyledProperty
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<CsSegmentedButton, bool>(nameof(IsSelected));

    // 2. 静态构造函数中注册变更处理器
    static CsSegmentedButton()
    {
        IsSelectedProperty.Changed.AddClassHandler<CsSegmentedButton>((button, args) =>
        {
            // 3. 当 IsSelected 变化时，切换伪类
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

**StyledProperty vs DirectProperty**

| 特性 | StyledProperty | DirectProperty |
|------|---------------|----------------|
| 样式支持 | 是（可在 `<Style>` 中设置） | 否 |
| 继承 | 支持（`Inherits = true`） | 不支持 |
| 动画 | 支持 | 不支持 |
| 绑定 | 支持 | 支持 |
| 性能 | 走属性系统，稍慢 | 直接字段访问，更快 |
| 典型用途 | UI 可绑定属性 | 内部状态、只读计算值 |

**属性选项与高级用法**

```csharp
// 带默认值
public static readonly StyledProperty<double> FontSizeProperty =
    AvaloniaProperty.Register<CsRollingNumber, double>(nameof(FontSize), 15d);

// 带验证器（返回 false 时抛出异常）
public static readonly StyledProperty<int> MaxItemsProperty =
    AvaloniaProperty.Register<CsMyControl, int>(nameof(MaxItems),
        validate: value => value > 0);

// 继承父控件的属性
public static readonly StyledProperty<FontFamily> FontFamilyProperty =
    TextBlock.FontFamilyProperty.AddOwner<CsMyControl>();

// 直接属性（不经过样式系统）
public static readonly DirectProperty<CsMyControl, double> ComputedValueProperty =
    AvaloniaProperty.RegisterDirect<CsMyControl, double>(
        nameof(ComputedValue),
        o => o.ComputedValue);

private double _computedValue;
public double ComputedValue
{
    get => _computedValue;
    private set => SetAndRaise(ComputedValueProperty, ref _computedValue, value);
}
```

### 9.2.4 AffectsMeasure 与 AffectsRender

告诉属性系统哪些属性变更需要重新测量或重绘：

```csharp
static CsRollingNumber()
{
    // 这些属性变更时需要重新测量布局（尺寸可能改变）
    AffectsMeasure<CsRollingNumber>(
        ValueProperty,
        UseCompactFormatProperty,
        FontSizeProperty,
        FontWeightProperty);

    // 这些属性变更时需要重绘（但尺寸不变）
    AffectsRender<CsRollingNumber>(
        ValueProperty,
        UseCompactFormatProperty,
        FontSizeProperty,
        FontWeightProperty,
        ForegroundProperty);
}
```

注意：`AffectsMeasure` 已隐含 `AffectsRender`（尺寸变化必然需要重绘），但反之不成立。`Foreground` 只影响颜色，不需要重新测量。

### 9.2.5 PseudoClasses（伪类）

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

**Avalonia 内置伪类**

| 伪类 | 说明 | 适用控件 |
|------|------|---------|
| `:pointerover` | 鼠标悬停 | 所有控件 |
| `:pressed` | 按下 | Button、ToggleButton |
| `:focus` | 聚焦 | 所有可聚焦控件 |
| `:focus-visible` | 键盘聚焦可见 | 所有可聚焦控件 |
| `:disabled` | 禁用（`IsEnabled=false`） | 所有控件 |
| `:checked` | 选中 | CheckBox、ToggleButton |
| `:selected` | 选中 | ListBoxItem、自定义 |
| `:expanded` | 展开 | TreeViewItem、ComboBox |
| `:empty` | 无子元素 | ItemsControl |
| `:indeterminate` | 不确定状态 | CheckBox（三态） |

## 9.3 进阶用法

### 9.3.1 ControlTemplate 深入

ControlTemplate 是 TemplatedControl 的外观定义。理解 ControlTemplate 的每个元素至关重要：

**TemplateBinding**

`TemplateBinding` 将模板内的属性绑定到控件自身的属性：

```xml
<ControlTemplate>
    <Border Background="{TemplateBinding Background}"
            BorderBrush="{TemplateBinding BorderBrush}"
            BorderThickness="{TemplateBinding BorderThickness}"
            CornerRadius="{TemplateBinding CornerRadius}"
            Padding="{TemplateBinding Padding}">
        <ContentPresenter/>
    </Border>
</ControlTemplate>
```

`TemplateBinding` 是单向的（控件属性 -> 模板元素），且只支持 StyledProperty。如果需要更复杂的绑定（如转换器、多级路径），使用 `{Binding RelativeSource={RelativeSource TemplatedParent}, Path=...}`。

**ContentPresenter**

`ContentPresenter` 是 `ContentControl` 模板中必须的元素，它负责显示 `Content` 属性的内容，并应用 `ContentTemplate`：

```xml
<ControlTemplate>
    <Border>
        <ContentPresenter Content="{TemplateBinding Content}"
                          ContentTemplate="{TemplateBinding ContentTemplate}"
                          HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}"
                          VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}"
                          Margin="{TemplateBinding Padding}"/>
    </Border>
</ControlTemplate>
```

**ItemsPresenter**

`ItemsPresenter` 是 `ItemsControl` 模板中必须的元素，它负责显示所有子项：

```xml
<!-- ListBox 的 ControlTemplate -->
<ControlTemplate>
    <Border Background="{TemplateBinding Background}"
            BorderBrush="{TemplateBinding BorderBrush}">
        <ScrollViewer>
            <ItemsPresenter Items="{TemplateBinding Items}"
                            ItemTemplate="{TemplateBinding ItemTemplate}">
                <ItemsPresenter.ItemsPanel>
                    <ItemsPanelTemplate>
                        <VirtualizingStackPanel/>
                    </ItemsPanelTemplate>
                </ItemsPresenter.ItemsPanel>
            </ItemsPresenter>
        </ScrollViewer>
    </Border>
</ControlTemplate>
```

### 9.3.2 PART_ 命名约定

Avalonia 的内置控件使用 `PART_` 前缀命名模板部件。了解这些约定有助于自定义内置控件模板：

**Slider 的模板部件**

```xml
<ControlTemplate TargetType="Slider">
    <Grid>
        <!-- PART_Track：滑动轨道容器 -->
        <Border x:Name="PART_Track" Height="4">
            <Border.Background>
                <SolidColorBrush Color="Gray"/>
            </Border.Background>
        </Border>

        <!-- PART_SelectionRange：选中范围指示 -->
        <Border x:Name="PART_SelectionRange" Height="4"/>

        <!-- PART_DecreaseButton：减少按钮（轨道左侧） -->
        <RepeatButton x:Name="PART_DecreaseButton"
                      Classes="slider-track"/>

        <!-- PART_IncreaseButton：增加按钮（轨道右侧） -->
        <RepeatButton x:Name="PART_IncreaseButton"
                      Classes="slider-track"/>

        <!-- PART_Thumb：拖拽滑块 -->
        <Thumb x:Name="PART_Thumb">
            <Thumb.Template>
                <ControlTemplate>
                    <Ellipse Width="16" Height="16"
                             Fill="{StaticResource CsPrimaryBrush}"/>
                </ControlTemplate>
            </Thumb.Template>
        </Thumb>
    </Grid>
</ControlTemplate>
```

**ProgressBar 的模板部件**

```xml
<ControlTemplate TargetType="ProgressBar">
    <Border x:Name="PART_Track"
            Background="{TemplateBinding Background}"
            CornerRadius="4">
        <Border x:Name="PART_Indicator"
                Background="{TemplateBinding Foreground}"
                HorizontalAlignment="Left"
                CornerRadius="4"/>
    </Border>
</ControlTemplate>
```

**TabControl 的模板部件**

```xml
<ControlTemplate TargetType="TabControl">
    <DockPanel>
        <!-- PART_HeaderPanel：标签头区域 -->
        <TabStrip x:Name="PART_HeaderPanel"
                  DockPanel.Dock="Top"
                  Items="{TemplateBinding Items}"
                  SelectedIndex="{TemplateBinding SelectedIndex}"/>

        <!-- PART_SelectedContentHost：选中项的内容区域 -->
        <ContentPresenter x:Name="PART_SelectedContentHost"
                          Content="{TemplateBinding SelectedContent}"
                          ContentTemplate="{TemplateBinding SelectedContentTemplate}"/>
    </DockPanel>
</ControlTemplate>
```

**TreeView 的模板部件**

```xml
<ControlTemplate TargetType="TreeView">
    <Border Background="{TemplateBinding Background}">
        <ScrollViewer>
            <ItemsPresenter x:Name="PART_ItemsPresenter"
                            Items="{TemplateBinding Items}">
                <ItemsPresenter.ItemsPanel>
                    <ItemsPanelTemplate>
                        <StackPanel/>
                    </ItemsPanelTemplate>
                </ItemsPresenter.ItemsPanel>
            </ItemsPresenter>
        </ScrollViewer>
    </Border>
</ControlTemplate>
```

### 9.3.3 StyleKeyOverride

当控件需要复用另一个控件的样式时：

```csharp
// CsInput 继承 TextBox，但用 "TextBox" 的样式
public class CsInput : TextBox
{
    // Style Selector="TextBox" 也会匹配 CsInput
    protected override Type StyleKeyOverride => typeof(TextBox);
}

// CsSwitch 继承 CheckBox
public class CsSwitch : CheckBox
{
    protected override Type StyleKeyOverride => typeof(CheckBox);
}
```

这让你可以为 `TextBox` 写通用样式，`CsInput` 自动继承，同时保留 `CsInput` 自己的类型选择器（`ui|CsInput`）。

### 9.3.4 OnApplyTemplate 与模板部件

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
    <Border Background="{StaticResource CsSegmentedBrush}"
            ClipToBounds="True"
            Padding="{TemplateBinding Padding}">
        <Grid x:Name="PART_SelectionLayer" ClipToBounds="True">
            <Border x:Name="PART_SelectedPill"
                    HorizontalAlignment="Left"
                    VerticalAlignment="Top"
                    IsHitTestVisible="False"
                    Opacity="0"
                    Background="{StaticResource CsSegmentedPillBrush}">
                <Border.RenderTransform>
                    <TranslateTransform/>
                </Border.RenderTransform>
            </Border>
            <ContentPresenter Content="{TemplateBinding Content}"/>
        </Grid>
    </Border>
</ControlTemplate>
```

### 9.3.5 自定义 ItemsControl

自定义 `ItemsControl` 需要理解 `ItemContainerGenerator`：

```csharp
public class CsTagList : ItemsControl
{
    // 自定义容器类型
    protected override Type StyleKeyOverride => typeof(ItemsControl);

    // 创建或配置项容器
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);

        if (container is CsTag tag && item is string text)
        {
            tag.Content = text;
            tag.Classes.Add("auto-generated");
        }
    }

    // 清理容器
    protected override void ClearContainerForItemOverride(Control container, object? item, int index)
    {
        base.ClearContainerForItemOverride(container, item, index);
        container.Classes.Remove("auto-generated");
    }

    // 判断是否是自己的容器（用于虚拟化）
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        return NeedsContainer<CsTag>(item, out recycleKey);
    }

    // 创建默认容器
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new CsTag();
    }
}
```

### 9.3.6 自定义 ContentControl

自定义 `ContentControl` 常用于需要额外属性的容器：

```csharp
public class CsCard : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<CsCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<CsCard, string?>(nameof(Description));

    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<CsCard, object?>(nameof(Icon));

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

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}
```

模板中使用 `TemplateBinding` 暴露这些属性：

```xml
<Style Selector="ui|CsCard">
    <Setter Property="Template">
        <ControlTemplate>
            <Border Background="{TemplateBinding Background}"
                    CornerRadius="{StaticResource CsRadiusMd}"
                    Padding="{TemplateBinding Padding}">
                <StackPanel Spacing="8">
                    <!-- 图标 + 标题行 -->
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <ContentPresenter Content="{TemplateBinding Icon}"/>
                        <TextBlock Text="{TemplateBinding Title}"
                                   FontWeight="SemiBold"
                                   FontSize="14"/>
                    </StackPanel>
                    <!-- 描述 -->
                    <TextBlock Text="{TemplateBinding Description}"
                               Opacity="0.7"
                               FontSize="12"/>
                    <!-- 主内容 -->
                    <ContentPresenter Content="{TemplateBinding Content}"/>
                </StackPanel>
            </Border>
        </ControlTemplate>
    </Setter>
</Style>
```

### 9.3.7 装饰器（Decorator）模式

Decorator 是一种包装子元素并添加额外视觉效果的控件：

```csharp
public class CsGlowBorder : Decorator
{
    public static readonly StyledProperty<IBrush?> GlowBrushProperty =
        AvaloniaProperty.Register<CsGlowBorder, IBrush?>(nameof(GlowBrush));

    public static readonly StyledProperty<double> GlowSizeProperty =
        AvaloniaProperty.Register<CsGlowBorder, double>(nameof(GlowSize), 10d);

    public IBrush? GlowBrush
    {
        get => GetValue(GlowBrushProperty);
        set => SetValue(GlowBrushProperty, value);
    }

    public double GlowSize
    {
        get => GetValue(GlowSizeProperty);
        set => SetValue(GlowSizeProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // 测量子元素时减去发光区域的空间
        var margin = GlowSize;
        var childAvailable = new Size(
            Math.Max(0, availableSize.Width - margin * 2),
            Math.Max(0, availableSize.Height - margin * 2));

        Child?.Measure(childAvailable);
        var desired = Child?.DesiredSize ?? default;

        return new Size(desired.Width + margin * 2, desired.Height + margin * 2);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var margin = GlowSize;
        Child?.Arrange(new Rect(margin, margin,
            Math.Max(0, finalSize.Width - margin * 2),
            Math.Max(0, finalSize.Height - margin * 2)));
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        if (GlowBrush is null) return;

        var glowRect = new Rect(GlowSize / 2, GlowSize / 2,
            Bounds.Width - GlowSize, Bounds.Height - GlowSize);

        // 绘制发光效果（使用 BoxShadow 或自定义绘制）
        context.DrawRectangle(null,
            new Pen(GlowBrush, GlowSize / 2),
            glowRect, 4, 4);

        base.Render(context);
    }
}
```

使用方式：

```xml
<ui:CsGlow GlowBrush="#404A9EFF" GlowSize="8">
    <Border Background="White" CornerRadius="8" Padding="16">
        <TextBlock Text="I have a glow effect!"/>
    </Border>
</ui:CsGlow>
```

### 9.3.8 AdornerLayer

`AdornerLayer` 是一个覆盖在控件上方的透明层，用于绘制装饰效果（如拖拽手柄、调整大小手柄、验证错误指示器）：

```csharp
public class ResizeAdorner : Adorner
{
    private Thumb? _bottomRightThumb;

    public ResizeAdorner(Control adornedElement) : base(adornedElement)
    {
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _bottomRightThumb = e.NameScope.Find<Thumb>("PART_BottomRightThumb");

        if (_bottomRightThumb is not null)
        {
            _bottomRightThumb.DragDelta += OnDragDelta;
        }
    }

    private void OnDragDelta(object? sender, VectorEventArgs e)
    {
        if (AdornedElement is Control control)
        {
            var newWidth = Math.Max(50, control.Width + e.Vector.X);
            var newHeight = Math.Max(50, control.Height + e.Vector.Y);
            control.Width = newWidth;
            control.Height = newHeight;
        }
    }

    // 覆盖排列，让 Adorner 与被装饰元素对齐
    protected override Size ArrangeOverride(Size finalSize)
    {
        var adornedSize = AdornedElement.DesiredSize;
        return base.ArrangeOverride(adornedSize);
    }
}

// 在代码中添加 Adorner
var adornerLayer = AdornerLayer.GetAdornerLayer(myControl);
if (adornerLayer is not null)
{
    var adorner = new ResizeAdorner(myControl);
    adornerLayer.Children.Add(adorner);
}

// 移除 Adorner
adornerLayer.Children.Remove(adorner);
```

**Adorner 的 AXAML 方式**

```xml
<AdornerDecorator>
    <Border x:Name="MyBorder" Width="200" Height="100">
        <TextBlock Text="Drag to resize"/>
    </Border>
    <AdornerLayer.Adorners>
        <local:ResizeAdorner Adorned="{Binding #MyBorder}"/>
    </AdornerLayer.Adorners>
</AdornerDecorator>
```

## 9.4 组件详解大全

### 9.4.1 ToggleSwitch 详解

`ToggleSwitch` 是一个开关控件，比 `CheckBox` 更适合表示开/关状态：

```xml
<ToggleSwitch IsChecked="{Binding IsDarkMode}"
              OnContent="Dark"
              OffContent="Light"/>
```

**核心属性**

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsChecked` | `bool?` | 是否选中（三态） |
| `OnContent` | `object?` | 开启时显示的内容 |
| `OffContent` | `object?` | 关闭时显示的内容 |
| `Content` | `object?` | 开关旁边的文字（与 OnContent/OffContent 互斥） |

**自定义模板**

```xml
<Style Selector="ToggleSwitch">
    <Setter Property="Template">
        <ControlTemplate>
            <StackPanel Orientation="Horizontal" Spacing="8">
                <!-- 开关轨道 -->
                <Border x:Name="PART_SwitchRoot"
                        Width="44" Height="24"
                        CornerRadius="12"
                        Background="{StaticResource CsMutedBrush}">
                    <!-- 开关滑块 -->
                    <Border x:Name="PART_SwitchThumb"
                            Width="20" Height="20"
                            CornerRadius="10"
                            Background="White"
                            Margin="2"
                            HorizontalAlignment="Left"/>
                </Border>
                <!-- 内容 -->
                <ContentPresenter Content="{TemplateBinding Content}"
                                  VerticalAlignment="Center"/>
            </StackPanel>
        </ControlTemplate>
    </Setter>
</Style>

<!-- 选中状态 -->
<Style Selector="ToggleSwitch:checked /template/ Border#PART_SwitchRoot">
    <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
</Style>

<Style Selector="ToggleSwitch:checked /template/ Border#PART_SwitchThumb">
    <Setter Property="HorizontalAlignment" Value="Right"/>
</Style>
```

### 9.4.2 Slider 详解

```xml
<Slider Minimum="0"
        Maximum="100"
        Value="{Binding Volume}"
        TickFrequency="10"
        IsSnapToTickEnabled="True"
        Orientation="Horizontal"/>
```

**核心属性**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Minimum` | `double` | 最小值 |
| `Maximum` | `double` | 最大值 |
| `Value` | `double` | 当前值 |
| `TickFrequency` | `double` | 刻度间距 |
| `IsSnapToTickEnabled` | `bool` | 是否吸附到刻度 |
| `Orientation` | `Orientation` | 水平/垂直 |
| `IsDirectionReversed` | `bool` | 是否反转方向 |

### 9.4.3 ProgressBar 详解

```xml
<ProgressBar Minimum="0"
             Maximum="100"
             Value="{Binding DownloadProgress}"
             IsIndeterminate="{Binding IsLoading}"
             ShowProgressText="True"
             CornerRadius="4"/>
```

**核心属性**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Minimum` | `double` | 最小值 |
| `Maximum` | `double` | 最大值 |
| `Value` | `double` | 当前值 |
| `IsIndeterminate` | `bool` | 不确定模式（加载动画） |
| `ShowProgressText` | `bool` | 是否显示百分比文字 |
| `CornerRadius` | `CornerRadius` | 圆角 |

### 9.4.4 NumericUpDown 详解

```xml
<NumericUpDown Minimum="0"
               Maximum="100"
               Value="{Binding Quantity}"
               FormatString="F0"
               Increment="1"
               Watermark="Enter a number"
               SpinnerPlacement="Right"/>
```

**核心属性**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Minimum` | `decimal?` | 最小值 |
| `Maximum` | `decimal?` | 最大值 |
| `Value` | `decimal?` | 当前值 |
| `Increment` | `decimal` | 步进值 |
| `FormatString` | `string?` | 数字格式化字符串 |
| `Watermark` | `string?` | 水印文本 |
| `SpinnerPlacement` | `Location` | 微调器位置 |

### 9.4.5 DataGrid 详解

```xml
<DataGrid ItemsSource="{Binding Providers}"
          AutoGenerateColumns="False"
          IsReadOnly="True"
          CanUserResizeColumns="True"
          CanUserSortColumns="True"
          GridLinesVisibility="Horizontal"
          SelectionMode="Single">
    <DataGrid.Columns>
        <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="200"/>
        <DataGridTextColumn Header="Status" Binding="{Binding Status}" Width="100"/>
        <DataGridCheckBoxColumn Header="Enabled" Binding="{Binding IsEnabled}" Width="80"/>
        <DataGridTemplateColumn Header="Actions" Width="120">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <Button Content="Edit" Classes="small"/>
                        <Button Content="Delete" Classes="small destructive"/>
                    </StackPanel>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </DataGrid.Columns>
</DataGrid>
```

## 9.5 CodexSwitch 实战

### 9.5.1 CsSegmentedControl 完整解析

`CsSegmentedControl` 是 CodexSwitch 中最复杂的自定义控件之一，它实现了一个带滑动选中指示器的分段选择器。

**控件类（逻辑层）**

```csharp
public class CsSegmentedControl : ContentControl
{
    private static readonly TimeSpan PillAnimationDuration = TimeSpan.FromMilliseconds(180);
    private readonly HashSet<CsSegmentedButton> _trackedButtons = [];
    private Border? _selectedPill;
    private Control? _selectionLayer;
    private DispatcherTimer? _animationTimer;
    private bool _hasPillPosition;
    private double _pillX, _pillY, _pillWidth, _pillHeight;
```

**关键设计：模板部件查找**

```csharp
protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
{
    base.OnApplyTemplate(e);

    // 从 ControlTemplate 中查找命名元素
    _selectedPill = e.NameScope.Find<Border>("PART_SelectedPill");
    _selectionLayer = e.NameScope.Find<Control>("PART_SelectionLayer");

    EnsurePillTransform();
    // Post 到 Loaded 优先级，确保布局完成后再计算位置
    Dispatcher.UIThread.Post(() => UpdateSelectionPill(animate: false), DispatcherPriority.Loaded);
}
```

**关键设计：视觉树跟踪**

```csharp
private void TrackSegmentedButtons()
{
    // 同时从视觉树和逻辑树查找所有 CsSegmentedButton
    var buttons = this.GetVisualDescendants()
        .OfType<CsSegmentedButton>()
        .Concat(this.GetLogicalDescendants().OfType<CsSegmentedButton>())
        .ToHashSet();

    // 取消订阅已移除的按钮
    foreach (var button in _trackedButtons.ToArray())
    {
        if (buttons.Contains(button)) continue;
        button.PropertyChanged -= OnSegmentedButtonPropertyChanged;
        _trackedButtons.Remove(button);
    }

    // 订阅新添加的按钮
    foreach (var button in buttons)
    {
        if (!_trackedButtons.Add(button)) continue;
        button.PropertyChanged += OnSegmentedButtonPropertyChanged;
    }
}
```

**关键设计：坐标转换与动画**

```csharp
private void UpdateSelectionPill(bool animate)
{
    var selected = _trackedButtons.FirstOrDefault(b => b.IsSelected && b.IsVisible);
    if (selected is null || selected.Bounds.Width <= 0)
    {
        _selectedPill.Opacity = 0;
        return;
    }

    // 将选中按钮的 (0,0) 转换到 _selectionLayer 的坐标系
    var topLeft = selected.TranslatePoint(new Point(0, 0), _selectionLayer);
    if (topLeft is null) return;

    var targetX = topLeft.Value.X;
    var targetY = topLeft.Value.Y;

    if (!animate || !_hasPillPosition)
    {
        // 直接跳到目标位置（无动画）
        ApplyPill(targetX, targetY, selected.Bounds.Width, selected.Bounds.Height, 1);
        return;
    }

    // 平滑动画移动到目标位置
    AnimatePill(targetX, targetY, selected.Bounds.Width, selected.Bounds.Height);
}
```

### 9.5.2 CsSegmentedButton 解析

这是最简洁的自定义控件——只添加一个 `IsSelected` 属性和对应的伪类：

```csharp
[PseudoClasses(":selected")]
public sealed class CsSegmentedButton : Button
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

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
}
```

配合样式系统，`:selected` 伪类驱动视觉变化：

```xml
<Style Selector="ui|CsSegmentedButton:selected">
    <Setter Property="Foreground" Value="{StaticResource CsForegroundBrush}"/>
</Style>
```

### 9.5.3 CsDialog 的极简淡入实现

```csharp
public sealed class CsDialog : ContentControl
{
    public CsDialog()
    {
        Opacity = 0; // 初始不可见
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // 当 IsVisible 变为 true 时
        if (change.Property != IsVisibleProperty ||
            change.NewValue is not bool isVisible || !isVisible)
            return;

        Opacity = 0;
        // Post 到 Render 优先级——下一帧再设置 Opacity=1
        // 这样 DoubleTransition 才能检测到从 0 到 1 的变化
        Dispatcher.UIThread.Post(() => Opacity = 1, DispatcherPriority.Render);
    }
}
```

配合样式中的 `DoubleTransition`：

```xml
<Style Selector="ui|CsDialog">
    <Setter Property="Transitions">
        <Transitions>
            <DoubleTransition Property="Opacity" Duration="0:0:0.2"/>
        </Transitions>
    </Setter>
</Style>
```

## 9.6 举一反三

### 9.6.1 与 WPF 的差异

| 特性 | WPF | Avalonia |
|------|-----|----------|
| 属性系统 | `DependencyProperty` | `StyledProperty` / `DirectProperty` |
| 模板查找 | `GetTemplateChild()` | `e.NameScope.Find()` |
| 伪类 | 无 | `PseudoClasses.Set()` |
| 样式选择器 | `TargetType` | CSS 风格 `Selector="Type:class:pseudoclass"` |
| 布局 | 相同（Measure/Arrange） | 相同 |
| 渲染 | `OnRender(DrawingContext)` | `Render(DrawingContext)` |

### 9.6.2 与 CSS 的类比

| CSS | Avalonia |
|-----|----------|
| `.class` | `Classes` / `.class` |
| `:hover` | `:pointerover` |
| `:active` | `:pressed` |
| `:checked` | `:checked` |
| `::before` | 无直接等价 |
| `[attr]` | `[Property=Value]` |

## 9.7 最佳实践与设计模式

### 9.7.1 Lookless 控件设计

```csharp
// 好：只定义属性和逻辑，外观在 Style 中
public class CsProgressRing : Control
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<CsProgressRing, double>(nameof(Progress));
    // 没有 Render() 覆盖——视觉在 Style 中定义
}
```

### 9.7.2 属性驱动外观

```csharp
// 好：通过属性值驱动不同样式
public class CsBadge : ContentControl
{
    public static readonly StyledProperty<string?> VariantProperty =
        AvaloniaProperty.Register<CsBadge, string?>(nameof(Variant));
}
```

```xml
<Style Selector="ui|CsBadge[Variant=success]">
    <Setter Property="Background" Value="{StaticResource CsSuccessBrush}"/>
</Style>
<Style Selector="ui|CsBadge[Variant=destructive]">
    <Setter Property="Background" Value="{StaticResource CsDestructiveBrush}"/>
</Style>
```

## Deep Dive：内部原理

### 渲染管线

```
属性变更
    -> AffectsRender 标记脏区域
    -> InvalidateVisual() 入队
    -> 渲染循环在下一帧调用 Render()
    -> DrawingContext 记录绘制指令
    -> Skia 批量执行 GPU 绘制
```

### 模板应用时序

```
构造函数
    -> 属性初始化（默认值）
    -> 添加到可视化树（OnAttachedToVisualTree）
    -> 首次测量（MeasureOverride）
    -> 模板应用（OnApplyTemplate）  ← 这里才能查找 PART_*
    -> 首次渲染（Render）
```

### 命中测试原理

```
用户点击坐标 (x, y)
    -> 从根元素开始递归检查
    -> 检查点是否在控件边界内
    -> 检查 IsHitTestVisible
    -> 检查命中测试几何形状（默认为矩形）
    -> 返回最具体的命中元素
```

自定义命中测试：

```csharp
public class CircleControl : Control
{
    public override bool HitTest(Point point)
    {
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var radius = Math.Min(Bounds.Width, Bounds.Height) / 2;
        var distance = Math.Sqrt(
            Math.Pow(point.X - center.X, 2) +
            Math.Pow(point.Y - center.Y, 2));
        return distance <= radius;
    }
}
```

## Cross References

- **样式系统**：控件模板在样式中定义，参见 [第 7 章](07-styling-theming.md)
- **数据模板**：ItemsControl 和 ContentControl 的模板机制参见 [第 8 章](08-data-templates.md)
- **动画过渡**：控件状态变化时的动画参见 [第 10 章](10-animation-transitions.md)
- **自定义渲染**：`Render(DrawingContext)` 的详细 API 参见 [第 14 章](14-custom-rendering.md)
- **属性系统**：StyledProperty 和 DirectProperty 的详细机制参见 [第 22 章](22-property-system.md)
- **可视化树**：控件树的结构和遍历参见 [第 23 章](23-visual-logical-tree.md)
- **编译绑定**：模板中的绑定优化参见 [第 15 章](15-compiled-bindings.md)

## Common Pitfalls

### 陷阱 1：在构造函数中访问模板部件

```csharp
// 错误：模板还没有应用
public CsSegmentedControl()
{
    _selectedPill = this.FindControl<Border>("PART_SelectedPill"); // null!
}

// 正确：在 OnApplyTemplate 中访问
protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
{
    _selectedPill = e.NameScope.Find<Border>("PART_SelectedPill");
}
```

### 陷阱 2：忘记清理资源

```csharp
// 错误：控件移除后定时器继续运行
protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    // 忘记停止 _animationTimer —— 内存泄漏
}

// 正确：总是清理
protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnDetachedFromVisualTree(e);
    _animationTimer?.Stop();
    _animationTimer = null;
    foreach (var button in _trackedButtons)
        button.PropertyChanged -= OnSegmentedButtonPropertyChanged;
    _trackedButtons.Clear();
}
```

### 陷阱 3：StyledProperty 默认值类型不匹配

```csharp
// 错误：double 赋给 int 属性
public static readonly StyledProperty<int> MaxProperty =
    AvaloniaProperty.Register<MyControl, int>(nameof(Max), 10.5); // 编译错误

// 正确：
AvaloniaProperty.Register<MyControl, int>(nameof(Max), 10);
```

### 陷阱 4：模板部件未使用 PART_ 前缀

```xml
<!-- 不规范：工具和文档无法识别 -->
<Border x:Name="SelectedPill"/>

<!-- 规范：PART_ 前缀是约定 -->
<Border x:Name="PART_SelectedPill"/>
```

### 陷阱 5：在 MeasureOverride 中触发 InvalidateMeasure

```csharp
// 错误：无限循环
protected override Size MeasureOverride(Size availableSize)
{
    InvalidateMeasure(); // 触发新的测量，再次调用 MeasureOverride...
    return new Size(100, 100);
}
```

### 陷阱 6：TemplateBinding 不支持复杂绑定

```xml
<!-- 错误：TemplateBinding 不支持 StringFormat -->
<TextBlock Text="{TemplateBinding Value, StringFormat='{}{0:F2}'}"/>

<!-- 正确：使用完整 Binding -->
<TextBlock Text="{Binding Value, RelativeSource={RelativeSource TemplatedParent}, StringFormat='{}{0:F2}'}"/>
```

### 陷阱 7：忘记调用 base.OnApplyTemplate

```csharp
// 错误：跳过基类
protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
{
    _myPart = e.NameScope.Find<Border>("PART_MyPart");
    // base.OnApplyTemplate(e); -- 漏了！
}
```

### 陷阱 8：ItemsControl 中忘记虚拟化

```xml
<!-- 问题：大数据集性能差 -->
<ItemsControl.ItemsPanel>
    <ItemsPanelTemplate>
        <StackPanel/>  <!-- 渲染所有项 -->
    </ItemsPanelTemplate>
</ItemsControl.ItemsPanel>

<!-- 解决：使用虚拟化面板 -->
<ItemsControl.ItemsPanel>
    <ItemsPanelTemplate>
        <VirtualizingStackPanel/>  <!-- 只渲染可见项 -->
    </ItemsPanelTemplate>
</ItemsControl.ItemsPanel>
```

### 陷阱 9：ContentPresenter 的 Content 与 ContentTemplate 混淆

```xml
<!-- Content 直接显示对象 -->
<ContentPresenter Content="{Binding MyObject}"/>

<!-- ContentTemplate 定义如何显示对象 -->
<ContentPresenter Content="{Binding MyObject}"
                  ContentTemplate="{StaticResource MyTemplate}"/>
```

### 陷阱 10：AdornerLayer 的叠加层位置

Adorner 默认相对于被装饰元素的左上角定位。如果被装饰元素在 ScrollViewer 中滚动，Adorner 不会自动跟随——需要手动更新位置。

### 陷阱 11：PseudoClasses 的 set/get 不对称

```csharp
// 错误：用 Classes 管理伪类
button.Classes.Add(":selected"); // 语法正确但不推荐

// 正确：用 PseudoClasses
button.PseudoClasses.Set(":selected", true);  // 设置
button.PseudoClasses.Set(":selected", false); // 移除
```

### 陷阱 12：Decorator 的 Child 属性

`Decorator.Child` 是单个子元素。如果需要多个子元素，使用 `Panel` 作为 Child，然后在 Panel 中放置多个子元素。

## Try It Yourself

### 练习 1：创建 CsProgressRing

创建一个环形进度控件：

```csharp
public class CsProgressRing : Control
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<CsProgressRing, double>(nameof(Progress), 0d);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<CsProgressRing, double>(nameof(StrokeThickness), 4d);

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<CsProgressRing, IBrush?>(nameof(Foreground));

    static CsProgressRing()
    {
        AffectsRender<CsProgressRing>(ProgressProperty, StrokeThicknessProperty, ForegroundProperty);
        AffectsMeasure<CsProgressRing>(StrokeThicknessProperty);
    }

    public double Progress { get => GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public double StrokeThickness { get => GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = Math.Min(
            double.IsInfinity(availableSize.Width) ? 40 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 40 : availableSize.Height);
        return new Size(size, size);
    }

    public override void Render(DrawingContext context)
    {
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var radius = Math.Min(Bounds.Width, Bounds.Height) / 2 - StrokeThickness / 2;

        // 背景环
        context.DrawEllipse(null, new Pen(Brushes.Gray, StrokeThickness), center, radius, radius);

        // 进度弧（使用 StreamGeometry）
        if (Progress > 0)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                var startAngle = -90;
                var sweepAngle = Progress * 360;
                var startRad = startAngle * Math.PI / 180;
                var endRad = (startAngle + sweepAngle) * Math.PI / 180;

                var startPoint = new Point(
                    center.X + radius * Math.Cos(startRad),
                    center.Y + radius * Math.Sin(startRad));
                var endPoint = new Point(
                    center.X + radius * Math.Cos(endRad),
                    center.Y + radius * Math.Sin(endRad));

                ctx.BeginFigure(startPoint, false);
                ctx.ArcTo(endPoint, new Size(radius, radius), 0, sweepAngle > 180, SweepDirection.Clockwise);
            }

            context.DrawGeometry(null, new Pen(Foreground ?? Brushes.Blue, StrokeThickness), geometry);
        }
    }
}
```

### 练习 2：创建 CsToast 通知

实现一个自动消失的通知控件：
- 定义 `Message`、`Duration`、`Variant` 属性
- 使用 `DispatcherTimer` 实现自动消失
- 使用 `TranslateTransform` + `DoubleTransition` 实现滑入/滑出动画

### 练习 3：创建 CsRating 评分控件

实现一个星级评分控件：
- 定义 `Value`（0-5）、`StarCount`、`StarSize` 属性
- 在 `Render` 中使用 `DrawGeometry` 绘制五角星
- 支持半星显示
- 支持鼠标悬停预览

### 练习 4：自定义 ListBoxItem 模板

为 ListBoxItem 创建自定义模板：
- 左侧图标 + 文字 + 右侧删除按钮
- 选中状态高亮
- 悬停效果
- 拖拽手柄

### 练习 5：实现 CsAccordion 手风琴控件

创建一个可折叠的面板组：
- 继承 `ItemsControl`
- 每个面板有标题和可折叠内容
- 展开时有高度动画
- 同一时间只展开一个面板（可选）

### 练习 6：创建 CsTooltip 自定义提示

使用 AdornerLayer 实现自定义提示：
- 鼠标悬停时在控件上方显示
- 有淡入淡出动画
- 自动定位避免超出屏幕

### 练习 7：实现自定义面板

创建一个 `FlowPanel`（类似 WrapPanel 但有行间距和列间距）：

```csharp
public class FlowPanel : Panel
{
    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<FlowPanel, double>(nameof(RowSpacing));

    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<FlowPanel, double>(nameof(ColumnSpacing));

    protected override Size MeasureOverride(Size availableSize)
    {
        // 遍历 Children，计算每行宽度，超过时换行
        // ...
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // 按行排列子元素
        // ...
    }
}
```

### 练习 8：对比 UserControl 和 TemplatedControl

将同一个控件分别用 UserControl 和 TemplatedControl 实现，对比：
- 代码量差异
- 可定制性差异
- 样式覆盖能力差异
