---
title: Getting Started
sidebar_position: 10
---

# Getting Started

This guide walks you through installing Washington and running your first cost estimate.

## Prerequisites

- An Azure subscription (for pricing API access)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (for building from source) or use the pre-built binary

## Installation

### Quick Install (Linux/macOS)

```bash
curl -sL https://bicepcostestimate.net/install.sh | bash
```

### From Source

```bash
git clone https://github.com/polatengin/washington.git
cd washington
dotnet build src/cli/cli.csproj
```

The built binary will be at `src/cli/bin/Debug/net10.0/cli`.

## First Estimate

```bash
# Point Washington at a Bicep file
washington estimate path/to/main.bicep
```

The output shows a table of resources with their estimated monthly costs.

## Next Steps

- [CLI Commands](/cli/commands) — full command reference
- [CLI Configuration](/cli/configuration) — configure defaults and output formats
- [VS Code Extension](/vscode-extension) — get estimates in your editor
- [GitHub Action](/github-action) — automate cost estimates in CI
