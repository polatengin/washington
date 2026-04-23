---
title: CLI Commands
description: Reference the BCE CLI commands, output formats, docs browser, cache commands, and version behavior.
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

`bce estimate` accepts either a `.bicep` file or an ARM JSON template.

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `--file <path>` | Path to a `.bicep` or ARM JSON file | required |
| `--params-file <path>` | Path to a `.bicepparam` file whose values should be applied during estimation | - |
| `--param <key=value>` | Override a parameter value from the command line | - |
| `--output <format>` | Output format: `table`, `json`, `csv`, or `markdown` | `table` |

Repeat `--param` as many times as needed. If you pass the same key more than once, the last value wins.

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

### Output Formats

#### `table`

Best for local terminal use. The table output includes:

- a title with the source file name
- the current date
- one row per priced resource
- an estimated monthly total row
- warning lines after the table

#### `json`

Best for CI, scripts, and downstream automation.

The JSON payload has this shape:

```json
{
	"lines": [
		{
			"resourceType": "Microsoft.Storage/storageAccounts",
			"resourceName": "mystorage",
			"pricingDetails": "Standard_LRS, Hot",
			"hourlyCost": 0.0123,
			"monthlyCost": 8.98
		}
	],
	"grandTotal": 8.98,
	"warnings": []
}
```

#### `csv`

Best when you want to import the result into spreadsheets or simple reporting pipelines.

- The first row is the header.
- Each priced resource becomes one row.
- The last row is a `TOTAL` row.

#### `markdown`

Best for pull request comments, workflow summaries, or docs.

- The output includes a heading and date.
- Resource costs are rendered as a markdown table.
- Warnings are appended as a bullet list when present.

### Behavior Details

- Unsupported resource types are skipped and returned as warnings in the output.
- Spot and low-priority pricing is excluded from the default pricing query set.
- Parameter values from `.bicepparam` files and repeatable `--param` overrides are applied during resource extraction.
- For ARM JSON input, parameter files are not layered on top of the template. Use a fully resolved ARM JSON file if you estimate compiled output directly.

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

### Browser Controls

When `bce docs` opens the interactive terminal browser:

- `Up` and `Down` move through the document list
- `Enter` opens the selected page
- `Esc` returns from a page to the document list
- `PageUp` and `PageDown` scroll faster inside a page
- `q` quits

### Behavior Details

- `bce docs` reads from the deployed docs site at `https://bicepcostestimator.net`, so it always shows the latest published content.
- When it runs in a terminal, it opens an interactive browser with arrow-key navigation.
- When output is redirected, `bce docs` prints the introduction page and exits.
- Redirected output is normalized for plain text, so ANSI styling is removed automatically.
- `bce docs /route` fetches the plain-text representation of that published page.
- `bce docs list` follows website sidebar order when navigation metadata is available.
- If the interactive browser cannot load the docs index, the command falls back to printing the introduction page.

## `bce cache info`

Show the current pricing cache size and entry count.

```bash
bce cache info
```

Typical output:

```text
Cache entries: 42
Cache size: 128.4 KB
```

## `bce cache clear`

Remove all cached pricing data.

```bash
bce cache clear
```

This prints a confirmation message after the cache directory is cleared.

## `bce lsp`

Start the Language Server Protocol server used by the VS Code extension.

```bash
bce lsp
```

This command is primarily an internal integration surface for the VS Code extension. It speaks JSON-RPC over standard input and output.

## Global Options

- `bce --help` shows command usage.
- `bce --version` prints the current CLI version.
- If the CLI can quickly reach the latest GitHub release metadata and a newer version exists, `bce --version` also prints an `Update available:` note.
