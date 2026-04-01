# Azure Deployments Cost Estimator

_Azure Deployments Cost Estimator_ (Washington) is a **FinOps** solution that estimates monthly Azure costs from Bicep files — before you deploy.

It ships as a **CLI tool**, a **VS Code extension**, and a **GitHub Action**, giving you cost visibility across local development, code review, and CI/CD pipelines.

## Key Concepts

- **Visibility** — Understand who is spending what in the cloud
- **Accountability** — Encourage teams to take responsibility for their usage
- **Optimization** — Find ways to reduce waste and save costs
- **Collaboration** — Finance, engineering, and product teams work together

---

## Features at a Glance

| Capability | CLI | VS Code Extension | GitHub Action |
| --- | :---: | :---: | :---: |
| Estimate costs from `.bicep` files | ✅ | ✅ | ✅ |
| Multiple output formats (table, JSON, CSV, markdown) | ✅ | — | ✅ |
| Inline cost annotations (CodeLens) | — | ✅ | — |
| Hover cost breakdowns | — | ✅ | — |
| Status bar total cost | — | ✅ | — |
| Sidebar cost breakdown panel | — | ✅ | — |
| Delta cost comparison (current vs base branch) | — | — | ✅ |
| Cost threshold failure gate | — | — | ✅ |
| Pricing cache (24h TTL, file-based) | ✅ | ✅ | ✅ |
| Parameter-aware (`.bicepparam` files) | ✅ | ✅ | ✅ |

---

## Supported Azure Resource Types (56)

| Category | Resource Types |
| --- | --- |
| **Compute** | Virtual Machines, Virtual Machine Scale Sets, Managed Disks, Batch Accounts, Spring Apps |
| **Containers** | AKS Managed Clusters, Container Registry, Container Apps, Container App Environments, Container Instances |
| **App Services** | App Service Plans, Function Apps, Static Web Apps |
| **Storage** | Storage Accounts |
| **Databases** | SQL Database, SQL Elastic Pools, SQL Managed Instances, Cosmos DB, PostgreSQL Flexible Servers, MySQL Flexible Servers, MariaDB Servers |
| **Networking** | Public IP Addresses, Application Gateways, Load Balancers, Virtual Network Gateways, Azure Firewall, Private Endpoints, NAT Gateways, Virtual Networks, Network Interfaces, Private DNS Zones, Traffic Manager, Bastion Hosts, DDoS Protection Plans, ExpressRoute Circuits, Front Door |
| **Security** | Key Vault |
| **Messaging** | Event Hub, Service Bus, Event Grid, Notification Hubs |
| **AI / ML** | Cognitive Services, Machine Learning Workspaces, Azure AI Search |
| **Monitoring** | Log Analytics Workspaces, Application Insights |
| **Integration** | API Management, Logic Apps, Data Factory |
| **Analytics** | Databricks Workspaces, Synapse Workspaces |
| **Caching** | Azure Cache for Redis |
| **Real-time** | SignalR Service |
| **IoT** | IoT Hub |
| **Config** | App Configuration |
| **Automation** | Automation Accounts |

Unmapped resource types produce a warning: `⚠ No pricing mapper for Microsoft.Xyz/abc — skipped`.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Clone the Repository

```bash
git clone https://github.com/polatengin/washington.git
cd washington
git submodule update --init --recursive
```

### Build

```bash
dotnet build src/cli/washington.csproj
```

---

## CLI

### Estimate Command

```bash
# Basic estimate
dotnet run --project ./src/cli/washington.csproj -- estimate --file main.bicep

# With parameters file
dotnet run --project ./src/cli/washington.csproj -- estimate --file main.bicep --params-file params.bicepparam

# Override parameter values
dotnet run --project ./src/cli/washington.csproj -- estimate --file main.bicep --param vmSize=Standard_D4s_v3 --param env=prod

# Choose output format
dotnet run --project ./src/cli/washington.csproj -- estimate --file main.bicep --output json
```

**Options:**

| Flag | Description | Default |
| --- | --- | --- |
| `--file <path>` | Path to `.bicep` or ARM JSON file | *(required)* |
| `--params-file <path>` | Path to `.bicepparam` file | — |
| `--param <key=value>` | Override a parameter value (repeatable) | — |
| `--output <format>` | `table`, `json`, `csv`, or `markdown` | `table` |

### Output Formats

- **`table`** — Human-readable ASCII table with resource names, types, pricing details, and monthly costs
- **`json`** — Machine-readable JSON with `lines`, `grandTotal`, and `warnings`
- **`csv`** — RFC 4180 CSV with headers and a TOTAL row
- **`markdown`** — GitHub-flavored markdown table

### Sample Commands

```bash
# 1. Estimate a single VM (default table output)
dotnet run --project ./src/cli/washington.csproj -- estimate --file ./tests/fixtures/simple-vm.bicep

# 2. Estimate all resources with a params file, output as markdown
dotnet run --project ./src/cli/washington.csproj -- estimate --file ./tests/fixtures/all.bicep --params-file ./tests/fixtures/all.bicepparam --output markdown

# 3. Estimate an AKS cluster, output as JSON
dotnet run --project ./src/cli/washington.csproj -- estimate --file ./tests/fixtures/aks.bicep --output json

# 4. Estimate AKS + VM together, output as CSV
dotnet run --project ./src/cli/washington.csproj -- estimate --file ./tests/fixtures/aks-vm.bicep --output csv

# 5. Estimate from a pre-compiled ARM template
dotnet run --project ./src/cli/washington.csproj -- estimate --file ./tests/fixtures/multi-resource.arm.json --output table
```

### Cache Management

