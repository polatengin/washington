---
title: Configuration
sidebar_position: 21
---

# CLI Configuration

Washington CLI can be configured via command-line options or a configuration file.

## Configuration File

Create a `.washington.json` file in your project root:

```json
{
  "defaultCurrency": "USD",
  "defaultRegion": "eastus2",
  "outputFormat": "table"
}
```

## Environment Variables

| Variable | Description |
|----------|-------------|
| `WASHINGTON_CURRENCY` | Default currency code |
| `WASHINGTON_REGION` | Default Azure region |
| `WASHINGTON_OUTPUT` | Default output format |

## Precedence

1. Command-line flags (highest priority)
2. Environment variables
3. `.washington.json` configuration file
4. Built-in defaults
