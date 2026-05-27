# 19. 值转换器

> **写给零基础的你**：值转换器就像"翻译官"。程序里的数据和界面上显示的格式经常不一样。比如程序里存的是 `true/false`，但界面上要显示"是/否"；程序里存的是数字 `1234567`，但界面上要显示 "1.2M"。值转换器就是做这种"翻译"工作的。

## 19.1 概述

值转换器（Value Converter）是数据绑定系统中的重要组件，它充当 ViewModel 数据与 UI 显示之间的"翻译官"。当 ViewModel 中的数据格式不能直接用于 UI 显示，或者 UI 输入不能直接存储到 ViewModel 时，就需要值转换器来完成格式转换。

典型应用场景：
- **布尔转可见性**：`true` -> `Visible`，`false` -> `Collapsed`
- **枚举转布尔**：选中的枚举值 -> `true`，其他 -> `false`
- **数字格式化**：`1234567` -> `1.2M`
- **状态转颜色**：`"Active"` -> 绿色，`"Error"` -> 红色
- **空值处理**：`null` -> `false`，非空 -> `true`

在 CodexSwitch 中，值转换器被用于：
- Provider 状态的颜色映射
- 使用量数据的格式化显示
- 条件样式的激活

## 19.2 IValueConverter 接口

### 19.2.1 接口定义

> **小白提示**：Convert 和 ConvertBack 就像翻译的两个方向。Convert 是"英译中"（数据 → 界面），ConvertBack 是"中译英"（界面 → 数据）。如果你只用单向绑定，只需要实现 Convert；如果用双向绑定，两个都要实现。

```csharp
public interface IValueConverter
{
    // 数据 -> UI（单向绑定或双向绑定的正向转换）
    // 比如：true -> "可见"，42 -> "42 条记录"
    object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    // UI -> 数据（双向绑定的反向转换）
    // 比如："可见" -> true，"42 条记录" -> 42
    object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
}
```

### 19.2.2 参数说明

| 参数 | 说明 | 示例 |
|------|------|------|
| `value` | 绑定源的值 | `true`, `42`, `"Active"` |
| `targetType` | 目标属性的类型 | `typeof(bool)`, `typeof(IBrush)` |
| `parameter` | `ConverterParameter` 的值 | `"reverse"`, `"F2"` |
| `culture` | 当前文化信息 | `CultureInfo.InvariantCulture` |

### 19.2.3 最简单的转换器

```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Avalonia 的 IsVisible 是 bool 类型，不是 WPF 的 Visibility 枚举
        return value is true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true;
    }
}
```

### 19.2.4 单例模式

转换器应该是无状态的，推荐使用单例模式：

```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    // 单例实例
    public static readonly BoolToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Avalonia 的 IsVisible 是 bool 类型，不是 WPF 的 Visibility 枚举
        return value is true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true;
    }
}
```

在 XAML 中使用：

```xml
<!-- 方式一：x:Static 引用单例 -->
<StackPanel IsVisible="{Binding IsExpanded,
    Converter={x:Static local:BoolToVisibilityConverter.Instance}}"/>

<!-- 方式二：作为静态资源（需要在 Resources 中定义） -->
<StackPanel.Resources>
    <local:BoolToVisibilityConverter x:Key="BoolToVisibility"/>
</StackPanel.Resources>
<StackPanel IsVisible="{Binding IsExpanded,
    Converter={StaticResource BoolToVisibility}}"/>
```

## 19.3 常用转换器模式

### 19.3.1 BoolToVisibilityConverter

```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolValue = value is true;

        // 支持反转参数
        if (parameter?.ToString() == "reverse")
            boolValue = !boolValue;

        // Avalonia 的 IsVisible 是 bool 类型，不是 WPF 的 Visibility 枚举
        return boolValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Avalonia 的 IsVisible 是 bool 类型
        bool isVisible = value is true;

        if (parameter?.ToString() == "reverse")
            isVisible = !isVisible;

        return isVisible;
    }
}
```

