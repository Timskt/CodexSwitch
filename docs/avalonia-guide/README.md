# CodexSwitch Avalonia 开发完全指南

> 基于 CodexSwitch 项目的实战经验，从零基础到高级开发，全面掌握 Avalonia UI 桌面应用开发

## 项目概况

CodexSwitch 是一个基于 **Avalonia 12.0.3** + **.NET 10** 的跨平台桌面应用，集成了 ASP.NET Core 本地代理服务器。它是一个学习 Avalonia 开发的理想仓库，涵盖了从基础到高级的几乎所有核心知识点。

### 技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| Avalonia | 12.0.3 | 跨平台 UI 框架 |
| .NET | 10.0 | 运行时 |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM 框架 |
| Lucide.Avalonia | 0.2.6 | 图标库 |
| ASP.NET Core | (内嵌) | 本地代理服务器 |

### 项目结构

```
CodexSwitch/
├── CodexSwitch/                    # 主应用项目
│   ├── App.axaml / App.axaml.cs    # 应用入口与生命周期
│   ├── Program.cs                  # 启动配置
│   ├── Controls/                   # 45+ 自定义控件
│   ├── Views/                      # 页面与窗口
│   │   ├── MainWindow.axaml        # 主窗口 (侧边栏+内容+对话框)
│   │   ├── MiniStatusWindow.axaml  # 迷你悬浮窗
│   │   ├── Pages/                  # 8 个页面
│   │   ├── Dialogs/                # 5 个对话框
│   │   └── Shell/                  # 顶栏
│   ├── ViewModels/                 # ViewModel 层 (MVVM)
│   ├── Services/                   # 服务层 (33 个服务)
│   ├── Styles/                     # 主题与样式
│   │   ├── CodexTheme.axaml        # 主题定义 (70+ 色彩令牌)
│   │   └── Components/             # 20+ 组件样式
│   ├── I18n/                       # 国际化系统
│   ├── Models/                     # 数据模型
│   ├── Proxy/                      # ASP.NET Core 代理服务器
│   └── Assets/                     # 资源文件 (字体/图标/i18n)
├── CodexSwitchUI/                  # 组件库 (Git 子模块)
└── CodexSwitch.Tests/              # 单元测试
```

### 统计数据

- **43** 个 AXAML 文件 (UI 布局与样式)
- **45+** 自定义控件
- **33** 个服务类
- **70+** 设计色彩令牌
- **20+** 组件样式文件
- **3** 个国际化语言文件
- **42** 个文档章节

---

## 学习路线

### 入门篇 — 从零搭建第一个 Avalonia 应用 (第 1-5 章)

| # | 章节 | 核心内容 | 关键源文件 |
|---|------|---------|-----------|
| 1 | [Avalonia 框架概览](01-avalonia-overview.md) | 什么是 Avalonia、与 WPF/MAUI 对比、跨平台原理、渲染架构、开发环境搭建 | `Program.cs`, `CodexSwitch.csproj` |
| 2 | [项目结构与启动流程](02-project-structure.md) | csproj 配置、Program.cs 启动流程、App 生命周期、依赖注入、窗口创建 | `Program.cs`, `App.axaml.cs`, `CodexSwitch.csproj` |
| 3 | [AXAML 基础语法](03-axaml-fundamentals.md) | 命名空间、x: 指令、标记扩展、事件绑定、资源引用、AXAML 编译机制 | `MainWindow.axaml`, `App.axaml` |
| 4 | [布局系统](04-layout-system.md) | Grid/StackPanel/DockPanel/Canvas/WrapPanel、嵌套布局、对齐与边距 | `MainWindow.axaml`, `ProvidersPage.axaml` |
| 5 | [数据绑定](05-data-binding.md) | Binding 语法、单向/双向/单次绑定、FallbackValue、编译绑定概述 | `ProvidersPage.axaml`, `MainWindowViewModel.cs` |

### 进阶篇 — 构建真实应用 (第 6-10 章)

