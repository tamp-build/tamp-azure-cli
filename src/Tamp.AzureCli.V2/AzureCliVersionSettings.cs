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

/// <summary>
/// Settings for <c>az account get-access-token</c> — acquire a bearer token for any ARM /
/// Azure-resource audience the signed-in principal has access to. Common audiences:
/// <list type="bullet">
///   <item><c>https://management.azure.com</c> — ARM REST API (default)</item>
///   <item><c>https://vault.azure.net</c> — Key Vault data-plane</item>
///   <item><c>https://&lt;org&gt;.crm.dynamics.com</c> — Dataverse Web API</item>
///   <item><c>https://graph.microsoft.com</c> — Microsoft Graph</item>
/// </list>
/// </summary>
/// <remarks>
/// <para>
/// The CLI emits the token to stdout (JSON or scalar depending on <see cref="AzureCliSettingsBase.OutputFormat"/>);
/// callers route the captured stdout to <see cref="ProcessRunner.Capture"/> rather than direct
/// execution and parse the result. The CLI does NOT redact the token automatically, so consumers
/// must wrap it in a <see cref="Secret"/> at the boundary — see the helper pattern in the README.
/// </para>
/// <para>
/// The <see cref="Tenant"/> override is useful on multi-tenant developer machines where the
/// signed-in CLI principal has access to multiple directories; explicit tenant selection avoids
/// the wrong-tenant token issue Git Credential Manager hits in the same scenario.
/// </para>
/// </remarks>
public sealed class AzureCliAccountGetAccessTokenSettings : AzureCliSettingsBase
{
    /// <summary>Audience / resource URL. Default <c>https://management.azure.com</c> (ARM).</summary>
    public string? Resource { get; set; }

    /// <summary>Tenant id or domain (<c>--tenant</c>). Optional; defaults to the signed-in tenant.</summary>
    public string? Tenant { get; set; }

    /// <summary>Scope (<c>--scope</c>). Optional; some token types require it instead of <see cref="Resource"/>.</summary>
    public string? Scope { get; set; }

    // Subscription is inherited from AzureCliSettingsBase and emitted via EmitCommonArguments.

    public AzureCliAccountGetAccessTokenSettings SetResource(string resource) { Resource = resource; return this; }
    public AzureCliAccountGetAccessTokenSettings SetTenant(string? tenant) { Tenant = tenant; return this; }
    public AzureCliAccountGetAccessTokenSettings SetScope(string? scope) { Scope = scope; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        var args = new List<string> { "account", "get-access-token" };
        EmitCommonArguments(args);
        if (!string.IsNullOrEmpty(Resource)) { args.Add("--resource"); args.Add(Resource!); }
        if (!string.IsNullOrEmpty(Tenant)) { args.Add("--tenant"); args.Add(Tenant!); }
        if (!string.IsNullOrEmpty(Scope)) { args.Add("--scope"); args.Add(Scope!); }
        return args;
    }
}