```xml
<!-- 正常使用 -->
<StackPanel IsVisible="{Binding IsExpanded,
    Converter={x:Static local:BoolToVisibilityConverter.Instance}}"/>

<!-- 反转：当 IsExpanded 为 false 时显示 -->
<StackPanel IsVisible="{Binding IsExpanded,
    Converter={x:Static local:BoolToVisibilityConverter.Instance},
    ConverterParameter=reverse}"/>

<!-- 注意：Avalonia 中 IsVisible 绑定 bool 时自动转换 -->
<!-- 所以简单的 bool 绑定不需要转换器 -->
<StackPanel IsVisible="{Binding IsExpanded}"/>
<!-- 这里只是演示转换器模式，实际中直接绑定即可 -->
```

### 19.3.2 BoolToOppositeConverter

```csharp
public class BoolToOppositeConverter : IValueConverter
{
    public static readonly BoolToOppositeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not true;
    }
}
```

```xml
<!-- 当 IsReadOnly 为 false 时启用 -->
<TextBox IsReadOnly="{Binding IsReadOnly}"
         IsEnabled="{Binding IsReadOnly,
             Converter={x:Static local:BoolToOppositeConverter.Instance}}"/>
```

### 19.3.3 EnumToBoolConverter

```csharp
public class EnumToBoolConverter : IValueConverter
{
    public static readonly EnumToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
            return false;

        return value.ToString() == parameter.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is not null)
        {
            // 将字符串参数转回枚举值
            return Enum.Parse(targetType, parameter.ToString()!);
        }

        return AvaloniaProperty.UnsetValue;
    }
}
```

```xml
<!-- 单选按钮组绑定到枚举 -->
<RadioButton Content="OpenAI Responses"
             IsChecked="{Binding SelectedProtocol,
                 Converter={x:Static local:EnumToBoolConverter.Instance},
                 ConverterParameter=OpenAiResponses}"/>
<RadioButton Content="OpenAI Chat"
             IsChecked="{Binding SelectedProtocol,
                 Converter={x:Static local:EnumToBoolConverter.Instance},
                 ConverterParameter=OpenAiChat}"/>
<RadioButton Content="Anthropic Messages"
             IsChecked="{Binding SelectedProtocol,
                 Converter={x:Static local:EnumToBoolConverter.Instance},
                 ConverterParameter=AnthropicMessages}"/>
```

### 19.3.4 StringToColorConverter / StatusToBrushConverter

```csharp
public class StatusToBrushConverter : IValueConverter
{
    public static readonly StatusToBrushConverter Instance = new();

    private static readonly IBrush SuccessBrush = new SolidColorBrush(Color.Parse("#36D399"));
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#FF6467"));
    private static readonly IBrush WarningBrush = new SolidColorBrush(Color.Parse("#FACC15"));
    private static readonly IBrush InfoBrush = new SolidColorBrush(Color.Parse("#60A5FA"));
    private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.Parse("#A3A3A3"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString()?.ToLowerInvariant() switch
        {
            "active" or "success" or "running" => SuccessBrush,
            "error" or "failed" => ErrorBrush,
            "warning" or "pending" => WarningBrush,
            "info" or "loading" => InfoBrush,
            _ => DefaultBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
```

```xml
<Border Background="{Binding Status,
    Converter={x:Static local:StatusToBrushConverter.Instance}}"
    CornerRadius="4" Padding="8,4">
    <TextBlock Text="{Binding Status}"/>
</Border>
```

### 19.3.5 NullToBoolConverter

```csharp
public class NullToBoolConverter : IValueConverter
{
    public static readonly NullToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNull = value is null;

        // 支持反转：null -> true, 非null -> false
        if (parameter?.ToString() == "reverse")
            return isNull;

        return !isNull;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
```

