using System.Runtime.InteropServices;
using Azure.Sdk.Tools.Cli.Models;
using Azure.Sdk.Tools.Cli.Helpers;
using Microsoft.Extensions.Logging;
using System.Security.Policy;

namespace Azure.Sdk.Tools.Cli.Services;

/// <summary>
/// .NET-specific implementation of language repository service.
/// Uses tools like dotnet CLI, MSBuild, NuGet, etc. for .NET development workflows.
/// </summary>
public class DotNetLanguageSpecificChecks : ILanguageSpecificChecks
{
    private readonly string dotnetCommand = "dotnet.exe";
    private readonly string dotnetCommandWindows = "dotnet";
    private readonly string codeChecksScript = "eng/code-checks.ps1";
    private readonly string aotCompatCheckScript = "eng/aot-compat-check.ps1";
    private readonly IProcessHelper _processHelper;
    private readonly INpxHelper _npxHelper;
    private readonly IGitHelper _gitHelper;
    private readonly ILogger<DotNetLanguageSpecificChecks> _logger;

    public DotNetLanguageSpecificChecks(
        IProcessHelper processHelper,
        INpxHelper npxHelper,
        IGitHelper gitHelper,
        ILogger<DotNetLanguageSpecificChecks> logger)
    {
        _processHelper = processHelper;
        _npxHelper = npxHelper;
        _gitHelper = gitHelper;
        _logger = logger;
    }

    public string SupportedLanguage => "dotnet";

    public async Task<CLICheckResponse> UpdateSnippetsAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var repoRoot = _gitHelper.DiscoverRepoRoot(packagePath);

            var scriptPath = Path.Combine(repoRoot, "eng", "scripts", "Update-Snippets.ps1");
            if (!File.Exists(scriptPath))
            {
                _logger.LogError("Python snippet updater script not found at: {ScriptPath}", scriptPath);
                return new CLICheckResponse(1, "", $"Python snippet updater script not found at: {scriptPath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{MethodName} failed with an exception", nameof(UpdateSnippetsAsync));
            return new CLICheckResponse(1, "", $"{nameof(UpdateSnippetsAsync)} failed with an exception: {ex.Message}");
        }
    }

    public async Task<CLICheckResponse> ExportAPIAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{MethodName} failed with an exception", nameof(ExportAPIAsync));
            return new CLICheckResponse(1, "", $"{nameof(ExportAPIAsync)} failed with an exception: {ex.Message}");
        }
    }

    public async Task<CLICheckResponse> RunGeneratedCodeValidationAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{MethodName} failed with an exception", nameof(RunGeneratedCodeValidationAsync));
            return new CLICheckResponse(1, "", $"{nameof(RunGeneratedCodeValidationAsync)} failed with an exception: {ex.Message}");
        }
    }

    public async Task<CLICheckResponse> CheckAotCompatibilityValidationAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{MethodName} failed with an exception", nameof(CheckAotCompatibilityValidationAsync));
            return new CLICheckResponse(1, "", $"{nameof(CheckAotCompatibilityValidationAsync)} failed with an exception: {ex.Message}");
        }
    }

    public async Task<CLICheckResponse> BuildCodeAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _processHelper.Run(new ProcessOptions(dotnetCommand, ["build"], dotnetCommandWindows, ["build"], workingDirectory: packagePath), ct);
            return new CLICheckResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{MethodName} failed with an exception", nameof(BuildCodeAsync));
            return new CLICheckResponse(1, "", $"{nameof(BuildCodeAsync)} failed with an exception: {ex.Message}");
        }
    }

    public async Task<CLICheckResponse> AnalyzeDependenciesAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new CLICheckResponse());
    }

    private string GetServiceDirectoryFromPath(string packagePath)
    {
        string serviceDirectory = null;
        var normalizedPath = packagePath.Replace('\\', '/');
        var sdkIndex = normalizedPath.IndexOf("/sdk/", StringComparison.OrdinalIgnoreCase);

        if (sdkIndex >= 0)
        {
            var pathAfterSdk = normalizedPath.Substring(sdkIndex + 5); // Skip "/sdk/"
            var segments = pathAfterSdk.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
            {
                serviceDirectory = segments[0];
            }
        }
        return serviceDirectory;
    }
}
