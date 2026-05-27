# 5. 数据绑定

## 5.1 绑定基础

数据绑定是 MVVM 模式的核心，它将 UI 元素与数据源连接起来。

### 绑定模式

| 模式 | 说明 | 用途 |
|------|------|------|
| `OneWay` | 数据源 → UI | 显示数据（默认） |
| `TwoWay` | 数据源 ↔ UI | 表单输入 |
| `OneTime` | 数据源 → UI（仅一次） | 静态数据 |
| `OneWayToSource` | UI → 数据源 | 少用 |

```xml
<!-- 单向绑定（默认） -->
<TextBlock Text="{Binding UserName}"/>

<!-- 双向绑定 -->
<TextBox Text="{Binding SearchText, Mode=TwoWay}"/>

<!-- 单次绑定 -->
<TextBlock Text="{Binding AppVersion, Mode=OneTime}"/>
```

### 绑定模式详解

**OneWay 绑定**：
- 数据源变化时自动更新 UI
- UI 变化不会影响数据源
- 适用于只读显示

**TwoWay 绑定**：
- 数据源变化时更新 UI
- UI 变化时更新数据源
- 适用于表单输入

**OneTime 绑定**：
- 只在初始化时绑定一次
- 后续数据源变化不会更新 UI
- 适用于静态数据

**OneWayToSource 绑定**：
- UI 变化时更新数据源
- 数据源变化不会更新 UI
- 适用于 UI 驱动的场景

## 5.2 绑定路径

```xml
<!-- 简单属性 -->
<TextBlock Text="{Binding Name}"/>

<!-- 嵌套属性 -->
<TextBlock Text="{Binding User.Address.City}"/>

<!-- 索引器 -->
<TextBlock Text="{Binding Items[0]}"/>

<!-- 自身 -->
<TextBlock Text="{Binding $self.Bounds.Width}"/>

<!-- 父元素 -->
<Border Background="{Binding #ParentPanel.Background}"/>
```

### 绑定路径语法详解

**简单属性**：
```xml
<TextBlock Text="{Binding Name}"/>
<!-- 等价于 -->
<TextBlock Text="{Binding Path=Name}"/>
```

**嵌套属性**：
```xml
<TextBlock Text="{Binding User.Address.City}"/>
<!-- 等价于 -->
<TextBlock Text="{Binding Path=User.Address.City}"/>
```

**索引器**：
```xml
<TextBlock Text="{Binding Items[0]}"/>
<!-- 等价于 -->
<TextBlock Text="{Binding Path=Items[0]}"/>

<!-- 多维索引器 -->
<TextBlock Text="{Binding Matrix[0,1]}"/>
```

**自身绑定**：
```xml
<TextBlock Text="{Binding $self.Bounds.Width}"/>
<!-- $self 引用当前元素 -->
```

**父元素绑定**：
```xml
<Border Background="{Binding #ParentPanel.Background}"/>
<!-- # 引用命名元素 -->
```

### 绑定路径中的特殊字符

**转义字符**：
```xml
<!-- 转义括号 -->
<TextBlock Text="{Binding Items[0\]}"/>

<!-- 转义点号 -->
<TextBlock Text="{Binding User\.Name}"/>
```

**空格**：
```xml
<!-- 路径中的空格需要转义 -->
<TextBlock Text="{Binding 'User Name'}"/>
```

## 5.3 编译绑定 (x:DataType)

```xml
<Window x:DataType="vm:MainWindowViewModel">
    <!-- 编译器会验证 AppName 是否存在 -->
    <TextBlock Text="{Binding AppName}"/>
</Window>
```

### 好处

1. **类型安全**: 编译时检查属性是否存在
2. **性能**: 直接属性访问，无需反射
3. **AOT 兼容**: 不依赖运行时元数据

### 编译绑定的工作原理

当启用编译绑定时：

1. **编译时**：XAML 编译器分析绑定表达式
2. **类型检查**：验证绑定路径在 x:DataType 上是否存在
3. **代码生成**：生成直接访问属性的代码
4. **运行时**：直接调用属性 getter/setter

```xml
<!-- 编译绑定 -->
<Window x:DataType="vm:MainViewModel">
    <TextBlock Text="{Binding Name}"/>
</Window>
```

