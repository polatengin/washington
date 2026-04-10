---
title: Roadmap
sidebar_position: 70
hide_table_of_contents: true
---

# Roadmap

The following features are planned for future releases:

- **Pre-built CLI binaries** - Publish platform-specific binaries as GitHub releases for faster Action startup (no build step needed)
- **Multi-file / module-aware projects** - Support Bicep projects that span multiple files and use modules
- **Website playground support for modules and params files** - Let the browser playground load local modules and separate parameter files instead of requiring a single self-contained template
- **Website playground file uploads** - Allow users to upload Bicep, ARM, and parameter files in the browser playground and run cost estimation without pasting template contents manually
- **Website playground compare mode** - Run two templates or two parameter sets side by side in the browser and show total and per-resource cost deltas
- **Generated CLI reference** - Generate command, option, and help output docs directly from the CLI to prevent the docs from drifting from the implementation
- **Task-based recipe library** - Add end-to-end guides for common workflows like estimating a Bicep app, comparing dev vs prod, validating CI budgets, and debugging missing prices
- **Interactive docs search improvements** - Improve the docs search with filters, highlighted matches, and better ranking so users can find commands, guides, and resource support pages faster
- **Grouped and navigable cost breakdown view** - Let users group the VS Code extension tree by service, resource type, file, or warning status, and click items to jump to the corresponding resource declaration
- **Live tree and status bar sync** - Keep the VS Code extension cost breakdown panel and status bar automatically in sync with LSP updates for the active file instead of relying mainly on manual commands
- **Parameter file and override picker** - Add a VS Code command or UI to choose a matching `.bicepparam` file, switch between parameter sets, and apply temporary overrides from inside the editor
- **Cost-aware code actions** - Offer VS Code quick fixes or suggestions when the estimate is incomplete, such as adding explicit locations, checking unsupported resource types, or opening related docs
- **Rich estimate panel** - Add a dedicated VS Code webview for charts, top cost drivers, grouped subtotals, warnings, and copy/export actions for the current estimate
- **Compare mode in the editor** - Compare the current file against another params file, a saved baseline, or another branch/workspace file directly inside VS Code
- **Docs and Learn links from editor results** - Let users open supported-resources docs or Microsoft Learn pages from the VS Code tree view, hover, or CodeLens
- **Remote environment support hardening** - Improve VS Code extension behavior in WSL, dev containers, and SSH remotes so CLI resolution and bundled binaries work predictably across environments
- **Cross-project test coverage expansion** - Increase automated test coverage across the CLI, VS Code extension, website, and GitHub Action to catch regressions earlier
- **Expanded fixture library** - Add more real-world Bicep, ARM, params, and module fixtures that cover additional Azure services, edge cases, and larger project layouts
- **Fixture-backed regression tests** - Build more tests around shared fixtures so pricing logic, extraction behavior, and output formatting stay stable as support expands
- **CLI integration and snapshot tests** - Add broader end-to-end CLI tests for commands, output formats, warnings, parameter handling, and comparison workflows
- **VS Code extension integration tests** - Expand tests for LSP startup, commands, CodeLens, tree views, status bar updates, and editor workflows
- **Website and playground test coverage** - Add automated tests for docs search, playground submissions, estimate rendering, upload flows, and browser-side error handling
- **Coverage thresholds and CI quality gates** - Enforce minimum coverage and required test suites in CI before shipping changes to pricing, docs, playground, or extension features
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
- **Resource breakdown charts in the playground** - Add service-level and type-level visual summaries above the raw estimate table
- **HTML report output** - Export a self-contained report with charts, subtotals, and collapsible resource sections for sharing in PR artifacts
- **Deep links from estimates to Microsoft Learn** - Let estimate result rows open official Microsoft Learn documentation for the corresponding Azure resource type
- **Source trace / provenance in output** - Show which file, module, or declaration produced each estimated resource so users can trace costs back to the source
- **Spot VM pricing** - Show spot and low-priority pricing alongside pay-as-you-go
- **Baseline snapshots** - Save an approved estimate and compare future runs against it, even when there is no live deployment to diff against
- **Custom pricing overrides** - Support Enterprise Agreement and CSP pricing via user-provided rate cards
- **Annual / multi-year projections** - Show costs beyond monthly (quarterly, annual)
- **Bicep module registry support** - Resolve modules from Azure Container Registry and template specs
