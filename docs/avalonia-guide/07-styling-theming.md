# 7. 样式与主题系统

## 7.1 样式架构

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
        ├── SegmentedControl.axaml
        ├── ProviderCard.axaml
        └── ...
```

## 7.2 设计令牌 (Design Tokens)

### 色彩系统

```xml
<!-- CodexTheme.axaml 中定义的色彩令牌 -->
<Styles.Resources>
    <!-- 基础色 -->
    <SolidColorBrush x:Key="CsBackgroundBrush" Color="#171717"/>
    <SolidColorBrush x:Key="CsForegroundBrush" Color="#FAFAFA"/>
    <SolidColorBrush x:Key="CsCardBrush" Color="#262626"/>

    <!-- 语义色 -->
    <SolidColorBrush x:Key="CsPrimaryBrush" Color="#E5E5E5"/>
    <SolidColorBrush x:Key="CsDestructiveBrush" Color="#FF6467"/>
    <SolidColorBrush x:Key="CsSuccessBrush" Color="#36D399"/>
    <SolidColorBrush x:Key="CsWarningBrush" Color="#FACC15"/>

    <!-- 交互状态色 -->
    <SolidColorBrush x:Key="CsPrimaryHoverBrush" Color="#D1D1D1"/>
    <SolidColorBrush x:Key="CsPrimaryPressedBrush" Color="#C7C7C7"/>
    <SolidColorBrush x:Key="CsDestructiveHoverBrush" Color="#E85C5F"/>

    <!-- 组件专用色 -->
    <SolidColorBrush x:Key="CsSidebarPrimaryBrush" Color="#5277FF"/>
    <SolidColorBrush x:Key="CsProviderCardActiveBrush" Color="#142235"/>
    <SolidColorBrush x:Key="CsDialogCardBrush" Color="#F01A202C"/>
</Styles.Resources>
```

### 圆角令牌

```xml
<CornerRadius x:Key="CsRadiusSm">6</CornerRadius>
<CornerRadius x:Key="CsRadiusMd">8</CornerRadius>
<CornerRadius x:Key="CsRadiusLg">10</CornerRadius>
<CornerRadius x:Key="CsRadiusXl">14</CornerRadius>
```

使用方式：
```xml
<Border CornerRadius="{StaticResource CsRadiusMd}"/>
```

## 7.3 主题切换实现

`AppThemeService` 展示了完整的运行时主题切换方案：

```csharp
public static class AppThemeService
{
    private static string _theme = "system";

    // 每个资源键对应深色/浅色两种颜色
    private static readonly IReadOnlyDictionary<string, ThemeColorPair> ThemeBrushes =
        new Dictionary<string, ThemeColorPair>
        {
            ["CsBackgroundBrush"]      = new("#171717", "#FAFAFA"),  // Dark, Light
            ["CsForegroundBrush"]      = new("#FAFAFA", "#09090B"),
            ["CsCardBrush"]            = new("#262626", "#FFFFFF"),
            ["CsDestructiveBrush"]     = new("#FF6467", "#DC2626"),
            // ... 70+ 颜色对
        };

    public static void Apply(string? theme)
    {
        var app = Application.Current;
        _theme = Normalize(theme);

        // 1. 设置 ThemeVariant（Avalonia 内置主题系统）
        app.RequestedThemeVariant = _theme switch
        {
            "light" => ThemeVariant.Light,
            "dark"  => ThemeVariant.Dark,
            _       => ThemeVariant.Default  // 跟随系统
        };

        // 2. 更新组件库主题
        ApplyComponentLibraryTheme(app);

        // 3. 更新所有色彩资源
        ApplyBrushes(app);
    }

    private static void ApplyBrushes(Application app)
    {
        var light = _theme == "light" ||
            (_theme == "system" && app.ActualThemeVariant == ThemeVariant.Light);

        foreach (var pair in ThemeBrushes)
            ApplyBrush(app, pair.Key, light ? pair.Value.Light : pair.Value.Dark);
    }

    // 关键：直接修改现有 SolidColorBrush 的 Color 属性
    // 这样所有引用该 Brush 的控件会自动更新
    private static void ApplyBrush(Application app, string key, string colorText)
    {
        var color = Color.Parse(colorText);

        if (app.TryGetResource(key, app.ActualThemeVariant, out var resource) &&
            resource is SolidColorBrush brush)
        {
            brush.Color = color;  // 就地修改，所有引用处自动更新
            return;
        }

        app.Resources[key] = new SolidColorBrush(color);
    }

