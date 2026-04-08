SHELL := bash
.SHELLFLAGS := -eu -o pipefail -c

.DEFAULT_GOAL := help

.PHONY: help info setup-cli setup-extension setup-website clean build-cli build-extension build-website dev-website dev-website-ssh test-cli test-extension package-cli

help:
	@echo "Washington development targets"
	@echo
	@awk 'BEGIN {FS = ":.*## "} /^[a-zA-Z0-9_.-]+:.*## / {printf "%-24s %s\n", $$1, $$2; found = 1} END {if (!found) print "  (no documented targets found)"}' $(MAKEFILE_LIST) | sort
	@echo
	@echo "Examples:"
	@echo "  make setup-cli"
	@echo "  make build-cli"
	@echo "  make test-cli"
	@echo "  make build-extension"

info: ## Show tool versions and current paths
	@echo "Washington build information"
	@echo "  CLI project:    src/cli/washington.csproj"
	@echo "  CLI binary:     src/cli/bin/Release/net10.0/bce"
	@echo "  CLI tests:      tests/cli.tests/washington.tests.csproj"
	@echo "  Dotnet SDK:     $$(dotnet --version)"
	@echo "  Node.js:        $$(node --version 2>/dev/null || echo 'not found')"
	@echo "  npm:            $$(npm --version 2>/dev/null || echo 'not found')"

setup-cli: ## Restore .NET dependencies for the CLI and tests
	dotnet restore washington.slnx

setup-extension: ## Install VS Code extension dependencies
	npm ci --prefix src/vscode-extension


setup-website: ## Install website dependencies and docfind
	npm ci --prefix src/website
	@if command -v docfind >/dev/null 2>&1 || [ -x "$$HOME/.local/bin/docfind" ]; then \
		echo "docfind is already installed"; \
	else \
		curl -fsSL https://microsoft.github.io/docfind/install.sh | sh; \
	fi

clean: ## Remove generated outputs
	dotnet clean washington.slnx
	rm -rf src/vscode-extension/bin src/vscode-extension/dist src/vscode-extension/out src/website/build src/website/.docusaurus src/website/static/docfind src/website/static/text publish

build-cli: clean ## Build the BCE CLI
	dotnet build src/cli/washington.csproj --configuration Release

build-extension: clean ## Build the VS Code extension
	rm -rf src/vscode-extension/bin
	mkdir -p src/vscode-extension/bin
	cp -r src/cli/bin/Release/net10.0/. src/vscode-extension/bin/
	chmod +x src/vscode-extension/bin/bce || true
	npm --prefix src/vscode-extension run compile

build-website: clean ## Build the documentation website
	npm --prefix src/website run build

dev-website: ## Run the documentation website locally with live reload and curl support
	npm --prefix src/website run dev

dev-website-ssh: ## Run the website server locally with SSH docs enabled on port 2222
	npm --prefix src/website run prebuild
	cd src/website && PORT="$${PORT:-3000}" SSH_PORT="$${SSH_PORT:-2222}" node --import tsx server.mts

test-cli: build-cli ## Run the CLI test suite
	dotnet test tests/cli.tests/washington.tests.csproj --configuration Release

test-extension: build-extension ## Run the VS Code extension tests
	npm --prefix src/vscode-extension test

package-cli: build-cli ## Publish self-contained CLI binaries for supported runtimes
	rm -rf publish
	mkdir -p publish
	for runtime in win-x64 linux-x64 osx-x64 osx-arm64; do \
		dotnet publish src/cli/washington.csproj --configuration Release -p:PublishSingleFile=true --self-contained true -r "$$runtime" -o "publish/$$runtime"; \
	done
	cp "publish/win-x64/bce.exe" "publish/bce-win-x64.exe"
	cp "publish/linux-x64/bce" "publish/bce-linux-x64"
	cp "publish/osx-x64/bce" "publish/bce-osx-x64"
	cp "publish/osx-arm64/bce" "publish/bce-osx-arm64"
