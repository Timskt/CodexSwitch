# 1. Avalonia 框架概览

## 1.1 什么是 Avalonia

Avalonia 是一个跨平台的 .NET UI 框架，类似于 WPF，但可以在 Windows、macOS、Linux、iOS、Android、WebAssembly 等多个平台上运行。它使用 XAML 的变体 AXAML 作为标记语言，支持 MVVM 模式，并且完全开源（MIT 许可证）。

### 核心理念

- **跨平台**: 一套代码，多平台运行
- **XAML 驱动**: 使用 AXAML 声明式 UI
- **MVVM 友好**: 内置对 MVVM 模式的支持
- **高性能**: 支持硬件加速渲染
- **可扩展**: 丰富的控件库和自定义能力

### Avalonia 的历史与起源

Avalonia 最初由 Steven Kirk 于 2013 年发起，项目名为 "Perspex"（后更名为 Avalonia）。它的设计目标是创建一个真正跨平台的 WPF 替代品，而不是像 Xamarin 或 MAUI 那样依赖平台原生控件。Avalonia 的名字来源于 Ava Lindstrom（一位虚构的角色），也暗示了 "avalanche"（雪崩）的概念——象征着它带来的技术革新。

从 0.x 版本的实验性项目，到 11.0 的正式稳定版，再到如今的 12.x，Avalonia 已经成为 .NET 生态中最成熟的跨平台 UI 框架之一。它的社区活跃度持续增长，GitHub star 数已经超过 25,000。

## 1.2 Avalonia vs WPF vs MAUI

| 特性 | Avalonia | WPF | MAUI |
|------|----------|-----|------|
| 跨平台 | 是 | 否 | 是 |
| XAML 方言 | AXAML | XAML | XAML |
| 渲染引擎 | Skia | DirectX | 平台原生 |
| 支持平台 | Win/macOS/Linux/iOS/Android/WASM | Windows | Win/macOS/iOS/Android |
| 开源 | 是 | 否 | 是 |
| MVVM 框架 | CommunityToolkit.Mvvm 或 ReactiveUI | CommunityToolkit.Mvvm 或 ReactiveUI | CommunityToolkit.Mvvm |
| 绑定系统 | 编译绑定 + 运行时绑定 | 运行时绑定 | 编译绑定 |
| 模板系统 | ControlTemplate | ControlTemplate | ControlTemplate |
| 样式系统 | CSS-like 选择器 | 基于类型的 Style | 基于类型的 Style |
| 性能 | 高（Skia 硬件加速） | 高（DirectX） | 中等 |
| 学习曲线 | 中等 | 中等 | 较高 |
| 社区规模 | 增长中 | 大 | 大 |
| 企业采用 | 逐渐增加 | 广泛 | 逐渐增加 |

### 深入对比：渲染策略的哲学差异

**WPF 的方式**：WPF 使用 DirectX 进行硬件加速渲染，但它只在 Windows 上运行。WPF 的渲染管线包括：UI 线程（构建视觉树）→ 渲染线程（将视觉树转换为渲染指令）→ DirectX（执行 GPU 渲染）。

**MAUI 的方式**：MAUI 采用"原生控件"策略——在每个平台上使用平台原生的 UI 控件。这意味着你的按钮在 iOS 上是 UIButton，在 Android 上是 android.widget.Button。优点是外观完全符合平台规范，缺点是跨平台行为不一致，调试困难。

**Avalonia 的方式**：Avalonia 使用 Skia 作为统一的渲染引擎，在所有平台上绘制像素完全相同的 UI。这类似于 Flutter 的策略，但 Avalonia 使用的是 .NET 生态系统，而不是 Dart。这种"像素级一致性"是 Avalonia 最大的优势之一。

### 什么时候选择 Avalonia

- **选择 Avalonia**：需要真正的跨平台一致性、WPF 开发者迁移、需要 Linux 支持、需要 WebAssembly 支持
- **选择 WPF**：只在 Windows 上运行、需要最成熟的生态系统、需要大量第三方控件库
- **选择 MAUI**：需要原生平台外观、需要深度平台集成（如 iOS 的 HealthKit）、团队已有 Xamarin 经验

