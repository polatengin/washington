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
- **Automatic refresh** — estimates are refreshed when a `.bicep` file is opened, saved, or changed

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

## Current Behavior and Limitations

- The extension shells out to `bce lsp`; it does not implement pricing logic in TypeScript.
- Workspace estimation scans all `.bicep` files recursively under the current workspace root.
- The extension currently refreshes on open, save, and debounced change regardless of the `washington.estimateOnSave` setting.
- `washington.showStatusBar` is wired today. Other exposed settings, such as `washington.defaultRegion`, `washington.showCodeLens`, and `washington.cacheTtlHours`, are currently reserved and documented here so the user-facing contract is visible while implementation catches up.
- Estimates shown in the editor are based on the `.bicep` file path. Paired `.bicepparam` files are not yet selected automatically by the extension.
