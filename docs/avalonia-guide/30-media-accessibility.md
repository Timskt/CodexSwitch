# 30. 媒体控件与无障碍

> **写给零基础的你**：媒体控件就是处理图片、视频、音频的工具。无障碍（Accessibility）是让你的软件对所有人友好——包括视力不好的人（需要大字体、高对比度）、行动不便的人（只能用键盘操作）、使用屏幕阅读器的人等。做好无障碍不仅是道德责任，在很多国家也是法律要求。

## 30.1 概述

本章讲解 Avalonia 的媒体控件（Image、RenderTargetBitmap、MediaElement）和无障碍（Accessibility）支持。这些是构建专业级桌面应用的重要组成部分。CodexSwitch 作为一款桌面工具，也需要关注无障碍支持。

学完本章后，你将能够：
- 掌握 Image 控件的所有属性和加载方式
- 掌握 RenderTargetBitmap 离屏渲染
- 了解 MediaElement 的 LibVLCSharp 集成
- 掌握 AutomationProperties 无障碍属性
- 掌握键盘 Tab 导航和焦点管理 API
- 了解高对比度支持

## 30.2 核心概念

### 30.2.1 Image 控件详解

Image 控件用于显示图像。它支持从多种来源加载图像，并提供灵活的拉伸和对齐选项。

#### Image 的所有属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Source` | IImage | null | 图像源 |
| `Stretch` | Stretch | Uniform | 拉伸模式 |
| `StretchDirection` | StretchDirection | Both | 拉伸方向 |
| `IsEnabled` | bool | true | 是否启用 |

#### Stretch 拉伸模式

| 值 | 说明 | 适用场景 |
|---|------|---------|
| `None` | 不拉伸，显示原始大小 | 小图标、精确尺寸 |
| `Fill` | 填充整个区域（可能变形） | 不关心比例的背景 |
| `Uniform` | 保持比例，适应区域（默认） | 大多数场景 |
| `UniformToFill` | 保持比例，填充区域（可能裁剪） | 背景图、头像 |

#### StretchDirection 拉伸方向

| 值 | 说明 |
|---|------|
| `UpOnly` | 只放大，不缩小 |
| `DownOnly` | 只缩小，不放大 |
| `Both` | 可放大也可缩小（默认） |

#### 示例 1：从 avares:// 加载嵌入资源

```xml
<!-- avares:// 是 Avalonia 的资源协议 -->
<Image Source="avares://CodexSwitch/Assets/logo.png"
       Width="200" Height="100"/>

<!-- 不指定大小，使用原始大小 -->
<Image Source="avares://CodexSwitch/Assets/logo.png"/>
```

**avares:// 路径格式：**
`avares://程序集名/资源路径`

程序集名通常是项目名称，资源路径是文件在项目中的相对路径。

#### 示例 2：从文件路径加载

```xml
<!-- 绝对路径 -->
<Image Source="/Users/username/Pictures/photo.jpg"/>

<!-- 相对路径（相对于工作目录） -->
<Image Source="./images/photo.png"/>
```

#### 示例 3：从 URL 加载

```xml
<!-- 从网络 URL 加载（需要网络支持） -->
<Image Source="https://example.com/image.png"/>
```

注意：从 URL 加载图像可能需要额外的配置和错误处理。

#### 示例 4：Stretch 模式对比

```xml
<!-- None：原始大小 -->
<Image Source="avares://MyApp/Assets/photo.jpg"
       Stretch="None"/>

<!-- Fill：填充（可能变形） -->
<Image Source="avares://MyApp/Assets/photo.jpg"
       Stretch="Fill"
       Width="200" Height="100"/>

<!-- Uniform：保持比例（默认） -->
<Image Source="avares://MyApp/Assets/photo.jpg"
       Stretch="Uniform"
       Width="200" Height="100"/>

<!-- UniformToFill：保持比例填充（可能裁剪） -->
<Image Source="avares://MyApp/Assets/photo.jpg"
       Stretch="UniformToFill"
       Width="200" Height="100"/>
```

#### 示例 5：StretchDirection 控制

```xml
<!-- 只放大不缩小 -->
<Image Source="avares://MyApp/Assets/icon.png"
       Stretch="Uniform"
       StretchDirection="UpOnly"
       Width="200" Height="200"/>

<!-- 只缩小不放大 -->
<Image Source="avares://MyApp/Assets/large-photo.jpg"
       Stretch="Uniform"
       StretchDirection="DownOnly"
       Width="400" Height="300"/>
```

#### 示例 6：Image 作为背景

```xml
<!-- 使用 ImageBrush 作为背景 -->
<Border Width="400" Height="300">
    <Border.Background>
        <ImageBrush Source="avares://MyApp/Assets/background.jpg"
                    Stretch="UniformToFill"/>
    </Border.Background>
    <TextBlock Text="前景文字" Foreground="White"
               FontSize="24" FontWeight="Bold"
               HorizontalAlignment="Center"
               VerticalAlignment="Center"/>
</Border>
```

