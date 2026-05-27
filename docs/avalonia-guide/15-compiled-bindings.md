# 15. 编译绑定与 AOT 发布

> **写给零基础的你**：编译绑定就像"提前检查作业"。普通的绑定是运行时才检查你写的属性名对不对（考试时才发现写错了），编译绑定是编译时就检查（交作业前老师就帮你检查好了）。这样程序跑起来更快，也不容易出错。

> **AOT 是什么？** AOT = Ahead Of Time（提前编译）。普通程序运行时需要"翻译"，AOT 是提前翻译好，运行时直接执行，所以启动更快、体积更小。

## 15.1 概述

编译绑定和 Native AOT 发布是 Avalonia 应用性能优化和发布的关键技术。学完本章，你将能够：

- 理解编译绑定（Compiled Bindings）的工作原理和优势
- 正确使用 `x:DataType` 实现编译时类型检查
- 配置 Native AOT 发布，生成高性能的原生可执行文件
- 理解 AOT 兼容性问题（反射、序列化、动态加载）
- 使用源生成器确保 AOT 兼容
- 处理裁剪警告
- 理解编译绑定的内部实现原理
- 对比编译绑定与运行时绑定的性能差异

CodexSwitch 从设计之初就以 AOT 发布为目标，所有绑定都使用编译绑定，所有 JSON 序列化都使用源生成器，确保在 Native AOT 下完美运行。

## 15.2 核心概念

### 15.2.1 编译绑定 vs 运行时绑定

> **小白提示**：两种绑定方式的区别就像"现场翻译"和"提前翻译好"：
> - **运行时绑定** = 现场翻译（程序运行时才去找属性，慢，容易出错）
> - **编译绑定** = 提前翻译好（编译时就检查好了，快，不容易出错）
>
> 推荐使用编译绑定，因为它更快、更安全。

```
传统绑定（运行时）：
AXAML "{Binding Name}"
    -> 运行时反射查找 "Name" 属性（现场翻译，慢）
    -> 性能开销，无类型检查
    -> 绑定错误只在运行时发现（翻译错了才发现）

编译绑定（编译时）：
AXAML "{Binding Name}" + x:DataType="vm:MyViewModel"
    -> 编译器生成强类型绑定代码（提前翻译好，快）
    -> 直接属性访问，零反射
    -> 编译时检查属性是否存在（翻译前就检查好了）
    -> 绑定错误在编译时报错（翻译前就知道有没有错）
```

### 15.2.2 启用编译绑定

```xml
<!-- .csproj -->
<PropertyGroup>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
</PropertyGroup>
```

设置为 `true` 后，所有 AXAML 文件默认使用编译绑定。也可以在单个文件中覆盖。

### 15.2.3 x:DataType 的作用

> **小白提示：什么是 x:DataType？**  `x:DataType` 就是告诉编译器"这个页面的数据长什么样"。就像你填快递单时要写"里面装的是什么"——写了之后，快递公司就能检查你有没有写错地址（属性名）。如果不写，快递公司就只能盲送（运行时才发现错误）。

`x:DataType` 指定绑定表达式中 `{Binding Path}` 的目标类型：

```xml
<!-- 窗口级别指定 DataType -->
<Window x:DataType="vm:MainWindowViewModel">
    <!-- 编译器知道 DataContext 是 MainWindowViewModel -->
    <TextBlock Text="{Binding UserName}"/>  <!-- 编译器验证 UserName 存在 -->
    <TextBlock Text="{Binding Nme}"/>       <!-- 编译错误：Nme 不存在 -->
</Window>
```

**每个使用编译绑定的 AXAML 元素必须有可解析的 `x:DataType`**。如果没有，绑定会回退到运行时模式，并产生警告。

### 15.2.4 编译时错误示例

```xml
<!-- 假设 MyViewModel 没有 Nme 属性（拼写错误） -->
<TextBlock Text="{Binding Nme}"/>
<!-- 编译错误：Cannot find property 'Nme' on type 'MyViewModel' -->

<!-- 假设 MyViewModel 的 Age 是 int，但绑定了 string 属性 -->
<TextBlock Text="{Binding Age.Length}"/>
<!-- 编译错误：Cannot find property 'Length' on type 'int' -->
```

### 15.2.5 Native AOT 发布配置

CodexSwitch 的 AOT 配置：

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
    <TrimMode>full</TrimMode>
    <InvariantGlobalization>false</InvariantGlobalization>