```xml
<!-- 当 SelectedItem 不为 null 时显示详情面板 -->
<Border IsVisible="{Binding SelectedItem,
    Converter={x:Static local:NullToBoolConverter.Instance}}">
    <!-- 详情内容 -->
</Border>

<!-- 当 SelectedItem 为 null 时显示占位符 -->
<TextBlock Text="No item selected"
           IsVisible="{Binding SelectedItem,
               Converter={x:Static local:NullToBoolConverter.Instance},
               ConverterParameter=reverse}"/>
```

### 19.3.6 StringFormatConverter

```csharp
public class StringFormatConverter : IValueConverter
{
    public static readonly StringFormatConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is string format)
        {
            return string.Format(culture, format, value);
        }
        return value?.ToString() ?? "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
```

```xml
<TextBlock Text="{Binding TokenCount,
    Converter={x:Static local:StringFormatConverter.Instance},
    ConverterParameter='{}{0:N0} tokens'}"/>
```

### 19.3.7 数字格式化转换器

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
                >= 1_000_000_000 => $"{count / 1_000_000_000.0:F1}B",
                >= 1_000_000 => $"{count / 1_000_000.0:F1}M",
                >= 1_000 => $"{count / 1_000.0:F1}K",
                _ => count.ToString("N0")
            };
        }
        if (value is int intCount)
        {
            return Convert(intCount, targetType, parameter, culture);
        }
        return value?.ToString() ?? "0";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
```

```xml
<TextBlock Text="{Binding InputTokens,
    Converter={x:Static local:TokenCountConverter.Instance}}"/>
<!-- 1234567 -> "1.2M" -->
<!-- 5678 -> "5.7K" -->
<!-- 42 -> "42" -->
```

### 19.3.8 CostFormatterConverter

```csharp
public class CostFormatterConverter : IValueConverter
{
    public static readonly CostFormatterConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal cost)
        {
            return cost switch
            {
                >= 1000m => $"${cost:N0}",
                >= 1m => $"${cost:F2}",
                >= 0.01m => $"${cost:F4}",
                _ => $"${cost:F6}"
            };
        }
        return "$0.00";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
```

### 19.3.9 CollectionCountConverter

```csharp
public class CollectionCountConverter : IValueConverter
{
    public static readonly CollectionCountConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ICollection collection)
        {
            var count = collection.Count;
            var label = parameter?.ToString() ?? "items";
            return $"{count} {label}";
        }
        if (value is IEnumerable enumerable)
        {
            var count = enumerable.Cast<object>().Count();
            var label = parameter?.ToString() ?? "items";
            return $"{count} {label}";
        }
        return "0 items";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
```

```xml
<TextBlock Text="{Binding Providers,
    Converter={x:Static local:CollectionCountConverter.Instance},
    ConverterParameter=providers}"/>
<!-- "5 providers" -->
```

### 19.3.10 EqualityConverter

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
        if (value is true)
            return parameter?.ToString();
        return AvaloniaProperty.UnsetValue;
    }
}
```

```xml
<!-- 判断当前页面是否为 "home" -->
<Border Classes.active="{Binding CurrentPage,
    Converter={x:Static local:EqualityConverter.Instance},
    ConverterParameter=home}"/>
```

## 19.4 参数化转换器（ConverterParameter）

### 19.4.1 ConverterParameter 的使用方式

```xml
<!-- 静态字符串参数 -->
<TextBlock Text="{Binding Count,
    Converter={x:Static local:FormatConverter.Instance},
    ConverterParameter='{}{0:N0} items'}"/>

<!-- 参数控制行为 -->
<StackPanel IsVisible="{Binding IsActive,
    Converter={x:Static local:BoolToVisibilityConverter.Instance},
    ConverterParameter=reverse}"/>
```

### 19.4.2 多功能转换器

