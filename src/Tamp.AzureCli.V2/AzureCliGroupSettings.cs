namespace Tamp.AzureCli.V2;

/// <summary>Settings for <c>az group show</c>.</summary>
public sealed class AzureCliGroupShowSettings : AzureCliSettingsBase
{
    public string? Name { get; set; }

    public AzureCliGroupShowSettings SetName(string name) { Name = name; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidOperationException("az group show: Name is required.");
        var args = new List<string> { "group", "show" };
        EmitCommonArguments(args);
        args.Add("--name"); args.Add(Name!);
        return args;
    }
}

/// <summary>Settings for <c>az group exists</c>.</summary>
public sealed class AzureCliGroupExistsSettings : AzureCliSettingsBase
{
    public string? Name { get; set; }

    public AzureCliGroupExistsSettings SetName(string name) { Name = name; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidOperationException("az group exists: Name is required.");
        var args = new List<string> { "group", "exists" };
        EmitCommonArguments(args);
        args.Add("--name"); args.Add(Name!);
        return args;
    }
}

/// <summary>Settings for <c>az group create</c>.</summary>
public sealed class AzureCliGroupCreateSettings : AzureCliSettingsBase
{
    public string? Name { get; set; }
    public string? Location { get; set; }
    public Dictionary<string, string> Tags { get; } = new();
    public string? ManagedBy { get; set; }

    public AzureCliGroupCreateSettings SetName(string name) { Name = name; return this; }
    public AzureCliGroupCreateSettings SetLocation(string location) { Location = location; return this; }
    public AzureCliGroupCreateSettings SetTag(string key, string value) { Tags[key] = value; return this; }
    public AzureCliGroupCreateSettings SetManagedBy(string id) { ManagedBy = id; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidOperationException("az group create: Name is required.");
        if (string.IsNullOrEmpty(Location))
            throw new InvalidOperationException("az group create: Location is required.");
        var args = new List<string> { "group", "create" };
        EmitCommonArguments(args);
        args.Add("--name"); args.Add(Name!);
        args.Add("--location"); args.Add(Location!);
        if (Tags.Count > 0)
        {
            args.Add("--tags");
            args.AddRange(Tags.Select(kv => $"{kv.Key}={kv.Value}"));
        }
        if (!string.IsNullOrEmpty(ManagedBy)) { args.Add("--managed-by"); args.Add(ManagedBy!); }
        return args;
    }
}

/// <summary>Settings for <c>az group delete</c>.</summary>
public sealed class AzureCliGroupDeleteSettings : AzureCliSettingsBase
{
    public string? Name { get; set; }
    /// <summary>Skip confirmation prompt. Maps to <c>--yes</c> / <c>-y</c>. Strongly recommended in CI.</summary>
    public bool Yes { get; set; }
    /// <summary>Don't wait for the long-running operation to finish. Maps to <c>--no-wait</c>.</summary>
    public bool NoWait { get; set; }
    /// <summary>Force-delete locked resources. Maps to <c>--force-deletion-types</c>.</summary>
    public string? ForceDeletionTypes { get; set; }

    public AzureCliGroupDeleteSettings SetName(string name) { Name = name; return this; }
    public AzureCliGroupDeleteSettings SetYes(bool v = true) { Yes = v; return this; }
    public AzureCliGroupDeleteSettings SetNoWait(bool v = true) { NoWait = v; return this; }
    public AzureCliGroupDeleteSettings SetForceDeletionTypes(string types) { ForceDeletionTypes = types; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidOperationException("az group delete: Name is required.");
        var args = new List<string> { "group", "delete" };
        EmitCommonArguments(args);
        args.Add("--name"); args.Add(Name!);
        if (Yes) args.Add("--yes");
        if (NoWait) args.Add("--no-wait");
        if (!string.IsNullOrEmpty(ForceDeletionTypes)) { args.Add("--force-deletion-types"); args.Add(ForceDeletionTypes!); }
        return args;
    }
}

/// <summary>Settings for <c>az group list</c>.</summary>
public sealed class AzureCliGroupListSettings : AzureCliSettingsBase
{
    /// <summary>Filter expression. Maps to <c>--tag</c> for tag-based filtering.</summary>
    public string? Tag { get; set; }

    public AzureCliGroupListSettings SetTagFilter(string tagExpr) { Tag = tagExpr; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        var args = new List<string> { "group", "list" };
        EmitCommonArguments(args);
        if (!string.IsNullOrEmpty(Tag)) { args.Add("--tag"); args.Add(Tag!); }
        return args;
    }
}
