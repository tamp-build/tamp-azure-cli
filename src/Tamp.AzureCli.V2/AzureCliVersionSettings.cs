namespace Tamp.AzureCli.V2;

/// <summary>Settings for <c>az version</c>.</summary>
public sealed class AzureCliVersionSettings : AzureCliSettingsBase
{
    protected override IEnumerable<string> BuildVerbArguments()
    {
        var args = new List<string> { "version" };
        EmitCommonArguments(args);
        return args;
    }
}

/// <summary>Settings for <c>az account show</c> — query the active subscription / tenant context.</summary>
public sealed class AzureCliAccountShowSettings : AzureCliSettingsBase
{
    protected override IEnumerable<string> BuildVerbArguments()
    {
        var args = new List<string> { "account", "show" };
        EmitCommonArguments(args);
        return args;
    }
}

/// <summary>Settings for <c>az account list</c>.</summary>
public sealed class AzureCliAccountListSettings : AzureCliSettingsBase
{
    /// <summary>Include all accounts (including disabled). Maps to <c>--all</c>.</summary>
    public bool All { get; set; }
    /// <summary>Refresh accounts by re-discovering. Maps to <c>--refresh</c>.</summary>
    public bool Refresh { get; set; }

    public AzureCliAccountListSettings SetAll(bool v = true) { All = v; return this; }
    public AzureCliAccountListSettings SetRefresh(bool v = true) { Refresh = v; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        var args = new List<string> { "account", "list" };
        EmitCommonArguments(args);
        if (All) args.Add("--all");
        if (Refresh) args.Add("--refresh");
        return args;
    }
}

/// <summary>Settings for <c>az account set</c> — switch the active subscription.</summary>
public sealed class AzureCliAccountSetSettings : AzureCliSettingsBase
{
    public string? SubscriptionId { get; set; }

    public AzureCliAccountSetSettings SetSubscriptionId(string id) { SubscriptionId = id; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(SubscriptionId))
            throw new InvalidOperationException("az account set: SubscriptionId is required.");
        var args = new List<string> { "account", "set" };
        EmitCommonArguments(args);
        args.Add("--subscription"); args.Add(SubscriptionId!);
        return args;
    }
}