## 1.3 Avalonia 的优势

### 跨平台一致性

Avalonia 使用 Skia 作为渲染引擎，确保在所有平台上 UI 外观完全一致。不像 MAUI 那样使用平台原生控件，Avalonia 在每个平台上都绘制相同的 UI。

### 编译绑定

Avalonia 支持编译时绑定检查，可以在编译期发现绑定错误，而不是运行时。这大大提高了代码质量和开发效率。

### CSS-like 样式系统

Avalonia 的样式系统类似于 CSS，支持选择器、伪类、继承等特性，比 WPF 的样式系统更灵活。

### Native AOT 支持

Avalonia 支持 Native AOT 编译，可以生成无依赖的原生可执行文件，启动速度快，内存占用小。

## 1.4 Avalonia 的架构

```
┌─────────────────────────────────────────────┐
│                Application                  │
├─────────────────────────────────────────────┤
│              AXAML Parser                   │
├─────────────────────────────────────────────┤
│           Control Framework                 │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐       │
│  │  Layout  │ │  Style  │ │ Binding │       │
│  └─────────┘ └─────────┘ └─────────┘       │
├─────────────────────────────────────────────┤
│            Rendering Engine                 │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐       │
│  │  Skia   │ │ Direct2D│ │ OpenGL  │       │
│  └─────────┘ └─────────┘ └─────────┘       │
├─────────────────────────────────────────────┤
│           Platform Abstraction              │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐       │
│  │Windows  │ │ macOS   │ │  Linux  │       │
│  └─────────┘ └─────────┘ └─────────┘       │
└─────────────────────────────────────────────┘
```

### 渲染管线深入解析

Avalonia 的渲染管线分为以下几个阶段：

1. **布局阶段（Layout Pass）**：遍历视觉树，调用 `MeasureOverride` 和 `ArrangeOverride` 计算每个控件的大小和位置。这是一个递归过程，从根节点开始，向下传播可用空间，然后向上汇聚所需空间。

2. **渲染阶段（Render Pass）**：遍历视觉树，收集所有需要绘制的指令（如绘制矩形、文本、图像等）。这些指令被转换为 Skia 绘图命令。

3. **合成阶段（Composition Pass）**：将多个渲染层合成到最终的帧缓冲区。Avalonia 11+ 引入了新的合成器（Compositor），支持硬件加速的层合成。

4. **提交阶段（Submit）**：将最终的帧缓冲区提交给操作系统显示。

### Skia 渲染引擎

Skia 是 Google 开发的 2D 图形库，被 Chrome、Android、Flutter 等项目使用。Avalonia 通过 SkiaSharp（Skia 的 .NET 封装）来使用 Skia。

Skia 的优势：
- **跨平台**：在所有平台上提供一致的渲染行为
- **硬件加速**：支持 GPU 加速渲染（通过 OpenGL、Vulkan、Metal）
- **高质量**：支持抗亚锯齿、子像素渲染、高质量文本渲染
- **性能**：针对 2D 图形渲染进行了高度优化

### 合成模型（Composition Model）

Avalonia 11+ 引入了新的合成模型，将渲染分为两个层次：

**视觉层（Visual Layer）**：
- 处理控件的布局、样式、绑定等高层逻辑
- 构建视觉树（Visual Tree）
- 管理控件的属性和状态

**渲染层（Rendering Layer）**：
- 将视觉树转换为渲染指令
- 使用 Skia 执行实际的像素绘制
- 管理渲染缓存和优化

这种分层设计使得 Avalonia 可以：
- 在不改变高层逻辑的情况下更换渲染后端
- 实现更高效的渲染缓存
- 支持硬件加速的动画和变换

## 1.5 Avalonia 的版本历史