```csharp
public class MathConverter : IValueConverter
{
    public static readonly MathConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d && parameter is string op)
        {
            var parts = op.Split(' ');
            if (parts.Length == 2 && double.TryParse(parts[1], out var operand))
            {
                return parts[0] switch
                {
                    "+" => d + operand,
                    "-" => d - operand,
                    "*" => d * operand,
                    "/" => operand != 0 ? d / operand : double.NaN,
                    "%" => d % operand,
                    _ => d
                };
            }
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d && parameter is string op)
        {
            var parts = op.Split(' ');
            if (parts.Length == 2 && double.TryParse(parts[1], out var operand))
            {
                return parts[0] switch
                {
                    "+" => d - operand,
                    "-" => d + operand,
                    "*" => operand != 0 ? d / operand : double.NaN,
                    "/" => d * operand,
                    _ => d
                };
            }
        }
        return value;
    }
}
```

```xml
<!-- 价格加上 10% 税 -->
<TextBlock Text="{Binding Price,
    Converter={x:Static local:MathConverter.Instance},
    ConverterParameter='* 1.1',
    StringFormat='{}${0:F2}'}"/>

<!-- 值减半 -->
<Rectangle Width="{Binding BarWidth,
    Converter={x:Static local:MathConverter.Instance},
    ConverterParameter='/ 2'}"/>
```

## 19.5 IMultiValueConverter

### 19.5.1 接口定义

```csharp
public interface IMultiValueConverter
{
    object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture);
}
```

多值转换器接收多个绑定值，适合需要组合多个数据源的场景。

### 19.5.2 AllTrueConverter

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
<Button Content="Save" IsEnabled="{Binding}">
    <Button.IsEnabled>
        <MultiBinding Converter="{x:Static local:AllTrueConverter.Instance}">
            <Binding Path="IsNameValid"/>
            <Binding Path="IsEmailValid"/>
            <Binding Path="IsTermsAccepted"/>
        </MultiBinding>
    </Button.IsEnabled>
</Button>
```

### 19.5.3 AnyTrueConverter

```csharp
public class AnyTrueConverter : IMultiValueConverter
{
    public static readonly AnyTrueConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        return values.Any(v => v is true);
    }
}
```

```xml
<!-- 任何一个筛选条件激活时显示"清除筛选"按钮 -->
<Button Content="Clear Filters" IsVisible="{Binding}">
    <Button.IsEnabled>
        <MultiBinding Converter="{x:Static local:AnyTrueConverter.Instance}">
            <Binding Path="HasNameFilter"/>
            <Binding Path="HasStatusFilter"/>
            <Binding Path="HasDateFilter"/>
        </MultiBinding>
    </Button.IsEnabled>
</Button>
```

### 19.5.4 StringJoinConverter

```csharp
public class StringJoinConverter : IMultiValueConverter
{
    public static readonly StringJoinConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var separator = parameter?.ToString() ?? ", ";
        var strings = values
            .Where(v => v is not null)
            .Select(v => v!.ToString())
            .Where(s => !string.IsNullOrEmpty(s));
        return string.Join(separator, strings);
    }
}
```

```xml
<TextBlock>
    <TextBlock.Text>
        <MultiBinding Converter="{x:Static local:StringJoinConverter.Instance}"
                      ConverterParameter=" - ">
            <Binding Path="ProviderName"/>
            <Binding Path="ModelName"/>
            <Binding Path="Protocol"/>
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
<!-- "OpenAI - gpt-4 - Responses" -->
```

### 19.5.5 SumConverter

```csharp
public class SumConverter : IMultiValueConverter
{
    public static readonly SumConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        double sum = 0;
        foreach (var value in values)
        {
            if (value is double d)
                sum += d;
            else if (value is int i)
                sum += i;
            else if (value is long l)
                sum += l;
            else if (value is decimal dec)
                sum += (double)dec;
        }
        return sum;
    }
}
```

```xml
<Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto"/>
    <ColumnDefinition Width="*"/>
</Grid.ColumnDefinitions>
<!-- 两列宽度之和 -->
<Rectangle Grid.Column="1" Width="{Binding}">
    <Rectangle.Width>
        <MultiBinding Converter="{x:Static local:SumConverter.Instance}">
            <Binding Path="LeftColumnWidth"/>
            <Binding Path="RightColumnWidth"/>
        </MultiBinding>
    </Rectangle.Width>
