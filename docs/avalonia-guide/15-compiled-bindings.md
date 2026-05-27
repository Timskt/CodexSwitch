# 15. 编译绑定与 AOT 发布

## 15.1 编译绑定详解

### 启用方式

```xml
<!-- .csproj -->
<PropertyGroup>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
</PropertyGroup>
```

### 工作原理

```
传统绑定（运行时）：
AXAML "{Binding Name}"
    → 运行时反射查找 "Name" 属性
    → 性能开销，无类型检查

编译绑定（编译时）：
AXAML "{Binding Name}" + x:DataType="vm:MyViewModel"
    → 编译器生成强类型绑定代码
    → 直接属性访问，零反射
    → 编译时检查属性是否存在
```

### 使用要求

每个使用编译绑定的 AXAML 元素必须指定 `x:DataType`：

```xml
<!-- 窗口级别 -->
<Window x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding Name}"/>  <!-- 编译器验证 Name 存在 -->

    <!-- DataTemplate 级别 -->
    <ItemsControl ItemsSource="{Binding Items}">
        <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="vm:ItemModel">
                <TextBlock Text="{Binding Title}"/>  <!-- 编译器验证 Title 存在 -->
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>
```

### 编译时错误

如果绑定的属性不存在，编译时就会报错：

```xml
<!-- 假设 MyViewModel 没有 Nme 属性（拼写错误） -->
<TextBlock Text="{Binding Nme}"/>
<!-- 编译错误：Cannot find property 'Nme' on type 'MyViewModel' -->
```

## 15.2 Native AOT 发布

### 配置

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
    <TrimMode>full</TrimMode>
    <InvariantGlobalization>false</InvariantGlobalization>
</PropertyGroup>
```

### AOT 兼容性要求

| 要求 | 说明 |
|------|------|
| 编译绑定 | 必须启用，运行时绑定依赖反射 |
| 源生成 JSON | 使用 `JsonSerializerContext` 而非 `JsonSerializer.Deserialize<T>()` |
| 无动态加载 | 不能使用 `Assembly.LoadFrom()` |
| 无反射创建 | 不能使用 `Activator.CreateInstance()` |
| TrimMode=full | 完整裁剪，移除未使用的代码 |

### 源生成 JSON 序列化

```csharp
// 声明序列化上下文
[JsonSerializable(typeof(I18nCatalog))]
[JsonSerializable(typeof(I18nLanguageResource))]
[JsonSerializable(typeof(AppConfig))]
internal partial class CodexSwitchJsonContext : JsonSerializerContext { }

// 使用（AOT 兼容）
return JsonSerializer.Deserialize(stream, CodexSwitchJsonContext.Default.I18nCatalog);

// 不要使用（AOT 不兼容）
return JsonSerializer.Deserialize<I18nCatalog>(stream);  // ❌ 依赖反射
```

### 发布命令

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

## 15.3 性能优化技巧

### 1. 静态资源 vs 动态资源

```xml
<!-- 快：编译时解析 -->
<Button Background="{StaticResource CsPrimaryBrush}"/>

<!-- 慢：运行时解析 -->
<Button Background="{DynamicResource CsPrimaryBrush}"/>
```

只在需要运行时切换的场景使用 `DynamicResource`（如主题切换）。

### 2. 虚拟化

```xml
<!-- 虚拟化：只渲染可见项 -->
<ItemsControl.ItemsPanel>
    <ItemsPanelTemplate>
        <VirtualizingStackPanel/>
    </ItemsPanelTemplate>
</ItemsControl.ItemsPanel>
```

### 3. 避免不必要的绑定

```xml
<!-- 好：直接设置静态值 -->
<TextBlock Text="Save" FontWeight="SemiBold"/>

<!-- 不好：绑定到不会变化的值 -->
<TextBlock Text="{Binding StaticLabel}" FontWeight="SemiBold"/>
```

### 4. 合理使用 ClipToBounds

```xml
<!-- 只在需要裁剪时启用 -->
<Border ClipToBounds="True">
    <!-- 有动画溢出的内容 -->
</Border>
```

`ClipToBounds` 会强制创建裁剪区域，有轻微性能开销。

### 5. 减少视觉树深度

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

### 6. DispatcherTimer 优先级

```csharp
// 使用合适的优先级
Dispatcher.UIThread.Post(() => UpdateUI(), DispatcherPriority.Render);

// 优先级从高到低：
// Invalid    - 无效
// Inactive   - 不活跃
// Input      - 输入处理
// Loaded     - 加载完成
// Render     - 渲染（推荐用于 UI 更新）
// DataBind   - 数据绑定
// Normal     - 普通
// Send       - 最高
```

## 15.4 AvaloniaUI.DiagnosticsSupport

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

## 15.5 内存管理

### 手动 GC

```csharp
// 在窗口关闭后建议 GC
private static void RequestMemoryTrim()
{
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced,
        blocking: false, compacting: true);
}
```

### 事件解绑

```csharp
// 窗口关闭时解绑事件
Closed += (_, _) =>
{
    _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    _collapseTimer.Stop();
    CloseDetailsWindow();
};
```

### IDisposable / IAsyncDisposable

```csharp
// ViewModel 实现 IAsyncDisposable
public async ValueTask DisposeAsync()
{
    await _proxyHostService.StopAsync();
    _usageLogWriter.Dispose();
    _updateChecker.Dispose();
}
```

### DispatcherTimer 清理

```csharp
protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnDetachedFromVisualTree(e);
    _animationTimer?.Stop();
    _animationTimer = null;
}
```

## 15.6 调试技巧

### 绑定错误日志

```csharp
// Program.cs
.LogToTrace()  // 将绑定错误输出到调试输出
```

### 诊断模式

在运行时按 `F12` 打开 Avalonia 诊断工具（如果安装了 AvaloniaUI.DiagnosticsSupport）。

---

## Deep Dive：编译绑定的内部实现

当你启用 `AvaloniaUseCompiledBindingsByDefault` 时，Avalonia 的 XAML 编译器会为每个 `{Binding}` 表达式生成强类型的 C# 代码。例如：

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

## Cross References

- [第 3 章 AXAML 基础语法](03-axaml-fundamentals.md) — 绑定语法详解
- [第 5 章 数据绑定](05-data-binding.md) — 绑定模式和高级用法
- [第 22 章 Avalonia 属性系统](22-property-system.md) — 属性系统内部原理

## Common Pitfalls

1. **忘记指定 x:DataType**: 编译绑定需要 `x:DataType`，否则会回退到运行时绑定
2. **AOT 不兼容的代码**: 使用反射的代码在 AOT 下会失败
3. **InvariantGlobalization 设为 true**: 这会导致 i18n 和数字格式化出问题

## Try It Yourself

1. 在 CodexSwitch 中找到一个使用 `{Binding}` 的 AXAML 文件，检查它是否指定了 `x:DataType`
2. 尝试故意拼错一个绑定属性名，观察编译错误
3. 使用 `dotnet publish` 命令发布一个 AOT 版本，比较文件大小
