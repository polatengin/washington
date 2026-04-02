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
| `--params-file <path>` | Compile a `.bicepparam` file for validation alongside the main template | — |
| `--param <key=value>` | Provide a parameter override token; currently parsed but not yet applied to the estimate result | — |
| `--output <format>` | Choose `table`, `json`, `csv`, or `markdown` output | `table` |

## Runtime Defaults

- If a resource region cannot be resolved from the source file, `bce` falls back to `eastus`.
- Pricing responses are cached for 24 hours under `~/.bicep-cost-estimator/cache`.
- The VS Code extension setting `washington.cliPath` can point to a custom `bce` binary.

## Current Limitations

- There is no persisted project or user-level CLI config file yet.
- `--params-file` is currently useful for validating that the paired params file compiles, but estimate values still come from the compiled template path.
- `--param` overrides are parsed by the CLI today, but they do not yet change the final estimate output.

## Examples

```bash
bce estimate --file main.bicep --output json

bce estimate --file main.bicep --params-file main.bicepparam --param env=prod
```