### 30.2.2 从代码加载图像

#### 方式 1：从 avares:// 加载

```csharp
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

// 使用 AssetLoader 加载嵌入资源
var bitmap = new Bitmap(AssetLoader.Open(
    new Uri("avares://MyApp/Assets/logo.png")));
```

#### 方式 2：从文件加载

```csharp
var bitmap = new Bitmap("/path/to/image.png");
```

#### 方式 3：从 Stream 加载

```csharp
using var stream = File.OpenRead("image.png");
var bitmap = new Bitmap(stream);
```

#### 方式 4：从字节数组加载

```csharp
byte[] imageData = await File.ReadAllBytesAsync("image.png");
using var stream = new MemoryStream(imageData);
var bitmap = new Bitmap(stream);
```

#### 方式 5：动态更新 Image

```csharp
// 在 ViewModel 中
[ObservableProperty]
private IImage? _currentImage;

// 切换图片
private void LoadNewImage(string path)
{
    CurrentImage = new Bitmap(path);
}
```

```xml
<Image Source="{Binding CurrentImage}"
       Width="200" Height="200"
       Stretch="UniformToFill"/>
```

### 30.2.3 RenderTargetBitmap（离屏渲染）

RenderTargetBitmap 允许你在内存中创建图像，然后使用 DrawingContext 绘制内容。这对于生成缩略图、截图、动态图像等非常有用。

**什么是离屏渲染？**
想象你在一张白纸上画画（而不是直接在屏幕上画）。画完之后，你可以把这张纸作为图片使用。RenderTargetBitmap 就是这张"白纸"。

#### RenderTargetBitmap 的属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `PixelSize` | PixelSize | 像素尺寸 |
| `Dpi` | Vector | DPI（每英寸点数） |

#### 示例 1：基本离屏渲染

```csharp
// 创建一个 200x200 的 RenderTargetBitmap
var rtb = new RenderTargetBitmap(
    new PixelSize(200, 200),
    new Vector(96, 96)); // 96 DPI

// 使用 DrawingContext 绘制
using (var ctx = rtb.CreateDrawingContext())
{
    // 绘制蓝色矩形
    ctx.DrawRectangle(Brushes.Blue, null, new Rect(0, 0, 200, 200));

    // 绘制文字
    var text = new FormattedText(
        "Hello",
        CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        new Typeface("Arial"),
        24,
        Brushes.White);
    ctx.DrawText(text, new Point(50, 80));
}

// 使用渲染结果
MyImage.Source = rtb;
```

#### 示例 2：生成控件截图

```csharp
// 将任意控件渲染为图片
public static async Task<RenderTargetBitmap> CaptureControlAsync(Control control)
{
    var pixelSize = new PixelSize(
        (int)control.Bounds.Width,
        (int)control.Bounds.Height);
    var dpi = new Vector(96, 96);

    var rtb = new RenderTargetBitmap(pixelSize, dpi);
    control.Render(await Task.FromResult(rtb.CreateDrawingContext()));
    return rtb;
}
```

#### 示例 3：保存到文件

```csharp
var rtb = new RenderTargetBitmap(new PixelSize(200, 200), new Vector(96, 96));
using (var ctx = rtb.CreateDrawingContext())
{
    ctx.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 200, 200));
}

// 保存为 PNG
using var stream = File.Create("output.png");
rtb.Save(stream);
```

#### 示例 4：生成缩略图

```csharp
public static RenderTargetBitmap CreateThumbnail(string imagePath, int maxWidth, int maxHeight)
{
    var original = new Bitmap(imagePath);
    var ratio = Math.Min(
        (double)maxWidth / original.PixelSize.Width,
        (double)maxHeight / original.PixelSize.Height);
    var width = (int)(original.PixelSize.Width * ratio);
    var height = (int)(original.PixelSize.Height * ratio);

    var rtb = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
    using (var ctx = rtb.CreateDrawingContext())
    {
        ctx.DrawImage(original, new Rect(0, 0, width, height));
    }
    return rtb;
}
```

#### 示例 5：动态图表渲染

```csharp
public RenderTargetBitmap RenderChart(double[] data)
{
    var width = 400;
    var height = 200;
    var rtb = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));

    using (var ctx = rtb.CreateDrawingContext())
    {
        // 背景
        ctx.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

        // 绘制柱状图
        var barWidth = (double)width / data.Length * 0.8;
        var max = data.Max();

        for (int i = 0; i < data.Length; i++)
        {
            var barHeight = data[i] / max * (height - 20);
            var x = i * (width / data.Length) + barWidth * 0.1;
            var y = height - barHeight;
            ctx.DrawRectangle(Brushes.SteelBlue, null,
                new Rect(x, y, barWidth, barHeight));
        }
    }
    return rtb;
}
```

