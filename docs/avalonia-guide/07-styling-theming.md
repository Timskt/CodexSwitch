# 7. 样式与主题系统

> **写给零基础的你**：样式就像给控件"穿衣服"。你可以在一个地方定义"所有按钮都是蓝色圆角的"，然后所有按钮自动穿上这件衣服，不用每个按钮单独设置。主题就是"整套衣服"——深色主题是暗色系衣服，浅色主题是亮色系衣服，一键换装。

> **小白提示**：如果你用过网页开发中的 CSS，Avalonia 的样式和 CSS 非常像。都是用选择器找到目标控件，然后给它设置外观属性。如果你没用过 CSS，也没关系——本章会从头解释。

## 7.1 概述

Avalonia 的样式系统类似于 CSS，支持选择器、伪类、继承等特性，比 WPF 的样式系统更灵活。本章详细讲解所有样式选择器、ControlTemplate、ThemeVariant 系统、运行时主题切换等核心知识点。

学完本章后，你将能够：
- 掌握所有样式选择器（类型、名称、类、伪类、属性等）
- 掌握 ControlTemplate 的所有用法
- 理解 TemplateBinding vs {Binding TemplatedParent} 的区别
- 掌握 ThemeVariant 系统和运行时主题切换
- 理解样式优先级和层叠规则
- 掌握 FluentTheme 和自定义主题
- 了解样式系统的内部渲染管线
- 避免 10+ 个最常见的样式陷阱

## 7.2 核心概念

### 7.2.1 样式架构

CodexSwitch 采用双层主题架构：

```
App.axaml
├── uiTheme:CodexSwitchTheme        ← 组件库主题（子模块提供）
└── CodexTheme.axaml                ← 应用层主题
    ├── 70+ 色彩资源 (Cs*Brush)
    ├── 圆角令牌 (CsRadius*)
    ├── 全局 Window 样式
    ├── 侧边栏菜单按钮样式
    └── StyleInclude (20+ 组件样式)
        ├── Button.axaml
        ├── Input.axaml
        ├── Dialog.axaml
        └── ...
```

这种架构将设计令牌（颜色、圆角等）集中在顶层，组件样式通过 `StyleInclude` 分文件组织，方便维护和扩展。

### 7.2.2 设计令牌 (Design Tokens)

> **小白提示：什么是设计令牌？**  设计令牌就像"调色板上的颜色编号"。设计师说"主色用 5 号色"，所有用到主色的地方都写"5 号"，而不是每次都写具体的颜色值。如果有一天要换主色，只需要把 5 号色换成新颜色，所有用到 5 号色的地方自动更新。

设计令牌是样式系统的"原子"——不可再分的视觉属性值。CodexSwitch 定义了 70+ 色彩令牌和 4 级圆角令牌：

```xml
<!-- CodexTheme.axaml 中定义的色彩令牌 -->
<Styles.Resources>
    <!-- 背景与前景 -->
    <SolidColorBrush x:Key="CsBackgroundBrush" Color="#171717"/>
    <SolidColorBrush x:Key="CsForegroundBrush" Color="#FAFAFA"/>
    <SolidColorBrush x:Key="CsCardBrush" Color="#262626"/>
    <SolidColorBrush x:Key="CsPrimaryBrush" Color="#E5E5E5"/>

    <!-- 语义色彩 -->
    <SolidColorBrush x:Key="CsDestructiveBrush" Color="#FF6467"/>
    <SolidColorBrush x:Key="CsSuccessBrush" Color="#36D399"/>
    <SolidColorBrush x:Key="CsWarningBrush" Color="#FACC15"/>
    <SolidColorBrush x:Key="CsInfoBrush" Color="#3B82F6"/>

    <!-- 交互状态 -->
    <SolidColorBrush x:Key="CsHoverBrush" Color="#33FFFFFF"/>
    <SolidColorBrush x:Key="CsPressedBrush" Color="#22FFFFFF"/>
    <SolidColorBrush x:Key="CsDisabledBrush" Color="#55FFFFFF"/>

    <!-- 圆角令牌 -->
    <CornerRadius x:Key="CsRadiusSm">6</CornerRadius>
    <CornerRadius x:Key="CsRadiusMd">8</CornerRadius>
    <CornerRadius x:Key="CsRadiusLg">10</CornerRadius>
    <CornerRadius x:Key="CsRadiusXl">14</CornerRadius>
</Styles.Resources>
```

**命名规范**：
- 前缀 `Cs` 代表 CodexSwitch，避免与其他库冲突
- 语义化命名：`CsPrimaryBrush` 而非 `CsBlueBrush`
- 后缀 `Brush` 表示 SolidColorBrush，`Pen` 表示 Pen

**使用方式**：
```xml
<!-- 静态引用（编译时解析，性能最好） -->
<Button Background="{StaticResource CsPrimaryBrush}"/>

<!-- 动态引用（运行时解析，支持主题切换） -->
<Button Background="{DynamicResource CsPrimaryBrush}"/>
```

### 7.2.3 样式基础语法

```xml
<!-- 基本样式：找到所有 Button，设置背景色 -->
<Style Selector="Button">
    <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
    <Setter Property="Foreground" Value="{StaticResource CsPrimaryForegroundBrush}"/>
    <Setter Property="CornerRadius" Value="{StaticResource CsRadiusMd}"/>
    <Setter Property="Padding" Value="12,8"/>
</Style>
```

**Style 的关键属性**：

```xml
<Style Selector="Button"
       x:DataType="vm:MyViewModel">  <!-- 可选：指定数据类型 -->
    <!-- 可以包含多个 Setter -->
    <Setter Property="Background" Value="Red"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="Padding" Value="8,4"/>

    <!-- 可以包含嵌套 Style -->
    <Style Selector="^:pointerover">  <!-- ^ 表示父选择器 -->
        <Setter Property="Background" Value="DarkRed"/>
    </Style>
</Style>
```

### 7.2.4 样式选择器完整参考

Avalonia 的选择器系统类似 CSS，但有自己的特色。以下是完整的选择器类型：

| 选择器 | 语法 | 示例 | 说明 |
|--------|------|------|------|
| 类型选择器 | `TypeName` | `Button` | 匹配所有 Button 控件 |
| 命名选择器 | `#Name` | `#MyButton` | 匹配 Name="MyButton" |
| 类选择器 | `.class` | `.primary` | 匹配 Classes 包含 "primary" |
| 伪类选择器 | `:pseudo` | `:pointerover` | 匹配鼠标悬停状态 |
| 属性选择器 | `[Prop=Value]` | `[IsEnabled=true]` | 匹配属性值 |
| 子模板选择器 | `/template/ Type` | `/template/ Border` | 匹配模板内的 Border |
| OR 组合 | `A, B` | `Button, TextBlock` | 匹配任一 |
| AND 组合 | `AB` | `Button.primary` | 同时满足 |
| 后代组合 | `A B` | `StackPanel TextBlock` | 后代关系 |
| 子组合 | `A > B` | `Grid > Border` | 直接子级 |

