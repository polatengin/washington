# Bicep Cost Estimator

Bicep Cost Estimator (_Washington_) estimates monthly Azure costs from Bicep and ARM templates before you deploy. The published CLI command is `bce`.

It currently ships as:

- a .NET CLI
- a VS Code extension backed by the CLI in LSP mode
- a GitHub Action for CI and pull request workflows
- a Docusaurus documentation site with browser and plain-text routes

## Quick Start

Install the published CLI:

```bash
curl -sL https://bicepcostestimator.net/install.sh | bash
bce estimate --file path/to/main.bicep
```

Build from source instead:

```bash
git clone https://github.com/polatengin/washington.git
cd washington
make setup-cli
make build-cli
./src/cli/bin/Release/net10.0/bce estimate --file ./tests/fixtures/simple-vm.bicep
```

## What It Does

- Compiles Bicep to ARM JSON using the embedded Bicep submodule
- Extracts nested resources, copy loops, and conditionally skipped resources
- Maps 87 Azure resource types to Azure Retail Prices API queries
- Queries pricing with pagination, retry, and a local 24-hour file cache
- Renders results as `table`, `json`, `csv`, or `markdown`

## Documentation

- [Introduction](https://bicepcostestimator.net/)
- [Getting Started](https://bicepcostestimator.net/getting-started)
- [CLI Commands](https://bicepcostestimator.net/cli/commands)
- [VS Code Extension](https://bicepcostestimator.net/vscode-extension)
- [GitHub Action](https://bicepcostestimator.net/github-action)
- [How Estimates Work](https://bicepcostestimator.net/guides/how-estimates-work)
- [Supported Resources](https://bicepcostestimator.net/guides/supported-resources)
- [Troubleshooting](https://bicepcostestimator.net/guides/troubleshooting)

## Current Limitations

- Unsupported resource types are skipped with a warning.
- Spot and low-priority prices are excluded from default estimates.
- The VS Code extension auto-refresh behavior is controlled by `washington.estimateOnSave`.

## Development

Common local workflows:

```bash
make setup-cli
make test-cli

make setup-extension
make build-extension

make setup-website
make build-website
```

The repository depends on the embedded `bicep` submodule, so use `make setup-cli` or `git submodule update --init --recursive` before building from a fresh clone.

## Roadmap

The following features are planned for future releases:

- **Pre-built CLI binaries** — Publish platform-specific binaries as GitHub releases for faster Action startup (no build step needed)
- **Multi-file / module-aware projects** — Support Bicep projects that span multiple files and use modules
- **Cost comparison between parameter sets** — Compare costs across different parameter sets (e.g. `dev` vs `prod`)
- **Historical price tracking** — Detect cost changes over time as Azure pricing evolves
- **`diff` command** — Show cost delta vs current deployment (`bce estimate diff main.bicep`)
- **PR comment template customization** — Allow users to customize the GitHub Action PR comment format
- **Currency selection** — Add `--currency` flag to the CLI and GitHub Action for non-USD currencies
- **Reserved Instances / Savings Plans** — Show RI and savings plan pricing alongside pay-as-you-go for comparison
- **SARIF output format** — Integrate cost warnings into GitHub Code Scanning / security tab
- **Cost optimization suggestions** — Recommend cheaper SKUs, redundant resources, or right-sizing opportunities
- **Tag-based cost grouping** — Group and subtotal costs by Azure resource tags (team, project, environment)
- **Spot VM pricing** — Show spot/low-priority pricing alongside pay-as-you-go
- **Custom pricing overrides** — Support Enterprise Agreement / CSP pricing via user-provided rate cards
- **Annual / multi-year projections** — Show costs beyond monthly (quarterly, annual)
- **Bicep module registry support** — Resolve modules from Azure Container Registry and template specs

## License

[MIT](LICENSE)