</PropertyGroup>
```

| 配置项 | 说明 | 建议值 |
|--------|------|--------|
| `PublishAot` | 启用 Native AOT | `true` |
| `TrimMode` | 裁剪模式 | `full`（完整裁剪） |
| `InvariantGlobalization` | 不变全球化 | `false`（保留区域信息） |

### 15.2.6 AOT 兼容性要求

| 要求 | 说明 | CodexSwitch 的做法 |
|------|------|-------------------|
| 编译绑定 | 必须启用，运行时绑定依赖反射 | `AvaloniaUseCompiledBindingsByDefault=true` |
| 源生成 JSON | 使用 `JsonSerializerContext` | `CodexSwitchJsonContext` |
| 无动态加载 | 不能使用 `Assembly.LoadFrom()` | 不使用 |
| 无反射创建 | 不能使用 `Activator.CreateInstance()` | 不使用 |
| TrimMode=full | 完整裁剪 | 已配置 |
| 无动态 LINQ | 不能使用 `Expression.Compile()` | 不使用 |

### 15.2.7 发布命令

```bash
# Windows x64
dotnet publish CodexSwitch/CodexSwitch.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=true

# macOS x64
dotnet publish CodexSwitch/CodexSwitch.csproj -c Release -r osx-x64 --self-contained true -p:PublishAot=true

# macOS ARM64 (Apple Silicon)
dotnet publish CodexSwitch/CodexSwitch.csproj -c Release -r osx-arm64 --self-contained true -p:PublishAot=true

# Linux x64
dotnet publish CodexSwitch/CodexSwitch.csproj -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```

## 15.3 进阶用法

### 15.3.1 DataType 的作用域规则

`x:DataType` 遵循视觉树的作用域规则：

```xml
<Window x:DataType="vm:MainWindowViewModel">
    <!-- 使用窗口级 DataType -->
    <TextBlock Text="{Binding Title}"/>

    <!-- DataTemplate 覆盖 DataType -->
    <ItemsControl ItemsSource="{Binding Users}">
        <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="models:UserModel">
                <!-- 此处 DataType 是 UserModel -->
                <TextBlock Text="{Binding Name}"/>
                <TextBlock Text="{Binding Email}"/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>

    <!-- ContentControl 使用 DataType 转换 -->
    <ContentControl Content="{Binding SelectedItem}">
        <ContentControl.DataTemplates>
            <DataTemplate x:DataType="models:SettingsModel">
                <TextBlock Text="{Binding SettingsTitle}"/>
            </DataTemplate>
        </ContentControl.DataTemplates>
    </ContentControl>
</Window>
```

### 15.3.2 不同绑定场景的 DataType 设置

**UserControl**

```xml
<UserControl x:DataType="vm:SettingsPageViewModel"
             xmlns:vm="using:MyApp.ViewModels">
    <StackPanel>
        <TextBlock Text="{Binding SettingName}"/>
        <ToggleSwitch IsChecked="{Binding IsEnabled}"/>
    </StackPanel>
</UserControl>
```

**DataTemplate**

```xml
<DataTemplate x:DataType="models:ProviderModel">
    <Border Classes="provider-list-row">
        <StackPanel>
            <TextBlock Text="{Binding Name}"/>
            <TextBlock Text="{Binding Status}"/>
        </StackPanel>
    </Border>
</DataTemplate>
```

**ItemsControl.ItemTemplate**

```xml
<ItemsControl ItemsSource="{Binding Items}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="models:ItemModel">
            <TextBlock Text="{Binding Title}"/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**Style 中的 ControlTemplate**

```xml
<Style Selector="ui|CsCard">
    <Setter Property="Template">
        <ControlTemplate>
            <!-- ControlTemplate 中使用 TemplatedParent 绑定 -->
            <Border Background="{TemplateBinding Background}">
                <TextBlock Text="{Binding Title, RelativeSource={RelativeSource TemplatedParent}}"/>
            </Border>
        </ControlTemplate>
    </Setter>
</Style>
```

### 15.3.3 处理多态绑定

当 `DataContext` 可能是多种类型时，使用接口：

```csharp
// 定义接口
public interface INamedItem
{
    string Name { get; }
}

// ViewModel 实现接口
public class ProviderViewModel : INamedItem
{
    public string Name { get; set; }
}
```

```xml
<DataTemplate x:DataType="models:INamedItem">
    <TextBlock Text="{Binding Name}"/>  <!-- 编译器验证接口属性 -->
</DataTemplate>
```

