# 35. 自动更新与应用分发

> **写给零基础的你**：你的软件发布后，用户怎么获取最新版本？手动下载安装太麻烦了。自动更新功能让你的软件能自己检查新版本、下载更新、然后自动安装——就像 Chrome 浏览器那样，你甚至感觉不到它在更新。

## 35.1 概述

本章涵盖 Avalonia 应用的自动更新与分发：

- **Velopack**：现代跨平台自动更新框架（推荐）
- **Squirrel.Windows**：Windows 传统的自动更新方案
- **Native AOT 发布**：独立发布，无需运行时
- **安装包制作**：Windows/macOS/Linux 安装程序
- **代码签名**：确保软件来源可信

## 35.2 Velopack（推荐方案）

### 35.2.1 什么是 Velopack

Velopack 是 Squirrel.Windows 的精神继承者，支持 Windows、macOS 和 Linux。它提供了：
- 增量更新（只下载变化的部分）
- 静默更新（用户无感知）
- 回滚机制（更新失败自动恢复）
- 跨平台支持

### 35.2.2 安装配置

```xml
<PackageReference Include="Velopack" Version="0.*" />
```

### 35.2.3 应用启动集成

```csharp
// Program.cs - Velopack 必须在 Avalonia 之前初始化
using Velopack;

public static void Main(string[] args)
{
    // Velopack 初始化（必须在最开始）
    VelopackApp.Build()
        .WithFirstRun((v) =>
        {
            // 首次安装运行时执行
            Console.WriteLine($"首次安装版本 {v}");
        })
        .WithBeforeUpdateFastCallback(() =>
        {
            // 更新前的快速回调（如保存数据）
        })
        .Run();

    // 正常的 Avalonia 启动
    BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
}
```

### 35.2.4 检查和应用更新

```csharp
public class UpdateService
{
    private readonly UpdateManager _mgr;
    private readonly string _updateUrl;

    public UpdateService(string updateUrl)
    {
        _updateUrl = updateUrl;
        _mgr = new UpdateManager(updateUrl);
    }

    /// <summary>
    /// 检查是否有新版本
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            return await _mgr.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "检查更新失败");
            return null;
        }
    }

    /// <summary>
    /// 下载更新
    /// </summary>
    public async Task DownloadAndUpdateAsync(UpdateInfo update,
        Action<int>? onProgress = null)
    {
        var progress = new Progress<int>(p => onProgress?.Invoke(p));
        await _mgr.DownloadUpdatesAsync(update, progress);
    }

    /// <summary>
    /// 应用更新并重启
    /// </summary>
    public void ApplyUpdatesAndRestart(UpdateInfo update)
    {
        _mgr.ApplyUpdatesAndRestart(update);
    }

    /// <summary>
    /// 当前版本
    /// </summary>
    public Version? CurrentVersion => _mgr.CurrentVersion;

    /// <summary>
    /// 是否已安装（非便携版）
    /// </summary>
    public bool IsInstalled => _mgr.IsInstalled;
}
```

### 35.2.5 在 UI 中集成更新流程

```csharp
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly UpdateService _updateService;

    [ObservableProperty]
    private bool _isCheckingUpdate;

    [ObservableProperty]
    private int _updateProgress;

    [ObservableProperty]
    private string? _updateStatus;

    [RelayCommand]
    private async Task CheckForUpdate()
    {
        IsCheckingUpdate = true;
        UpdateStatus = "正在检查更新...";

        try
        {
            var update = await _updateService.CheckForUpdatesAsync();
            if (update == null)
            {
                UpdateStatus = "已是最新版本";
                return;
            }

            UpdateStatus = $"发现新版本 {update.TargetFullRelease.Version}，正在下载...";

            await _updateService.DownloadAndUpdateAsync(update, progress =>
            {
                UpdateProgress = progress;
            });

            UpdateStatus = "更新已就绪，重启后生效";

            // 询问用户是否立即重启
            var result = await ShowDialog("更新下载完成", "是否立即重启以应用更新？");
            if (result)
            {
                _updateService.ApplyUpdatesAndRestart(update);
            }
        }
        catch (Exception ex)
        {
            UpdateStatus = $"更新失败: {ex.Message}";
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    /// <summary>
    /// 应用启动时自动检查更新
    /// </summary>
    public async Task AutoCheckUpdate()
    {
        // 延迟 5 秒，让应用先完成初始化
        await Task.Delay(5000);

        var update = await _updateService.CheckForUpdatesAsync();
        if (update != null)
        {
            // 静默下载
            await _updateService.DownloadAndUpdateAsync(update);

            // 下载完成后提示用户
            UpdateStatus = "有新版本可用，重启后生效";
        }
    }
}
```

