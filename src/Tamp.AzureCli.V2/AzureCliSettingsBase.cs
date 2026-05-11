namespace Tamp.AzureCli.V2;

/// <summary>
/// Common base for <c>az</c> verb settings. The az CLI exposes a
/// uniform vocabulary of global args across every subcommand:
/// <c>--output</c>, <c>--query</c>, <c>--subscription</c>,
/// <c>--verbose</c>, <c>--debug</c>, <c>--only-show-errors</c>.
/// Concrete classes layer verb-specific args on top via
/// <see cref="BuildVerbArguments"/>.
/// </summary>
public abstract class AzureCliSettingsBase
{
    public string? WorkingDirectory { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; } = new();

    /// <summary>Output format. Maps to <c>--output</c> / <c>-o</c>. Allowed: <c>json</c>, <c>jsonc</c>, <c>none</c>, <c>table</c>, <c>tsv</c>, <c>yaml</c>, <c>yamlc</c>.</summary>
    public string? Output { get; set; }

    /// <summary>JMESPath query. Maps to <c>--query</c>.</summary>
    public string? Query { get; set; }

    /// <summary>Override subscription. Maps to <c>--subscription</c>. Name or GUID.</summary>
    public string? Subscription { get; set; }

    /// <summary>Increase logging verbosity. Maps to <c>--verbose</c>.</summary>
    public bool Verbose { get; set; }

    /// <summary>Full debug logs. Maps to <c>--debug</c>.</summary>
    public bool Debug { get; set; }

    /// <summary>Suppress warnings; only show errors. Maps to <c>--only-show-errors</c>.</summary>
    public bool OnlyShowErrors { get; set; }

    /// <summary>Disable TLS connection verification — for Zscaler / proxy environments. Sets <c>AZURE_CLI_DISABLE_CONNECTION_VERIFICATION=1</c> in the process env.</summary>
    public bool DisableConnectionVerification { get; set; }

    protected abstract IEnumerable<string> BuildVerbArguments();

    protected virtual string? BuildStandardInput() => null;

    protected virtual IReadOnlyList<Secret> CollectSecrets() => Array.Empty<Secret>();

    protected void EmitCommonArguments(List<string> args)
    {
        if (!string.IsNullOrEmpty(Output)) { args.Add("--output"); args.Add(Output!); }
        if (!string.IsNullOrEmpty(Query)) { args.Add("--query"); args.Add(Query!); }
        if (!string.IsNullOrEmpty(Subscription)) { args.Add("--subscription"); args.Add(Subscription!); }
        if (Verbose) args.Add("--verbose");
        if (Debug) args.Add("--debug");
        if (OnlyShowErrors) args.Add("--only-show-errors");
    }

    public CommandPlan ToCommandPlan(Tool tool)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        var args = BuildVerbArguments().ToList();
        var env = new Dictionary<string, string>(EnvironmentVariables);
        if (DisableConnectionVerification)
            env["AZURE_CLI_DISABLE_CONNECTION_VERIFICATION"] = "1";
        return new CommandPlan
        {
            Executable = tool.Executable.Value,
            Arguments = args,
            Environment = env,
            WorkingDirectory = WorkingDirectory,
            StandardInput = BuildStandardInput(),
            Secrets = CollectSecrets(),
        };
    }
}

/// <summary>Generic fluent setters for the shared base.</summary>
public static class AzureCliSettingsBaseExtensions
{
    public static T SetWorkingDirectory<T>(this T s, string? cwd) where T : AzureCliSettingsBase { s.WorkingDirectory = cwd; return s; }
    public static T SetEnv<T>(this T s, string key, string value) where T : AzureCliSettingsBase { s.EnvironmentVariables[key] = value; return s; }
    public static T SetOutput<T>(this T s, string format) where T : AzureCliSettingsBase { s.Output = format; return s; }
    public static T SetQuery<T>(this T s, string jmesPath) where T : AzureCliSettingsBase { s.Query = jmesPath; return s; }
    public static T SetSubscription<T>(this T s, string nameOrId) where T : AzureCliSettingsBase { s.Subscription = nameOrId; return s; }
    public static T SetVerbose<T>(this T s, bool v = true) where T : AzureCliSettingsBase { s.Verbose = v; return s; }
    public static T SetDebug<T>(this T s, bool v = true) where T : AzureCliSettingsBase { s.Debug = v; return s; }
    public static T SetOnlyShowErrors<T>(this T s, bool v = true) where T : AzureCliSettingsBase { s.OnlyShowErrors = v; return s; }
    public static T SetDisableConnectionVerification<T>(this T s, bool v = true) where T : AzureCliSettingsBase { s.DisableConnectionVerification = v; return s; }
}
