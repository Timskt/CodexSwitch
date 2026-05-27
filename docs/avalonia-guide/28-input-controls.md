# 28. 高级输入控件

> **写给零基础的你**：第 16 章讲了键盘和鼠标事件，本章讲"高级输入控件"——就是那些比普通文本框更聪明的输入工具。比如日期选择器（不用手打日期，点一下就选好了）、数字输入框（只能输数字，还有上下箭头调大小）、滑块（拖一下就能选值）。这些控件让用户的输入更方便、更不容易出错。

## 28.1 概述

除了基础的 TextBox 和 CheckBox，Avalonia 还提供了多种高级输入控件，用于特定类型的数据输入。这些控件内置了数据验证、格式化和用户友好的交互方式。正确使用这些控件可以大大提升用户体验和数据质量。

学完本章后，你将能够：
- 掌握 NumericUpDown 数字输入控件
- 掌握 DatePicker 和 TimePicker 日期时间选择
- 掌握 AutoCompleteBox 自动补全输入
- 掌握 Slider 和 ProgressBar 控件
- 理解输入验证的最佳实践（INotifyDataErrorInfo）

## 28.2 核心概念

### 28.2.1 NumericUpDown

NumericUpDown 是一个数字输入框，带有上下箭头按钮，限制输入为数字。它比 TextBox 更适合数字输入，因为它自动处理验证和格式化。

**什么是 NumericUpDown？**
想象一个音量控制器：有一个数字显示框，旁边有上下箭头。你可以在框里直接输入数字，也可以点箭头增减。而且它会自动限制你只能输入数字，不能输入字母。

#### NumericUpDown 的所有属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Value` | decimal? | null | 当前值（可以为 null） |
| `Minimum` | decimal | 0 | 最小值 |
| `Maximum` | decimal | 100 | 最大值 |
| `Increment` | decimal | 1 | 步进值（点击箭头时增减的量） |
| `FormatString` | string | null | 显示格式（如 "N0", "F2"） |
| `Watermark` | string | null | 占位文本 |
| `IsReadOnly` | bool | false | 是否只读 |
| `AllowSpin` | bool | true | 是否允许旋转（箭头按钮） |
| `ShowButtonSpinner` | bool | true | 是否显示箭头按钮 |
| `NumberStyle` | NumberStyles | Number | 数字样式（允许哪些字符） |
| `CultureInfo` | CultureInfo | null | 区域信息（影响小数点符号等） |
| `ClipValueToMinMax` | bool | false | 是否将值限制在 Min/Max 范围内 |

#### NumericUpDown 的事件

| 事件 | 说明 |
|------|------|
| `ValueChanged` | 值改变时触发 |

#### FormatString 格式字符串

| 格式 | 说明 | 示例输入 | 显示结果 |
|------|------|---------|---------|
| `N0` | 数字，0位小数，千分位 | 12345 | 12,345 |
| `N2` | 数字，2位小数 | 12345 | 12,345.00 |
| `F0` | 定点，0位小数 | 12345 | 12345 |
| `F2` | 定点，2位小数 | 12345 | 12345.00 |
| `P0` | 百分比，0位小数 | 0.75 | 75% |
| `P2` | 百分比，2位小数 | 0.75 | 75.00% |
| `C2` | 货币，2位小数 | 12345 | ¥12,345.00 |
| `0'%'` | 自定义百分比 | 75 | 75% |

#### 示例 1：基本 NumericUpDown

```xml
<NumericUpDown Value="{Binding Quantity, Mode=TwoWay}"
               Minimum="0"
               Maximum="100"
               Increment="1"
               FormatString="N0"/>
```

#### 示例 2：小数输入

```xml
<NumericUpDown Value="{Binding Price, Mode=TwoWay}"
               Minimum="0"
               Maximum="9999.99"
               Increment="0.01"
               FormatString="F2"
               Watermark="输入价格"/>
```

#### 示例 3：百分比输入

```xml
<NumericUpDown Value="{Binding Percentage, Mode=TwoWay}"
               Minimum="0"
               Maximum="100"
               Increment="5"
               FormatString="0'%'"
               Watermark="输入百分比"/>
```

#### 示例 4：只显示旋转按钮（隐藏文本框）

```xml
<NumericUpDown Value="{Binding FontSize, Mode=TwoWay}"
               Minimum="8"
               Maximum="72"
               Increment="1"
               FormatString="N0"
               ShowButtonSpinner="True"/>
```

#### 示例 5：只读模式

```xml
<NumericUpDown Value="{Binding CalculatedResult}"
               IsReadOnly="True"
               FormatString="F2"
               ShowButtonSpinner="False"/>
```

#### 示例 6：自定义数字样式

