using System.ComponentModel;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

using ModelContextProtocol.Server;

using RuijieAC.MCP.Services;

namespace RuijieAC.MCP.Tools;

[McpServerToolType]
public static class ApTool
{
    [McpServerTool(Name = "query_access_point")]
    [Description("Query and return No.[start, end] wireless access points that meet all query parameters, end have to greater or equals start.")]
    public static async Task<string> QueryAccessPoints(
        IServiceProvider sp,
        [Description("An array of key-value pairs representing query conditions (AND logic).\nKeys: \"name\", \"mac\", \"ip\", \"location\", or \"state\".\nValues: Any string, except for key \"state\" where the value must be \"Quit\" or \"Run\".")] KeyValuePair<string, string>[] query,
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
        foreach (var kv in query)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (kv.Value is null) continue;
            form.Add($"query[{kv.Key}]", kv.Value);
        }
        var controller = sp.GetRequiredService<ControllerService>();
        var result = await controller.PostAsync("/web/init.cgi/ac.dashboard.ap_list/getApList", form, ct: ct);
        return result;
    }
}