### 35.2.6 打包发布

```bash
# 使用 vpk CLI 工具打包
# 安装: dotnet tool install -g vkp

# Windows 安装包
vpk pack --packId MyApp --packVersion 1.0.0 --packDir ./publish/win-x64 --runtime win-x64

# macOS 安装包
vpk pack --packId MyApp --packVersion 1.0.0 --packDir ./publish/osx-arm64 --runtime osx-arm64

# Linux 安装包
vpk pack --packId MyApp --packVersion 1.0.0 --packDir ./publish/linux-x64 --runtime linux-x64
```

## 35.3 Native AOT 发布

### 35.3.1 什么是 Native AOT

Native AOT（Ahead-of-Time）将 .NET 应用编译为原生机器码，无需安装 .NET 运行时：

| 特性 | 普通发布 | Self-Contained | Native AOT |
|------|---------|---------------|------------|
| 需要运行时 | 是 | 否 | 否 |
| 启动速度 | 慢 | 中等 | 快 |
| 内存占用 | 高 | 高 | 低 |
| 文件大小 | 小 | 大 | 中等 |
| 兼容性 | 高 | 高 | 部分限制 |

### 35.3.2 配置 AOT 发布

```xml
<!-- .csproj -->
<PropertyGroup>
    <PublishAot>true</PublishAot>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <StripSymbols>true</StripSymbols>
    <InvariantGlobalization>false</InvariantGlobalization>
</PropertyGroup>
```

```bash
# 发布命令
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishAot=true
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishAot=true
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```

### 35.3.3 AOT 兼容性注意事项

```csharp
// AOT 不支持反射的场景需要源生成
// JSON 序列化
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(List<Project>))]
internal partial class AppJsonContext : JsonSerializerContext { }

// 使用源生成的序列化上下文
var json = JsonSerializer.Serialize(config, AppJsonContext.Default.AppConfig);
var config = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppConfig);
```

### 35.3.4 Avalonia 的 AOT 支持

```csharp
// Avalonia 已内置 AOT 支持
// 需要使用编译绑定（x:DataType）替代运行时绑定
// 详见第 15 章
```

## 35.4 安装包制作

### 35.4.1 Windows 安装程序

**Inno Setup：**

```ini
[Setup]
AppName=MyApp
AppVersion=1.0.0
DefaultDirName={autopf}\MyApp
DefaultGroupName=MyApp
OutputDir=installer
OutputBaseFilename=MyApp-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "publish\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\MyApp"; Filename: "{app}\MyApp.exe"
Name: "{group}\Uninstall MyApp"; Filename: "{uninstallexe}"
Name: "{autodesktop}\MyApp"; Filename: "{app}\MyApp.exe"

[Run]
Filename: "{app}\MyApp.exe"; Description: "Launch MyApp"; Flags: nowait postinstall
```

**MSIX 打包（Windows Store）：**

```xml
<!-- Package.appxmanifest -->
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10">
    <Identity Name="MyCompany.MyApp" Version="1.0.0.0"
              Publisher="CN=MyCompany" />
    <Properties>
        <DisplayName>My App</DisplayName>
        <PublisherDisplayName>My Company</PublisherDisplayName>
        <Logo>Assets\StoreLogo.png</Logo>
    </Properties>
    <Applications>
        <Application Id="App" Executable="MyApp.exe" EntryPoint="MyApp.Program">
            <uap:VisualElements DisplayName="My App"
                               Description="My desktop application"
                               BackgroundColor="transparent"
                               Square150x150Logo="Assets\Square150x150Logo.png">
            </uap:VisualElements>
        </Application>
    </Applications>
</Package>
```

### 35.4.2 macOS 安装包

```bash
# 创建 .app bundle
mkdir -p MyApp.app/Contents/MacOS
mkdir -p MyApp.app/Contents/Resources

# 复制可执行文件
cp publish/osx-arm64/MyApp MyApp.app/Contents/MacOS/

# 创建 Info.plist
cat > MyApp.app/Contents/Info.plist << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>MyApp</string>
    <key>CFBundleIdentifier</key>
    <string>com.mycompany.myapp</string>
    <key>CFBundleName</key>
    <string>MyApp</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon.icns</string>
</dict>
</plist>
EOF

# 创建 DMG
hdiutil create -volname "MyApp" -srcfolder MyApp.app -ov -format UDZO MyApp.dmg
```

