# 17. 对话框与弹出层

> **写给零基础的你**：对话框就是弹出来的小窗口。比如你删除文件时弹出的"确定要删除吗？"、保存文件时弹出的文件选择器、软件设置里的各种配置面板——这些都是对话框。弹出层（Popup）更轻量，比如右键菜单、下拉选项、提示气泡。

## 17.1 概述

对话框和弹出层是桌面应用中最常见的 UI 模式之一。它们用于向用户展示临时信息、收集输入、确认操作或提供上下文选项。Avalonia 提供了多种实现方式，从轻量级的 ToolTip 到完整的模态对话框，开发者可以根据场景选择最合适的方案。

在 CodexSwitch 中，对话框系统被广泛使用：
- **ProviderEditorDialog**：编辑 Provider 配置的完整表单对话框
- **DeleteProviderDialog**：删除确认对话框
- **CodexAuthImportDialog**：OAuth 导入对话框
- **ModelEditorDialog**：模型编辑对话框
- **ToolTip**：为各种控件提供提示信息

本章将详细讲解 Avalonia 中所有弹出层相关组件的完整用法。

## 17.2 对话框架构选择

### 17.2.1 四种对话框模式

> **小白提示**：对话框有四种"风格"，就像你问路的方式：
> - **覆盖层** = 在原地画一个弹窗（像微信的"确认删除"弹窗）
> - **独立窗口** = 开一个新窗口（像 Word 的"另存为"窗口）
> - **Flyout** = 弹出一个小气泡（像鼠标悬停时的提示）
> - **ContentDialog** = 标准化的弹窗组件（像系统自带的"确认"对话框）

| 模式 | 实现方式 | 优点 | 缺点 | 适用场景 | 类比 |
|------|---------|------|------|---------|------|
| 覆盖层 | `ContentControl` + `IsVisible` | 与主窗口共享 ViewModel，状态保持 | 布局复杂 | 应用内对话框 | 微信删除确认弹窗 |
| 独立窗口 | `Window.ShowDialog()` | 完全隔离，原生窗口 | 需要窗口间通信 | 系统级对话框 | Word 的"另存为" |
| Flyout | `Flyout` / `Popup` | 轻量级，不遮挡背景 | 功能有限 | 工具提示、简单操作 | 鼠标悬停提示 |
| ContentDialog | 自定义或第三方 | 标准化对话框 | 可能与自定义样式冲突 | 通用对话框 | 系统确认对话框 |

### 17.2.2 选择指南

```
需要模态（阻塞交互，用户必须先处理这个对话框才能继续操作）？
├── 是 → 需要独立窗口？
│   ├── 是 → Window.ShowDialog()（像"另存为"窗口）
│   └── 否 → 覆盖层对话框 CsDialog（像"确认删除"弹窗）
└── 否 → 需要遮挡背景？
    ├── 是 → Popup（像右键菜单）
    └── 否 → Flyout / ToolTip（像悬停提示）
```

## 17.3 CsDialog 自定义对话框控件

### 17.3.1 实现原理

CodexSwitch 使用自定义的 `CsDialog` 控件实现覆盖层对话框：

```csharp
public sealed class CsDialog : ContentControl
{
    public CsDialog()
    {
        Opacity = 0;  // 初始不可见
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // 监听 IsVisible 属性变化
        if (change.Property != IsVisibleProperty ||
            change.NewValue is not bool isVisible || !isVisible)
            return;

        // 变为可见时：先设 Opacity=0，再在下一帧设为 1
        Opacity = 0;
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () => Opacity = 1,
            Avalonia.Threading.DispatcherPriority.Render);
    }
}
```

工作流程：
1. `CsDialog` 初始化时设置 `Opacity = 0`
2. 当 `IsVisible` 变为 `true` 时，先设置 `Opacity = 0`
3. 通过 `Dispatcher.UIThread.Post` 在渲染优先级的下一帧设置 `Opacity = 1`
4. 配合样式中的 `DoubleTransition`，实现平滑的淡入效果

### 17.3.2 CsDialog 样式

```xml
<Style Selector="ui|CsDialog">
    <!-- 淡入过渡动画 -->
    <Setter Property="Transitions">
        <Transitions>
            <DoubleTransition Property="Opacity" Duration="0:0:0.2"/>
        </Transitions>
    </Setter>

    <!-- 对话框模板 -->
    <Setter Property="Template">
        <ControlTemplate>
            <!-- 半透明背景遮罩 -->
            <Border Background="#80000000">
                <!-- 对话框卡片容器 -->
                <Border Classes="dialog-card"
                        MaxWidth="600"
                        MaxHeight="80vh"
                        Background="{DynamicResource CsDialogCardBrush}"
                        CornerRadius="{DynamicResource CsRadiusLg}"
                        Padding="24"
                        HorizontalAlignment="Center"
                        VerticalAlignment="Center">
                    <ContentPresenter/>
                </Border>
            </Border>
        </ControlTemplate>
    </Setter>
</Style>
```

### 17.3.3 在 XAML 中使用 CsDialog

```xml
<Grid>
    <!-- 主内容 -->
    <pages:HomePage IsVisible="{Binding IsHomePageVisible}"/>
    <pages:ProvidersPage IsVisible="{Binding IsProvidersPageVisible}"/>

    <!-- 对话框覆盖层（跨列覆盖整个窗口） -->
    <dialogs:ProviderEditorDialog Grid.ColumnSpan="2"/>
    <dialogs:DeleteProviderDialog Grid.ColumnSpan="2"/>
</Grid>
```

