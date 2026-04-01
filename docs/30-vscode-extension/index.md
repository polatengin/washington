---
title: VS Code Extension
sidebar_position: 30
---

# VS Code Extension

The Washington VS Code extension shows real-time Azure cost estimates directly in your editor as you write infrastructure-as-code.

## Installation

Search for **Washington** in the VS Code Extensions marketplace, or install from the command line:

```bash
code --install-extension polatengin.washington
```

## Features

- **Inline cost estimates** — see estimated monthly costs as CodeLens annotations above each resource
- **Hover details** — hover over a resource to see a detailed cost breakdown
- **Status bar** — total estimated cost shown in the VS Code status bar
- **Multi-file support** — works with Bicep, ARM JSON, and Terraform files

## Configuration

Open VS Code settings and search for `washington`:

| Setting | Description | Default |
|---------|-------------|---------|
| `washington.currency` | Display currency | `USD` |
| `washington.region` | Override region | (from file) |
| `washington.enabled` | Enable/disable estimates | `true` |
