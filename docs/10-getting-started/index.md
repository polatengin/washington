---
title: Getting Started
sidebar_position: 10
---

# Getting Started

This guide walks you through installing the Washington CLI, published as `bce`, and running your first cost estimate.

## Prerequisites

- An Azure subscription (for pricing API access)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (for building from source) or use the pre-built binary
- GNU Make (for building from source)

## Installation

### Quick Install (Linux/macOS)

```bash
curl -sL https://bicepcostestimator.net/install.sh | bash
```

This installs `bce` to `~/.bce/bin` by default.

### From Source

```bash
git clone https://github.com/polatengin/washington.git
cd washington
make setup-cli
make build-cli
```

The built binary will be at `src/cli/bin/Release/net10.0/bce`.

## First Estimate

```bash
# Point bce at a Bicep file
bce estimate --file path/to/main.bicep
```

The output shows a table of resources with their estimated monthly costs.

## Next Steps

- [CLI Commands](/cli/commands) — full command reference
- [CLI Configuration](/cli/configuration) — configure defaults and output formats
- [VS Code Extension](/vscode-extension) — get estimates in your editor
- [GitHub Action](/github-action) — automate cost estimates in CI