| # | 章节 | 核心内容 | 关键源文件 |
|---|------|---------|-----------|
| 6 | [MVVM 模式实战](06-mvvm-pattern.md) | CommunityToolkit.Mvvm、[ObservableProperty]、[RelayCommand]、ViewModel 通信 | `ViewModelBase.cs`, `MainWindowViewModel.cs` |
| 7 | [样式与主题系统](07-styling-theming.md) | 样式选择器、伪类、ControlTemplate、运行时主题切换、70+ 色彩令牌 | `CodexTheme.axaml`, `AppThemeService.cs` |
| 8 | [DataTemplate 深度解析](08-data-templates.md) | 数据模板、x:DataType、DataTemplateSelector、虚拟化、ItemsPresenter | `MainWindow.axaml`, 各 Page.axaml |
| 9 | [自定义控件开发](09-custom-controls.md) | StyledProperty、模板部件 (PART_)、PseudoClasses、AffectsMeasure/Render | `CsSegmentedControl.cs`, `CsDialog.cs` |
| 10 | [动画与过渡效果](10-animation-transitions.md) | DispatcherTimer、DoubleTransition、BrushTransition、缓动函数、关键帧 | `CsRollingNumber.cs`, `CsDialog.cs` |

### 高级篇 — 专业级技巧 (第 11-21 章)

| # | 章节 | 核心内容 | 关键源文件 |
|---|------|---------|-----------|
| 11 | [国际化 (i18n) 系统](11-i18n.md) | 自定义 MarkupExtension、IObservable 模式、avares:// 资源加载 | `TrExtension.cs`, `I18nService.cs` |
| 12 | [多窗口与系统托盘](12-multi-window-tray.md) | TrayIcon、NativeMenu、悬浮窗、屏幕感知定位 | `MiniStatusWindow.axaml.cs`, `TrayMenuController.cs` |
| 13 | [拖拽交互实现](13-drag-drop.md) | Pointer.Capture、TranslateTransform、命中测试、拖拽排序 | `ProvidersPage.axaml.cs` |
| 14 | [自定义渲染](14-custom-rendering.md) | Render(DrawingContext)、TextLayout、DrawText/DrawGeometry | `CsRollingNumber.cs`, `CsActivityArrow.cs` |
| 15 | [编译绑定与 AOT 发布](15-compiled-bindings.md) | 编译时绑定、Native AOT、TrimMode、源生成 JSON | `CodexSwitch.csproj` |
| 16 | [输入处理与事件系统](16-input-events.md) | 键盘/鼠标/指针事件、路由事件、手势识别 | `ProvidersPage.axaml.cs`, `MiniStatusWindow.axaml.cs` |
| 17 | [对话框与弹出层](17-dialogs-popups.md) | 模态/非模态对话框、Popup、ToolTip、Overlay 层 | `CsDialog.cs`, `MainWindow.axaml` |
| 18 | [命令系统](18-commands.md) | ICommand、RelayCommand、AsyncRelayCommand、命令参数 | `MainWindowViewModel.cs` |
| 19 | [值转换器](19-value-converters.md) | IValueConverter、BoolToBadgeVariant、MultiBinding 转换器 | `Converters/` 目录 |
| 20 | [跨平台适配](20-cross-platform.md) | Windows/macOS/Linux 差异、平台检测、条件编译 | `App.axaml.cs`, `Program.cs` |
| 21 | [调试与诊断](21-debugging.md) | DevTools、日志、性能分析、诊断工具 | 全项目 |

### 精通篇 — 框架内部原理 (第 22-25 章)