### 15.3.4 绑定到静态属性

```xml
<!-- 绑定到静态属性需要 x:Static -->
<TextBlock Text="{x:Static local:AppInfo.Version}"/>

<!-- 或使用编译绑定的静态属性路径 -->
<TextBlock Text="{Binding (local:AppInfo.Version)}"/>
```

### 15.3.5 绑定到附加属性

```xml
<!-- 附加属性需要括号语法 -->
<Grid>
    <TextBlock Text="{Binding (Grid.Row), RelativeSource={RelativeSource Self}}"/>
</Grid>
```

### 15.3.6 源生成 JSON 序列化

这是 AOT 兼容的关键。CodexSwitch 的实现：

```csharp
[JsonSerializable(typeof(I18nCatalog))]
[JsonSerializable(typeof(I18nLanguageResource))]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(ProviderConfig))]
[JsonSerializable(typeof(UsageLogEntry))]
internal partial class CodexSwitchJsonContext : JsonSerializerContext { }
```

使用：

```csharp
// AOT 兼容
return JsonSerializer.Deserialize(stream, CodexSwitchJsonContext.Default.I18nCatalog);

// AOT 不兼容 -- 运行时反射
return JsonSerializer.Deserialize<I18nCatalog>(stream);  // 不要使用
```

### 15.3.7 AvaloniaUI.DiagnosticsSupport

CodexSwitch 在 Debug 模式下启用诊断支持：

```xml
<PackageReference Include="AvaloniaUI.DiagnosticsSupport">
    <IncludeAssets Condition="'$(Configuration)' != 'Debug'">None</IncludeAssets>
    <PrivateAssets Condition="'$(Configuration)' != 'Debug'">All</PrivateAssets>
</PackageReference>
```

这只在 Debug 构建中包含，Release 构建完全排除。

诊断工具提供：
- 实时视觉树检查
- 属性编辑器
- 布局调试
- 绑定错误日志
- 性能分析

## 15.4 组件详解大全

### 15.4.1 编译绑定的内部实现

当你启用 `AvaloniaUseCompiledBindingsByDefault` 时，Avalonia 的 XAML 编译器会为每个 `{Binding}` 表达式生成强类型的 C# 代码：

```xml
<TextBlock Text="{Binding UserName}" x:DataType="vm:MyViewModel"/>
```

会被编译为类似这样的代码：

```csharp
// 生成的绑定代码（简化版）
var binding = new CompiledBindingPathBuilder()
    .Property(ObservableObjectProperty, vm => vm.UserName)
    .Build();

textBlock[!TextBlock.TextProperty] = binding;
```

这比运行时绑定快得多，因为：
1. 属性访问是直接的，不需要反射
2. 路径解析在编译时完成
3. 类型检查在编译时完成

### 15.4.2 运行时绑定的内部实现

对比运行时绑定：

```csharp
// 运行时绑定（简化版）
var propertyPath = "UserName";  // 字符串路径
var parts = propertyPath.Split('.');

// 通过反射查找属性
var propertyInfo = viewModel.GetType().GetProperty(parts[0]);
var value = propertyInfo?.GetValue(viewModel);

// 订阅 INotifyPropertyChanged
viewModel.PropertyChanged += (s, e) =>
{
    if (e.PropertyName == parts[0])
    {
        var newValue = propertyInfo?.GetValue(viewModel);
        textBlock.Text = newValue?.ToString();
    }
};
```

运行时绑定需要反射、字符串解析和运行时类型检查。

### 15.4.3 性能对比

| 操作 | 运行时绑定 | 编译绑定 |
|------|-----------|---------|
| 首次绑定 | 慢（反射+解析） | 快（直接调用） |
| 属性访问 | 慢（反射） | 快（直接字段访问） |
| 内存占用 | 较高（反射缓存） | 较低 |
| 编译时间 | 不影响 | 略慢（生成代码） |
| 启动时间 | 较慢 | 较快 |

### 15.4.4 InvariantGlobalization

```xml
<InvariantGlobalization>false</InvariantGlobalization>
```

| 值 | 效果 |
|------|------|
| `true` | 不包含 ICU 数据，文件更小，但数字/日期格式化不可用 |
| `false` | 包含 ICU 数据，支持所有区域格式化 |

CodexSwitch 使用 `false`，因为需要支持多种语言的数字和日期格式化。

### 15.4.5 源生成器在 AOT 中的应用

