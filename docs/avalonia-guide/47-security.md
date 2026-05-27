# 42. 安全 -- 认证、加密与权限

> **写给零基础的你**：安全就是保护你的软件和用户数据不被坏人攻击或窃取。比如用户登录时要加密密码传输，保存敏感数据时要加密存储。本章教你如何在 Avalonia 应用中实现安全功能。

## 42.1 概述

本章涵盖 Avalonia 应用的安全实践：

- **OAuth2/OIDC 认证**：第三方登录
- **JWT Token 管理**：API 认证
- **数据加密**：文件加密/解密
- **安全存储**：密钥和凭据保护
- **输入验证**：防止注入攻击
- **代码签名**：确保软件来源可信

## 42.2 OAuth2/OIDC 认证

### 42.2.1 浏览器登录流程

```csharp
// NuGet: IdentityModel.OidcClient
using IdentityModel.OidcClient;

public class OAuthService
{
    private readonly OidcClient _client;

    public OAuthService()
    {
        var options = new OidcClientOptions
        {
            Authority = "https://auth.example.com",
            ClientId = "myapp",
            ClientSecret = "secret",
            RedirectUri = "myapp://callback",
            Scope = "openid profile email",
            Browser = new SystemBrowser() // 使用系统浏览器
        };

        _client = new OidcClient(options);
    }

    public async Task<LoginResult> LoginAsync()
    {
        return await _client.LoginAsync();
    }

    public async Task<LogoutResult> LogoutAsync()
    {
        return await _client.LogoutAsync();
    }
}

// 系统浏览器实现（简化）
public class SystemBrowser : IBrowser
{
    public async Task<BrowserResult> InvokeAsync(BrowserOptions options,
        CancellationToken cancellationToken = default)
    {
        // 在系统浏览器中打开授权页面
        Process.Start(new ProcessStartInfo
        {
            FileName = options.StartUrl,
            UseShellExecute = true
        });

        // 等待回调（通过本地 HTTP 监听器或自定义 URI 处理）
        var code = await WaitForCallbackAsync();
        return new BrowserResult
        {
            Response = code,
            ResultType = BrowserResultType.Success
        };
    }
}
```

### 42.2.2 在 Avalonia 中嵌入 WebView 登录

```csharp
public class WebViewOAuthService
{
    public async Task<string?> LoginWithWebView(string authorizeUrl, string redirectUri)
    {
        var tcs = new TaskCompletionSource<string?>();

        var window = new Window
        {
            Width = 600,
            Height = 700,
            Title = "登录"
        };

        var webView = new WebView();
        window.Content = webView;

        webView.Address = authorizeUrl;
        webView.BeforeNavigate += (url) =>
        {
            if (url.StartsWith(redirectUri))
            {
                var code = ExtractCode(url);
                tcs.SetResult(code);
                window.Close();
                return false;
            }
            return true;
        };

        window.Show();
        return await tcs.Task;
    }

    private string? ExtractCode(string url)
    {
        var uri = new Uri(url);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query["code"];
    }
}
```

## 42.3 JWT Token 管理

### 42.3.1 Token 存储和刷新

```csharp
public class TokenManager
{
    private readonly ISecureStorage _secureStorage;
    private readonly HttpClient _httpClient;
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _expiresAt;

    public TokenManager(ISecureStorage secureStorage, HttpClient httpClient)
    {
        _secureStorage = secureStorage;
        _httpClient = httpClient;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        // 如果 Token 未过期，直接返回
        if (_accessToken != null && DateTime.UtcNow < _expiresAt.AddMinutes(-5))
        {
            return _accessToken;
        }

        // 尝试刷新 Token
        if (_refreshToken != null)
        {
            var success = await RefreshTokenAsync();
            if (success) return _accessToken!;
        }

        // 需要重新登录
        throw new AuthenticationException("需要重新登录");
    }

    private async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/oauth/token", new
            {
                grant_type = "refresh_token",
                refresh_token = _refreshToken,
                client_id = "myapp"
            });

            if (!response.IsSuccessStatusCode) return false;

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            UpdateTokens(tokenResponse!);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void UpdateTokens(TokenResponse response)
    {
        _accessToken = response.AccessToken;
        _refreshToken = response.RefreshToken;
        _expiresAt = DateTime.UtcNow.AddSeconds(response.ExpiresIn);

        // 安全存储
        _secureStorage.Save("access_token", _accessToken);
        _secureStorage.Save("refresh_token", _refreshToken);
    }

    public void ClearTokens()
    {
        _accessToken = null;
        _refreshToken = null;
        _secureStorage.Delete("access_token");
        _secureStorage.Delete("refresh_token");
    }
}
```

