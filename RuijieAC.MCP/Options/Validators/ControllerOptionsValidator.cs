using Microsoft.Extensions.Options;

using RuijieAC.MCP.Utils;

namespace RuijieAC.MCP.Options.Validators;

public class ControllerOptionsValidator : IValidateOptions<ControllerOptions>
{
    public ValidateOptionsResult Validate(string? name, ControllerOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Url))
        {
            return ValidateOptionsResult.Fail($"{nameof(ControllerOptions.Url)} is required.");
        }

        if (options.Url.EndsWith('/'))
        {
            options.Url = options.Url.TrimEnd('/');
        }

        if (string.IsNullOrWhiteSpace(options.Username))
        {
            return ValidateOptionsResult.Fail($"{nameof(ControllerOptions.Username)} is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            return ValidateOptionsResult.Fail($"{nameof(ControllerOptions.Password)} is required.");
        }

        if (string.IsNullOrEmpty(options.CertFilePath))
        {
            return ValidateOptionsResult.Fail($"{nameof(ControllerOptions.CertFilePath)} is required.");
        }

        try
        {
            CommonUtil.LoadCertificate(options.CertFilePath);
        }
        catch (Exception e)
        {
            return ValidateOptionsResult.Fail($"Failed to load certificate{Environment.NewLine}{e.GetType()}: {e.Message}\n{e.StackTrace}");
        }

        return ValidateOptionsResult.Success;
    }
}