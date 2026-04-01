---
title: VS Code Extension
sidebar_position: 30
---

# VS Code Extension

The Washington VS Code extension shows real-time Azure cost estimates directly in your editor as you write infrastructure-as-code. It resolves costs by invoking the `bce` CLI in LSP mode.

## Installation

Search for **Washington** in the VS Code Extensions marketplace, or install from the command line:

```bash
code --install-extension polatengin.washington
```

## Features

- **CodeLens annotations** — see estimated monthly costs above each resource
- **Hover details** — hover over a resource to see a detailed cost breakdown
- **Status bar totals** — view the current file's estimated monthly cost at a glance
- **Cost breakdown panel** — inspect per-resource totals in the explorer view
- **Estimate on save** — refresh costs automatically when you save a `.bicep` file

## Configuration

Open VS Code settings and search for `washington`:

| Setting | Description | Default |
|---------|-------------|---------|
| `washington.defaultRegion` | Default Azure region when not specified in a resource | `eastus` |
| `washington.cliPath` | Path to the `bce` CLI binary; auto-detected if empty | `""` |
| `washington.estimateOnSave` | Automatically estimate costs when saving `.bicep` files | `true` |
| `washington.showCodeLens` | Show cost annotations as CodeLens above resources | `true` |
| `washington.showStatusBar` | Show total estimated cost in the status bar | `true` |
| `washington.cacheTtlHours` | Pricing cache time-to-live in hours | `24` |

If `washington.cliPath` is empty, the extension first tries the bundled `bce` binary, then a workspace build, then `bce` on your `PATH`.
