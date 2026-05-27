# 34. Markdown 编辑器与富文本

> **写给零基础的你**：Markdown 是一种"用简单符号排版"的语言，比如用 `#` 表示标题，用 `**文字**` 加粗文字。它不需要鼠标点击格式按钮，纯打字就能排版，非常适合写文档、笔记、聊天消息。本章将教你如何在 Avalonia 应用里实现 Markdown 编辑器和实时预览。

## 34.1 概述

### 34.1.1 为什么需要 Markdown 编辑器

| 应用场景 | 示例 |
|---------|------|
| 文档编写 | README、API 文档、技术博客 |
| 笔记应用 | 个人知识库、学习笔记 |
| 聊天消息 | AI 对话渲染、客服系统 |
| 内容管理 | CMS 后台编辑器、论坛发帖 |
| 代码文档 | 注释渲染、代码审查 |
| 教学平台 | 课件编写、作业提交 |

### 34.1.2 Avalonia 中的 Markdown 方案总览

```
┌─────────────────────────────────────────────────────┐
│                  Markdown 方案                       │
├──────────────────┬──────────────────┬───────────────┤
│  Markdown.Avalonia│  WebView + HTML  │  自定义渲染器  │
│  （原生控件渲染）  │  （浏览器渲染）    │  （完全自定义） │
├──────────────────┼──────────────────┼───────────────┤
│  推荐方案         │  需要 WebView    │  工作量大      │
│  性能好           │  兼容性最好       │  灵活度最高    │
│  样式可控         │  支持 MathJax    │  深度定制      │
└──────────────────┴──────────────────┴───────────────┘
```

### 34.1.3 核心库依赖

```
Markdig                  -- Markdown 解析引擎（AST 生成）
Markdown.Avalonia        -- Avalonia 原生 Markdown 渲染控件
AvaloniaEdit             -- 代码/文本编辑器控件（语法高亮）
ColorTextBlock.Avalonia  -- 彩色文本渲染（可选）
```

## 34.2 Markdown 渲染方案

### 34.2.1 Markdown.Avalonia（推荐）

**Markdown.Avalonia** 是 Avalonia 生态中最成熟的 Markdown 渲染方案，它底层使用 **Markdig** 解析 Markdown，然后用 Avalonia 原生控件进行渲染，无需 WebView。

**安装：**

```xml
<!-- 在 .csproj 中添加 NuGet 引用 -->
<PackageReference Include="Markdown.Avalonia" Version="11.*" />
```

**MarkdownScrollViewer 基本用法：**

```xml
<!-- 在 AXAML 中使用 -->
<Window xmlns:md="clr-namespace:Markdown.Avalonia;assembly=Markdown.Avalonia">
    <md:MarkdownScrollViewer
        Markdown="{Binding DocumentText}"
        VerticalAlignment="Stretch"
        HorizontalAlignment="Stretch" />
</Window>
```

```csharp
// ViewModel
public class DocumentViewModel : ObservableObject
{
    private string _documentText = "";

    public string DocumentText
    {
        get => _documentText;
        set => SetProperty(ref _documentText, value);
    }

    public DocumentViewModel()
    {
        DocumentText = """
            # 欢迎使用 Markdown

            这是一个 **Markdown** 编辑器示例。

            ## 功能列表

            - 粗体和 *斜体*
            - [链接](https://example.com)
            - `行内代码`
            - 列表和表格

            ```csharp
            Console.WriteLine("Hello, World!");
            ```
            """;
    }
}
```

### 34.2.2 自定义 MarkdownScrollViewer 样式

```xml
<Window xmlns:md="clr-namespace:Markdown.Avalonia;assembly=Markdown.Avalonia">
    <md:MarkdownScrollViewer Markdown="{Binding DocumentText}">
        <!-- 通过样式自定义渲染外观 -->
        <md:MarkdownScrollViewer.Styles>
            <!-- 标题样式 -->
            <Style Selector="md|MarkdownScrollViewer TextBlock.h1">
                <Setter Property="FontSize" Value="28" />
                <Setter Property="FontWeight" Value="Bold" />
                <Setter Property="Foreground" Value="{DynamicResource AccentColor}" />
                <Setter Property="Margin" Value="0,16,0,8" />
            </Style>

            <Style Selector="md|MarkdownScrollViewer TextBlock.h2">
                <Setter Property="FontSize" Value="22" />
                <Setter Property="FontWeight" Value="Bold" />
                <Setter Property="Foreground" Value="{DynamicResource TextPrimary}" />
                <Setter Property="Margin" Value="0,14,0,6" />
            </Style>

            <Style Selector="md|MarkdownScrollViewer TextBlock.h3">
                <Setter Property="FontSize" Value="18" />
                <Setter Property="FontWeight" Value="SemiBold" />
                <Setter Property="Margin" Value="0,12,0,4" />
            </Style>

            <!-- 代码块样式 -->
            <Style Selector="md|MarkdownScrollViewer Border.codeblock">
                <Setter Property="Background" Value="#1E1E2E" />
                <Setter Property="CornerRadius" Value="6" />
                <Setter Property="Padding" Value="12" />
                <Setter Property="Margin" Value="0,8" />
            </Style>

            <!-- 引用块样式 -->
            <Style Selector="md|MarkdownScrollViewer Border.quoteblock">
                <Setter Property="BorderBrush" Value="#6C7086" />
                <Setter Property="BorderThickness" Value="3,0,0,0" />
                <Setter Property="Padding" Value="12,4" />
                <Setter Property="Margin" Value="0,8" />
                <Setter Property="Background" Value="#313244" />
            </Style>

            <!-- 表格样式 -->
            <Style Selector="md|MarkdownScrollViewer Border.md-table">
                <Setter Property="BorderBrush" Value="#585B70" />
                <Setter Property="BorderThickness" Value="1" />
                <Setter Property="CornerRadius" Value="4" />
            </Style>
        </md:MarkdownScrollViewer.Styles>
    </md:MarkdownScrollViewer>
</Window>
```

### 34.2.3 Markdown 样式 MarkdownStyle

```csharp
// 在代码中设置 MarkdownStyle
using Markdown.Avalonia;

public class MarkdownPreviewView : UserControl
{
    public MarkdownPreviewView()
    {
        var viewer = new MarkdownScrollViewer();

        // 使用内置主题
        viewer.MarkdownStyleName = "Standard";

        // 或者通过代码加载自定义样式
        // viewer.Styles.Add(new StyleInclude(...)
        // {
        //     Source = new Uri("avares://MyApp/Styles/MarkdownTheme.axaml")
        // });

        Content = viewer;
    }
}
```

### 34.2.4 Markdown 配置（Markdig Pipeline）

```csharp
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Extensions.Emoji;

// 创建自定义 Markdown 管道
public static class MarkdownPipelineFactory
{
    public static MarkdownPipeline Create()
    {
        return new MarkdownPipelineBuilder()
            // 启用所有 GFM 扩展
            .UseAdvancedExtensions()
            // 或者逐个启用
            .UsePipeTables()          // 表格
            .UseTaskLists()           // 任务列表
            .UseEmphasisExtras()      // 删除线、下标、上标
            .UseEmojiAndSmiley()      // 表情符号
            .UseAutoLinks()           // 自动链接
            .UseGenericAttributes()   // 自定义属性
            .UseAutoIdentifiers()     // 自动标题 ID
            .UseFootnotes()           // 脚注
            .UseMathematics()         // 数学公式
            .UseDiagrams()            // 图表
            .UseYamlFrontMatter()     // YAML 头信息
            .Build();
    }

    // 用于聊天消息的精简管道
    public static MarkdownPipeline CreateChatPipeline()
    {
        return new MarkdownPipelineBuilder()
            .UseEmphasisExtras()
            .UsePipeTables()
            .UseTaskLists()
            .UseAutoLinks()
            .UseEmojiAndSmiley()
            .DisableHtml()  // 禁用 HTML（安全考虑）
            .Build();
    }
}
```

```xml
<!-- 在 AXAML 中绑定自定义管道 -->
<md:MarkdownScrollViewer
    Markdown="{Binding Text}"
    MarkdownPipeline="{Binding Pipeline}" />
```

### 34.2.5 代码块高亮

Markdown.Avalonia 支持对代码块进行语法高亮。需要配合 **ColorTextBlock.Avalonia** 或内置高亮器。

```xml
<!-- 安装代码高亮支持 -->
<PackageReference Include="Markdown.Avalonia" Version="11.*" />
<PackageReference Include="ColorTextBlock.Avalonia" Version="11.*" />
```

```csharp
// 为代码块配置语法高亮
using Markdig;
using Markdig.SyntaxHighlighting;

public static class HighlightMarkdownPipeline
{
    public static MarkdownPipeline Create()
    {
        return new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseSyntaxHighlighting()  // 启用语法高亮扩展
            .Build();
    }
}
```

```markdown
<!-- Markdown 中的代码块会被高亮渲染 -->

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}
```

```json
{
  "name": "MyApp",
  "version": "1.0.0",
  "dependencies": {
    "Avalonia": "11.2.0"
  }
}
```

```python
def fibonacci(n):
    a, b = 0, 1
    for _ in range(n):
        yield a
        a, b = b, a + b
```
```

### 34.2.6 图片加载

```csharp
// 自定义图片加载器
using Markdown.Avalonia;

public class CustomImageLoader : IImageLoader
{
    public async Task<IImage?> LoadAsync(string url, CancellationToken ct)
    {
        // 从网络加载
        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            using var client = new HttpClient();
            var bytes = await client.GetByteArrayAsync(url, ct);
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }

        // 从本地文件加载
        if (File.Exists(url))
        {
            return new Bitmap(url);
        }

        // 从嵌入资源加载
        if (url.StartsWith("avares://"))
        {
            var asset = AssetLoader.Open(new Uri(url));
            return new Bitmap(asset);
        }

        return null;
    }
}
```

```xml
<!-- 使用自定义图片加载器 -->
<md:MarkdownScrollViewer
    Markdown="{Binding DocumentText}"
    ImageLoader="{Binding ImageLoader}" />
```

```csharp
// ViewModel 中设置图片加载器
public class DocumentViewModel : ObservableObject
{
    public IImageLoader ImageLoader { get; } = new CustomImageLoader();
}
```

### 34.2.7 表格渲染

Markdown.Avalonia 自动渲染 GFM 表格，支持对齐方式：

```markdown
<!-- 基本表格 -->
| 功能 | 状态 | 备注 |
|------|------|------|
| 标题 | 已完成 | H1-H6 |
| 列表 | 已完成 | 有序/无序 |
| 表格 | 进行中 | GFM 扩展 |
| 数学公式 | 计划中 | LaTeX 语法 |

<!-- 对齐方式 -->
| 左对齐 | 居中对齐 | 右对齐 |
|:-------|:--------:|-------:|
| 文本   |  文本    |   100  |
| 文本   |  文本    | 200.50 |
```

```xml
<!-- 自定义表格样式 -->
<Style Selector="md|MarkdownScrollViewer Grid.md-table">
    <Setter Property="Margin" Value="0,8" />
</Style>

<Style Selector="md|MarkdownScrollViewer Border.md-table-header">
    <Setter Property="Background" Value="{DynamicResource TableHeaderBg}" />
    <Setter Property="Padding" Value="8,6" />
</Style>

<Style Selector="md|MarkdownScrollViewer TextBlock.md-table-cell">
    <Setter Property="Padding" Value="8,4" />
    <Setter Property="FontSize" Value="14" />
</Style>

<Style Selector="md|MarkdownScrollViewer Border.md-table-row:nth-child(even)">
    <Setter Property="Background" Value="{DynamicResource AlternateRowBg}" />
</Style>
```

### 34.2.8 任务列表

```markdown
<!-- GitHub 风格任务列表 -->
## 项目进度

- [x] 搭建项目结构
- [x] 实现数据绑定
- [ ] 添加 Markdown 渲染
- [ ] 实现语法高亮
- [ ] 添加导出功能
```

```xml
<!-- 自定义任务列表样式 -->
<Style Selector="md|MarkdownScrollViewer CheckBox.task">
    <Setter Property="Margin" Value="4,2,8,2" />
    <Setter Property="Foreground" Value="{DynamicResource AccentColor}" />
</Style>
```

### 34.2.9 WebView 方式渲染

如果需要最完整的 Markdown 渲染效果（特别是数学公式），可以通过 WebView 将 Markdown 转换为 HTML 来渲染：

```csharp
// Markdown 转 HTML 渲染器
using Markdig;

