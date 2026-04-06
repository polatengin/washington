---
title: GitHub Action Examples
sidebar_position: 41
---

# GitHub Action Examples

This page collects practical workflow examples for the Washington GitHub Action.

## Pull Request Cost Check

Run cost estimation whenever Bicep files change in a pull request:

```yaml
name: Cost Estimate

on:
  pull_request:
    paths:
      - '**/*.bicep'
      - '**/*.bicepparam'

jobs:
  estimate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - id: cost
        uses: polatengin/washington@main
        with:
          file: infra/main.bicep
          params-file: infra/main.bicepparam
          output-format: json

      - name: Print estimate
        run: |
          echo "Monthly total: ${{ steps.cost.outputs.total-cost }}"
```

## Threshold Gate

Fail the workflow if a deployment estimate exceeds a monthly budget threshold:

```yaml
- id: cost
  uses: polatengin/washington@main
  with:
    file: infra/main.bicep
    params-file: infra/prod.bicepparam
    fail-on-threshold: 1000
```

## Delta Comparison Against a Base File

Compare the current template against a base branch version and consume the delta output:

```yaml
- id: cost
  uses: polatengin/washington@main
  with:
    file: infra/main.bicep
    params-file: infra/main.bicepparam
    base-file: base/infra/main.bicep
    base-params-file: base/infra/main.bicepparam
    output-format: json

- name: Show delta
  run: |
    echo "Current: ${{ steps.cost.outputs.total-cost }}"
    echo "Base: ${{ steps.cost.outputs.base-cost }}"
    echo "Delta: ${{ steps.cost.outputs.delta-cost }}"
```

## Markdown Output for Workflow Summaries

Request markdown output when you want to reuse the formatted result in a job summary or downstream step:

```yaml
- id: cost
  uses: polatengin/washington@main
  with:
    file: infra/main.bicep
    output-format: markdown

- name: Append summary
  run: |
    echo '${{ steps.cost.outputs.estimation-result }}' >> "$GITHUB_STEP_SUMMARY"
```

## Related Reading

- [GitHub Action](/github-action)