**伪类选择器完整列表**：

| 伪类 | 触发条件 | 常用控件 |
|------|---------|---------|
| `:pointerover` | 鼠标悬停 | 所有控件 |
| `:pressed` | 鼠标按下 | Button, ToggleButton |
| `:focus` | 键盘焦点 | TextBox, Button |
| `:disabled` | IsEnabled=false | 所有控件 |
| `:checked` | 选中 | CheckBox, RadioButton |
| `:unchecked` | 未选中 | CheckBox, RadioButton |
| `:indeterminate` | 不确定 | CheckBox (三态) |
| `:selected` | 选中 | ListBoxItem, TreeViewItem |
| `:expanded` | 展开 | TreeViewItem, Expander |
| `:empty` | 无内容 | ContentControl |
| `:notempty` | 有内容 | ContentControl |
| `:nth-child(n)` | 第 n 个子项 | ItemsControl 子项 |

## 7.3 选择器详解

### 7.3.1 类型选择器

```xml
<!-- 匹配所有 TextBlock -->
<Style Selector="TextBlock">
    <Setter Property="Foreground" Value="{StaticResource CsForegroundBrush}"/>
    <Setter Property="FontSize" Value="14"/>
</Style>

<!-- 匹配所有 TextBox -->
<Style Selector="TextBox">
    <Setter Property="Background" Value="{StaticResource CsInputBrush}"/>
    <Setter Property="CornerRadius" Value="{StaticResource CsRadiusMd}"/>
</Style>
```

### 7.3.2 Class 选择器

```xml
<!-- 定义 .primary 类的样式 -->
<Style Selector="Button.primary">
    <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
    <Setter Property="Foreground" Value="{StaticResource CsPrimaryForegroundBrush}"/>
</Style>

<!-- 定义 .destructive 类的样式 -->
<Style Selector="Button.destructive">
    <Setter Property="Background" Value="{StaticResource CsDestructiveBrush}"/>
</Style>

<!-- 定义 .outline 类的样式 -->
<Style Selector="Button.outline">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderBrush" Value="{StaticResource CsBorderBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
</Style>
```

在 AXAML 中使用 Class：

```xml
<Button Classes="primary" Content="Save"/>
<Button Classes="destructive" Content="Delete"/>
<Button Classes="outline" Content="Cancel"/>
```

**在代码中操作 Class**：

```csharp
// 添加样式类
button.Classes.Add("primary");
button.Classes.Add("large");

// 检查是否包含样式类
bool isPrimary = button.Classes.Contains("primary");

// 替换样式类
button.Classes.Replace("primary", "secondary");

// 移除样式类
button.Classes.Remove("large");

// 批量操作
button.Classes.Clear();
button.Classes.AddRange(new[] { "primary", "large" });
```

### 7.3.3 伪类选择器

```xml
<!-- 鼠标悬停时改变背景 -->
<Style Selector="Button:pointerover">
    <Setter Property="Background" Value="{StaticResource CsPrimaryHoverBrush}"/>
</Style>

<!-- 按下时改变背景 -->
<Style Selector="Button:pressed">
    <Setter Property="Background" Value="{StaticResource CsPrimaryPressedBrush}"/>
</Style>

<!-- 禁用时改变透明度 -->
<Style Selector="Button:disabled">
    <Setter Property="Opacity" Value="0.5"/>
</Style>

<!-- 聚焦时显示边框 -->
<Style Selector="TextBox:focus">
    <Setter Property="BorderBrush" Value="{StaticResource CsRingBrush}"/>
</Style>
```

**使用 PseudoClasses（自定义伪类）**：

```csharp
// 在自定义控件中注册伪类
public class MyToggle : ToggleButton
{
    static MyToggle()
    {
        IsCheckedProperty.Changed.AddClassHandler<MyToggle>((x, e) =>
        {
            if (x.IsChecked == true)
                x.PseudoClasses.Add(":checked");
            else
                x.PseudoClasses.Remove(":checked");
        });
    }
}
```

```xml
<!-- 使用自定义伪类 -->
<Style Selector="MyToggle:checked">
    <Setter Property="Background" Value="Green"/>
</Style>
```

### 7.3.4 组合选择器

```xml
<!-- AND：同时满足类型和类 -->
<Style Selector="Button.primary:pointerover">
    <Setter Property="Background" Value="{StaticResource CsPrimaryHoverBrush}"/>
</Style>

<!-- AND：类型 + 类 + 伪类 -->
<Style Selector="Button.destructive:pressed">
    <Setter Property="Background" Value="{StaticResource CsDestructivePressedBrush}"/>
</Style>

<!-- OR：多个选择器 -->
<Style Selector="Button, ToggleButton, RepeatButton">
    <Setter Property="Cursor" Value="Hand"/>
</Style>

<!-- 后代选择器 -->
<Style Selector="StackPanel TextBlock">
    <Setter Property="Margin" Value="0,4"/>
</Style>

<!-- 子选择器 -->
<Style Selector="Grid > Border">
    <Setter Property="Margin" Value="8"/>
</Style>
```

### 7.3.5 模板选择器

```xml
<!-- 匹配自定义控件模板内的 Border -->
<Style Selector="ui|CodexSidebarMenuButton:pointerover /template/ Border#PART_MenuButtonRoot">
    <Setter Property="Background" Value="{StaticResource CsSidebarAccentBrush}"/>
</Style>

<!-- 匹配 Slider 模板内的 Thumb -->
<Style Selector="Slider /template/ Thumb">
    <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
</Style>

<!-- 匹配 ProgressBar 模板内的指示器 -->
<Style Selector="ProgressBar /template/ Border#PART_Indicator">
    <Setter Property="Background" Value="{StaticResource CsSuccessBrush}"/>
</Style>
```

### 7.3.6 属性选择器

```xml
<!-- 匹配 IsEnabled=true 的控件 -->
<Style Selector="Button[IsEnabled=true]">
    <Setter Property="Opacity" Value="1"/>
</Style>

<!-- 匹配 IsEnabled=false 的控件 -->
<Style Selector="Button[IsEnabled=false]">
    <Setter Property="Opacity" Value="0.5"/>
</Style>

<!-- 匹配特定属性值 -->
<Style Selector="TextBox[AcceptsReturn=true]">
    <Setter Property="TextWrapping" Value="Wrap"/>
</Style>
```

