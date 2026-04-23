---
title: Configuration
description: Configure per-run estimation flags and the Bicep compiler environment used by BCE.
sidebar_position: 21
---

# CLI Configuration

The current CLI does not support a dedicated config file or `BCE_*` environment variables. Configure behavior on each invocation with command flags.

## Per-Run Options

| Option | Description | Default |
|----------|-------------|---------|
| `--params-file <path>` | Apply parameter values from a `.bicepparam` file | - |
| `--param <key=value>` | Override a parameter value on the command line | - |
| `--output <format>` | Choose `table`, `json`, `csv`, or `markdown` output | `table` |

## Runtime Defaults

- If a resource region cannot be resolved from the source file, `bce` falls back to `eastus`.
- Pricing responses are cached for 24 hours under `~/.bicep-cost-estimator/cache`.
- The VS Code extension setting `bce.cliPath` can point to a custom `bce` binary.

## Current Limitations

- There is no persisted project or user-level CLI config file yet.
- Parameter application only affects values that flow through template parameters and into priced resource properties.

## Examples

```bash
bce estimate --file main.bicep --output json

bce estimate --file main.bicep --params-file main.bicepparam --param env=prod
```
