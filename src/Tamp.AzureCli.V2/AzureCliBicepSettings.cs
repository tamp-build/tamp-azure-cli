namespace Tamp.AzureCli.V2;

/// <summary>
/// Settings for <c>az bicep build</c> — compile a Bicep template to
/// ARM JSON. The dedicated <c>Tamp.Bicep</c> package wraps the
/// standalone Bicep CLI; this lives here for the in-process az path
/// since `az bicep` ships the bundled Bicep binary.
/// </summary>
public sealed class AzureCliBicepBuildSettings : AzureCliSettingsBase
{
    /// <summary>Path to the Bicep file. Required. Maps to <c>--file</c> / <c>-f</c>.</summary>
    public string? File { get; set; }

    /// <summary>Output directory. Maps to <c>--outdir</c>.</summary>
    public string? OutDir { get; set; }

    /// <summary>Output file path. Maps to <c>--outfile</c>.</summary>
    public string? OutFile { get; set; }

    /// <summary>Print to stdout instead of writing files. Maps to <c>--stdout</c>.</summary>
    public bool Stdout { get; set; }

    /// <summary>Build without restoring external modules. Maps to <c>--no-restore</c>.</summary>
    public bool NoRestore { get; set; }

    public AzureCliBicepBuildSettings SetFile(string path) { File = path; return this; }
    public AzureCliBicepBuildSettings SetOutDir(string path) { OutDir = path; return this; }
    public AzureCliBicepBuildSettings SetOutFile(string path) { OutFile = path; return this; }
    public AzureCliBicepBuildSettings SetStdout(bool v = true) { Stdout = v; return this; }
    public AzureCliBicepBuildSettings SetNoRestore(bool v = true) { NoRestore = v; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(File))
            throw new InvalidOperationException("az bicep build: File is required.");
        var args = new List<string> { "bicep", "build" };
        EmitCommonArguments(args);
        args.Add("--file"); args.Add(File!);
        if (!string.IsNullOrEmpty(OutDir)) { args.Add("--outdir"); args.Add(OutDir!); }
        if (!string.IsNullOrEmpty(OutFile)) { args.Add("--outfile"); args.Add(OutFile!); }
        if (Stdout) args.Add("--stdout");
        if (NoRestore) args.Add("--no-restore");
        return args;
    }
}

/// <summary>Settings for <c>az bicep install</c> — install or refresh the bundled Bicep binary.</summary>
public sealed class AzureCliBicepInstallSettings : AzureCliSettingsBase
{
    /// <summary>Specific version (e.g. <c>v0.40.31</c>). Default: latest. Maps to <c>--version</c>.</summary>
    public string? Version { get; set; }
    /// <summary>Target architecture override. Maps to <c>--target-platform</c>. Values: <c>win-x64</c>, <c>win-arm64</c>, <c>linux-x64</c>, <c>linux-arm64</c>, <c>linux-musl-x64</c>, <c>osx-x64</c>, <c>osx-arm64</c>.</summary>
    public string? TargetPlatform { get; set; }

    public AzureCliBicepInstallSettings SetVersion(string version) { Version = version; return this; }
    public AzureCliBicepInstallSettings SetTargetPlatform(string platform) { TargetPlatform = platform; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        var args = new List<string> { "bicep", "install" };
        EmitCommonArguments(args);
        if (!string.IsNullOrEmpty(Version)) { args.Add("--version"); args.Add(Version!); }
        if (!string.IsNullOrEmpty(TargetPlatform)) { args.Add("--target-platform"); args.Add(TargetPlatform!); }
        return args;
    }
}

/// <summary>Settings for <c>az bicep version</c>.</summary>
public sealed class AzureCliBicepVersionSettings : AzureCliSettingsBase
{
    protected override IEnumerable<string> BuildVerbArguments()
    {
        var args = new List<string> { "bicep", "version" };
        EmitCommonArguments(args);
        return args;
    }
}