生成的代码：
```csharp
// 编译器生成的代码
var textBlock = new TextBlock();
textBlock.Bind(TextBlock.TextProperty, 
    new CompiledBinding<string>(
        // 直接访问属性，无反射
        () => ((MainViewModel)DataContext).Name
    ));
```

### 编译绑定的限制

1. **必须设置 x:DataType**：没有 x:DataType 的绑定会回退到运行时绑定
2. **类型必须匹配**：绑定路径必须在指定类型上存在
3. **不支持动态类型**：不能绑定到 `dynamic` 或 `object` 类型的属性
4. **索引器限制**：编译绑定对索引器的支持有限

## 5.4 RelativeSource 绑定

```xml
<!-- 绑定到自身 -->
<TextBlock Text="{Binding $self.Name}"/>

<!-- 绑定到 TemplatedParent（在 ControlTemplate 中） -->
<Border Background="{Binding Background, RelativeSource={RelativeSource TemplatedParent}}"/>

<!-- 绑定到父级 -->
<TextBlock Text="{Binding DataContext.Title, RelativeSource={RelativeSource AncestorType=Window}}"/>

<!-- 绑定到命名元素 -->
<Button Command="{Binding #MyListBox.SelectedItem.DeleteCommand}"/>
```

### RelativeSource 模式详解

**Self**：
```xml
<!-- 绑定到自身 -->
<TextBlock Text="{Binding Name, RelativeSource={RelativeSource Self}}"/>
<!-- 等价于 -->
<TextBlock Text="{Binding $self.Name}"/>
```

**TemplatedParent**：
```xml
<!-- 在 ControlTemplate 中绑定到模板父元素 -->
<ControlTemplate>
    <Border Background="{Binding Background, 
                        RelativeSource={RelativeSource TemplatedParent}}"/>
</ControlTemplate>
<!-- 等价于 -->
<ControlTemplate>
    <Border Background="{TemplateBinding Background}"/>
</ControlTemplate>
```

**AncestorType**：
```xml
<!-- 绑定到指定类型的祖先元素 -->
<TextBlock Text="{Binding DataContext.Title, 
                 RelativeSource={RelativeSource AncestorType=Window}}"/>

<!-- 绑定到指定类型的祖先元素（指定层级） -->
<TextBlock Text="{Binding DataContext.Title, 
                 RelativeSource={RelativeSource AncestorType=Window, AncestorLevel=2}}"/>
```

**FindAncestor**：
```xml
<!-- 查找祖先元素 -->
<TextBlock Text="{Binding DataContext.Title, 
                 RelativeSource={RelativeSource FindAncestor, 
                                 AncestorType={x:Type Window}}}"/>
```

## 5.5 FallbackValue 和 TargetNullValue

```xml
<!-- 当绑定失败时显示的值 -->
<TextBlock Text="{Binding UserName, FallbackValue='Guest'}"/>

<!-- 当绑定值为 null 时显示的值 -->
<TextBlock Text="{Binding Description, TargetNullValue='No description'}"/>
```

### FallbackValue 与 TargetNullValue 的区别

**FallbackValue**：
- 绑定失败时使用
- 属性不存在
- 路径错误
- 数据类型不匹配

**TargetNullValue**：
- 绑定成功但值为 null 时使用
- 属性存在但值为 null

```xml
<!-- 示例 -->
<TextBlock Text="{Binding UserName, 
                 FallbackValue='User not found',
                 TargetNullValue='Anonymous'}"/>
```

## 5.6 StringFormat

```xml
<!-- 数字格式化 -->
<TextBlock Text="{Binding Price, StringFormat='{}${0:F2}'}"/>

<!-- 日期格式化 -->
<TextBlock Text="{Binding CreatedAt, StringFormat='{}{0:yyyy-MM-dd}'}"/>

<!-- 复合格式化 -->
<TextBlock Text="{Binding Count, StringFormat='Total: {0:N0} items'}"/>
```

### StringFormat 语法

**数字格式化**：
```xml
<!-- 货币 -->
<TextBlock Text="{Binding Price, StringFormat='{}${0:F2}'}"/>
<!-- 输出: $123.45 -->

<!-- 百分比 -->
<TextBlock Text="{Binding Rate, StringFormat='{}{0:P2}'}"/>
<!-- 输出: 12.34% -->

<!-- 千位分隔符 -->
<TextBlock Text="{Binding Count, StringFormat='{}{0:N0}'}"/>
<!-- 输出: 1,234 -->
```

