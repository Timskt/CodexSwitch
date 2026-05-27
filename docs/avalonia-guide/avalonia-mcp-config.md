# Avalonia MCP 服务器配置

## 什么是 MCP

MCP (Model Context Protocol) 是 Anthropic 提出的开放协议，用于让 AI 助手连接外部工具和数据源。通过配置 Avalonia MCP 服务器，Claude Code 可以获得 Avalonia 开发的专业能力。

## 推荐的 MCP 服务器

### 1. AvaloniaUI.MCP（开发辅助）

**功能**：提供 AvaloniaUI 开发的企业级工具，包括项目脚手架、XAML 验证、安全模式、性能分析等。

**安装步骤**：

```bash
# 1. 克隆仓库
git clone https://github.com/decriptor/AvaloniaUI.MCP.git
cd AvaloniaUI.MCP

# 2. 构建
dotnet build

# 3. 测试（可选）
dotnet test
```

**Claude Code 配置**：

```bash
claude mcp add avalonia -- dotnet run --project /path/to/AvaloniaUI.MCP/src/AvaloniaUI.MCP/AvaloniaUI.MCP.csproj
```

**提供的工具**：
- `ProjectGeneratorTool` — 项目脚手架生成
- `XamlValidationTool` — XAML 语法检查
- `SecurityPatternTool` — 安全模式检测
- `ThemingTool` — 主题配置辅助
- `CustomControlGenerator` — 自定义控件生成
- `AnimationTool` — 动画配置辅助
- `LocalizationTool` — 国际化辅助
- `DiagnosticTool` — 诊断工具
- `PerformanceAnalysisTool` — 性能分析
- `ArchitectureTemplateTool` — 架构模板

### 2. AvaloniaMcp（运行时调试）

**功能**：连接运行中的 Avalonia 应用，进行实时检查、调试和交互。

**安装步骤**：

```bash
# 1. 克隆仓库
git clone https://github.com/adirh3/AvaloniaMcp.git
cd AvaloniaMcp

# 2. 安装为 dotnet tool
dotnet pack src/AvaloniaMcp.Server -o nupkg
dotnet new tool-manifest
dotnet tool install AvaloniaMcp --add-source ./nupkg
```

**应用集成**（在你的 Avalonia 项目中）：

```csharp
// 添加 NuGet 包引用后，在 Program.cs 中：
using AvaloniaMcp.Diagnostics;

public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .UseMcpDiagnostics()   // 添加这一行
        .LogToTrace();
```

**Claude Code 配置**：

```bash
claude mcp add avalonia-debug -- dotnet avalonia-mcp
```

**提供的工具（15 个）**：

| 工具 | 说明 |
|------|------|
| `list_windows` | 列出所有窗口 |
| `get_visual_tree` | 获取视觉树 |
| `get_logical_tree` | 获取逻辑树 |
| `find_control` | 搜索控件 |
| `get_control_properties` | 获取控件属性 |
| `get_data_context` | 获取 ViewModel 数据 |
| `get_binding_errors` | 获取绑定错误 |
| `take_screenshot` | 截图 |
| `get_applied_styles` | 获取应用的样式 |
| `get_resources` | 获取资源字典 |
| `get_focused_element` | 获取焦点元素 |
| `click_control` | 模拟点击 |
| `set_property` | 运行时修改属性 |
| `input_text` | 模拟输入文本 |
| `discover_apps` | 发现运行中的 Avalonia 应用 |

## 当前项目配置

本项目已通过 Claude Code 的 MCP 功能配置了 AvaloniaUI.MCP 服务器。

配置位于项目的 `.claude/settings.json` 文件中。