    private readonly record struct ThemeColorPair(string Dark, string Light);
}
```

### 核心技巧

**直接修改 `SolidColorBrush.Color`** 而非替换整个 Brush 对象。因为 AXAML 中的 `{StaticResource CsPrimaryBrush}` 返回的是同一个 Brush 实例的引用，修改它的 Color 属性会自动通知所有绑定到它的 UI 元素。

## 7.4 Avalonia 的 ThemeVariant

Avalonia 11+ 内置了主题变体系统：

```xml
<!-- 跟随系统 -->
<Application RequestedThemeVariant="Default">

<!-- 强制深色 -->
<Application RequestedThemeVariant="Dark">

<!-- 强制浅色 -->
<Application RequestedThemeVariant="Light">
```

在样式中可以为不同变体提供不同值：
```xml
<Application.Resources>
    <Color x:Key="MyColor" x:ThemeVariant="Light">White</Color>
    <Color x:Key="MyColor" x:ThemeVariant="Dark">Black</Color>
</Application.Resources>
```

但 CodexSwitch 选择手动管理颜色映射，因为需要更精细的控制（70+ 颜色对）。

## 7.5 组件样式模式

### 按钮样式（变体模式）

```xml
<!-- Button.axaml 定义多种变体 -->
<Style Selector="Button.primary">
    <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
    <Setter Property="Foreground" Value="{StaticResource CsPrimaryForegroundBrush}"/>
</Style>
<Style Selector="Button.primary:pointerover">
    <Setter Property="Background" Value="{StaticResource CsPrimaryHoverBrush}"/>
</Style>

<Style Selector="Button.destructive">
    <Setter Property="Background" Value="{StaticResource CsDestructiveBrush}"/>
</Style>
<Style Selector="Button.destructive:pointerover">
    <Setter Property="Background" Value="{StaticResource CsDestructiveHoverBrush}"/>
</Style>

<Style Selector="Button.outline">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderBrush" Value="{StaticResource CsBorderBrush}"/>
</Style>

<Style Selector="Button.ghost">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderBrush" Value="Transparent"/>
</Style>
```

使用：
```xml
<Button Classes="primary" Content="Save"/>
<Button Classes="destructive" Content="Delete"/>
<Button Classes="outline ghost" Content="Cancel"/>  <!-- 组合多个 class -->
```

### 尺寸变体

```xml
<Style Selector="Button.size-sm">
    <Setter Property="Height" Value="32"/>
    <Setter Property="Padding" Value="12,6"/>
    <Setter Property="FontSize" Value="12"/>
</Style>

<Style Selector="Button.size-lg">
    <Setter Property="Height" Value="44"/>
    <Setter Property="Padding" Value="24,10"/>
    <Setter Property="FontSize" Value="15"/>
</Style>
```

## 7.6 过渡动画

在样式中定义过渡，让属性变化自动动画化：

```xml
<Style Selector="Border.app-shell">
    <Setter Property="BorderBrush" Value="{StaticResource CsBorderBrush}"/>
    <Setter Property="Transitions">
        <Transitions>
            <!-- BorderBrush 变化时，180ms 渐变过渡 -->
            <BrushTransition Property="BorderBrush" Duration="0:0:0.18"/>
        </Transitions>
    </Setter>
</Style>
<Style Selector="Border.app-shell.alert">
    <Setter Property="BorderBrush" Value="{StaticResource CsAlertBorderBrush}"/>
</Style>
```

当 `.alert` class 被添加/移除时，边框颜色会平滑过渡。

### 过渡类型

| 类型 | 用途 |
|------|------|
| `DoubleTransition` | 数值属性（Opacity、Width 等） |
| `BrushTransition` | 画刷属性（Background、Foreground） |
| `TransformOperationsTransition` | 变换操作 |
| `ColorTransition` | 颜色属性 |

## 7.7 DynamicResource vs StaticResource

```xml
<!-- StaticResource：编译时解析，不可更改 -->
<Button Background="{StaticResource CsPrimaryBrush}"/>