**日期格式化**：
```xml
<!-- 短日期 -->
<TextBlock Text="{Binding Date, StringFormat='{}{0:d}'}"/>
<!-- 输出: 12/31/2025 -->

<!-- 长日期 -->
<TextBlock Text="{Binding Date, StringFormat='{}{0:D}'}"/>
<!-- 输出: Tuesday, December 31, 2025 -->

<!-- 自定义格式 -->
<TextBlock Text="{Binding Date, StringFormat='{}{0:yyyy-MM-dd HH:mm}'}"/>
<!-- 输出: 2025-12-31 14:30 -->
```

**复合格式化**：
```xml
<!-- 多个值 -->
<TextBlock Text="{Binding Count, StringFormat='Total: {0} items'}"/>
<!-- 输出: Total: 42 items -->

<!-- 对齐和填充 -->
<TextBlock Text="{Binding Name, StringFormat='Hello, {0,-10}!'}"/>
<!-- 输出: Hello, John      ! -->
```

## 5.7 绑定到集合

```xml
<!-- 绑定到 ObservableCollection -->
<ListBox ItemsSource="{Binding Items}">
    <ListBox.ItemTemplate>
        <DataTemplate x:DataType="vm:ItemModel">
            <TextBlock Text="{Binding Name}"/>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### ObservableCollection vs List

| 类型 | 说明 |
|------|------|
| `List<T>` | 不通知 UI 更新 |
| `ObservableCollection<T>` | 添加/删除时通知 UI |
| `ReadOnlyObservableCollection<T>` | 只读版本 |

### ObservableCollection 的使用

```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ItemViewModel> _items = new();
    
    [RelayCommand]
    private void AddItem()
    {
        Items.Add(new ItemViewModel { Name = "New Item" });
    }
    
    [RelayCommand]
    private void RemoveItem(ItemViewModel item)
    {
        Items.Remove(item);
    }
}
```

### 集合绑定的注意事项

1. **不要替换集合引用**：替换集合引用不会触发 UI 更新
2. **使用 ObservableCollection**：确保集合变化时 UI 更新
3. **避免频繁修改**：大量修改时考虑使用 `AddRange` 或批量更新

```csharp
// 错误：替换集合引用
Items = new ObservableCollection<ItemViewModel>(newItems);

// 正确：修改现有集合
Items.Clear();
foreach (var item in newItems)
{
    Items.Add(item);
}
```

## 5.8 命令绑定

```xml
<!-- 绑定到 ICommand -->
<Button Command="{Binding SaveCommand}"/>

<!-- 带参数 -->
<Button Command="{Binding DeleteCommand}"
        CommandParameter="{Binding Id}"/>

<!-- 绑定到带参数的命令 -->
<Button Command="{Binding MoveProviderCommand}"
        CommandParameter="{Binding}"/>
```

### 命令绑定详解

**基本命令绑定**：
```xml
<Button Command="{Binding SaveCommand}"/>
```

**带参数的命令绑定**：
```xml
<!-- 参数是静态值 -->
<Button Command="{Binding DeleteCommand}"
        CommandParameter="123"/>

<!-- 参数是绑定值 -->
<Button Command="{Binding DeleteCommand}"
        CommandParameter="{Binding Id}"/>

<!-- 参数是 DataContext -->
<Button Command="{Binding DeleteCommand}"
        CommandParameter="{Binding}"/>
```

### 命令的 CanExecute

```csharp
[RelayCommand(CanExecute = nameof(CanSave))]
private void Save()
{
    // 保存逻辑
}

private bool CanSave => !string.IsNullOrEmpty(Name);
```

```xml
<!-- 当 CanSave 为 false 时，按钮自动禁用 -->
<Button Command="{Binding SaveCommand}"/>
```

### 异步命令

```csharp
[RelayCommand]
private async Task LoadDataAsync(CancellationToken token)
{
    // 异步加载数据
    await Task.Delay(1000, token);
}
```

```xml
<!-- 异步命令会自动处理加载状态 -->
<Button Command="{Binding LoadDataCommand}"/>
```

## 5.9 绑定到静态资源

```xml
<!-- 绑定到资源字典中的值 -->
<Button Background="{StaticResource CsPrimaryBrush}"/>

