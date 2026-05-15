# Tamp.AzureCli

Wrapper for the **Azure CLI (`az`) 2.x** — the foundational Azure
wrapper that everything else builds on. Service principal secrets and
WIF federated tokens are typed as `Secret`.

```csharp
using Tamp.AzureCli.V2;
```

| Package | az CLI | Status |
|---|---|---|
| `Tamp.AzureCli.V2` | 2.x | preview |

Requires `Tamp.Core ≥ 1.0.4`.

## Verbs (v0.1.0)

The az surface is enormous (hundreds of subgroups). v0.1.0 types the
load-bearing verbs that the Strata pipeline needs to adopt; the rest
ships incrementally and lives behind `AzureCli.Raw(...)` for now.

| Verb | Notes |
|---|---|
| `Login` | Five modes via `AzureCliLoginMode`: Interactive, DeviceCode, ServicePrincipal (Secret), ServicePrincipalCertificate, ManagedIdentity, FederatedToken (Secret for WIF). |
| `Logout` | Active account or named username. |
| `Rest` | The generic REST proxy. Load-bearing for FC1 listKeys + ARM operations without typed verbs. |
| `Version` | `az version`. |
| `Group.Show` / `Exists` / `Create` / `Delete` / `List` | Resource group lifecycle. |
| `Account.Show` / `List` / `Set` | Subscription / tenant context management. |
| `Bicep.Build` / `Install` / `Version` | Bundled Bicep binary — alternative to the standalone `Tamp.Bicep` package. |
| `Raw` | Escape hatch for the long tail (`acr`, `aks`, `sql`, `keyvault`, `eventhubs`, `staticwebapp`, `functionapp`, `webapp`, ...). |

**Common flags (all verbs)**: `--output` / `-o`, `--query`,
`--subscription`, `--verbose`, `--debug`, `--only-show-errors`.

**Env knob**: `SetDisableConnectionVerification(true)` sets
`AZURE_CLI_DISABLE_CONNECTION_VERIFICATION=1` in the process env —
for Zscaler-behind users. Pair with `SetEnv("SSL_CERT_FILE",
"...")` for the proper-trust path.

## Quick example — WIF login + Flex Consumption listKeys

```csharp
using Tamp;
using Tamp.AzureCli.V2;

[NuGetPackage("az", UseSystemPath = true)]
readonly Tool AzTool = null!;

[Secret("WIF OIDC token", EnvironmentVariable = "AZURE_FEDERATED_TOKEN")]
readonly Secret WifToken = null!;

Target Login => _ => _.Executes(() =>
    AzureCli.Login(AzTool, s => s
        .SetMode(AzureCliLoginMode.FederatedToken)
        .SetUsername(WifClientId)
        .SetFederatedToken(WifToken)
        .SetTenant(TenantId)
        .SetSkipSubscriptionDiscovery()));

// FC1 (Flex Consumption) host listKeys isn't exposed via `az functionapp keys list`.
// Must go through az rest directly.
Target FetchFunctionHostKey => _ => _
    .DependsOn(nameof(Login))
    .Executes(() =>
        AzureCli.Rest(AzTool, s => s
            .SetMethod("post")
            .SetUri($"https://management.azure.com/subscriptions/{SubId}/resourceGroups/{Rg}/providers/Microsoft.Web/sites/{FuncName}/host/default/listKeys?api-version=2023-12-01")
            .SetQuery("functionKeys.default")
            .SetOutput("tsv")));
```

## CI behaviour to know about

**`az` is preinstalled on GitHub-hosted runners** (ubuntu-latest,
windows-latest, macos-latest). No install step needed — the wrapper's
own CI just runs `az version` as a smoke check.

**WIF token via stdin?** The az CLI 2.86 only accepts
`--federated-token` as an argv flag. Token argv exposure is mitigated
by the redaction table: the `Secret` value joins the runner's mask
list, so it's scrubbed from subsequent log output. If/when az adds
stdin support, the wrapper switches automatically.

**`AZURE_CLI_DISABLE_CONNECTION_VERIFICATION=1`** disables TLS cert
validation for environments behind a TLS-intercepting proxy
(Zscaler). Use only when SSL_CERT_FILE isn't a viable alternative.

## What's NOT in v0.1.0

Hand-rolled today via `Raw`, slated for v0.2.0 typed verbs:
`ad app/sp/federated-credential` (TAM-99 dependency),
`role assignment`, `deployment group` (Bicep ARM deploy),
`staticwebapp`, `functionapp`, `webapp`, `keyvault`, `acr`, `aks`,
`sql`, `network`, `eventhubs`, `servicebus` (admin operations).

## Releasing

See [MAINTAINERS.md](MAINTAINERS.md).

## Settings authoring style

Examples above use the fluent `Set*`-chain shape. Every wrapper verb also accepts a `new XxxSettings { ... }` object-init form — both produce identical `CommandPlan`s. The fluent shape stays canonical in docs and the `tamp init` template; opt into object-init scaffolding via `tamp init --settings-style=init`.

See [Build Script Authoring → Two authoring styles](https://github.com/tamp-build/tamp/wiki/Build-Script-Authoring#two-authoring-styles-for-wrapper-calls-120) on the wiki for the side-by-side comparison.