| # | 章节 | 核心内容 | 关键源文件 |
|---|------|---------|-----------|
| 22 | [Avalonia 属性系统](22-property-system.md) | StyledProperty/DirectProperty/AttachedProperty、属性优先级、存储机制 | `CsSegmentedButton.cs`, `CsRollingNumber.cs` |
| 23 | [视觉树与逻辑树](23-visual-logical-tree.md) | 两棵树的关系、遍历、坐标转换、命中测试 | `ProvidersPage.axaml.cs`, `CsSegmentedControl.cs` |
| 24 | [资源系统](24-resource-system.md) | avares://、资源字典、StaticResource vs DynamicResource、字体/图标资源 | `CodexTheme.axaml`, `App.axaml` |
| 25 | [Avalonia 与 ASP.NET Core 集成](25-aspnet-integration.md) | 嵌入 HTTP 服务器、WebSocket 代理、线程模型、生命周期管理 | `Proxy/` 目录 |

### 补充篇 — 控件与平台能力 (第 26-30 章)

| # | 章节 | 核心内容 | 关键源文件 |
|---|------|---------|-----------|
| 26 | [导航控件与页面路由](26-navigation-controls.md) | TabControl、Expander、Frame、NavigationView、ContentControl 导航 | `MainWindow.axaml` |
| 27 | [数据展示控件](27-data-controls.md) | DataGrid、TreeView、TreeDataGrid、虚拟化、ListBox、ComboBox | `ProvidersPage.axaml` |
| 28 | [高级输入控件](28-input-controls.md) | NumericUpDown、DatePicker、TimePicker、AutoCompleteBox、Slider、ProgressBar | 表单页面 |
| 29 | [形状与矢量绘图](29-shapes-drawing.md) | Line、Rectangle、Ellipse、Polygon、Path、PathMiniLanguage、Brush 类型 | 图标和装饰元素 |
| 30 | [媒体控件与无障碍](30-media-accessibility.md) | Image、MediaElement、AutomationProperties、键盘导航、高对比度 | 全项目 |

### 重量级应用篇 -- 系统集成与专业功能 (第 31-42 章)

| # | 章节 | 核心内容 | 关键源文件 |
|---|------|---------|-----------|
| 31 | [系统集成](31-system-integration.md) | 文件关联、协议处理器、Jump List、开机自启动、全局快捷键、剪切板高级操作、文件系统监听 | 全项目 |
| 32 | [窗口特效](32-window-effects.md) | 透明窗口、毛玻璃效果（Acrylic/Mica）、圆角窗口、阴影、穿透点击、窗口吸附、多显示器 | 全项目 |
| 33 | [WebView 集成](33-webview-integration.md) | 内嵌浏览器、OutSystems WebView、WebView2、NativeControlHost、OAuth 登录、Markdown 预览 | 全项目 |
| 34 | [本地数据存储](34-data-storage.md) | SQLite、Entity Framework Core、凭据管理（Windows/macOS/Linux）、配置文件管理、日志系统 | 全项目 |
| 35 | [自动更新与分发](35-auto-update-packaging.md) | Velopack、Native AOT 发布、安装包制作（Inno Setup/DMG/AppImage）、代码签名、CI/CD | 全项目 |
| 36 | [通知与任务栏](36-notifications-taskbar.md) | 应用内通知、系统原生通知（Toast/Notification Center）、任务栏进度条、ToolTip 高级用法 | 全项目 |
| 37 | [数据可视化](37-data-visualization.md) | LiveCharts2、ScottPlot、OxyPlot、实时数据图表、自定义图表控件、环形进度图 | 全项目 |
| 38 | [代码编辑器](38-code-editor.md) | AvaloniaEdit、TextMate 语法高亮、代码折叠、自动补全、搜索替换、Markdown 编辑器 | 全项目 |
| 39 | [测试](39-testing.md) | Avalonia.Headless、xUnit 集成、ViewModel 测试、UI 自动化测试、截图测试、集成测试 | 全项目 |
| 40 | [性能优化](40-performance.md) | 渲染管线、GPU 加速（Vulkan/Metal）、UI 虚拟化、编译绑定、内存管理、性能分析 | 全项目 |
| 41 | [网络通信](41-networking.md) | HttpClient、Refit、WebSocket、SignalR、gRPC、网络状态检测、代理配置 | 全项目 |
| 42 | [安全](42-security.md) | OAuth2/OIDC 认证、JWT Token 管理、数据加密、安全存储、输入验证、代码签名 | 全项目 |

