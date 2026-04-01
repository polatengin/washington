# Contributing

We welcome contributions to Washington! This guide explains how to set up a development environment and submit changes.

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (for website development)

### Clone and Build

```bash
git clone https://github.com/polatengin/washington.git
cd washington

# Build the CLI
dotnet build src/cli/cli.csproj

# Run tests
dotnet test tests/cli.tests/
```

### Website Development

```bash
cd src/website
npm install
npm start
```

This starts a local development server at `http://localhost:3000`.

## Submitting Changes

1. Fork the repository
2. Create a feature branch: `git checkout -b my-feature`
3. Make your changes and add tests
4. Run the test suite: `dotnet test`
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
