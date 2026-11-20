# Changelog

All notable changes to `Tamp.AzureCli` are documented in this file.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/), and the project follows [Semantic Versioning 2.0.0](https://semver.org/).

Pre-1.0 versions may break public API freely between minor versions; the `0.x` line is intentionally a stabilization run.

## [Unreleased]

## [0.2.0] - 2026-11-20 - .NET 11 in, .NET 8 and .NET 9 out

### Changed

- **Multi-targets `net10.0;net11.0`** instead of `net8.0;net9.0;net10.0` ([ADR 0015](https://github.com/tamp-build/tamp/blob/main/docs/adr/0015-target-framework-strategy.md)). .NET 11 shipped 2026-11-10 and .NET 8 and .NET 9 reached end of support the same day; the rule is to add a TFM the day a release ships and drop one the day Microsoft ends its support. `global.json` pins SDK 11.0.100.

  **If you target `net8.0` or `net9.0`, stay on the previous version** — this one carries no assets your project can resolve.

- **`Tamp.*` pinned to 1.21.0**, which carries a SARIF fix worth knowing about if this project's output is read as evidence: before 1.21.0, a SARIF result whose scanner omitted `level` was read as `none` — the least severe value — instead of the declared `warning`, on every runtime before net11 ([tamp#166](https://github.com/tamp-build/tamp/issues/166)).

- **Publishing moved to nuget.org trusted publishing (OIDC).** The `tamp_nuget_api_key` org secret expired 2026-11-01 along with every other pre-2026-08-17 nuget.org key. The publish job now mints a short-lived key from its own OIDC token, so there is no long-lived credential to rotate or leak.

## [0.1.3] — 2026-09-27

- Package now ships XML documentation files (`.xml`) alongside the assembly, so consumers get IntelliSense and API docs. (Mirrors [tamp-build/tamp#3](https://github.com/tamp-build/tamp/pull/50).)

## [0.1.2] — 2026-05-13

### Added

- **`AzureCli.Account.GetAccessToken(...)`** — `CommandPlan` factory for
  `az account get-access-token --resource <X>`. Audiences include
  `https://management.azure.com` (ARM, default), `https://vault.azure.net`
  (Key Vault data-plane), Dataverse, Microsoft Graph, etc.

- **`AzureCli.Account.GetAccessTokenAsSecret(tool, resource, tenant?, subscription?, secretName?)`** —
  capture-and-parse helper that spawns the CLI, parses the `accessToken` from the JSON
  response, wraps it in a `Secret`. Suitable for `ApiCredential.Bearer(...)` against
  ARM / KV / Dataverse / Graph without the caller handling stdout parsing or
  redaction themselves.

  ```csharp
  var armToken = AzureCli.Account.GetAccessTokenAsSecret(Az,
      resource: "https://management.azure.com",
      tenant: "contoso.onmicrosoft.com");

  using var mgmt = new ManagementClient(subId, rg, "strata-api-dev",
      ApiCredential.Bearer(armToken));
  ```

- **`AzureCliAccountGetAccessTokenSettings`** — fluent + object-init settings type
  with `Resource`, `Tenant`, `Scope` knobs (Subscription inherited from base).

### Notes

- Driven by strata-scott's 2026-05-13 follow-up after the TAM-172–177 wave —
  high-frequency call pattern for direct ARM/KV/Dataverse REST against Tamp.Kudu's
  `ManagementClient` and similar bearer-auth consumers.

## [0.1.1] — 2026-05-11

### Added — TAM-161

- Object-init overloads on every Azure CLI wrapper (TAM-161 satellite fanout). Every tool-bound verb that takes `(Tool, Action<TSettings>)` now also accepts `(Tool, TSettings)`, mirroring the fluent body with no configurer invocation. Both styles produce byte-equal `CommandPlan`s. Fluent stays canonical in docs; object-init is available for consumers who prefer the C# initializer shape.