### 7.3.7 样式的嵌套与 ^ 引用

Avalonia 支持样式的嵌套，子选择器用 `^` 引用父选择器：

```xml
<Style Selector="Button">
    <Setter Property="Background" Value="{StaticResource CsCardBrush}"/>
    <Setter Property="Foreground" Value="{StaticResource CsForegroundBrush}"/>
    <Setter Property="CornerRadius" Value="{StaticResource CsRadiusMd}"/>
    <Setter Property="Padding" Value="12,6"/>

    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="Background" Duration="0:0:0.18"/>
        </Transitions>
    </Setter>

    <!-- 嵌套样式：^ 代表父选择器 Button -->
    <Style Selector="^:pointerover">
        <Setter Property="Background" Value="{StaticResource CsHoverBrush}"/>
    </Style>
    <Style Selector="^:pressed">
        <Setter Property="Background" Value="{StaticResource CsPressedBrush}"/>
    </Style>
    <Style Selector="^:disabled">
        <Setter Property="Opacity" Value="0.5"/>
    </Style>

    <!-- 嵌套样式：类组合 -->
    <Style Selector="^.primary">
        <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
        <Setter Property="Foreground" Value="{StaticResource CsBackgroundBrush}"/>
    </Style>
    <Style Selector="^.primary:pointerover">
        <Setter Property="Background" Value="{StaticResource CsPrimaryHoverBrush}"/>
    </Style>
</Style>
```

### 7.3.8 样式资源字典

你可以将样式提取到独立的资源字典文件中：

```xml
<!-- Styles/Button.axaml -->
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Style Selector="Button">
        <Setter Property="Background" Value="{StaticResource CsCardBrush}"/>
        <Setter Property="CornerRadius" Value="{StaticResource CsRadiusMd}"/>
        <Setter Property="Padding" Value="12,6"/>
    </Style>
    <Style Selector="Button.primary">
        <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
    </Style>
</Styles>
```

```xml
<!-- 在主文件中引用 -->
<StyleInclude Source="/Styles/Button.axaml"/>
```

**Source 路径格式**：
- `/Styles/Button.axaml` — 相对于项目根目录
- `avares://AssemblyName/Path/File.axaml` — 引用程序集中的资源

## 7.4 ControlTemplate

### 7.4.1 基本 ControlTemplate

ControlTemplate 定义控件的内部结构。它允许你完全重新定义控件的外观，而不改变行为：

```xml
<Style Selector="Button">
    <Setter Property="Template">
        <ControlTemplate TargetType="Button">
            <Border Background="{TemplateBinding Background}"
                    CornerRadius="{TemplateBinding CornerRadius}"
                    Padding="{TemplateBinding Padding}"
                    BorderBrush="{TemplateBinding BorderBrush}"
                    BorderThickness="{TemplateBinding BorderThickness}">
                <ContentPresenter Content="{TemplateBinding Content}"
                                  ContentTemplate="{TemplateBinding ContentTemplate}"
                                  HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}"
                                  VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}"/>
            </Border>
        </ControlTemplate>
    </Setter>
</Style>
```

**TemplateBinding 详解**：

`TemplateBinding` 是控件模板中最重要的绑定方式，它将模板内部元素的属性绑定到模板外部控件的属性：

```xml
<!-- 定义模板 -->
<ControlTemplate TargetType="Button">
    <!-- 这里的 Background 绑定到 Button 的 Background 属性 -->
    <Border Background="{TemplateBinding Background}">
        <ContentPresenter Content="{TemplateBinding Content}"/>
    </Border>
</ControlTemplate>

<!-- 使用模板 -->
<Button Background="Red" Content="Click Me"/>
<!-- Border 的 Background 会自动变为 Red -->
```

### 7.4.2 TemplateBinding vs Binding TemplatedParent

```xml
<!-- TemplateBinding：简写，只能用于 ControlTemplate -->
<Border Background="{TemplateBinding Background}"/>

<!-- 等价于 -->
<Border Background="{Binding Background, RelativeSource={RelativeSource TemplatedParent}}"/>

<!-- Binding TemplatedParent：更灵活，支持转换器等 -->
<Border Background="{Binding Background,
    RelativeSource={RelativeSource TemplatedParent},
    Converter={x:Static converters:MyConverter.Instance}}"/>
```

| 特性 | TemplateBinding | Binding TemplatedParent |
|------|----------------|------------------------|
| 语法 | 简短 | 较长 |
| 方向 | 仅 OneWay | 支持所有模式 |
| 转换器 | 不支持 | 支持 |
| StringFormat | 不支持 | 支持 |
| 性能 | 略快 | 略慢 |
| 使用场景 | 属性透传 | 需要转换或双向绑定时 |

### 7.4.3 PART_ 命名约定

Avalonia 的模板部件使用 `PART_` 前缀命名：

```xml
<!-- Slider 的模板部件 -->
<ControlTemplate TargetType="Slider">
    <Border Name="PART_TrackBackground">
        <Border Name="PART_SelectionRoot">
            <Border Name="PART_Indicator"/>
        </Border>
        <Thumb Name="PART_Thumb"/>
    </Border>
</ControlTemplate>

<!-- ProgressBar 的模板部件 -->
<ControlTemplate TargetType="ProgressBar">
    <Border Name="PART_Track">
        <Border Name="PART_Indicator"/>
    </Border>
</ControlTemplate>

<!-- TabControl 的模板部件 -->
<ControlTemplate TargetType="TabControl">
    <DockPanel>
        <TabStrip Name="PART_Header"/>
        <ContentPresenter Name="PART_Content"/>
    </DockPanel>
</ControlTemplate>
```

`PART_` 命名约定的规则：
- 以 `PART_` 开头，后跟部件名称
- 名称使用 PascalCase（如 `PART_MenuButtonRoot`）
- 在代码中通过 `GetTemplateChild()` 或 `Name` 属性访问
- 自定义控件的模板必须包含必要的 `PART_` 部件

### 7.4.4 自定义 ControlTemplate 实战

为按钮创建一个包含图标和文字的自定义模板：

```xml
<ControlTemplate TargetType="Button">
    <Border Background="{TemplateBinding Background}"
            CornerRadius="{TemplateBinding CornerRadius}"
            Padding="{TemplateBinding Padding}"
            BorderBrush="{TemplateBinding BorderBrush}"
            BorderThickness="{TemplateBinding BorderThickness}">
        <StackPanel Orientation="Horizontal" Spacing="8">
            <PathIcon Data="{TemplateBinding Tag}" Width="16" Height="16"/>
            <ContentPresenter Content="{TemplateBinding Content}"/>
        </StackPanel>
    </Border>
</ControlTemplate>
```

