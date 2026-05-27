# 24. 资源系统

> **写给零基础的你**：资源就是"共享的东西"。比如你定义了一个蓝色主题色，整个软件的 50 个页面都要用这个颜色。你不会在每个页面都写一遍颜色值，而是把它定义成"资源"，所有页面引用这个资源就行。改颜色的时候只改一处，50 个页面自动更新。

## 24.1 概述

Avalonia 的资源系统是管理颜色、样式、字体、图标等共享资源的核心机制。它支持静态和动态资源引用、资源继承、运行时主题切换等高级功能。理解资源系统是开发现代 Avalonia 应用的基础。

**为什么需要学习资源系统：**
- 统一管理应用的视觉资源（颜色、字体、图标）
- 支持运行时主题切换
- 实现资源复用，减少代码重复
- 支持跨程序集的资源共享

**应用场景：**
- 定义应用的颜色主题
- 管理字体和图标资源
- 实现深色/浅色主题切换
- 组织和管理样式文件

## 24.2 avares:// URI 方案

> **小白提示**：`avares://` 就像一个特殊的"文件路径"。普通文件路径是 `C:\Users\sky\photo.jpg`，而 `avares://` 是 Avalonia 用来找"打包在程序里面的资源"的路径。就像你去超市买东西，商品的"货架号"就是它的地址。

### 24.2.1 URI 语法

Avalonia 使用自定义的 `avares://` URI 方案访问嵌入资源：

```
avares://程序集名/路径/到/资源
```

**URI 组成部分：**
- `avares://`：协议前缀（就像 `https://` 是网页地址的前缀）
- `程序集名`：包含资源的程序集名称（就像超市的名字）
- `路径`：资源在程序集中的路径（就像超市里的货架号）

### 24.2.2 加载嵌入资源

```csharp
// 方式 1：使用 AssetLoader
using var stream = AssetLoader.Open(new Uri("avares://CodexSwitch/Assets/favicon.ico"));

// 方式 2：在 AXAML 中引用
<Image Source="avares://CodexSwitch/Assets/logo.png"/>

// 方式 3：引用样式文件
<StyleInclude Source="avares://CodexSwitch/Styles/CodexTheme.axaml"/>

// 方式 4：引用字体
public const string DefaultFontFamily =
    "avares://CodexSwitch/Assets/Fonts/AlibabaPuHuiTi#Alibaba PuHuiTi 3.0";
```

### 24.2.3 资源嵌入配置

```xml
<!-- 在 .csproj 中配置资源嵌入 -->
<ItemGroup>
    <!-- 嵌入整个 Assets 目录 -->
    <AvaloniaResource Include="Assets\**" />

    <!-- 嵌入特定文件 -->
    <AvaloniaResource Include="Assets\favicon.ico" />
    <AvaloniaResource Include="Assets\Fonts\*.ttf" />

    <!-- 排除特定文件 -->
    <AvaloniaResource Remove="Assets\temp\**" />
</ItemGroup>
```

### 24.2.4 资源路径规则

```csharp
// 1. 绝对路径（推荐）
"avares://CodexSwitch/Assets/logo.png"

// 2. 相对路径（在当前程序集中）
"Assets/logo.png"  // 自动转换为 avares://当前程序集/Assets/logo.png

// 3. 跨程序集引用
"avares://CodexSwitchUI/Themes/Default.axaml"

// 4. 带空格的路径（需要编码）
"avares://CodexSwitch/Assets/My%20Image.png"
```

## 24.3 资源字典详解

### 24.3.1 ResourceDictionary

