# 17. 对话框与弹出层

## 17.1 对话框模式

CodexSwitch 使用覆盖层模式实现对话框：

```xml
<Grid>
    <!-- 主内容 -->
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="220"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <!-- 页面... -->

    <!-- 对话框覆盖层 -->
    <dialogs:ProviderEditorDialog Grid.ColumnSpan="2"/>
</Grid>
```

### 对话框架构选择

| 模式 | 优点 | 缺点 | 适用场景 |
|------|------|------|---------|
| 覆盖层（CodexSwitch） | 与主窗口共享 ViewModel，状态保持 | 布局复杂 | 应用内对话框 |
| 独立窗口 | 完全隔离，可拖拽到别处 | 需要窗口间通信 | 系统级对话框 |
| ContentControl 切换 | 简单 | 丢失主内容 | 简单表单 |

## 17.2 CsDialog 控件

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

        // 当变为可见时
        if (change.Property != IsVisibleProperty || change.NewValue is not bool isVisible || !isVisible)
            return;

        Opacity = 0;
        // 使用 Dispatcher.UIThread.Post 确保在渲染帧中执行
        Dispatcher.UIThread.Post(() => Opacity = 1, DispatcherPriority.Render);
    }
}
```

### 工作原理

1. `CsDialog` 初始 `Opacity=0`
2. 当 `IsVisible` 变为 `true` 时，先设置 `Opacity=0`
3. 然后通过 `Dispatcher.UIThread.Post` 在下一帧设置 `Opacity=1`
4. 配合样式中的 `DoubleTransition`，实现淡入效果

## 17.3 对话框样式

```xml
<Style Selector="ui|CsDialog">
    <Setter Property="Transitions">
        <Transitions>
            <DoubleTransition Property="Opacity" Duration="0:0:0.2"/>
        </Transitions>
    </Setter>
    <Setter Property="Template">
        <ControlTemplate>
            <!-- 半透明背景 -->
            <Border Background="#80000000">
                <!-- 对话框卡片 -->
                <Border Classes="dialog-card"
                        MaxWidth="600"
                        MaxHeight="80vh"
                        Background="{StaticResource CsDialogCardBrush}"
                        CornerRadius="{StaticResource CsRadiusLg}"
                        Padding="24">
                    <ContentPresenter/>
                </Border>
            </Border>
        </ControlTemplate>
    </Setter>
</Style>
```

## 17.4 确认对话框

```csharp
// ViewModel
[ObservableProperty]
private bool _isDeleteDialogOpen;

[ObservableProperty]
private string? _deleteTargetId;

[RelayCommand]
private void RequestDelete(string id)
{
    _deleteTargetId = id;
    IsDeleteDialogOpen = true;
}

[RelayCommand]
private void ConfirmDelete()
{
    if (_deleteTargetId is not null)
        _providerService.Delete(_deleteTargetId);
    IsDeleteDialogOpen = false;
}

[RelayCommand]
private void CancelDelete()
{
    IsDeleteDialogOpen = false;
}
```

```xml
<ui:CsDialog IsVisible="{Binding IsDeleteDialogOpen}">
    <StackPanel Spacing="16" Padding="24">
        <TextBlock Text="确认删除" FontWeight="Bold" FontSize="18"/>
        <TextBlock Text="此操作不可撤销，确定要删除吗？"/>
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Spacing="8">
            <Button Classes="outline" Content="取消"
                    Command="{Binding CancelDeleteCommand}"/>
            <Button Classes="destructive" Content="删除"
                    Command="{Binding ConfirmDeleteCommand}"/>
        </StackPanel>
    </StackPanel>
</ui:CsDialog>
```

## 17.5 Popup 控件

Popup 是轻量级的弹出层，不遮挡背景：

```xml
<Button x:Name="TriggerButton" Content="Show Popup">
    <Button.Flyout>
        <Flyout Placement="BottomEdgeAlignedLeft">
            <StackPanel Width="200">
                <TextBlock Text="Popup Content"/>
                <Button Content="Action"/>
            </StackPanel>
        </Flyout>
    </Button.Flyout>
</Button>
```

### Popup vs Dialog

| 特性 | Popup | Dialog |
|------|-------|--------|
| 背景遮挡 | 否 | 是 |
| 焦点 | 可选 | 通常锁定 |
| 关闭方式 | 点击外部 | 需要显式关闭 |
| 用途 | 下拉菜单、工具提示 | 表单、确认框 |

## 17.6 ToolTip

```xml
<!-- 简单文本 -->
<Button Content="Save" ToolTip.Tip="保存当前更改"/>

<!-- 复杂内容 -->
<Button Content="Info">
    <ToolTip.Tip>
        <StackPanel>
            <TextBlock Text="详细信息" FontWeight="Bold"/>
            <TextBlock Text="这里是详细描述"/>
        </StackPanel>
    </ToolTip.Tip>
</Button>
```

### ToolTip 配置

```xml
<Button Content="Hover me"
        ToolTip.Tip="Tooltip text"
        ToolTip.ShowDelay="500"        <!-- 显示延迟 -->
        ToolTip.Placement="Bottom"/>   <!-- 显示位置 -->
```

## 17.7 Flyout

Flyout 是更灵活的弹出层：

```xml
<Button Content="Options">
    <Button.Flyout>
        <MenuFlyout>
            <MenuItem Header="Edit" Command="{Binding EditCommand}"/>
            <MenuItem Header="Delete" Command="{Binding DeleteCommand}"/>
            <Separator/>
            <MenuItem Header="Properties" Command="{Binding PropertiesCommand}"/>
        </MenuFlyout>
    </Button.Flyout>
</Button>
```

---

## Deep Dive：对话框服务模式

在大型应用中，通常使用对话框服务来解耦对话框的显示逻辑：

```csharp
public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task<string?> ShowInputAsync(string title, string prompt, string defaultValue = "");
    Task<TResult?> ShowDialogAsync<TViewModel, TResult>(TViewModel viewModel) where TViewModel : IDialogViewModel<TResult>;
}

// ViewModel 中使用
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;

    [RelayCommand]
    private async Task DeleteProviderAsync(string id)
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "确认删除", "此操作不可撤销");

        if (confirmed)
            _providerService.Delete(id);
    }
}
```

这种模式使得 ViewModel 不直接依赖 UI，更容易测试。

## Cross References

- [第 6 章 MVVM 模式实战](06-mvvm-pattern.md) — 对话框的 ViewModel 设计
- [第 10 章 动画与过渡效果](10-animation-transitions.md) — 对话框的淡入动画
- [第 16 章 输入处理与事件系统](16-input-events.md) — 右键菜单的实现

## Common Pitfalls

1. **对话框关闭后不清理状态**: 确保关闭对话框时重置相关状态
2. **模态对话框不锁定焦点**: 用户可能意外与背景交互
3. **对话框嵌套**: 多层对话框的 Z-Index 管理

## Try It Yourself

1. 在 CodexSwitch 中找到 `ProviderEditorDialog`，研究它如何与 ViewModel 交互
2. 创建一个新的确认对话框，实现"确认/取消"模式
3. 尝试为对话框添加键盘快捷键（ESC 关闭，Enter 确认）
