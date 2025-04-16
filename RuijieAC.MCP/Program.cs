using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ModelContextProtocol.Server;

using RuijieAC.MCP;
using RuijieAC.MCP.Options;
using RuijieAC.MCP.Options.Validators;
using RuijieAC.MCP.Services;
using RuijieAC.MCP.Utils;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Add configuration
builder.Configuration.AddEnvironmentVariables();

// Add options
builder.Services.AddOptionsWithValidateOnStart<ControllerOptions, ControllerOptionsValidator>()
    .Bind(builder.Configuration.GetSection("Controller"));

// Add Mcp server service
builder.Services
    .AddMcpServer(options =>
    {
        options.InitializationTimeout = TimeSpan.FromSeconds(114514);
    })
    .WithStdioServerTransport()
    .WithToolsFromAssemblySourceGen(GeneratedJsonContext.Default.Options);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ControllerService>();

await builder.Build().RunAsync();