<!-- DynamicResource：运行时解析，支持运行时替换 -->
<Window FontFamily="{DynamicResource CodexSwitch.FontFamily}"/>
```

CodexSwitch 的字体配置使用 DynamicResource，因为需要运行时切换字体：

```csharp
// AppFonts.cs
public static class AppFonts
{
    // avares:// URI 引用嵌入的字体文件
    public const string DefaultFontFamily =
        "avares://CodexSwitch/Assets/Fonts/AlibabaPuHuiTi#Alibaba PuHuiTi 3.0";
}
```

## 7.8 样式选择器完整参考

| 选择器 | 示例 | 说明 |
|--------|------|------|
| 类型 | `Button` | 匹配所有 Button 控件 |
| 命名 | `#MyButton` | 匹配 Name="MyButton" |
| Class | `.primary` | 匹配 Classes 包含 "primary" |
| 伪类 | `:pointerover` | 匹配鼠标悬停状态 |
| 伪类 | `:pressed` | 匹配按下状态 |
| 伪类 | `:focus` | 匹配聚焦状态 |
| 伪类 | `:disabled` | 匹配禁用状态 |
| 伪类 | `:checked` | 匹配选中状态（CheckBox） |
| 伪类 | `:selected` | 匹配选中状态（自定义） |
| 子模板 | `/template/ Border` | 匹配模板内的 Border |
| 子模板 | `/template/ Border#PART_Root` | 匹配模板内名为 PART_Root 的 Border |
| 属性 | `[IsEnabled=true]` | 匹配属性值 |
| 属性 | `[IsVisible=false]` | 匹配属性值 |
| OR | `Button, TextBlock` | 匹配任一 |
| AND | `Button.primary` | 同时满足 |

## 7.9 深入：样式系统内部原理

### 样式解析流程

当 Avalonia 加载 AXAML 时，样式系统的处理分为几个阶段：

1. **解析阶段**：AXAML 解析器将 `<Style>` 元素转换为 `Style` 对象，每个 `Setter` 转换为 `Setter` 对象
2. **匹配阶段**：当控件进入可视化树时，样式引擎遍历所有已注册的样式，用选择器匹配控件
3. **应用阶段**：匹配的 Setter 按优先级顺序应用到控件的属性系统
4. **缓存阶段**：匹配结果被缓存，避免重复计算

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

### 样式优先级（特异性）

当多个样式匹配同一个控件时，Avalonia 按特异性（specificity）排序：

| 优先级 | 选择器类型 | 示例 |
|--------|-----------|------|
| 最低 | 类型选择器 | `Button` |
| 中 | 类选择器 | `.primary` |
| 中 | 伪类选择器 | `:pointerover` |
| 高 | 命名选择器 | `#MyButton` |
| 最高 | 内联样式 | `<Button Background="Red"/>` |

特异性相同时，后定义的样式覆盖先定义的。这与 CSS 的层叠规则类似。

### 样式继承

Avalonia 的样式继承与 CSS 不同。某些属性（如 `FontFamily`、`FontSize`、`Foreground`）会沿可视化树向下传播：

```xml
<Window FontFamily="Arial">
    <StackPanel>
        <!-- 这个 TextBlock 会继承 Window 的 FontFamily -->
        <TextBlock Text="Hello"/>
    </StackPanel>
</Window>
```

继承通过 `Inherits` 标记的 `StyledProperty` 实现。在属性注册时：

```csharp
// 带继承的属性注册
public static readonly StyledProperty<FontFamily> FontFamilyProperty =
    TextBlock.FontFamilyProperty.AddOwner<CsMyControl>();
```

### 样式缓存机制

Avalonia 对样式匹配结果进行缓存以提高性能：

- 控件的类名（Classes）变化时，缓存失效
- 伪类状态变化时，缓存部分失效
- 样式树结构变化时（添加/移除 Style），全局缓存失效

缓存键由控件类型、类名集合、伪类集合组合而成。

## 7.10 深入：ThemeVariant 系统内部

### ThemeVariant 的工作原理

`ThemeVariant` 是 Avalonia 11 引入的内置主题变体系统。它不是一个简单的字符串切换，而是一套完整的资源解析机制：

```csharp
// ThemeVariant 本质上是一个标识符
public sealed class ThemeVariant : IEquatable<ThemeVariant>
{
    public static readonly ThemeVariant Light = new("Light");
    public static readonly ThemeVariant Dark = new("Dark");
    public static readonly ThemeVariant Default = new("Default");
}
```

当 `Application.RequestedThemeVariant` 设置为 `Default` 时，Avalonia 会查询操作系统的主题偏好（通过平台 API），然后设置 `ActualThemeVariant` 为 `Light` 或 `Dark`。

### 资源解析与 ThemeVariant

资源系统在解析 `{StaticResource}` 或 `{DynamicResource}` 时，会考虑当前的 `ActualThemeVariant`：

