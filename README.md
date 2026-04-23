# Bicep Cost Estimator

[![CodeQL](https://github.com/polatengin/washington/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/polatengin/washington/actions/workflows/github-code-scanning/codeql)
[![Deploy Website](https://github.com/polatengin/washington/actions/workflows/deploy-website.yml/badge.svg)](https://github.com/polatengin/washington/actions/workflows/deploy-website.yml)
[![Release BCE CLI](https://github.com/polatengin/washington/actions/workflows/release-cli.yml/badge.svg)](https://github.com/polatengin/washington/actions/workflows/release-cli.yml)
[![BCE CLI version](https://img.shields.io/github/v/release/polatengin/washington?display_name=tag&label=bce%20cli)](https://github.com/polatengin/washington/releases/latest)

`Bicep Cost Estimator` estimates monthly Azure costs from Bicep and ARM templates before you deploy. The published CLI command is `bce`.

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

The installer uses `~/.local/bin` on Linux, `/usr/local/bin` on Intel macOS, and `/opt/homebrew/bin` on Apple Silicon by default. Set `INSTALL_DIR` to override that choice. If the install directory is not on `PATH`, the installer prints the `export PATH=...` command to run.

Run via Docker instead, without installing the CLI binary locally:

```bash
bce() {
  docker run --rm \
    -v "$PWD:/work" \
    -w /work \
    --entrypoint /app/bin/bce \
    ghcr.io/polatengin/washington:latest \
    "$@"
}
```

Add that function to `~/.bashrc` or `~/.zshrc`, reload your shell, and then use `bce` like a normal command:

```bash
bce estimate --file ./main.bicep --output json
```

The wrapper works by starting the published website container, overriding the default startup command so it runs `/app/bin/bce` instead of the docs server, mounting your current working directory at `/work` so relative `--file` and `--params-file` paths keep working, and mounting `~/.bicep-cost-estimator` so the pricing cache persists across runs. Docker is the only local dependency; the image is pulled automatically on first use.

Build from source instead:

```bash
git clone https://github.com/polatengin/washington.git
cd washington
make setup-cli
make build-cli
./src/cli/bin/Release/net10.0/bce estimate --file ./tests/fixtures/simple-vm.bicep
```

## VS Code Extension

Install the published extension from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=EnginPolat.bce), or from the command line:

```bash
code --install-extension enginpolat.bce
```

Open a `.bicep` file after installation to get inline cost estimates, hover details, status bar totals, and workspace estimation commands. Full usage and settings are documented at [bicepcostestimator.net/vscode-extension](https://bicepcostestimator.net/vscode-extension).

## What It Does

- Compiles Bicep to ARM JSON through the Bicep CLI via the Azure.Bicep.RpcClient package
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

## Development

Common local workflows:

```bash
make setup-cli
make test-cli

make setup-extension
make build-extension

make setup-website
make build-website
make dev-website
```

`make setup-cli` restores the .NET dependencies needed for the CLI and tests. `bce` resolves the Bicep CLI at runtime through the `Azure.Bicep.RpcClient` nuget package.

## Roadmap

The roadmap lives in [docs/70-roadmap.md](docs/70-roadmap.md) and on the documentation site at [bicepcostestimator.net/roadmap](https://bicepcostestimator.net/roadmap).

## License

[MIT](LICENSE)
