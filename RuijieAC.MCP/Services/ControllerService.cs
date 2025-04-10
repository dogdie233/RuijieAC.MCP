using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Options;

using RuijieAC.MCP.Options;
using RuijieAC.MCP.Utils;

namespace RuijieAC.MCP.Services;

public sealed class ControllerService(IOptions<ControllerOptions> controllerOptions, HttpClient client, CookieContainer cookieContainer) : IAsyncDisposable
{
    private bool _disposed;
    private const string HmacPath = "/hmac_info.do";
    private const string LoginPath = "/login.do";
    private const string LogoutPath = "/logout.do";

    public async Task<string> GetAsync(string path, bool noAuth = false)
    {
        EnsureNotDisposed();
        if (!noAuth) await EnsureLoginAsync();
        var response = await client.GetAsync(path);
        return await response.Content.ReadAsStringAsync();
    }

    public async ValueTask EnsureLoginAsync()
    {
        EnsureNotDisposed();
        var cookie = cookieContainer.GetCookies(client.BaseAddress!);
        if (cookie.Any(c => c.Name == "SIDS")) return;  // Already logged in

        var username = controllerOptions.Value.Username;
        var hmacRequestPath = HmacPath + $"?user={username}&_={Random.Shared.Next()}";
        var hmacInfo = await client.GetFromJsonAsync(hmacRequestPath, SourceGeneratedJsonContext.Default.HmacInfo);
        if (hmacInfo is null) throw new Exception("Login failed, hmacInfo is null");
        
        var password = CommonUtil.LoginHmac(hmacInfo, username, controllerOptions.Value.CertFilePath);
        var payload = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("user", username),
            new KeyValuePair<string, string>("key", password)
        ]);
        var response = await client.PostAsync(LoginPath, payload);
        response.EnsureSuccessStatusCode();
        
        var responseString = await response.Content.ReadAsStringAsync();
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
        var response = await client.GetAsync(requestPath);
        response.EnsureSuccessStatusCode();
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