# 23. 视觉树与逻辑树

## 23.1 两棵树的关系

Avalonia 有两棵树：
- **视觉树 (Visual Tree)**: 实际渲染的控件层次
- **逻辑树 (Logical Tree)**: 逻辑上的控件层次

```
视觉树:                          逻辑树:
Window                           Window
├── Border                       ├── ContentControl
│   └── Grid                     │   └── TextBlock
│       ├── TextBlock
│       └── ContentPresenter
│           └── ContentControl
│               └── TextBlock
```

### 为什么有两棵树？

- **逻辑树**: 反映 AXAML 中声明的结构，用于资源查找、数据上下文继承
- **视觉树**: 反映实际渲染的结构，用于命中测试、布局、渲染

## 23.2 遍历视觉树

```csharp
// 向下遍历
var children = control.GetVisualChildren();      // 直接子元素
var descendants = control.GetVisualDescendants(); // 所有后代

// 向上遍历
var parent = control.GetVisualParent();    // 直接父元素
var ancestors = control.GetVisualAncestors(); // 所有祖先

// 查找特定类型
var buttons = control.GetVisualDescendants().OfType<Button>().ToList();

// 查找特定名称
var named = control.GetVisualDescendants()
    .OfType<Control>()
    .FirstOrDefault(c => c.Name == "PART_Root");

// 查找特定类
var providerRows = control.GetVisualDescendants()
    .OfType<Control>()
    .Where(c => c.Classes.Contains("provider-list-row"))
    .ToList();
```

## 23.3 遍历逻辑树

```csharp
var logicalChildren = control.GetLogicalChildren();
var logicalDescendants = control.GetLogicalDescendants();
```

### 视觉树 vs 逻辑树遍历

```csharp
// 视觉树包含模板内部元素
var templateChildren = control.GetVisualDescendants()
    .Where(v => v.TemplatedParent == control)
    .ToList();

// 逻辑树只包含声明的子元素
var logicalChildren = control.GetLogicalDescendants().ToList();
```

## 23.4 坐标转换

```csharp
// 将点从一个控件转换到另一个控件的坐标系
var point = source.TranslatePoint(new Point(0, 0), target);

// 在父控件中的位置
var topLeft = control.TranslatePoint(new Point(0, 0), parentControl);

// 屏幕坐标
var screenPoint = control.PointToScreen(new Point(0, 0));
```

### CodexSwitch 中的坐标转换

```csharp
// 在 SegmentedControl 中计算选中指示器位置
var topLeft = selected.TranslatePoint(new Point(0, 0), _selectionLayer);
if (topLeft is null) return;

var targetX = topLeft.Value.X;
var targetY = topLeft.Value.Y;
```

## 23.5 FindControl

```csharp
// 通过名称查找控件
var myButton = this.FindControl<Button>("MyButton");

// 在模板中查找
protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
{
    var part = e.NameScope.Find<Border>("PART_Root");
}
```

## 23.6 GetVisualAncestors 实战

```csharp
// 查找包含特定 class 的父元素
Control? FindProviderRow(Control source)
{
    if (source.Classes.Contains("provider-list-row"))
        return source;

    return source.GetVisualAncestors()
        .OfType<Control>()
        .FirstOrDefault(c => c.Classes.Contains("provider-list-row"));
}
```

---

## Deep Dive：视觉树的内部实现

### 命中测试 (Hit Testing)

当用户点击屏幕时，Avalonia 通过视觉树进行命中测试：

```
用户点击 (x, y)
    → 从根节点开始
    → 检查每个控件的 Bounds 是否包含 (x, y)
    → 如果包含，递归检查子元素
    → 返回最具体的控件
```

```csharp
// 手动命中测试
var hitControl = this.InputHitTest(new Point(x, y));
```

### 布局与视觉树

布局过程遍历视觉树：

```
Measure 阶段:
    从叶节点开始
    每个控件告诉父控件它需要多大空间
    父控件汇总子控件的需求

Arrange 阶段:
    从根节点开始
    父控件告诉每个子控件它被分配了多大空间
    子控件在分配的空间内排列
```

### 渲染与视觉树

渲染过程也遍历视觉树：

```
Render 阶段:
    从根节点开始
    按 ZIndex 和添加顺序渲染
    每个控件调用 Render(DrawingContext)
```

## Cross References

- [第 4 章 布局系统](04-layout-system.md) — 布局与视觉树的关系
- [第 9 章 自定义控件开发](09-custom-controls.md) — 模板与视觉树
- [第 16 章 输入处理与事件系统](16-input-events.md) — 事件路由与视觉树

## Common Pitfalls

1. **在模板中查找控件太早**: 需要在 `OnApplyTemplate` 中查找
2. **忽略坐标转换**: 不同控件的坐标系不同
3. **视觉树遍历性能**: 大量遍历可能影响性能

## Try It Yourself

1. 在 CodexSwitch 中找到 `FindProviderRow` 方法，理解视觉树遍历
2. 使用 `GetVisualDescendants()` 输出 MainWindow 的视觉树
3. 尝试使用 `TranslatePoint` 计算两个控件之间的相对位置