## 7.5 主题系统

### 7.5.1 ThemeVariant

Avalonia 支持三种主题变体：

```csharp
// 设置主题
Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
Application.Current.RequestedThemeVariant = ThemeVariant.Light;
Application.Current.RequestedThemeVariant = ThemeVariant.Default;  // 跟随系统

// 获取当前主题
ThemeVariant current = Application.Current.ActualThemeVariant;
bool isDark = current == ThemeVariant.Dark;
```

**RequestedThemeVariant vs ActualThemeVariant**：
- `RequestedThemeVariant`：请求的主题，可以设置为 `Default`
- `ActualThemeVariant`：实际应用的主题，当 `RequestedThemeVariant` 为 `Default` 时跟随系统

### 7.5.2 主题切换实现

```csharp
public static class AppThemeService
{
    private static string _theme = "system";

    // 70+ 颜色对，每个颜色有深色和浅色两个值
    private static readonly IReadOnlyDictionary<string, ThemeColorPair> ThemeBrushes =
        new Dictionary<string, ThemeColorPair>
        {
            ["CsBackgroundBrush"]      = new("#171717", "#FAFAFA"),
            ["CsForegroundBrush"]      = new("#FAFAFA", "#09090B"),
            ["CsCardBrush"]            = new("#262626", "#FFFFFF"),
            ["CsPrimaryBrush"]         = new("#E5E5E5", "#18181B"),
            ["CsDestructiveBrush"]     = new("#FF6467", "#DC2626"),
            ["CsSuccessBrush"]         = new("#36D399", "#16A34A"),
            ["CsWarningBrush"]         = new("#FACC15", "#CA8A04"),
            // ... 更多颜色对
        };

    public static void Apply(string? theme)
    {
        var app = Application.Current;
        _theme = Normalize(theme);

        // 设置主题变体
        app.RequestedThemeVariant = _theme switch
        {
            "light" => ThemeVariant.Light,
            "dark"  => ThemeVariant.Dark,
            _       => ThemeVariant.Default  // 跟随系统
        };

        // 应用组件库主题
        ApplyComponentLibraryTheme(app);

        // 应用所有颜色刷子
        ApplyBrushes(app);
    }

    private static void ApplyBrush(Application app, string key, string colorText)
    {
        var color = Color.Parse(colorText);

        // 尝试获取已存在的资源，就地修改
        if (app.TryGetResource(key, app.ActualThemeVariant, out var resource) &&
            resource is SolidColorBrush brush)
        {
            brush.Color = color;  // 就地修改，所有引用处自动更新
            return;
        }

        // 如果不存在，创建新资源
        app.Resources[key] = new SolidColorBrush(color);
    }

    private readonly record struct ThemeColorPair(string Dark, string Light);
}
```

**主题切换的核心原理**：

1. 设置 `Application.RequestedThemeVariant` 触发主题变体变化
2. 遍历所有颜色令牌，修改 `SolidColorBrush.Color`
3. 由于 `SolidColorBrush` 是引用类型，修改 `Color` 属性后，所有使用该刷子的控件自动更新
4. 这就是为什么使用 `{StaticResource}` 也能响应主题切换的原因

### 7.5.3 DynamicResource vs StaticResource

```xml
<!-- StaticResource：编译时解析，不可更改 -->
<Button Background="{StaticResource CsPrimaryBrush}"/>

<!-- DynamicResource：运行时解析，支持运行时替换 -->
<Window FontFamily="{DynamicResource CodexSwitch.FontFamily}"/>
```

| 特性 | StaticResource | DynamicResource |
|------|---------------|-----------------|
| 解析时机 | 编译时 | 运行时 |
| 性能 | 更快 | 稍慢 |
| 可更新 | 否 | 是 |
| 用途 | 固定值 | 主题切换、字体切换 |

### 7.5.4 FluentTheme

Avalonia 内置了 Fluent 主题（类似 Windows 11 风格）：

```xml
<Application.Styles>
    <FluentTheme/>

    <!-- 或指定模式 -->
    <FluentTheme Mode="Dark"/>
    <FluentTheme Mode="Light"/>
</Application.Styles>
```

FluentTheme 提供了所有内置控件的默认样式，包括：
- 圆角按钮、输入框
- 平滑的过渡动画
- Material Design 风格的涟漪效果
- 自动适配深色/浅色主题

### 7.5.5 ControlTheme

`ControlTheme` 是定义控件默认主题的方式：

```xml
<Application.Resources>
    <ControlTheme x:Key="{x:Type Button}" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource CsCardBrush}"/>
        <Setter Property="CornerRadius" Value="{StaticResource CsRadiusMd}"/>
        <Setter Property="Template">
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}"
                        CornerRadius="{TemplateBinding CornerRadius}">
                    <ContentPresenter Content="{TemplateBinding Content}"/>
                </Border>
            </ControlTemplate>
        </Setter>
    </ControlTheme>
</Application.Resources>
```

**ControlTheme vs Style**：

| 特性 | ControlTheme | Style |
|------|-------------|-------|
| 定义位置 | Application.Resources | Window/Control.Styles |
| 作用范围 | 全局默认样式 | 局部覆盖 |
| 优先级 | 最低 | 较高 |
| 使用场景 | 定义控件默认外观 | 覆盖特定控件样式 |

## 7.6 过渡动画

### 7.6.1 BrushTransition

```xml
<Style Selector="Border.app-shell">
    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="BorderBrush" Duration="0:0:0.18"/>
            <BrushTransition Property="Background" Duration="0:0:0.18"/>
        </Transitions>
    </Setter>
</Style>
```

### 7.6.2 DoubleTransition

```xml
<Style Selector="Border.card">
    <Setter Property="Transitions">
        <Transitions>
            <DoubleTransition Property="Opacity" Duration="0:0:0.2"/>
            <DoubleTransition Property="CornerRadius" Duration="0:0:0.15"/>
        </Transitions>
    </Setter>
</Style>
```

### 7.6.3 组合过渡

```xml
<Style Selector="Button">
    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="Background" Duration="0:0:0.15"/>
            <BrushTransition Property="BorderBrush" Duration="0:0:0.15"/>
            <DoubleTransition Property="Opacity" Duration="0:0:0.15"/>
        </Transitions>
    </Setter>
</Style>
```

### 7.6.4 可用的过渡类型

| 过渡类型 | 适用属性 | 说明 |
|---------|---------|------|
| `DoubleTransition` | Opacity, Width, Height, FontSize | 浮点数属性 |
| `BrushTransition` | Background, BorderBrush, Foreground | 颜色属性 |
| `ThicknessTransition` | Margin, Padding, BorderThickness | 厚度属性 |
| `CornerRadiusTransition` | CornerRadius | 圆角属性 |
| `BoxShadowsTransition` | BoxShadow | 阴影属性 |