### 42.3.2 HttpClient 自动附加 Token

```csharp
public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly TokenManager _tokenManager;

    public AuthenticatedHttpClientHandler(TokenManager tokenManager)
    {
        _tokenManager = tokenManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenManager.GetAccessTokenAsync();
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        // 如果 401，尝试刷新 Token 后重试
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            token = await _tokenManager.GetAccessTokenAsync();
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }
}
```

## 42.4 数据加密

### 42.4.1 AES 文件加密

```csharp
using System.Security.Cryptography;

public static class FileEncryption
{
    public static void EncryptFile(string inputPath, string outputPath, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var outputStream = File.Create(outputPath);
        // 写入 IV（初始化向量）
        outputStream.Write(aes.IV, 0, aes.IV.Length);

        using var cryptoStream = new CryptoStream(outputStream,
            aes.CreateEncryptor(), CryptoStreamMode.Write);
        using var inputStream = File.OpenRead(inputPath);
        inputStream.CopyTo(cryptoStream);
    }

    public static void DecryptFile(string inputPath, string outputPath, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;

        using var inputStream = File.OpenRead(inputPath);
        // 读取 IV
        var iv = new byte[16];
        inputStream.Read(iv, 0, iv.Length);
        aes.IV = iv;

        using var cryptoStream = new CryptoStream(inputStream,
            aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var outputStream = File.Create(outputPath);
        cryptoStream.CopyTo(outputStream);
    }

    public static byte[] DeriveKeyFromPassword(string password, byte[] salt)
    {
        using var deriveBytes = new Rfc2898DeriveBytes(
            password, salt, 100000, HashAlgorithmName.SHA256);
        return deriveBytes.GetBytes(32); // 256 位密钥
    }
}
```

### 42.4.2 字符串加密

```csharp
public static class StringEncryption
{
    public static string Encrypt(string plainText, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var writer = new StreamWriter(cs))
        {
            writer.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string cipherText, byte[] key)
    {
        var buffer = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = key;

        var iv = new byte[16];
        Array.Copy(buffer, 0, iv, 0, 16);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(buffer, 16, buffer.Length - 16);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);
        return reader.ReadToEnd();
    }
}
```

### 42.4.3 哈希和签名

```csharp
public static class HashHelper
{
    /// <summary>
    /// 计算文件 SHA256
    /// </summary>
    public static string ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// 密码哈希（使用 BCrypt）
    /// </summary>
    // NuGet: BCrypt.Net-Next
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

## 42.5 输入验证

### 42.5.1 防止路径遍历攻击

```csharp
public static class PathValidator
{
    public static bool IsSafePath(string basePath, string targetPath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(basePath, targetPath));
        return fullPath.StartsWith(Path.GetFullPath(basePath),
            StringComparison.OrdinalIgnoreCase);
    }

    public static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(c => !invalidChars.Contains(c))
            .ToArray());

        // 防止特殊文件名
        if (sanitized is ".." or "." or "")
            sanitized = "unnamed";

        return sanitized;
    }
}
```

### 42.5.2 防止 XSS（在 WebView 中）

```csharp
public static class HtmlSanitizer
{
    // NuGet: HtmlSanitizer
    public static string Sanitize(string html)
    {
        var sanitizer = new Ganss.XSS.HtmlSanitizer();
        sanitizer.AllowedTags.Add("b");
        sanitizer.AllowedTags.Add("i");
        sanitizer.AllowedTags.Add("p");
        sanitizer.AllowedTags.Add("br");
        sanitizer.AllowedAttributes.Add("class");

        return sanitizer.Sanitize(html);
    }
}
```

### 42.5.3 SQL 注入防护

```csharp
// 使用 EF Core 参数化查询（自动防注入）
public async Task<User?> FindUser(string username)
{
    // 安全：EF Core 自动参数化
    return await _context.Users
        .FirstOrDefaultAsync(u => u.Username == username);

    // 不安全：永远不要拼接 SQL
    // var sql = $"SELECT * FROM Users WHERE Username = '{username}'";
}
```

## 42.6 代码签名

### 42.6.1 Windows 代码签名

```csharp
// 使用 SignTool 签名
// signtool sign /f cert.pfx /p password /tr http://timestamp.digicert.com /fd sha256 MyApp.exe

