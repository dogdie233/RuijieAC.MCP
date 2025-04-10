using System.Text.Json.Serialization;

using RuijieAC.MCP.Models;

namespace RuijieAC.MCP.Utils;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(HmacInfo))]
[JsonSerializable(typeof(Dictionary<string, string>), TypeInfoPropertyName = "DicKeyStringValueString")]
internal partial class SourceGeneratedJsonContext : JsonSerializerContext
{
}