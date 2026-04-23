---
title: Troubleshooting
sidebar_position: 52
---

# Troubleshooting

This page covers the most common setup and estimation issues in Washington.

## `bce`: command not found

Install the published CLI or point directly at a local build:

```bash
curl -sL https://bicepcostestimator.net/install.sh | bash

# or from the repo root
./src/cli/bin/Release/net10.0/bce estimate --file ./tests/fixtures/simple-vm.bicep
```

When you estimate a `.bicep` file, Washington will use a `bicep` binary if one is available on `PATH`. Otherwise it downloads the pinned Bicep CLI release automatically.

To restore the repo's .NET dependencies from a fresh clone, run:

```bash
make setup-cli
```

## File not found or empty output

`bce estimate` requires `--file` and will emit an error if the path does not exist. For table output, a template with no supported resources can legitimately produce a nearly empty report.

Check:

- the file path you passed to `--file`
- whether the template contains resources after compilation
- whether those resource types are covered in [Supported Resources](/guides/supported-resources)

## `No pricing mapper ... skipped`

This warning means the resource extractor found a resource, but Washington does not yet have a mapper for that Azure resource type.

What to do:

- confirm whether the resource type appears in [Supported Resources](/guides/supported-resources)
- split the template into smaller test files to see which resource is missing coverage
- treat the report total as partial until a mapper exists for that resource

## Unexpected `eastus` pricing

If Washington cannot resolve a location expression, it falls back to `eastus`.

Common causes:

- the location comes from an ARM expression that is not fully resolved
- the value is indirect through parameters or variables that do not collapse to a simple string at extraction time

If regional accuracy matters, prefer explicit locations or verify the compiled ARM template first.

## Stale pricing or suspicious totals

Washington caches Azure Retail Prices API responses for 24 hours in:

```text
~/.bicep-cost-estimator/cache
```

Clear the cache and rerun:

```bash
bce cache clear
bce cache info
```

## `.bicepparam` file does not change the estimate

Washington applies values from `.bicepparam` files and `--param` overrides during resource extraction.

If the estimate still does not change, check:

- whether the parameter actually influences a priced property such as SKU, instance count, or location
- whether the parameter flows through a template expression the extractor can resolve
- whether you passed the correct `.bicepparam` file for the template you are estimating

## VS Code extension does not show costs

Check the following in order:

1. Open a `.bicep` file, since the extension activates on the Bicep language.
2. If you want to force a known `bce` binary, set the `CLI Path` setting from the Settings UI.
3. If you edit `settings.json` directly, use the key prefix suggested by VS Code IntelliSense for your installed extension build.
4. If `CLI Path` is empty, the extension will try the bundled binary, then a workspace `src/cli/washington.csproj`, then `bce` on your `PATH`.
5. If you expect automatic refresh, confirm `Estimate On Save` is enabled.
6. Run the extension's estimate command manually from the Command Palette to verify the LSP path is working.

## Workspace estimate looks larger than expected

The extension's workspace estimate walks every `.bicep` file recursively under the workspace root and aggregates all line items into one report. If your repo contains sample files, experiments, or duplicated environments, they will all be included.

## GitHub Action feels slow

The action currently builds the CLI from source inside the workflow before running the estimate. That keeps the action self-contained, but it also adds startup cost compared with downloading a prebuilt binary.

## Related Reading

- [CLI Commands](/cli/commands)
- [VS Code Extension](/vscode-extension)
- [How Estimates Work](/guides/how-estimates-work)