```xml
<Application.Resources>
    <!-- 同一个 Key 可以有多个 ThemeVariant 的值 -->
    <Color x:Key="MyColor" x:ThemeVariant="Light">White</Color>
    <Color x:Key="MyColor" x:ThemeVariant="Dark">Black</Color>
    <!-- 没有指定 ThemeVariant 的是通用值 -->
    <Color x:Key="MyColor">Gray</Color>
</Application.Resources>
```

解析顺序：
1. 精确匹配当前 `ActualThemeVariant` 的资源
2. 没有指定 `ThemeVariant` 的通用资源
3. 父级资源字典中的资源

### 为什么 CodexSwitch 手动管理主题

CodexSwitch 选择手动管理 70+ 颜色对而不是使用 `x:ThemeVariant`，原因包括：

1. **精细控制**：需要为每个颜色对单独定义深色/浅色值，`x:ThemeVariant` 会导致 AXAML 文件膨胀
2. **组件库同步**：需要同步更新外部组件库（CodexSwitchUI）的主题
3. **就地修改**：通过修改 `SolidColorBrush.Color` 属性实现即时更新，无需重建资源树
4. **AOT 兼容**：避免运行时资源解析的反射开销

```csharp
// CodexSwitch 的做法：直接修改现有 Brush 的 Color
private static void ApplyBrush(Application app, string key, string colorText)
{
    var color = Color.Parse(colorText);
    if (app.TryGetResource(key, app.ActualThemeVariant, out var resource) &&
        resource is SolidColorBrush brush)
    {
        brush.Color = color;  // 就地修改，所有引用处自动更新
        return;
    }
    app.Resources[key] = new SolidColorBrush(color);
}
```

## 7.11 深入：CSS-like 选择器详解

### 选择器组合器

Avalonia 支持多种选择器组合方式：

```xml
<!-- 后代选择器：匹配 Grid 内部的所有 Button -->
<Style Selector="Grid Button">
<!-- 子选择器：匹配 Grid 直接子元素的 Button -->
<Style Selector="Grid > Button">
<!-- 模板选择器：匹配控件模板内部的元素 -->
<Style Selector="Button /template/ Border">
<!-- 命名模板元素 -->
<Style Selector="Button /template/ Border#PART_Root">
```

### 选择器链与特异性计算

选择器链的特异性由各部分的权重累加：

```xml
<!-- 特异性：1（类型） + 1（类） = 2 -->
<Style Selector="Button.primary">

<!-- 特异性：1（类型） + 2（类） = 3 -->
<Style Selector="Button.primary.large">

<!-- 特异性：1（类型） + 1（伪类） = 2 -->
<Style Selector="Button:pointerover">

<!-- 特异性：1（类型） + 1（类） + 1（伪类） = 3 -->
<Style Selector="Button.primary:pointerover">
```

### 模板选择器深度解析

模板选择器 `/template/` 是 Avalonia 特有的，用于穿透控件模板：

```xml
<!-- 改变所有 Button 模板内部的 Border 背景 -->
<Style Selector="Button /template/ Border">
    <Setter Property="CornerRadius" Value="8"/>
</Style>

<!-- 只改变特定命名的模板部件 -->
<Style Selector="Button:focus-visible /template/ Border#PART_ButtonRoot">
    <Setter Property="BoxShadow" Value="0 0 0 3 #808E8E8E"/>
</Style>
```

CodexSwitch 中的实际使用：

```xml
<!-- 按钮聚焦时的发光效果 -->
<Style Selector="ui|CsButton:focus-visible /template/ Border#PART_ButtonRoot">
    <Setter Property="BoxShadow" Value="0 0 0 3 #808E8E8E"/>
</Style>
```

### 属性选择器

属性选择器基于控件的属性值匹配：

```xml
<!-- 匹配 IsEnabled=false 的控件 -->
<Style Selector="[IsEnabled=false]">

<!-- 匹配 Tag="special" 的控件 -->
<Style Selector="[Tag=special]">

<!-- 组合使用 -->
<Style Selector="Button[IsVisible=true]">
```

## 7.12 深入：样式性能优化

### 样式匹配的性能影响

样式匹配发生在以下时机：
- 控件首次进入可视化树
- 控件的 Classes 属性变化
- 控件的伪类状态变化
- 样式树结构变化

### 优化策略

**1. 减少选择器复杂度**

```xml
<!-- 差：深层嵌套选择器 -->
<Style Selector="Grid StackPanel Border TextBlock">
<!-- 好：直接类型或类选择器 -->
<Style Selector="TextBlock.muted">
```

