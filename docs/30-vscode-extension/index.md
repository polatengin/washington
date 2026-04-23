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
- **Automatic refresh** - estimates can refresh when a `.bicep` file is opened, saved, or changed

## Commands

The extension adds commands for:

- estimating the active Bicep file
- estimating all Bicep files under the current workspace
- clearing the pricing cache

Open the Command Palette and search for `Bicep Cost Estimator` to find them.

## What The UI Shows

After an estimate runs, the extension can surface the report in several places:

- **CodeLens** above priced resources
- **Hover content** with cost details for the current resource
- **Status bar** with the current monthly total
- **Cost Breakdown** in the explorer

The `Cost Breakdown` view is more than a flat list. It shows:

- a total row for the current report
- one expandable row per priced resource
- a warnings node when the estimate returned warnings

When you expand a resource, the view shows its ARM type, pricing details, hourly cost, and monthly cost.

The status bar tooltip also summarizes the current total, resource count, and warning count.

## Typical Editor Flow

1. Open a `.bicep` file.
2. Wait for automatic estimation or trigger the estimate command manually.
3. Read CodeLens or hover details on the resources you care about.
4. Expand the `Cost Breakdown` view to inspect the whole report.
5. Clear the cache or rerun after edits if the result looks stale.

## Workspace Estimates

The workspace estimate command aggregates all `.bicep` files under the current workspace root into one report.

Use it when:

- your repository contains several deployable Bicep templates
- you want a combined monthly total for one infra folder or repo
- you want to find the highest-cost files and resources quickly from the tree view

## Configuration

Open VS Code settings and search for `Bicep Cost Estimator`.

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

## Practical Notes

- If automatic refresh is disabled, the extension still works, but estimation happens only when you run the command manually.
- If a file estimate returns warnings, those warnings appear in the breakdown view as a separate node instead of being hidden.
- The extension is best at single-file authoring feedback. For repeatable CI workflows or exported artifacts, prefer the CLI or GitHub Action.