**性能提示**：
- 优先使用 `Opacity`、`RenderTransform` 等不影响布局的属性做动画
- 避免对 `Width`、`Height`、`Margin` 等触发布局的属性做动画
- `BrushTransition` 性能很好，因为颜色插值在 GPU 上完成
- Transitions 必须设置在基础选择器上，而不是伪类选择器上

## 7.7 组件详解大全

### 7.7.1 Setter 的所有属性类型

`Setter` 可以设置任何 Avalonia 属性，以下是常见的属性类型：

```xml
<!-- 基本类型 -->
<Setter Property="Width" Value="200"/>                    <!-- double -->
<Setter Property="IsVisible" Value="False"/>               <!-- bool -->
<Setter Property="Content" Value="Click Me"/>              <!-- string/object -->
<Setter Property="Opacity" Value="0.8"/>                   <!-- double -->

<!-- 颜色和刷子 -->
<Setter Property="Background" Value="Red"/>                <!-- Color -> SolidColorBrush -->
<Setter Property="Foreground" Value="#FF0000"/>            <!-- 字符串 -> SolidColorBrush -->
<Setter Property="BorderBrush" Value="{StaticResource CsBorderBrush}"/>

<!-- 厚度和圆角 -->
<Setter Property="Margin" Value="10"/>                     <!-- 统一值 -->
<Setter Property="Margin" Value="10,20"/>                  <!-- 左右,上下 -->
<Setter Property="Margin" Value="10,20,10,20"/>            <!-- 左,上,右,下 -->
<Setter Property="CornerRadius" Value="8"/>                <!-- 统一值 -->

<!-- 枚举值 -->
<Setter Property="Orientation" Value="Horizontal"/>
<Setter Property="HorizontalAlignment" Value="Center"/>
<Setter Property="FontWeight" Value="SemiBold"/>

<!-- 复杂类型 -->
<Setter Property="Transitions">
    <Transitions>
        <BrushTransition Property="Background" Duration="0:0:0.2"/>
    </Transitions>
</Setter>
<Setter Property="BoxShadow">
    <BoxShadow Blur="10" OffsetX="0" OffsetY="4" Color="#33000000"/>
</Setter>
```

### 7.7.2 样式中的资源定义

样式可以包含自己的局部资源：

```xml
<Style Selector="Button.primary">
    <!-- 定义局部资源 -->
    <Style.Resources>
        <SolidColorBrush x:Key="HoverColor" Color="#33FFFFFF"/>
        <SolidColorBrush x:Key="PressedColor" Color="#22FFFFFF"/>
    </Style.Resources>

    <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>

    <Style Selector="^:pointerover">
        <Setter Property="Background" Value="{StaticResource HoverColor}"/>
    </Style>
</Style>
```

### 7.7.3 自定义样式类

除了内置的伪类，你可以通过 `Classes` 属性添加自定义类：

```xml
<!-- 在 XAML 中添加类 -->
<Button Classes="primary large"/>

<!-- 定义类样式 -->
<Style Selector="Button.large">
    <Setter Property="FontSize" Value="18"/>
    <Setter Property="Padding" Value="24,12"/>
</Style>
```

### 7.7.4 StyleInclude 详解

`StyleInclude` 用于引用外部样式文件：

```xml
<Window.Styles>
    <!-- 引用项目内的样式文件 -->
    <StyleInclude Source="/Styles/Button.axaml"/>
    <StyleInclude Source="/Styles/Input.axaml"/>
    <StyleInclude Source="/Styles/Dialog.axaml"/>

    <!-- 引用组件库的样式 -->
    <StyleInclude Source="avares://CodexSwitchUI/Themes/Button.axaml"/>
</Window.Styles>
```

### 7.7.5 Styles 节点

`Styles` 是样式的容器，可以嵌套：

```xml
<Window.Styles>
    <!-- 内联样式 -->
    <Style Selector="Button.primary">
        <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
    </Style>

    <!-- 引用外部样式文件 -->
    <StyleInclude Source="/Styles/Button.axaml"/>
    <StyleInclude Source="/Styles/Input.axaml"/>
</Window.Styles>
```

## 7.8 CodexSwitch 实战

### 7.8.1 按钮变体系统

```xml
<!-- 主要按钮 -->
<Style Selector="Button.primary">
    <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
    <Setter Property="Foreground" Value="{StaticResource CsPrimaryForegroundBrush}"/>
</Style>
<Style Selector="Button.primary:pointerover">
    <Setter Property="Background" Value="{StaticResource CsPrimaryHoverBrush}"/>
</Style>

<!-- 危险按钮 -->
<Style Selector="Button.destructive">
    <Setter Property="Background" Value="{StaticResource CsDestructiveBrush}"/>
</Style>
<Style Selector="Button.destructive:pointerover">
    <Setter Property="Background" Value="{StaticResource CsDestructiveHoverBrush}"/>
</Style>

<!-- 轮廓按钮 -->
<Style Selector="Button.outline">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderBrush" Value="{StaticResource CsBorderBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
</Style>

<!-- 幽灵按钮 -->
<Style Selector="Button.ghost">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderThickness" Value="0"/>
</Style>
<Style Selector="Button.ghost:pointerover">
    <Setter Property="Background" Value="{StaticResource CsSubtleButtonBrush}"/>
</Style>
```

### 7.8.2 输入框样式

```xml
<Style Selector="TextBox">
    <Setter Property="Background" Value="{StaticResource CsInputBrush}"/>
    <Setter Property="BorderBrush" Value="{StaticResource CsBorderBrush}"/>
    <Setter Property="CornerRadius" Value="{StaticResource CsRadiusMd}"/>
    <Setter Property="Padding" Value="10,8"/>

    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="BorderBrush" Duration="0:0:0.15"/>
        </Transitions>
    </Setter>

    <Style Selector="^:focus">
        <Setter Property="BorderBrush" Value="{StaticResource CsRingBrush}"/>
        <Setter Property="Background" Value="{StaticResource CsInputFocusBackgroundBrush}"/>
    </Style>
    <Style Selector="^:pointerover:not(:focus)">
        <Setter Property="BorderBrush" Value="{StaticResource CsBorderHoverBrush}"/>
    </Style>
    <Style Selector="^:disabled">
        <Setter Property="Opacity" Value="0.5"/>
    </Style>
</Style>
```

### 7.8.3 卡片样式