**2. 使用类选择器而非属性选择器**

```xml
<!-- 差：属性选择器（每次属性变化都重新匹配） -->
<Style Selector="[IsVisible=true]">
<!-- 好：类选择器（只在类名变化时匹配） -->
<Style Selector=".visible">
```

**3. 避免过度使用 DynamicResource**

```xml
<!-- 差：DynamicResource（运行时解析，每次访问都查找） -->
<Button Background="{DynamicResource CsPrimaryBrush}"/>
<!-- 好：StaticResource（编译时解析，直接引用） -->
<Button Background="{StaticResource CsPrimaryBrush}"/>
```

**4. 缓存 SolidColorBrush 实例**

```csharp
// 好：静态缓存
private static readonly SolidColorBrush CachedBrush = new(Colors.Red);

// 差：每次创建新实例
var brush = new SolidColorBrush(Colors.Red);
```

**5. 合理使用 Transitions**

```xml
<!-- 差：对所有属性都加过渡 -->
<Setter Property="Transitions">
    <Transitions>
        <BrushTransition Property="Background" Duration="0:0:0.3"/>
        <BrushTransition Property="Foreground" Duration="0:0:0.3"/>
        <DoubleTransition Property="Opacity" Duration="0:0:0.3"/>
        <DoubleTransition Property="Width" Duration="0:0:0.3"/>
    </Transitions>
</Setter>

<!-- 好：只对需要的属性加过渡 -->
<Setter Property="Transitions">
    <Transitions>
        <BrushTransition Property="Background" Duration="0:0:0.14"/>
    </Transitions>
</Setter>
```

## 7.13 深入：自定义主题实现

### 创建自定义主题类

如果需要创建可复用的主题系统，可以实现自定义主题：

```csharp
public class CustomTheme : AvaloniaObject, IStyleHost
{
    public IStyleHost? StylingParent => null;

    public Styles Styles { get; } = new();

    public CustomTheme()
    {
        // 加载主题资源
        var uri = new Uri("avares://MyApp/Styles/Theme.axaml");
        Styles.Add(new StyleInclude(uri) { Source = uri });
    }

    public void Apply(Application app, ThemeVariant variant)
    {
        // 更新资源
        app.Resources["PrimaryBrush"] = new SolidColorBrush(
            variant == ThemeVariant.Light ? Colors.Blue : Colors.LightBlue);
    }
}
```

### 主题切换的最佳实践

```csharp
public class ThemeManager
{
    private readonly Dictionary<string, ThemePreset> _presets = new();

    public void RegisterPreset(string name, ThemePreset preset)
    {
        _presets[name] = preset;
    }

    public void ApplyPreset(string name)
    {
        if (!_presets.TryGetValue(name, out var preset))
            return;

        var app = Application.Current;
        if (app is null) return;

        // 1. 更新 ThemeVariant
        app.RequestedThemeVariant = preset.Variant;

        // 2. 批量更新资源
        foreach (var (key, color) in preset.Colors)
        {
            if (app.TryGetResource(key, app.ActualThemeVariant, out var resource) &&
                resource is SolidColorBrush brush)
            {
                brush.Color = color;
            }
        }

        // 3. 触发主题变更事件
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(name));
    }
}

public record ThemePreset(
    ThemeVariant Variant,
    IReadOnlyDictionary<string, Color> Colors);
```

## 7.14 深入：样式调试技巧

### 查看控件应用的样式

在调试时，可以查看控件当前应用了哪些样式：

```csharp
// 获取控件的样式类
var classes = myControl.Classes;  // 查看当前类名

// 检查伪类状态
var pseudoClasses = myControl.GetPseudoClasses();

// 查看属性值来源
var value = myControl.GetValue(Button.BackgroundProperty);
```

### 使用 DevTools 检查样式

Avalonia 的 DevTools（按 F12 启动）可以：
- 查看控件的可视化树
- 查看每个控件应用的样式
- 查看属性值及其来源（样式、本地值、继承值）
- 实时修改属性值

```csharp
#if DEBUG
this.AttachDevTools();
#endif
```

### 常见样式问题排查

1. **样式不生效**：检查选择器是否正确匹配（类型名、命名空间、类名）
2. **样式被覆盖**：检查特异性，后定义的样式会覆盖先定义的
3. **主题切换后颜色不变**：确保使用 `DynamicResource` 或手动更新 Brush
4. **过渡动画不工作**：确保 Transitions 设置在正确的选择器上