```xml
<!-- 允许输入十六进制数字 -->
<NumericUpDown Value="{Binding HexValue, Mode=TwoWay}"
               NumberStyle="HexNumber"
               FormatString="X"/>
```

### 28.2.2 DatePicker

DatePicker 是一个日期选择控件，提供日历弹出界面，用户可以直接点击选择日期，不需要手动输入。

**什么是 DatePicker？**
想象你在填一个表单，需要选择出生日期。如果用 TextBox，你可能要输入 "2024-01-15"，格式不对就报错。用 DatePicker 就简单多了：点击一下弹出日历，点选日期就好了。

#### DatePicker 的所有属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `SelectedDate` | DateTimeOffset? | 选中的日期 |
| `MinYear` | DateTimeOffset | 最早可选日期 |
| `MaxYear` | DateTimeOffset | 最晚可选日期 |
| `Watermark` | string | 占位文本 |
| `CustomDateFormatString` | string | 自定义日期格式 |
| `DateFormat` | string | 日期格式 |
| `DayFormat` | string | 日格式 |
| `MonthFormat` | string | 月格式 |
| `YearFormat` | string | 年格式 |
| `IsDropDownOpen` | bool | 日历是否打开 |

#### DatePicker 的事件

| 事件 | 说明 |
|------|------|
| `SelectedDateChanged` | 选中日期改变时触发 |
| `CalendarOpened` | 日历打开时触发 |
| `CalendarClosed` | 日历关闭时触发 |

#### 示例 1：基本 DatePicker

```xml
<DatePicker SelectedDate="{Binding StartDate, Mode=TwoWay}"
            Watermark="选择开始日期"/>
```

#### 示例 2：限制日期范围

```xml
<DatePicker SelectedDate="{Binding SelectedDate, Mode=TwoWay}"
            MinYear="2020-01-01"
            MaxYear="2030-12-31"
            Watermark="选择日期"/>
```

#### 示例 3：自定义显示格式

```xml
<!-- 使用斜杠分隔 -->
<DatePicker SelectedDate="{Binding SelectedDate, Mode=TwoWay}"
            CustomDateFormatString="yyyy/MM/dd"/>

<!-- 使用中文格式 -->
<DatePicker SelectedDate="{Binding SelectedDate, Mode=TwoWay}"
            CustomDateFormatString="yyyy年MM月dd日"/>

<!-- 短日期 -->
<DatePicker SelectedDate="{Binding SelectedDate, Mode=TwoWay}"
            CustomDateFormatString="MM/dd"/>
```

#### 示例 4：只读 DatePicker

```xml
<DatePicker SelectedDate="{Binding CreatedAt}"
            IsEnabled="False"
            Watermark="创建日期"/>
```

#### 示例 5：双向绑定到 DateTime

```csharp
// ViewModel 中处理 DateTimeOffset 和 DateTime 的转换
private DateTime? _startDate;
public DateTime? StartDate
{
    get => _startDate;
    set
    {
        SetProperty(ref _startDate, value);
        // 如果需要 DateTimeOffset
        StartDateOffset = value.HasValue
            ? new DateTimeOffset(value.Value)
            : null;
    }
}

public DateTimeOffset? StartDateOffset { get; set; }
```

### 28.2.3 TimePicker

TimePicker 是一个时间选择控件，提供时钟弹出界面。

**什么是 TimePicker？**
TimePicker 让用户选择一个时间（时:分），不需要手动输入。它可以设置为 12 小时制或 24 小时制。

#### TimePicker 的所有属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `SelectedTime` | TimeSpan? | null | 选中的时间 |
| `ClockIdentifier` | string | "12HourClock" | 时钟标识（"12HourClock" 或 "24HourClock"） |
| `MinuteIncrement` | int | 1 | 分钟增量 |
| `Watermark` | string | null | 占位文本 |
| `IsDropDownOpen` | bool | false | 时钟是否打开 |

#### TimePicker 的事件

| 事件 | 说明 |
|------|------|
| `SelectedTimeChanged` | 选中时间改变时触发 |

#### 示例 1：基本 TimePicker

```xml
<TimePicker SelectedTime="{Binding StartTime, Mode=TwoWay}"
            Watermark="选择开始时间"/>
```

#### 示例 2：24 小时制

```xml
<TimePicker SelectedTime="{Binding SelectedTime, Mode=TwoWay}"
            ClockIdentifier="24HourClock"
            Watermark="选择时间"/>
```

#### 示例 3：12 小时制

```xml
<TimePicker SelectedTime="{Binding SelectedTime, Mode=TwoWay}"
            ClockIdentifier="12HourClock"
            Watermark="选择时间"/>
```

#### 示例 4：分钟增量（只允许选择整刻钟）

```xml
<TimePicker SelectedTime="{Binding MeetingTime, Mode=TwoWay}"
            MinuteIncrement="15"
            Watermark="选择会议时间"/>
```

