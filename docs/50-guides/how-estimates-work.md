---
title: How Estimates Work
sidebar_position: 50
---

# How Estimates Work

Washington is intentionally thin at the edges. The CLI, VS Code extension, and GitHub Action all converge on the same estimation pipeline.

## Pipeline Overview

1. **Compile** the source file.
   For `.bicep` input, Washington invokes the embedded Bicep compiler from the repository submodule and produces an ARM JSON template.
2. **Extract** Azure resources from the resulting template.
   The extractor walks the `resources` array, handles nested resources, skips `condition: false`, expands simple copy loops, and resolves a limited set of ARM expressions.
3. **Map** each resource type to pricing queries.
   Each supported resource type has a mapper that converts resource metadata into Azure Retail Prices API filters.
4. **Query** Azure pricing.
   Washington calls the Azure Retail Prices API, follows paginated results, retries transient HTTP failures, and stores results in a local file cache.
5. **Aggregate** the report.
   Mapped resources become `lines`, skipped resources become warnings, and the CLI renders the final report as table, JSON, CSV, or markdown.

## Surface-Specific Flow

### CLI

The CLI is the canonical entry point. `bce estimate` can read either a `.bicep` file or a precompiled ARM JSON template, and it can layer in values from a `.bicepparam` file plus repeatable `--param` overrides.

### VS Code Extension

The extension launches `bce lsp` and talks to it over JSON-RPC. CodeLens, hover, the tree view, status bar, and workspace estimation all come from the same server-side report generation. When a matching `.bicepparam` file in the same directory targets the active `.bicep` file, the extension applies those parameter values automatically.

### GitHub Action

The action builds the CLI from source inside the workflow, runs `bce estimate`, and exposes the result as workflow outputs. Optional base-file inputs let you calculate cost deltas in pull request workflows.

## Pricing Source

Washington uses the Azure Retail Prices API. The current implementation:

- follows `NextPageLink` pagination until all records are collected
- retries transient HTTP failures with exponential backoff
- filters out spot and low-priority pricing from the default result set
- caches query results locally for 24 hours under `~/.bicep-cost-estimator/cache`

## Resource Extraction Details

The extractor does a pragmatic amount of ARM expression handling rather than trying to fully evaluate every template expression.

- Simple parameter default expressions such as `parameters('location')` can be resolved.
- If a location expression cannot be resolved, Washington falls back to `eastus`.
- Unsupported or unresolved pricing paths do not fail the whole estimate. They are surfaced as warnings instead.

## Current Limitations

- Workspace estimation in the VS Code extension scans every `.bicep` file under the workspace root and aggregates the totals into a single report.
- Unsupported Azure resource types are skipped with a warning rather than partially estimated.

## Where To Go Next

- [Supported Resources](/guides/supported-resources)
- [Troubleshooting](/guides/troubleshooting)
- [CLI Commands](/cli/commands)