### 30.2.4 MediaElement（LibVLCSharp 集成）

Avalonia 本身不内置视频播放功能，但可以通过 LibVLCSharp.Avalonia 包实现视频播放。

**安装：**
```xml
<PackageReference Include="LibVLCSharp.Avalonia" />
<PackageReference Include="VideoLAN.LibVLC.Windows" /> <!-- Windows -->
<!-- 或 -->
<PackageReference Include="LibVLC" /> <!-- 跨平台 -->
```

#### 示例 1：基本视频播放

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:vlc="using:LibVLCSharp.Avalonia">
    <vlc:VideoView x:Name="VideoView"
                   Width="640" Height="480"/>
</Window>
```

```csharp
using LibVLCSharp.Shared;

public partial class MainWindow : Window
{
    private LibVLC _libVLC;
    private MediaPlayer _mediaPlayer;

    public MainWindow()
    {
        InitializeComponent();

        _libVLC = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVLC);
        VideoView.MediaPlayer = _mediaPlayer;
    }

    private void PlayVideo(string url)
    {
        using var media = new Media(_libVLC, new Uri(url));
        _mediaPlayer.Play(media);
    }

    protected override void OnClosed(EventArgs e)
    {
        _mediaPlayer.Dispose();
        _libVLC.Dispose();
        base.OnClosed(e);
    }
}
```

#### 示例 2：带播放控制的视频播放器

```xml
<Grid RowDefinitions="*,Auto">
    <vlc:VideoView x:Name="VideoView" Grid.Row="0"/>

    <StackPanel Grid.Row="1" Orientation="Horizontal"
                Spacing="8" Margin="8">
        <Button Content="播放" Command="{Binding PlayCommand}"/>
        <Button Content="暂停" Command="{Binding PauseCommand}"/>
        <Button Content="停止" Command="{Binding StopCommand}"/>
        <Slider Minimum="0" Maximum="100"
                Value="{Binding Position, Mode=TwoWay}"
                Width="200"/>
        <TextBlock Text="{Binding Duration}"/>
    </StackPanel>
</Grid>
```

#### 示例 3：音频播放

```csharp
// 使用 LibVLC 播放音频
private void PlayAudio(string audioPath)
{
    using var media = new Media(_libVLC, audioPath, FromType.FromPath);
    _mediaPlayer.Play(media);
}

// 播放网络音频流
private void PlayStream(string streamUrl)
{
    using var media = new Media(_libVLC, new Uri(streamUrl));
    _mediaPlayer.Play(media);
}
```

## 30.3 无障碍（Accessibility）

无障碍（Accessibility，简称 A11y）确保你的应用可以被所有人使用，包括有视觉、听觉、运动或认知障碍的人。

**为什么无障碍重要？**
- **道德责任**：每个人都应该能使用你的软件
- **法律要求**：很多国家有无障碍法规（如美国的 ADA、欧盟的 EAA）
- **用户体验**：无障碍改进通常也提升了所有用户的体验
- **市场覆盖**：全球约 15% 的人有某种形式的障碍

### 30.3.1 AutomationProperties

AutomationProperties 为屏幕阅读器等辅助技术提供控件的语义信息。

**什么是屏幕阅读器？**
屏幕阅读器是一种软件，它读取屏幕上的内容并通过语音或盲文输出给视障用户。AutomationProperties 就是给屏幕阅读器的"标签"。

#### AutomationProperties 的所有附加属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Name` | string | 控件的可读名称（屏幕阅读器读出来的文字） |
| `HelpText` | string | 帮助文本（额外的描述信息） |
| `LabeledBy` | Control | 标签控件引用（关联的标签） |
| `ControlType` | AutomationControlType | 控件类型（Button, TextBox, Group 等） |
| `IsOffscreenBehavior` | IsOffscreenBehavior | 离屏行为 |
| `AccessKey` | string | 访问键（Alt+键） |
| `AcceleratorKey` | string | 加速键（快捷键） |

#### AutomationControlType 控件类型

| 值 | 说明 |
|---|------|
| `Button` | 按钮 |
| `Calendar` | 日历 |
| `CheckBox` | 复选框 |
| `ComboBox` | 下拉框 |
| `Edit` | 编辑框 |
| `Hyperlink` | 超链接 |
| `Image` | 图像 |
| `ListItem` | 列表项 |
| `List` | 列表 |
| `Menu` | 菜单 |
| `MenuBar` | 菜单栏 |
| `MenuItem` | 菜单项 |
| `ProgressBar` | 进度条 |
| `RadioButton` | 单选按钮 |
| `ScrollBar` | 滚动条 |
| `Slider` | 滑块 |
| `Spinner` | 微调器 |
| `StatusBar` | 状态栏 |
| `Tab` | 选项卡 |
| `TabItem` | 选项卡项 |
| `Text` | 文本 |
| `ToolBar` | 工具栏 |
| `ToolTip` | 工具提示 |
| `Tree` | 树 |
| `TreeItem` | 树项 |
| `Group` | 分组 |
| `Thumb` | 滑块拖动块 |
| `DataGrid` | 数据网格 |
| `DataItem` | 数据项 |
| `Document` | 文档 |
| `SplitButton` | 分割按钮 |
| `Window` | 窗口 |
| `Pane` | 窗格 |
| `Header` | 标题 |
| `HeaderItem` | 标题项 |
| `Table` | 表格 |
| `Separator` | 分隔符 |