```xml
<Style Selector="Border.card">
    <Setter Property="Background" Value="{StaticResource CsCardBrush}"/>
    <Setter Property="CornerRadius" Value="{StaticResource CsRadiusLg}"/>
    <Setter Property="Padding" Value="16"/>
    <Setter Property="BorderBrush" Value="{StaticResource CsBorderBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>

    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="BorderBrush" Duration="0:0:0.18"/>
            <BrushTransition Property="Background" Duration="0:0:0.18"/>
        </Transitions>
    </Setter>
</Style>
<Style Selector="Border.card:pointerover">
    <Setter Property="Background" Value="{StaticResource CsCardHoverBrush}"/>
</Style>
<Style Selector="Border.card.active">
    <Setter Property="BorderBrush" Value="{StaticResource CsPrimaryBrush}"/>
    <Setter Property="Background" Value="{StaticResource CsCardActiveBrush}"/>
</Style>
```

### 7.8.4 侧边栏菜单按钮样式

```xml
<!-- 基础样式 -->
<Style Selector="ui|CodexSidebarMenuButton">
    <Setter Property="Height" Value="36"/>
    <Setter Property="Margin" Value="0,2"/>
    <Setter Property="CornerRadius" Value="{StaticResource CsRadiusSm}"/>

    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="Background" Duration="0:0:0.12"/>
        </Transitions>
    </Setter>
</Style>

<!-- 悬停状态 -->
<Style Selector="ui|CodexSidebarMenuButton:pointerover">
    <Setter Property="Background" Value="{StaticResource CsSidebarHoverBrush}"/>
</Style>

<!-- 选中状态 -->
<Style Selector="ui|CodexSidebarMenuButton:checked">
    <Setter Property="Background" Value="{StaticResource CsSidebarActiveBrush}"/>
</Style>

<!-- 模板内部元素样式 -->
<Style Selector="ui|CodexSidebarMenuButton /template/ TextBlock#PART_Title">
    <Setter Property="FontSize" Value="13"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
</Style>
```

## 7.9 举一反三

### 7.9.1 从 CSS 到 Avalonia 样式

如果你熟悉 CSS，这个对照表会帮助你快速上手：

| CSS | Avalonia | 说明 |
|-----|----------|------|
| `button { }` | `Style Selector="Button"` | 类型选择器 |
| `.primary { }` | `Style Selector=".primary"` | 类选择器 |
| `#myBtn { }` | `Style Selector="#MyBtn"` | ID 选择器 |
| `button:hover { }` | `Style Selector="Button:pointerover"` | 伪类 |
| `button.primary { }` | `Style Selector="Button.primary"` | 组合选择器 |
| `background: red` | `Setter Property="Background" Value="Red"` | 属性设置 |
| `transition: all 0.3s` | `<DoubleTransition Property="Opacity" Duration="0:0:0.3"/>` | 过渡 |
| `var(--color)` | `{StaticResource CsColorBrush}` | 变量 |
| `@media (prefers-color-scheme: dark)` | `ThemeVariant.Dark` | 主题 |
| `!important` | 内联样式 `<Button Background="Red"/>` | 优先级覆盖 |

### 7.9.2 样式与代码的配合

样式可以在代码中动态操作：

```csharp
// 运行时添加样式类
button.Classes.Add("primary");
button.Classes.Add("large");

// 检查是否包含样式类
bool isPrimary = button.Classes.Contains("primary");

// 替换样式类
button.Classes.Replace("primary", "secondary");

// 使用 PseudoClasses
button.PseudoClasses.Add(":custom-state");
```

### 7.9.3 样式调试技巧

使用 Avalonia DevTools 调试样式问题：

1. 按 F12 打开 DevTools
2. 在"Visual Tree"面板选择控件
3. 在"Styles"面板查看所有匹配的样式
4. 查看样式优先级和覆盖关系
5. 检查 `Classes` 和 `PseudoClasses` 是否正确
6. 使用"Computed"面板查看最终生效的属性值

## 7.10 最佳实践与设计模式

### 7.10.1 设计令牌模式

```xml
<!-- 好：使用语义化命名 -->
<Style Selector="Button.primary">
    <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
</Style>

<!-- 差：直接使用颜色值 -->
<Style Selector="Button.primary">
    <Setter Property="Background" Value="#E5E5E5"/>
</Style>

<!-- 差：使用非语义化命名 -->
<Style Selector="Button.primary">
    <Setter Property="Background" Value="{StaticResource CsLightGrayBrush}"/>
</Style>
```

### 7.10.2 样式分层

```
Layer 1: 设计令牌 (CsBackgroundBrush, CsPrimaryBrush, ...)
    ↓
Layer 2: 控件默认样式 (Button, TextBox, ...)
    ↓
Layer 3: 变体样式 (Button.primary, Button.destructive, ...)
    ↓
Layer 4: 页面特定样式 (Page-specific overrides)
```

每一层只覆盖需要改变的属性，避免重复定义。

### 7.10.3 样式命名规范

- 使用小写和连字符：`.primary`, `.destructive`, `.outline`
- 语义化命名：`.primary` 而非 `.blue`
- 状态命名：`.active`, `.disabled`, `.selected`
- 尺寸命名：`.small`, `.medium`, `.large`
- 模板部分命名：`PART_Title`, `PART_Icon`（使用 `PART_` 前缀）

### 7.10.4 性能优化

1. **优先使用 StaticResource**：编译时解析，性能最好
2. **避免深层嵌套选择器**：`StackPanel TextBlock` 比 `Grid StackPanel Border TextBlock` 快
3. **使用 ^ 引用父选择器**：避免重复书写长选择器
4. **合理使用 Transition**：只对需要动画的属性添加过渡
5. **避免在样式中使用绑定**：样式中的 Setter 应该使用 StaticResource
6. **减少样式数量**：合并相似的样式，使用类选择器组合
7. **分离样式文件**：按组件拆分样式文件，使用 StyleInclude 引用

## 7.11 Deep Dive

### 7.11.1 样式解析流程

```
控件进入可视化树
    ↓
样式引擎扫描所有 Style 节点
    ↓
选择器匹配（类型、类名、伪类、属性）
    ↓
计算优先级（特异性排序）
    ↓
应用 Setter 到 StyledProperty
    ↓
缓存匹配结果
```

### 7.11.2 特异性计算规则

选择器的特异性由三部分组成：`(ID数, 类数, 类型数)`

```
Button               → (0, 0, 1)
.primary             → (0, 1, 0)
Button.primary       → (0, 1, 1)
#MyButton            → (1, 0, 0)
Button.primary.large → (0, 2, 1)
```

比较时从左到右，数字大的优先级更高。

### 7.11.3 样式优先级

