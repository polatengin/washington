---
slug: /
title: Introduction
description: Estimate Azure infrastructure costs from Bicep and ARM templates before you deploy.
sidebar_position: 0
---

# Bicep Cost Estimator

`Bicep Cost Estimator` (`bce`) estimates the monthly cost of Azure infrastructure before you deploy it. It works with Bicep and ARM JSON templates and ships as a CLI, a VS Code extension, a GitHub Action, and a browser playground.

## Choose Your Workflow

### CLI

Use the published `bce` command locally, in scripts, or in CI when you want direct control over files, parameter overrides, and output formats.

```bash
curl -sL https://bicepcostestimator.net/install.sh | bash
bce estimate --file main.bicep
```

See [Getting Started](/getting-started) and [CLI Commands](/cli/commands).

### VS Code Extension

Use the extension when you want estimates while authoring Bicep: CodeLens totals, hover details, a status bar total, and a cost breakdown view in the explorer.

```bash
code --install-extension enginpolat.bce
```

See [VS Code Extension](/vscode-extension).

### GitHub Action

Use the action when you want cost checks in pull requests or workflow gates based on the estimated monthly total.

```yaml
- id: cost
  uses: polatengin/washington@main
  with:
    file: infra/main.bicep
    output-format: json
```

See [GitHub Action](/github-action).

### Playground

Use the browser playground when you want to paste a self-contained template and inspect the result without installing anything locally.

See [Playground](/playground).

## Quick Start With The CLI

```bash
curl -sL https://bicepcostestimator.net/install.sh | bash
bce estimate --file main.bicep
```

Add a params file or choose a machine-readable format when you need a more realistic or automatable run:

```bash
bce estimate --file main.bicep --params-file main.bicepparam
bce estimate --file main.bicep --output json
```

Run via Docker instead if you do not want to install the CLI directly:

> ```bash
> bce() {
>   docker run --rm \
>     -v "$PWD:/work" \
>     -w /work \
>     --entrypoint /app/bin/bce \
>     ghcr.io/polatengin/washington:latest \
>     "$@"
> }
> ```
>
> Add that function to `~/.bashrc` or `~/.zshrc`, reload your shell, and then use `bce` like a normal command.

## What You Get

- Estimates from Bicep and ARM JSON templates
- Support for `.bicepparam` files and repeatable `--param key=value` overrides
- Output formats for terminal and automation: `table`, `json`, `csv`, and `markdown`
- Shared pricing behavior across the CLI, VS Code extension, GitHub Action, and browser playground
- Warning-based handling for unsupported resources so mixed templates still produce partial totals

## How It Works

1. Compile Bicep to ARM JSON when needed.
2. Extract Azure resources, locations, and pricing-relevant properties.
3. Map supported resource types to Azure Retail Prices API queries.
4. Aggregate the results into line items, warnings, and a grand total.

See [How Estimates Work](/guides/how-estimates-work) for the full pipeline.

## Good To Know

- Estimation uses the public Azure Retail Prices API. An Azure subscription sign-in is not required just to run an estimate.
- Pricing responses are cached locally for 24 hours under `~/.bicep-cost-estimator/cache`.
- If a resource location cannot be resolved, the estimator falls back to `eastus`.
- Unsupported resource types are reported as warnings instead of failing the whole run.

## Available Tools

| Tool | Description |
|------|-------------|
| [Playground](/playground) | Paste standalone Bicep into the browser and get an on-demand estimate |
| [CLI](/cli/commands) | Command-line interface for local and CI use, published as `bce` |
| [VS Code Extension](/vscode-extension) | Real-time cost estimates in your editor |
| [GitHub Action](/github-action) | Cost estimates on every pull request |

## Where To Go Next

- [Getting Started](/getting-started) for installation paths, first estimates, and runtime defaults
- [CLI Commands](/cli/commands) for command and flag reference
- [VS Code Extension](/vscode-extension) for editor workflows and settings
- [GitHub Action](/github-action) for CI and pull request automation
- [Supported Resources](/guides/supported-resources) for coverage details
- [Troubleshooting](/guides/troubleshooting) for setup, cache, and unsupported-resource issues