#### 示例 1：基本 AutomationProperties

```xml
<!-- 设置控件名称（屏幕阅读器会读出来） -->
<Button Content="保存"
        AutomationProperties.Name="保存文档"
        AutomationProperties.HelpText="将当前文档保存到磁盘"/>
```

#### 示例 2：为输入框设置标签

```xml
<!-- 方法 1：使用 LabeledBy -->
<StackPanel Spacing="4">
    <TextBlock x:Name="NameLabel" Text="姓名："/>
    <TextBox AutomationProperties.Name="姓名"
             AutomationProperties.LabeledBy="{x:Reference NameLabel}"
             Watermark="请输入姓名"/>
</StackPanel>

<!-- 方法 2：直接在 TextBox 上设置 Name -->
<StackPanel Spacing="4">
    <TextBlock Text="邮箱："/>
    <TextBox AutomationProperties.Name="邮箱地址"
             Watermark="请输入邮箱"/>
</StackPanel>
```

#### 示例 3：为复杂控件设置类型

```xml
<!-- 自定义控件设置 ControlType -->
<Border AutomationProperties.ControlType="Group"
        AutomationProperties.Name="用户设置">
    <StackPanel>
        <TextBox AutomationProperties.Name="用户名"/>
        <TextBox AutomationProperties.Name="密码"/>
    </StackPanel>
</Border>
```

#### 示例 4：为图标按钮设置无障碍名称

```xml
<!-- 只有图标的按钮必须设置 Name -->
<Button AutomationProperties.Name="关闭">
    <PathIcon Data="{StaticResource CloseIcon}" Width="16" Height="16"/>
</Button>

<!-- 带 ToolTip 的按钮可以自动获取名称 -->
<Button ToolTip.Tip="关闭窗口">
    <PathIcon Data="{StaticResource CloseIcon}" Width="16" Height="16"/>
</Button>
```

#### 示例 5：为列表项设置无障碍信息

```xml
<ListBox ItemsSource="{Binding Items}">
    <ListBox.ItemTemplate>
        <DataTemplate x:DataType="vm:ItemViewModel">
            <StackPanel AutomationProperties.Name="{Binding DisplayName}">
                <TextBlock Text="{Binding Name}"/>
                <TextBlock Text="{Binding Description}" Foreground="Gray"/>
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

#### 示例 6：为状态指示器设置无障碍信息

```xml
<!-- 状态变化时更新 AutomationProperties -->
<Border AutomationProperties.Name="{Binding StatusText}"
        AutomationProperties.HelpText="服务器连接状态">
    <Ellipse Width="12" Height="12"
             Fill="{Binding StatusColor}"/>
</Border>
```

#### 示例 7：隐藏装饰性元素

```xml
<!-- 装饰性元素应该从无障碍树中隐藏 -->
<PathIcon Data="{StaticResource DecorativeIcon}"
          AutomationProperties.AccessibilityView="Raw"/>
<!-- 或使用 IsOffscreenBehavior -->
<Border AutomationProperties.IsOffscreenBehavior="Offscreen">
    <TextBlock Text="装饰性文字"/>
</Border>
```

### 30.3.2 键盘 Tab 导航

键盘导航允许用户只使用键盘（不需要鼠标）来操作应用。

**为什么键盘导航重要？**
- 有些用户无法使用鼠标（运动障碍）
- 有些用户更喜欢键盘（效率）
- 屏幕阅读器依赖键盘导航
- 键盘导航是无障碍的基础

#### Tab 导航属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `TabIndex` | int | Tab 键顺序（值越小越先获得焦点） |
| `IsTabStop` | bool | 是否参与 Tab 导航 |
| `TabNavigation` | KeyboardNavigationMode | Tab 导航模式 |

#### KeyboardNavigationMode 枚举

| 值 | 说明 |
|---|------|
| `Continue` | 继续到下一个控件（默认） |
| `Once` | 该容器只获得一次焦点 |
| `None` | 不参与 Tab 导航 |
| `Cycle` | 在容器内循环 |
| `Contained` | 在容器内导航，不跳出 |

#### 示例 1：设置 Tab 键顺序

```xml
<StackPanel>
    <TextBox TabIndex="1" Watermark="第一个"/>
    <TextBox TabIndex="2" Watermark="第二个"/>
    <Button TabIndex="3" Content="提交"/>
