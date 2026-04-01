---
slug: /
title: Introduction
sidebar_position: 0
---

# Washington - Azure Cost Estimator

Washington is an open-source tool that estimates the cost of your Azure infrastructure **before** you deploy it. Its CLI is published as `bce` and works with Bicep and ARM templates.

## How It Works

1. Parse your infrastructure-as-code files
2. Resolve resource types, SKUs, and regions
3. Query Azure pricing APIs
4. Output a human-readable cost estimate

## Available Tools

| Tool | Description |
|------|-------------|
| [CLI](/cli/commands) | Command-line interface for local and CI use, published as `bce` |
| [VS Code Extension](/vscode-extension) | Real-time cost estimates in your editor |
| [GitHub Action](/github-action) | Cost estimates on every pull request |

## Quick Start

```bash
# Install the CLI
curl -sL https://bicepcostestimator.net/install.sh | bash

# Estimate costs for a Bicep file
bce estimate --file main.bicep
```

See [Getting Started](/getting-started) for detailed installation and usage instructions.
