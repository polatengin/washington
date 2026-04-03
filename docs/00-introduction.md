---
slug: /
title: Introduction
sidebar_position: 0
---

# Bicep Cost Estimator

`Bicep Cost Estimator` (_bce_) is an open-source tool that estimates the cost of your Azure infrastructure **before** you deploy it. It works with Bicep and ARM templates.

## Install the CLI

```bash
curl -sL https://bicepcostestimator.net/install.sh | bash
```

## Quick Start

```bash
# Estimate costs for a Bicep file
bce estimate --file main.bicep
```

## How It Works

1. Parse your infrastructure-as-code files
2. Resolve resource types, SKUs, and regions
3. Query Azure pricing APIs
4. Output a human-readable cost estimate

## Available Tools

| Tool | Description |
|------|-------------|
| [Playground](/playground) | Paste standalone Bicep into the browser and get an on-demand estimate |
| [CLI](/cli/commands) | Command-line interface for local and CI use, published as `bce` |
| [VS Code Extension](/vscode-extension) | Real-time cost estimates in your editor |
| [GitHub Action](/github-action) | Cost estimates on every pull request |

## Learn More

- [How Estimates Work](/guides/how-estimates-work) explains the compile, extract, price, and aggregate pipeline.
- [Supported Resources](/guides/supported-resources) lists the Azure resource families mapped today.
- [Troubleshooting](/guides/troubleshooting) covers unsupported resources, cache issues, region fallback, and extension setup.
- [Playground](/playground) gives you a quick browser-based estimate for pasted Bicep templates.
- [Getting Started](/getting-started) for detailed installation and usage instructions.