</Rectangle>
```

## 19.6 FuncValueConverter（泛型版本）

### 19.6.1 使用 FuncValueConverter

Avalonia 提供了 `FuncValueConverter`，可以用 lambda 简单定义转换器：

```csharp
// 简单的布尔反转
public class InverseBoolConverter : FuncValueConverter<bool, bool>
{
    public InverseBoolConverter() : base(b => !b) { }
}

// 字符串转大写
public class ToUpperConverter : FuncValueConverter<string?, string>
{
    public ToUpperConverter() : base(s => s?.ToUpper() ?? "") { }
}

// 数字格式化
public class ThousandsConverter : FuncValueConverter<long, string>
{
    public ThousandsConverter() : base(n => n.ToString("N0")) { }
}
```

### 19.6.2 泛型 FuncValueConverter

```csharp
// 带输入和输出类型的泛型转换器
public class FuncValueConverter<TIn, TOut> : IValueConverter
{
    private readonly Func<TIn?, TOut> _convert;
    private readonly Func<TOut?, TIn>? _convertBack;

    public FuncValueConverter(Func<TIn?, TOut> convert, Func<TOut?, TIn>? convertBack = null)
    {
        _convert = convert;
        _convertBack = convertBack;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TIn input)
            return _convert(input);
        return default(TOut);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (_convertBack is null)
            throw new NotSupportedException();
        if (value is TOut output)
            return _convertBack(output);
        return default(TIn);
    }
}
```

### 19.6.3 内联 FuncValueConverter

```csharp
// 在 AXAML 中直接定义（Avalonia 内置）
<Window.Resources>
    <FuncValueConverter x:Key="BoolToOpacity"
        Func="{x:Static local:Converters.BoolToOpacity}"/>
</Window.Resources>

<TextBlock Opacity="{Binding IsEnabled,
    Converter={StaticResource BoolToOpacity}}"/>
```

```csharp
public static class Converters
{
    public static Func<bool, double> BoolToOpacity => b => b ? 1.0 : 0.5;
}
```

## 19.7 StringFormat 替代转换器

### 19.7.1 StringFormat 的使用

CodexSwitch 大量使用 `StringFormat` 替代简单的格式化转换器：

```xml
<!-- 数字格式化 -->
<TextBlock Text="{Binding InputTokens, StringFormat='{}{0:N0}'}"/>
<!-- 1234567 -> "1,234,567" -->

<!-- 货币格式化 -->
<TextBlock Text="{Binding EstimatedCost, StringFormat='{}${0:F4}'}"/>
<!-- 0.1234 -> "$0.1234" -->

<!-- 日期格式化 -->
<TextBlock Text="{Binding Timestamp, StringFormat='{}{0:yyyy-MM-dd HH:mm}'}"/>

<!-- 百分比 -->
<TextBlock Text="{Binding Progress, StringFormat='{}{0:P1}'}"/>
<!-- 0.856 -> "85.6%" -->

<!-- 复合格式化 -->
<TextBlock Text="{Binding Count, StringFormat='Total: {0:N0} items'}"/>