```xml
<!-- 半小时增量 -->
<TimePicker SelectedTime="{Binding ReminderTime, Mode=TwoWay}"
            MinuteIncrement="30"/>
```

#### 示例 5：TimePicker 与 TimeSpan 转换

```csharp
// 从 DateTime 获取 TimeSpan
var timeSpan = DateTime.Now.TimeOfDay;

// 从 TimeSpan 创建 DateTime
var dateTime = DateTime.Today.Add(timeSpan);

// 格式化显示
var timeString = timeSpan.ToString(@"hh\:mm"); // "14:30"
```

### 28.2.4 AutoCompleteBox

AutoCompleteBox 是一个带自动补全功能的输入框。用户输入时会自动显示匹配的建议列表。

**什么是 AutoCompleteBox？**
想象你在 Google 搜索框中输入文字，每输入一个字符，下面就会出现一些建议。AutoCompleteBox 就是这样的控件。

#### AutoCompleteBox 的所有属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ItemsSource` | IEnumerable | null | 建议列表数据源 |
| `Text` | string | null | 输入文本 |
| `SelectedItem` | object | null | 选中的项 |
| `MinimumPrefixLength` | int | 1 | 触发自动补全的最小字符数 |
| `MinimumPopulationDelay` | TimeSpan | 100ms | 输入后延迟多久开始过滤 |
| `FilterMode` | AutoCompleteFilterMode | StartsWith | 过滤模式 |
| `IsTextCompletionEnabled` | bool | false | 是否启用文本补全 |
| `IsDropDownOpen` | bool | false | 下拉是否打开 |
| `MaxDropDownHeight` | double | 200 | 下拉最大高度 |
| `ValueMemberBinding` | IBinding | null | 值成员绑定（从对象提取文本） |
| `ItemTemplate` | DataTemplate | null | 建议项模板 |
| `Watermark` | string | null | 占位文本 |
| `IsCompletionListSquared` | bool | false | 下拉列表是否为方形 |

#### AutoCompleteFilterMode 过滤模式

| 模式 | 说明 | 适用场景 |
|------|------|---------|
| `None` | 不过滤 | 自定义过滤逻辑 |
| `StartsWith` | 以输入文本开头 | 常规搜索 |
| `Contains` | 包含输入文本 | 模糊搜索 |
| `Equals` | 等于输入文本 | 精确匹配 |
| `Custom` | 自定义过滤逻辑 | 复杂过滤需求 |

#### AutoCompleteBox 的事件

| 事件 | 说明 |
|------|------|
| `TextChanged` | 文本改变时触发 |
| `SelectionChanged` | 选中项改变时触发 |
| `DropDownOpening` | 下拉正在打开时触发 |
| `DropDownOpened` | 下拉已打开时触发 |
| `Populating` | 正在填充建议时触发（用于异步加载） |
| `Populated` | 建议填充完成时触发 |

#### 示例 1：基本 AutoCompleteBox

```xml
<AutoCompleteBox ItemsSource="{Binding Suggestions}"
                 Text="{Binding SearchText, Mode=TwoWay}"
                 Watermark="搜索..."
                 MinimumPrefixLength="2"
                 FilterMode="Contains"/>
```

#### 示例 2：使用 ValueMemberBinding

```xml
<!-- 当 ItemsSource 是对象集合时，使用 ValueMemberBinding 提取显示文本 -->
<AutoCompleteBox ItemsSource="{Binding Providers}"
                 SelectedItem="{Binding SelectedProvider, Mode=TwoWay}"
                 ValueMemberBinding="{Binding Name}"
                 Watermark="选择 Provider">
    <AutoCompleteBox.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderModel">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <PathIcon Data="{Binding Icon}" Width="16" Height="16"/>
                <TextBlock Text="{Binding Name}"/>
                <TextBlock Text="{Binding Protocol}" Foreground="Gray"/>
            </StackPanel>
        </DataTemplate>
    </AutoCompleteBox.ItemTemplate>
</AutoCompleteBox>
```

#### 示例 3：文本补全模式

```xml
<!-- IsTextCompletionEnabled: 输入时自动补全到第一个匹配项 -->
<AutoCompleteBox ItemsSource="{Binding Countries}"
                 Text="{Binding SelectedCountry, Mode=TwoWay}"
                 IsTextCompletionEnabled="True"
                 MinimumPrefixLength="1"
                 FilterMode="StartsWith"/>
```

#### 示例 4：异步数据源

```xml
<AutoCompleteBox Text="{Binding QueryText, Mode=TwoWay}"
                 ItemsSource="{Binding AsyncResults}"
                 IsTextCompletionEnabled="True"
                 MinimumPrefixLength="3"
                 Populating="OnPopulating"/>
```