<!-- 动态资源（支持运行时更改） -->
<Window FontFamily="{DynamicResource CodexSwitch.FontFamily}"/>
```

### 静态资源 vs 动态资源

**静态资源**：
- 编译时解析
- 不会更新
- 性能更好
- 适用于不会变化的资源

**动态资源**：
- 运行时解析
- 资源变化时自动更新
- 性能较差
- 适用于需要主题切换的资源

## 5.10 绑定到枚举

```csharp
public enum ProviderStatus
{
    Active,
    Inactive,
    Error
}
```

```xml
<!-- 在 AXAML 中引用枚举值 -->
<StackPanel>
    <TextBlock Text="{Binding Status}"/>
</StackPanel>
```

### 枚举绑定的注意事项

1. **显示枚举名称**：默认显示枚举的 ToString() 值
2. **使用值转换器**：可以使用转换器显示友好的名称
3. **绑定到枚举值**：可以使用 x:Static 引用枚举值

```xml
<!-- 使用值转换器 -->
<TextBlock Text="{Binding Status, 
             Converter={x:Static converters:StatusConverter.Instance}}"/>
```

## 5.11 MultiBinding（多绑定）

```xml
<!-- 多个绑定组合 -->
<TextBlock>
    <TextBlock.Text>
        <MultiBinding StringFormat="{}{0} - {1}">
            <Binding Path="FirstName"/>
            <Binding Path="LastName"/>
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
```

### MultiBinding 的使用场景

**组合多个值**：
```xml
<TextBlock>
    <TextBlock.Text>
        <MultiBinding StringFormat="{}{0} {1}">
            <Binding Path="FirstName"/>
            <Binding Path="LastName"/>
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
```

**使用值转换器**：
```xml
<TextBlock>
    <TextBlock.Text>
        <MultiBinding Converter="{x:Static converters:FullNameConverter.Instance}">
            <Binding Path="FirstName"/>
            <Binding Path="LastName"/>
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
```

## 5.12 设计时数据

```xml
<Window x:DataType="vm:MainWindowViewModel">
    <!-- 为设计器提供数据上下文 -->
    <Design.DataContext>
        <vm:MainWindowViewModel/>
    </Design.DataContext>

    <!-- 设计时属性 -->
    <TextBlock d:DataContext="{d:DesignInstance vm:MainWindowViewModel}"
               Text="{Binding Name}"/>
</Window>
```

这使 Avalonia 设计器（VS/Rider）能在设计时解析绑定表达式。

### 设计时数据的好处

1. **可视化设计**：在设计器中看到绑定的数据
2. **智能提示**：获得绑定路径的智能提示
3. **早期错误检测**：在设计时发现绑定错误

### Design.DataContext 的限制

1. **不会在运行时生效**：只在设计器中使用
2. **需要默认构造函数**：ViewModel 需要有无参构造函数
3. **可能与实际数据不同**：设计时数据可能与运行时数据不同

---

## Deep Dive

### 绑定路径语法详解

**路径表达式**：
```
property.subproperty[indexer].subproperty
```

**特殊字符**：
- `.` — 属性访问
- `[]` — 索引器访问
- `$self` — 自身引用
- `#` — 命名元素引用

**转义字符**：
- `\.` — 转义点号
- `\[` — 转义左括号
- `\]` — 转义右括号

### 绑定到方法

Avalonia 不直接支持绑定到方法，但可以通过以下方式实现：

**使用命令**：
```csharp
[RelayCommand]
private void MyMethod()
{
    // 方法逻辑
}
```

```xml
<Button Command="{Binding MyMethodCommand}"/>
```

**使用委托**：
```csharp
public Action MyMethod { get; } = () => { /* 方法逻辑 */ };
```

```xml
<Button Command="{Binding MyMethod}"/>
```

### 绑定到索引器

```xml
<!-- 绑定到列表索引 -->
<TextBlock Text="{Binding Items[0]}"/>

<!-- 绑定到字典索引 -->
<TextBlock Text="{Binding Settings['Theme']}"/>

<!-- 绑定到多维索引 -->
<TextBlock Text="{Binding Matrix[0,1]}"/>
```

### PriorityBinding

PriorityBinding 允许指定多个绑定，按优先级尝试：