```bash
# View cache statistics (entry count and size)
dotnet run --project ./src/cli/washington.csproj -- cache info

# Clear all cached pricing data
dotnet run --project ./src/cli/washington.csproj -- cache clear
```

Cache is stored at `~/.bicep-cost-estimator/cache/` with a default 24-hour TTL.

### LSP Server Mode

```bash
dotnet run --project ./src/cli/washington.csproj -- lsp
```

Starts a Language Server Protocol server over stdin/stdout (JSON-RPC). Used by editors for real-time cost estimation.

---

## VS Code Extension

The extension activates on `.bicep` files and communicates with the CLI in LSP mode — **zero logic duplication**.

### Editor Features

- **CodeLens** — Inline cost annotations (`💰 $XX.XX/mo`) above each resource declaration
- **Hover** — Detailed cost breakdown table on hover over resources
- **Status Bar** — Grand total estimated cost for the active file
- **Cost Breakdown Panel** — Explorer sidebar TreeView listing all resources with costs
- **Auto-estimate on save** — Re-estimates costs every time a `.bicep` file is saved

### Commands

| Command | Description |
| --- | --- |
| `Washington: Estimate File Cost` | Estimate cost of the current `.bicep` file |
| `Washington: Estimate Workspace Cost` | Estimate all `.bicep` files in the workspace |
| `Washington: Clear Pricing Cache` | Clear the local pricing cache |

### Settings

| Setting | Description | Default |
| --- | --- | --- |
| `washington.defaultRegion` | Default Azure region | `eastus` |
| `washington.cliPath` | Path to CLI binary (auto-detected if empty) | `""` |
| `washington.estimateOnSave` | Auto-estimate on save | `true` |
| `washington.showCodeLens` | Show CodeLens cost annotations | `true` |
| `washington.showStatusBar` | Show total cost in status bar | `true` |
| `washington.cacheTtlHours` | Pricing cache TTL in hours | `24` |

---

## GitHub Action

Integrate cost estimation into your CI/CD pipeline and PR review workflow.

### Usage

```yaml
- name: Estimate Azure Costs
  id: cost
  uses: polatengin/washington@main
  with:
    file: infra/main.bicep
    params-file: infra/main.bicepparam
    base-file: base-branch/infra/main.bicep        # optional: enables delta comparison
    base-params-file: base-branch/infra/main.bicepparam
    output-format: json
    fail-on-threshold: 1000                         # optional: fail if total > $1000/month
```

### Inputs

| Input | Description | Required | Default |
| --- | --- | :---: | --- |
| `file` | Path to `.bicep` file | ✅ | — |
| `params-file` | Path to `.bicepparam` file | — | — |
| `base-file` | Base branch `.bicep` file (enables delta comparison) | — | — |
| `base-params-file` | Base branch `.bicepparam` file | — | — |
| `output-format` | `json`, `table`, or `markdown` | — | `json` |
| `fail-on-threshold` | Fail if estimated monthly cost exceeds this value | — | — |

### Outputs

| Output | Description |
| --- | --- |
| `estimation-result` | Full JSON cost estimation result |
| `total-cost` | Estimated monthly total (numeric) |
| `base-cost` | Base branch cost (numeric, `0` if no base file) |
| `delta-cost` | Monthly cost delta (current − base) |

---

## Architecture

```text
┌─────────────┐     ┌──────────────────┐     ┌────────────────┐     ┌──────────────┐
│ .bicep file │────▶│ Bicep Library    │────▶│ ARM JSON      │────▶│ Resource     │
│             │     │ (git submodule)  │     │ Template       │     │ Extractor    │
└─────────────┘     └──────────────────┘     └────────────────┘     └──────┬───────┘
                                                                           │
                                                                           ▼
┌──────────────┐     ┌──────────────┐     ┌────────────────┐     ┌──────────────┐
│ Cost Report  │◀────│ Cost         │◀────│ Pricing API   │◀────│ Resource     │
│ (table/json/ │     │ Aggregator   │     │ Client + Cache │     │ Cost Mappers │
│  csv/md)     │     └──────────────┘     └────────────────┘     └──────────────┘
└──────────────┘
```

- **Bicep Library** — Compiled via git submodule (no external Bicep CLI needed)
- **Resource Extractor** — Parses ARM JSON into resource descriptors; handles nested resources, copy loops, and conditional resources
- **Resource Cost Mappers** — Per-resource-type mappers translate descriptors into Azure Retail Prices API queries
- **Pricing API Client** — Queries `https://prices.azure.com/api/retail/prices` with automatic pagination, retry with exponential backoff, and file-based caching
- **Cost Aggregator** — Combines mapper results into a structured `CostReport`

---

## Pricing Data

Cost data comes from the [Azure Retail Prices API](https://learn.microsoft.com/en-us/rest/api/cost-management/retail-prices/azure-retail-prices) — **free, no authentication required**. Spot and low-priority pricing is excluded by default.

---

## Running Tests

```bash
dotnet test tests/cli.tests/
```

Tests cover resource extraction, cost mappers, output formatters, and full end-to-end estimation pipelines.

---

## Roadmap

The following features are planned for future releases:

- **Pre-built CLI binaries** — Publish platform-specific binaries as GitHub releases for faster Action startup (no build step needed)
- **Multi-file / module-aware projects** — Support Bicep projects that span multiple files and use modules
- **Cost comparison between parameter sets** — Compare costs across different parameter sets (e.g. `dev` vs `prod`)
- **Historical price tracking** — Detect cost changes over time as Azure pricing evolves
- **`diff` command** — Show cost delta vs current deployment (`washington estimate diff main.bicep`)
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

---

## License

[MIT](LICENSE)