### 17.3.4 对话框与 ViewModel 的绑定

```csharp
// ViewModel 中控制对话框的显示
[ObservableProperty]
private bool _isDeleteProviderDialogOpen;

[ObservableProperty]
private string _providerPendingDeleteName = "";

[RelayCommand]
private void RequestDeleteProvider(string providerId)
{
    var provider = _config.Providers.FirstOrDefault(p => p.Id == providerId);
    if (provider == null) return;

    _providerPendingDeleteId = providerId;
    ProviderPendingDeleteName = provider.Name;
    IsDeleteProviderDialogOpen = true;
}

[RelayCommand]
private void ConfirmDeleteProvider()
{
    if (_providerPendingDeleteId is not null)
    {
        _store.DeleteProvider(_providerPendingDeleteId);
        RefreshProviderList();
    }
    IsDeleteProviderDialogOpen = false;
    _providerPendingDeleteId = null;
}

[RelayCommand]
private void CancelDeleteProvider()
{
    IsDeleteProviderDialogOpen = false;
    _providerPendingDeleteId = null;
}
```

```xml
<!-- DeleteProviderDialog.axaml -->
<ui:CsDialog IsVisible="{Binding IsDeleteProviderDialogOpen}">
    <StackPanel Spacing="16" MaxWidth="400">
        <TextBlock Text="{i18n:Tr dialogs.deleteProvider.title}"
                   FontWeight="Bold" FontSize="18"/>
        <TextBlock TextWrapping="Wrap">
            <TextBlock.Text>
                <MultiBinding StringFormat="确定要删除 Provider \"{0}\" 吗？此操作不可撤销。">
                    <Binding Path="ProviderPendingDeleteName"/>
                </MultiBinding>
            </TextBlock.Text>
        </TextBlock>
        <StackPanel Orientation="Horizontal"
                    HorizontalAlignment="Right"
                    Spacing="8">
            <Button Classes="outline"
                    Content="{i18n:Tr common.cancel}"
                    Command="{Binding CancelDeleteProviderCommand}"/>
            <Button Classes="destructive"
                    Content="{i18n:Tr common.delete}"
                    Command="{Binding ConfirmDeleteProviderCommand}"/>
        </StackPanel>
    </StackPanel>
</ui:CsDialog>
```

## 17.4 独立窗口对话框

### 17.4.1 使用 ShowDialog

```csharp
// 定义对话框窗口
public class SettingsDialog : Window
{
    public SettingsDialog()
    {
        Title = "Settings";
        Width = 500;
        Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }
}

// 显示模态对话框
var dialog = new SettingsDialog();
dialog.DataContext = new SettingsViewModel();

// ShowDialog 是异步的，等待对话框关闭
var result = await dialog.ShowDialog<bool>(ownerWindow);

if (result)
{
    // 用户点击了"确定"
    ApplySettings();
}
```

### 17.4.2 对话框返回值

```csharp
// 对话框内部设置结果
public class FilePickerDialog : Window
{
    public string? SelectedFile { get; private set; }

    private void OnFileSelected(string path)
    {
        SelectedFile = path;
        Close(true);  // 关闭并返回 true
    }

    private void OnCancel()
    {
        Close(false); // 关闭并返回 false
    }
}

// 使用
var dialog = new FilePickerDialog();
var accepted = await dialog.ShowDialog<bool>(ownerWindow);
if (accepted)
{
    var file = dialog.SelectedFile;
}
```

### 17.4.3 窗口所有权

```csharp
// 设置窗口所有者
dialog.ShowDialog(ownerWindow);

// 所有效果：
// 1. 对话框始终在所有者窗口之上
// 2. 对话框跟随所有者窗口移动
// 3. 所有者窗口被禁用（模态）
// 4. 关闭所有者窗口时对话框也关闭
```

## 17.5 Flyout 组件详解

### 17.5.1 Flyout 基础

Flyout 是附加在控件上的轻量级弹出层：

```xml
<Button Content="Options">
    <Button.Flyout>
        <Flyout>
            <StackPanel Width="200" Spacing="8">
                <TextBlock Text="Choose an option:" FontWeight="Bold"/>
                <RadioButton Content="Option A" GroupName="opts"/>
                <RadioButton Content="Option B" GroupName="opts"/>
                <RadioButton Content="Option C" GroupName="opts"/>
            </StackPanel>
        </Flyout>
    </Button.Flyout>
</Button>
```

### 17.5.2 Flyout Placement（定位）

```xml
<Flyout Placement="Top">          <!-- 上方 -->
<Flyout Placement="Bottom">        <!-- 下方（默认） -->
<Flyout Placement="Left">          <!-- 左侧 -->
<Flyout Placement="Right">         <!-- 右侧 -->
<Flyout Placement="Full">          <!-- 覆盖整个目标 -->
<Flyout Placement="TopEdgeAlignedLeft"/>    <!-- 上方左对齐 -->
<Flyout Placement="TopEdgeAlignedRight"/>   <!-- 上方右对齐 -->
<Flyout Placement="BottomEdgeAlignedLeft"/> <!-- 下方左对齐 -->
<Flyout Placement="BottomEdgeAlignedRight"/><!-- 下方右对齐 -->
<Flyout Placement="LeftEdgeAlignedTop"/>    <!-- 左侧上对齐 -->
<Flyout Placement="LeftEdgeAlignedBottom"/> <!-- 左侧下对齐 -->
<Flyout Placement="RightEdgeAlignedTop"/>   <!-- 右侧上对齐 -->
<Flyout Placement="RightEdgeAlignedBottom"/><!-- 右侧下对齐 -->
```

