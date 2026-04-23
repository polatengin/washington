---
title: Configuration
description: Configure per-run estimation flags and runtime defaults used by BCE.
sidebar_position: 21
---

# CLI Configuration

The CLI does not support a dedicated project config file yet. Today you configure estimation behavior per run with command flags.

## Per-Run Flags

| Option | Description | Default |
|----------|-------------|---------|
| `--params-file <path>` | Apply parameter values from a `.bicepparam` file | - |
| `--param <key=value>` | Override a parameter value on the command line | - |
| `--output <format>` | Choose `table`, `json`, `csv`, or `markdown` output | `table` |

## Runtime Defaults

- For `.bicep` compilation, `bce` uses a `bicep` binary from `PATH` when one is available. Otherwise it downloads the pinned Bicep CLI release automatically.
- If a resource region cannot be resolved from the source file, `bce` falls back to `eastus`.
- Pricing responses are cached for 24 hours under `~/.bicep-cost-estimator/cache`.
- The VS Code extension has its own separate CLI discovery flow and `CLI Path` setting.

## Current Limitations

- There is no persisted project-level or user-level BCE config file yet.
- There are no estimator-specific runtime settings yet for defaults like output format, cache TTL, or default region.
- Parameter application only affects values that flow through template parameters and into priced resource properties.

## Examples

```bash
bce estimate --file main.bicep --output json

bce estimate --file main.bicep --params-file main.bicepparam --param env=prod

bce estimate --file main.bicep
```