<!-- 多个值的复合格式化 -->
<TextBlock>
    <TextBlock.Text>
        <MultiBinding StringFormat="{}{0} of {1} items">
            <Binding Path="SelectedCount"/>
            <Binding Path="TotalCount"/>
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
```

### 19.7.2 StringFormat vs Converter 选择指南

| 场景 | 推荐方式 | 原因 |
|------|---------|------|
| 简单数字格式化 | `StringFormat` | 内置，无需额外代码 |
| 简单日期格式化 | `StringFormat` | 内置，灵活 |
| 复合格式化 | `StringFormat` | 多值绑定支持 |
| 布尔转可见性 | 直接绑定 | Avalonia 自动转换 |
| 复杂逻辑转换 | `IValueConverter` | 逻辑无法用格式表达 |
| 条件样式 | `IValueConverter` | 需要条件判断 |
| 空值处理 | `IValueConverter` | 需要逻辑判断 |
| 枚举转换 | `IValueConverter` | 需要类型转换 |
| 多值组合 | `IMultiValueConverter` | 需要多个输入 |

### 19.7.3 StringFormat 的局限性

```csharp
// StringFormat 不能做的：
// 1. 条件逻辑（if/else）
// 2. 类型转换（bool -> Visibility）
// 3. 空值处理（null -> "N/A"）
// 4. 枚举映射（enum -> string）
// 5. 多值计算（A + B）

// 这些场景需要使用 IValueConverter
```

## 19.8 转换器的注册和复用

### 19.8.1 静态资源注册

```xml
<!-- App.axaml 全局注册 -->
<Application.Resources>
    <local:BoolToVisibilityConverter x:Key="BoolToVisibility"/>
    <local:StatusToBrushConverter x:Key="StatusToBrush"/>
    <local:TokenCountConverter x:Key="TokenCount"/>
    <local:NullToBoolConverter x:Key="NullToBool"/>
</Application.Resources>

<!-- 使用 -->
<TextBlock Text="{Binding Tokens, Converter={StaticResource TokenCount}}"/>
```

### 19.8.2 单例静态字段

```csharp
// 推荐：使用静态单例
public class MyConverter : IValueConverter
{
    public static readonly MyConverter Instance = new();

    // ...
}

// XAML 中使用
<TextBlock Text="{Binding Value,
    Converter={x:Static local:MyConverter.Instance}}"/>
```

### 19.8.3 转换器工厂

对于需要参数化的场景：

```csharp
public class ConverterFactory
{
    public static IValueConverter CreateConditional(Func<object?, bool> predicate)
    {
        return new ConditionalConverter(predicate);
    }

    private class ConditionalConverter : IValueConverter
    {
        private readonly Func<object?, bool> _predicate;