### 17.5.3 FlyoutBase.AttachedFlyout

为任意控件附加 Flyout：

```xml
<Border FlyoutBase.AttachedFlyout="{StaticResource MyFlyout}"
        Tapped="OnBorderTapped">
    <TextBlock Text="Click me for details"/>
</Border>
```

```csharp
private void OnBorderTapped(object? sender, TappedEventArgs e)
{
    if (sender is Control control)
    {
        FlyoutBase.GetAttachedFlyout(control)?.ShowAt(control);
    }
}
```

### 17.5.4 MenuFlyout

菜单样式的 Flyout：

```xml
<Button Content="Actions">
    <Button.Flyout>
        <MenuFlyout>
            <MenuItem Header="Edit" Command="{Binding EditCommand}">
                <MenuItem.Icon>
                    <PathIcon Data="{StaticResource EditIcon}"/>
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header="Duplicate" Command="{Binding DuplicateCommand}"/>
            <Separator/>
            <MenuItem Header="Delete" Command="{Binding DeleteCommand}">
                <MenuItem.Icon>
                    <PathIcon Data="{StaticResource DeleteIcon}"/>
                </MenuItem.Icon>
            </MenuItem>
        </MenuFlyout>
    </Button.Flyout>
</Button>
```

### 17.5.5 MenuFlyout 子菜单

```xml
<MenuFlyout>
    <MenuItem Header="File">
        <MenuItem Header="New" Command="{Binding NewCommand}"/>
        <MenuItem Header="Open" Command="{Binding OpenCommand}"/>
        <Separator/>
        <MenuItem Header="Recent Files">
            <MenuItem Header="file1.txt"/>
            <MenuItem Header="file2.txt"/>
            <MenuItem Header="file3.txt"/>
        </MenuItem>
    </MenuItem>
    <MenuItem Header="Edit">
        <MenuItem Header="Undo" InputGesture="Ctrl+Z"/>
        <MenuItem Header="Redo" InputGesture="Ctrl+Y"/>
    </MenuItem>
</MenuFlyout>
```

### 17.5.6 控制 Flyout 的显示和隐藏

```xml
<Flyout IsOpen="{Binding IsFlyoutOpen}"
        ShowMode="TransientWithDismissOnPointerMoveAway"
        Placement="Bottom">
    <StackPanel>
        <TextBlock Text="Flyout content"/>
        <Button Content="Close" Command="{Binding CloseFlyoutCommand}"/>
    </StackPanel>
</Flyout>
```

```csharp
// 代码中控制 Flyout
var flyout = new Flyout
{
    Content = new TextBlock { Text = "Hello!" },
    Placement = PlacementMode.Bottom
};

// 显示
flyout.ShowAt(myControl);

// 关闭
flyout.Hide();
```

## 17.6 ToolTip 组件详解

### 17.6.1 基本用法

```xml
<!-- 简单文本 ToolTip -->
<Button Content="Save" ToolTip.Tip="Save the current document"/>

<!-- 复杂内容 ToolTip -->
<Button Content="Info">
    <ToolTip.Tip>
        <StackPanel Spacing="4" Width="200">
            <TextBlock Text="Version 2.0" FontWeight="Bold"/>
            <TextBlock Text="Release Date: 2024-01-15"/>
            <TextBlock Text="New features: Dark mode, Export to PDF"/>
        </StackPanel>
    </ToolTip.Tip>
</Button>

<!-- 图片 ToolTip -->
<Image Source="/Assets/icon.png" Width="32" Height="32">
    <ToolTip.Tip>
        <Image Source="/Assets/icon-large.png" Width="200" Height="200"/>
    </ToolTip.Tip>
</Image>
```

### 17.6.2 ToolTipService 配置

```xml
<Button Content="Hover me"
        ToolTip.Tip="This is a tooltip"
        ToolTip.ShowDelay="500"
        ToolTip.ShowOnDisabled="True"
        ToolTip.Placement="Bottom"
        ToolTip.HorizontalOffset="10"
        ToolTip.VerticalOffset="5"/>
```

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ToolTip.Tip` | `object` | -- | ToolTip 内容 |
| `ToolTip.ShowDelay` | `int` | 400ms | 显示前的延迟（毫秒） |
| `ToolTip.ShowOnDisabled` | `bool` | false | 禁用时是否显示 |
| `ToolTip.Placement` | `PlacementMode` | Bottom | 显示位置 |
| `ToolTip.HorizontalOffset` | `double` | 0 | 水平偏移 |
| `ToolTip.VerticalOffset` | `double` | 0 | 垂直偏移 |
| `ToolTip.IsOpen` | `bool` | -- | 是否打开（可绑定） |
| `ToolTip.ServiceEnabled` | `bool` | true | 是否启用 ToolTip 服务 |

### 17.6.3 动态 ToolTip

```xml
<Border ToolTip.Tip="{Binding UsageToolTip}">
    <!-- 使用信息 -->
</Border>
```

```csharp
// ViewModel 中动态生成 ToolTip 内容
[ObservableProperty]
private string _usageToolTip = "";

