namespace Tamp.AzureCli.V2;

/// <summary>
/// Login modes supported by <c>az login</c>. The wrapper picks the
/// flag combination based on this mode + the credential properties
/// you populate.
/// </summary>
public enum AzureCliLoginMode
{
    /// <summary>Interactive browser flow. Default — for local dev only.</summary>
    Interactive,
    /// <summary>Device code flow (interactive in a separate browser). <c>--use-device-code</c>.</summary>
    DeviceCode,
    /// <summary>Service principal with secret. Username = client ID, Password = SPN secret (typed as <see cref="Secret"/>).</summary>
    ServicePrincipal,
    /// <summary>Service principal with certificate. Username = client ID, Certificate = PEM path.</summary>
    ServicePrincipalCertificate,
    /// <summary>Managed identity. Optionally specify <see cref="UserAssignedClientId"/>, <see cref="UserAssignedObjectId"/>, or <see cref="UserAssignedResourceId"/>.</summary>
    ManagedIdentity,
    /// <summary>OIDC / Workload Identity Federation. Username = client ID, FederatedToken = OIDC token from the trust provider (typed as <see cref="Secret"/>).</summary>
    FederatedToken,
}

/// <summary>
/// Settings for <c>az login</c>. Picks the flag set based on
/// <see cref="Mode"/>; SPN secret and federated token are typed as
/// <see cref="Secret"/> and join the redaction table.
/// </summary>
public sealed class AzureCliLoginSettings : AzureCliSettingsBase
{
    /// <summary>Which authentication flow to use. Default: <see cref="AzureCliLoginMode.Interactive"/>.</summary>
    public AzureCliLoginMode Mode { get; set; } = AzureCliLoginMode.Interactive;

    /// <summary>Client ID (for SPN / WIF) or user name (for Interactive). Maps to <c>--username</c> / <c>-u</c>.</summary>
    public string? Username { get; set; }

    /// <summary>SPN secret. Maps to <c>--password</c> / <c>-p</c>. Required for <see cref="AzureCliLoginMode.ServicePrincipal"/>.</summary>
    public Secret? Password { get; set; }

    /// <summary>PEM certificate path for SPN-with-cert. Required for <see cref="AzureCliLoginMode.ServicePrincipalCertificate"/>.</summary>
    public string? Certificate { get; set; }

    /// <summary>Use Subject Name + Issuer authentication for SPN-with-cert. Maps to <c>--use-cert-sn-issuer</c>.</summary>
    public bool UseCertSnIssuer { get; set; }

    /// <summary>OIDC / federated token. Required for <see cref="AzureCliLoginMode.FederatedToken"/>.</summary>
    public Secret? FederatedToken { get; set; }

    /// <summary>Tenant ID or domain. Maps to <c>--tenant</c> / <c>-t</c>. Required for SPN and FederatedToken modes.</summary>
    public string? Tenant { get; set; }

    /// <summary>Set the default subscription after login. Maps to <c>--subscription</c> / <c>-s</c> on login (NOT the global --subscription).</summary>
    public string? DefaultSubscription { get; set; }

    /// <summary>For <see cref="AzureCliLoginMode.ManagedIdentity"/>: client ID of the user-assigned identity. Maps to <c>--client-id</c>.</summary>
    public string? UserAssignedClientId { get; set; }

    /// <summary>For <see cref="AzureCliLoginMode.ManagedIdentity"/>: object ID of the user-assigned identity. Maps to <c>--object-id</c>.</summary>
    public string? UserAssignedObjectId { get; set; }

    /// <summary>For <see cref="AzureCliLoginMode.ManagedIdentity"/>: resource ID of the user-assigned identity. Maps to <c>--resource-id</c>.</summary>
    public string? UserAssignedResourceId { get; set; }

    /// <summary>Allow login to tenants without subscriptions. Maps to <c>--allow-no-subscriptions</c>.</summary>
    public bool AllowNoSubscriptions { get; set; }

    /// <summary>Skip subscription discovery (faster for SPN logins that don't need it). Maps to <c>--skip-subscription-discovery</c>.</summary>
    public bool SkipSubscriptionDiscovery { get; set; }