</StackPanel>
```

#### 示例 2：跳过装饰性控件

```xml
<StackPanel>
    <TextBox TabIndex="1" Watermark="名称"/>
    <!-- 这个装饰性图标不参与 Tab 导航 -->
    <PathIcon Data="{StaticResource InfoIcon}"
              IsTabStop="False"/>
    <TextBox TabIndex="2" Watermark="邮箱"/>
    <Button TabIndex="3" Content="提交"/>
</StackPanel>
```

#### 示例 3：容器内导航

```xml
<!-- TabNavigation="Contained"：Tab 键只在容器内循环 -->
<StackPanel TabNavigation="Contained">
    <TextBox Watermark="搜索"/>
    <Button Content="搜索"/>
</StackPanel>
<!-- Tab 键不会跳出这个 StackPanel -->
```

#### 示例 4：禁用某个控件的 Tab 导航

```xml
<Button Content="装饰性按钮"
        IsTabStop="False"/>
```

#### 示例 5：自定义焦点顺序的表单

```xml
<StackPanel Spacing="8">
    <TextBlock Text="用户注册"/>
    <StackPanel Spacing="4">
        <TextBlock Text="用户名"/>
        <TextBox TabIndex="1" AutomationProperties.Name="用户名"/>
    </StackPanel>
    <StackPanel Spacing="4">
        <TextBlock Text="邮箱"/>
        <TextBox TabIndex="2" AutomationProperties.Name="邮箱"/>
    </StackPanel>
    <StackPanel Spacing="4">
        <TextBlock Text="密码"/>
        <TextBox TabIndex="3" PasswordChar="*"
                 AutomationProperties.Name="密码"/>
    </StackPanel>
    <StackPanel Orientation="Horizontal" Spacing="8">
        <Button TabIndex="4" Content="注册"/>
        <Button TabIndex="5" Content="取消" IsTabStop="True"/>
    </StackPanel>
</StackPanel>
```

### 30.3.3 焦点管理 API

焦点管理 API 允许你在代码中控制哪个控件获得焦点。

#### 焦点相关属性和方法

| 属性/方法 | 说明 |
|----------|------|
| `IsFocused` | 是否有焦点 |
| `Focus()` | 尝试获取焦点 |
| `Focusable` | 是否可以获取焦点 |
| `GotFocus` | 获得焦点事件 |
| `LostFocus` | 失去焦点事件 |
| `FocusManager.GetFocusManager()` | 获取焦点管理器 |
| `FocusManager.Focus()` | 通过焦点管理器设置焦点 |

#### 示例 1：在代码中设置焦点

```csharp
// 方式 1：直接调用 Focus()
SearchBox.Focus();

// 方式 2：使用 FocusManager
var focusManager = FocusManager.GetFocusManager(this);
focusManager?.Focus(SearchBox);

// 方式 3：在窗口加载后设置焦点
protected override void OnOpened(EventArgs e)
{
    base.OnOpened(e);
    SearchBox.Focus();
}
```

#### 示例 2：处理焦点事件

```xml
<TextBox x:Name="SearchBox"
         GotFocus="OnSearchGotFocus"
         LostFocus="OnSearchLostFocus"
         Watermark="搜索..."/>
```

```csharp
private void OnSearchGotFocus(object? sender, GotFocusEventArgs e)
{
    // 获得焦点时的处理
    SearchBox.SelectAll();
}

private void OnSearchLostFocus(object? sender, RoutedEventArgs e)
{
    // 失去焦点时的处理
    if (string.IsNullOrWhiteSpace(SearchBox.Text))
    {
        // 恢复默认状态
    }
}
```

#### 示例 3：焦点可见性指示器

```xml
<!-- 只在使用键盘时显示焦点指示器 -->
<Window.Styles>
    <!-- 使用键盘时显示焦点矩形 -->
    <Style Selector="TextBox:focus-visible">
        <Setter Property="BorderBrush" Value="{StaticResource FocusBrush}"/>
        <Setter Property="BorderThickness" Value="2"/>
    </Style>

    <!-- 使用鼠标时不显示焦点矩形 -->
    <Style Selector="TextBox:focus">
        <Setter Property="BorderBrush" Value="{StaticResource DefaultBorderBrush}"/>
        <Setter Property="BorderThickness" Value="1"/>
    </Style>
</Window.Styles>
```

#### 示例 4：焦点陷阱（对话框中）

```xml
<!-- 对话框中的 Tab 导航应该限制在对话框内 -->
<Border TabNavigation="Contained"
        Focusable="True"
        AutomationProperties.ControlType="Window"
        AutomationProperties.Name="确认对话框">
    <StackPanel>
        <TextBlock Text="确定要删除吗？"/>
        <StackPanel Orientation="Horizontal">
            <Button Content="确定" TabIndex="1"/>
            <Button Content="取消" TabIndex="2"/>
        </StackPanel>
    </StackPanel>