.NET 的源生成器在编译时生成代码，避免运行时反射：

```csharp
// System.Text.Json 源生成器
[JsonSerializable(typeof(MyClass))]
internal partial class MyJsonContext : JsonSerializerContext { }

// INotifyPropertyChanged 源生成器（CommunityToolkit.Mvvm）
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;  // 自动生成 Name 属性和通知
}
```

### 15.4.6 裁剪警告处理

当裁剪器检测到可能不兼容的代码时，会发出警告：

```
warning IL2026: Member 'Type.GetMethod(String)' with trimming...
```

处理方式：
1. 使用 `[DynamicallyAccessedMembers]` 注解
2. 使用源生成器替代反射
3. 使用 `[UnconditionalSuppressMessage]` 抑制已确认安全的警告

```csharp
// 使用注解保留成员
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
public Type MyType { get; set; }

// 抑制已确认安全的警告
[UnconditionalSuppressMessage("Trimming", "IL2026")]
public void SafeMethod()
{
    // 已确认此代码路径安全
}
```

## 15.5 举一反三

### 15.5.1 编译绑定 vs 运行时绑定选择指南

| 场景 | 推荐 | 原因 |
|------|------|------|
| 新项目 | 编译绑定 | 性能好，类型安全 |
| AOT 发布 | 编译绑定 | 必须使用 |
| 快速原型 | 运行时绑定 | 不需要 x:DataType |
| 动态类型 | 运行时绑定 | 编译绑定需要已知类型 |
| 代码生成 | 编译绑定 | 天然兼容 |

### 15.5.2 单文件发布 vs AOT 发布

| 特性 | 单文件发布 | AOT 发布 |
|------|-----------|---------|
| 命令 | `-p:PublishSingleFile=true` | `-p:PublishAot=true` |
| 代码类型 | 托管代码打包 | 原生机器码 |
| 启动时间 | 较快 | 最快 |
| 文件大小 | 较大（包含运行时） | 较小 |
| 兼容性 | 好 | 需要 AOT 兼容 |
| 反射 | 支持 | 不支持 |

### 15.5.3 与 Web 前端编译优化的对比

| Web (Bundler) | Avalonia (AOT) |
|---------------|----------------|
| Tree Shaking | TrimMode=full |
| TypeScript 类型检查 | 编译绑定类型检查 |
| Source Map | PDB 文件 |

## 15.6 最佳实践与设计模式

### 15.6.1 静态资源 vs 动态资源

```xml
<!-- 快：编译时解析 -->
<Button Background="{StaticResource CsPrimaryBrush}"/>

<!-- 慢：运行时解析 -->
<Button Background="{DynamicResource CsPrimaryBrush}"/>
```

只在需要运行时切换的场景使用 `DynamicResource`（如主题切换）。CodexSwitch 的主题切换通过修改 `SolidColorBrush.Color` 属性实现，避免使用 `DynamicResource`。

### 15.6.2 虚拟化

```xml
<ItemsControl.ItemsPanel>
    <ItemsPanelTemplate>
        <VirtualizingStackPanel/>  <!-- 只渲染可见项 -->
    </ItemsPanelTemplate>
</ItemsControl.ItemsPanel>
```

### 15.6.3 减少视觉树深度

```xml
<!-- 好：扁平结构 -->
<Grid ColumnDefinitions="Auto,*,Auto">
    <Image/>
    <TextBlock Grid.Column="1"/>
    <Button Grid.Column="2"/>
</Grid>

<!-- 不好：嵌套 StackPanel -->
<StackPanel>
    <DockPanel>
        <Image/>
        <StackPanel>
            <TextBlock/>
            <Button/>
        </StackPanel>
    </DockPanel>
</StackPanel>
```

### 15.6.4 内存管理

```csharp
// 窗口关闭时解绑事件
Closed += (_, _) =>
{
    _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    _collapseTimer.Stop();
    CloseDetailsWindow();
};

// ViewModel 实现 IAsyncDisposable
public async ValueTask DisposeAsync()
{
    await _proxyHostService.StopAsync();
    _usageLogWriter.Dispose();
    _updateChecker.Dispose();
}

// DispatcherTimer 清理
protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnDetachedFromVisualTree(e);
    _animationTimer?.Stop();
    _animationTimer = null;
}
```

### 15.6.5 调试技巧

```csharp
// Program.cs -- 将绑定错误输出到调试输出
.LogToTrace()
```

