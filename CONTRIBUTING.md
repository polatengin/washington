# Contributing

We welcome contributions to Washington! This guide explains how to set up a development environment and submit changes.

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [GNU Make](https://www.gnu.org/software/make/)
- [Docker](https://www.docker.com/) (for website development)

### Clone and Build

```bash
git clone https://github.com/polatengin/washington.git
cd washington
make setup-cli
make build-cli
make test-cli
```

If you're working on the VS Code extension or documentation website, install those dependencies explicitly:

```bash
make setup-extension
make setup-website
```

### Website Development

```bash
make setup-website
make dev-website
```

This starts the local docs stack at `http://localhost:3000` through the Express server, proxies browser traffic to Docusaurus with live reload, and serves the curl/plain-text pages. If you also want the playground API locally, build the CLI first with `make build-cli`.

## Submitting Changes

1. Fork the repository
2. Create a feature branch: `git checkout -b my-feature`
3. Make your changes and add tests
4. Run `make test-cli` and, for extension changes, `make test-extension`
5. Submit a pull request

## Project Structure

| Directory | Description |
|-----------|-------------|
| `src/cli/` | .NET CLI application |
| `src/vscode-extension/` | VS Code extension |
| `src/website/` | Documentation website |
| `tests/` | Test projects |
| `docs/` | Documentation content (Markdown) |
| `infra/` | Azure infrastructure (Bicep) |