| 版本 | 发布日期 | 主要特性 |
|------|---------|---------|
| 0.7 | 2018 | 首个稳定版本 |
| 0.9 | 2019 | 改进的样式系统 |
| 0.10 | 2021 | 稳定 API、改进性能 |
| 11.0 | 2023 | 重大更新、ThemeVariant、编译绑定改进 |
| 11.1 | 2024 | 改进的设计器支持、性能优化 |
| 12.0 | 2025 | 最新版本、更多平台支持 |

### 版本选择建议

对于新项目，强烈建议使用 Avalonia 11.0 或更高版本。这些版本提供了：
- 更稳定的 API
- 更好的编译绑定支持
- 改进的设计器体验
- 更好的 Native AOT 支持

## 1.6 开发环境搭建

### 必需工具

1. **.NET 10 SDK** — 运行时和编译工具
2. **IDE** — Visual Studio 2022、JetBrains Rider 或 VS Code
3. **Avalonia 扩展** — 提供 AXAML 设计器和智能提示

### 安装 Avalonia 模板

```bash
dotnet new install Avalonia.Templates
```

### 创建新项目

```bash
# 创建 Avalonia 应用
dotnet new avalonia.app -n MyApp

# 创建 Avalonia MVVM 应用
dotnet new avalonia.mvvm -n MyApp

# 创建 Avalonia 类库
dotnet new avalonia.lib -n MyLib
```

### IDE 配置建议

**Visual Studio 2022**：
- 安装 "Avalonia for Visual Studio 2022" 扩展
- 启用 XAML 热重载（如果支持）
- 配置 IntelliSense 使用 AXAML 语法

**JetBrains Rider**：
- 安装 "AvaloniaRider" 插件
- 启用 AXAML 设计器
- 配置代码分析使用 Avalonia 类型

**VS Code**：
- 安装 "Avalonia for VS Code" 扩展
- 配置 C# 扩展使用 .NET 10
- 启用 AXAML 语法高亮

## 1.7 CodexSwitch 项目概览

CodexSwitch 是一个学习 Avalonia 的理想项目，因为它涵盖了：

- **基础**: 项目结构、AXAML 语法、数据绑定
- **进阶**: MVVM 模式、样式系统、自定义控件
- **高级**: 动画、国际化、多窗口、拖拽、自定义渲染
- **专业**: 编译绑定、AOT 发布、性能优化

通过学习 CodexSwitch，你可以掌握 Avalonia 开发的方方面面。

### CodexSwitch 的架构亮点

CodexSwitch 展示了 Avalonia 在实际项目中的最佳实践：

1. **MVVM 模式**：使用 CommunityToolkit.Mvvm 实现清晰的职责分离
2. **编译绑定**：启用 `AvaloniaUseCompiledBindingsByDefault` 确保类型安全
3. **设计令牌系统**：70+ 色彩资源作为设计令牌，支持主题切换
4. **自定义控件**：20+ 自定义控件展示 Avalonia 的可扩展性
5. **国际化**：完整的 i18n 支持，使用自定义 MarkupExtension
6. **多窗口**：主窗口 + 迷你状态窗口，展示多窗口管理
7. **系统托盘**：完整的托盘菜单集成
8. **Native AOT**：支持 AOT 编译，生成无依赖的可执行文件

---

## Deep Dive

### Avalonia 的视觉树与逻辑树

Avalonia 有两种树结构：

**视觉树（Visual Tree）**：
- 包含所有参与渲染的元素
- 包括控件内部的模板元素
- 用于渲染、命中测试、布局

**逻辑树（Logical Tree）**：
- 包含开发者显式创建的元素
- 不包括模板内部元素
- 用于资源查找、数据绑定

例如，一个 `Button` 在逻辑树中只有一个节点，但在视觉树中可能包含 `Border`、`ContentPresenter` 等多个节点。

### Avalonia 的属性系统

Avalonia 使用 `AvaloniaProperty` 而不是普通的 CLR 属性。这提供了：

1. **样式支持**：属性可以通过样式设置
2. **绑定支持**：属性可以参与数据绑定
3. **动画支持**：属性可以被动画化
4. **继承支持**：属性值可以从父元素继承
5. **默认值**：属性可以有默认值
6. **变更通知**：属性变更时自动触发通知