```xml
<!-- 在 Styles.Resources 中定义资源 -->
<Styles.Resources>
    <!-- 颜色资源 -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="#007ACC"/>
    <SolidColorBrush x:Key="SecondaryBrush" Color="#6C757D"/>

    <!-- 数值资源 -->
    <CornerRadius x:Key="RadiusSm">4</CornerRadius>
    <CornerRadius x:Key="RadiusMd">8</CornerRadius>
    <CornerRadius x:Key="RadiusLg">12</CornerRadius>

    <Thickness x:Key="PaddingSm">4</Thickness>
    <Thickness x:Key="PaddingMd">8</Thickness>
    <Thickness x:Key="PaddingLg">16</Thickness>

    <!-- 对象资源 -->
    <FontFamily x:Key="DefaultFont">Segoe UI</FontFamily>
    <FontFamily x:Key="MonoFont">Consolas</FontFamily>

    <!-- 复杂对象 -->
    <DropShadowEffect x:Key="CardShadow" BlurRadius="10" OffsetY="2"/>
</Styles.Resources>
```

### 24.3.2 在 AXAML 中使用资源

```xml
<!-- 使用 StaticResource（编译时解析） -->
<Button Background="{StaticResource PrimaryBrush}"
        CornerRadius="{StaticResource RadiusMd}"
        Padding="{StaticResource PaddingMd}"
        Content="Click me"/>

<!-- 使用 DynamicResource（运行时解析） -->
<Window FontFamily="{DynamicResource DefaultFont}"/>

<!-- 在样式中使用资源 -->
<Style Selector="Button">
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="CornerRadius" Value="{StaticResource RadiusMd}"/>
</Style>
```

### 24.3.3 在代码中使用资源

```csharp
// 方式 1：从 Application 查找
if (Application.Current.TryFindResource("PrimaryBrush", out var resource))
{
    var brush = resource as SolidColorBrush;
    Debug.WriteLine($"Primary brush color: {brush?.Color}");
}

// 方式 2：从控件查找
if (myButton.TryFindResource("PrimaryBrush", out var resource))
{
    var brush = resource as SolidColorBrush;
}

// 方式 3：直接访问 Resources 字典
var brush = Application.Current.Resources["PrimaryBrush"] as SolidColorBrush;

// 方式 4：使用索引器
var brush = myButton.Resources["PrimaryBrush"] as SolidColorBrush;
```

## 24.4 资源查找顺序

### 24.4.1 查找层次

```
资源查找顺序（从内到外）：
1. 当前控件的 Resources
2. 父控件的 Resources
3. 一直到 Application.Resources
4. 系统主题资源
```

### 24.4.2 查找算法

```csharp
// 资源查找的简化算法
object? FindResource(string key)
{
    var current = this as IResourceNode;

    while (current != null)
    {
        if (current.Resources.TryGetValue(key, out var resource))
            return resource;

        current = current.GetParent() as IResourceNode;
    }

    // 最后查找系统主题资源
    return SystemResources.GetValueOrDefault(key);
}
```

### 24.4.3 查找示例

```csharp
// 示例：资源查找链
// AXAML 结构：
// <Window.Resources>
//     <SolidColorBrush x:Key="WindowBrush" Color="Red"/>
// </Window.Resources>
// <StackPanel>
//     <StackPanel.Resources>
//         <SolidColorBrush x:Key="PanelBrush" Color="Blue"/>
//     </StackPanel.Resources>
//     <Button Content="Click me"/>
// </StackPanel>

// Button 查找 "PanelBrush"：
// 1. Button.Resources → 未找到
// 2. StackPanel.Resources → 找到！返回 Blue

// Button 查找 "WindowBrush"：
// 1. Button.Resources → 未找到
// 2. StackPanel.Resources → 未找到
// 3. Window.Resources → 找到！返回 Red
```

### 24.4.4 资源覆盖

```xml
<!-- 子控件可以覆盖父控件的资源 -->
<Window.Resources>
    <SolidColorBrush x:Key="PrimaryBrush" Color="Blue"/>
</Window.Resources>

<StackPanel>
    <StackPanel.Resources>
        <!-- 覆盖 Window 的 PrimaryBrush -->
        <SolidColorBrush x:Key="PrimaryBrush" Color="Red"/>
    </StackPanel.Resources>

    <Button Background="{StaticResource PrimaryBrush}"/>
    <!-- Button 使用 Red，因为 StackPanel 的资源覆盖了 Window 的资源 -->
</StackPanel>
```