private void UpdateUsageToolTip()
{
    UsageToolTip = $"Provider: {_currentProvider.Name}\n" +
                   $"Requests: {_requestCount:N0}\n" +
                   $"Tokens: {_totalTokens:N0}\n" +
                   $"Cost: ${_estimatedCost:F4}";
}
```

### 17.6.4 ToolTip 自定义样式

```xml
<Style Selector="ToolTip">
    <Setter Property="Background" Value="#1E1E2E"/>
    <Setter Property="Foreground" Value="#CDD6F4"/>
    <Setter Property="CornerRadius" Value="6"/>
    <Setter Property="Padding" Value="8,4"/>
    <Setter Property="FontSize" Value="12"/>
    <Setter Property="BorderBrush" Value="#45475A"/>
    <Setter Property="BorderThickness" Value="1"/>
</Style>
```

### 17.6.5 ToolTip 与数据绑定

```csharp
// 在 CodexSwitch 中，ToolTip 绑定到 ViewModel 属性
// ProvidersPage.axaml 示例：
<Border Classes="provider-usage"
        ToolTip.Tip="{Binding UsageToolTip}">
    <!-- 使用信息 UI -->
</Border>

// CodexSelect 的 ToolTip：
<ui:CodexSelect Classes="provider-model-select"
                ToolTip.Tip="{i18n:Tr providers.defaultModel}">
    <!-- 选择器内容 -->
</ui:CodexSelect>
```

## 17.7 ContextMenu 组件详解

### 17.7.1 XAML 中定义 ContextMenu

```xml
<Border Background="LightGray">
    <Border.ContextMenu>
        <ContextMenu>
            <MenuItem Header="Cut" Command="{Binding CutCommand}" InputGesture="Ctrl+X">
                <MenuItem.Icon>
                    <PathIcon Data="{StaticResource CutIcon}"/>
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header="Copy" Command="{Binding CopyCommand}" InputGesture="Ctrl+C">
                <MenuItem.Icon>
                    <PathIcon Data="{StaticResource CopyIcon}"/>
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header="Paste" Command="{Binding PasteCommand}" InputGesture="Ctrl+V">
                <MenuItem.Icon>
                    <PathIcon Data="{StaticResource PasteIcon}"/>
                </MenuItem.Icon>
            </MenuItem>
            <Separator/>
            <MenuItem Header="Select All" Command="{Binding SelectAllCommand}"
                      InputGesture="Ctrl+A"/>
        </ContextMenu>
    </Border.ContextMenu>

    <TextBlock Text="Right-click me"/>
</Border>
```

### 17.7.2 代码中创建 ContextMenu

```csharp
var menu = new ContextMenu();

var editItem = new MenuItem { Header = "Edit" };
editItem.Click += (_, _) => EditItem(itemId);
editItem.Icon = new PathIcon { Data = EditIconData };
menu.Items.Add(editItem);

var deleteItem = new MenuItem { Header = "Delete" };
deleteItem.Click += (_, _) => DeleteItem(itemId);
menu.Items.Add(deleteItem);

var separator = new Separator();
menu.Items.Add(separator);

var propertiesItem = new MenuItem { Header = "Properties" };
propertiesItem.Click += (_, _) => ShowProperties(itemId);
menu.Items.Add(propertiesItem);

// 显示菜单
menu.Open(targetControl);
```

### 17.7.3 MenuItem 的 InputGesture 显示

```xml
<MenuItem Header="Save" InputGesture="Ctrl+S"/>
<!-- 显示为：Save                    Ctrl+S -->

<MenuItem Header="Quit" InputGesture="Alt+F4"/>
<!-- 显示为：Quit                    Alt+F4 -->
```

InputGesture 只是视觉提示，不会自动绑定快捷键。实际快捷键绑定需要使用 `KeyBinding`。

### 17.7.4 子菜单

```xml
<ContextMenu>
    <MenuItem Header="Sort By">
        <MenuItem Header="Name" Command="{Binding SortByNameCommand}"
                  IsChecked="{Binding SortBy, Converter={x:Static converters:EqualityConverter.Instance},
                            ConverterParameter=Name}"/>
        <MenuItem Header="Date" Command="{Binding SortByDateCommand}"
                  IsChecked="{Binding SortBy, Converter={x:Static converters:EqualityConverter.Instance},
                            ConverterParameter=Date}"/>
        <MenuItem Header="Size" Command="{Binding SortBySizeCommand}"
                  IsChecked="{Binding SortBy, Converter={x:Static converters:EqualityConverter.Instance},
                            ConverterParameter=Size}"/>
    </MenuItem>
</ContextMenu>
```

### 17.7.5 禁用菜单项

```xml
<MenuItem Header="Paste" Command="{Binding PasteCommand}"
          IsEnabled="{Binding CanPaste}"/>

