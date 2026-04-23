---
title: VS Code Extension
description: Use BCE inside VS Code for inline estimates, hover details, and workspace cost checks.
sidebar_position: 30
---

# VS Code Extension

The Bicep Cost Estimator VS Code extension shows real-time Azure cost estimates directly in your editor as you write infrastructure-as-code. It resolves costs by invoking the `bce` CLI in LSP mode.

## Installation

Install from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=EnginPolat.bce), or from the command line:

```bash
code --install-extension enginpolat.bce
```

## Features

- **CodeLens annotations** - see estimated monthly costs above each resource
- **Hover details** - hover over a resource to see a detailed cost breakdown
- **Status bar totals** - view the current file's estimated monthly cost at a glance
- **Cost breakdown panel** - inspect per-resource totals in the explorer view
- **Automatic refresh** - when `bce.estimateOnSave` is enabled, estimates are refreshed when a `.bicep` file is opened, saved, or changed

## Configuration

Open VS Code settings and search for `bce`:

| Setting | Description | Default |
|---------|-------------|---------|
| `bce.defaultRegion` | Default Azure region when not specified in a resource | `eastus` |
| `bce.cliPath` | Path to the `bce` CLI binary; auto-detected if empty | `""` |
| `bce.estimateOnSave` | Automatically refresh estimates for open `.bicep` files | `true` |
| `bce.showCodeLens` | Show cost annotations as CodeLens above resources | `true` |
| `bce.showStatusBar` | Show total estimated cost in the status bar | `true` |
| `bce.cacheTtlHours` | Pricing cache time-to-live in hours | `24` |

If `bce.cliPath` is empty, the extension first tries the bundled `bce` binary, then a workspace build, then `bce` on your `PATH`.

For a full settings reference, see [Extension Settings](/vscode-extension/settings).

## Current Behavior and Limitations

- The extension shells out to `bce lsp`; it does not implement pricing logic in TypeScript.
- Workspace estimation scans all `.bicep` files recursively under the current workspace root.
- `bce.defaultRegion`, `bce.estimateOnSave`, `bce.showCodeLens`, `bce.showStatusBar`, and `bce.cacheTtlHours` are applied by the language server on startup.
- If a matching `.bicepparam` file in the same directory targets the active `.bicep` file, the extension applies its parameter values automatically during estimation.