## 7.15 跨引用

- **数据模板**：样式在 DataTemplate 中同样有效，参见 [第 8 章](08-data-templates.md)
- **自定义控件**：StyledProperty 和 PseudoClasses 是样式系统的基础，参见 [第 9 章](09-custom-controls.md)
- **动画过渡**：Transitions 是样式系统的一部分，参见 [第 10 章](10-animation-transitions.md)
- **属性系统**：StyledProperty 的详细机制，参见 [第 22 章](22-property-system.md)
- **资源系统**：StaticResource 和 DynamicResource 的解析机制，参见 [第 24 章](24-resource-system.md)

## 7.16 常见陷阱

### 陷阱 1：StaticResource 无法运行时更新

```xml
<!-- 问题：StaticResource 在编译时解析，主题切换时不会更新 -->
<Button Background="{StaticResource CsPrimaryBrush}"/>

<!-- 解决：使用 DynamicResource -->
<Button Background="{DynamicResource CsPrimaryBrush}"/>
```

但 CodexSwitch 的方案更高效：直接修改 `SolidColorBrush.Color`，这样即使是 `StaticResource` 也会自动更新，因为引用的是同一个 Brush 实例。

### 陷阱 2：样式选择器命名空间

```xml
<!-- 问题：忘记命名空间前缀 -->
<Style Selector="CsButton">  <!-- 错误：找不到 CsButton -->

<!-- 正确：使用命名空间前缀 -->
<Style Selector="ui|CsButton">  <!-- 正确 -->
```

### 陷阱 3：伪类拼写错误

```xml
<!-- 问题：伪类拼写错误 -->
<Style Selector="Button:hover">  <!-- 错误：Avalonia 没有 :hover -->

<!-- 正确：使用 :pointerover -->
<Style Selector="Button:pointerover">
```

### 陷阱 4：模板选择器嵌套

```xml
<!-- 问题：模板选择器不能嵌套 -->
<Style Selector="Button /template/ Border /template/ TextBlock">  <!-- 错误 -->

<!-- 正确：只穿透一层模板 -->
<Style Selector="Button /template/ Border">
```

### 陷阱 5：样式优先级困惑

```xml
<!-- 先定义 -->
<Style Selector="Button">
    <Setter Property="Background" Value="Red"/>
</Style>

<!-- 后定义，会覆盖上面的 -->
<Style Selector="Button">
    <Setter Property="Background" Value="Blue"/>
</Style>

<!-- 类选择器优先级更高，会覆盖类型选择器 -->
<Style Selector="Button.special">
    <Setter Property="Background" Value="Green"/>
</Style>
```

## 7.17 动手练习

### 练习 1：为 CodexSwitch 添加新主题

在 `AppThemeService.cs` 中添加一个新的 "midnight" 主题预设：

```csharp
// 1. 在 ThemeBrushes 字典中添加新的颜色对
["CsBackgroundBrush"] = new("#0A0A1A", "#FAFAFA"),  // 深蓝色背景

// 2. 在 Apply 方法中添加 "midnight" 分支
_theme switch
{
    "light" => ThemeVariant.Light,
    "dark"  => ThemeVariant.Dark,
    "midnight" => ThemeVariant.Dark,  // 使用 Dark 变体
    _       => ThemeVariant.Default
};

// 3. 在 UI 中添加主题切换选项
```

### 练习 2：创建自定义按钮样式

在 `Styles/Components/Button.axaml` 中添加一个新的按钮变体：

```xml
<!-- 添加 "glow" 变体，带有发光效果 -->
<Style Selector="ui|CsButton.glow">
    <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
</Style>
<Style Selector="ui|CsButton.glow:pointerover">
    <Setter Property="Effect">
        <DropShadowEffect Color="#5277FF" BlurRadius="15" Opacity="0.5"/>
    </Setter>
</Style>
```

### 练习 3：调试样式问题

1. 在 CodexSwitch 中找到一个使用了 `StaticResource` 的控件
2. 尝试切换主题，观察颜色是否变化
3. 将其改为 `DynamicResource`，再次测试
4. 使用 DevTools 查看属性值的来源

### 练习 4：性能测试

1. 创建一个包含 1000 个 Button 的列表
2. 分别使用简单选择器（`Button`）和复杂选择器（`Grid StackPanel Button.primary`）
3. 测量渲染时间差异
4. 使用 DevTools 的性能分析工具观察样式匹配的耗时