## 24.5 StaticResource vs DynamicResource

### 24.5.1 解析时机

| 特性 | StaticResource | DynamicResource |
|------|---------------|-----------------|
| 解析时机 | 编译时 | 运行时 |
| 性能 | 更快 | 稍慢 |
| 可更新 | 否 | 是 |
| 内存占用 | 更少 | 更多 |
| 错误检测 | 编译时报错 | 运行时才报错 |

### 24.5.2 使用场景

```xml
<!-- StaticResource：适用于固定值 -->
<!-- 颜色、尺寸等不会改变的资源 -->
<Button Background="{StaticResource PrimaryBrush}"
        CornerRadius="{StaticResource RadiusMd}"
        Padding="{StaticResource PaddingMd}"/>

<!-- DynamicResource：适用于运行时更改的值 -->
<!-- 主题切换、字体切换等场景 -->
<Window FontFamily="{DynamicResource DefaultFont}"/>
<TextBlock Foreground="{DynamicResource TextBrush}"/>
```

### 24.5.3 性能对比

```csharp
// StaticResource 性能优势：
// 1. 编译时解析，无需运行时查找
// 2. 直接引用资源对象，无间接访问
// 3. 不需要监听资源变更

// DynamicResource 性能开销：
// 1. 运行时查找资源
// 2. 需要监听资源变更
// 3. 变更时需要重新应用

// 最佳实践：
// - 大多数情况使用 StaticResource
// - 只有需要运行时更改的资源使用 DynamicResource
```

### 24.5.4 混合使用

```xml
<!-- 在同一控件中混合使用 -->
<Window FontFamily="{DynamicResource DefaultFont}">  <!-- 字体可能切换 -->
    <StackPanel>
        <Button Background="{StaticResource PrimaryBrush}"  <!-- 颜色固定 -->
                Content="Click me"/>
        <TextBlock Foreground="{DynamicResource TextBrush}"  <!-- 颜色可能切换 -->
                   Text="Hello"/>
    </StackPanel>
</Window>
```

## 24.6 MergedDictionaries 和 ResourceInclude

### 24.6.1 合并多个资源字典

```xml
<!-- 在 Application.Resources 中合并多个资源文件 -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://CodexSwitch/Styles/Colors.axaml"/>
            <ResourceInclude Source="avares://CodexSwitch/Styles/Fonts.axaml"/>
            <ResourceInclude Source="avares://CodexSwitch/Styles/Sizes.axaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### 24.6.2 条件资源加载

```xml
<!-- 根据主题加载不同的资源字典 -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- 基础资源 -->
            <ResourceInclude Source="avares://App/Styles/Base.axaml"/>

            <!-- 主题资源（运行时切换） -->
            <ResourceInclude Source="avares://App/Styles/Light.axaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### 24.6.3 跨程序集资源

```xml
<!-- 引用其他程序集的资源 -->
<ResourceDictionary.MergedDictionaries>
    <!-- 引用 CodexSwitchUI 的资源 -->
    <ResourceInclude Source="avares://CodexSwitchUI/Themes/Default.axaml"/>

    <!-- 引用 CodexSwitchUI.ECharts 的资源 -->
    <ResourceInclude Source="avares://CodexSwitchUI.ECharts/Themes/UsageTrendChart.axaml"/>
</ResourceDictionary.MergedDictionaries>
```

## 24.7 字体资源管理

### 24.7.1 嵌入字体

```csharp
// 方式 1：使用 avares:// URI
public static class AppFonts
{
    public const string DefaultFontFamily =
        "avares://CodexSwitch/Assets/Fonts/AlibabaPuHuiTi#Alibaba PuHuiTi 3.0";
}

// 方式 2：使用 FontFamily 对象
var fontFamily = new FontFamily("avares://CodexSwitch/Assets/Fonts/AlibabaPuHuiTi#Alibaba PuHuiTi 3.0");
```

### 24.7.2 FontManagerOptions 配置

