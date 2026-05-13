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

        /// <summary>
        /// <c>az account get-access-token</c> — produces a <see cref="CommandPlan"/> that prints
        /// the bearer-token JSON to stdout. Use this for the plan-as-Target case (dry-run preview, etc.).
        /// For most consumers the <see cref="GetAccessTokenAsSecret"/> capture-and-parse helper is what you want.
        /// </summary>
        public static CommandPlan GetAccessToken(Tool tool, Action<AzureCliAccountGetAccessTokenSettings>? configure = null)
            => AzureCli.Build<AzureCliAccountGetAccessTokenSettings>(tool, configure);

        /// <summary>
        /// Acquire a bearer token directly into a <see cref="Secret"/>. Spawns
        /// <c>az account get-access-token --output json --resource &lt;resource&gt;</c>, captures
        /// stdout, parses the <c>accessToken</c> field, wraps it. The returned Secret is
        /// suitable for <c>ApiCredential.Bearer(...)</c> against ARM, Key Vault data-plane,
        /// Dataverse Web API, Microsoft Graph, etc.
        /// </summary>
        /// <param name="tool">Resolved <c>az</c> tool.</param>
        /// <param name="resource">Target audience. Default <c>https://management.azure.com</c> (ARM).</param>
        /// <param name="tenant">Optional tenant override — useful on multi-tenant developer machines.</param>
        /// <param name="subscription">Optional subscription scope.</param>
        /// <param name="secretName">Identifier for the resulting Secret. Default <c>az-token</c>.</param>
        /// <exception cref="InvalidOperationException">Thrown when the CLI exits non-zero or the
        /// stdout JSON is missing the <c>accessToken</c> field.</exception>
        public static Secret GetAccessTokenAsSecret(
            Tool tool,
            string resource = "https://management.azure.com",
            string? tenant = null,
            string? subscription = null,
            string secretName = "az-token")
        {
            if (tool is null) throw new ArgumentNullException(nameof(tool));
            if (string.IsNullOrWhiteSpace(resource))
                throw new ArgumentException("resource must not be empty.", nameof(resource));

            var settings = new AzureCliAccountGetAccessTokenSettings { Output = "json" };
            settings.SetResource(resource);
            if (!string.IsNullOrEmpty(tenant)) settings.SetTenant(tenant);
            if (!string.IsNullOrEmpty(subscription)) settings.SetSubscription(subscription);

            var plan = settings.ToCommandPlan(tool);
            var capture = ProcessRunner.Capture(plan);
            if (capture.ExitCode != 0)
                throw new InvalidOperationException(
                    $"az account get-access-token failed (exit {capture.ExitCode}): {capture.StderrText}");

            var stdout = capture.StdoutText;
            using var doc = System.Text.Json.JsonDocument.Parse(stdout);
            if (!doc.RootElement.TryGetProperty("accessToken", out var tokenElem))
                throw new InvalidOperationException(
                    "az account get-access-token: response JSON did not contain an 'accessToken' field. " +
                    "Captured stdout (head): " +
                    stdout.Substring(0, Math.Min(200, stdout.Length)));

            var tokenValue = tokenElem.GetString()
                ?? throw new InvalidOperationException("accessToken field was null.");
            return new Secret(secretName, tokenValue);
        }

        // ---- Object-init overloads (0.2.0+, TAM-161) ----

        public static CommandPlan Show(Tool tool, AzureCliAccountShowSettings settings) => Plan(tool, settings);
        public static CommandPlan List(Tool tool, AzureCliAccountListSettings settings) => Plan(tool, settings);
        public static CommandPlan Set(Tool tool, AzureCliAccountSetSettings settings) => Plan(tool, settings);
        public static CommandPlan GetAccessToken(Tool tool, AzureCliAccountGetAccessTokenSettings settings) => Plan(tool, settings);
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
