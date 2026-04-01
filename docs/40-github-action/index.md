---
title: GitHub Action
sidebar_position: 40
---

# GitHub Action

The Washington GitHub Action posts Azure cost estimates as comments on your pull requests, helping you catch unexpected cost changes before merging.

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
      - uses: polatengin/washington@main
        with:
          file: main.bicep
```

## Inputs

| Input | Description | Required | Default |
|-------|-------------|----------|---------|
| `file` | Path to infrastructure file | Yes | — |
| `currency` | Display currency | No | `USD` |
| `comment` | Post PR comment | No | `true` |

## Example PR Comment

The action posts a comment like:

```
## 💰 Azure Cost Estimate

| Resource | Type | SKU | Monthly Cost |
|----------|------|-----|-------------|
| myVm | Microsoft.Compute/virtualMachines | Standard_D2s_v3 | $70.08 |
| myStorage | Microsoft.Storage/storageAccounts | Standard_LRS | $21.84 |

**Total estimated monthly cost: $91.92**
```