在运行时按 `F12` 打开 Avalonia 诊断工具（如果安装了 AvaloniaUI.DiagnosticsSupport）。

## Deep Dive：编译绑定的内部实现

### XAML 编译器的绑定代码生成

当 Avalonia XAML 编译器处理 `{Binding UserName}` 时：

1. **解析绑定表达式**：提取路径 `UserName`
2. **查找 DataType**：从 `x:DataType` 或父元素继承
3. **验证属性**：检查 `UserName` 是否存在于 DataType 类型上
4. **生成绑定代码**：生成强类型的属性访问代码

```csharp
// 编译器生成的代码（概念）
public void ApplyBindings(MyViewModel viewModel)
{
    // 直接属性访问，无反射
    this[!TextBlock.TextProperty] = new CompiledBinding<MyViewModel, string>(
        viewModel,
        vm => vm.UserName,
        (vm, value) => vm.UserName = value,
        BindingMode.OneWay);
}
```

### AOT 下的绑定性能

```
运行时绑定：
    字符串解析 -> 反射查找 -> IL 调用 -> 值返回
    每次属性访问约 100-200ns

编译绑定：
    直接字段访问 -> 值返回
    每次属性访问约 5-10ns
```

编译绑定比运行时绑定快 10-20 倍。

### AvaloniaUseCompiledBindingsByDefault 的作用

这个 MSBuild 属性设置了 AXAML 编译器的默认行为。当为 `true` 时：

1. 所有 `{Binding}` 表达式默认使用编译绑定
2. 没有 `x:DataType` 的绑定会产生警告
3. 编译器会尝试为每个绑定生成强类型代码

当为 `false` 时（默认）：

1. 所有 `{Binding}` 表达式默认使用运行时绑定
2. 只有标记了 `x:DataType` 的元素使用编译绑定
3. 不会产生缺少 DataType 的警告

## Cross References

- **AXAML 基础**：绑定语法详解，参见 [第 3 章](03-axaml-fundamentals.md)
- **数据绑定**：绑定模式和高级用法，参见 [第 5 章](05-data-binding.md)
- **属性系统**：属性系统内部原理，参见 [第 22 章](22-property-system.md)
- **样式系统**：StaticResource vs DynamicResource，参见 [第 7 章](07-styling-theming.md)
- **国际化**：源生成 JSON 序列化，参见 [第 11 章](11-i18n.md)
- **自定义控件**：StyledProperty 的注册，参见 [第 9 章](09-custom-controls.md)

## Common Pitfalls

### 陷阱 1：忘记指定 x:DataType

```xml
<!-- 警告：没有 DataType，回退到运行时绑定 -->
<Window>
    <TextBlock Text="{Binding Name}"/>
</Window>

<!-- 正确：指定 DataType -->
<Window x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding Name}"/>
</Window>
```

### 陷阱 2：AOT 不兼容的代码

```csharp
// 错误：运行时反射序列化
JsonSerializer.Deserialize<T>(stream);

// 正确：源生成序列化
JsonSerializer.Deserialize(stream, CodexSwitchJsonContext.Default.MyType);

// 错误：动态创建实例
var obj = Activator.CreateInstance(type);

// 正确：已知类型直接 new
var obj = new MyClass();

// 错误：动态加载程序集
var assembly = Assembly.LoadFrom("plugin.dll");

// 正确：不使用动态加载
```

### 陷阱 3：InvariantGlobalization 设为 true

```xml
<!-- 问题：数字和日期格式化不可用 -->
<InvariantGlobalization>true</InvariantGlobalization>

<!-- 正确：保留区域信息 -->
<InvariantGlobalization>false</InvariantGlobalization>
```

### 陷阱 4：DataType 与实际 DataContext 不匹配

```xml
<!-- 错误：DataType 是 ViewModel，但 DataContext 是 Model -->
<UserControl x:DataType="vm:SettingsViewModel">
    <!-- 如果 DataContext 实际上是 SettingsModel，绑定会失败 -->
</UserControl>
```

### 陷阱 5：DataTemplate 中未指定 DataType

```xml
<!-- 错误：ItemTemplate 中没有 DataType -->
<ItemsControl.ItemTemplate>
    <DataTemplate>
        <TextBlock Text="{Binding Name}"/>  <!-- 编译警告 -->
    </DataTemplate>
</ItemsControl.ItemTemplate>

<!-- 正确：指定 DataType -->
<ItemsControl.ItemTemplate>
    <DataTemplate x:DataType="models:ItemModel">
        <TextBlock Text="{Binding Name}"/>
    </DataTemplate>
</ItemsControl.ItemTemplate>
```