    public AzureCliLoginSettings SetMode(AzureCliLoginMode mode) { Mode = mode; return this; }
    public AzureCliLoginSettings SetUsername(string? user) { Username = user; return this; }
    public AzureCliLoginSettings SetPassword(Secret? pwd) { Password = pwd; return this; }
    public AzureCliLoginSettings SetCertificate(string? path) { Certificate = path; return this; }
    public AzureCliLoginSettings SetUseCertSnIssuer(bool v = true) { UseCertSnIssuer = v; return this; }
    public AzureCliLoginSettings SetFederatedToken(Secret? token) { FederatedToken = token; return this; }
    public AzureCliLoginSettings SetTenant(string? tenant) { Tenant = tenant; return this; }
    public AzureCliLoginSettings SetDefaultSubscription(string? nameOrId) { DefaultSubscription = nameOrId; return this; }
    public AzureCliLoginSettings SetUserAssignedClientId(string? id) { UserAssignedClientId = id; return this; }
    public AzureCliLoginSettings SetUserAssignedObjectId(string? id) { UserAssignedObjectId = id; return this; }
    public AzureCliLoginSettings SetUserAssignedResourceId(string? id) { UserAssignedResourceId = id; return this; }
    public AzureCliLoginSettings SetAllowNoSubscriptions(bool v = true) { AllowNoSubscriptions = v; return this; }
    public AzureCliLoginSettings SetSkipSubscriptionDiscovery(bool v = true) { SkipSubscriptionDiscovery = v; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        ValidateMode();
        var args = new List<string> { "login" };
        EmitCommonArguments(args);

        switch (Mode)
        {
            case AzureCliLoginMode.Interactive:
                if (!string.IsNullOrEmpty(Username)) { args.Add("--username"); args.Add(Username!); }
                if (!string.IsNullOrEmpty(Tenant)) { args.Add("--tenant"); args.Add(Tenant!); }
                break;

            case AzureCliLoginMode.DeviceCode:
                args.Add("--use-device-code");
                if (!string.IsNullOrEmpty(Tenant)) { args.Add("--tenant"); args.Add(Tenant!); }
                break;

            case AzureCliLoginMode.ServicePrincipal:
                args.Add("--service-principal");
                args.Add("--username"); args.Add(Username!);
                args.Add("--password"); args.Add(Password!.Reveal());
                args.Add("--tenant"); args.Add(Tenant!);
                break;

            case AzureCliLoginMode.ServicePrincipalCertificate:
                args.Add("--service-principal");
                args.Add("--username"); args.Add(Username!);
                args.Add("--certificate"); args.Add(Certificate!);
                if (UseCertSnIssuer) args.Add("--use-cert-sn-issuer");
                args.Add("--tenant"); args.Add(Tenant!);
                break;

            case AzureCliLoginMode.ManagedIdentity:
                args.Add("--identity");
                if (!string.IsNullOrEmpty(UserAssignedClientId)) { args.Add("--client-id"); args.Add(UserAssignedClientId!); }
                if (!string.IsNullOrEmpty(UserAssignedObjectId)) { args.Add("--object-id"); args.Add(UserAssignedObjectId!); }
                if (!string.IsNullOrEmpty(UserAssignedResourceId)) { args.Add("--resource-id"); args.Add(UserAssignedResourceId!); }
                break;

            case AzureCliLoginMode.FederatedToken:
                args.Add("--service-principal");
                args.Add("--username"); args.Add(Username!);
                args.Add("--federated-token"); args.Add(FederatedToken!.Reveal());
                args.Add("--tenant"); args.Add(Tenant!);
                break;
        }

        if (AllowNoSubscriptions) args.Add("--allow-no-subscriptions");
        if (SkipSubscriptionDiscovery) args.Add("--skip-subscription-discovery");
        if (!string.IsNullOrEmpty(DefaultSubscription))
        {
            // -s on login is the "set default" form (NOT the global
            // --subscription override). EmitCommonArguments handled
            // the global form via Subscription/--subscription; this
            // one is login-specific.
            args.Add("-s"); args.Add(DefaultSubscription!);
        }

        return args;
    }

    private void ValidateMode()
    {
        switch (Mode)
        {
            case AzureCliLoginMode.ServicePrincipal:
                if (string.IsNullOrEmpty(Username)) throw new InvalidOperationException("az login --service-principal: Username (client ID) is required.");
                if (Password is null) throw new InvalidOperationException("az login --service-principal: Password (SPN secret) is required.");
                if (string.IsNullOrEmpty(Tenant)) throw new InvalidOperationException("az login --service-principal: Tenant is required.");
                break;
            case AzureCliLoginMode.ServicePrincipalCertificate:
                if (string.IsNullOrEmpty(Username)) throw new InvalidOperationException("az login --service-principal --certificate: Username (client ID) is required.");
                if (string.IsNullOrEmpty(Certificate)) throw new InvalidOperationException("az login --service-principal --certificate: Certificate path is required.");
                if (string.IsNullOrEmpty(Tenant)) throw new InvalidOperationException("az login --service-principal --certificate: Tenant is required.");
                break;
            case AzureCliLoginMode.FederatedToken:
                if (string.IsNullOrEmpty(Username)) throw new InvalidOperationException("az login --federated-token: Username (client ID) is required.");
                if (FederatedToken is null) throw new InvalidOperationException("az login --federated-token: FederatedToken is required.");
                if (string.IsNullOrEmpty(Tenant)) throw new InvalidOperationException("az login --federated-token: Tenant is required.");
                break;
        }
    }

    protected override IReadOnlyList<Secret> CollectSecrets()
    {
        var secrets = new List<Secret>();
        if (Password is not null) secrets.Add(Password);
        if (FederatedToken is not null) secrets.Add(FederatedToken);
        return secrets;
    }
}

/// <summary>Settings for <c>az logout</c>.</summary>
public sealed class AzureCliLogoutSettings : AzureCliSettingsBase
{
    /// <summary>User name or service principal client ID to log out. Maps to <c>--username</c>. Empty = log out the active account.</summary>
    public string? Username { get; set; }

    public AzureCliLogoutSettings SetUsername(string? user) { Username = user; return this; }

    protected override IEnumerable<string> BuildVerbArguments()
    {
        var args = new List<string> { "logout" };
        EmitCommonArguments(args);
        if (!string.IsNullOrEmpty(Username)) { args.Add("--username"); args.Add(Username!); }
        return args;
    }
}
