using System.Text.Json.Serialization;

using RuijieAC.MCP.Utils.JsonConverters;

namespace RuijieAC.MCP.Models;

public class HmacInfo
{
    [JsonPropertyName("salt")] public string Salt { get; set; } = string.Empty;
    [JsonPropertyName("iter")] public int Iter { get; set; }
    [JsonPropertyName("digest")] public string Digest { get; set; } = string.Empty;
    [JsonPropertyName("keylen")] public int KeyLen { get; set; }
    [JsonPropertyName("aaa_enable")][JsonConverter(typeof(BooleanNumberConverter))] public bool AaaEnable { get; set; }
}