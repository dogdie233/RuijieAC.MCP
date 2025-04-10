namespace RuijieAC.MCP.Options;

public class ControllerOptions
{
    public string Url { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string CertFilePath { get; set; } = "cert.crt";
}