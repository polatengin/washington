---
title: Configuration
sidebar_position: 21
---

# CLI Configuration

Washington's CLI is published as `bce`.

The current CLI does not support a dedicated config file or `BCE_*` environment variables. Configure behavior on each invocation with command flags.

## Per-Run Options

| Option | Description | Default |
|----------|-------------|---------|
| `--params-file <path>` | Load parameter values from a `.bicepparam` file | — |
| `--param <key=value>` | Override a parameter value on the command line | — |
| `--output <format>` | Choose `table`, `json`, `csv`, or `markdown` output | `table` |

## Runtime Defaults

- If a resource region cannot be resolved from the source file, `bce` falls back to `eastus`.
- Pricing responses are cached for 24 hours under `~/.bicep-cost-estimator/cache`.
- The VS Code extension setting `washington.cliPath` can point to a custom `bce` binary.

## Examples

```bash
bce estimate --file main.bicep --output json

bce estimate --file main.bicep --params-file main.bicepparam --param env=prod
```
