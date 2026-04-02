---
title: CLI Commands
sidebar_position: 20
---

# CLI Commands

Washington's CLI is published as `bce`.

## `bce estimate`

Estimate the cost of an infrastructure-as-code file.

```bash
bce estimate --file <path> [options]
```

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--file <path>` | Path to a `.bicep` or ARM JSON file | required |
| `--params-file <path>` | Path to a `.bicepparam` file; currently compiled for validation alongside the main template | — |
| `--param <key=value>` | Parsed as a parameter override; not yet applied to the compiled estimate result | — |
| `--output <format>` | Output format: `table`, `json`, `csv`, or `markdown` | `table` |

### Examples

```bash
# Basic estimate
bce estimate --file main.bicep

# JSON output for CI pipelines
bce estimate --file main.bicep --output json

# Validate an associated params file while estimating the template
bce estimate --file main.bicep --params-file main.bicepparam --param env=prod

# Estimate from an ARM template
bce estimate --file main.arm.json --output markdown
```

## Current Behavior Notes

- Unsupported resource types are skipped and returned as warnings in the output.
- Spot and low-priority pricing is excluded from the default pricing query set.
- The current implementation accepts `--params-file` and `--param`, but estimate results are still driven by the compiled template output rather than merged parameter values.

## `bce cache info`

Show the current pricing cache size and entry count.

```bash
bce cache info
```

## `bce cache clear`

Remove all cached pricing data.

```bash
bce cache clear
```

## `bce lsp`

Start the Language Server Protocol server used by the VS Code extension.

```bash
bce lsp
```

## Global Options

- `bce --help` shows command usage.
- `bce --version` prints the current CLI version.