// 在 CI/CD 中自动化
public static class CodeSigner
{
    public static async Task SignWindows(string exePath, string certPath, string password)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "signtool",
            Arguments = $"sign /f \"{certPath}\" /p {password} /tr http://timestamp.digicert.com /td sha256 /fd sha256 \"{exePath}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false
        });

        await process!.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new Exception("代码签名失败");
    }
}
```

### 42.6.2 macOS 签名和公证

```bash
#!/bin/bash
# sign-and-notarize.sh

APP_PATH="MyApp.app"
IDENTITY="Developer ID Application: Your Name (TEAMID)"
APPLE_ID="your@email.com"
TEAM_ID="TEAMID"

# 签名
codesign --force --options runtime --deep --sign "$IDENTITY" "$APP_PATH"

# 创建 DMG
hdiutil create -volname "MyApp" -srcfolder "$APP_PATH" -ov -format UDZO MyApp.dmg

# 签名 DMG
codesign --sign "$IDENTITY" MyApp.dmg

# 公证
xcrun notarytool submit MyApp.dmg \
    --apple-id "$APPLE_ID" \
    --team-id "$TEAM_ID" \
    --wait

# 装订
xcrun stapler staple MyApp.dmg
```

## 42.7 安全最佳实践

### 42.7.1 敏感数据处理

```csharp
public static class SecureDataHandling
{
    /// <summary>
    /// 安全清除内存中的敏感数据
    /// </summary>
    public static void SecureClear(byte[] data)
    {
        Array.Clear(data, 0, data.Length);
    }

    /// <summary>
    /// 安全清除字符串（使用 Span）
    /// </summary>
    public static void SecureClearString(Span<char> data)
    {
        data.Clear();
    }

    /// <summary>
    /// 使用 SecureString 存储密码
    /// </summary>
    public static SecureString ToSecureString(string input)
    {
        var secure = new SecureString();
        foreach (var c in input)
        {
            secure.AppendChar(c);
        }
        secure.MakeReadOnly();
        return secure;
    }
}
```

### 42.7.2 日志脱敏

```csharp
public static class LogSanitizer
{
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "token", "secret", "authorization", "cookie", "apikey"
    };

    public static string Sanitize(string logMessage)
    {
        // 脱敏 JSON 日志
        foreach (var field in SensitiveFields)
        {
            // 替换 "password": "xxx" 为 "password": "***"
            logMessage = Regex.Replace(logMessage,
                $"\"{field}\"\\s*:\\s*\"[^\"]*\"",
                $"\"{field}\": \"***\"",
                RegexOptions.IgnoreCase);
        }

        return logMessage;
    }
}
```

### 42.7.3 安全配置

```csharp
public class SecurityConfig
{
    // 从环境变量或配置文件读取，不要硬编码
    public string ApiKey => Environment.GetEnvironmentVariable("API_KEY") ?? "";

    public string ConnectionString => Environment.GetEnvironmentVariable("DB_CONNECTION") ?? "";

    // 使用 Azure Key Vault 或 AWS Secrets Manager
    public async Task<string> GetSecret(string secretName)
    {
        // 实际实现应使用密钥管理服务
        return Environment.GetEnvironmentVariable(secretName) ?? "";
    }
}
```

## 42.8 安全审计检查清单

```
[ ] 所有 API 请求使用 HTTPS
[ ] 密码使用 BCrypt/PBKDF2 哈希存储
[ ] 敏感数据使用 AES-256 加密
[ ] Token 使用安全存储（Windows Credential Manager / macOS Keychain）
[ ] 用户输入经过验证和转义
[ ] 文件路径检查防止路径遍历
[ ] 日志中不包含敏感信息
[ ] 代码已签名
[ ] 依赖项无已知漏洞（dotnet list package --vulnerable）
[ ] 使用 CSP 和 CORS 限制
```

## 42.9 Cross References

- **第 34 章**：本地数据存储（安全存储实现）
- **第 33 章**：WebView 集成（OAuth 登录流程）
- **第 35 章**：自动更新与分发（代码签名）
- **第 41 章**：网络通信（HTTPS 和 Token 管理）

## 42.10 Common Pitfalls

1. **硬编码密钥**：永远不要在代码中硬编码密钥、密码或 Token
2. **明文存储**：密码和 Token 必须加密存储
3. **忽略 HTTPS**：所有 API 请求必须使用 HTTPS
4. **日志泄露**：日志中不要包含敏感信息
5. **过时依赖**：定期检查和更新依赖项
6. **证书验证**：不要在生产环境禁用证书验证

## 42.11 Try It Yourself

1. 实现 OAuth2 登录流程，使用 IdentityModel.OidcClient
2. 创建 AES 文件加密/解密工具
3. 实现 JWT Token 自动刷新机制
4. 编写安全审计检查脚本
