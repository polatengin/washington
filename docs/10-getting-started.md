title: Getting Started
description: Install the BCE CLI, run your first estimate, and understand the estimator's defaults.
sidebar_position: 10
---

# Getting Started

This guide walks you through installing the CLI, published as `bce`, and running your first cost estimate.

## Prerequisites

- For the published install script: Linux or macOS with a bash-compatible shell
- For the Docker wrapper: Docker
- For source builds: [.NET 10 SDK](https://dotnet.microsoft.com/download) and GNU Make
- Internet access so the estimator can query the public Azure Retail Prices API and, when needed, download a Bicep CLI release

You do not need to sign in to Azure or have an active Azure subscription just to run an estimate.

## Installation

### Published CLI (Linux/macOS)

```bash
curl -sL https://bicepcostestimator.net/install.sh | bash
```

The installer uses `~/.local/bin` on Linux, `/usr/local/bin` on Intel macOS, and `/opt/homebrew/bin` on Apple Silicon by default. Set `INSTALL_DIR` to override that choice. If the install directory is not on `PATH`, the installer prints the `export PATH=...` command to run.

### Docker Wrapper

If you prefer not to install the CLI binary directly, add this function to your shell profile:

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

Reload your shell, then use `bce` like a normal command.

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
bce estimate --file path/to/main.bicep
```

The output shows a table of resources with their estimated monthly costs.

Apply a `.bicepparam` file when your template depends on parameter values:

```bash
bce estimate --file path/to/main.bicep --params-file path/to/main.bicepparam
```

Choose a different output when you want to feed the result into automation or a report:

```bash
bce estimate --file path/to/main.bicep --output json
bce estimate --file path/to/main.arm.json --output markdown
```

## Runtime Defaults

- Pricing responses are cached for 24 hours under `~/.bicep-cost-estimator/cache`.
- If a resource location cannot be resolved from the template, the estimator falls back to `eastus`.
- Unsupported resources produce warnings instead of failing the whole estimate.

## Choose Your Next Step

- [CLI Commands](/cli/commands) for every command and flag
- [CLI Configuration](/cli/configuration) for compiler environment variables and runtime defaults
- [Playground](/playground) if you want to paste a self-contained template into the browser
- [VS Code Extension](/vscode-extension) if you want estimates while editing
- [GitHub Action](/github-action) if you want CI or pull request checks
- [Troubleshooting](/guides/troubleshooting) if your first estimate does not behave as expected
