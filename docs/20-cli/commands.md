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
| `--params-file <path>` | Path to a `.bicepparam` file | — |
| `--param <key=value>` | Override a parameter value; repeat as needed | — |
| `--output <format>` | Output format: `table`, `json`, `csv`, or `markdown` | `table` |

### Examples

```bash
# Basic estimate
bce estimate --file main.bicep

# JSON output for CI pipelines
bce estimate --file main.bicep --output json

# Use a params file and parameter overrides
bce estimate --file main.bicep --params-file main.bicepparam --param env=prod

# Estimate from an ARM template
bce estimate --file main.arm.json --output markdown
```

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
