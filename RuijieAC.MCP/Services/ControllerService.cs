using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RuijieAC.MCP.Options;
using RuijieAC.MCP.Utils;

namespace RuijieAC.MCP.Services;

public sealed class ControllerService(IOptions<ControllerOptions> controllerOptions, ILogger<ControllerService> logger,
    HttpClient client, CookieContainer cookieContainer) : IAsyncDisposable
{
    private bool _disposed;
    private const string HmacPath = "/hmac_info.do";
    private const string LoginPath = "/login.do";
    private const string LogoutPath = "/logout.do";

    public async Task<string> GetAsync(string path, bool noAuth = false, CancellationToken ct = default)
    {
        EnsureNotDisposed();
        if (!noAuth) await EnsureLoginAsync(ct);
        logger.LogInformation("Sending {Method} request to {Host} {Path}", "GET", client.BaseAddress, path);
        var response = await client.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> PostAsync(string path, Dictionary<string, string> form, bool noAuth = false, CancellationToken ct = default)
    {
        EnsureNotDisposed();
        if (!noAuth) await EnsureLoginAsync(ct);
        var payload = new FormUrlEncodedContent(form);
        var payloadLog = JsonSerializer.Serialize(form, SourceGeneratedJsonContext.Default.DicKeyStringValueString);
        logger.LogInformation("Sending {Method} request to {Host} {Path} with form payload: {payload}", "POST", client.BaseAddress, path, payloadLog);
        var response = await client.PostAsync(path, payload, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async ValueTask EnsureLoginAsync(CancellationToken ct)
    {
        EnsureNotDisposed();
        var cookie = cookieContainer.GetCookies(client.BaseAddress!);
        if (cookie.Any(c => c.Name == "SIDS")) return;  // Already logged in

        var username = controllerOptions.Value.Username;
        var hmacRequestPath = HmacPath + $"?user={username}&_={Random.Shared.Next()}";
        var hmacInfoString = await GetAsync(hmacRequestPath, true, ct);
        var hmacInfo = JsonSerializer.Deserialize(hmacInfoString, SourceGeneratedJsonContext.Default.HmacInfo);
        if (hmacInfo is null) throw new Exception("Login failed, hmacInfo is null");
        
        var password = CommonUtil.LoginHmac(hmacInfo, username, controllerOptions.Value.CertFilePath);
        var payload = new Dictionary<string, string>
        {
            { "user", username },
            { "key", password },
        };
        var responseString = await PostAsync(LoginPath, payload, true, ct);
        var code = CommonUtil.ParseReturnCode(responseString);
        code ??= -1;
        if (code != 0) throw new LoginException(code.Value);
    }

    public async ValueTask LogoutAsync()
    {
        EnsureNotDisposed();
        var cookie = cookieContainer.GetCookies(client.BaseAddress!);
        if (cookie.All(c => c.Name != "SIDS")) return;  // Already logged out
        
        var requestPath = LogoutPath + $"?_={Random.Shared.Next()}";
        await GetAsync(requestPath, true);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await LogoutAsync();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureNotDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ControllerService));
    }
}