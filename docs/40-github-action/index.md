---
title: GitHub Action
sidebar_position: 40
---

# GitHub Action

The Washington GitHub Action builds and runs the `bce` CLI in CI so you can estimate Azure costs directly from your workflow.

## Usage

Add to your workflow file (`.github/workflows/cost-estimate.yml`):

```yaml
name: Cost Estimate
on:
  pull_request:
    paths:
      - '**/*.bicep'
      - '**/*.json'

jobs:
  estimate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - id: cost
        uses: polatengin/washington@main
        with:
          file: main.bicep
          params-file: main.bicepparam
          output-format: json
```

## Inputs

| Input | Description | Required | Default |
|-------|-------------|----------|---------|
| `file` | Path to the `.bicep` file to estimate | Yes | — |
| `params-file` | Optional `.bicepparam` file | No | — |
| `base-file` | Base branch `.bicep` file for delta comparison | No | — |
| `base-params-file` | Base branch `.bicepparam` file for delta comparison | No | — |
| `output-format` | Result format: `json`, `table`, or `markdown` | No | `json` |
| `fail-on-threshold` | Fail the step if the monthly total exceeds this value | No | — |

## Outputs

| Output | Description |
|-------|-------------|
| `estimation-result` | Full estimation result in the selected output format |
| `total-cost` | Estimated monthly total |
| `base-cost` | Estimated monthly total for the base file, or `0` |
| `delta-cost` | Difference between current and base totals |

## Example

```yaml
- name: Show estimated total
  run: echo "Monthly total: ${{ steps.cost.outputs.total-cost }}"
```
