---
title: Roadmap
sidebar_position: 70
---

# Roadmap

The following features are planned for future releases:

- **Pre-built CLI binaries** - Publish platform-specific binaries as GitHub releases for faster Action startup (no build step needed)
- **Multi-file / module-aware projects** - Support Bicep projects that span multiple files and use modules
- **Cost comparison between parameter sets** - Compare costs across different parameter sets (for example, `dev` vs `prod`)
- **Historical price tracking** - Detect cost changes over time as Azure pricing evolves
- **`diff` command** - Show cost delta vs current deployment (`bce estimate diff main.bicep`)
- **PR comment template customization** - Allow users to customize the GitHub Action PR comment format
- **Currency selection** - Add a `--currency` flag to the CLI and GitHub Action for non-USD currencies
- **Reserved Instances / Savings Plans** - Show RI and savings plan pricing alongside pay-as-you-go for comparison
- **SARIF output format** - Integrate cost warnings into GitHub Code Scanning and the security tab
- **Cost optimization suggestions** - Recommend cheaper SKUs, redundant resources, or right-sizing opportunities
- **Tag-based cost grouping** - Group and subtotal costs by Azure resource tags (team, project, environment)
- **Spot VM pricing** - Show spot and low-priority pricing alongside pay-as-you-go
- **Custom pricing overrides** - Support Enterprise Agreement and CSP pricing via user-provided rate cards
- **Annual / multi-year projections** - Show costs beyond monthly (quarterly, annual)
- **Bicep module registry support** - Resolve modules from Azure Container Registry and template specs
