namespace Tamp.AzureCli.V2;

/// <summary>
/// Escape hatch for the long tail of az verbs. The az CLI has
/// hundreds of subgroups (acr, aks, sql, eventhubs, keyvault, …);
/// v0.1.0 types only the load-bearing ones. Everything else goes
/// through here.
/// </summary>
public sealed class AzureCliRawSettings : AzureCliSettingsBase
{
    public List<string> RawArguments { get; } = [];

    public AzureCliRawSettings AddArgs(params string[] args) { RawArguments.AddRange(args); return this; }

    protected override IEnumerable<string> BuildVerbArguments() => RawArguments;
}
