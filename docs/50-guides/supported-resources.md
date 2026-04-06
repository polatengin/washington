---
title: Supported Resources
sidebar_position: 51
---

# Supported Resources

Washington currently ships with pricing mappers for <!-- GENERATED:RESOURCE_COUNT -->0<!-- /GENERATED:RESOURCE_COUNT --> Azure resource types. Support is implemented in the CLI mapper registry and shared by the CLI, VS Code extension, and GitHub Action.

## Generated Matrix

<!-- BEGIN GENERATED SUPPORTED RESOURCE MATRIX -->
This matrix is generated from `src/cli/Mappers/MapperRegistry.cs` and each mapper's `ResourceType` property.
The registry order is preserved so the table stays aligned with the implementation.

### Registry Summary

| Registry Group | Mappers |
| --- | ---: |

### Coverage Matrix

| Registry Group | ARM Resource Type | Mapper |
| --- | --- | --- |
<!-- END GENERATED SUPPORTED RESOURCE MATRIX -->

## What Supported Means

Supported does not mean that every billing nuance for a service is modeled.

In practice, support means:

- Washington can recognize the resource type.
- Washington can derive one or more pricing queries from the resource's SKU or properties.
- Washington can produce a monthly cost line item from the returned price records.

Some services have a simple one-to-one mapping. Others are approximated from the most relevant recurring meter for the chosen SKU.

## Unsupported Resources

When a resource type does not have a mapper yet, Washington does not fail the whole estimate. It emits a warning like this instead:

```text
⚠ No pricing mapper for Microsoft.Xyz/abc - skipped
```

That behavior is useful in mixed templates, but it also means your total can be incomplete if some resource types are not yet mapped.

## Pricing Assumptions

The current mapper set uses pay-as-you-go retail pricing by default.

- Spot and low-priority pricing are excluded from the default queries.
- Reserved Instance, Savings Plan, and contract-specific pricing are not modeled yet.
- If a region cannot be resolved from the template, the estimator falls back to `eastus`.

## Choosing Good Validation Templates

If you want to validate mapper coverage quickly, start with templates that contain:

- one resource type per file
- explicit `location` and `sku` values
- minimal ARM expression indirection around SKU and sizing properties

That makes it easier to confirm whether a cost line is missing because the resource is unsupported or because an expression could not be resolved.

## Related Reading

- [How Estimates Work](/guides/how-estimates-work)
- [Troubleshooting](/guides/troubleshooting)