```csharp
// 在 Program.cs 中配置字体
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new FontManagerOptions
        {
            // 默认字体
            DefaultFamilyName = AppFonts.DefaultFontFamily,

            // 字体回退
            FontFallbacks =
            [
                new FontFallback { FontFamily = new FontFamily(AppFonts.DefaultFontFamily) },
                new FontFallback { FontFamily = new FontFamily("Arial") },
                new FontFallback { FontFamily = new FontFamily("Microsoft YaHei") }
            ]
        })
        ;
```

### 24.7.3 字体回退机制

```csharp
// 字体回退的实现：
// 1. 首先尝试使用指定的字体
// 2. 如果字符不在字体中，尝试使用回退字体
// 3. 如果所有回退字体都不支持，使用系统默认字体

// 示例：中文字符回退
// 指定字体：Alibaba PuHuiTi（支持中文）
// 回退字体：Arial（不支持中文）
// 实际使用：Alibaba PuHuiTi（因为支持中文）

// 示例：特殊字符回退
// 指定字体：Alibaba PuHuiTi（不支持特殊符号）
// 回退字体：Segoe UI Symbol（支持特殊符号）
// 实际使用：Segoe UI Symbol
```

### 24.7.4 字体样式

```xml
<!-- 在 AXAML 中使用字体 -->
<TextBlock Text="Hello"
           FontFamily="{StaticResource DefaultFont}"
           FontSize="16"
           FontWeight="Normal"
           FontStyle="Italic"/>

<!-- 使用字体资源 -->
<TextBlock Text="Hello"
           FontFamily="{DynamicResource DefaultFont}"/>
```

## 24.8 图标资源管理

### 24.8.1 使用 Lucide 图标

```xml
<!-- 使用 Lucide 图标库 -->
<lucide:LucideIcon Kind="Settings" Size="17" StrokeWidth="2"/>
<lucide:LucideIcon Kind="User" Size="24" StrokeWidth="1.5"/>
<lucide:LucideIcon Kind="Home" Size="32" StrokeWidth="2"/>
```

### 24.8.2 自定义图标控件

```csharp
// CsImageIcon 控件实现
public class CsImageIcon : Image
{
    public static readonly StyledProperty<string?> PathProperty =
        AvaloniaProperty.Register<CsImageIcon, string?>(nameof(Path));

    static CsImageIcon()
    {
        PathProperty.Changed.AddClassHandler<CsImageIcon>((icon, args) =>
        {
            if (args.NewValue is string path)
            {
                if (path.StartsWith("avares://"))
                {
                    // 从嵌入资源加载
                    icon.Source = new Bitmap(AssetLoader.Open(new Uri(path)));
                }
                else if (File.Exists(path))
                {
                    // 从文件系统加载
                    icon.Source = new Bitmap(path);
                }
            }
        });
    }

    public string? Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }
}
```

```xml
<!-- 使用自定义图标控件 -->
<ui:CsImageIcon Path="avares://CodexSwitch/Assets/Icons/settings.svg"
                Width="24" Height="24"/>

<ui:CsImageIcon Path="/path/to/local/icon.png"
                Width="32" Height="32"/>
```

### 24.8.3 SVG 图标

```xml
<!-- 使用 SVG 图标 -->
<Image Source="avares://CodexSwitch/Assets/Icons/icon.svg"
       Width="24" Height="24"/>

<!-- SVG 作为资源 -->
<Styles.Resources>
    <Bitmap x:Key="SettingsIcon" x:FactoryMethod="AssetLoader.Open">
        <x:Arguments>
            <Uri>avares://CodexSwitch/Assets/Icons/settings.svg</Uri>
        </x:Arguments>
    </Bitmap>
</Styles.Resources>

<Image Source="{StaticResource SettingsIcon}" Width="24" Height="24"/>
```

## 24.9 StyleInclude 和样式文件组织

### 24.9.1 StyleInclude