```csharp
// 定义一个 Avalonia 属性
public static readonly StyledProperty<string> MyPropertyProperty =
    AvaloniaProperty.Register<MyControl, string>(nameof(MyProperty), "default");

public string MyProperty
{
    get => GetValue(MyPropertyProperty);
    set => SetValue(MyPropertyProperty, value);
}
```

### Avalonia 的平台抽象层

Avalonia 通过平台抽象层（Platform Abstraction Layer）支持多个平台：

- **Windows**：使用 Win32 API + DirectX/OpenGL
- **macOS**：使用 Cocoa API + Metal/OpenGL
- **Linux**：使用 X11/Wayland + OpenGL/Vulkan
- **iOS**：使用 UIKit + Metal
- **Android**：使用 Android SDK + OpenGL/Vulkan
- **WebAssembly**：使用 Canvas API

每个平台都实现了相同的接口，使得上层代码完全不需要关心平台差异。

### 性能优化策略

Avalonia 使用多种策略来优化性能：

1. **渲染缓存**：不常变化的控件会被缓存为位图
2. **脏区域检测**：只重绘发生变化的区域
3. **虚拟化**：对大量数据使用虚拟化面板
4. **编译绑定**：避免运行时反射开销
5. **延迟加载**：控件模板按需加载
6. **异步渲染**：渲染操作在后台线程执行

---

## Cross References

- **[第 2 章：项目结构](02-project-structure.md)** — 了解 Avalonia 项目的文件结构和构建流程
- **[第 3 章：AXAML 基础](03-axaml-fundamentals.md)** — 学习 AXAML 语法和 XAML 编译
- **[第 4 章：布局系统](04-layout-system.md)** — 理解 Avalonia 的测量和排列机制
- **[第 5 章：数据绑定](05-data-binding.md)** — 掌握 Avalonia 的绑定系统
- **[第 6 章：MVVM 模式](06-mvvm-pattern.md)** — 学习 MVVM 架构和 CommunityToolkit.Mvvm
- **[第 22 章：属性系统](22-property-system.md)** — 深入了解 AvaloniaProperty
- **[第 23 章：视觉树与逻辑树](23-visual-logical-tree.md)** — 理解两种树结构的差异

---

## Common Pitfalls

### 1. 混淆 Avalonia 和 WPF 的 XAML

**问题**：WPF 开发者经常使用 WPF 的命名空间和语法，导致编译错误。

```xml
<!-- 错误：使用 WPF 命名空间 -->
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">

<!-- 正确：使用 Avalonia 命名空间 -->
<Window xmlns="https://github.com/avaloniaui">
```

### 2. 忘记设置 x:DataType

**问题**：不设置 `x:DataType` 会导致绑定使用运行时反射，失去编译绑定的优势。

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

### 3. 在非 UI 线程更新 UI

**问题**：Avalonia 的 UI 操作必须在 UI 线程执行。

```csharp
// 错误：在后台线程更新 UI
Task.Run(() => {
    MyText = "Updated";  // 可能崩溃
});

// 正确：使用 Dispatcher
Dispatcher.UIThread.Post(() => {
    MyText = "Updated";
});
```

### 4. 忽略平台差异

**问题**：虽然 Avalonia 提供跨平台一致性，但某些平台特定的行为仍然存在差异。

```csharp
// 文件路径分隔符
// Windows: C:\Users\...
// macOS/Linux: /Users/...

// 正确：使用 Path.Combine
var path = Path.Combine("folder", "file.txt");
```

### 5. 过度使用动态资源

**问题**：`DynamicResource` 比 `StaticResource` 性能差，因为它需要监听资源变化。

```xml
<!-- 错误：对不会变化的资源使用 DynamicResource -->
<Button Background="{DynamicResource CsPrimaryBrush}"/>

<!-- 正确：对不会变化的资源使用 StaticResource -->
<Button Background="{StaticResource CsPrimaryBrush}"/>
```

---

## Try It Yourself

### 练习 1：创建你的第一个 Avalonia 项目