```csharp
private async void OnPopulating(object? sender, PopulatingEventArgs e)
{
    e.Cancel = true; // 取消默认的同步过滤

    var box = (AutoCompleteBox)sender!;
    var query = box.Text;

    // 异步加载数据
    var results = await _searchService.SearchAsync(query);

    // 更新数据源
    box.ItemsSource = results;
    box.PopulateComplete(); // 通知完成
}
```

#### 示例 5：自定义过滤逻辑

```csharp
// 在 ViewModel 中设置自定义过滤
AutoCompleteBox.FilterMode = AutoCompleteFilterMode.Custom;
AutoCompleteBox.ItemFilter = (searchText, item) =>
{
    var provider = (ProviderModel)item;
    return provider.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
        || provider.BaseUrl.Contains(searchText, StringComparison.OrdinalIgnoreCase);
};
```

### 28.2.5 Slider 详解

Slider 是一个滑块控件，用于在范围内选择值。它适合选择连续值，如音量、亮度、进度等。

**什么是 Slider？**
想象音量条：你拖动滑块，音量就跟着变化。Slider 就是这样的控件。

#### Slider 的所有属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Minimum` | double | 0 | 最小值 |
| `Maximum` | double | 10 | 最大值 |
| `Value` | double | 0 | 当前值 |
| `TickFrequency` | double | 0 | 刻度频率（0 表示无刻度） |
| `IsSnapToTickEnabled` | bool | false | 是否吸附到刻度 |
| `Ticks` | IList | null | 自定义刻度位置 |
| `Orientation` | Orientation | Horizontal | 方向（Horizontal/Vertical） |
| `IsDirectionReversed` | bool | false | 是否反转方向 |
| `IsMoveToPointEnabled` | bool | false | 点击刻度条是否直接跳到该位置 |
| `Interval` | double | 0 | 自动重复的间隔 |

#### Slider 的事件

| 事件 | 说明 |
|------|------|
| `ValueChanged` | 值改变时触发 |

#### 示例 1：基本 Slider

```xml
<Slider Minimum="0" Maximum="100"
        Value="{Binding Volume, Mode=TwoWay}"/>
```

#### 示例 2：带刻度的 Slider

```xml
<!-- TickFrequency: 均匀分布的刻度 -->
<Slider Minimum="0" Maximum="100"
        Value="{Binding Value, Mode=TwoWay}"
        TickFrequency="10"
        IsSnapToTickEnabled="True"/>
```

#### 示例 3：自定义刻度位置

```xml
<!-- Ticks: 指定具体刻度位置 -->
<Slider Minimum="0" Maximum="4"
        Value="{Binding Rating, Mode=TwoWay}"
        TickFrequency="1"
        IsSnapToTickEnabled="True"
        Ticks="0,1,2,3,4"/>
```

#### 示例 4：垂直 Slider

```xml
<Slider Minimum="0" Maximum="100"
        Value="{Binding Volume, Mode=TwoWay}"
        Orientation="Vertical"
        Height="200"/>
```

#### 示例 5：点击直接跳转

```xml
<!-- IsMoveToPointEnabled: 点击刻度条任意位置直接跳到该处 -->
<Slider Minimum="0" Maximum="100"
        Value="{Binding Progress, Mode=TwoWay}"
        IsMoveToPointEnabled="True"/>
```

#### 示例 6：带标签的 Slider

```xml
<StackPanel Spacing="4">
    <Grid ColumnDefinitions="*,Auto">
        <TextBlock Text="音量"/>
        <TextBlock Grid.Column="1"
                   Text="{Binding Volume, StringFormat='{}{0:F0}%'}"/>
    </Grid>
    <Slider Minimum="0" Maximum="100"
            Value="{Binding Volume, Mode=TwoWay}"/>
</StackPanel>
```

### 28.2.6 ProgressBar 详解

ProgressBar 显示操作的进度。它可以是确定的（显示具体进度）或不确定的（显示加载动画）。

#### ProgressBar 的所有属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Value` | double | 0 | 当前进度值 |
| `Minimum` | double | 0 | 最小值 |
| `Maximum` | double | 100 | 最大值 |
| `IsIndeterminate` | bool | false | 是否不确定（加载动画） |
| `Orientation` | Orientation | Horizontal | 方向 |
| `ProgressTextFormat` | string | null | 进度文本格式 |

#### 示例 1：确定进度

```xml
<ProgressBar Value="75" Maximum="100"/>
```

#### 示例 2：绑定到 ViewModel

```xml
<ProgressBar Value="{Binding Progress}" Maximum="100"
             Foreground="{StaticResource CsPrimaryBrush}"/>
```

#### 示例 3：不确定进度（加载动画）

```xml
<ProgressBar IsIndeterminate="True"/>
```