```xml
<!-- 引用本地样式文件 -->
<StyleInclude Source="avares://CodexSwitch/Styles/Components/Button.axaml"/>
<StyleInclude Source="avares://CodexSwitch/Styles/Components/Input.axaml"/>

<!-- 引用组件库样式 -->
<StyleInclude Source="avares://CodexSwitchUI.ECharts/Themes/UsageTrendChart.axaml"/>
```

### 24.9.2 样式文件组织

```
Styles/
├── CodexTheme.axaml           ← 主题入口，包含所有资源和样式
├── Colors.axaml               ← 颜色定义
├── Fonts.axaml                ← 字体定义
├── Sizes.axaml                ← 尺寸定义
└── Components/
    ├── Button.axaml           ← 按钮样式
    ├── Input.axaml            ← 输入框样式
    ├── Dialog.axaml           ← 对话框样式
    ├── SegmentedControl.axaml ← 分段控件样式
    ├── Card.axaml             ← 卡片样式
    ├── Badge.axaml            ← 徽章样式
    ├── Table.axaml            ← 表格样式
    └── ...
```

### 24.9.3 CodexTheme.axaml 结构

```xml
<!-- CodexSwitch 的主题文件结构 -->
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ui="using:CodexSwitchUI.Controls">

    <!-- 1. 资源定义 -->
    <Styles.Resources>
        <!-- 圆角 -->
        <CornerRadius x:Key="CsRadiusSm">6</CornerRadius>
        <CornerRadius x:Key="CsRadiusMd">8</CornerRadius>
        <CornerRadius x:Key="CsRadiusLg">10</CornerRadius>

        <!-- 颜色（70+ 种颜色） -->
        <SolidColorBrush x:Key="CsBackgroundBrush" Color="#171717"/>
        <SolidColorBrush x:Key="CsForegroundBrush" Color="#FAFAFA"/>
        <SolidColorBrush x:Key="CsPrimaryBrush" Color="#E5E5E5"/>
        <!-- ... 更多颜色 ... -->
    </Styles.Resources>

    <!-- 2. 全局样式 -->
    <Style Selector="Window">
        <Setter Property="FontFamily" Value="{DynamicResource CodexSwitch.FontFamily}"/>
        <Setter Property="Background" Value="{StaticResource CsBackgroundBrush}"/>
    </Style>

    <!-- 3. 组件样式引用 -->
    <StyleInclude Source="avares://CodexSwitch/Styles/Components/Text.axaml"/>
    <StyleInclude Source="avares://CodexSwitch/Styles/Components/Button.axaml"/>
    <StyleInclude Source="avares://CodexSwitch/Styles/Components/IconButton.axaml"/>
    <!-- ... 更多样式 ... -->
</Styles>
```

## 24.10 运行时资源修改和主题切换

### 24.10.1 修改运行时资源

```csharp
// 修改 Application 级别的资源
Application.Current.Resources["PrimaryBrush"] = new SolidColorBrush(Colors.Red);

// 修改控件级别的资源
myButton.Resources["ButtonBrush"] = new SolidColorBrush(Colors.Blue);

// 修改后，所有使用 DynamicResource 的控件会自动更新
```

### 24.10.2 主题切换实现

```csharp
// AppThemeService.cs 中的主题切换实现
public static void Apply(string? theme)
{
    var app = Application.Current;
    if (app is null)
        return;

    _theme = Normalize(theme);

    // 设置主题变体
    app.RequestedThemeVariant = _theme switch
    {
        "light" => ThemeVariant.Light,
        "dark" => ThemeVariant.Dark,
        _ => ThemeVariant.Default
    };

    // 监听系统主题变化
    EnsureSystemThemeListener(app);

    // 应用组件库主题
    ApplyComponentLibraryTheme(app);

    // 应用颜色资源
    ApplyBrushes(app);
}

// 应用颜色资源
private static void ApplyBrushes(Application app)
{
    var light = _theme == "light" ||
        (_theme == "system" && app.ActualThemeVariant == ThemeVariant.Light);

    foreach (var pair in ThemeBrushes)
        ApplyBrush(app, pair.Key, light ? pair.Value.Light : pair.Value.Dark);
}

// 修改单个画刷
private static void ApplyBrush(Application app, string key, string colorText)
{
    var color = Color.Parse(colorText);
    if (app.TryGetResource(key, app.ActualThemeVariant, out var resource) &&
        resource is SolidColorBrush brush)
    {
        // 直接修改现有画刷的颜色
        brush.Color = color;
        return;
    }

    // 创建新的画刷
    app.Resources[key] = new SolidColorBrush(color);
}
```