```xml
<TextBlock>
    <TextBlock.Text>
        <PriorityBinding>
            <Binding Path="FastValue"/>
            <Binding Path="MediumValue"/>
            <Binding Path="SlowValue" FallbackValue="Loading..."/>
        </PriorityBinding>
    </TextBlock.Text>
</TextBlock>
```

### 绑定调试

**使用调试器**：
```csharp
// 在绑定表达式中添加调试信息
<TextBlock Text="{Binding Name, diag:PresentationTraceSources.TraceLevel=High}"/>
```

**使用输出窗口**：
- 绑定错误会输出到调试日志
- 检查输出窗口中的绑定错误

**使用 Avalonia DevTools**：
- 查看绑定状态
- 检查绑定路径
- 验证数据上下文

### 绑定性能优化

**1. 使用编译绑定**：
```xml
<Window x:DataType="vm:MainViewModel">
    <TextBlock Text="{Binding Name}"/>
</Window>
```

**2. 避免复杂的绑定路径**：
```xml
<!-- 错误：复杂路径 -->
<TextBlock Text="{Binding User.Address.City.ZipCode}"/>

<!-- 正确：简化路径 -->
<TextBlock Text="{Binding UserZipCode}"/>
```

**3. 使用 OneTime 绑定**：
```xml
<!-- 对于静态数据，使用 OneTime 绑定 -->
<TextBlock Text="{Binding AppVersion, Mode=OneTime}"/>
```

**4. 避免不必要的绑定**：
```xml
<!-- 错误：对静态文本使用绑定 -->
<TextBlock Text="{Binding StaticText}"/>

<!-- 正确：直接设置文本 -->
<TextBlock Text="Static Text"/>
```

### 绑定到异步属性

```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _data = "Loading...";
    
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        Data = "Loading...";
        var result = await _service.GetDataAsync();
        Data = result;
    }
}
```

```xml
<TextBlock Text="{Binding Data}"/>
```

### 绑定组（Binding Groups）

绑定组允许将多个绑定分组，一起验证和提交：

```xml
<StackPanel>
    <StackPanel.BindingGroup>
        <BindingGroup>
            <BindingGroup.ValidationRules>
                <local:MyValidationRule/>
            </BindingGroup.ValidationRules>
        </BindingGroup>
    </StackPanel.BindingGroup>
    
    <TextBox Text="{Binding Name, Mode=TwoWay}"/>
    <TextBox Text="{Binding Email, Mode=TwoWay}"/>
    
    <Button Content="Save" Command="{Binding SaveCommand}"/>
</StackPanel>
```

---

## Cross References

- **[第 3 章：AXAML 基础](03-axaml-fundamentals.md)** — 学习 AXAML 中的绑定语法
- **[第 6 章：MVVM 模式](06-mvvm-pattern.md)** — 理解 ViewModel 和数据绑定
- **[第 19 章：值转换器](19-value-converters.md)** — 学习自定义值转换器
- **[第 22 章：属性系统](22-property-system.md)** — 深入了解 AvaloniaProperty
- **[第 24 章：资源系统](24-resource-system.md)** — 理解资源绑定

---

## Common Pitfalls

### 1. 忘记设置 x:DataType

**问题**：没有设置 x:DataType 导致绑定使用运行时反射。

```xml
<!-- 错误：没有 x:DataType -->
<Window>
    <TextBlock Text="{Binding Name}"/>  <!-- 运行时绑定 -->
</Window>

<!-- 正确：设置 x:DataType -->
<Window x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding Name}"/>  <!-- 编译绑定 -->
</Window>
```

### 2. 绑定路径拼写错误

**问题**：绑定路径拼写错误，编译时不会报错（如果没有 x:DataType）。

```xml
<!-- 错误：拼写错误 -->
<TextBlock Text="{Binding UserNmae}"/>

<!-- 正确：正确拼写 -->
<TextBlock Text="{Binding UserName}"/>
```

### 3. 忘记 Mode=TwoWay

**问题**：对于需要双向绑定的属性，忘记设置 Mode。

```xml
<!-- 错误：默认是 OneWay -->
<TextBox Text="{Binding Name}"/>

<!-- 正确：设置 TwoWay -->
<TextBox Text="{Binding Name, Mode=TwoWay}"/>
```

### 4. 绑定到不存在的属性

