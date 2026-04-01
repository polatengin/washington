---
title: CLI Commands
sidebar_position: 20
---

# CLI Commands

The Washington CLI provides commands for estimating Azure infrastructure costs from the terminal.

## `washington estimate`

Estimate the cost of an infrastructure-as-code file.

```bash
washington estimate <file> [options]
```

### Arguments

| Argument | Description |
|----------|-------------|
| `<file>` | Path to a Bicep, ARM JSON, or Terraform file |

### Options

| Option | Description |
|--------|-------------|
| `--output`, `-o` | Output format: `table`, `json`, `csv` (default: `table`) |
| `--currency` | Currency code (default: `USD`) |
| `--region` | Override Azure region |

### Examples

```bash
# Basic estimate
washington estimate main.bicep

# JSON output for CI pipelines
washington estimate main.bicep -o json

# Specify currency
washington estimate main.bicep --currency EUR
```

## `washington version`

Display the installed version.

```bash
washington version
```
