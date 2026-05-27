# 24. 资源系统

## 24.1 avares:// URI

Avalonia 使用自定义 URI 方案访问嵌入资源：

```
avares://程序集名/路径/到/资源
```

```csharp
// 加载嵌入资源
using var stream = AssetLoader.Open(new Uri("avares://CodexSwitch/Assets/favicon.ico"));

// 在 AXAML 中引用
<StyleInclude Source="avares://CodexSwitch/Styles/CodexTheme.axaml"/>
<Image Source="avares://CodexSwitch/Assets/logo.png"/>
```

### 资源嵌入方式

```xml
<!-- 在 .csproj 中 -->
<ItemGroup>
    <AvaloniaResource Include="Assets\**" />
</ItemGroup>
```

这会将 `Assets` 目录下的所有文件作为 Avalonia 资源嵌入到程序集中。

## 24.2 资源字典

```xml
<Styles.Resources>
    <!-- 颜色 -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="#007ACC"/>

    <!-- 数值 -->
    <CornerRadius x:Key="RadiusMd">8</CornerRadius>
    <Thickness x:Key="PaddingMd">16</Thickness>

    <!-- 对象 -->
    <FontFamily x:Key="DefaultFont">Segoe UI</FontFamily>

    <!-- 复杂对象 -->
    <DropShadowEffect x:Key="CardShadow" BlurRadius="10" OffsetY="2"/>
</Styles.Resources>
```

### 资源查找顺序

1. 当前控件的 `Resources`
2. 父控件的 `Resources`
3. 一直到 `Application.Resources`
4. 系统主题资源

```csharp
// 在代码中查找资源
if (Application.Current.TryFindResource("PrimaryBrush", out var resource))
{
    var brush = resource as SolidColorBrush;
}
```

## 24.3 StaticResource vs DynamicResource

| 特性 | StaticResource | DynamicResource |
|------|---------------|-----------------|
| 解析时机 | 编译时 | 运行时 |
| 性能 | 更快 | 稍慢 |
| 可更新 | 否 | 是 |
| 用途 | 固定值 | 主题切换 |

```xml
<!-- StaticResource：编译时解析，不可更改 -->
<Button Background="{StaticResource CsPrimaryBrush}"/>

<!-- DynamicResource：运行时解析，支持运行时替换 -->
<Window FontFamily="{DynamicResource CodexSwitch.FontFamily}"/>
```

### 何时使用哪种

- **StaticResource**: 大多数情况，特别是颜色、尺寸等固定值
- **DynamicResource**: 需要运行时更改的值，如主题切换、字体切换

## 24.4 字体资源

```csharp
public static class AppFonts
{
    // avares:// URI 引用嵌入的字体文件
    public const string DefaultFontFamily =
        "avares://CodexSwitch/Assets/Fonts/AlibabaPuHuiTi#Alibaba PuHuiTi 3.0";
}
```

字体文件需要放在 `Assets/Fonts/` 目录下，并设置为 `AvaloniaResource`。

### 字体配置

```csharp
// Program.cs
.With(new FontManagerOptions
{
    DefaultFamilyName = AppFonts.DefaultFontFamily,
    FontFallbacks =
    [
        new FontFallback { FontFamily = new FontFamily(AppFonts.DefaultFontFamily) }
    ]
})
```

## 24.5 图标资源

```csharp
// 使用 Lucide 图标
<lucide:LucideIcon Kind="Settings" Size="17" StrokeWidth="2"/>

// 使用自定义图标
<ui:CodexImageIcon Path="{Binding IconPath}" Width="24" Height="24"/>
```

### CsImageIcon 控件

```csharp
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
                    icon.Source = new Bitmap(AssetLoader.Open(new Uri(path)));
                }
                else if (File.Exists(path))
                {
                    icon.Source = new Bitmap(path);
                }
            }
        });
    }
}
```

## 24.6 StyleInclude

```xml
<!-- 引用本地样式文件 -->
<StyleInclude Source="avares://CodexSwitch/Styles/Components/Button.axaml"/>

<!-- 引用组件库样式 -->
<StyleInclude Source="avares://CodexSwitchUI.ECharts/Themes/UsageTrendChart.axaml"/>
```

### 样式文件的组织

```
Styles/
├── CodexTheme.axaml           ← 主题入口
└── Components/
    ├── Button.axaml           ← 按钮样式
    ├── Input.axaml            ← 输入框样式
    ├── Dialog.axaml           ← 对话框样式
    ├── SegmentedControl.axaml ← 分段控件样式
    └── ...
```

---

## Deep Dive：资源系统的内部实现

### 资源查找算法

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

### 资源变更通知

当资源值变化时，所有引用该资源的控件会自动更新：

```csharp
// 修改资源
Application.Current.Resources["PrimaryBrush"] = new SolidColorBrush(Colors.Red);

// 所有使用 {DynamicResource PrimaryBrush} 的控件会自动更新
```

### 资源合并

```xml
<!-- 合并多个资源字典 -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://CodexSwitch/Styles/Colors.axaml"/>
            <ResourceInclude Source="avares://CodexSwitch/Styles/Fonts.axaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

## Cross References

- [第 2 章 项目结构与启动流程](02-project-structure.md) — AvaloniaResource 配置
- [第 7 章 样式与主题系统](07-styling-theming.md) — 资源与样式的关系
- [第 15 章 编译绑定与 AOT 发布](15-compiled-bindings.md) — 资源与 AOT 兼容性

## Common Pitfalls

1. **资源键拼写错误**: 运行时才会发现，不会编译报错
2. **StaticResource 引用不存在的资源**: 编译时会报错
3. **DynamicResource 性能问题**: 大量使用可能影响性能
4. **资源文件未设置为 AvaloniaResource**: 文件不会被嵌入

## Try It Yourself

1. 在 CodexSwitch 中找到 `CodexTheme.axaml`，研究 70+ 色彩资源的定义
2. 创建一个新的资源文件，并在 `App.axaml` 中引用
3. 尝试使用 `DynamicResource` 实现运行时字体切换