### 35.4.3 Linux 打包

```bash
# AppImage
# 使用 appimagetool
./appimagetool MyApp.AppDir MyApp-x86_64.AppImage

# .deb 包
mkdir -p myapp_1.0.0_amd64/DEBIAN
mkdir -p myapp_1.0.0_amd64/usr/local/bin
cp publish/linux-x64/MyApp myapp_1.0.0_amd64/usr/local/bin/
dpkg-deb --build myapp_1.0.0_amd64

# Flatpak / Snap 也是可选方案
```

## 35.5 代码签名

### 35.5.1 Windows 代码签名

```bash
# 使用 SignTool
signtool sign /f certificate.pfx /p password /tr http://timestamp.digicert.com /td sha256 /fd sha256 MyApp.exe
```

### 35.5.2 macOS 签名和公证

```bash
# 签名
codesign --force --options runtime --sign "Developer ID Application: Your Name (TEAMID)" MyApp.app

# 公证
xcrun notarytool submit MyApp.dmg --apple-id your@email.com --password app-specific-password --team-id TEAMID

# 装订
xcrun stapler staple MyApp.dmg
```

## 35.6 CI/CD 集成

```yaml
# GitHub Actions 示例
name: Release
on:
  push:
    tags: ['v*']

jobs:
  release:
    strategy:
      matrix:
        os: [windows-latest, macos-latest, ubuntu-latest]
        include:
          - os: windows-latest
            rid: win-x64
          - os: macos-latest
            rid: osx-arm64
          - os: ubuntu-latest
            rid: linux-x64

    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Publish AOT
        run: dotnet publish -c Release -r ${{ matrix.rid }} --self-contained true -p:PublishAot=true

      - name: Package with Velopack
        run: vpk pack --packId MyApp --packVersion ${{ github.ref_name }} --packDir ./publish/${{ matrix.rid }}

      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: ${{ matrix.rid }}
          path: ./releases/*
```

## 35.7 Deep Dive: 更新策略设计

### 35.7.1 强制更新 vs 可选更新

```csharp
public class UpdatePolicy
{
    public enum UpdateType
    {
        Silent,      // 静默安装，下次启动生效
        Notify,      // 通知用户，用户选择时机
        Force        // 强制更新，不更新无法使用
    }

    public static UpdateType GetUpdateType(Version current, Version available)
    {
        // 主版本号不同 = 强制更新（破坏性变更）
        if (available.Major != current.Major)
            return UpdateType.Force;

        // 次版本号不同 = 通知更新（新功能）
        if (available.Minor != current.Minor)
            return UpdateType.Notify;

        // 补丁版本 = 静默更新（Bug 修复）
        return UpdateType.Silent;
    }
}
```

### 35.7.2 更新源配置

```csharp
// 支持多种更新源
public class UpdateSourceConfig
{
    public string GitHubReleases { get; set; } = "https://github.com/myorg/myapp/releases";
    public string CustomServer { get; set; } = "https://updates.myapp.com";
    public string S3Bucket { get; set; } = "https://myapp-releases.s3.amazonaws.com";
}
```

## 35.8 Cross References

- **第 15 章**：编译绑定与 AOT 发布（Native AOT 详细配置）
- **第 2 章**：项目结构与启动流程（应用启动时的更新检查）

## 35.9 Common Pitfalls

1. **Velopack 初始化顺序**：必须在 Avalonia 启动之前调用 `VelopackApp.Build().Run()`
2. **AOT 兼容性**：不是所有 NuGet 包都兼容 AOT，需要测试
3. **macOS 签名**：未签名的应用在 macOS 上会被 Gatekeeper 阻止
4. **更新回滚**：确保旧版本数据格式与新版本兼容
5. **网络超时**：更新检查可能因网络问题失败，需要优雅降级
6. **文件锁定**：Windows 上更新时可能文件被锁定，需要重启替换

## 35.10 Try It Yourself

1. 集成 Velopack，实现应用启动时自动检查更新
2. 使用 Native AOT 发布你的 Avalonia 应用
3. 为 Windows 创建 Inno Setup 安装程序
4. 配置 GitHub Actions 实现跨平台自动发布
