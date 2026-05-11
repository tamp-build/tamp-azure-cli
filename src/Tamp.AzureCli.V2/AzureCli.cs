namespace Tamp.AzureCli.V2;

/// <summary>Facade for the Azure CLI (az) 2.x.</summary>
/// <remarks>
/// <para>Resolve via <c>[NuGetPackage(UseSystemPath = true)]</c>:</para>
/// <code>
/// [NuGetPackage("az", UseSystemPath = true)]
/// readonly Tool AzTool;
/// </code>
/// <para>The az binary is preinstalled on all three GitHub-hosted runner images.</para>
/// </remarks>
public static class AzureCli
{
    /// <summary><c>az login</c> — pick auth flow via <see cref="AzureCliLoginSettings.Mode"/>.</summary>
    public static CommandPlan Login(Tool tool, Action<AzureCliLoginSettings> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        return Build(tool, configure);
    }

    /// <summary><c>az logout</c>.</summary>
    public static CommandPlan Logout(Tool tool, Action<AzureCliLogoutSettings>? configure = null)
        => Build<AzureCliLogoutSettings>(tool, configure);

    /// <summary><c>az rest</c> — generic REST proxy. The Swiss-army knife for ARM operations without a typed verb.</summary>
    public static CommandPlan Rest(Tool tool, Action<AzureCliRestSettings> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        return Build(tool, configure);
    }

    /// <summary><c>az version</c>.</summary>
    public static CommandPlan Version(Tool tool, Action<AzureCliVersionSettings>? configure = null)
        => Build<AzureCliVersionSettings>(tool, configure);

    /// <summary>Sub-facade for <c>az group &lt;verb&gt;</c>.</summary>
    public static class Group
    {
        public static CommandPlan Show(Tool tool, Action<AzureCliGroupShowSettings> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            return Build(tool, configure);
        }

        public static CommandPlan Exists(Tool tool, Action<AzureCliGroupExistsSettings> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            return Build(tool, configure);
        }

        public static CommandPlan Create(Tool tool, Action<AzureCliGroupCreateSettings> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            return Build(tool, configure);
        }

        public static CommandPlan Delete(Tool tool, Action<AzureCliGroupDeleteSettings> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            return Build(tool, configure);
        }

        public static CommandPlan List(Tool tool, Action<AzureCliGroupListSettings>? configure = null)
            => AzureCli.Build<AzureCliGroupListSettings>(tool, configure);

        // ---- Object-init overloads (0.2.0+, TAM-161) ----
        // Two equivalent authoring styles; both produce identical CommandPlans. Fluent
        // stays canonical in docs and `tamp init` templates; object-init available for
        // consumers who prefer the C# initializer shape.
        //
        //     AzureCli.Group.Show(AzTool, new() { Name = "rg-prod" });
        //
        // is equivalent to:
        //
        //     AzureCli.Group.Show(AzTool, s => s.SetName("rg-prod"));

        public static CommandPlan Show(Tool tool, AzureCliGroupShowSettings settings) => Plan(tool, settings);
        public static CommandPlan Exists(Tool tool, AzureCliGroupExistsSettings settings) => Plan(tool, settings);
        public static CommandPlan Create(Tool tool, AzureCliGroupCreateSettings settings) => Plan(tool, settings);
        public static CommandPlan Delete(Tool tool, AzureCliGroupDeleteSettings settings) => Plan(tool, settings);
        public static CommandPlan List(Tool tool, AzureCliGroupListSettings settings) => Plan(tool, settings);
    }

    /// <summary>Sub-facade for <c>az account &lt;verb&gt;</c>.</summary>
    public static class Account
    {
        public static CommandPlan Show(Tool tool, Action<AzureCliAccountShowSettings>? configure = null)
            => AzureCli.Build<AzureCliAccountShowSettings>(tool, configure);

        public static CommandPlan List(Tool tool, Action<AzureCliAccountListSettings>? configure = null)
            => AzureCli.Build<AzureCliAccountListSettings>(tool, configure);

        public static CommandPlan Set(Tool tool, Action<AzureCliAccountSetSettings> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            return AzureCli.Build(tool, configure);
        }

        // ---- Object-init overloads (0.2.0+, TAM-161) ----

        public static CommandPlan Show(Tool tool, AzureCliAccountShowSettings settings) => Plan(tool, settings);
        public static CommandPlan List(Tool tool, AzureCliAccountListSettings settings) => Plan(tool, settings);
        public static CommandPlan Set(Tool tool, AzureCliAccountSetSettings settings) => Plan(tool, settings);
    }

    /// <summary>Sub-facade for <c>az bicep &lt;verb&gt;</c>. Uses the Bicep binary bundled with the az CLI.</summary>
    public static class Bicep
    {
        public static CommandPlan Build(Tool tool, Action<AzureCliBicepBuildSettings> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            return AzureCli.Build(tool, configure);
        }

        public static CommandPlan Install(Tool tool, Action<AzureCliBicepInstallSettings>? configure = null)
            => AzureCli.Build<AzureCliBicepInstallSettings>(tool, configure);

        public static CommandPlan Version(Tool tool, Action<AzureCliBicepVersionSettings>? configure = null)
            => AzureCli.Build<AzureCliBicepVersionSettings>(tool, configure);

        // ---- Object-init overloads (0.2.0+, TAM-161) ----

        public static CommandPlan Build(Tool tool, AzureCliBicepBuildSettings settings) => Plan(tool, settings);
        public static CommandPlan Install(Tool tool, AzureCliBicepInstallSettings settings) => Plan(tool, settings);
        public static CommandPlan Version(Tool tool, AzureCliBicepVersionSettings settings) => Plan(tool, settings);
    }

    /// <summary>Escape hatch for verbs we haven't typed in v0.1.0 (the long tail: acr, aks, sql, keyvault, eventhubs, …).</summary>
    public static CommandPlan Raw(Tool tool, params string[] arguments)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (arguments is null || arguments.Length == 0)
            throw new ArgumentException("Raw requires at least one argument.", nameof(arguments));
        var s = new AzureCliRawSettings();
        s.AddArgs(arguments);
        return s.ToCommandPlan(tool);
    }

    private static CommandPlan Build<T>(Tool tool, Action<T>? configure) where T : AzureCliSettingsBase, new()
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        var s = new T();
        configure?.Invoke(s);
        return s.ToCommandPlan(tool);
    }

    private static CommandPlan Plan<T>(Tool tool, T settings) where T : AzureCliSettingsBase
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        return settings.ToCommandPlan(tool);
    }

    // ---- Object-init overloads (0.2.0+, TAM-161) ----
    // Two equivalent authoring styles; both produce identical CommandPlans. Fluent
    // stays canonical in docs and `tamp init` templates; object-init available for
    // consumers who prefer the C# initializer shape.
    //
    //     AzureCli.Login(AzTool, new() { Mode = AzureCliLoginMode.DeviceCode, Tenant = "contoso.onmicrosoft.com" });
    //
    // is equivalent to:
    //
    //     AzureCli.Login(AzTool, s => s.SetMode(AzureCliLoginMode.DeviceCode).SetTenant("contoso.onmicrosoft.com"));

    public static CommandPlan Login(Tool tool, AzureCliLoginSettings settings) => Plan(tool, settings);
    public static CommandPlan Logout(Tool tool, AzureCliLogoutSettings settings) => Plan(tool, settings);
    public static CommandPlan Rest(Tool tool, AzureCliRestSettings settings) => Plan(tool, settings);
    public static CommandPlan Version(Tool tool, AzureCliVersionSettings settings) => Plan(tool, settings);
}