public class MarkdownHtmlRenderer
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownHtmlRenderer()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseMathematics()
            .Build();
    }

    public string RenderToHtml(string markdown)
    {
        var htmlBody = Markdig.Markdown.ToHtml(markdown, _pipeline);
        return WrapInHtmlDocument(htmlBody);
    }

    private string WrapInHtmlDocument(string body)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8" />
                <style>
                    body {{
                        font-family: -apple-system, BlinkMacSystemFont,
                            "Segoe UI", Helvetica, Arial, sans-serif;
                        line-height: 1.6;
                        color: #cdd6f4;
                        background: #1e1e2e;
                        padding: 16px;
                        max-width: 800px;
                        margin: 0 auto;
                    }}
                    h1, h2, h3, h4, h5, h6 {{
                        color: #cba6f7;
                        border-bottom: 1px solid #45475a;
                        padding-bottom: 0.3em;
                    }}
                    code {{
                        background: #313244;
                        padding: 2px 6px;
                        border-radius: 4px;
                        font-family: "Cascadia Code", Consolas, monospace;
                    }}
                    pre {{
                        background: #313244;
                        padding: 16px;
                        border-radius: 8px;
                        overflow-x: auto;
                    }}
                    pre code {{
                        background: none;
                        padding: 0;
                    }}
                    blockquote {{
                        border-left: 4px solid #6c7086;
                        margin: 0;
                        padding: 0.5em 1em;
                        color: #a6adc8;
                    }}
                    table {{
                        border-collapse: collapse;
                        width: 100%;
                    }}
                    th, td {{
                        border: 1px solid #45475a;
                        padding: 8px 12px;
                    }}
                    th {{
                        background: #313244;
                    }}
                    a {{
                        color: #89b4fa;
                    }}
                    img {{
                        max-width: 100%;
                    }}
                </style>
                <!-- 生产环境务必生成并添加 integrity 哈希值，
                     运行: curl -s URL | openssl dgst -sha384 -binary | openssl base64 -A
                     然后填入 integrity="sha384-..." -->
                <script src="https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js"
                        crossorigin="anonymous"
                        referrerpolicy="no-referrer"></script>
            </head>
            <body>
                {body}
            </body>
            </html>
            """;
    }
}
```

```xml
<!-- 通过 WebView 显示 -->
<WebViewControl:WebView
    x:Name="MarkdownWebView"
    HorizontalAlignment="Stretch"
    VerticalAlignment="Stretch" />
```

```csharp
// 在代码中加载渲染后的 HTML
public partial class MarkdownPreview : Window
{
    private readonly MarkdownHtmlRenderer _renderer = new();

    public void UpdatePreview(string markdown)
    {
        var html = _renderer.RenderToHtml(markdown);
        MarkdownWebView.LoadHtml(html);
    }
}
```

### 34.2.10 自定义渲染器

当 Markdown.Avalonia 的默认渲染不能满足需求时，可以自定义渲染管线：

```csharp
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

// 自定义 HTML 渲染器 -- 为代码块添加复制按钮
public class CustomHtmlRenderer : HtmlRenderer
{
    public CustomHtmlRenderer(TextWriter writer) : base(writer)
    {
        // 替换默认的代码块渲染
        ObjectRenderers.Replace<CodeBlockRenderer>(new CodeBlockWithCopyRenderer());
    }
}

public class CodeBlockWithCopyRenderer : CodeBlockRenderer
{
    protected override void Write(HtmlRenderer renderer, CodeBlock obj)
    {
        var code = obj.Lines.ToString();
        var lang = (obj as FencedCodeBlock)?.Info ?? "text";

        renderer.Write($"""
            <div class="code-block">
                <div class="code-header">
                    <span class="lang">{lang}</span>
                    <button onclick="copyCode(this)">复制</button>
                </div>
                <pre><code class="language-{lang}">{EscapeHtml(code)}</code></pre>
            </div>
            """);
    }

    private static string EscapeHtml(string text)
        => text.Replace("&", "&amp;")
               .Replace("<", "&lt;")
               .Replace(">", "&gt;");
}
```

## 34.3 Markdown 语法完整参考

### 34.3.1 标题（H1-H6）

```markdown
# 一级标题
## 二级标题
### 三级标题
#### 四级标题
##### 五级标题
###### 六级标题
```

```csharp
// 标题演示文本
public const string HeadingDemo = """
    # 一级标题 -- 最大最醒目
    ## 二级标题 -- 章节标题
    ### 三级标题 -- 小节标题
    #### 四级标题 -- 常用小标题
    ##### 五级标题 -- 较少使用
    ###### 六级标题 -- 最小标题
    """;
```

### 34.3.2 段落和换行

```markdown
这是第一个段落。段落之间用一个空行分隔。

这是第二个段落。同一段落内需要在行尾加两个空格
才能换行。或者使用 HTML 的 <br> 标签。

另一种换行方式是使用反斜杠\
这样也能换行。
```

```csharp
// 段落演示文本
public const string ParagraphDemo = """
    这是一个段落。Markdown 会把连续的文本合并成一个段落。

    这是另一个段落。用空行分隔。

    如果想在段落内换行，
    可以在行尾加两个空格，
    或使用反斜杠\换行。
    """;
```

### 34.3.3 强调（粗体、斜体、删除线）

```markdown
*斜体文本*
_斜体文本_

**粗体文本**
__粗体文本__

***粗斜体文本***
___粗斜体文本___

~~删除线文本~~

这是 H~2~O（下标）
这是 E=mc^2^（上标）
```

```csharp
// 强调演示文本
public const string EmphasisDemo = """
    - *斜体* 或 _斜体_
    - **粗体** 或 __粗体__
    - ***粗斜体*** 或 ___粗斜体___
    - ~~删除线~~
    - H~2~O（下标，需启用 EmphasisExtras）
    - E=mc^2^（上标，需启用 EmphasisExtras）
    """;
```

### 34.3.4 列表（有序、无序、任务列表）

```markdown
<!-- 无序列表 -->
- 第一项
- 第二项
  - 嵌套项 A
  - 嵌套项 B
    - 更深层嵌套
- 第三项

<!-- 有序列表 -->
1. 第一步
2. 第二步
   1. 子步骤 A
   2. 子步骤 B
3. 第三步

<!-- 混合列表 -->
1. 安装依赖
   - Avalonia
   - Markdown.Avalonia
2. 配置项目
   - 添加引用
   - 设置主题
3. 运行应用

<!-- 任务列表 -->
- [x] 已完成的任务
- [ ] 待完成的任务
- [ ] 另一个待完成的任务
```

### 34.3.5 链接和图片

```markdown
<!-- 基本链接 -->
[Avalonia 官网](https://avaloniaui.net)

<!-- 带标题的链接 -->
[Avalonia 官网](https://avaloniaui.net "点击访问")

<!-- 自动链接 -->
<https://avaloniaui.net>

<!-- 引用式链接 -->
[ Avalonia 官网 ][ref]

[ref]: https://avaloniaui.net

<!-- 图片 -->
![Avalonia Logo](https://avares.org/logo.png)

<!-- 带标题的图片 -->
![Avalonia Logo](https://avares.org/logo.png "Avalonia UI")

<!-- 图片链接 -->
[![Alt text](image.png)](https://example.com)
```

### 34.3.6 代码（行内代码、代码块）

```markdown
<!-- 行内代码 -->
使用 `Console.WriteLine()` 输出。

<!-- 代码块 -->
```
这是一个代码块
没有语法高亮
```

<!-- 带语言标识的代码块 -->
```csharp
public class Hello
{
    public static void Main()
    {
        Console.WriteLine("Hello!");
    }
}
```

<!-- 带行号和高亮行标记（GFM 扩展） -->
```python{1,3-5}
def hello():
    print("Hello")
    x = 1
    y = 2
    return x + y
```
```

### 34.3.7 表格

```markdown
<!-- 基本表格 -->
| 名称 | 类型 | 说明 |
|------|------|------|
| Name | string | 姓名 |
| Age | int | 年龄 |
| Email | string | 邮箱 |

<!-- 对齐方式 -->
| 左对齐 | 居中 | 右对齐 |
|:-------|:----:|-------:|
| 文本 | 文本 | 12345 |
| 文本 | 文本 | 67890 |

<!-- 简化写法（只有一个列的表格） |
名称 |
------|
Avalonia |
WPF |
```

### 34.3.8 引用块

```markdown
<!-- 基本引用 -->
> 这是一段引用文字。

<!-- 多行引用 -->
> 这是第一行引用。
> 这是第二行引用。
>
> 这是引用中的新段落。

<!-- 嵌套引用 -->
> 第一层引用
>> 第二层引用
>>> 第三层引用

<!-- 引用中包含其他元素 -->
> ## 引用中的标题
>
> - 列表项 1
> - 列表项 2
>
> ```csharp
> // 引用中的代码块
> Console.WriteLine("Hello!");
> ```
```

### 34.3.9 水平线

```markdown
三种写法都可以创建水平线：

---

***

___
```

### 34.3.10 转义字符

```markdown
\*不是斜体\*
\# 不是标题
\[不是链接\](真的不是)
\`不是代码\`

需要转义的特殊字符：
\  反斜杆
`  反引号
*  星号
_  下划线
{} 花括号
[] 方括号
() 小括号
#  井号
+  加号
-  减号（连字符）
.  句点
!  感叹号
|  管道符
```

### 34.3.11 GFM 扩展语法

```markdown
<!-- 删除线（GFM） -->
~~已删除的文字~~

<!-- 自动链接 -->
https://github.com/user/repo

<!-- 表格（GFM） -->
| 功能 | 支持 |
|------|------|
| 表格 | Yes |

<!-- 任务列表（GFM） -->
- [x] 完成
- [ ] 未完成

<!-- 脚注（需启用扩展） -->
这里有一个脚注[^1]。

[^1]: 这是脚注的内容。

<!-- 定义列表（需启用扩展） -->
术语
:   定义说明

<!-- 缩写（需启用扩展））
*[HTML]: 超文本标记语言
HTML 是网页的基础。
```

## 34.4 实时预览编辑器

### 34.4.1 左右分栏布局

```xml
<!-- Markdown 编辑器 + 预览 的分栏布局 -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="clr-namespace:Markdown.Avalonia;assembly=Markdown.Avalonia"
             xmlns:ae="clr-namespace:AvaloniaEdit;assembly=AvaloniaEdit"
             x:Class="MyApp.Views.MarkdownEditorView">

    <Grid ColumnDefinitions="*,Auto,*">
        <!-- 左侧：编辑器 -->
        <Border Grid.Column="0"
                BorderBrush="{DynamicResource BorderColor}"
                BorderThickness="0,0,1,0">
            <DockPanel>
                <TextBlock DockPanel.Dock="Top"
                           Text="编辑"
                           FontWeight="Bold"
                           Padding="12,8"
                           Background="{DynamicResource HeaderBg}" />
                <ae:TextEditor
                    x:Name="Editor"
                    FontFamily="Cascadia Code,Consolas,Monaco,monospace"
                    FontSize="14"
                    ShowLineNumbers="True"
                    WordWrap="True"
                    SyntaxHighlighting="Markdown"
                    Text="{Binding MarkdownText, Mode=TwoWay}" />
            </DockPanel>
        </Border>

        <!-- 拖动分隔条 -->
        <GridSplitter Grid.Column="1"
                      Width="4"
                      Background="{DynamicResource BorderColor}"
                      HorizontalAlignment="Center" />

        <!-- 右侧：预览 -->
        <Border Grid.Column="2">
            <DockPanel>
                <TextBlock DockPanel.Dock="Top"
                           Text="预览"
                           FontWeight="Bold"
                           Padding="12,8"
                           Background="{DynamicResource HeaderBg}" />
                <md:MarkdownScrollViewer
                    Markdown="{Binding MarkdownText}"
                    MarkdownPipeline="{Binding Pipeline}" />
            </DockPanel>
        </Border>
    </Grid>
</UserControl>
```

### 34.4.2 同步滚动

```csharp
// 同步滚动实现
public class SyncScrollManager : IDisposable
{
    private readonly ScrollViewer _editorScroll;
    private readonly ScrollViewer _previewScroll;
    private bool _isSyncing;

    public SyncScrollManager(ScrollViewer editorScroll, ScrollViewer previewScroll)
    {
        _editorScroll = editorScroll;
        _previewScroll = previewScroll;

        _editorScroll.ScrollChanged += OnEditorScrolled;
        _previewScroll.ScrollChanged += OnPreviewScrolled;
    }

    private void OnEditorScrolled(object? sender, ScrollChangedEventArgs e)
    {
        if (_isSyncing) return;
        _isSyncing = true;

        // 按比例同步滚动
        var ratio = _editorScroll.Offset.Y /
                    Math.Max(1, _editorScroll.Extent.Height - _editorScroll.Viewport.Height);

        var targetOffset = ratio *
            (_previewScroll.Extent.Height - _previewScroll.Viewport.Height);

        _previewScroll.Offset = new Vector(0, targetOffset);

        _isSyncing = false;
    }

    private void OnPreviewScrolled(object? sender, ScrollChangedEventArgs e)
    {
        if (_isSyncing) return;
        _isSyncing = true;

        var ratio = _previewScroll.Offset.Y /
                    Math.Max(1, _previewScroll.Extent.Height - _previewScroll.Viewport.Height);

        var targetOffset = ratio *
            (_editorScroll.Extent.Height - _editorScroll.Viewport.Height);

        _editorScroll.Offset = new Vector(0, targetOffset);

        _isSyncing = false;
    }

    public void Dispose()
    {
        _editorScroll.ScrollChanged -= OnEditorScrolled;
        _previewScroll.ScrollChanged -= OnPreviewScrolled;
    }
}
```

```csharp
// 在控件中使用同步滚动
public partial class MarkdownEditorView : UserControl
{
    private SyncScrollManager? _scrollSync;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // 查找两个 ScrollViewer
        var editorScroll = FindEditorScrollViewer();
        var previewScroll = FindPreviewScrollViewer();

        if (editorScroll != null && previewScroll != null)
        {
            _scrollSync = new SyncScrollManager(editorScroll, previewScroll);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _scrollSync?.Dispose();
        base.OnDetachedFromVisualTree(e);
    }

    private ScrollViewer? FindEditorScrollViewer()
    {
        return Editor?.FindDescendantOfType<ScrollViewer>();
    }

    private ScrollViewer? FindPreviewScrollViewer()
    {
        return this.FindControl<ScrollViewer>("PreviewScroll");
    }
}
```

### 34.4.3 实时渲染（防抖）

```csharp
// 使用防抖避免频繁渲染
public class MarkdownEditorViewModel : ObservableObject
{
    private readonly DispatcherTimer _debounceTimer;
    private readonly MarkdownPipeline _pipeline;
    private string _markdownText = "";
    private string _renderedHtml = "";

    public MarkdownEditorViewModel()
    {
        _pipeline = MarkdownPipelineFactory.Create();

        // 300ms 防抖
        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _debounceTimer.Tick += OnDebounceTimerTick;
    }

    public string MarkdownText
    {
        get => _markdownText;
        set
        {
            if (SetProperty(ref _markdownText, value))
            {
                // 每次输入都重置定时器
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        }
    }

    private void OnDebounceTimerTick(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        try
        {
            // Markdown.Avalonia 的 MarkdownScrollViewer 会自动渲染
            // 这里主要用于需要转换 HTML 的场景
            RenderedHtml = Markdig.Markdown.ToHtml(_markdownText, _pipeline);
        }
        catch (Exception ex)
        {
            RenderedHtml = $"<p style='color:red'>渲染错误: {ex.Message}</p>";
        }
    }

    public string RenderedHtml
    {
        get => _renderedHtml;
        private set => SetProperty(ref _renderedHtml, value);
    }
}
```

### 34.4.4 编辑器控件选择

| 控件 | 语法高亮 | 代码折叠 | 多光标 | 适用场景 |
|------|---------|---------|--------|---------|
| TextBox | 无 | 无 | 无 | 简单编辑 |
| AvaloniaEdit | 有 | 有 | 有 | 代码/Markdown 编辑 |
| RichTextBox | 有 | 无 | 无 | 富文本编辑 |

```xml
<!-- 方案 1：简单 TextBox -->
<TextBox Text="{Binding MarkdownText}"
         AcceptsReturn="True"
         TextWrapping="Wrap"
         FontFamily="Cascadia Code,Consolas,monospace"
         FontSize="14" />

<!-- 方案 2：AvaloniaEdit（推荐） -->
<AvaloniaEdit:TextEditor
    x:Name="Editor"
    Text="{Binding MarkdownText, Mode=TwoWay}"
    SyntaxHighlighting="Markdown"
    ShowLineNumbers="True"
    FontFamily="Cascadia Code,Consolas,monospace"
    FontSize="14"
    WordWrap="True"
    Background="#1E1E2E"
    Foreground="#CDD6F4"
    LineNumberForeground="#6C7086" />
```

### 34.4.5 快捷键支持

```csharp
// Markdown 编辑器快捷键
public class MarkdownEditorKeyBindings
{
    private readonly TextEditor _editor;

    public MarkdownEditorKeyBindings(TextEditor editor)
    {
        _editor = editor;

        // Ctrl+B: 粗体
        AddKeyBinding(Key.B, WrapSelection, "**");

        // Ctrl+I: 斜体
        AddKeyBinding(Key.I, WrapSelection, "*");

        // Ctrl+K: 链接
        AddKeyBinding(Key.K, InsertLink);

        // Ctrl+Shift+K: 代码块
        AddKeyBinding(Key.K, KeyModifiers.Shift, InsertCodeBlock);

        // Ctrl+Shift+C: 行内代码
        AddKeyBinding(Key.C, KeyModifiers.Shift, WrapSelection, "`");

        // Ctrl+Shift+Q: 引用
        AddKeyBinding(Key.Q, KeyModifiers.Shift, PrefixLine, "> ");

        // Tab: 缩进
        _editor.TextArea.KeyDown += OnKeyDown;
    }

    private void WrapSelection(string wrapper)
    {
        var selection = _editor.SelectedText;
        if (!string.IsNullOrEmpty(selection))
        {
            _editor.SelectedText = $"{wrapper}{selection}{wrapper}";
        }
        else
        {
            var caret = _editor.CaretOffset;
            _editor.Document.Insert(caret, $"{wrapper}{wrapper}");
            _editor.CaretOffset = caret + wrapper.Length;
        }
    }

    private void InsertLink()
    {
        var selection = _editor.SelectedText;
        if (!string.IsNullOrEmpty(selection))
        {
            _editor.SelectedText = $"[{selection}](url)";
        }
        else
        {
            var caret = _editor.CaretOffset;
            _editor.Document.Insert(caret, "[链接文本](url)");
            _editor.CaretOffset = caret + 1; // 光标到 [ 后
        }
    }

    private void InsertCodeBlock()
    {
        var selection = _editor.SelectedText;
        var code = string.IsNullOrEmpty(selection) ? "// 代码" : selection;
        _editor.SelectedText = $"\n```\n{code}\n```\n";
    }

    private void PrefixLine(string prefix)
    {
        var line = _editor.Document.GetLineByOffset(_editor.CaretOffset);
        var text = _editor.Document.GetText(line);
        if (!text.StartsWith(prefix))
        {
            _editor.Document.Insert(line.Offset, prefix);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            if (_editor.SelectedText.Length > 0)
            {
                // 多行缩进
                IndentSelection(!e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
            }
        }
    }

    private void IndentSelection(bool indent)
    {
        // 实现选中区域的缩进/取消缩进
        var startLine = _editor.Document.GetLineByOffset(_editor.SelectionStart);
        var endLine = _editor.Document.GetLineByOffset(_editor.SelectionEnd);

        _editor.Document.BeginUpdate();
        try
        {
            for (var line = startLine; line != null; line = line.NextLine)
            {
                var text = _editor.Document.GetText(line);
                if (indent)
                {
                    _editor.Document.Insert(line.Offset, "  ");
                }
                else if (text.StartsWith("  "))
                {
                    _editor.Document.Remove(line.Offset, 2);
                }

                if (line == endLine) break;
            }
        }
        finally
        {
            _editor.Document.EndUpdate();
        }
    }

    private void AddKeyBinding(Key key, Action<string> action, string param)
    {
        // 注册快捷键（需要在实际控件中实现）
    }

    private void AddKeyBinding(Key key, KeyModifiers mods, Action<string> action, string param)
    {
        // 注册带修饰键的快捷键
    }

    private void AddKeyBinding(Key key, KeyModifiers mods, Action action)
    {
        // 注册带修饰键的快捷键（无参数）
    }
}
```

## 34.5 AvaloniaEdit 集成

### 34.5.1 AvaloniaEdit 简介

**AvaloniaEdit** 是 SharpDevelop 的 AvalonEdit 文本编辑器组件的 Avalonia 移植版，提供专业级的文本编辑功能。

```xml
<!-- 安装 AvaloniaEdit -->
<PackageReference Include="AvaloniaEdit" Version="11.*" />
<PackageReference Include="AvaloniaEdit.TextMate" Version="11.*" />  <!-- TextMate 语法支持 -->
```

### 34.5.2 基本 TextEditor 控件

```xml
<!-- 基本 TextEditor -->
<AvaloniaEdit:TextEditor
    x:Name="Editor"
    FontFamily="Cascadia Code,Consolas,Monaco,monospace"
    FontSize="14"
    ShowLineNumbers="True"
    WordWrap="True"
    Background="#1E1E2E"
    Foreground="#CDD6F4"
    LineNumberForeground="#6C7086"
    SelectionBrush="#45475A"
    SelectionForeground="#CDD6F4"
    CurrentLineBackground="#313244"
    CurrentLineBorderThickness="0"
    AllowOverwriteMode="True"
    Options-AllowToggleOverstrikeMode="True" />
```

```csharp
// TextEditor 配置
public partial class EditorWindow : Window
{
    public EditorWindow()
    {
        InitializeComponent();

        // 设置文本
        Editor.Text = "// 在这里输入代码...\n";

        // 设置语法高亮
        Editor.SyntaxHighlighting =
            HighlightingManager.Instance.GetDefinition("C#");

        // 配置编辑器选项
        Editor.Options.EnableHyperlinks = true;
        Editor.Options.EnableEmailHyperlinks = true;
        Editor.Options.ShowSpaces = false;
        Editor.Options.ShowTabs = false;
        Editor.Options.ShowEndOfLine = false;
        Editor.Options.HighlightCurrentLine = true;
        Editor.Options.AllowScrollBelowDocument = false;
        Editor.Options.ShowColumnRuler = true;
        Editor.Options.ColumnRulerPosition = 80;

        // 监听文本变化
        Editor.TextChanged += (s, e) =>
        {
            Console.WriteLine($"文本长度: {Editor.Text.Length}");
        };

        // 监听光标位置变化
        Editor.TextArea.Caret.PositionChanged += (s, e) =>
        {
            var pos = Editor.TextArea.Caret.Position;
            Console.WriteLine($"行: {pos.Line}, 列: {pos.Column}");
        };
    }
}
```

### 34.5.3 语法高亮

```csharp
// 使用内置语法高亮
using ICSharpCode.AvaloniaEdit.Highlighting;

public class SyntaxHighlightSetup
{
    public static void Configure(TextEditor editor, string language)
    {
        // 获取预定义语法高亮
        var highlighting = HighlightingManager.Instance.GetDefinition(language);
        editor.SyntaxHighlighting = highlighting;
    }

    // 支持的语言列表
    public static IEnumerable<string> GetSupportedLanguages()
    {
        return HighlightingManager.Instance.HighlightingDefinitions
            .Select(h => h.Name);
    }

    // 注册自定义语法高亮
    public static void RegisterCustomHighlighting()
    {
        using var stream = AssetLoader.Open(
            new Uri("avares://MyApp/SyntaxHighlighting/Markdown.xshd"));

        var definition = HighlightingLoader.Load(
            new StreamReader(stream),
            HighlightingManager.Instance);

        HighlightingManager.Instance.RegisterHighlighting(
            "Markdown", new[] { ".md", ".markdown" }, definition);
    }
}
```

### 34.5.4 TextMate 语法高亮（推荐）

TextMate 方案支持更多语言和主题：

```xml
<!-- 安装 TextMate 支持 -->
<PackageReference Include="AvaloniaEdit.TextMate" Version="11.*" />
```

```csharp
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

public class TextMateHighlightSetup
{
    private TextMate.Installation? _textMateInstallation;

    public void Setup(TextEditor editor)
    {
        // 创建注册表
        var registryOptions = new RegistryOptions(ThemeName.DarkPlus);

        // 安装 TextMate 高亮
        _textMateInstallation = editor.InstallTextMate(registryOptions);

        // 设置语言语法
        _textMateInstallation.SetGrammar(
            registryOptions.GetScopeByLanguageId("csharp"));
    }

    // 切换主题
    public void ChangeTheme(TextEditor editor, ThemeName theme)
    {
        _textMateInstallation?.Dispose();

        var registryOptions = new RegistryOptions(theme);
        _textMateInstallation = editor.InstallTextMate(registryOptions);

        _textMateInstallation.SetGrammar(
            registryOptions.GetScopeByLanguageId("csharp"));
    }

    // 获取所有可用主题
    public IEnumerable<ThemeName> GetAvailableThemes()
    {
        return Enum.GetValues<ThemeName>();
    }

    // 获取所有可用语言
    public IEnumerable<string> GetAvailableLanguages(RegistryOptions options)
    {
        return options.GetAvailableLanguages();
    }
}
```

```csharp
// TextMate 主题列表
public static class TextMateThemes
{
    // 流行主题
    public static readonly ThemeName[] PopularThemes =
    [
        ThemeName.DarkPlus,       // VS Code 深色
        ThemeName.LightPlus,      // VS Code 浅色
        ThemeName.DarkModern,     // 现代深色
        ThemeName.DarkQuiet,      // 简洁深色
        ThemeName.Abbys,          // Abyss 主题
        ThemeName.KimbieDark,     // Kimbie 深色
        ThemeName.Monokai,        // Monokai
        ThemeName.MonokaiDimmed,  // Monokai Dimmed
        ThemeName.QuietLight,     // 安静浅色
        ThemeName.Red,            // 红色主题
        ThemeName.SolarizedDark,  // Solarized 深色
        ThemeName.SolarizedLight, // Solarized 浅色
        ThemeName.TomorrowNightBlue, // Tomorrow Night Blue
    ];
}
```

### 34.5.5 代码折叠

```csharp
// 启用代码折叠
using ICSharpCode.AvaloniaEdit.Folding;

public class CodeFoldingSetup
{
    private FoldingManager? _foldingManager;
    private AbstractFoldingStrategy? _foldingStrategy;

    public void Setup(TextEditor editor)
    {
        // 创建 FoldingManager
        _foldingManager = FoldingManager.Install(editor.TextArea);

        // 根据语法选择折叠策略
        _foldingStrategy = new BraceFoldingStrategy();
    }

    public void UpdateFoldings()
    {
        _foldingStrategy?.UpdateFoldings(_foldingManager,
            Editor.Document);
    }
}

// 基于大括号的折叠策略
public class BraceFoldingStrategy : AbstractFoldingStrategy
{
    public override void UpdateFoldings(
        FoldingManager manager, TextDocument document)
    {
        var newFoldings = CreateNewFoldings(document);
        manager.UpdateFoldings(newFoldings);
    }

    private IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
    {
        var text = document.Text;
        var stack = new Stack<int>();
        var foldings = new List<NewFolding>();

        for (int i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '{':
                    stack.Push(i);
                    break;
                case '}':
                    if (stack.Count > 0)
                    {
                        var start = stack.Pop();
                        // 折叠区域从大括号前开始
                        var lineStart = document.GetLineByOffset(start);
                        foldings.Add(new NewFolding(start, i + 1));
                    }
                    break;
            }
        }

        // 按位置排序
        foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        return foldings;
    }
}

// 基于 #region 的折叠策略
public class RegionFoldingStrategy : AbstractFoldingStrategy
{
    private static readonly Regex RegionStartRegex = new(
        @"^\s*#region\b", RegexOptions.Multiline);
    private static readonly Regex RegionEndRegex = new(
        @"^\s*#endregion\b", RegexOptions.Multiline);

    public override void UpdateFoldings(
        FoldingManager manager, TextDocument document)
    {
        var foldings = new List<NewFolding>();
        var text = document.Text;
        var startStack = new Stack<int>();

        foreach (Match match in RegionStartRegex.Matches(text))
        {
            startStack.Push(match.Index);
        }

        foreach (Match match in RegionEndRegex.Matches(text))
        {
            if (startStack.Count > 0)
            {
                var start = startStack.Pop();
                foldings.Add(new NewFolding(start,
                    match.Index + match.Length));
            }
        }

        foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        manager.UpdateFoldings(foldings);
    }
}
```

### 34.5.6 行号显示和自定义

```xml
<!-- 行号显示（内置支持） -->
<AvaloniaEdit:TextEditor
    ShowLineNumbers="True"
    LineNumberForeground="#6C7086" />
```

```csharp
// 自定义行号渲染
public class CustomLineNumberMargin : LineNumberMargin
{
    protected override void OnRender(DrawingContext context)
    {
        // 自定义行号样式
        var typeface = new Typeface("Cascadia Code",
            FontStyle.Normal, FontWeight.Normal);

        foreach (var visualLine in TextView.VisualLines)
        {
            var lineNo = visualLine.FirstDocumentLine.LineNumber;
            var text = new FormattedText(
                lineNo.ToString(),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                FontSize,
                new SolidColorBrush(Color.Parse("#6C7086")));

            var y = visualLine.VisualTop - TextView.ScrollOffset.Y;
            context.DrawText(text, new Point(Bounds.Width - text.Width - 8, y));
        }
    }
}
```

### 34.5.7 搜索和替换

```csharp
// 搜索和替换功能
using ICSharpCode.AvaloniaEdit.Search;

public class SearchReplaceSetup
{
    private readonly TextEditor _editor;
    private SearchPanel? _searchPanel;

    public SearchReplaceSetup(TextEditor editor)
    {
        _editor = editor;
    }

    // 打开搜索面板
    public void OpenSearch()
    {
        _searchPanel ??= SearchPanel.Install(_editor.TextArea);
        _searchPanel.Open();
        _searchPanel.SearchPattern = _editor.SelectedText;
    }

    // 打开替换面板
    public void OpenReplace()
    {
        _searchPanel ??= SearchPanel.Install(_editor.TextArea);
        _searchPanel.Open();
        _searchPanel.ShowReplace = true;
    }

    // 代码中执行查找
    public int FindNext(string pattern, bool caseSensitive = false,
        bool useRegex = false)
    {
        var options = caseSensitive
            ? RegexOptions.None
            : RegexOptions.IgnoreCase;

        if (useRegex)
        {
            var regex = new Regex(pattern, options);
            var match = regex.Match(_editor.Text, _editor.CaretOffset);
            if (!match.Success)
                match = regex.Match(_editor.Text, 0); // 从头开始

            if (match.Success)
            {
                _editor.Select(match.Index, match.Length);
                _editor.TextArea.Caret.Offset = match.Index;
                return match.Index;
            }
        }
        else
        {
            var comparison = caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            var index = _editor.Text.IndexOf(pattern,
                _editor.CaretOffset, comparison);

            if (index < 0)
                index = _editor.Text.IndexOf(pattern, 0, comparison);

            if (index >= 0)
            {
                _editor.Select(index, pattern.Length);
                _editor.TextArea.Caret.Offset = index;
                return index;
            }
        }

        return -1; // 未找到
    }

    // 全部替换
    public int ReplaceAll(string find, string replace,
        bool caseSensitive = false)
    {
        var comparison = caseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        var count = 0;
        var text = _editor.Text;

        _editor.Document.BeginUpdate();
        try
        {
            int index = 0;
            while ((index = text.IndexOf(find, index, comparison)) >= 0)
            {
                _editor.Document.Replace(index, find.Length, replace);
                index += replace.Length;
                count++;
            }
        }
        finally
        {
            _editor.Document.EndUpdate();
        }

        return count;
    }
}
```

### 34.5.8 自动缩进

```csharp
// 自动缩进策略
using ICSharpCode.AvaloniaEdit.Indentation;

public class MarkdownIndentationStrategy : DefaultIndentationStrategy
{
    public override void IndentLine(TextDocument document, DocumentLine line)
    {
        var previousLine = line.PreviousLine;
        if (previousLine == null) return;

        var previousText = document.GetText(previousLine);

        // 计算缩进
        var indent = GetIndentation(previousText);

        // 如果上一行以列表符号结尾，保持缩进
        if (IsListItem(previousText))
        {
            indent = GetListItemIndent(previousText);
        }

        // 如果上一行以引用符号结尾
        if (IsBlockquote(previousText))
        {
            indent = GetBlockquoteIndent(previousText);
        }

        var currentText = document.GetText(line);
        var currentIndent = GetLeadingWhitespace(currentText);

        if (currentIndent != indent)
        {
            document.Replace(line.Offset, currentIndent.Length, indent);
        }
    }

    private static string GetIndentation(string lineText)
    {
        var match = Regex.Match(lineText, @"^(\s*)");
        return match.Groups[1].Value;
    }

    private static bool IsListItem(string lineText)
    {
        return Regex.IsMatch(lineText, @"^\s*[-*+]\s|^\s*\d+\.\s");
    }

    private static string GetListItemIndent(string lineText)
    {
        var match = Regex.Match(lineText, @"^(\s*[-*+]\s|^\s*\d+\.\s)");
        return match.Success ? match.Value : "";
    }

    private static bool IsBlockquote(string lineText)
    {
        return Regex.IsMatch(lineText, @"^\s*>");
    }

    private static string GetBlockquoteIndent(string lineText)
    {
        var match = Regex.Match(lineText, @"^(\s*>\s*)");
        return match.Success ? match.Value : "";
    }

    private static string GetLeadingWhitespace(string text)
    {
        var match = Regex.Match(text, @"^(\s*)");
        return match.Groups[1].Value;
    }
}
```

### 34.5.9 多光标编辑

```csharp
// 多光标编辑支持
public class MultiCursorEditor
{
    private readonly TextEditor _editor;

    public MultiCursorEditor(TextEditor editor)
    {
        _editor = editor;
        _editor.TextArea.KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl+D: 选择下一个相同单词
        if (e.Key == Key.D && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            SelectNextOccurrence();
            e.Handled = true;
        }

        // Alt+Click: 添加光标（需要配合鼠标事件）
        // Ctrl+Shift+L: 选择所有相同单词
        if (e.Key == L && e.KeyModifiers.HasFlag(KeyModifiers.Control)
                       && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            SelectAllOccurrences();
            e.Handled = true;
        }
    }

    private void SelectNextOccurrence()
    {
        var selectedText = _editor.SelectedText;
        if (string.IsNullOrEmpty(selectedText)) return;

        // 从当前选择位置之后查找
        var startIndex = _editor.SelectionStart + _editor.SelectionLength;
        var index = _editor.Text.IndexOf(selectedText,
            startIndex, StringComparison.Ordinal);

        if (index < 0)
        {
            // 从头开始查找（循环查找）
            index = _editor.Text.IndexOf(selectedText,
                StringComparison.Ordinal);
        }

        if (index >= 0)
        {
            // 添加新的选择区域
            _editor.TextArea.Selection = new RectangleSelection(
                _editor.TextArea,
                _editor.Document.GetLocation(index),
                _editor.Document.GetLocation(index + selectedText.Length));
        }
    }

    private void SelectAllOccurrences()
    {
        var selectedText = _editor.SelectedText;
        if (string.IsNullOrEmpty(selectedText)) return;

        // 查找所有出现位置
        var indices = new List<int>();
        int index = 0;
        while ((index = _editor.Text.IndexOf(selectedText,
            index, StringComparison.Ordinal)) >= 0)
        {
            indices.Add(index);
            index += selectedText.Length;
        }

        // 选中所有出现
        if (indices.Count > 0)
        {
            _editor.Select(indices[0], selectedText.Length);
        }
    }
}
```

### 34.5.10 自定义语法定义（.xshd 文件）

```xml
<!-- Markdown.xshd - Markdown 语法高亮定义 -->
<SyntaxDefinition name="Markdown"
                  xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
    <Color name="Heading" foreground="#F38BA8" fontWeight="bold" />
    <Color name="Bold" fontWeight="bold" />
    <Color name="Italic" fontStyle="italic" />
    <Color name="Code" foreground="#A6E3A1" background="#313244" />
    <Color name="CodeBlock" foreground="#A6E3A1" background="#313244" />
    <Color name="Link" foreground="#89B4FA" underline="true" />
    <Color name="Image" foreground="#F9E2AF" />
    <Color name="ListMarker" foreground="#CBA6F7" fontWeight="bold" />
    <Color name="Blockquote" foreground="#6C7086" fontStyle="italic" />
    <Color name="HorizontalRule" foreground="#585B70" />
    <Color name="HtmlTag" foreground="#89DCEB" />
    <Color name="Escape" foreground="#F5C2E7" />

    <!-- 标题 -->
    <Rule color="Heading">
        ^#{1,6}\s+.*$
    </Rule>

    <!-- 粗体 -->
    <Rule color="Bold">
        \*\*[^*]+\*\*
    </Rule>
    <Rule color="Bold">
        __[^_]+__
    </Rule>

    <!-- 斜体 -->
    <Rule color="Italic">
        \*[^*]+\*
    </Rule>
    <Rule color="Italic">
        _[^_]+_
    </Rule>

    <!-- 行内代码 -->
    <Rule color="Code">
        `[^`\n]+`
    </Rule>

    <!-- 代码块 -->
    <Rule color="CodeBlock">
        ```[\s\S]*?```
    </Rule>

    <!-- 链接 -->
    <Rule color="Link">
        \[([^\]]+)\]\(([^)]+)\)
    </Rule>

    <!-- 图片 -->
    <Rule color="Image">
        !\[([^\]]*)\]\(([^)]+)\)
    </Rule>

    <!-- 列表标记 -->
    <Rule color="ListMarker">
        ^\s*[-*+]\s
    </Rule>
    <Rule color="ListMarker">
        ^\s*\d+\.\s
    </Rule>
    <Rule color="ListMarker">
        ^\s*-\s*\[[ xX]\]
    </Rule>

    <!-- 引用 -->
    <Rule color="Blockquote">
        ^&gt;.*$
    </Rule>

    <!-- 水平线 -->
    <Rule color="HorizontalRule">
        ^[-*_]{3,}\s*$
    </Rule>

    <!-- HTML 标签 -->
    <Rule color="HtmlTag">
        &lt;/?[a-zA-Z][^&]*?&gt;
    </Rule>

    <!-- 转义字符 -->
    <Rule color="Escape">
        \\[\*\_\[\]\(\)\#\+\-\.\!]
    </Rule>
</SyntaxDefinition>
```

```csharp
// 加载自定义语法定义
public static class CustomSyntaxLoader
{
    public static void LoadMarkdownSyntax()
    {
        using var stream = AssetLoader.Open(
            new Uri("avares://MyApp/SyntaxHighlighting/Markdown.xshd"));

        using var reader = new StreamReader(stream);
        var definition = HighlightingLoader.Load(reader,
            HighlightingManager.Instance);

        HighlightingManager.Instance.RegisterHighlighting(
            "Markdown",
            new[] { ".md", ".markdown", ".mdx" },
            definition);
    }
}
```

## 34.6 语法高亮实现

### 34.6.1 预定义语法加载

```csharp
// AvaloniaEdit 内置支持的语法
public static class BuiltInLanguages
{
    public static readonly string[] Languages =
    [
        "C#", "C++", "CSS", "HTML", "Java", "JavaScript",
        "JSON", "PowerShell", "Python", "SQL", "TypeScript",
        "VB.NET", "XML", "XAML", "YAML"
    ];

    public static void ApplyHighlighting(TextEditor editor, string language)
    {
        var def = HighlightingManager.Instance.GetDefinition(language);
        if (def != null)
        {
            editor.SyntaxHighlighting = def;
        }
    }
}
```

### 34.6.2 TextMate 主题切换

```csharp
// 深色/浅色主题切换
public class ThemeSwitcher
{
    private TextMate.Installation? _installation;
    private ThemeName _currentTheme;

    public void ApplyTheme(TextEditor editor, bool isDark)
    {
        _currentTheme = isDark ? ThemeName.DarkPlus : ThemeName.LightPlus;

        _installation?.Dispose();

        var options = new RegistryOptions(_currentTheme);
        _installation = editor.InstallTextMate(options);

        // 同步编辑器背景色
        if (isDark)
        {
            editor.Background = new SolidColorBrush(Color.Parse("#1E1E2E"));
            editor.Foreground = new SolidColorBrush(Color.Parse("#D4D4D4"));
        }
        else
        {
            editor.Background = new SolidColorBrush(Color.Parse("#FFFFFF"));
            editor.Foreground = new SolidColorBrush(Color.Parse("#000000"));
        }
    }

    // 应用自定义配色方案
    public void ApplyCustomTheme(TextEditor editor, EditorTheme theme)
    {
        editor.Background = new SolidColorBrush(theme.Background);
        editor.Foreground = new SolidColorBrush(theme.Foreground);
        editor.LineNumberForeground = new SolidColorBrush(theme.LineNumber);
        editor.SelectionBrush = new SolidColorBrush(theme.Selection);
        editor.CurrentLineBackground = new SolidColorBrush(theme.CurrentLine);
    }
}

// 编辑器主题数据结构
public record EditorTheme(
    Color Background,
    Color Foreground,
    Color LineNumber,
    Color Selection,
    Color CurrentLine,
    Color Keyword,
    Color Comment,
    Color String,
    Color Number,
    Color Operator
);

// 预定义主题
public static class EditorThemes
{
    public static EditorTheme Monokai => new(
        Background:    Color.Parse("#272822"),
        Foreground:    Color.Parse("#F8F8F2"),
        LineNumber:    Color.Parse("#90908A"),
        Selection:     Color.Parse("#49483E"),
        CurrentLine:   Color.Parse("#3E3D32"),
        Keyword:       Color.Parse("#F92672"),
        Comment:       Color.Parse("#75715E"),
        String:        Color.Parse("#E6DB74"),
        Number:        Color.Parse("#AE81FF"),
        Operator:      Color.Parse("#F8F8F2")
    );

    public static EditorTheme Dracula => new(
        Background:    Color.Parse("#282A36"),
        Foreground:    Color.Parse("#F8F8F2"),
        LineNumber:    Color.Parse("#6272A4"),
        Selection:     Color.Parse("#44475A"),
        CurrentLine:   Color.Parse("#44475A"),
        Keyword:       Color.Parse("#FF79C6"),
        Comment:       Color.Parse("#6272A4"),
        String:        Color.Parse("#F1FA8C"),
        Number:        Color.Parse("#BD93F9"),
        Operator:      Color.Parse("#FF79C6")
    );

    public static EditorTheme SolarizedDark => new(
        Background:    Color.Parse("#002B36"),
        Foreground:    Color.Parse("#839496"),
        LineNumber:    Color.Parse("#586E75"),
        Selection:     Color.Parse("#073642"),
        CurrentLine:   Color.Parse("#073642"),
        Keyword:       Color.Parse("#859900"),
        Comment:       Color.Parse("#586E75"),
        String:        Color.Parse("#2AA198"),
        Number:        Color.Parse("#D33682"),
        Operator:      Color.Parse("#859900")
    );

    public static EditorTheme OneDark => new(
        Background:    Color.Parse("#282C34"),
        Foreground:    Color.Parse("#ABB2BF"),
        LineNumber:    Color.Parse("#5C6370"),
        Selection:     Color.Parse("#3E4451"),
        CurrentLine:   Color.Parse("#2C313C"),
        Keyword:       Color.Parse("#C678DD"),
        Comment:       Color.Parse("#5C6370"),
        String:        Color.Parse("#98C379"),
        Number:        Color.Parse("#D19A66"),
        Operator:      Color.Parse("#56B6C2")
    );

    public static EditorTheme CatppuccinMocha => new(
        Background:    Color.Parse("#1E1E2E"),
        Foreground:    Color.Parse("#CDD6F4"),
        LineNumber:    Color.Parse("#6C7086"),
        Selection:     Color.Parse("#45475A"),
        CurrentLine:   Color.Parse("#313244"),
        Keyword:       Color.Parse("#CBA6F7"),
        Comment:       Color.Parse("#6C7086"),
        String:        Color.Parse("#A6E3A1"),
        Number:        Color.Parse("#FAB387"),
        Operator:      Color.Parse("#89DCEB")
    );
}
```

## 34.7 富文本编辑器

### 34.7.1 RichTextBox 基础

Avalonia 的 `RichTextBox` 提供基本的富文本编辑能力：

```xml
<!-- 基本 RichTextBox -->
<RichTextBox
    AcceptsReturn="True"
    TextWrapping="Wrap"
    FontFamily="Microsoft YaHei"
    FontSize="14"
    Padding="8" />
```

```csharp
// 通过代码操作 RichTextBox
public class RichTextEditor
{
    private readonly RichTextBox _richTextBox;

    public RichTextEditor(RichTextBox richTextBox)
    {
        _richTextBox = richTextBox;
    }

    // 插入带格式的文本
    public void InsertFormattedText(string text, bool bold = false,
        bool italic = false, Color? color = null)
    {
        var run = new Run(text);

        if (bold) run.FontWeight = FontWeight.Bold;
        if (italic) run.FontStyle = FontStyle.Italic;
        if (color.HasValue)
            run.Foreground = new SolidColorBrush(color.Value);

        var paragraph = _richTextBox.Document?.Blocks
            .FirstOrDefault() as Paragraph;

        paragraph?.Inlines?.Add(run);
    }

    // 设置文本对齐方式
    public void SetAlignment(TextAlignment alignment)
    {
        var paragraph = _richTextBox.Document?.Blocks
            .FirstOrDefault() as Paragraph;
        if (paragraph != null)
        {
            paragraph.TextAlignment = alignment;
        }
    }
}
```

### 34.7.2 FlowDocument 文档模型

```csharp
// 创建和操作 FlowDocument
public class DocumentBuilder
{
    public static FlowDocument CreateSampleDocument()
    {
        var doc = new FlowDocument();

        // 添加标题段落
        var heading = new Paragraph();
        heading.FontSize = 24;
        heading.FontWeight = FontWeight.Bold;
        heading.Inlines.Add(new Run("文档标题"));
        doc.Blocks.Add(heading);

        // 添加正文段落
        var para = new Paragraph();
        para.Inlines.Add(new Run("这是 "));
        para.Inlines.Add(new Run("粗体") { FontWeight = FontWeight.Bold });
        para.Inlines.Add(new Run(" 和 "));
        para.Inlines.Add(new Run("斜体") { FontStyle = FontStyle.Italic });
        para.Inlines.Add(new Run(" 文本示例。"));
        doc.Blocks.Add(para);

        // 添加引用
        var quote = new Paragraph();
        quote.Padding = new Thickness(20, 8);
        quote.BorderBrush = new SolidColorBrush(Colors.Gray);
        quote.Inlines.Add(new Run("这是一段引用文字。")
        {
            FontStyle = FontStyle.Italic
        });
        doc.Blocks.Add(quote);

        // 添加列表
        var list = new List();
        list.MarkerStyle = TextMarkerStyle.Disc;
        list.ListItems.Add(new ListItem(
            new Paragraph(new Run("列表项 1"))));
        list.ListItems.Add(new ListItem(
            new Paragraph(new Run("列表项 2"))));
        list.ListItems.Add(new ListItem(
            new Paragraph(new Run("列表项 3"))));
        doc.Blocks.Add(list);

        return doc;
    }

    // 从 Markdown 创建 FlowDocument
    public static FlowDocument FromMarkdown(string markdown)
    {
        var doc = new FlowDocument();
        var lines = markdown.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("# "))
            {
                doc.Blocks.Add(new Paragraph(
                    new Run(trimmed[2..]))
                {
                    FontSize = 24,
                    FontWeight = FontWeight.Bold
                });
            }
            else if (trimmed.StartsWith("## "))
            {
                doc.Blocks.Add(new Paragraph(
                    new Run(trimmed[3..]))
                {
                    FontSize = 20,
                    FontWeight = FontWeight.Bold
                });
            }
            else if (trimmed.StartsWith("- "))
            {
                doc.Blocks.Add(new Paragraph(
                    new Run("  • " + trimmed[2..])));
            }
            else
            {
                doc.Blocks.Add(new Paragraph(new Run(trimmed)));
            }
        }

        return doc;
    }
}
```

### 34.7.3 文本格式化操作

```csharp
// 富文本格式化工具
public class RichTextFormatter
{
    private readonly RichTextBox _editor;

    public RichTextFormatter(RichTextBox editor)
    {
        _editor = editor;
    }

    // 切换粗体
    public void ToggleBold()
    {
        var selection = _editor.Selection;
        if (selection == null) return;

        var currentWeight = selection.GetPropertyValue(
            TextElement.FontWeightProperty);

        if (currentWeight is FontWeight.Bold)
        {
            selection.ApplyPropertyValue(
                TextElement.FontWeightProperty, FontWeight.Normal);
        }
        else
        {
            selection.ApplyPropertyValue(
                TextElement.FontWeightProperty, FontWeight.Bold);
        }
    }

    // 切换斜体
    public void ToggleItalic()
    {
        var selection = _editor.Selection;
        if (selection == null) return;

        var currentStyle = selection.GetPropertyValue(
            TextElement.FontStyleProperty);

        if (currentStyle is FontStyle.Italic)
        {
            selection.ApplyPropertyValue(
                TextElement.FontStyleProperty, FontStyle.Normal);
        }
        else
        {
            selection.ApplyPropertyValue(
                TextElement.FontStyleProperty, FontStyle.Italic);
        }
    }

    // 设置字体大小
    public void SetFontSize(double size)
    {
        _editor.Selection?.ApplyPropertyValue(
            TextElement.FontSizeProperty, size);
    }

    // 设置字体颜色
    public void SetForeground(Color color)
    {
        _editor.Selection?.ApplyPropertyValue(
            TextElement.ForegroundProperty,
            new SolidColorBrush(color));
    }

    // 设置背景色（高亮）
    public void SetBackground(Color color)
    {
        _editor.Selection?.ApplyPropertyValue(
            TextElement.BackgroundProperty,
            new SolidColorBrush(color));
    }

    // 设置下划线
    public void ToggleUnderline()
    {
        var selection = _editor.Selection;
        if (selection == null) return;

        var currentDecorations = selection.GetPropertyValue(
            TextDecorations.Underline);

        if (currentDecorations == TextDecorations.Underline)
        {
            // 移除下划线
            selection.ApplyPropertyValue(
                TextDecorations.Underline, null);
        }
        else
        {
            selection.ApplyPropertyValue(
                TextDecorations.Underline, TextDecorations.Underline);
        }
    }

    // 插入超链接
    public void InsertHyperlink(string text, string url)
    {
        var link = new Avalonia.Controls.Documents.Hyperlink
        {
            NavigateUri = new Uri(url),
            Foreground = new SolidColorBrush(Colors.CornflowerBlue),
            TextDecorations = TextDecorations.Underline
        };
        link.Inlines.Add(new Run(text));

        _editor.Selection?.Start?.Paragraph?.Inlines?.Add(link);
    }
}
```

### 34.7.4 撤销/重做

```csharp
// 撤销/重做管理
public class UndoRedoManager
{
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();
    private readonly TextBox _textBox;
    private bool _isUndoRedo;

    public UndoRedoManager(TextBox textBox)
    {
        _textBox = textBox;
        _textBox.TextChanged += OnTextChanged;
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (_isUndoRedo) return;

        _undoStack.Push(_textBox.Text ?? "");
        _redoStack.Clear(); // 新操作清空重做栈
    }

    public bool CanUndo => _undoStack.Count > 1;
    public bool CanRedo => _redoStack.Count > 0;

    public void Undo()
    {
        if (!CanUndo) return;

        _isUndoRedo = true;
        _redoStack.Push(_undoStack.Pop());
        _textBox.Text = _undoStack.Peek();
        _isUndoRedo = false;
    }

    public void Redo()
    {
        if (!CanRedo) return;

        _isUndoRedo = true;
        var text = _redoStack.Pop();
        _undoStack.Push(text);
        _textBox.Text = text;
        _isUndoRedo = false;
    }
}
```

### 34.7.5 复制/粘贴

```csharp
// 增强的复制/粘贴功能
public class ClipboardHelper
{
    // 复制为纯文本
    public static async Task CopyAsPlainText(string text)
    {
        var clipboard = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime?.MainWindow)?.Clipboard;

        await clipboard?.SetTextAsync(text)!;
    }

    // 复制为 Markdown（带格式标记）
    public static async Task CopyAsMarkdown(string text, bool isCode = false)
    {
        var markdown = isCode ? $"```\n{text}\n```" : text;
        await CopyAsPlainText(markdown);
    }

    // 从剪贴板粘贴（处理 HTML 格式）
    public static async Task<string?> PasteFromClipboard()
    {
        var clipboard = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime?.MainWindow)?.Clipboard;

        if (clipboard == null) return null;

        // 尝试获取文本格式
        var text = await clipboard.GetTextAsync();
        return text;
    }
}
```

## 34.8 实战：笔记应用

### 34.8.1 笔记模型

```csharp
// 笔记数据模型
public class Note : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "新笔记";
    public string Content { get; set; } = "";
    public string[] Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string? Folder { get; set; }

    // 用于列表显示的摘要
    public string Preview => Content.Length > 100
        ? Content[..100] + "..."
        : Content;
}

// 笔记文件夹
public class NoteFolder : ObservableObject
{
    public string Name { get; set; } = "";
    public ObservableCollection<Note> Notes { get; set; } = [];
    public ObservableCollection<NoteFolder> SubFolders { get; set; } = [];
}
```

### 34.8.2 笔记服务

```csharp
// 笔记数据服务（SQLite 存储）
public class NoteService
{
    private readonly string _dbPath;

    public NoteService(string dbPath)
    {
        _dbPath = dbPath;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Notes (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL DEFAULT '',
                Tags TEXT DEFAULT '[]',
                Folder TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Notes_Folder ON Notes(Folder);
            CREATE INDEX IF NOT EXISTS IX_Notes_UpdatedAt ON Notes(UpdatedAt DESC);
            """;
        command.ExecuteNonQuery();
    }

    public async Task<List<Note>> GetAllNotesAsync()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Notes ORDER BY UpdatedAt DESC";

        var notes = new List<Note>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            notes.Add(ReadNote(reader));
        }

        return notes;
    }

    public async Task<Note?> GetNoteByIdAsync(Guid id)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Notes WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id.ToString());

        using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadNote(reader) : null;
    }

    public async Task SaveNoteAsync(Note note)
    {
        note.UpdatedAt = DateTime.Now;

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR REPLACE INTO Notes
                (Id, Title, Content, Tags, Folder, CreatedAt, UpdatedAt)
            VALUES
                (@Id, @Title, @Content, @Tags, @Folder, @CreatedAt, @UpdatedAt)
            """;

        command.Parameters.AddWithValue("@Id", note.Id.ToString());
        command.Parameters.AddWithValue("@Title", note.Title);
        command.Parameters.AddWithValue("@Content", note.Content);
        command.Parameters.AddWithValue("@Tags",
            JsonSerializer.Serialize(note.Tags));
        command.Parameters.AddWithValue("@Folder",
            note.Folder ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAt",
            note.CreatedAt.ToString("o"));
        command.Parameters.AddWithValue("@UpdatedAt",
            note.UpdatedAt.ToString("o"));

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteNoteAsync(Guid id)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Notes WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id.ToString());

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<Note>> SearchNotesAsync(string query)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM Notes
            WHERE Title LIKE @Query OR Content LIKE @Query
            ORDER BY UpdatedAt DESC
            """;
        command.Parameters.AddWithValue("@Query", $"%{query}%");

        var notes = new List<Note>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            notes.Add(ReadNote(reader));
        }

        return notes;
    }

    private static Note ReadNote(SqliteDataReader reader)
    {
        return new Note
        {
            Id = Guid.Parse(reader.GetString(0)),
            Title = reader.GetString(1),
            Content = reader.GetString(2),
            Tags = JsonSerializer.Deserialize<string[]>(reader.GetString(3))
                   ?? [],
            Folder = reader.IsDBNull(4) ? null : reader.GetString(4),
            CreatedAt = DateTime.Parse(reader.GetString(5)),
            UpdatedAt = DateTime.Parse(reader.GetString(6))
        };
    }
}
```

### 34.8.3 笔记编辑器视图

```xml
<!-- NoteEditorView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="clr-namespace:Markdown.Avalonia;assembly=Markdown.Avalonia"
             xmlns:ae="clr-namespace:AvaloniaEdit;assembly=AvaloniaEdit"
             x:Class="MyApp.Views.NoteEditorView">

    <DockPanel>
        <!-- 工具栏 -->
        <StackPanel DockPanel.Dock="Top"
                    Orientation="Horizontal"
                    Spacing="4"
                    Margin="8">
            <!-- 快捷格式按钮 -->
            <Button Content="B" FontWeight="Bold"
                    Command="{Binding ToggleBoldCommand}"
                    ToolTip.Tip="粗体 (Ctrl+B)"
                    Width="32" />
            <Button Content="I" FontStyle="Italic"
                    Command="{Binding ToggleItalicCommand}"
                    ToolTip.Tip="斜体 (Ctrl+I)"
                    Width="32" />
            <Button Content="~"
                    Command="{Binding ToggleStrikethroughCommand}"
                    ToolTip.Tip="删除线"
                    Width="32" />
            <Separator Width="1" />
            <Button Content="H1"
                    Command="{Binding InsertHeadingCommand}"
                    CommandParameter="1"
                    Width="36" />
            <Button Content="H2"
                    Command="{Binding InsertHeadingCommand}"
                    CommandParameter="2"
                    Width="36" />
            <Button Content="H3"
                    Command="{Binding InsertHeadingCommand}"
                    CommandParameter="3"
                    Width="36" />
            <Separator Width="1" />
            <Button Content="&#x1F517;"
                    Command="{Binding InsertLinkCommand}"
                    ToolTip.Tip="链接 (Ctrl+K)"
                    Width="32" />
            <Button Content="&#x1F4BB;"
                    Command="{Binding InsertCodeBlockCommand}"
                    ToolTip.Tip="代码块"
                    Width="32" />
            <Button Content="&#x1F4CB;"
                    Command="{Binding InsertTableCommand}"
                    ToolTip.Tip="表格"
                    Width="32" />
            <Button Content="&#x2611;"
                    Command="{Binding InsertTaskListCommand}"
                    ToolTip.Tip="任务列表"
                    Width="32" />
            <Separator Width="1" />
            <Button Content="&#x2B06;&#xFE0F;"
                    Command="{Binding InsertImageCommand}"
                    ToolTip.Tip="插入图片"
                    Width="32" />
        </StackPanel>

        <!-- 标签栏 -->
        <Border DockPanel.Dock="Top"
                Background="{DynamicResource HeaderBg}"
                Padding="8,4">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <TextBlock Text="标签:"
                           VerticalAlignment="Center"
                           FontSize="12" />
                <ItemsControl ItemsSource="{Binding Tags}">
                    <ItemsControl.ItemsPanel>
                        <ItemsPanelTemplate>
                            <StackPanel Orientation="Horizontal"
                                        Spacing="4" />
                        </ItemsPanelTemplate>
                    </ItemsControl.ItemsPanel>
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Border Background="{DynamicResource AccentColor}"
                                    CornerRadius="10"
                                    Padding="8,2">
                                <TextBlock Text="{Binding}"
                                           Foreground="White"
                                           FontSize="11" />
                            </Border>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
                <Button Content="+"
                        Command="{Binding AddTagCommand}"
                        Width="24" Height="24"
                        Padding="0" />
            </StackPanel>
        </Border>

        <!-- 编辑区和预览区 -->
        <Grid ColumnDefinitions="*,4,*">
            <!-- 编辑器 -->
            <ae:TextEditor Grid.Column="0"
                          x:Name="Editor"
                          Text="{Binding Note.Content, Mode=TwoWay}"
                          FontFamily="Cascadia Code,Consolas,monospace"
                          FontSize="14"
                          ShowLineNumbers="True"
                          WordWrap="True"
                          SyntaxHighlighting="Markdown"
                          Background="{DynamicResource EditorBg}"
                          Foreground="{DynamicResource TextPrimary}" />

            <!-- 分隔条 -->
            <GridSplitter Grid.Column="1"
                          Background="{DynamicResource BorderColor}" />

            <!-- 预览 -->
            <md:MarkdownScrollViewer Grid.Column="2"
                x:Name="Preview"
                Markdown="{Binding Note.Content}"
                MarkdownPipeline="{Binding Pipeline}" />
        </Grid>

        <!-- 状态栏 -->
        <Border DockPanel.Dock="Bottom"
                Background="{DynamicResource HeaderBg}"
                Padding="8,4">
            <StackPanel Orientation="Horizontal" Spacing="16">
                <TextBlock FontSize="11">
                    <TextBlock.Text>
                        <MultiBinding StringFormat="行 {0}, 列 {1}">
                            <Binding Path="CaretLine" />
                            <Binding Path="CaretColumn" />
                        </MultiBinding>
                    </TextBlock.Text>
                </TextBlock>
                <TextBlock Text="{Binding WordCount, StringFormat='{}{0} 字'}"
                           FontSize="11" />
                <TextBlock Text="{Binding CharacterCount,
                    StringFormat='{}{0} 字符'}"
                           FontSize="11" />
                <TextBlock Text="{Binding Note.UpdatedAt,
                    StringFormat='{}最后修改: {0:yyyy-MM-dd HH:mm}'}"
                           FontSize="11"
                           HorizontalAlignment="Right" />
            </StackPanel>
        </Border>
    </DockPanel>
