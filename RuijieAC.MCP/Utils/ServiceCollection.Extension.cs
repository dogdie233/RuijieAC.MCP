using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using RuijieAC.MCP.Options;

namespace RuijieAC.MCP.Utils;

internal static class ServiceCollectionExtension
{
    public static void AddHttpClient(this IServiceCollection service)
    {
        service.AddSingleton<CookieContainer>();
        service.AddKeyedSingleton<HttpClient>("RuijieHttpClient", (sp, key) =>
        {
            var controllerOptions = sp.GetRequiredService<IOptions<ControllerOptions>>().Value;
            var serverCert = CommonUtil.LoadCertificate(controllerOptions.CertFilePath);
            var handler = new SocketsHttpHandler
            {
                UseCookies = true,
                CookieContainer = sp.GetRequiredService<CookieContainer>(),
                SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                    {
                        return true;
                        if (errors == SslPolicyErrors.None) return true;
                        if (cert is X509Certificate2 x509Cert)
                            return x509Cert.Thumbprint == serverCert.Thumbprint || x509Cert.Verify();

                        return false;
                    },
                },
                PlaintextStreamFilter = (context, _) => new ValueTask<Stream>(context.PlaintextStream is SslStream sslStream
                    ? new StatusLineFixStream(sslStream)
                    : context.PlaintextStream), 
                ConnectTimeout = TimeSpan.FromSeconds(114514),
                PooledConnectionLifetime = TimeSpan.Zero,
            };
            
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(controllerOptions.Url),
                Timeout = TimeSpan.FromSeconds(114514),
                DefaultRequestVersion = HttpVersion.Version11,
                DefaultRequestHeaders =
                {
                    ConnectionClose = true,
                    UserAgent = { new ProductInfoHeaderValue("RuijieAC.MCP", "1.0") }
                },
            };
        });
    } 
}