<!-- 或通过 Command 的 CanExecute 自动禁用 -->
<MenuItem Header="Undo" Command="{Binding UndoCommand}"/>
<!-- 当 UndoCommand.CanExecute 返回 false 时自动禁用 -->
```

## 17.8 Popup 组件详解

### 17.8.1 Popup 基础

Popup 是最底层的弹出层组件，Flyout 和 ToolTip 都基于它实现：

```xml
<Grid>
    <Button x:Name="PopupButton" Content="Toggle Popup"
            Click="OnTogglePopup"/>

    <Popup IsOpen="{Binding #PopupButton.IsChecked}"
           PlacementTarget="{Binding #PopupButton}"
           PlacementMode="Bottom"
           IsLightDismissEnabled="True"
           WindowManagerAddShadowHint="True">
        <Border Background="White" BorderBrush="Gray"
                BorderThickness="1" CornerRadius="8"
                Padding="16" Width="250">
            <StackPanel Spacing="8">
                <TextBlock Text="Popup Content" FontWeight="Bold"/>
                <TextBlock Text="This is a popup that appears below the button."/>
                <Button Content="Close" Click="OnClosePopup"/>
            </StackPanel>
        </Border>
    </Popup>
</Grid>
```

### 17.8.2 Popup PlacementMode

| 值 | 说明 |
|------|------|
| `Bottom` | 在目标下方（默认） |
| `Top` | 在目标上方 |
| `Left` | 在目标左侧 |
| `Right` | 在目标右侧 |
| `Full` | 覆盖目标 |
| `TopEdgeAlignedLeft` | 上方，左对齐 |
| `TopEdgeAlignedRight` | 上方，右对齐 |
| `BottomEdgeAlignedLeft` | 下方，左对齐 |
| `BottomEdgeAlignedRight` | 下方，右对齐 |
| `LeftEdgeAlignedTop` | 左侧，上对齐 |
| `LeftEdgeAlignedBottom` | 左侧，下对齐 |
| `RightEdgeAlignedTop` | 右侧，上对齐 |
| `RightEdgeAlignedBottom` | 右侧，下对齐 |
| `AnchorAndGravity` | 使用自定义锚点和重力 |
| `Pointer` | 在指针位置 |

### 17.8.3 Popup 关键属性

```xml
<Popup x:Name="MyPopup"
       IsOpen="False"
       PlacementMode="Bottom"
       PlacementTarget="{Binding #TargetButton}"
       IsLightDismissEnabled="True"
       WindowManagerAddShadowHint="True"
       MaxWidth="300"
       MaxHeight="400"
       HorizontalOffset="10"
       VerticalOffset="5">
    <!-- 内容 -->
</Popup>
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsOpen` | `bool` | 是否打开 |
| `PlacementMode` | `PlacementMode` | 定位方式 |
| `PlacementTarget` | `Control` | 定位目标控件 |
| `IsLightDismissEnabled` | `bool` | 点击外部是否自动关闭 |
| `WindowManagerAddShadowHint` | `bool` | 是否添加阴影 |
| `HorizontalOffset` | `double` | 水平偏移 |
| `VerticalOffset` | `double` | 垂直偏移 |
| `Topmost` | `bool` | 是否始终在最上层 |

### 17.8.4 Popup vs Flyout vs Dialog

| 特性 | Popup | Flyout | Dialog |
|------|-------|--------|--------|
| 背景遮挡 | 否 | 否 | 是 |
| 焦点 | 可选 | 自动 | 通常锁定 |
| 关闭方式 | 点击外部/代码 | 点击外部/代码 | 需要显式关闭 |
| 定位 | 相对于目标 | 相对于目标 | 居中/全屏 |
| 用途 | 自定义弹出 | 工具提示、菜单 | 表单、确认框 |
| MVVM | 需手动管理 | 可绑定 | 可绑定 |
| 复杂度 | 高 | 中 | 中 |

## 17.9 Overlay 层与 ZIndex

### 17.9.1 ZIndex 控制层叠顺序

```xml
<Grid>
    <!-- 底层内容 -->
    <Border Background="White" Panel.ZIndex="0">
        <TextBlock Text="Main Content"/>
    </Border>

    <!-- 中间层（浮动工具栏） -->
    <Border Background="LightBlue" Panel.ZIndex="10"
            HorizontalAlignment="Right" VerticalAlignment="Top">
        <TextBlock Text="Toolbar"/>
    </Border>

    <!-- 顶层（对话框遮罩） -->
    <Border Background="#80000000" Panel.ZIndex="100"
            IsVisible="{Binding IsDialogOpen}">
        <!-- 对话框内容 -->
    </Border>
</Grid>
```

### 17.9.2 动态管理 Overlay

```csharp
// 创建动态 Overlay 层
public class OverlayManager
{
    private readonly Panel _overlayContainer;
    private readonly Stack<Control> _overlayStack = new();

    public void ShowOverlay(Control overlay)
    {
        _overlayStack.Push(overlay);
        Panel.SetZIndex(overlay, 100 + _overlayStack.Count);
        _overlayContainer.Children.Add(overlay);
    }

    public void HideOverlay(Control overlay)
    {
        _overlayContainer.Children.Remove(overlay);
        if (_overlayStack.Peek() == overlay)
            _overlayStack.Pop();
    }

    public void HideTopOverlay()
    {
        if (_overlayStack.Count > 0)
        {
            var top = _overlayStack.Pop();
            _overlayContainer.Children.Remove(top);
        }
    }
}
```

### 17.9.3 多对话框管理

```xml
<Grid>
    <!-- 主内容 -->
    <ContentPresenter/>

    <!-- 对话框层（按 ZIndex 排序） -->
    <dialogs:ProviderEditorDialog Panel.ZIndex="100"
                                   IsVisible="{Binding IsProviderDialogOpen}"/>
    <dialogs:ModelEditorDialog Panel.ZIndex="101"
                                IsVisible="{Binding IsModelDialogOpen}"/>
    <dialogs:DeleteConfirmDialog Panel.ZIndex="102"
                                  IsVisible="{Binding IsDeleteDialogOpen}"/>