        public ConditionalConverter(Func<object?, bool> predicate)
        {
            _predicate = predicate;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return _predicate(value);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
```

## 19.9 转换器链

### 19.9.1 链式转换

Avalonia 不直接支持转换器链，但可以通过中间属性或 MultiBinding 实现：

```csharp
// 方式一：组合转换器
public class ChainConverter : IValueConverter
{
    private readonly IValueConverter[] _converters;

    public ChainConverter(params IValueConverter[] converters)
    {
        _converters = converters;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = value;
        foreach (var converter in _converters)
        {
            result = converter.Convert(result, targetType, parameter, culture);
        }
        return result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = value;
        for (int i = _converters.Length - 1; i >= 0; i--)
        {
            result = _converters[i].ConvertBack(result, targetType, parameter, culture);
        }
        return result;
    }
}
```

```xml
<!-- 方式二：使用中间 ViewModel 属性 -->
<!-- 在 ViewModel 中计算中间值，避免在 XAML 中做复杂转换 -->
```

## 19.10 最佳实践

### 转换器设计原则

1. **保持无状态**：转换器不应该是有状态的，确保线程安全
2. **使用单例**：无状态转换器应该使用单例模式
3. **处理 null**：始终检查输入为 null 的情况
4. **抛出 ConvertBack 异常**：单向绑定的转换器在 ConvertBack 中抛出 `NotSupportedException`
5. **返回正确类型**：确保返回值与目标属性类型匹配

### 何时使用转换器

```
需要格式化？
├── 简单格式化 → StringFormat
├── 条件逻辑 → IValueConverter
├── 多值组合 → IMultiValueConverter
└── 简单数学 → MathConverter 或 ViewModel 计算属性
```

### 何时将逻辑放在 ViewModel

```csharp
// 如果转换逻辑只用于一个地方，考虑放在 ViewModel 中
[ObservableProperty]
private long _tokenCount;

// 直接在 ViewModel 中提供格式化后的值
public string TokenCountDisplay => TokenCount switch
{
    >= 1_000_000 => $"{TokenCount / 1_000_000.0:F1}M",
    >= 1_000 => $"{TokenCount / 1_000.0:F1}K",
    _ => TokenCount.ToString("N0")
};

// 如果转换逻辑需要复用，使用 IValueConverter
```

## 19.11 CodexSwitch 实战

### 19.11.1 CodexSwitch 中的 StringFormat 使用

```xml
<!-- CodexSwitch 大量使用 StringFormat -->
<TextBlock Text="{Binding RequestCount, StringFormat='{}{0:N0}'}"/>
<TextBlock Text="{Binding EstimatedCost, StringFormat='{}${0:F4}'}"/>

<!-- 多值绑定的 StringFormat -->
<TextBlock>
    <TextBlock.Text>
        <MultiBinding StringFormat="{}{0} / {1}">
            <Binding Path="UsedTokens"/>
            <Binding Path="TotalTokens"/>
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
```

### 19.11.2 CodexSwitch 中的条件样式

CodexSwitch 使用 Classes 而非转换器来实现条件样式：

```xml
<!-- 使用 Classes 实现条件样式 -->
<Border Classes="provider-usage"
        Classes.success="{Binding IsUsageValid}"
        Classes.error="{Binding IsUsageError}"
        Classes.refreshing="{Binding IsUsageRefreshing}"/>
```

```xml
<!-- 对应的样式定义 -->
<Style Selector="Border.provider-usage.success">
    <Setter Property="BorderBrush" Value="{DynamicResource CodexSwitch.SuccessBrush}"/>
</Style>
<Style Selector="Border.provider-usage.error">
    <Setter Property="BorderBrush" Value="{DynamicResource CodexSwitch.DestructiveBrush}"/>
</Style>
<Style Selector="Border.provider-usage.refreshing">
    <Setter Property="BorderBrush" Value="{DynamicResource CodexSwitch.PrimaryBrush}"/>
</Style>
```

这种模式比转换器更灵活，因为：
- 样式可以组合（同时有多个 class）
- 样式可以使用伪类（`:pointerover` 等）
- 样式支持过渡动画

## 19.12 Deep Dive：转换器的性能

### 19.12.1 转换器调用频率

转换器在以下情况被调用：
1. 绑定首次建立时
2. 绑定源值变化时
3. `ConverterParameter` 变化时（注意：ConverterParameter 不支持绑定）

在列表中，每个列表项的每次值变化都会调用转换器。如果列表有 1000 项，转换器可能被调用 1000 次。

### 19.12.2 性能优化

```csharp
// 不好：每次都创建新的 SolidColorBrush
public object? Convert(object? value, ...)
{
    return new SolidColorBrush(Color.Parse("#36D399")); // 每次调用都创建新对象
}

// 好：使用静态缓存的 Brush
private static readonly IBrush CachedBrush = new SolidColorBrush(Color.Parse("#36D399"));

public object? Convert(object? value, ...)
{
    return CachedBrush; // 复用同一个对象
}
```

### 19.12.3 转换器缓存

```csharp
public class CachedConverter : IValueConverter
{
    private readonly ConcurrentDictionary<object, object?> _cache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return null;

        return _cache.GetOrAdd(value, v => ExpensiveConversion(v));
    }

    private object? ExpensiveConversion(object value)
    {
        // 耗时的转换逻辑
        return value.ToString()?.ToUpper();
    }

    // ...
}
```

### 19.12.4 编译绑定与转换器

在编译绑定（`x:DataType`）模式下，转换器的类型检查在编译时完成：

```xml
<!-- 编译绑定会检查转换器的输入输出类型 -->
<TextBlock Text="{Binding TokenCount,
    Converter={x:Static local:TokenCountConverter.Instance}}"
    x:DataType="vm:MainWindowViewModel"/>
```

如果 `TokenCount` 是 `long` 类型，而转换器期望 `int`，编译时会发出警告。

## 19.13 Cross References

- [第 5 章 数据绑定](05-data-binding.md) -- 绑定语法和 StringFormat
- [第 7 章 样式与主题系统](07-styling-theming.md) -- 条件样式（Classes 模式）
- [第 15 章 编译绑定与 AOT 发布](15-compiled-bindings.md) -- 转换器与 AOT 兼容性
- [第 18 章 命令系统](18-commands.md) -- 命令参数的类型转换

## 19.14 Common Pitfalls

1. **忘记处理 null 值**：转换器的输入可能是 null，特别是当绑定源的属性未初始化时。始终检查 `value is null` 的情况。

2. **ConvertBack 未实现**：如果只用于单向绑定（OneWay），可以在 ConvertBack 中抛出 `NotSupportedException`。但如果用于双向绑定（TwoWay），必须实现 ConvertBack。

3. **转换器返回错误类型**：确保返回值与目标属性类型匹配。例如，`IsVisible` 期望 `bool` 或 `Visibility`，如果返回 `string` 会出错。

4. **ConverterParameter 不支持绑定**：`ConverterParameter` 是一个普通属性，不支持数据绑定。如果需要动态参数，考虑使用多值转换器或在 ViewModel 中计算。

5. **每次调用创建新对象**：如 `new SolidColorBrush()` 在转换器中被频繁调用，会增加 GC 压力。使用静态缓存的 Brush。

6. **在转换器中做耗时操作**：转换器在 UI 线程上执行，频繁调用时耗时操作会导致卡顿。考虑缓存结果或将逻辑移到 ViewModel。

7. **混淆 StringFormat 和 Converter**：简单的格式化用 StringFormat，复杂逻辑用 Converter。不要为了格式化 `"{0:N0}"` 而创建一个转换器。

8. **转换器中的类型检查不完整**：`value` 可能是 `AvaloniaProperty.UnsetValue` 或 `BindingOperations.DisconnectedItem`，而不仅仅是预期的类型。使用 `is` 模式匹配确保类型安全。

9. **忘记 x:Static 语法**：在 XAML 中引用单例转换器必须使用 `{x:Static local:MyConverter.Instance}`，使用 `{StaticResource}` 需要先在 Resources 中定义。

10. **Classes 模式 vs 转换器的选择**：CodexSwitch 展示了使用 `Classes` 绑定来实现条件样式，这比转换器更灵活且支持过渡动画。在条件样式场景下，优先考虑 Classes 模式。

## 19.15 Try It Yourself

1. **基础练习**：实现一个 `BoolToVisibilityConverter`，支持 `ConverterParameter=reverse` 来反转逻辑。

2. **格式化练习**：实现一个 `FileSizeConverter`，将字节数转换为人类可读的格式（如 `1.5 GB`、`234 MB`、`12 KB`）。

3. **多值转换器练习**：实现一个 `RangeCheckConverter`（IMultiValueConverter），检查一个值是否在最小值和最大值之间。

4. **枚举转换练习**：实现一个 `EnumToStringConverter`，将枚举值转换为友好的显示文本（如 `ProviderProtocol.OpenAiResponses` -> `"OpenAI Responses"`）。

5. **参数化转换器练习**：实现一个 `MathConverter`，支持 `+`、`-`、`*`、`/` 四种运算，通过 `ConverterParameter` 指定运算和操作数。

6. **缓存练习**：为一个耗时的转换器添加缓存机制，使用 `ConcurrentDictionary` 确保线程安全。

7. **Classes 模式练习**：不使用转换器，改用 `Classes.xxx="{Binding ...}"` 模式实现条件样式，对比两种方式的优劣。

8. **综合练习**：在 CodexSwitch 中找到所有使用 `StringFormat` 的地方，分析为什么选择 StringFormat 而非转换器。然后找到使用 `Classes` 绑定的地方，研究它如何替代转换器实现条件样式。