### 陷阱 6：DynamicResource 与编译绑定的冲突

`DynamicResource` 本身不支持编译绑定。如果需要运行时切换资源，仍然使用 `DynamicResource`。

### 陷阱 7：裁剪器移除了需要的代码

```csharp
// 问题：裁剪器不知道此类型在运行时需要
var type = Type.GetType("MyApp.MyClass");  // 可能被裁剪

// 解决：使用 [DynamicallyAccessedMembers] 注解
[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MyClass))]
public void EnsureType() { }
```

### 陷阱 8：单文件发布与 AOT 混淆

```bash
# AOT 发布（原生编译）
dotnet publish -p:PublishAot=true

# 单文件发布（托管代码打包）
dotnet publish -p:PublishSingleFile=true

# 两者不同：AOT 生成原生代码，SingleFile 只是打包
```

### 陷阱 9：AOT 发布的平台特定依赖

AOT 发布必须为每个目标平台单独编译。不能在 macOS 上编译 Windows 的 AOT 可执行文件。

### 陷阱 10：社区库的 AOT 兼容性

不是所有 NuGet 包都支持 AOT。使用前检查：
- 是否使用了 `JsonSerializer.Deserialize<T>()`？
- 是否使用了 `Activator.CreateInstance()`？
- 是否有源生成器版本？

CodexSwitch 使用的 CommunityToolkit.Mvvm 通过源生成器生成属性通知代码，天然 AOT 兼容。

### 陷阱 11：调试 AOT 发布的应用

AOT 发布的应用调试信息有限。建议：
- 先在 Debug 模式下修复所有问题
- 使用 `PublishAot=false` 测试裁剪效果
- 最后启用 `PublishAot=true` 进行最终测试

### 陷阱 12：编译绑定不支持某些绑定表达式

编译绑定不支持所有运行时绑定的语法。以下表达式在编译绑定中不可用：
- `Binding` 到非存在的属性（运行时会静默失败，编译时会报错）
- 复杂的多级路径（如 `A.B.C.D`）在某些场景下可能不支持
- 使用字符串索引器的绑定

## Try It Yourself

### 练习 1：验证编译绑定的类型检查

1. 在 CodexSwitch 中找到一个使用 `{Binding}` 的 AXAML 文件
2. 确认它指定了 `x:DataType`
3. 故意拼错一个绑定属性名
4. 观察编译错误

### 练习 2：发布 AOT 版本

```bash
# 发布当前平台的 AOT 版本
dotnet publish CodexSwitch/CodexSwitch.csproj -c Release -r osx-arm64 --self-contained true -p:PublishAot=true

# 比较文件大小
ls -lh CodexSwitch/bin/Release/net10.0/osx-arm64/publish/
```

### 练习 3：添加源生成 JSON 上下文

为一个新的数据类型添加源生成 JSON 支持：

```csharp
[JsonSerializable(typeof(MyNewType))]
internal partial class CodexSwitchJsonContext : JsonSerializerContext { }
```

### 练习 4：测试裁剪效果

```bash
# 启用裁剪分析
dotnet publish -p:PublishTrimmed=true -p:TrimMode=full 2>&1 | grep "IL"
```

观察裁剪警告，逐一修复。

### 练习 5：性能基准测试

创建一个简单的基准测试，对比编译绑定和运行时绑定的性能。

### 练习 6：优化 AOT 文件大小

尝试以下优化：
1. 移除未使用的 NuGet 包
2. 设置 `InvariantGlobalization=true`（如果不需要区域格式化）
3. 启用 `TrimMode=full`
4. 比较优化前后的文件大小

### 练习 7：实现跨平台 AOT 构建脚本

创建一个脚本，一次构建所有平台的 AOT 版本：

```bash
#!/bin/bash
for rid in win-x64 osx-x64 osx-arm64 linux-x64; do
    echo "Building for $rid..."
    dotnet publish CodexSwitch/CodexSwitch.csproj \
        -c Release \
        -r $rid \
        --self-contained true \
        -p:PublishAot=true \
        -o publish/$rid
done
```

### 练习 8：对比 AOT 与非 AOT 的启动时间

分别测量 AOT 和非 AOT 版本的启动时间，验证 AOT 的性能优势。