| 优先级 | 选择器类型 | 示例 | 特异性 |
|--------|-----------|------|--------|
| 最低 | 类型选择器 | `Button` | (0,0,1) |
| 低 | 类选择器 | `.primary` | (0,1,0) |
| 低 | 伪类选择器 | `:pointerover` | (0,1,0) |
| 中 | 属性选择器 | `[IsEnabled=true]` | (0,1,0) |
| 高 | 命名选择器 | `#MyButton` | (1,0,0) |
| 最高 | 内联样式 | `<Button Background="Red"/>` | N/A |

### 7.11.4 ThemeVariant 系统内部

**主题变体的工作原理**：

1. `RequestedThemeVariant` 是请求的主题，`ActualThemeVariant` 是实际应用的主题
2. 当设置为 `Default` 时，`ActualThemeVariant` 跟随系统设置
3. 主题变化时，Avalonia 重新评估所有使用 `DynamicResource` 的资源引用
4. 样式中的 `ThemeVariants` 属性允许为不同主题定义不同值

**CodexSwitch 的主题切换策略**：

CodexSwitch 没有使用传统的 `DynamicResource` 方式，而是采用了"就地修改 `SolidColorBrush.Color`"的策略：

```csharp
// 传统方式：定义两套资源，切换主题时重新加载

// CodexSwitch 方式：修改同一个 SolidColorBrush 的 Color 属性
if (app.TryGetResource(key, app.ActualThemeVariant, out var resource) &&
    resource is SolidColorBrush brush)
{
    brush.Color = color;  // 修改 Color，所有引用处自动更新
}
```

这种方式的优点：
- 不需要定义多套资源字典
- 所有 `{StaticResource}` 引用都能自动更新
- 实现简单，易于维护

### 7.11.5 样式系统的内存模型

```
┌─────────────────────────────────────────────┐
│ Style Host (Window/UserControl)              │
│  ┌─────────────────────────────────────┐     │
│  │ Style Children[]                     │     │
│  │  ├─ Style { Selector, Setters[] }   │     │
│  │  ├─ StyleInclude { Source }         │     │
│  │  └─ Style { Selector, Children[] }  │     │
│  └─────────────────────────────────────┘     │
│                                              │
│  ┌─────────────────────────────────────┐     │
│  │ Style Cache                          │     │
│  │  ├─ { ControlType -> Style[] }       │     │
│  │  └─ { Classes -> Style[] }           │     │
│  └─────────────────────────────────────┘     │
└─────────────────────────────────────────────┘
```

Avalonia 的样式引擎会缓存选择器匹配结果，避免每次属性变化时重新计算。缓存键包括：
- 控件类型
- 样式类集合
- 伪类状态

当控件的类或伪类变化时，缓存会失效并重新计算。

### 7.11.6 样式与模板的协作

样式和模板是协作关系：

1. 样式设置属性值（`Setter`）
2. 模板读取属性值并渲染（`TemplateBinding`）
3. 伪类触发样式变化
4. 样式变化触发模板重新渲染

```xml
<!-- 样式设置 Background -->
<Style Selector="Button.primary">
    <Setter Property="Background" Value="Red"/>
</Style>

<!-- 模板读取 Background 并渲染 -->
<ControlTemplate TargetType="Button">
    <Border Background="{TemplateBinding Background}">
        <ContentPresenter/>
    </Border>
</ControlTemplate>
```

### 7.11.7 样式继承链

Avalonia 的样式系统支持视觉树继承：

```
Window (定义全局样式)
├── StackPanel (继承 Window 的样式)
│   ├── Button (继承 StackPanel 的样式)
│   └── TextBox (继承 StackPanel 的样式)
└── Border (定义局部样式)
    └── Button (继承 Border 的局部样式，覆盖全局样式)
```

**继承规则**：
- 子控件自动继承父控件的样式资源
- 子控件的局部样式优先于父控件的样式
- 应用级样式优先级最低

## 7.12 Cross References

- **[第 1 章：Avalonia 概览](01-avalonia-overview.md)** — 了解 Avalonia 的整体架构
- **[第 2 章：项目结构](02-project-structure.md)** — 了解 App.axaml 和样式文件的组织
- **[第 3 章：AXAML 基础](03-axaml-fundamentals.md)** — 了解 AXAML 语法和标记扩展
- **[第 5 章：数据绑定](05-data-binding.md)** — 了解 TemplateBinding 的底层原理
- **[第 6 章：MVVM 模式](06-mvvm-pattern.md)** — 了解 ViewModel 与样式的配合
- **[第 8 章：DataTemplate](08-data-templates.md)** — 了解 DataTemplate 中的样式
- **[第 9 章：自定义控件](09-custom-controls.md)** — ControlTemplate 的高级用法

## 7.13 Common Pitfalls

### 陷阱 1：伪类拼写错误

```xml
<!-- 错误：CSS 风格的 :hover -->
<Style Selector="Button:hover">

<!-- 正确：Avalonia 的 :pointerover -->
<Style Selector="Button:pointerover">
```

### 陷阱 2：模板选择器命名空间

```xml
<!-- 错误：缺少命名空间 -->
<Style Selector="CsButton">

<!-- 正确：使用命名空间前缀 -->
<Style Selector="ui|CsButton">
```

### 陷阱 3：过渡动画位置错误

```xml
<!-- 错误：Transitions 放在伪类上 -->
<Style Selector="Button:pointerover">
    <Setter Property="Transitions">...</Setter>
</Style>

<!-- 正确：Transitions 放在基础选择器上 -->
<Style Selector="Button">
    <Setter Property="Transitions">...</Setter>
</Style>
```

### 陷阱 4：StaticResource 引用不存在的资源

```xml
<!-- 错误：资源名拼写错误，编译时报错 -->
<Button Background="{StaticResource CsPrimayBrush}"/>

<!-- 正确：确保资源名正确 -->
<Button Background="{StaticResource CsPrimaryBrush}"/>
```

### 陷阱 5：混淆 TemplateBinding 和 Binding

```xml
<!-- 错误：在 ControlTemplate 外使用 TemplateBinding -->
<TextBlock Text="{TemplateBinding Title}"/>

<!-- 正确：TemplateBinding 只能在 ControlTemplate 内使用 -->
<TextBlock Text="{Binding Title, RelativeSource={RelativeSource AncestorType=Window}}"/>
```

### 陷阱 6：忘记设置 ControlTemplate 的 TargetType

```xml
<!-- 错误：没有 TargetType -->
<ControlTemplate>
    <Border Background="{TemplateBinding Background}"/>
</ControlTemplate>

<!-- 正确：设置 TargetType -->
<ControlTemplate TargetType="Button">
    <Border Background="{TemplateBinding Background}"/>
</ControlTemplate>
```

### 陷阱 7：ControlTemplate 中缺少 ContentPresenter

