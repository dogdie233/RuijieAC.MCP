using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuijieAC.MCP;

[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(KeyValuePair<string, string>))]
[JsonSerializable(typeof(KeyValuePair<string, string>[]))]
internal partial class GeneratedJsonContext : JsonSerializerContext 
{
}