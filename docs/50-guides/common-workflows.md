---
title: Common Workflows
description: Use BCE effectively across the CLI, VS Code extension, GitHub Action, and terminal docs browser.
---

# Common Workflows

This page focuses on the most common end-to-end ways people use Bicep Cost Estimator in practice.

## Estimate A Local Bicep File

Use the CLI when you want a direct answer for one file on disk.

```bash
bce estimate --file infra/main.bicep
```

If the template depends on parameter values, include the matching `.bicepparam` file:

```bash
bce estimate --file infra/main.bicep --params-file infra/main.bicepparam
```

If you need to override one or two values without editing the parameter file, add repeatable `--param` flags:

```bash
bce estimate \
  --file infra/main.bicep \
  --params-file infra/main.bicepparam \
  --param env=prod \
  --param location=westeurope
```

## Produce Output For Automation

Switch the output format depending on where the estimate needs to go next.

```bash
# Machine-readable output for scripts
bce estimate --file infra/main.bicep --output json

# Spreadsheet-friendly rows
bce estimate --file infra/main.bicep --output csv

# Markdown for PR summaries or reports
bce estimate --file infra/main.bicep --output markdown
```

Use `json` when another tool needs to read `lines`, `grandTotal`, or `warnings`. Use `markdown` when you want a ready-to-paste table.

## Estimate Compiled ARM JSON

If you already have ARM JSON, estimate it directly instead of compiling Bicep again:

```bash
bce estimate --file infra/main.arm.json
```

This is useful in workflows that already produce ARM JSON as an artifact.

## Browse The Published Docs From The Terminal

The CLI includes a docs browser that reads the published docs site in plain text.

```bash
# Open the interactive browser
bce docs

# List published routes in website sidebar order
bce docs list

# Print a single page
bce docs /getting-started

# Search titles and summaries
bce docs search cache
```

In the interactive browser, use `Up` and `Down` to move, `Enter` to open, `Esc` to go back, `PageUp` and `PageDown` to scroll faster, and `q` to quit.

## Inspect Costs Inside VS Code

Use the extension when you want feedback while editing Bicep instead of after the fact.

Typical flow:

1. Open a `.bicep` file in VS Code.
2. Let automatic refresh run, or trigger the estimate command manually from the Command Palette.
3. Check CodeLens totals above resources and hover for details.
4. Open the `Cost Breakdown` view in the explorer to inspect the report tree.

The tree view shows:

- a root total for the current report
- one item per priced resource
- a warnings node when unsupported or unresolved resources were skipped

When you expand a resource node, the extension shows its resource type, pricing details, hourly cost, and monthly cost.

## Estimate An Entire Workspace In VS Code

Use the workspace estimate command when you want one combined report for everything under the current repo root.

Current behavior:

- the extension scans all `.bicep` files recursively under the workspace root
- the result is aggregated into one report
- the tree view and status bar update from that aggregated report

This is useful for monorepos or infra folders that contain several deployable templates.

## Gate A Pull Request On Cost

Use the GitHub Action when you want cost checks in CI.

```yaml
- id: cost
  uses: polatengin/washington@main
  with:
    file: infra/main.bicep
    params-file: infra/prod.bicepparam
    output-format: markdown
    fail-on-threshold: 1000

- name: Append estimate summary
  run: |
    echo '${{ steps.cost.outputs.estimation-result }}' >> "$GITHUB_STEP_SUMMARY"
```

This gives you a visible workflow summary and fails the job when the monthly total is above the threshold.

## Investigate Partial Or Suspicious Totals

When a total looks incomplete or stale, check in this order:

1. Read the warnings section first. Unsupported resource types are surfaced there instead of failing the whole estimate.
2. Confirm the resource type appears in [Supported Resources](/guides/supported-resources).
3. Prefer explicit `location`, SKU, and sizing values when expression resolution is ambiguous.
4. Clear the pricing cache and rerun if the numbers look stale.

```bash
bce cache info
bce cache clear
```

## Related Reading

- [CLI Commands](/cli/commands)
- [VS Code Extension](/vscode-extension)
- [GitHub Action](/github-action)
- [Troubleshooting](/guides/troubleshooting)
