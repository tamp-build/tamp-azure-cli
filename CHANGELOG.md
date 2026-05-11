# Changelog

All notable changes to `Tamp.AzureCli` are documented in this file.

The format follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/), and the project follows [Semantic Versioning 2.0.0](https://semver.org/).

Pre-1.0 versions may break public API freely between minor versions; the `0.x` line is intentionally a stabilization run.

## [Unreleased]

## [0.1.1] — 2026-05-11

### Added — TAM-161

- Object-init overloads on every Azure CLI wrapper (TAM-161 satellite fanout). Every tool-bound verb that takes `(Tool, Action<TSettings>)` now also accepts `(Tool, TSettings)`, mirroring the fluent body with no configurer invocation. Both styles produce byte-equal `CommandPlan`s. Fluent stays canonical in docs; object-init is available for consumers who prefer the C# initializer shape.
