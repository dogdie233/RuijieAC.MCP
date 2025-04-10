using System.Net;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using RuijieAC.MCP.Options;

namespace RuijieAC.MCP.Utils;

internal static class ServiceCollectionExtension
{
    public static void AddHttpClient(this IServiceCollection service)
    {
        service.AddSingleton<CookieContainer>();
        service.AddSingleton<HttpClient>(sp =>
        {
            var controllerOptions = sp.GetRequiredService<IOptions<ControllerOptions>>().Value;
            var serverCert = CommonUtil.LoadCertificate(controllerOptions.CertFilePath);
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    if (cert == null)
                        return false;
                        
                    return cert.Thumbprint.Equals(serverCert.Thumbprint, 
                        StringComparison.OrdinalIgnoreCase);
                },
                CookieContainer = sp.GetRequiredService<CookieContainer>()
            };
            
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(controllerOptions.Url),
                Timeout = TimeSpan.FromSeconds(30)
            };
        });
    } 
}