</Grid>
```

## 17.10 常见对话框模式

### 17.10.1 确认对话框

```csharp
// ViewModel
[ObservableProperty]
private bool _isConfirmDialogOpen;

[ObservableProperty]
private string _confirmDialogTitle = "";

[ObservableProperty]
private string _confirmDialogMessage = "";

[ObservableProperty]
private string _confirmDialogConfirmText = "Confirm";

[ObservableProperty]
private string _confirmDialogCancelText = "Cancel";

[ObservableProperty]
private bool _isConfirmDialogDestructive;

private TaskCompletionSource<bool>? _confirmTcs;

public Task<bool> ShowConfirmAsync(string title, string message,
    bool destructive = false)
{
    ConfirmDialogTitle = title;
    ConfirmDialogMessage = message;
    IsConfirmDialogDestructive = destructive;
    IsConfirmDialogOpen = true;

    _confirmTcs = new TaskCompletionSource<bool>();
    return _confirmTcs.Task;
}

[RelayCommand]
private void ConfirmDialogAccept()
{
    IsConfirmDialogOpen = false;
    _confirmTcs?.TrySetResult(true);
}

[RelayCommand]
private void ConfirmDialogCancel()
{
    IsConfirmDialogOpen = false;
    _confirmTcs?.TrySetResult(false);
}
```

### 17.10.2 输入对话框

```csharp
// ViewModel
[ObservableProperty]
private bool _isInputDialogOpen;

[ObservableProperty]
private string _inputDialogTitle = "";

[ObservableProperty]
private string _inputDialogValue = "";

[ObservableProperty]
private string _inputDialogPlaceholder = "";

private TaskCompletionSource<string?>? _inputTcs;

public Task<string?> ShowInputAsync(string title, string placeholder,
    string defaultValue = "")
{
    InputDialogTitle = title;
    InputDialogPlaceholder = placeholder;
    InputDialogValue = defaultValue;
    IsInputDialogOpen = true;

    _inputTcs = new TaskCompletionSource<string?>();
    return _inputTcs.Task;
}

[RelayCommand]
private void InputDialogAccept()
{
    IsInputDialogOpen = false;
    _inputTcs?.TrySetResult(InputDialogValue);
}

[RelayCommand]
private void InputDialogCancel()
{
    IsInputDialogOpen = false;
    _inputTcs?.TrySetResult(null);
}
```

### 17.10.3 对话框服务模式

```csharp
// 接口定义
public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message,
        bool destructive = false);
    Task<string?> ShowInputAsync(string title, string placeholder,
        string defaultValue = "");
    Task<TResult?> ShowCustomDialogAsync<TViewModel, TResult>(
        TViewModel viewModel) where TViewModel : IDialogViewModel<TResult>;
}

// 在 ViewModel 中使用
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;

    [RelayCommand]
    private async Task DeleteProviderAsync(string id)
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "确认删除",
            "此操作不可撤销，确定要删除吗？",
            destructive: true);

        if (confirmed)
        {
            _providerService.Delete(id);
            RefreshProviderList();
        }
    }
}
```

## 17.11 对话框键盘交互

### 17.11.1 ESC 关闭和 Enter 确认

```csharp
// 在对话框控件中添加键盘处理
public class ConfirmDialog : CsDialog
{
    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                CloseDialog(false);
                e.Handled = true;
                break;