### 24.10.3 主题切换最佳实践

```csharp
// 1. 使用 DynamicResource 引用需要切换的资源
<Window FontFamily="{DynamicResource CodexSwitch.FontFamily}"/>
<TextBlock Foreground="{DynamicResource CsForegroundBrush}"/>

// 2. 使用 StaticResource 引用固定资源
<Button Background="{StaticResource CsPrimaryBrush}"/>

// 3. 监听主题变化
Application.Current.ActualThemeVariantChanged += (sender, args) =>
{
    Debug.WriteLine($"Theme changed to: {Application.Current.ActualThemeVariant}");
};

// 4. 使用 ThemeVariant 感知的资源
<Styles.Resources>
    <!-- 深色主题资源 -->
    <SolidColorBrush x:Key="CsBackgroundBrush" Color="#171717"
                     x:DataType="ThemeVariant"
                     x:Arguments="Dark"/>
    <!-- 浅色主题资源 -->
    <SolidColorBrush x:Key="CsBackgroundBrush" Color="#FAFAFA"
                     x:DataType="ThemeVariant"
                     x:Arguments="Light"/>
</Styles.Resources>
```

## 24.11 CodexSwitch 实战：资源系统应用

### 24.11.1 I18nService 的资源加载

```csharp
// I18nService.cs 中的资源加载
public sealed class I18nService : INotifyPropertyChanged
{
    public const string CatalogResourceUri = "avares://CodexSwitch/Assets/i18n/languages.json";

    public static I18nService LoadDefault()
    {
        var catalog = LoadCatalog();
        var resources = catalog.Languages.Select(language => LoadLanguageResource(language.Code)).ToArray();
        return new I18nService(catalog, resources);
    }

    public static I18nCatalog LoadCatalog()
    {
        // 使用 avares:// URI 加载嵌入资源
        using var stream = AssetLoader.Open(new Uri(CatalogResourceUri));
        return DeserializeCatalog(stream);
    }

    public static I18nLanguageResource LoadLanguageResource(string code)
    {
        // 动态构建资源 URI
        var resourceUri = $"avares://CodexSwitch/Assets/i18n/{code}.json";
        using var stream = AssetLoader.Open(new Uri(resourceUri));
        return DeserializeLanguageResource(stream, code);
    }
}
```

### 24.11.2 主题资源管理

```csharp
// AppThemeService.cs 中的主题资源管理
private static readonly IReadOnlyDictionary<string, ThemeColorPair> ThemeBrushes =
    new Dictionary<string, ThemeColorPair>(StringComparer.Ordinal)
    {
        // 定义 70+ 种颜色的深色/浅色值
        ["CsBackgroundBrush"] = new("#171717", "#FAFAFA"),
        ["CsForegroundBrush"] = new("#FAFAFA", "#09090B"),
        ["CsCardBrush"] = new("#262626", "#FFFFFF"),
        ["CsPrimaryBrush"] = new("#E5E5E5", "#18181B"),
        // ... 更多颜色
    };

// 主题切换时更新所有颜色
private static void ApplyBrushes(Application app)
{
    var light = _theme == "light" ||
        (_theme == "system" && app.ActualThemeVariant == ThemeVariant.Light);

    foreach (var pair in ThemeBrushes)
        ApplyBrush(app, pair.Key, light ? pair.Value.Light : pair.Value.Dark);
}
```

### 24.11.3 字体资源管理