```xml
<!-- 错误：忘记添加 ContentPresenter，内容不会显示 -->
<ControlTemplate TargetType="Button">
    <Border Background="{TemplateBinding Background}"/>
</ControlTemplate>

<!-- 正确：必须添加 ContentPresenter -->
<ControlTemplate TargetType="Button">
    <Border Background="{TemplateBinding Background}">
        <ContentPresenter Content="{TemplateBinding Content}"/>
    </Border>
</ControlTemplate>
```

### 陷阱 8：样式中的绑定表达式

```xml
<!-- 错误：样式中的 Setter 不支持复杂绑定 -->
<Style Selector="Button">
    <Setter Property="Background" Value="{Binding MyColor}"/>
</Style>

<!-- 正确：样式中使用 StaticResource -->
<Style Selector="Button">
    <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
</Style>
```

### 陷阱 9：Classes 与 PseudoClasses 混淆

```xml
<!-- 错误：使用 PseudoClass 语法设置自定义类 -->
<Button Classes=":primary"/>

<!-- 正确：自定义类不需要冒号 -->
<Button Classes="primary"/>

<!-- 伪类才需要冒号，它们是系统自动管理的 -->
<!-- :pointerover, :pressed, :focus 等是伪类 -->
```

### 陷阱 10：StyleInclude 的路径格式

```xml
<!-- 错误：路径格式不正确 -->
<StyleInclude Source="Styles/Button.axaml"/>

<!-- 正确：使用完整路径 -->
<StyleInclude Source="/Styles/Button.axaml"/>

<!-- 或者引用程序集资源 -->
<StyleInclude Source="avares://AssemblyName/Styles/Button.axaml"/>
```

### 陷阱 11：样式的作用域

```xml
<!-- 问题：Window 中定义的样式对子窗口无效 -->
<Window.Styles>
    <Style Selector="Button.primary">
        <Setter Property="Background" Value="Red"/>
    </Style>
</Window.Styles>

<!-- 解决：将全局样式放在 Application.Styles 中 -->
<Application.Styles>
    <Style Selector="Button.primary">
        <Setter Property="Background" Value="Red"/>
    </Style>
</Application.Styles>
```

### 陷阱 12：CornerRadius 的值格式

```xml
<!-- 错误：使用逗号分隔 -->
<Setter Property="CornerRadius" Value="8,8,8,8"/>

<!-- 正确：使用空格分隔 -->
<Setter Property="CornerRadius" Value="8 8 8 8"/>

<!-- 或者使用统一值 -->
<Setter Property="CornerRadius" Value="8"/>
```

## 7.14 Try It Yourself

### 练习 1：创建自定义按钮变体

创建一个 `.success` 按钮变体，使用绿色背景（`CsSuccessBrush`），包含 hover 和 pressed 状态：

```xml
<Style Selector="Button.success">
    <Setter Property="Background" Value="{StaticResource CsSuccessBrush}"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="CornerRadius" Value="{StaticResource CsRadiusMd}"/>
    <Setter Property="Padding" Value="16,8"/>

    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="Background" Duration="0:0:0.18"/>
        </Transitions>
    </Setter>

    <Style Selector="^:pointerover">
        <Setter Property="Background" Value="{StaticResource CsSuccessHoverBrush}"/>
    </Style>
    <Style Selector="^:pressed">
        <Setter Property="Background" Value="{StaticResource CsSuccessPressedBrush}"/>
    </Style>
    <Style Selector="^:disabled">
        <Setter Property="Opacity" Value="0.5"/>
    </Style>
</Style>
```

### 练习 2：实现主题切换 UI

在设置页面中，添加一个主题切换开关（深色/浅色/跟随系统），使用 `AppThemeService.Apply()` 切换主题：

```xml
<RadioButton GroupName="Theme" Content="深色"
             Command="{Binding SetThemeCommand}"
             CommandParameter="dark"/>
<RadioButton GroupName="Theme" Content="浅色"
             Command="{Binding SetThemeCommand}"
             CommandParameter="light"/>
<RadioButton GroupName="Theme" Content="跟随系统"
             Command="{Binding SetThemeCommand}"
             CommandParameter="system"/>
```

### 练习 3：调试样式问题

1. 故意拼错一个选择器（如 `Button.prmary`），观察样式不生效
2. 使用 Avalonia DevTools 检查控件的 Applied Styles
3. 修复选择器，确认样式生效

### 练习 4：创建 ControlTemplate

为一个自定义控件创建 ControlTemplate，使用 `TemplateBinding` 绑定 Background、Padding、CornerRadius 等属性。

### 练习 5：实现过渡动画

为按钮添加 hover 过渡动画，让背景色在 150ms 内平滑变化。

### 练习 6：使用模板选择器

创建一个自定义控件，在样式中使用 `/template/` 选择器修改模板内部元素的外观。

### 练习 7：组织样式文件

将 CodexSwitch 的样式拆分为多个文件（Button.axaml、Input.axaml、Card.axaml），使用 `StyleInclude` 在 CodexTheme.axaml 中引用。

### 练习 8：实现卡片悬停效果

创建一个带悬停动画的卡片组件，使用 `BrushTransition` 让边框颜色在鼠标悬停时平滑变化：

```xml
<Style Selector="Border.hover-card">
    <Setter Property="Background" Value="{StaticResource CsCardBrush}"/>
    <Setter Property="CornerRadius" Value="{StaticResource CsRadiusLg}"/>
    <Setter Property="Padding" Value="16"/>
    <Setter Property="BorderBrush" Value="{StaticResource CsBorderBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>

    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="BorderBrush" Duration="0:0:0.2"/>
            <BrushTransition Property="Background" Duration="0:0:0.2"/>
        </Transitions>
    </Setter>
</Style>

<Style Selector="Border.hover-card:pointerover">
    <Setter Property="BorderBrush" Value="{StaticResource CsPrimaryBrush}"/>
    <Setter Property="Background" Value="{StaticResource CsCardHoverBrush}"/>
</Style>
```

### 练习 9：实现响应式样式

通过代码检测窗口大小并切换样式类，在小屏幕上隐藏侧边栏：

```csharp
public class MainWindow : Window
{
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (e.NewSize.Width < 800)
            Sidebar.Classes.Add("collapsed");
        else
            Sidebar.Classes.Remove("collapsed");
    }
}
```

### 练习 10：性能测试

对比不同选择器的性能：
1. 创建 1000 个 Button
2. 使用简单选择器（`Button`）定义样式
3. 使用复杂选择器（`Grid StackPanel Border Button.primary`）定义样式
4. 使用 DevTools 的性能分析工具比较两者的渲染时间
5. 记录结论并优化选择器