            case Key.Enter when !e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                CloseDialog(true);
                e.Handled = true;
                break;
        }
        base.OnKeyDown(e);
    }

    private void CloseDialog(bool result)
    {
        IsVisible = false;
        // 通知 ViewModel
    }
}
```

### 17.11.2 焦点陷阱

```csharp
// 确保 Tab 键只在对话框内循环
protected override void OnPreviewKeyDown(KeyEventArgs e)
{
    if (e.Key == Key.Tab)
    {
        var focusable = this.GetVisualDescendants()
            .OfType<InputElement>()
            .Where(x => x.Focusable && x.IsTabStop && x.IsEffectivelyVisible)
            .ToList();

        if (focusable.Count == 0) return;

        var current = FocusManager.GetFocusManager(this)?.GetFocusedElement();
        var currentIndex = focusable.IndexOf(current as InputElement ?? focusable[0]);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            // Shift+Tab: 上一个
            var prev = currentIndex > 0 ? focusable[currentIndex - 1] : focusable[^1];
            prev.Focus();
        }
        else
        {
            // Tab: 下一个
            var next = currentIndex < focusable.Count - 1
                ? focusable[currentIndex + 1]
                : focusable[0];
            next.Focus();
        }

        e.Handled = true;
    }
}
```

## 17.12 CodexSwitch 实战：完整对话框系统

### 17.12.1 ProviderEditorDialog

CodexSwitch 的 Provider 编辑对话框是一个完整的表单对话框：

```xml
<!-- ProviderEditorDialog.axaml -->
<ui:CsDialog IsVisible="{Binding IsProviderDialogOpen}">
    <Border Classes="dialog-card" MaxWidth="600" MaxHeight="80vh">
        <Grid RowDefinitions="Auto,*,Auto">
            <!-- 标题栏 -->
            <Grid ColumnDefinitions="*,Auto" Margin="0,0,0,16">
                <TextBlock Text="{Binding ProviderDialogTitle}"
                           FontSize="18" FontWeight="Bold"/>
                <ui:CodexIconButton Grid.Column="1"
                                     Command="{Binding CancelProviderCommand}"
                                     Variant="Ghost">
                    <lucide:LucideIcon Kind="X" Size="16"/>
                </ui:CodexIconButton>
            </Grid>

            <!-- 表单内容 -->
            <ScrollViewer Grid.Row="1">
                <StackPanel Spacing="12">
                    <!-- Name -->
                    <StackPanel Spacing="4">
                        <TextBlock Text="Name" FontWeight="SemiBold"/>
                        <TextBox Text="{Binding SelectedProviderName}"/>
                    </StackPanel>

                    <!-- Base URL -->
                    <StackPanel Spacing="4">
                        <TextBlock Text="Base URL" FontWeight="SemiBold"/>
                        <TextBox Text="{Binding SelectedBaseUrl}"
                                 Watermark="https://api.example.com"/>
                    </StackPanel>

                    <!-- API Key -->
                    <StackPanel Spacing="4">
                        <TextBlock Text="API Key" FontWeight="SemiBold"/>
                        <TextBox Text="{Binding SelectedApiKey}"
                                 PasswordChar="*"/>
                    </StackPanel>

                    <!-- Protocol -->
                    <StackPanel Spacing="4">
                        <TextBlock Text="Protocol" FontWeight="SemiBold"/>
                        <ComboBox ItemsSource="{Binding ProtocolOptions}"
                                  SelectedItem="{Binding SelectedProtocol}"/>
                    </StackPanel>
                </StackPanel>
            </ScrollViewer>

            <!-- 操作按钮 -->
            <StackPanel Grid.Row="2" Orientation="Horizontal"
                        HorizontalAlignment="Right" Spacing="8"
                        Margin="0,16,0,0">
                <Button Classes="outline"
                        Content="Cancel"
                        Command="{Binding CancelProviderCommand}"/>
                <Button Content="Save"
                        Command="{Binding SaveProviderCommand}"/>
            </StackPanel>
        </Grid>
    </Border>
</ui:CsDialog>
```

### 17.12.2 对话框动画效果

```xml
<!-- CsDialog 的淡入动画 -->
<Style Selector="ui|CsDialog">
    <Setter Property="Transitions">
        <Transitions>
            <DoubleTransition Property="Opacity" Duration="0:0:0.2"/>
        </Transitions>
    </Setter>
</Style>

<!-- 对话框卡片的缩放动画 -->
<Style Selector="ui|CsDialog Border.dialog-card">
    <Setter Property="RenderTransformOrigin" Value="0.5,0.5"/>
    <Setter Property="Transitions">
        <Transitions>
            <DoubleTransition Property="Opacity" Duration="0:0:0.15"/>
            <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.15"/>
        </Transitions>
    </Setter>
</Style>
```

### 17.12.3 MainWindow 中的对话框布局

```xml
<!-- MainWindow.axaml 中的对话框层 -->
<Grid>
    <!-- 侧边栏 -->
    <ui:CodexSidebar Grid.Column="0"/>

    <!-- 主内容区 -->
    <Grid Grid.Column="1">
        <pages:HomePage IsVisible="{Binding IsHomePageVisible}"/>
        <pages:ProvidersPage IsVisible="{Binding IsProvidersPageVisible}"/>
        <!-- ... 其他页面 -->
    </Grid>

    <!-- 对话框覆盖层（跨列覆盖整个窗口） -->
    <dialogs:ProviderEditorDialog Grid.ColumnSpan="2"/>
    <dialogs:CodexAuthImportDialog Grid.ColumnSpan="2"/>
    <dialogs:ModelEditorDialog Grid.ColumnSpan="2"/>
    <dialogs:DeleteModelDialog Grid.ColumnSpan="2"/>
    <dialogs:DeleteProviderDialog Grid.ColumnSpan="2"/>
</Grid>
```

这种布局确保对话框覆盖整个窗口（包括侧边栏），提供完整的模态体验。

## 17.13 最佳实践

### 对话框设计原则

1. **明确对话框类型**：确认、输入、信息、选择，每种类型有不同的交互模式
2. **提供清晰的操作按钮**：主操作（确认/保存）和次操作（取消/关闭）要有视觉区分
3. **支持键盘操作**：ESC 关闭、Enter 确认、Tab 导航
4. **保持状态同步**：对话框关闭时清理所有临时状态
5. **避免嵌套对话框**：如果必须嵌套，确保 ZIndex 正确管理

### ToolTip 使用原则

1. **提供额外信息**：ToolTip 应该补充，而非替代主 UI 信息
2. **保持简洁**：ToolTip 内容应该简短，复杂内容使用 Flyout
3. **设置合理延迟**：默认 400ms，太短会干扰用户，太长会让人以为没有 ToolTip
4. **禁用控件时考虑是否显示**：如果禁用原因需要说明，设置 `ShowOnDisabled="True"`

### ContextMenu 使用原则

1. **只放相关操作**：上下文菜单应该只包含当前元素相关的操作
2. **使用分隔线分组**：相关操作放在一起，不同类别的操作用分隔线隔开
3. **提供快捷键提示**：`InputGesture` 属性显示快捷键，帮助用户学习
4. **使用图标**：常用操作配图标提高识别度

## 17.14 Deep Dive：Popup 的内部机制

### 17.14.1 Popup 的渲染层级

Popup 在 Avalonia 中使用独立的渲染层级。在大多数平台上，Popup 创建一个新的原生窗口（透明的），这确保了：

1. Popup 可以超出父窗口边界
2. Popup 的渲染不受父窗口裁剪
3. Popup 有独立的输入处理

```
主窗口 (Native Window)
├── 主内容渲染
└── Popup (另一个 Native Window 或 Overlay)
    └── Popup 内容渲染