#### 示例 4：带标签的进度条

```xml
<StackPanel Spacing="4">
    <Grid ColumnDefinitions="*,Auto">
        <TextBlock Text="下载中..."/>
        <TextBlock Grid.Column="1"
                   Text="{Binding Progress, StringFormat='{}{0:F0}%'}"/>
    </Grid>
    <ProgressBar Value="{Binding Progress}" Maximum="100"/>
</StackPanel>
```

#### 示例 5：垂直进度条

```xml
<ProgressBar Value="{Binding Progress}" Maximum="100"
             Orientation="Vertical"
             Height="200"/>
```

#### 示例 6：带进度文本格式的 ProgressBar

```xml
<!-- ProgressTextFormat: 在进度条内显示文本 -->
<ProgressBar Value="{Binding Progress}" Maximum="100"
             ProgressTextFormat="{0:F0}%"/>
```

### 28.2.7 RangeBase 公共属性

Slider 和 ProgressBar 都继承自 RangeBase，共享以下属性：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Minimum` | double | 最小值 |
| `Maximum` | double | 最大值 |
| `Value` | double | 当前值 |
| `SmallChange` | double | 小步变化量（键盘方向键） |
| `LargeChange` | double | 大步变化量（Page Up/Down） |

## 28.3 进阶用法

### 28.3.1 输入验证（INotifyDataErrorInfo）

Avalonia 支持通过 `INotifyDataErrorInfo` 进行数据验证。这是一种标准的验证机制，允许 ViewModel 提供验证错误信息，UI 自动显示错误提示。

**什么是 INotifyDataErrorInfo？**
这是一个接口，让你的 ViewModel 能够告诉 UI "这个字段有错误"。比如：名字不能为空、邮箱格式不对、年龄必须在 0-150 之间。

#### 验证接口

```csharp
public interface INotifyDataErrorInfo
{
    bool HasErrors { get; }                                    // 是否有错误
    IEnumerable GetErrors(string? propertyName);               // 获取指定属性的错误
    event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged; // 错误改变事件
}
```

#### 示例 1：使用 CommunityToolkit 的验证

```csharp
// 安装 CommunityToolkit.Mvvm
// <PackageReference Include="CommunityToolkit.Mvvm" />

public partial class ValidatableViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "名称不能为空")]
    [MinLength(3, ErrorMessage = "名称至少 3 个字符")]
    [MaxLength(50, ErrorMessage = "名称最多 50 个字符")]
    private string _name = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    private string _email = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, 150, ErrorMessage = "年龄必须在 0-150 之间")]
    private int _age;
}
```

#### 示例 2：手动实现 INotifyDataErrorInfo

```csharp
public partial class ManualValidationViewModel : ObservableObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();

    [ObservableProperty]
    private string _name = "";

    partial void OnNameChanged(string value)
    {
        ClearErrors(nameof(Name));
        if (string.IsNullOrWhiteSpace(value))
            AddError(nameof(Name), "名称不能为空");
        else if (value.Length < 3)
            AddError(nameof(Name), "名称至少 3 个字符");
    }

    public bool HasErrors => _errors.Any();
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (propertyName != null && _errors.ContainsKey(propertyName))
            return _errors[propertyName];
        return Enumerable.Empty<string>();
    }

    private void AddError(string propertyName, string error)
    {
        if (!_errors.ContainsKey(propertyName))
            _errors[propertyName] = new List<string>();
        _errors[propertyName].Add(error);
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    private void ClearErrors(string propertyName)
    {
        if (_errors.Remove(propertyName))
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}
```

#### 示例 3：在 AXAML 中显示验证错误

```xml
<!-- Avalonia 会自动显示验证错误 -->
<TextBox Text="{Binding Name, Mode=TwoWay}"
         Watermark="输入名称"/>

<!-- 使用 DataErrors 附加属性自定义错误显示 -->
<StackPanel>
    <TextBox Text="{Binding Name, Mode=TwoWay}"
             Watermark="输入名称"/>
    <ItemsControl ItemsSource="{Binding (DataErrors.Errors)[Name]}">
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <TextBlock Text="{Binding}" Foreground="Red" FontSize="12"/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</StackPanel>
```

#### 示例 4：提交时验证所有字段

```csharp
[RelayCommand(CanExecute = nameof(CanSubmit))]
private async Task SubmitAsync()
{
    // 触发所有属性的验证
    ValidateAllProperties();

    if (HasErrors)
    {
        // 显示错误摘要
        var allErrors = GetErrors().Select(e => e.ErrorMessage);
        await ShowErrorDialogAsync(string.Join("\n", allErrors));
        return;
    }

    // 提交数据...
}

private bool CanSubmit() => !HasErrors;
```

### 28.3.2 键盘输入处理

#### 示例 1：TextBox 中处理回车键

```xml
<TextBox Text="{Binding SearchText, Mode=TwoWay}"
         KeyDown="OnSearchKeyDown"
         Watermark="按 Enter 搜索"/>
```

```csharp
private void OnSearchKeyDown(object? sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter)
    {
        SearchCommand.Execute(null);
        e.Handled = true;
    }
}
```

#### 示例 2：使用 KeyBinding 绑定快捷键

```xml
<Window.KeyBindings>
    <KeyBinding Gesture="Ctrl+F" Command="{Binding SearchCommand}"/>
    <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}"/>
    <KeyBinding Gesture="Ctrl+N" Command="{Binding NewItemCommand}"/>
    <KeyBinding Gesture="Escape" Command="{Binding CancelCommand}"/>
    <KeyBinding Gesture="Delete" Command="{Binding DeleteCommand}"/>