---

## 如何使用本指南

### 按学习阶段

1. **初学者** (第 1-5 章): 从框架概览开始，依次阅读，理解 Avalonia 基础概念
2. **中级开发者** (第 6-10 章): 学习 MVVM 架构、样式系统、自定义控件开发
3. **高级开发者** (第 11-21 章): 掌握专业级技巧，如 i18n、拖拽、自定义渲染、AOT
4. **框架专家** (第 22-25 章): 深入理解属性系统、视觉树、资源系统等内部原理
5. **重量级应用开发** (第 31-42 章): 系统集成、窗口特效、WebView、数据库、自动更新、图表、编辑器、测试、性能优化、网络安全

### 按主题查找

| 你想学习 | 推荐章节 |
|---------|---------|
| 快速上手 | 第 1、2、3 章 |
| 布局与数据绑定 | 第 4、5 章 |
| MVVM 架构 | 第 6 章 |
| 样式与主题 | 第 7 章 |
| 自定义控件 | 第 9、14 章 |
| 动画效果 | 第 10 章 |
| 国际化 | 第 11 章 |
| 多窗口管理 | 第 12 章 |
| 拖拽交互 | 第 13 章 |
| 性能优化 | 第 15、21 章 |
| 输入处理 | 第 16、28 章 |
| 对话框 | 第 17 章 |
| 命令模式 | 第 18 章 |
| 跨平台开发 | 第 20 章 |
| 框架原理 | 第 22-25 章 |
| 导航与页面路由 | 第 26 章 |
| 数据表格与树形控件 | 第 27 章 |
| 矢量绘图与形状 | 第 29 章 |
| 无障碍与媒体 | 第 30 章 |
| 系统集成（文件关联、全局快捷键） | 第 31 章 |
| 窗口特效（透明、毛玻璃、圆角） | 第 32 章 |
| 内嵌浏览器 | 第 33 章 |
| 数据库与安全存储 | 第 34 章 |
| 自动更新与分发 | 第 35 章 |
| 通知与任务栏 | 第 36 章 |
| 数据可视化与图表 | 第 37 章 |
| 代码编辑器 | 第 38 章 |
| 测试策略 | 第 39 章 |
| 性能优化 | 第 40 章 |
| 网络通信 | 第 41 章 |
| 安全与认证 | 第 42 章 |

### 每章结构

每章包含以下部分：

- **概述**: 知识点介绍与应用场景
- **核心概念**: 详细的技术讲解
- **CodexSwitch 实战**: 来自真实项目的代码示例
- **Deep Dive**: 深入原理与内部机制
- **Cross References**: 与其他章节的关联
- **Common Pitfalls**: 常见错误与解决方案
- **Try It Yourself**: 动手实践练习

---

## MCP 工具

本项目已配置 Avalonia MCP 服务器，提供智能代码辅助：

```bash
# 已配置的 MCP 服务器
avalonia: dotnet run --project /Users/sky/code/tools/AvaloniaUI.MCP/src/AvaloniaUI.MCP/AvaloniaUI.MCP.csproj -c Release
```

详见 [MCP 配置说明](avalonia-mcp-config.md)

---

## 贡献指南

欢迎贡献新的章节或改进现有内容：

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/new-chapter`)
3. 提交更改 (`git commit -m 'Add new chapter: XXX'`)
4. 推送到分支 (`git push origin feature/new-chapter`)
5. 创建 Pull Request

### 章节命名规范

- 文件名: `XX-topic-name.md` (XX 为两位数字)
- 标题: `# XX. 主题名称`
- 确保包含所有标准部分 (概述、核心概念、实战、Deep Dive、Cross References、Common Pitfalls、Try It Yourself)

---

## 许可证

本指南基于 CodexSwitch 项目，遵循项目许可证。