</Border>
```

### 30.3.4 高对比度支持

高对比度模式帮助视力不佳的用户更好地看清界面。

**什么是高对比度模式？**
高对比度模式使用高对比度的颜色组合（如黑底白字、白底黑字），减少渐变和透明效果，使界面更容易看清。

#### 示例 1：检测高对比度模式

```csharp
// 在 Windows 上检测高对比度
if (OperatingSystem.IsWindows())
{
    // 使用 Windows API
    var isHighContrast = SystemParameters.HighContrast;
}
```

#### 示例 2：为高对比度模式定义主题

```xml
<Application.Styles>
    <FluentTheme>
        <FluentTheme.Styles>
            <!-- 高对比度主题 -->
            <Style Selector="Border.high-contrast">
                <Setter Property="Background" Value="Black"/>
                <Setter Property="BorderBrush" Value="White"/>
            </Style>
        </FluentTheme.Styles>
    </FluentTheme>
</Application.Styles>
```

#### 示例 3：高对比度友好的设计

```xml
<!-- 不要只用颜色传达信息 -->
<!-- 错误：只用红色表示错误 -->
<Ellipse Fill="Red"/>

<!-- 正确：同时使用图标和文字 -->
<StackPanel Orientation="Horizontal" Spacing="4">
    <PathIcon Data="{StaticResource ErrorIcon}" Foreground="Red"/>
    <TextBlock Text="错误" Foreground="Red"/>
</StackPanel>

<!-- 确保足够的对比度 -->
<!-- 文本和背景的对比度至少 4.5:1 -->
<TextBlock Text="重要信息"
           Foreground="White"
           Background="Black"/>
```

### 30.3.5 无障碍最佳实践

#### 示例 1：为应用设置标题

```xml
<Window Title="CodexSwitch - AI 模型管理工具">
    <!-- 内容 -->
</Window>
```

#### 示例 2：为图像提供替代文本

```xml
<!-- 有意义的图像需要替代文本 -->
<Image Source="avares://MyApp/Assets/chart.png"
       AutomationProperties.Name="月度使用量趋势图：1月 100次，2月 150次，3月 200次"/>

<!-- 装饰性图像隐藏 -->
<PathIcon Data="{StaticResource DecorativeIcon}"
          AutomationProperties.AccessibilityView="Raw"/>
```

#### 示例 3：使用语义化控件

```xml
<!-- 错误：用 Border + TextBlock 模拟按钮 -->
<Border Background="Blue" Cursor="Hand"
        PointerPressed="OnClicked">
    <TextBlock Text="点击我" Foreground="White"/>
</Border>

<!-- 正确：使用 Button 控件 -->
<Button Content="点击我"/>
```

#### 示例 4：提供键盘快捷键

```xml
<!-- 为常用操作提供快捷键 -->
<Window.InputBindings>
    <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}"/>
    <KeyBinding Gesture="Ctrl+N" Command="{Binding NewCommand}"/>
    <KeyBinding Gesture="Ctrl+F" Command="{Binding SearchCommand}"/>
</Window.InputBindings>

<!-- 在菜单中显示快捷键 -->
<MenuItem Header="保存(_S)"
          Command="{Binding SaveCommand}"
          InputGesture="Ctrl+S"/>
```

## 30.4 进阶用法

### 30.4.1 图像缓存和性能优化

```csharp
// 使用 WeakReference 缓存图像
private static readonly ConcurrentDictionary<string, WeakReference<Bitmap>> _imageCache = new();

public static Bitmap LoadCachedImage(string path)
{
    if (_imageCache.TryGetValue(path, out var weakRef)
        && weakRef.TryGetTarget(out var cached))
    {
        return cached;
    }

    var bitmap = new Bitmap(path);
    _imageCache[path] = new WeakReference<Bitmap>(bitmap);
    return bitmap;
}
```

### 30.4.2 异步图像加载

```csharp
public async Task<IImage> LoadImageAsync(string url)
{
    using var httpClient = new HttpClient();
    var bytes = await httpClient.GetByteArrayAsync(url);
    using var stream = new MemoryStream(bytes);
    return new Bitmap(stream);
}
```

### 30.4.3 自定义无障碍提供程序

```csharp
// 创建自定义的 AutomationPeer
public class CustomControlAutomationPeer : ControlAutomationPeer
{
    public CustomControlAutomationPeer(CustomControl owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Custom;
    }

    protected override string? GetNameCore()
    {
        return ((CustomControl)Owner).DisplayName;
    }

    protected override bool IsKeyboardFocusableCore()
    {
        return true;
    }
}
```

## 30.5 CodexSwitch 实战

### 30.5.1 CodexSwitch 的图像使用

CodexSwitch 使用 Image 和 PathIcon 来显示图标：

```xml
<!-- 使用自定义的 CodexImageIcon -->
<ui:CodexImageIcon Path="{Binding CodexIconPath}"
                   Width="24" Height="24"/>