</Window.KeyBindings>
```

#### 示例 3：数字输入框的键盘限制

```csharp
// 只允许输入数字
private void OnNumericKeyDown(object? sender, KeyEventArgs e)
{
    // 允许数字键、退格、删除、方向键、小数点
    var allowedKeys = new[]
    {
        Key.D0, Key.D1, Key.D2, Key.D3, Key.D4,
        Key.D5, Key.D6, Key.D7, Key.D8, Key.D9,
        Key.Back, Key.Delete, Key.Left, Key.Right, Key.Decimal,
        Key.OemPeriod
    };

    if (!allowedKeys.Contains(e.Key))
        e.Handled = true;
}
```

### 28.3.3 自定义输入控件

#### 示例 1：颜色选择器（使用 Slider 组合）

```xml
<StackPanel Spacing="8">
    <TextBlock Text="颜色预览"/>
    <Border Width="100" Height="50"
            Background="{Binding SelectedColor}"/>
    <StackPanel Spacing="4">
        <TextBlock Text="{Binding Red, StringFormat='R: {0}'}"/>
        <Slider Minimum="0" Maximum="255"
                Value="{Binding Red, Mode=TwoWay}"
                TickFrequency="1" IsSnapToTickEnabled="True"/>
    </StackPanel>
    <StackPanel Spacing="4">
        <TextBlock Text="{Binding Green, StringFormat='G: {0}'}"/>
        <Slider Minimum="0" Maximum="255"
                Value="{Binding Green, Mode=TwoWay}"
                TickFrequency="1" IsSnapToTickEnabled="True"/>
    </StackPanel>
    <StackPanel Spacing="4">
        <TextBlock Text="{Binding Blue, StringFormat='B: {0}'}"/>
        <Slider Minimum="0" Maximum="255"
                Value="{Binding Blue, Mode=TwoWay}"
                TickFrequency="1" IsSnapToTickEnabled="True"/>
    </StackPanel>
</StackPanel>
```

## 28.4 CodexSwitch 实战

### 28.4.1 CodexSwitch 中的输入控件使用

CodexSwitch 在各种表单中使用了这些高级输入控件：

- **NumericUpDown**：端口号输入、超时设置、重试次数
- **ComboBox（CodexSelect）**：Provider 选择、模型选择、协议选择
- **TextBox**：API Key 输入、URL 输入、名称输入

### 28.4.2 CodexSwitch 的样式主题

CodexSwitch 在 CodexTheme.axaml 中定义了输入控件的样式：

```xml
<!-- 输入框背景 -->
<SolidColorBrush x:Key="CsInputBrush" Color="#26FFFFFF"/>
<SolidColorBrush x:Key="CsInputBackgroundBrush" Color="#4D404040"/>
<SolidColorBrush x:Key="CsInputFocusBackgroundBrush" Color="#66404040"/>

<!-- 用于输入框的边框 -->
<SolidColorBrush x:Key="CsBorderBrush" Color="#1AFFFFFF"/>
<SolidColorBrush x:Key="CsHoverBorderBrush" Color="#33FFFFFF"/>
<SolidColorBrush x:Key="CsRingBrush" Color="#8E8E8E"/>
```

## 28.5 举一反三

### 28.5.1 带验证的表单

```xml
<StackPanel Spacing="16" Width="400">
    <TextBlock Text="用户注册" FontSize="20" FontWeight="Bold"/>

    <StackPanel Spacing="4">
        <TextBlock Text="用户名"/>
        <TextBox Text="{Binding Username, Mode=TwoWay}"/>
    </StackPanel>

    <StackPanel Spacing="4">
        <TextBlock Text="邮箱"/>
        <TextBox Text="{Binding Email, Mode=TwoWay}"/>
    </StackPanel>

    <StackPanel Spacing="4">
        <TextBlock Text="年龄"/>
        <NumericUpDown Value="{Binding Age, Mode=TwoWay}"
                       Minimum="0" Maximum="150" FormatString="N0"/>
    </StackPanel>

    <StackPanel Spacing="4">
        <TextBlock Text="出生日期"/>
        <DatePicker SelectedDate="{Binding BirthDate, Mode=TwoWay}"
                    MaxYear="2010-01-01"/>
    </StackPanel>

    <StackPanel Spacing="4">
        <TextBlock Text="偏好时间"/>
        <TimePicker SelectedTime="{Binding PreferredTime, Mode=TwoWay}"
                    ClockIdentifier="24HourClock"/>
    </StackPanel>

    <Button Content="注册" Command="{Binding RegisterCommand}"
            IsEnabled="{Binding !HasErrors}"/>