```

### 17.14.2 IsLightDismissEnabled 的工作原理

当 `IsLightDismissEnabled="True"` 时：
1. Popup 监听全局的 PointerPressed 事件
2. 如果点击位置不在 Popup 内部，设置 `IsOpen = false`
3. 这个机制在所有平台上统一实现

```csharp
// 伪代码
private void OnGlobalPointerPressed(PointerPressedEventArgs e)
{
    if (!IsLightDismissEnabled) return;

    var hit = this.InputHitTest(e.GetPosition(this));
    if (hit == null)
    {
        IsOpen = false; // 点击外部，关闭
    }
}
```

### 17.14.3 Popup 的性能考量

- Popup 内容在 `IsOpen = false` 时可能仍然存在于可视化树中
- 如果 Popup 内容复杂，考虑在打开时动态创建，关闭时释放
- 避免在 Popup 中放置大量数据绑定，这会影响初始化性能

## 17.15 Cross References

- [第 6 章 MVVM 模式实战](06-mvvm-pattern.md) -- 对话框的 ViewModel 设计
- [第 10 章 动画与过渡效果](10-animation-transitions.md) -- 对话框的淡入动画
- [第 16 章 输入处理与事件系统](16-input-events.md) -- 右键菜单的实现、键盘事件
- [第 18 章 命令系统](18-commands.md) -- 菜单项和按钮的命令绑定
- [第 7 章 样式与主题系统](07-styling-theming.md) -- ToolTip 和 ContextMenu 的样式自定义

## 17.16 Common Pitfalls

1. **对话框关闭后不清理状态**：关闭对话框时必须重置所有临时状态（`_pendingDeleteId = null` 等），否则下次打开时可能残留旧数据。

2. **模态对话框不锁定焦点**：覆盖层对话框应该阻止用户与背景交互。可以通过设置 `IsHitTestVisible="True"` 在遮罩层上来实现。

3. **对话框嵌套导致 ZIndex 混乱**：多层对话框同时打开时，ZIndex 需要递增管理，否则后面的对话框可能被前面的遮挡。

4. **ToolTip 内容过长**：ToolTip 应该简短。如果需要显示大量信息，考虑使用 Flyout 或展开面板。

5. **Popup 的 IsLightDismissEnabled 与表单冲突**：如果 Popup 中有表单输入，点击 Popup 外部（如另一个输入框）会导致 Popup 关闭。对于这种情况，设置 `IsLightDismissEnabled="False"` 并提供显式关闭按钮。

6. **忘记设置 PlacementTarget**：Popup 的 `PlacementTarget` 默认为父控件。如果不设置，Popup 可能出现在意外位置。

7. **ContextMenu 中使用 InputGesture 误以为会自动绑定**：`InputGesture` 只是视觉提示文本，不会自动注册快捷键。需要额外使用 `KeyBinding` 实现实际的快捷键功能。

8. **对话框动画闪烁**：直接设置 `IsVisible` 可能导致闪烁。CodexSwitch 的 `CsDialog` 通过 `Dispatcher.UIThread.Post` 延迟设置 Opacity 来解决这个问题。

9. **Flyout 在 ListBox/ItemsControl 中的上下文问题**：在数据模板中使用 Flyout 时，`CommandParameter` 需要绑定到数据项本身（`{Binding}`），而非父 ViewModel。

10. **独立窗口对话框在 macOS 上的样式不一致**：`ShowDialog` 创建的是原生窗口，可能与应用的自定义样式不一致。考虑使用覆盖层对话框以保持视觉一致性。

## 17.17 Try It Yourself

1. **基础练习**：创建一个确认对话框，使用 `CsDialog` 模式，包含标题、消息、确认和取消按钮。确认按钮使用红色（destructive）样式。

2. **ToolTip 练习**：为一个列表中的每个项目创建动态 ToolTip，显示该项目的详细信息（名称、描述、创建时间）。

3. **ContextMenu 练习**：创建一个文件列表，右键弹出上下文菜单，包含"打开"、"重命名"、"删除"三个选项，删除选项使用红色文字。

4. **Flyout 练习**：创建一个设置按钮，点击弹出 Flyout，包含几个 RadioButton 和一个"应用"按钮。

5. **输入对话框练习**：实现一个输入对话框，包含一个 TextBox 和确认/取消按钮，使用 `TaskCompletionSource` 实现异步等待用户输入。

6. **多对话框练习**：在同一个页面中实现多个对话框（确认、输入、信息），确保它们的 ZIndex 正确，且同一时间只有一个对话框打开。

7. **对话框服务练习**：创建一个 `IDialogService` 接口和实现，将对话框逻辑从 ViewModel 中解耦，并编写单元测试。

8. **综合练习**：在 CodexSwitch 中找到 `ProviderEditorDialog`，研究它如何与 `MainWindowViewModel` 交互，然后为其添加 ESC 关闭和 Enter 保存的键盘快捷键支持。
