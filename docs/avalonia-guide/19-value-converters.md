# 19. 值转换器

## 19.1 IValueConverter 接口

```csharp
public interface IValueConverter
{
    object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);
    object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
}
```

### 值转换器的作用

值转换器在绑定表达式中对数据进行转换：

```
ViewModel 数据 → [Convert] → UI 显示
UI 输入 → [ConvertBack] → ViewModel 数据
```

## 19.2 常用转换器

### 布尔转可见性

```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Visibility.Visible;
    }
}
```

```xml
<StackPanel IsVisible="{Binding IsExpanded, Converter={x:Static converters:BoolToVisibilityConverter.Instance}}"/>
```

### 数字格式化

```csharp
public class TokenCountConverter : IValueConverter
{
    public static readonly TokenCountConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long count)
        {
            return count switch
            {
                >= 1_000_000 => $"{count / 1_000_000.0:F1}M",
                >= 1_000 => $"{count / 1_000.0:F1}K",
                _ => count.ToString("N0")
            };
        }
        return value?.ToString() ?? "";
    }
}
```

### 颜色转换

```csharp
public class StatusToColorConverter : IValueConverter
{
    public static readonly StatusToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Active" => new SolidColorBrush(Color.Parse("#36D399")),
            "Error" => new SolidColorBrush(Color.Parse("#FF6467")),
            "Warning" => new SolidColorBrush(Color.Parse("#FACC15")),
            _ => new SolidColorBrush(Color.Parse("#A3A3A3"))
        };
    }
}
```

## 19.3 参数化转换器

```csharp
public class EqualityConverter : IValueConverter
{
    public static readonly EqualityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Equals(value?.ToString(), parameter?.ToString());
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
```

```xml
<!-- 检查 CurrentPage 是否等于 "home" -->
<Border Background="{Binding CurrentPage,
    Converter={x:Static converters:EqualityConverter.Instance},
    ConverterParameter=home,
    Converter={x:Static BoolToBrushConverter.Instance}}"/>
```

## 19.4 多值转换器 (IMultiValueConverter)

```csharp
public interface IMultiValueConverter
{
    object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture);
}
```

```csharp
public class AllTrueConverter : IMultiValueConverter
{
    public static readonly AllTrueConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        return values.All(v => v is true);
    }
}
```

```xml
<Button.IsEnabled>
    <MultiBinding Converter="{x:Static converters:AllTrueConverter.Instance}">
        <Binding Path="IsNameValid"/>
        <Binding Path="IsEmailValid"/>
        <Binding Path="IsAgreed"/>
    </MultiBinding>
</Button.IsEnabled>
```

## 19.5 StringFormat 替代转换器

CodexSwitch 大量使用 `StringFormat` 替代值转换器：

```xml
<!-- 数字格式化 -->
<TextBlock Text="{Binding InputTokens, StringFormat='{}{0:N0}'}"/>

<!-- 货币格式化 -->
<TextBlock Text="{Binding EstimatedCost, StringFormat='{}${0:F4}'}"/>

<!-- 日期格式化 -->
<TextBlock Text="{Binding Timestamp, StringFormat='{}{0:yyyy-MM-dd HH:mm}'}"/>

<!-- 复合格式化 -->
<TextBlock Text="{Binding Count, StringFormat='Total: {0:N0} items'}"/>
```

### 何时使用 StringFormat vs Converter

| 场景 | 推荐方式 |
|------|---------|
| 简单数字格式化 | `StringFormat` |
| 简单日期格式化 | `StringFormat` |
| 复杂逻辑转换 | `IValueConverter` |
| 布尔转可见性 | `IValueConverter` |
| 条件样式 | `IValueConverter` |

---

## Deep Dive：转换器的性能考虑

### 转换器是单例

```csharp
// 好：使用单例
public class MyConverter : IValueConverter
{
    public static readonly MyConverter Instance = new();
}

// 不好：每次使用都创建新实例
<StackPanel.Resources>
    <local:MyConverter x:Key="MyConverter"/>
</StackPanel.Resources>
```

### 转换器的缓存

如果转换器的计算开销较大，可以考虑缓存结果：

```csharp
public class ExpensiveConverter : IValueConverter
{
    private readonly Dictionary<object, object?> _cache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (_cache.TryGetValue(value!, out var cached))
            return cached;

        var result = ExpensiveCalculation(value);
        _cache[value!] = result;
        return result;
    }
}
```

## Cross References

- [第 5 章 数据绑定](05-data-binding.md) — 绑定语法和 StringFormat
- [第 7 章 样式与主题系统](07-styling-theming.md) — 条件样式
- [第 15 章 编译绑定与 AOT 发布](15-compiled-bindings.md) — 转换器与 AOT 兼容性

## Common Pitfalls

1. **忘记处理 null 值**: 转换器的输入可能是 null
2. **ConvertBack 未实现**: 如果只用于单向绑定，可以抛出 `NotSupportedException`
3. **转换器返回错误类型**: 确保返回值与目标属性类型匹配

## Try It Yourself

1. 创建一个 `BoolToVisibilityConverter`，支持反转参数
2. 实现一个 `MultiValueConverter`，检查多个条件
3. 在 CodexSwitch 中找到使用 `StringFormat` 的地方，考虑是否可以改用转换器