```csharp
// 字体资源定义
public static class AppFonts
{
    // 使用 avares:// URI 引用嵌入的字体文件
    public const string DefaultFontFamily =
        "avares://CodexSwitch/Assets/Fonts/AlibabaPuHuiTi#Alibaba PuHuiTi 3.0";
}

// 在 Program.cs 中配置
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new FontManagerOptions
        {
            DefaultFamilyName = AppFonts.DefaultFontFamily,
            FontFallbacks =
            [
                new FontFallback { FontFamily = new FontFamily(AppFonts.DefaultFontFamily) }
            ]
        })
        ;
```

## 24.12 最佳实践

### 24.12.1 资源组织最佳实践

```xml
<!-- 1. 使用语义化命名 -->
<SolidColorBrush x:Key="CsPrimaryBrush" Color="#007ACC"/>  <!-- 好 -->
<SolidColorBrush x:Key="BlueBrush" Color="#007ACC"/>  <!-- 不好 -->

<!-- 2. 使用层次化组织 -->
<Styles.Resources>
    <!-- 颜色 -->
    <SolidColorBrush x:Key="CsPrimaryBrush" Color="#007ACC"/>

    <!-- 尺寸 -->
    <CornerRadius x:Key="CsRadiusMd">8</CornerRadius>
    <Thickness x:Key="CsPaddingMd">16</Thickness>

    <!-- 字体 -->
    <FontFamily x:Key="CsDefaultFont">Segoe UI</FontFamily>
</Styles.Resources>

<!-- 3. 分离关注点 -->
<!-- Colors.axaml: 只包含颜色 -->
<!-- Fonts.axaml: 只包含字体 -->
<!-- Sizes.axaml: 只包含尺寸 -->
```

### 24.12.2 性能优化最佳实践

```xml
<!-- 1. 优先使用 StaticResource -->
<Button Background="{StaticResource PrimaryBrush}"/>  <!-- 好 -->
<Button Background="{DynamicResource PrimaryBrush}"/>  <!-- 仅在需要时使用 -->

<!-- 2. 避免在循环中创建资源 -->
<!-- 错误示例 -->
<ItemsControl.ItemTemplate>
    <DataTemplate>
        <Border Background="{DynamicResource ItemBrush}"/>  <!-- 每个项都动态查找 -->
    </DataTemplate>
</ItemsControl.ItemTemplate>

<!-- 正确示例 -->
<ItemsControl.ItemTemplate>
    <DataTemplate>
        <Border Background="{StaticResource ItemBrush}"/>  <!-- 编译时解析 -->
    </DataTemplate>
</ItemsControl.ItemTemplate>

<!-- 3. 使用 MergedDictionaries 分离大型资源 -->
<ResourceDictionary.MergedDictionaries>
    <ResourceInclude Source="avares://App/Styles/LargeResource.axaml"/>
</ResourceDictionary.MergedDictionaries>
```

### 24.12.3 主题切换最佳实践

```csharp
// 1. 使用 ThemeVariant 感知的资源
app.RequestedThemeVariant = ThemeVariant.Dark;

// 2. 监听主题变化
Application.Current.ActualThemeVariantChanged += (sender, args) =>
{
    // 更新非主题感知的资源
    UpdateCustomResources();
};

// 3. 使用 TryGetResource 检查资源存在性
if (app.TryGetResource(key, app.ActualThemeVariant, out var resource))
{
    // 资源存在
}

// 4. 批量更新资源以提高性能
ApplyBrushes(app);  // 一次性更新所有颜色
```

---

## Deep Dive：资源系统的内部实现

### 资源查找算法

```csharp
// 资源查找的完整算法
object? FindResource(string key, ThemeVariant? themeVariant = null)
{
    // 1. 查找当前控件的资源
    if (Resources.TryGetValue(key, out var resource))
        return resource;

    // 2. 查找父控件的资源
    var parent = GetVisualParent();
    if (parent != null)
    {
        var result = parent.FindResource(key, themeVariant);
        if (result != null)
            return result;
    }

    // 3. 查找 Application 的资源
    if (Application.Current != null)
    {
        var result = Application.Current.FindResource(key, themeVariant);
        if (result != null)
            return result;
    }

    // 4. 查找系统主题资源
    return SystemResources.GetValueOrDefault(key);
}
```