1. 安装 Avalonia 模板：
   ```bash
   dotnet new install Avalonia.Templates
   ```

2. 创建一个新的 MVVM 项目：
   ```bash
   dotnet new avalonia.mvvm -n MyFirstApp
   ```

3. 运行项目：
   ```bash
   cd MyFirstApp
   dotnet run
   ```

4. 修改 `MainWindow.axaml`，添加一个按钮：
   ```xml
   <Window x:DataType="vm:MainWindowViewModel">
       <StackPanel>
           <TextBlock Text="{Binding Greeting}" FontSize="24"/>
           <Button Content="Click Me" Command="{Binding ClickCommand}"/>
       </StackPanel>
   </Window>
   ```

5. 在 `MainWindowViewModel.cs` 中添加属性和命令：
   ```csharp
   [ObservableProperty]
   private string _greeting = "Hello, Avalonia!";

   [RelayCommand]
   private void Click()
   {
       Greeting = "Button Clicked!";
   }
   ```

### 练习 2：探索 CodexSwitch 的项目结构

1. 打开 CodexSwitch 项目：
   ```bash
   cd /path/to/CodexSwitch
   ```

2. 查看项目文件结构：
   ```bash
   find CodexSwitch -name "*.csproj" -o -name "*.axaml" -o -name "*.cs" | head -30
   ```

3. 阅读关键文件：
   - `CodexSwitch.csproj` — 项目配置
   - `Program.cs` — 入口点
   - `App.axaml` — 应用程序定义
   - `Views/MainWindow.axaml` — 主窗口布局

4. 运行项目并观察控制台输出：
   ```bash
   dotnet run --project CodexSwitch
   ```

### 练习 3：修改设计令牌

1. 打开 `Styles/CodexTheme.axaml`

2. 修改一个颜色令牌：
   ```xml
   <!-- 将主色调改为蓝色 -->
   <SolidColorBrush x:Key="CsPrimaryBrush" Color="#3B82F6"/>
   ```

3. 运行项目，观察 UI 变化

4. 尝试修改其他令牌：
   - `CsRadiusMd` — 圆角大小
   - `CsBackgroundBrush` — 背景色
   - `CsForegroundBrush` — 前景色

### 练习 4：添加新的 AXAML 页面

1. 在 `Views/Pages/` 目录下创建一个新文件 `TestPage.axaml`：
   ```xml
   <UserControl xmlns="https://github.com/avaloniaui"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                x:Class="CodexSwitch.Views.Pages.TestPage">
       <StackPanel>
           <TextBlock Text="This is a test page" FontSize="20"/>
           <Button Content="Go Back" />
       </StackPanel>
   </UserControl>
   ```

2. 在 `TestPage.axaml.cs` 中添加代码后置：
   ```csharp
   using Avalonia.Controls;

   namespace CodexSwitch.Views.Pages;

   public partial class TestPage : UserControl
   {
       public TestPage()
       {
           InitializeComponent();
       }
   }
   ```

3. 在 `MainWindowViewModel.cs` 中添加导航逻辑：
   ```csharp
   [ObservableProperty]
   private bool _isTestPageVisible;

   [RelayCommand]
   private void ShowTest()
   {
       IsTestPageVisible = true;
   }
   ```

4. 在 `MainWindow.axaml` 中添加页面：
   ```xml
   <pages:TestPage IsVisible="{Binding IsTestPageVisible}"/>
   ```

### 练习 5：调试 Avalonia 应用

1. 在 Visual Studio 或 Rider 中设置断点

2. 在 `MainWindowViewModel.cs` 的构造函数中添加断点：
   ```csharp
   public MainWindowViewModel()
   {
       // 在这里设置断点
       System.Diagnostics.Debug.WriteLine("ViewModel created");
   }
   ```

3. 以调试模式运行项目

4. 观察调试输出窗口，查看 Avalonia 的内部日志

5. 尝试在 `App.axaml.cs` 的 `OnFrameworkInitializationCompleted` 中设置断点，观察应用生命周期
