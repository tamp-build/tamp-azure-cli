using System.IO;
using Tamp;
using Xunit;
using Xunit.Abstractions;

namespace Tamp.AzureCli.V2.IntegrationTests;

/// <summary>
/// Exercises the wrapper against a real <c>az</c> CLI. We avoid any
/// verb that requires an authenticated Azure subscription:
/// <c>version</c>, <c>--help</c> shape probes for the typed verbs,
/// <c>bicep version</c> (no auth needed), and the post-Prepare flag
/// validation for <c>az rest</c>.
/// </summary>
public sealed class AzureCliIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public AzureCliIntegrationTests(ITestOutputHelper output) => _output = output;

    private static string? ResolveOnPath(string baseName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var names = OperatingSystem.IsWindows()
            ? new[] { $"{baseName}.cmd", $"{baseName}.exe", $"{baseName}.bat", baseName }
            : new[] { baseName };
        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            if (string.IsNullOrEmpty(dir)) continue;
            foreach (var n in names)
            {
                var c = Path.Combine(dir, n);
                if (File.Exists(c)) return c;
            }
        }
        return null;
    }

    private static Tool ResolveTool() =>
        new(AbsolutePath.Create(ResolveOnPath("az")
            ?? throw new InvalidOperationException("az not found on PATH. Install: https://learn.microsoft.com/cli/azure/install-azure-cli")));

    private CaptureResult Run(CommandPlan plan)
    {
        _output.WriteLine($"$ {plan.Executable} {string.Join(' ', plan.Arguments)}");
        var result = ProcessRunner.Capture(plan);
        foreach (var line in result.Lines)
            _output.WriteLine($"  [{line.Type}] {line.Text}");
        _output.WriteLine($"  → exit {result.ExitCode}");
        return result;
    }

    [Fact]
    public void Version_Json_Reports_2_x()
    {
        var tool = ResolveTool();
        var plan = AzureCli.Version(tool, s => s.SetOutput("json"));
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        using var doc = System.Text.Json.JsonDocument.Parse(result.StdoutText);
        Assert.True(doc.RootElement.TryGetProperty("azure-cli", out var ver));
        Assert.Matches(@"^2\.\d+\.\d+", ver.GetString());
    }

    [Fact]
    public void Raw_Help_Reports_Available_Subgroups()
    {
        var tool = ResolveTool();
        var plan = AzureCli.Raw(tool, "--help");
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        var combined = result.StdoutText + result.StderrText;
        // every az install ships these subgroups
        foreach (var sub in new[] { "login", "logout", "rest", "group", "account", "bicep" })
        {
            Assert.Contains(sub, combined);
        }
    }

    [Fact]
    public void Raw_Login_Help_Surfaces_Expected_Flags()
    {
        var tool = ResolveTool();
        var plan = AzureCli.Raw(tool, "login", "--help");
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        var combined = result.StdoutText + result.StderrText;
        foreach (var flag in new[] { "--service-principal", "--federated-token", "--identity", "--use-device-code", "--tenant", "--certificate" })
        {
            Assert.Contains(flag, combined);
        }
    }

    [Fact]
    public void Raw_Rest_Help_Surfaces_Expected_Flags()
    {
        var tool = ResolveTool();
        var plan = AzureCli.Raw(tool, "rest", "--help");
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        var combined = result.StdoutText + result.StderrText;
        foreach (var flag in new[] { "--uri", "--method", "--body", "--headers", "--uri-parameters", "--resource", "--output-file", "--skip-authorization-header" })
        {
            Assert.Contains(flag, combined);
        }
    }

    [Fact]
    public void Raw_Group_Show_Help_Surfaces_Name_Flag()
    {
        var tool = ResolveTool();
        var plan = AzureCli.Raw(tool, "group", "show", "--help");
        var result = Run(plan);
        Assert.Equal(0, result.ExitCode);
        var combined = result.StdoutText + result.StderrText;
        Assert.Contains("--name", combined);
        Assert.Contains("--resource-group", combined);
    }

    [Fact]
    public void Bicep_Build_From_Real_Source()
    {
        // Stage a trivial Bicep file in a temp dir and compile it.
        // No auth required — bicep build runs entirely locally.
        var workdir = Path.Combine(Path.GetTempPath(), $"tamp-azcli-it-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workdir);
        try
        {
            var bicepFile = Path.Combine(workdir, "trivial.bicep");
            File.WriteAllText(bicepFile, "param location string = resourceGroup().location\noutput loc string = location\n");

            var tool = ResolveTool();
            var plan = AzureCli.Bicep.Build(tool, s => s
                .SetFile(bicepFile)
                .SetOutFile(Path.Combine(workdir, "trivial.json")));
            var result = Run(plan);
            Assert.Equal(0, result.ExitCode);

            var armPath = Path.Combine(workdir, "trivial.json");
            Assert.True(File.Exists(armPath), $"Expected ARM output at {armPath}");
            var arm = File.ReadAllText(armPath);
            Assert.Contains("\"$schema\"", arm);
            Assert.Contains("\"outputs\"", arm);
            Assert.Contains("\"loc\"", arm);
        }
        finally
        {
            try { Directory.Delete(workdir, recursive: true); } catch { }
        }
    }
}