### 资源变更通知机制

```csharp
// 当资源值变化时，所有引用该资源的控件会自动更新
// 内部实现：

// 1. DynamicResource 注册监听
void RegisterDynamicResource(AvaloniaObject target, AvaloniaProperty property, string key)
{
    // 注册到资源变更通知列表
    _dynamicResourceListeners.Add(new DynamicResourceListener(target, property, key));
}

// 2. 资源变更时通知
void NotifyResourceChanged(string key)
{
    foreach (var listener in _dynamicResourceListeners)
    {
        if (listener.Key == key)
        {
            // 更新目标属性
            listener.Target.SetValue(listener.Property, FindResource(key));
        }
    }
}
```

### 资源合并机制

```csharp
// MergedDictionaries 的合并机制
public class ResourceDictionary
{
    private List<ResourceDictionary> _mergedDictionaries = new();
    private Dictionary<string, object> _resources = new();

    public object? GetValue(string key)
    {
        // 1. 在当前字典中查找
        if (_resources.TryGetValue(key, out var value))
            return value;

        // 2. 在合并的字典中查找（按顺序）
        foreach (var dict in _mergedDictionaries)
        {
            var result = dict.GetValue(key);
            if (result != null)
                return result;
        }

        return null;
    }
}
```

## Cross References

- [第 2 章 项目结构与启动流程](02-project-structure.md) — AvaloniaResource 配置
- [第 7 章 样式与主题系统](07-styling-theming.md) — 资源与样式的关系
- [第 15 章 编译绑定与 AOT 发布](15-compiled-bindings.md) — 资源与 AOT 兼容性
- [第 21 章 调试与诊断](21-debugging.md) — 资源查找调试
- [第 22 章 Avalonia 属性系统](22-property-system.md) — 属性与资源的关系

## Common Pitfalls

1. **资源键拼写错误**: 运行时才会发现，不会编译报错（StaticResource 会报错）
2. **StaticResource 引用不存在的资源**: 编译时会报错，需要确保资源已定义
3. **DynamicResource 性能问题**: 大量使用可能影响性能，应限制使用范围
4. **资源文件未设置为 AvaloniaResource**: 文件不会被嵌入到程序集中
5. **资源查找顺序错误**: 子控件的资源会覆盖父控件的同名资源
6. **主题切换未更新所有资源**: 需要同时更新 DynamicResource 和手动修改的资源
7. **跨程序集资源引用错误**: 需要使用正确的程序集名称
8. **字体路径错误**: 字体 URI 格式不正确会导致字体加载失败
9. **资源字典合并顺序错误**: 后合并的资源会覆盖先合并的同名资源
10. **未释放资源引用**: 大量资源对象可能导致内存问题

## Try It Yourself

1. **基础练习**: 在 CodexSwitch 中找到 `CodexTheme.axaml`，研究 70+ 种颜色资源的定义和命名规范

2. **资源创建**: 创建一个新的资源文件 `CustomColors.axaml`，定义 5 种颜色，并在 `App.axaml` 中引用

3. **DynamicResource 练习**: 使用 DynamicResource 实现运行时字体切换，切换字体后所有文本自动更新

4. **主题切换**: 实现一个简单的深色/浅色主题切换功能，使用 AppThemeService 的模式

5. **跨程序集资源**: 创建一个类库项目，定义资源，在主项目中引用

6. **字体嵌入**: 嵌入一个自定义字体，配置 FontManagerOptions，测试字体回退

7. **图标管理**: 创建一个自定义图标控件，支持从 avares:// 和文件系统加载图标

8. **综合项目**: 实现一个完整的主题系统，支持 3 种主题（深色、浅色、自定义），使用资源字典管理所有视觉资源
