using System.Runtime.InteropServices;
using Azure.Sdk.Tools.Cli.Models;
using Azure.Sdk.Tools.Cli.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Education.Classes.Item.Assignments.Item.Submissions.Item.Return;

namespace Azure.Sdk.Tools.Cli.Services;

/// <summary>
/// .NET-specific implementation of language repository service.
/// Uses tools like dotnet CLI, MSBuild, NuGet, etc. for .NET development workflows.
/// </summary>
public class DotNetLanguageSpecificChecks : ILanguageSpecificChecks
{
    private readonly IProcessHelper _processHelper;
    private readonly INpxHelper _npxHelper;
    private readonly IGitHelper _gitHelper;
    private readonly ILogger<DotNetLanguageSpecificChecks> _logger;
    private readonly string dotnetCommand = "dotnet.exe";
    private readonly string dotnetCommandWindows = "dotnet";
    private readonly string powerShellCommand = "pwsh";

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

    #region CI pipeline checks

    public async Task<CLICheckResponse> RunBuildCodeAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _processHelper.Run(new ProcessOptions(dotnetCommand, ["build"], dotnetCommandWindows, ["build"], workingDirectory: solutionPath), cancellationToken);
            return new CLICheckResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{MethodName} failed with an exception", nameof(RunBuildCodeAsync));
            return new CLICheckResponse(1, "", $"{nameof(RunBuildCodeAsync)} failed with an exception: {ex.Message}");
        }
    }

    public async Task<CLICheckResponse> RunCheckAotCompatibilityValidationAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var serviceDirectory = GetServiceDirectoryFromPath(packagePath);
            var packageName = GetPackageNameFromPath(packagePath);
            if (serviceDirectory == null || packageName == null)
            {
                return new CLICheckResponse(1, "", "Failed to determine service directory or package name from the provided package path.");
            }
            var repoRoot = _gitHelper.DiscoverRepoRoot(packagePath);
            var scriptPath = Path.Combine(repoRoot, "eng", "scripts", "compatibility", "Check-AOT-Compatibility.ps1");
            var args = new[] { scriptPath, "-ServiceDirectory", serviceDirectory, packageName };
            var timeout = TimeSpan.FromMinutes(6);
            var result = await _processHelper.Run(new(powerShellCommand, args, timeout: timeout), cancellationToken);

            return result.ExitCode switch
            {
                0 => new CLICheckResponse(result.ExitCode, result.Output),
                _ => new CLICheckResponse(result.ExitCode, result.Output, "Process failed"),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{MethodName} failed with an exception", nameof(RunCheckAotCompatibilityValidationAsync));
            return new CLICheckResponse(1, "", $"{nameof(RunCheckAotCompatibilityValidationAsync)} failed with an exception: {ex.Message}");
        }
    }

    public async Task<CLICheckResponse> RunGeneratedCodeValidationAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var serviceDirectory = GetServiceDirectoryFromPath(packagePath);
            if (serviceDirectory == null)
            {
                return new CLICheckResponse(1, "", "Failed to determine service directory from the provided package path.");
            }
            var repoRoot = _gitHelper.DiscoverRepoRoot(packagePath);
            var scriptPath = Path.Combine(repoRoot, "eng", "scripts", "CodeChecks.ps1");
            var args = new[] { scriptPath, "-ServiceDirectory", serviceDirectory };
            var timeout = TimeSpan.FromMinutes(6);
            var result = await _processHelper.Run(new(powerShellCommand, args, timeout: timeout), cancellationToken);

            return result.ExitCode switch
            {
                0 => new CLICheckResponse(result.ExitCode, result.Output),
                _ => new CLICheckResponse(result.ExitCode, result.Output, "Process failed"),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{MethodName} failed with an exception", nameof(RunGeneratedCodeValidationAsync));
            return new CLICheckResponse(1, "", $"{nameof(RunGeneratedCodeValidationAsync)} failed with an exception: {ex.Message}");
        }
    }

    #endregion

    #region Scripts to run for CIs

    public async Task<CLICheckResponse> UpdateSnippetsAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var serviceDirectory = GetServiceDirectoryFromPath(packagePath);
            if (serviceDirectory == null)
            {
                return new CLICheckResponse(1, "", "Failed to determine service directory from the provided package path.");
            }
            var repoRoot = _gitHelper.DiscoverRepoRoot(packagePath);
            var scriptPath = Path.Combine(repoRoot, "eng", "scripts", "Update-Snippets.ps1");
            var args = new[] { scriptPath, serviceDirectory };
            var timeout = TimeSpan.FromMinutes(2);
            var result = await _processHelper.Run(new(powerShellCommand, args, timeout: timeout), cancellationToken);

            return result.ExitCode switch
            {
                0 => new CLICheckResponse(result.ExitCode, result.Output),
                _ => new CLICheckResponse(result.ExitCode, result.Output, "Process failed"),
            };
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
            var serviceDirectory = GetServiceDirectoryFromPath(packagePath);
            if (serviceDirectory == null)
            {
                return new CLICheckResponse(1, "", "Failed to determine service directory from the provided package path.");
            }
            var repoRoot = _gitHelper.DiscoverRepoRoot(packagePath);
            var scriptPath = Path.Combine(repoRoot, "eng", "scripts", "Export-API.ps1");
            var args = new[] { scriptPath, serviceDirectory };
            var timeout = TimeSpan.FromMinutes(5);
            var result = await _processHelper.Run(new(powerShellCommand, args, timeout: timeout), cancellationToken);

            return result.ExitCode switch
            {
                0 => new CLICheckResponse(result.ExitCode, result.Output),
                _ => new CLICheckResponse(result.ExitCode, result.Output, "Process failed"),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{MethodName} failed with an exception", nameof(ExportAPIAsync));
            return new CLICheckResponse(1, "", $"{nameof(ExportAPIAsync)} failed with an exception: {ex.Message}");
        }
    }

    #endregion

    #region Helpers

    private string? GetServiceDirectoryFromPath(string packagePath)
    {
        string? serviceDirectory = null;
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

    private string? GetPackageNameFromPath(string packagePath)
    {
        string? packageName = null;
        var normalizedPath = packagePath.Replace('\\', '/');
        var sdkIndex = normalizedPath.IndexOf("/sdk/", StringComparison.OrdinalIgnoreCase);

        if (sdkIndex >= 0)
        {
            var pathAfterSdk = normalizedPath.Substring(sdkIndex + 5); // Skip "/sdk/"
            var segments = pathAfterSdk.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 1)
            {
                packageName = segments[1];
            }
        }
        return packageName;
    }

    #endregion
}
