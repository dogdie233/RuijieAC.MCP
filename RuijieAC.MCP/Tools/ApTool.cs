using System.ComponentModel;
using System.Runtime.InteropServices;

using Microsoft.Extensions.DependencyInjection;

using ModelContextProtocol.Server;

using RuijieAC.MCP.Services;

namespace RuijieAC.MCP.Tools;

[McpServerToolType]
public static class ApTool
{
    [McpServerTool(Name = "query_access_point")]
    [Description("Query and return No.[start, end] wireless access points that meet all query parameters, end have to greater or equals start. If a query parameter is null, it will be ignored.")]
    public static async Task<string> QueryAccessPoints(
        IServiceProvider sp,
        [Description("The ap's name, support fuzzy search")] string? name,
        [Description("The ap's mac address, support fuzzy search")] string? mac,
        [Description("The ap's ip address, support fuzzy search")] string? ip,
        [Description("The ap's location, support fuzzy search")] string? location,
        [Description("The ap's state, permitted value: [\"Run\", \"Quit\"]")] string? state,
        [Description("The starting index of the query range (1-based)")] int start = 1,
        [Description("The ending index of the query range (inclusive), suggest to use 20")] int end = 20,
        CancellationToken ct = default)
    {
        var form = new Dictionary<string, string>
        {
            { "Start", start.ToString() },
            { "End", end.ToString() },
            { "withRadioInfo", "false" },
        };
        if (name is not null) form.Add("query[name]", name);
        if (mac is not null) form.Add("query[mac]", mac);
        if (ip is not null) form.Add("query[ip]", ip);
        if (location is not null) form.Add("query[location]", location);
        if (state is not null) form.Add("query[state]", state);
        var controller = sp.GetRequiredService<ControllerService>();
        return await controller.PostAsync("/web/init.cgi/ac.dashboard.ap_list/getApList", form, ct: ct);;
    }
}