<!-- 使用 Lucide 图标库 -->
<lucide:LucideIcon Kind="LayoutDashboard" Size="17" StrokeWidth="2"/>
```

### 30.5.2 CodexSwitch 的自定义渲染

CodexSwitch 的 `CsRollingNumber` 控件展示了如何使用 DrawingContext 进行自定义渲染：

```csharp
public override void Render(DrawingContext context)
{
    var text = FormatValue((long)Math.Round(_displayValue), UseCompactFormat);
    var layout = CreateTextLayout(text);
    var y = Math.Round((Bounds.Height - layout.Height) / 2d);

    // 使用 TextLayout 绘制文字
    layout.Draw(context, new Point(0, y));
}
```

## 30.6 举一反三

### 30.6.1 创建无障碍友好的表单

```xml
<StackPanel Spacing="16"
            AutomationProperties.ControlType="Group"
            AutomationProperties.Name="用户注册表单">
    <TextBlock Text="用户注册" FontSize="20"/>

    <StackPanel Spacing="4">
        <TextBlock x:Name="UsernameLabel" Text="用户名（必填）"/>
        <TextBox AutomationProperties.Name="用户名"
                 AutomationProperties.LabeledBy="{x:Reference UsernameLabel}"
                 TabIndex="1"/>
    </StackPanel>

    <StackPanel Spacing="4">
        <TextBlock x:Name="EmailLabel" Text="邮箱（必填）"/>
        <TextBox AutomationProperties.Name="邮箱"
                 AutomationProperties.LabeledBy="{x:Reference EmailLabel}"
                 TabIndex="2"/>
    </StackPanel>

    <StackPanel Orientation="Horizontal" Spacing="8">
        <Button Content="注册" TabIndex="3"
                AutomationProperties.Name="注册新用户"/>
        <Button Content="取消" TabIndex="4"/>
    </StackPanel>
</StackPanel>
```

### 30.6.2 创建无障碍友好的状态指示器

```xml
<!-- 不要只用颜色 -->
<StackPanel Orientation="Horizontal" Spacing="4">
    <Ellipse Width="12" Height="12"
             Fill="{Binding StatusColor}"/>
    <TextBlock Text="{Binding StatusText}"
               AutomationProperties.Name="{Binding StatusDescription}"/>
</StackPanel>
```

## 30.7 最佳实践与设计模式

1. **为所有交互控件设置 AutomationProperties.Name**：屏幕阅读器需要
2. **确保 Tab 导航顺序合理**：用户应该能用键盘完成所有操作
3. **提供足够的颜色对比度**：文本和背景的对比度至少 4.5:1
4. **不要只用颜色传达信息**：同时使用图标或文字
5. **测试键盘操作**：确保没有鼠标也能使用应用
6. **为图像提供替代文本**：有意义的图像需要描述
7. **使用语义化控件**：优先使用标准控件而不是自定义模拟
8. **提供键盘快捷键**：为常用操作提供快捷键

## Deep Dive

### Image 的渲染流程

1. 接收 Source 属性（IImage 类型）
2. 根据 Stretch 计算目标尺寸
3. 根据 StretchDirection 限制缩放方向
4. 使用 DrawingContext.DrawImage 绘制
5. 如果是矢量图像（SVG），进行矢量渲染

### RenderTargetBitmap 的工作原理

1. 创建一个离屏渲染表面（GPU 或 CPU）
2. 提供 DrawingContext 用于绘制
3. 绘制命令被记录到渲染表面
4. 渲染完成后可以保存为文件或显示在 Image 控件中
5. 渲染表面使用与屏幕相同的渲染管线

### AutomationProperties 的传播机制

1. Avalonia 维护一个无障碍树（与可视化树平行）
2. AutomationProperties 被映射到无障碍树中的节点
3. 屏幕阅读器通过平台 API 访问无障碍树
4. 属性变化时自动通知屏幕阅读器

### 键盘焦点系统

1. Avalonia 维护一个焦点范围（Focus Scope）
2. 每个窗口通常是一个焦点范围
3. Tab 键在焦点范围内按 TabIndex 顺序导航
4. FocusManager 管理焦点的分配
5. IsTabStop=false 的控件不参与导航

## Cross References

- **[第 1 章：Avalonia 概览](01-avalonia-overview.md)** -- Image 控件基础
- **[第 14 章：自定义渲染](14-custom-rendering.md)** -- RenderTargetBitmap 和 DrawingContext
- **[第 16 章：输入事件](16-input-events.md)** -- 键盘事件和焦点
- **[第 29 章：形状与绘图](29-shapes-drawing.md)** -- Shape 和 Brush
- **[第 15 章：自定义控件](15-custom-controls.md)** -- 创建无障碍友好的自定义控件
- **[第 7 章：样式与主题](07-styling-theming.md)** -- 高对比度主题

## Common Pitfalls

### 1. avares:// 路径大小写错误

**问题**：avares:// 路径大小写敏感。

```xml
<!-- 错误 -->
<Image Source="avares://MyApp/assets/Logo.PNG"/>