</UserControl>
```

### 34.8.4 笔记 ViewModel

```csharp
// NoteEditorViewModel
public class NoteEditorViewModel : ObservableObject
{
    private readonly NoteService _noteService;
    private readonly MarkdownPipeline _pipeline;
    private Note _note;
    private int _caretLine;
    private int _caretColumn;
    private int _wordCount;
    private int _characterCount;
    private ObservableCollection<string> _tags;

    public NoteEditorViewModel(NoteService noteService)
    {
        _noteService = noteService;
        _pipeline = MarkdownPipelineFactory.Create();
        _note = new Note();
        _tags = [];

        ToggleBoldCommand = new RelayCommand(ToggleBold);
        ToggleItalicCommand = new RelayCommand(ToggleItalic);
        ToggleStrikethroughCommand = new RelayCommand(ToggleStrikethrough);
        InsertHeadingCommand = new RelayCommand<int>(InsertHeading);
        InsertLinkCommand = new RelayCommand(InsertLink);
        InsertCodeBlockCommand = new RelayCommand(InsertCodeBlock);
        InsertTableCommand = new RelayCommand(InsertTable);
        InsertTaskListCommand = new RelayCommand(InsertTaskList);
        InsertImageCommand = new RelayCommand(InsertImage);
        AddTagCommand = new RelayCommand(AddTag);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public Note Note
    {
        get => _note;
        set
        {
            if (SetProperty(ref _note, value))
            {
                Tags = new ObservableCollection<string>(value.Tags);
                UpdateCounts();
            }
        }
    }

    public MarkdownPipeline Pipeline => _pipeline;

    public ObservableCollection<string> Tags
    {
        get => _tags;
        set => SetProperty(ref _tags, value);
    }

    public int CaretLine
    {
        get => _caretLine;
        set => SetProperty(ref _caretLine, value);
    }

    public int CaretColumn
    {
        get => _caretColumn;
        set => SetProperty(ref _caretColumn, value);
    }

    public int WordCount
    {
        get => _wordCount;
        private set => SetProperty(ref _wordCount, value);
    }

    public int CharacterCount
    {
        get => _characterCount;
        private set => SetProperty(ref _characterCount, value);
    }

    // 命令
    public ICommand ToggleBoldCommand { get; }
    public ICommand ToggleItalicCommand { get; }
    public ICommand ToggleStrikethroughCommand { get; }
    public ICommand InsertHeadingCommand { get; }
    public ICommand InsertLinkCommand { get; }
    public ICommand InsertCodeBlockCommand { get; }
    public ICommand InsertTableCommand { get; }
    public ICommand InsertTaskListCommand { get; }
    public ICommand InsertImageCommand { get; }
    public ICommand AddTagCommand { get; }
    public ICommand SaveCommand { get; }

    private void ToggleBold()
    {
        InsertMarkdownWrapper("**");
    }

    private void ToggleItalic()
    {
        InsertMarkdownWrapper("*");
    }

    private void ToggleStrikethrough()
    {
        InsertMarkdownWrapper("~~");
    }

    private void InsertHeading(int level)
    {
        var prefix = new string('#', level) + " ";
        InsertLinePrefix(prefix);
    }

    private void InsertLink()
    {
        InsertMarkdown("[链接文本](url)");
    }

    private void InsertCodeBlock()
    {
        InsertMarkdown("\n```csharp\n// 代码\n```\n");
    }

    private void InsertTable()
    {
        InsertMarkdown("""
            | 列1 | 列2 | 列3 |
            |-----|-----|-----|
            | 值1 | 值2 | 值3 |
            """);
    }

    private void InsertTaskList()
    {
        InsertMarkdown("""
            - [ ] 任务1
            - [ ] 任务2
            - [ ] 任务3
            """);
    }

    private void InsertImage()
    {
        InsertMarkdown("![图片描述](图片URL)");
    }

    private async Task SaveAsync()
    {
        Note.Tags = Tags.ToArray();
        await _noteService.SaveNoteAsync(Note);
    }

    private void UpdateCounts()
    {
        var text = Note.Content;
        CharacterCount = text.Length;
        WordCount = text.Split(new[] { ' ', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // 以下方法需要 TextEditor 引用
    private void InsertMarkdownWrapper(string wrapper)
    {
        // 由 View 中的 TextEditor 配合执行
    }

    private void InsertLinePrefix(string prefix)
    {
        // 由 View 中的 TextEditor 配合执行
    }

    private void InsertMarkdown(string text)
    {
        // 由 View 中的 TextEditor 配合执行
    }
}
```

### 34.8.5 导出功能

```csharp
// 笔记导出服务
public class NoteExportService
{
    private readonly MarkdownPipeline _pipeline;

    public NoteExportService()
    {
        _pipeline = MarkdownPipelineFactory.Create();
    }

    // 导出为 HTML
    public async Task ExportAsHtmlAsync(Note note, string filePath)
    {
        var htmlBody = Markdig.Markdown.ToHtml(note.Content, _pipeline);
        var html = $"""
            <!DOCTYPE html>
            <html lang="zh-CN">
            <head>
                <meta charset="utf-8" />
                <title>{note.Title}</title>
                <style>
                    body {{
                        font-family: -apple-system, BlinkMacSystemFont,
                            "Segoe UI", "Microsoft YaHei", sans-serif;
                        max-width: 800px;
                        margin: 0 auto;
                        padding: 2em;
                        line-height: 1.8;
                        color: #333;
                    }}
                    h1 {{ border-bottom: 2px solid #eee; padding-bottom: 0.3em; }}
                    h2 {{ border-bottom: 1px solid #eee; padding-bottom: 0.3em; }}
                    code {{
                        background: #f4f4f4;
                        padding: 2px 6px;
                        border-radius: 3px;
                        font-family: "Cascadia Code", Consolas, monospace;
                    }}
                    pre {{
                        background: #f4f4f4;
                        padding: 16px;
                        border-radius: 6px;
                        overflow-x: auto;
                    }}
                    pre code {{
                        background: none;
                        padding: 0;
                    }}
                    blockquote {{
                        border-left: 4px solid #ddd;
                        margin: 0;
                        padding: 0.5em 1em;
                        color: #666;
                    }}
                    table {{ border-collapse: collapse; width: 100%; }}
                    th, td {{
                        border: 1px solid #ddd;
                        padding: 8px 12px;
                        text-align: left;
                    }}
                    th {{ background: #f4f4f4; }}
                    img {{ max-width: 100%; }}
                    .meta {{
                        color: #999;
                        font-size: 0.9em;
                        margin-bottom: 2em;
                    }}
                    .tags {{ margin-top: 2em; }}
                    .tag {{
                        display: inline-block;
                        background: #e3f2fd;
                        color: #1976d2;
                        padding: 2px 10px;
                        border-radius: 12px;
                        font-size: 0.85em;
                        margin-right: 4px;
                    }}
                </style>
            </head>
            <body>
                <h1>{note.Title}</h1>
                <div class="meta">
                    创建: {note.CreatedAt:yyyy-MM-dd HH:mm} |
                    修改: {note.UpdatedAt:yyyy-MM-dd HH:mm}
                </div>
                {htmlBody}
                <div class="tags">
                    {string.Join("", note.Tags.Select(t =>
                        $"<span class='tag'>{t}</span>"))}
                </div>
            </body>
            </html>
            """;

        await File.WriteAllTextAsync(filePath, html);
    }

    // 导出为纯文本 Markdown
    public async Task ExportAsMarkdownAsync(Note note, string filePath)
    {
        var content = $"---\ntitle: {note.Title}\ndate: {note.CreatedAt:yyyy-MM-dd}\ntags: [{string.Join(", ", note.Tags)}]\n---\n\n{note.Content}";
        await File.WriteAllTextAsync(filePath, content);
    }
}
```

## 34.9 实战：聊天消息渲染

### 34.9.1 消息模型

```csharp
// 聊天消息模型
public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SenderName { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public MessageRole Role { get; set; }
    public bool IsCodeExpanded { get; set; }
}

public enum MessageRole
{
    User,
    Assistant,
    System
}
```

### 34.9.2 消息气泡控件

```xml
<!-- MessageBubble.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="clr-namespace:Markdown.Avalonia;assembly=Markdown.Avalonia"
             x:Class="MyApp.Controls.MessageBubble">

    <Border CornerRadius="12"
            Padding="12,8"
            MaxWidth="680"
            HorizontalAlignment="{Binding Alignment}">
        <Border.Styles>
            <!-- 用户消息样式 -->
            <Style Selector="Border[Tag=User]">
                <Setter Property="Background"
                        Value="{DynamicResource UserMessageBg}" />
                <Setter Property="HorizontalAlignment" Value="Right" />
            </Style>

            <!-- AI 消息样式 -->
            <Style Selector="Border[Tag=Assistant]">
                <Setter Property="Background"
                        Value="{DynamicResource AssistantMessageBg}" />
                <Setter Property="HorizontalAlignment" Value="Left" />
            </Style>
        </Border.Styles>

        <DockPanel>
            <!-- 发送者名称和时间 -->
            <StackPanel DockPanel.Dock="Top"
                        Orientation="Horizontal"
                        Spacing="8"
                        Margin="0,0,0,4">
                <TextBlock Text="{Binding SenderName}"
                           FontWeight="SemiBold"
                           FontSize="13" />
                <TextBlock Text="{Binding Timestamp,
                    StringFormat='{}{0:HH:mm}'}"
                           Foreground="{DynamicResource TextSecondary}"
                           FontSize="11"
                           VerticalAlignment="Center" />
            </StackPanel>

            <!-- 消息内容（Markdown 渲染） -->
            <md:MarkdownScrollViewer
                Markdown="{Binding Content}"
                MarkdownPipeline="{Binding Pipeline}" />
        </DockPanel>
    </Border>
</UserControl>
```

```csharp
// MessageBubble ViewModel
public class MessageBubbleViewModel : ObservableObject
{
    private static readonly MarkdownPipeline ChatPipeline =
        MarkdownPipelineFactory.CreateChatPipeline();

    public string SenderName { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public MessageRole Role { get; set; }
    public string Alignment => Role == MessageRole.User ? "Right" : "Left";
    public MarkdownPipeline Pipeline => ChatPipeline;
}
```

### 34.9.3 代码块折叠

```csharp
// 代码块折叠功能
public class CodeBlockExpander : Border
{
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<CodeBlockExpander, bool>(
            nameof(IsExpanded), defaultValue: true);

    public static readonly StyledProperty<string> LanguageProperty =
        AvaloniaProperty.Register<CodeBlockExpander, string>(
            nameof(Language));

    public static readonly StyledProperty<string> CodeProperty =
        AvaloniaProperty.Register<CodeBlockExpander, string>(
            nameof(Code));

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public string Language
    {
        get => GetValue(LanguageProperty);
        set => SetValue(LanguageProperty, value);
    }

    public string Code
    {
        get => GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public CodeBlockExpander()
    {
        var header = new DockPanel();
        var toggleBtn = new ToggleButton
        {
            Content = "▼",
            Classes = { "code-toggle" }
        };
        toggleBtn.IsCheckedChanged += (s, e) =>
        {
            IsExpanded = toggleBtn.IsChecked == true;
        };

        var langLabel = new TextBlock
        {
            Text = Language,
            Foreground = new SolidColorBrush(Color.Parse("#6C7086")),
            FontSize = 12
        };

        var copyBtn = new Button
        {
            Content = "复制",
            Classes = { "copy-button" }
        };
        copyBtn.Click += async (s, e) =>
        {
            await TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(Code)!;
        };

        header.Children.Add(toggleBtn);
        header.Children.Add(langLabel);
        header.Children.Add(copyBtn);

        Child = new DockPanel();
    }
}
```

### 34.9.4 @提及高亮

```csharp
// @提及处理
public class MentionProcessor
{
    private static readonly Regex MentionRegex = new(
        @"@(\w+)", RegexOptions.Compiled);

    // 检测并高亮 @提及
    public static string ProcessMentions(string text,
        IEnumerable<string> knownUsers)
    {
        var users = new HashSet<string>(knownUsers,
            StringComparer.OrdinalIgnoreCase);

        return MentionRegex.Replace(text, match =>
        {
            var username = match.Groups[1].Value;
            if (users.Contains(username))
            {
                return $"<span class='mention'>@{username}</span>";
            }
            return match.Value;
        });
    }

    // 提取所有提及的用户
    public static IEnumerable<string> ExtractMentions(string text)
    {
        return MentionRegex.Matches(text)
            .Select(m => m.Groups[1].Value)
            .Distinct();
    }
}
```

```markdown
<!-- 消息中的 @提及示例 -->
@Alice 你好！请查看这个 PR。

@Bob @Charlie 这个 bug 可能和 @David 的代码有关。

@所有人 注意：今晚有维护窗口。
```

### 34.9.5 链接预览

```csharp
// 链接预览生成
public class LinkPreviewGenerator
{
    private readonly HttpClient _httpClient = new();

    public async Task<LinkPreview?> GeneratePreviewAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(url);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(response);

            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText
                ?? "";
            var description = doc.DocumentNode
                .SelectSingleNode("//meta[@name='description']")
                ?.GetAttributeValue("content", "") ?? "";
            var image = doc.DocumentNode
                .SelectSingleNode("//meta[@property='og:image']")
                ?.GetAttributeValue("content", "");

            return new LinkPreview
            {
                Url = url,
                Title = title.Trim(),
                Description = description.Trim(),
                ImageUrl = image
            };
        }
        catch
        {
            return null;
        }
    }
}

public class LinkPreview
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? ImageUrl { get; set; }
}
```

## 34.10 数学公式渲染

### 34.10.1 LaTeX 语法简介

```markdown
<!-- LaTeX 数学公式语法速览 -->

<!-- 行内公式 -->
质能方程 $E = mc^2$ 是物理学的基本公式。

二次方程 $ax^2 + bx + c = 0$ 的解为
$x = \frac{-b \pm \sqrt{b^2 - 4ac}}{2a}$

<!-- 块级公式 -->
$$
\int_{-\infty}^{\infty} e^{-x^2} dx = \sqrt{\pi}
$$

$$
\sum_{n=1}^{\infty} \frac{1}{n^2} = \frac{\pi^2}{6}
$$

<!-- 矩阵 -->
$$
\begin{pmatrix}
a & b \\
c & d
\end{pmatrix}
$$
```

### 34.10.2 MathJax 集成（WebView 方式）

```csharp
// MathJax 渲染器
public class MathJaxRenderer
{
    public string WrapWithMathJax(string htmlBody)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8" />
                <script>
                    window.MathJax = {{
                        tex: {{
                            inlineMath: [['$', '$'], ['\\(', '\\)']],
                            displayMath: [['$$', '$$'], ['\\[', '\\]']],
                            processEscapes: true
                        }},
                        options: {{
                            skipHtmlTags: ['script', 'noscript',
                                'style', 'textarea', 'pre']
                        }}
                    }};
                </script>
                <!-- 生产环境务必生成并添加 integrity 哈希值，
                     运行: curl -s URL | openssl dgst -sha384 -binary | openssl base64 -A -->
                <script src="https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js"
                        crossorigin="anonymous"
                        referrerpolicy="no-referrer"
                        async></script>
                <style>
                    body {{
                        font-family: -apple-system, "Microsoft YaHei",
                            sans-serif;
                        color: #CDD6F4;
                        background: #1E1E2E;
                        padding: 16px;
                        line-height: 1.8;
                    }}
                </style>
            </head>
            <body>
                {htmlBody}
            </body>
            </html>
            """;
    }
}
```

### 34.10.3 WpfMath/AvaloniaMath（原生方式）

```xml
<!-- 安装 AvaloniaMath -->
<PackageReference Include="AvaloniaMath" Version="11.*" />
```

```xml
<!-- 在 AXAML 中使用数学公式控件 -->
<Window xmlns:math="clr-namespace:WpfMath.Avalonia;assembly=WpfMath.Avalonia">
    <StackPanel Spacing="16" Margin="20">
        <!-- 基本公式 -->
        <math:FormulaControl
            Formula="E = mc^2"
            FontSize="20"
            Foreground="{DynamicResource TextPrimary}" />

        <!-- 分数 -->
        <math:FormulaControl
            Formula="\frac{a}{b}"
            FontSize="24" />

        <!-- 积分 -->
        <math:FormulaControl
            Formula="\int_{0}^{\infty} e^{-x^2} dx = \frac{\sqrt{\pi}}{2}"
            FontSize="18" />

        <!-- 矩阵 -->
        <math:FormulaControl
            Formula="\begin{pmatrix} a & b \\ c & d \end{pmatrix}"
            FontSize="20" />

        <!-- 求和 -->
        <math:FormulaControl
            Formula="\sum_{n=0}^{\infty} \frac{x^n}{n!} = e^x"
            FontSize="18" />
    </StackPanel>
</Window>
```

```csharp
// 代码中渲染公式
using WpfMath;
using WpfMath.Avalonia;

public class FormulaRenderer
{
    private readonly TexFormulaParser _parser = new();

    public Control? RenderFormula(string latex, double fontSize = 16)
    {
        try
        {
            var formula = _parser.Parse(latex);
            var renderer = formula.GetRenderer(
                TexStyle.Display, fontSize, "Cambria Math");

            var control = new FormulaControl
            {
                Formula = latex,
                FontSize = fontSize
            };

            return control;
        }
        catch (TexException ex)
        {
            return new TextBlock
            {
                Text = $"公式错误: {ex.Message}",
                Foreground = new SolidColorBrush(Colors.Red)
            };
        }
    }
}
```

### 34.10.4 行内公式 vs 块级公式

```csharp
// LaTeX 公式检测和处理
public class MathFormulaProcessor
{
    // 行内公式: $...$ 或 \(...\)
    private static readonly Regex InlineMathRegex = new(
        @"\$([^\$\n]+?)\$|\\\((.+?)\\\)",
        RegexOptions.Compiled);

    // 块级公式: $$...$$ 或 \[...\]
    private static readonly Regex BlockMathRegex = new(
        @"\$\$([\s\S]+?)\$\$|\\\[([\s\S]+?)\\\]",
        RegexOptions.Compiled);

    // 将 Markdown 中的公式转换为 HTML
    public static string ProcessFormulas(string markdown)
    {
        // 先处理块级公式
        markdown = BlockMathRegex.Replace(markdown, match =>
        {
            var formula = match.Groups[1].Success
                ? match.Groups[1].Value
                : match.Groups[2].Value;
            return $"""
                <div class="math-block">
                    \[{formula}\]
                </div>
                """;
        });

        // 再处理行内公式
        markdown = InlineMathRegex.Replace(markdown, match =>
        {
            var formula = match.Groups[1].Success
                ? match.Groups[1].Value
                : match.Groups[2].Value;
            return $"""<span class="math-inline">\({formula}\)</span>""";
        });

        return markdown;
    }
}
```

## 34.11 代码块高亮主题

### 34.11.1 主题配置系统

```csharp
// 代码高亮主题管理
public class CodeThemeManager
{
    private readonly Dictionary<string, EditorTheme> _themes = new();

    public CodeThemeManager()
    {
        // 注册内置主题
        Register("monokai", EditorThemes.Monokai);
        Register("dracula", EditorThemes.Dracula);
        Register("solarized-dark", EditorThemes.SolarizedDark);
        Register("one-dark", EditorThemes.OneDark);
        Register("catppuccin", EditorThemes.CatppuccinMocha);
    }

    public void Register(string name, EditorTheme theme)
    {
        _themes[name] = theme;
    }

    public EditorTheme? GetTheme(string name)
    {
        return _themes.GetValueOrDefault(name);
    }

    public IEnumerable<string> GetThemeNames() => _themes.Keys;
}
```

### 34.11.2 自定义主题创建

```csharp
// 创建自定义代码高亮主题
public class CustomCodeTheme
{
    // GitHub Dark 主题
    public static EditorTheme GitHubDark => new(
        Background:    Color.Parse("#0D1117"),
        Foreground:    Color.Parse("#C9D1D9"),
        LineNumber:    Color.Parse("#484F58"),
        Selection:     Color.Parse("#264F78"),
        CurrentLine:   Color.Parse("#161B22"),
        Keyword:       Color.Parse("#FF7B72"),
        Comment:       Color.Parse("#8B949E"),
        String:        Color.Parse("#A5D6FF"),
        Number:        Color.Parse("#79C0FF"),
        Operator:      Color.Parse("#FF7B72")
    );

    // Tokyo Night 主题
    public static EditorTheme TokyoNight => new(
        Background:    Color.Parse("#1A1B26"),
        Foreground:    Color.Parse("#A9B1D6"),
        LineNumber:    Color.Parse("#3B4261"),
        Selection:     Color.Parse("#33467C"),
        CurrentLine:   Color.Parse("#1F2335"),
        Keyword:       Color.Parse("#BB9AF7"),
        Comment:       Color.Parse("#565F89"),
        String:        Color.Parse("#9ECE6A"),
        Number:        Color.Parse("#FF9E64"),
        Operator:      Color.Parse("#89DDFF")
    );

    // Nord 主题
    public static EditorTheme Nord => new(
        Background:    Color.Parse("#2E3440"),
        Foreground:    Color.Parse("#D8DEE9"),
        LineNumber:    Color.Parse("#4C566A"),
        Selection:     Color.Parse("#434C5E"),
        CurrentLine:   Color.Parse("#3B4252"),
        Keyword:       Color.Parse("#81A1C1"),
        Comment:       Color.Parse("#616E88"),
        String:        Color.Parse("#A3BE8C"),
        Number:        Color.Parse("#B48EAD"),
        Operator:      Color.Parse("#81A1C1")
    );
}
```

### 34.11.3 动态主题切换

```xml
<!-- 主题切换 UI -->
<StackPanel Orientation="Horizontal" Spacing="8" Margin="8">
    <TextBlock Text="代码主题:"
               VerticalAlignment="Center" />
    <ComboBox x:Name="ThemeSelector"
              SelectedIndex="0"
              Width="180">
        <ComboBoxItem Content="Monokai" />
        <ComboBoxItem Content="Dracula" />
        <ComboBoxItem Content="Solarized Dark" />
        <ComboBoxItem Content="One Dark" />
        <ComboBoxItem Content="Catppuccin Mocha" />
        <ComboBoxItem Content="GitHub Dark" />
        <ComboBoxItem Content="Tokyo Night" />
        <ComboBoxItem Content="Nord" />
    </ComboBox>
</StackPanel>
```

```csharp
// 主题切换逻辑
public partial class ThemeSettingsView : UserControl
{
    private readonly CodeThemeManager _themeManager;
    private readonly TextEditor _editor;

    public ThemeSettingsView()
    {
        InitializeComponent();

        _themeManager = new CodeThemeManager();
        _editor = this.FindControl<TextEditor>("Editor")!;

        ThemeSelector.SelectionChanged += OnThemeChanged;
    }

    private void OnThemeChanged(object? sender,
        SelectionChangedEventArgs e)
    {
        var themeName = (ThemeSelector.SelectedItem as ComboBoxItem)?
            .Content?.ToString()?.ToLower()
            .Replace(" ", "-") ?? "monokai";

        var theme = _themeManager.GetTheme(themeName);
        if (theme != null)
        {
            ApplyTheme(_editor, theme);
        }
    }

    private void ApplyTheme(TextEditor editor, EditorTheme theme)
    {
        editor.Background = new SolidColorBrush(theme.Background);
        editor.Foreground = new SolidColorBrush(theme.Foreground);
        editor.LineNumberForeground = new SolidColorBrush(theme.LineNumber);
        editor.SelectionBrush = new SolidColorBrush(theme.Selection);
        editor.CurrentLineBackground = new SolidColorBrush(theme.CurrentLine);
    }
}
```

## Deep Dive

### Markdown 解析器原理

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Markdown 文本 │───>│   词法分析    │───>│  Token 流    │
│  (原始字符串)  │    │  (Lexer)     │    │              │
└──────────────┘    └──────────────┘    └──────┬───────┘
                                               │
                                               v
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   HTML 输出   │<───│   渲染器     │<───│  AST 语法树  │
│  或控件树     │    │  (Renderer)  │    │  (MarkdownDocument) │
└──────────────┘    └──────────────┘    └──────────────┘
```

**Markdig 的 AST 结构：**

```csharp
// Markdig AST 节点类型
MarkdownDocument          -- 根节点
├── HeadingBlock          -- 标题 (H1-H6)
│   └── ContainerInline
│       └── LiteralInline -- 文本内容
├── ParagraphBlock        -- 段落
│   └── ContainerInline
│       ├── LiteralInline
│       ├── EmphasisInline -- 强调 (粗/斜)
│       ├── LinkInline     -- 链接
│       ├── CodeInline     -- 行内代码
│       └── LineBreakInline
├── ListBlock             -- 列表
│   └── ListItemBlock
│       └── ParagraphBlock
├── FencedCodeBlock       -- 代码块
├── QuoteBlock            -- 引用块
├── Table                 -- 表格
│   ├── TableRow (Header)
│   └── TableRow
│       └── TableCell
├── ThematicBreakBlock    -- 水平线
└── HtmlBlock             -- HTML 块
```

```csharp
// 遍历 Markdig AST
public static void InspectMarkdown(string markdown)
{
    var pipeline = new MarkdownPipelineBuilder().Build();
    var document = Markdig.Markdown.Parse(markdown, pipeline);

    foreach (var block in document)
    {
        switch (block)
        {
            case HeadingBlock heading:
                Console.WriteLine($"标题 H{heading.Level}: {heading.Inline}");
                break;

            case ParagraphBlock paragraph:
                Console.WriteLine($"段落: {paragraph.Inline}");
                break;

            case FencedCodeBlock code:
                Console.WriteLine($"代码块 [{code.Info}]: {code.Lines}");
                break;

            case ListBlock list:
                Console.WriteLine($"列表 ({list.IsOrdered})");
                foreach (ListItemBlock item in list)
                {
                    Console.WriteLine($"  - {item}");
                }
                break;

            case QuoteBlock quote:
                Console.WriteLine("引用块");
                break;

            case Table table:
                Console.WriteLine($"表格: {table.Count} 行");
                break;
        }
    }
}
```

### 渲染管线性能优化

```csharp
// 增量渲染优化
public class IncrementalMarkdownRenderer
{
    private string _lastMarkdown = "";
    private string _lastHtml = "";

    public string Render(string markdown)
    {
        // 快速路径：内容没变
        if (markdown == _lastMarkdown) return _lastHtml;

        // 增量更新：只重新渲染变化的部分
        var diff = ComputeDiff(_lastMarkdown, markdown);

        if (diff.IsMinorChange)
        {
            // 只更新变化的段落
            _lastHtml = PatchHtml(_lastHtml, diff);
        }
        else
        {
            // 完全重新渲染
            _lastHtml = Markdig.Markdown.ToHtml(markdown);
        }

        _lastMarkdown = markdown;
        return _lastHtml;
    }

    private MarkdownDiff ComputeDiff(string oldText, string newText)
    {
        // 简单的 diff 策略：比较段落数量和内容
        var oldLines = oldText.Split('\n');
        var newLines = newText.Split('\n');

        if (Math.Abs(oldLines.Length - newLines.Length) > 5)
        {
            return new MarkdownDiff { IsMinorChange = false };
        }

        // 找到第一个不同行
        int diffStart = 0;
        while (diffStart < Math.Min(oldLines.Length, newLines.Length)
               && oldLines[diffStart] == newLines[diffStart])
        {
            diffStart++;
        }

        return new MarkdownDiff
        {
            IsMinorChange = true,
            DiffStartLine = diffStart
        };
    }

    private string PatchHtml(string html, MarkdownDiff diff)
    {
        // 实际项目中，这里需要更复杂的 HTML diff/patch 逻辑
        return html;
    }
}

public class MarkdownDiff
{
    public bool IsMinorChange { get; set; }
    public int DiffStartLine { get; set; }
}
```

## Cross References

- **第 8 章 数据模板** -- 消息气泡和列表项的模板设计
- **第 14 章 自定义渲染** -- 自定义绘图和渲染逻辑
- **第 31 章 系统集成** -- 文件关联、剪贴板操作
- **第 33 章 WebView 集成** -- 通过 WebView 渲染 HTML/Markdown

## Common Pitfalls

1. **不要在每次按键时都重新渲染 Markdown**。Markdown 解析虽然快，但对大文档来说频繁渲染仍会卡顿。使用防抖（Debounce）延迟 200-300ms 再渲染。

2. **不要信任用户输入的 HTML**。如果允许 Markdown 中的 HTML 标签，要注意 XSS 风险。聊天场景应禁用 HTML（`DisableHtml()`）。

3. **不要忽略大文档的性能**。Markdown.Avalonia 对超长文档（>10000 行）可能变慢。考虑虚拟化或只渲染可见区域。

4. **同步滚动不要用等距偏移**。编辑器和预览的内容高度不同，应按比例（百分比）同步，而非固定像素。

5. **图片路径要区分相对和绝对**。Markdown 中的 `![](image.png)` 是相对路径，在不同平台上的解析可能不同。建议使用绝对 URL 或 `avares://` 协议。

6. **AvaloniaEdit 的 Text 属性绑定要设 Mode=TwoWay**。否则编辑器中的修改不会回写到 ViewModel。

7. **语法高亮文件（.xshd）的 XML 实体需要转义**。在 .xshd 文件中，`>` 要写成 `&gt;`，`<` 要写成 `&lt;`，`&` 要写成 `&amp;`。

8. **不要在 RichTextBox 中直接用 Markdown 语法**。RichTextBox 使用 FlowDocument 模型，与 Markdown 完全不同。需要显示 Markdown 用 MarkdownScrollViewer，需要编辑 Markdown 用 TextEditor。

9. **数学公式渲染在离线环境下可能失败**。MathJax 通过 CDN 加载，离线时需要本地部署或使用 AvaloniaMath 原生方案。

10. **代码块的语言标识要与语法定义匹配**。```csharp 和 ```cs 可能指向不同的高亮定义，确保使用标准的标识名。

## Try It Yourself

1. **基础 Markdown 预览器**：创建一个窗口，左侧 TextBox 编辑 Markdown，右侧 MarkdownScrollViewer 实时预览。尝试输入各种 Markdown 语法。

2. **增强编辑器**：使用 AvaloniaEdit 替换 TextBox，添加行号显示、Markdown 语法高亮、以及 Ctrl+B/Ctrl+I 快捷键。

3. **代码块主题切换**：在编辑器中添加一个 ComboBox，让用户切换 Monokai/Dracula/One Dark 等代码高亮主题。

4. **聊天消息界面**：实现一个类似 ChatGPT 的聊天界面，左侧显示用户消息，右侧显示 AI 回复，AI 回复中的 Markdown 应正确渲染。

5. **笔记应用 MVP**：实现一个最小的笔记应用，支持创建、编辑、预览笔记，使用 SQLite 存储，支持按标题搜索。

6. **Markdown 转 HTML 导出**：添加一个"导出"按钮，将当前编辑的 Markdown 内容转换为带样式的 HTML 文件并保存。

7. **同步滚动**：在左右分栏布局中实现编辑器和预览的同步滚动功能，确保滚动比例一致。

8. **数学公式支持**：在 Markdown 预览中启用 MathJax 或 AvaloniaMath，输入 LaTeX 公式验证渲染效果。