**问题**：绑定到 ViewModel 中不存在的属性。

```xml
<!-- 错误：属性不存在 -->
<TextBlock Text="{Binding NonExistentProperty}"/>

<!-- 正确：确保属性存在 -->
<TextBlock Text="{Binding ExistingProperty}"/>
```

### 5. 忘记 INotifyPropertyChanged

**问题**：ViewModel 没有实现 INotifyPropertyChanged，UI 不会更新。

```csharp
// 错误：没有实现 INotifyPropertyChanged
public class MainViewModel
{
    public string Name { get; set; }
}

// 正确：实现 INotifyPropertyChanged
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;
}
```

### 6. 混淆 StaticResource 和 DynamicResource

**问题**：对需要主题切换的资源使用 StaticResource。

```xml
<!-- 错误：主题切换时不会更新 -->
<Button Background="{StaticResource CsBackgroundBrush}"/>

<!-- 正确：主题切换时会更新 -->
<Button Background="{DynamicResource CsBackgroundBrush}"/>
```

### 7. 绑定到错误的数据上下文

**问题**：绑定使用了错误的数据上下文。

```xml
<!-- 错误：Window 的 DataContext 不是 MainViewModel -->
<Window>
    <TextBlock Text="{Binding Name}"/>
</Window>

<!-- 正确：设置正确的 DataContext -->
<Window x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding Name}"/>
</Window>
```

### 8. 忘记 FallbackValue

**问题**：绑定失败时没有回退值，导致 UI 显示空白。

```xml
<!-- 错误：没有 FallbackValue -->
<TextBlock Text="{Binding UserName}"/>

<!-- 正确：设置 FallbackValue -->
<TextBlock Text="{Binding UserName, FallbackValue='Guest'}"/>
```

---

## Try It Yourself

### 练习 1：创建基本绑定

1. 创建一个简单的 ViewModel：
   ```csharp
   public partial class TestViewModel : ObservableObject
   {
       [ObservableProperty]
       private string _name = "World";
       
       [ObservableProperty]
       private int _count;
       
       [RelayCommand]
       private void Increment()
       {
           Count++;
       }
   }
   ```

2. 创建对应的 View：
   ```xml
   <StackPanel>
       <TextBlock Text="{Binding Name}"/>
       <TextBox Text="{Binding Name, Mode=TwoWay}"/>
       <TextBlock Text="{Binding Count, StringFormat='Count: {0}'}"/>
       <Button Content="Increment" Command="{Binding IncrementCommand}"/>
   </StackPanel>
   ```

3. 运行项目，测试绑定

### 练习 2：探索 CodexSwitch 的绑定

1. 打开 `Views/MainWindow.axaml`

2. 识别以下绑定：
   - 简单属性绑定
   - 命令绑定
   - 资源绑定
   - 设计时数据绑定

3. 尝试修改一个绑定，观察编译错误

4. 添加一个新的绑定到 ViewModel 属性

### 练习 3：实现值转换器

1. 创建一个布尔到可见性转换器：
   ```csharp
   public class BoolToVisibilityConverter : IValueConverter
   {
       public static readonly BoolToVisibilityConverter Instance = new();
       
       public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
       {
           if (value is bool boolValue)
               return boolValue ? Visibility.Visible : Visibility.Collapsed;
           return Visibility.Collapsed;
       }
       
       public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
       {
           if (value is Visibility visibility)
               return visibility == Visibility.Visible;
           return false;
       }
   }
   ```

2. 在 AXAML 中使用：
   ```xml
   <TextBlock Text="Hello"
              IsVisible="{Binding IsVisible, 
                          Converter={x:Static converters:BoolToVisibilityConverter.Instance}}"/>
   ```

3. 运行项目，测试转换器

### 练习 4：实现集合绑定

1. 创建一个包含集合的 ViewModel：
   ```csharp
   public partial class ListViewModel : ObservableObject
   {
       [ObservableProperty]
       private ObservableCollection<ItemViewModel> _items = new();
       
       [RelayCommand]
       private void AddItem()
       {
           Items.Add(new ItemViewModel 
           { 
               Name = $"Item {Items.Count + 1}" 
           });
       }
       
       [RelayCommand]
       private void RemoveItem(ItemViewModel item)
       {
           Items.Remove(item);
       }
   }
   ```