<!-- 正确 -->
<Image Source="avares://MyApp/Assets/Logo.png"/>
```

### 2. 大图像导致内存问题

**问题**：加载大图像到内存可能消耗大量 RAM。

```csharp
// 解决：使用 DecodePixelWidth 限制解码尺寸
var options = new BitmapDecodeOptions
{
    DecodePixelWidth = 400
};
var bitmap = new Bitmap(stream, options);
```

### 3. 忘记为自定义控件设置无障碍属性

**问题**：自定义控件默认没有无障碍信息。

```csharp
// 解决：重写 OnCreateAutomationPeer
protected override AutomationPeer OnCreateAutomationPeer()
{
    return new CustomControlAutomationPeer(this);
}
```

### 4. RenderTargetBitmap 的 DPI 问题

**问题**：不同 DPI 的显示器上图像显示不正确。

```csharp
// 解决：使用正确的 DPI
var dpi = VisualRoot?.RenderScaling ?? 1.0;
var rtb = new RenderTargetBitmap(
    new PixelSize((int)(width * dpi), (int)(height * dpi)),
    new Vector(96 * dpi, 96 * dpi));
```

### 5. 图像缓存导致内存泄漏

**问题**：缓存的 Bitmap 没有被释放。

```csharp
// 解决：使用 WeakReference 缓存
private static readonly ConcurrentDictionary<string, WeakReference<Bitmap>> _cache = new();
```

### 6. Tab 导航顺序混乱

**问题**：TabIndex 设置不正确导致焦点跳转混乱。

```xml
<!-- 解决：确保 TabIndex 是连续的正整数 -->
<TextBox TabIndex="1"/>
<TextBox TabIndex="2"/>
<Button TabIndex="3"/>
```

### 7. 屏幕阅读器无法读取动态内容

**问题**：动态更新的内容没有通知屏幕阅读器。

```csharp
// 解决：触发自动化事件
var peer = FrameworkElementAutomationPeer.FromElement(myControl);
peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
```

### 8. 高对比度模式下颜色不可见

**问题**：使用了与背景色相近的颜色。

```xml
<!-- 解决：使用高对比度主题 -->
<!-- 定义高对比度下的颜色资源 -->
<SolidColorBrush x:Key="TextBrush"
                 Color="White"/>  <!-- 高对比度模式 -->
```

### 9. Image 的 Stretch 模式选择错误

**问题**：图像变形或被意外裁剪。

```xml
<!-- 解决：根据需求选择合适的 Stretch -->
<!-- 保持比例，适应容器（推荐大多数场景） -->
<Image Stretch="Uniform"/>

<!-- 保持比例，填充容器（可能裁剪） -->
<Image Stretch="UniformToFill"/>
```

### 10. MediaElement 视频不播放

**问题**：LibVLCSharp 初始化失败或路径错误。

```csharp
// 解决：确保 LibVLC 正确初始化
Core.Initialize(); // 初始化 LibVLC
var libVLC = new LibVLC();
var media = new Media(libVLC, new Uri(videoPath));
```

## Try It Yourself

### 练习 1：创建图像浏览器

创建一个简单的图像浏览器，支持从文件加载图像，切换 Stretch 模式。

**提示**：使用 Image 控件 + ComboBox 选择 Stretch 模式。

### 练习 2：实现无障碍友好的表单

创建一个用户注册表单，确保所有控件都有正确的 AutomationProperties。

**提示**：使用 LabeledBy 关联标签，设置 Name 和 HelpText。

### 练习 3：创建自定义进度指示器

使用 RenderTargetBitmap 创建一个自定义的环形进度指示器。

**提示**：使用 DrawingContext 绘制弧线和文字。

### 练习 4：实现键盘导航测试

创建一个表单，测试 Tab 导航顺序是否正确。

**提示**：设置 TabIndex，使用 FocusManager 跟踪焦点。

### 练习 5：创建高对比度友好的界面

创建一个界面，确保在高对比度模式下仍然清晰可读。

**提示**：使用 DynamicResource 定义颜色，支持主题切换。

### 练习 6：实现图像缓存

创建一个图像缓存服务，支持异步加载和 WeakReference 缓存。

**提示**：使用 ConcurrentDictionary + WeakReference。

### 练习 7：创建无障碍友好的数据展示

创建一个 DataGrid，确保列头和单元格都有正确的无障碍信息。

**提示**：使用 AutomationProperties.Name 为列头设置名称。

### 练习 8：CodexSwitch 风格的图标系统

模仿 CodexSwitch 的图标系统，创建一个使用 PathIcon 的图标库。

**提示**：定义 Geometry 资源字典，使用 PathIcon 引用。
