---
title: Roadmap
sidebar_position: 70
---

# Roadmap

The following features are planned for future releases:

- **Pre-built CLI binaries** - Publish platform-specific binaries as GitHub releases for faster Action startup (no build step needed)
- **Multi-file / module-aware projects** - Support Bicep projects that span multiple files and use modules
- **Website playground support for modules and params files** - Let the browser playground load local modules and separate parameter files instead of requiring a single self-contained template
- **Project configuration file** - Support a `.bce.json` or `.bce.yaml` file for shared defaults like region, output format, cache TTL, and estimation settings
- **Cost comparison between parameter sets** - Compare costs across different parameter sets (for example, `dev` vs `prod`)
- **Workload profiles / usage assumptions** - Let users choose or define estimation profiles for things like storage growth, transaction volume, and baseline throughput
- **Regional cost comparison** - Compare the same deployment across Azure regions to highlight pricing deltas and default-region assumptions
- **Historical price tracking** - Detect cost changes over time as Azure pricing evolves
- **`diff` command** - Show cost delta vs current deployment (`bce estimate diff main.bicep`)
- **Rich terminal output** - Add ANSI colors, styled tables, and clearer warning/success formatting in CLI output, with `--no-color` and plain-text fallbacks for CI and redirected output
- **PR comment template customization** - Allow users to customize the GitHub Action PR comment format
- **Currency selection** - Add a `--currency` flag to the CLI and GitHub Action for non-USD currencies
- **Reserved Instances / Savings Plans** - Show RI and savings plan pricing alongside pay-as-you-go for comparison
- **SARIF output format** - Integrate cost warnings into GitHub Code Scanning and the security tab
- **Cost optimization suggestions** - Recommend cheaper SKUs, redundant resources, or right-sizing opportunities
- **Group-by and top views** - Add breakdowns by service, resource type, location, or module, and highlight the biggest cost drivers
- **Tag-based cost grouping** - Group and subtotal costs by Azure resource tags (team, project, environment)
- **HTML report output** - Export a self-contained report with charts, subtotals, and collapsible resource sections for sharing in PR artifacts
- **Source trace / provenance in output** - Show which file, module, or declaration produced each estimated resource so users can trace costs back to the source
- **Spot VM pricing** - Show spot and low-priority pricing alongside pay-as-you-go
- **Baseline snapshots** - Save an approved estimate and compare future runs against it, even when there is no live deployment to diff against
- **Custom pricing overrides** - Support Enterprise Agreement and CSP pricing via user-provided rate cards
- **Annual / multi-year projections** - Show costs beyond monthly (quarterly, annual)
- **Bicep module registry support** - Resolve modules from Azure Container Registry and template specs
