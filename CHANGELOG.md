# Changelog

All notable changes to `Tamp.AzureCli` are documented in this file.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/), and the project follows [Semantic Versioning 2.0.0](https://semver.org/).

Pre-1.0 versions may break public API freely between minor versions; the `0.x` line is intentionally a stabilization run.

## [Unreleased]

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