</StackPanel>
```

### 28.5.2 音量控制器

```xml
<StackPanel Spacing="8" Width="300">
    <Grid ColumnDefinitions="Auto,*,Auto">
        <PathIcon Data="{StaticResource VolumeIcon}" Width="16" Height="16"/>
        <Slider Grid.Column="1" Margin="8,0"
                Minimum="0" Maximum="100"
                Value="{Binding Volume, Mode=TwoWay}"/>
        <TextBlock Grid.Column="2"
                   Text="{Binding Volume, StringFormat='{}{0:F0}'}"
                   Width="30"/>
    </Grid>
</StackPanel>
```

## 28.6 最佳实践与设计模式

1. **使用合适的输入控件**：数字用 NumericUpDown，日期用 DatePicker，时间用 TimePicker
2. **设置合理的范围**：Minimum 和 Maximum 防止无效输入
3. **提供占位文本**：Watermark 告诉用户应该输入什么
4. **使用双向绑定**：输入控件通常需要 Mode=TwoWay
5. **即时验证**：使用 INotifyDataErrorInfo 提供实时反馈
6. **提供 FormatString**：确保数字显示格式一致
7. **Slider 配合标签**：显示当前值让用户知道具体数值
8. **不确定进度用 IsIndeterminate**：当不知道具体进度时使用

## Deep Dive

### NumericUpDown 的输入解析流程

1. 用户在文本框中输入字符
2. NumericUpDown 根据 NumberStyle 判断字符是否合法
3. 如果不合法，字符被忽略
4. 如果合法，尝试解析为 decimal
5. 如果解析成功，检查是否在 Minimum/Maximum 范围内
6. 如果设置了 ClipValueToMinMax，值被限制在范围内
7. 触发 ValueChanged 事件

### AutoCompleteBox 的过滤流程

1. 用户输入字符
2. 等待 MinimumPopulationDelay 毫秒
3. 根据 FilterMode 对 ItemsSource 进行过滤
4. 将过滤结果显示在下拉列表中
5. 如果 IsTextCompletionEnabled，将第一个匹配项补全到文本框
6. 用户选择一个建议项或继续输入

### INotifyDataErrorInfo 的工作原理

1. 当绑定的属性值改变时，Avalonia 检查目标是否实现了 INotifyDataErrorInfo
2. 如果实现了，调用 GetErrors(propertyName) 获取错误
3. 如果有错误，在控件周围显示红色边框
4. 错误信息可以通过 ToolTip 或自定义方式显示

## Cross References

- **[第 5 章：数据绑定](05-data-binding.md)** -- TwoWay 绑定
- **[第 16 章：输入事件](16-input-events.md)** -- 键盘和指针事件
- **[第 18 章：命令系统](18-commands.md)** -- KeyBinding 和命令
- **[第 19 章：值转换器](19-value-converters.md)** -- 输入格式转换
- **[第 15 章：自定义控件](15-custom-controls.md)** -- 创建自定义输入控件

## Common Pitfalls

### 1. NumericUpDown 的 Value 为 null

**问题**：Value 属性是 `decimal?` 类型，可以为 null。

```xml
<!-- 问题：没有处理 null -->
<TextBlock Text="{Binding Quantity}"/>

<!-- 解决：使用 FallbackValue 或 StringFormat -->
<TextBlock Text="{Binding Quantity, FallbackValue='0'}"/>
<TextBlock Text="{Binding Quantity, StringFormat='{}{0:N0}'}"/>
```

### 2. DatePicker 的时区问题

**问题**：DatePicker 使用 DateTimeOffset，时区可能导致日期偏移。

```csharp
// 解决：使用 DateTimeOffset 而不是 DateTime
public DateTimeOffset? SelectedDate { get; set; }