2. 创建对应的 View：
   ```xml
   <StackPanel>
       <Button Content="Add Item" Command="{Binding AddItemCommand}"/>
       <ListBox ItemsSource="{Binding Items}">
           <ListBox.ItemTemplate>
               <DataTemplate x:DataType="vm:ItemViewModel">
                   <StackPanel Orientation="Horizontal">
                       <TextBlock Text="{Binding Name}"/>
                       <Button Content="Remove" 
                               Command="{Binding $parent[ListBox].DataContext.RemoveItemCommand}"
                               CommandParameter="{Binding}"/>
                   </StackPanel>
               </DataTemplate>
           </ListBox.ItemTemplate>
       </ListBox>
   </StackPanel>
   ```

3. 运行项目，测试集合绑定

### 练习 5：实现 MultiBinding

1. 创建一个 MultiBinding 转换器：
   ```csharp
   public class FullNameConverter : IMultiValueConverter
   {
       public static readonly FullNameConverter Instance = new();
       
       public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
       {
           if (values.Count >= 2 && values[0] is string firstName && values[1] is string lastName)
               return $"{firstName} {lastName}";
           return string.Empty;
       }
   }
   ```

2. 在 AXAML 中使用：
   ```xml
   <TextBlock>
       <TextBlock.Text>
           <MultiBinding Converter="{x:Static converters:FullNameConverter.Instance}">
               <Binding Path="FirstName"/>
               <Binding Path="LastName"/>
           </MultiBinding>
       </TextBlock.Text>
   </TextBlock>
   ```

3. 运行项目，测试 MultiBinding

### 练习 6：调试绑定错误

1. 故意创建一些绑定错误：
   ```xml
   <!-- 错误 1：属性不存在 -->
   <TextBlock Text="{Binding NonExistentProperty}"/>
   
   <!-- 错误 2：路径错误 -->
   <TextBlock Text="{Binding User.Address.City.ZipCode}"/>
   
   <!-- 错误 3：类型不匹配 -->
   <TextBlock Text="{Binding Count}"/>
   ```

2. 运行项目，观察调试输出

3. 使用 Avalonia DevTools 检查绑定状态

4. 修复绑定错误

### 练习 7：实现异步绑定

1. 创建一个异步加载的 ViewModel：
   ```csharp
   public partial class AsyncViewModel : ObservableObject
   {
       [ObservableProperty]
       private string _data = "Loading...";
       
       [ObservableProperty]
       private bool _isLoading;
       
       [RelayCommand]
       private async Task LoadDataAsync(CancellationToken token)
       {
           IsLoading = true;
           Data = "Loading...";
           
           try
           {
               await Task.Delay(2000, token);
               Data = "Data loaded successfully!";
           }
           catch (OperationCanceledException)
           {
               Data = "Loading cancelled.";
           }
           finally
           {
               IsLoading = false;
           }
       }
   }
   ```

2. 创建对应的 View：
   ```xml
   <StackPanel>
       <Button Content="Load Data" 
               Command="{Binding LoadDataCommand}"
               IsEnabled="{Binding !IsLoading}"/>
       <ProgressBar IsVisible="{Binding IsLoading}" 
                    IsIndeterminate="True"/>
       <TextBlock Text="{Binding Data}"/>
   </StackPanel>
   ```

3. 运行项目，测试异步绑定

### 练习 8：实现设计时数据

1. 创建一个支持设计时数据的 ViewModel：
   ```csharp
   public partial class DesignTimeViewModel : ObservableObject
   {
       [ObservableProperty]
       private string _name = "Design Time Data";
       
       [ObservableProperty]
       private int _count = 42;
       
       // 设计时构造函数
       public DesignTimeViewModel()
       {
           if (Design.IsDesignMode)
           {
               Name = "Design Time Name";
               Count = 100;
           }
       }
   }
   ```

2. 在 AXAML 中使用设计时数据：
   ```xml
   <Window x:DataType="vm:DesignTimeViewModel">
       <Design.DataContext>
           <vm:DesignTimeViewModel/>
       </Design.DataContext>
       
       <StackPanel>
           <TextBlock Text="{Binding Name}"/>
           <TextBlock Text="{Binding Count, StringFormat='Count: {0}'}"/>
       </StackPanel>
   </Window>
   ```

3. 在设计器中查看效果
