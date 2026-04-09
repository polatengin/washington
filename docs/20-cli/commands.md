---
title: CLI Commands
sidebar_position: 20
---

# CLI Commands

_Bicep Cost Estimator_ CLI can be installed by running the following command: `curl -sL https://bicepcostestimator.net/install.sh | bash`

After it is installed, you can use it with the `bce` command.

## `bce estimate`

Estimate the cost of an infrastructure-as-code file.

```bash
bce estimate --file <path> [options]
```

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--file <path>` | Path to a `.bicep` or ARM JSON file | required |
| `--params-file <path>` | Path to a `.bicepparam` file whose values should be applied during estimation | - |
| `--param <key=value>` | Override a parameter value from the command line | - |
| `--output <format>` | Output format: `table`, `json`, `csv`, or `markdown` | `table` |

### Examples

```bash
# Basic estimate
bce estimate --file main.bicep

# JSON output for CI pipelines
bce estimate --file main.bicep --output json

# Apply a params file and override one value at the command line
bce estimate --file main.bicep --params-file main.bicepparam --param env=prod

# Estimate from an ARM template
bce estimate --file main.arm.json --output markdown
```

## `bce docs`

Browse the live documentation site from your terminal.

```bash
bce docs
```

### Examples

```bash
# Open the interactive terminal browser
bce docs

# Print a single page
bce docs /getting-started

# List all published routes
bce docs list

# Search titles and summaries
bce docs search troubleshooting
```

### Current Behavior Notes

- `bce docs` reads from the deployed docs site at `https://bicepcostestimator.net`, so it always shows the latest published content.
- When it runs in a terminal, it opens an interactive browser with arrow-key navigation.
- When output is redirected, `bce docs` prints the introduction page and exits.

## Current Behavior Notes

- Unsupported resource types are skipped and returned as warnings in the output.
- Spot and low-priority pricing is excluded from the default pricing query set.
- Parameter values from `.bicepparam` files and repeatable `--param` overrides are applied during resource extraction.

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