// 如果必须使用 DateTime
public DateTime? SelectedDate
{
    get => _selectedDate;
    set
    {
        SetProperty(ref _selectedDate, value);
        // 转换为 DateTimeOffset
        SelectedDateOffset = value.HasValue
            ? new DateTimeOffset(value.Value, TimeSpan.Zero)
            : null;
    }
}
```

### 3. AutoCompleteBox 的过滤性能

**问题**：大数据集的自动补全可能很慢。

```xml
<!-- 解决：增加 MinimumPrefixLength，减少过滤数据量 -->
<AutoCompleteBox MinimumPrefixLength="3"
                 MinimumPopulationDelay="0:0:0.3"
                 ItemsSource="{Binding FilteredSuggestions}"/>
```

### 4. Slider 值绑定不更新

**问题**：Slider 值改变但 ViewModel 中的属性没有更新。

```xml
<!-- 错误：缺少 TwoWay 绑定 -->
<Slider Value="{Binding Volume}"/>

<!-- 正确：使用 TwoWay 绑定 -->
<Slider Value="{Binding Volume, Mode=TwoWay}"/>
```

### 5. TimePicker 的 12/24 小时制混淆

**问题**：用户看到 12 小时制但期望 24 小时制。

```xml
<!-- 解决：明确设置 ClockIdentifier -->
<TimePicker ClockIdentifier="24HourClock"
            SelectedTime="{Binding Time, Mode=TwoWay}"/>
```

### 6. ProgressBar 的 Value 超出范围

**问题**：Value 大于 Maximum 或小于 Minimum 时显示异常。

```csharp
// 解决：在 ViewModel 中限制值
private double _progress;
public double Progress
{
    get => _progress;
    set => SetProperty(ref _progress, Math.Clamp(value, 0, 100));
}
```

### 7. NumericUpDown 的 FormatString 不生效

**问题**：设置了 FormatString 但显示没有变化。

```xml
<!-- 原因：FormatString 需要与 NumberStyle 匹配 -->
<!-- 错误：整数输入但格式包含小数 -->
<NumericUpDown NumberStyle="Integer" FormatString="F2"/>

<!-- 正确：允许小数输入 -->
<NumericUpDown NumberStyle="Number" FormatString="F2"/>
```

### 8. 验证错误不显示

**问题**：实现了 INotifyDataErrorInfo 但错误提示不显示。

```csharp
// 原因：忘记触发 ErrorsChanged 事件
private void AddError(string propertyName, string error)
{
    if (!_errors.ContainsKey(propertyName))
        _errors[propertyName] = new List<string>();
    _errors[propertyName].Add(error);
    // 必须触发这个事件！
    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
}
```

### 9. DatePicker 的 MinYear/MaxYear 格式

**问题**：设置 MinYear/MaxYear 时格式不对导致不生效。

```xml
<!-- 错误：直接使用数字 -->
<DatePicker MinYear="2020"/>

<!-- 正确：使用完整日期字符串 -->
<DatePicker MinYear="2020-01-01" MaxYear="2030-12-31"/>
```

### 10. AutoCompleteBox 的下拉列表位置异常

**问题**：下拉列表出现在错误的位置。

```xml
<!-- 解决：确保 AutoCompleteBox 有足够的空间 -->
<AutoCompleteBox MaxDropDownHeight="200"
                 ItemsSource="{Binding Suggestions}"/>
```

## Try It Yourself

### 练习 1：创建温度转换器

创建一个界面，有一个 Slider 选择温度（摄氏度），自动显示华氏度和开尔文。

**提示**：使用 Slider + TextBlock + 值转换器。

### 练习 2：实现表单验证

创建一个用户注册表单，包含用户名、邮箱、密码、年龄，使用 INotifyDataErrorInfo 进行验证。

**提示**：使用 CommunityToolkit.Mvvm 的 ObservableValidator。

### 练习 3：创建搜索框

创建一个带自动补全的搜索框，从远程 API 加载建议。

**提示**：使用 AutoCompleteBox 的 Populating 事件实现异步加载。

### 练习 4：实现日期范围选择

创建两个 DatePicker，第一个选择开始日期，第二个的 MinYear 自动更新为第一个的选中日期。

**提示**：绑定第二个 DatePicker 的 MinYear 到第一个的 SelectedDate。

### 练习 5：创建进度监控面板

创建一个面板，显示多个任务的进度，包括确定和不确定进度条。

**提示**：使用 ItemsControl + ProgressBar + ViewModel 绑定。

### 练习 6：实现颜色选择器

使用三个 Slider（R、G、B）创建一个简单的颜色选择器。

**提示**：使用 Slider + 值转换器将三个值组合成 Color。

### 练习 7：创建带快捷键的表单

创建一个表单，支持 Ctrl+S 保存、Escape 取消、Enter 提交。

**提示**：使用 KeyBinding 绑定快捷键到 Command。

### 练习 8：CodexSwitch 风格的设置面板

模仿 CodexSwitch 的设置页面，创建一个包含各种输入控件的设置面板。

**提示**：使用 StackPanel 组织各种输入控件，配合 GroupBox 分组。
