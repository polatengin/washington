---
title: Extension Settings
sidebar_position: 31
---

# Extension Settings

This page documents the Washington VS Code extension settings in one place, including what each setting affects at runtime.

## Settings Reference

| Setting | Type | Default | Behavior |
| --- | --- | --- | --- |
| `washington.defaultRegion` | string | `eastus` | Used by the language server when a resource location cannot be resolved from the template. |
| `washington.cliPath` | string | `""` | Overrides CLI discovery and points the extension to a specific `bce` binary. |
| `washington.estimateOnSave` | boolean | `true` | Enables automatic estimate refresh for open `.bicep` files on open, save, and debounced change. |
| `washington.showCodeLens` | boolean | `true` | Enables or disables cost CodeLens annotations returned by the language server. |
| `washington.showStatusBar` | boolean | `true` | Shows or hides the total monthly cost status bar item. |
| `washington.cacheTtlHours` | number | `24` | Controls the language server pricing cache TTL in hours. |

## How `cliPath` Is Resolved

If `washington.cliPath` is empty, the extension tries these options in order:

1. The bundled `bce` binary shipped with the extension.
2. A workspace build at `src/cli/washington.csproj` using `dotnet run`.
3. `bce` on your `PATH`.

If you want predictable behavior across machines, set `washington.cliPath` explicitly.

## Automatic Refresh

When `washington.estimateOnSave` is enabled, the extension refreshes estimates when:

- a `.bicep` file is opened
- a `.bicep` file is saved
- a `.bicep` file changes and the LSP debounce interval elapses

If you prefer manual estimation only, disable `washington.estimateOnSave` and use the `Washington: Estimate File Cost` command when needed.

## Parameter Files in the Editor

When the extension estimates a file, it looks for a matching `.bicepparam` file in the same directory that targets the active `.bicep` file with a `using` statement. If it finds one, the parameter values are applied automatically.

## Updating Settings

The extension restarts its language client when Washington settings change, so changes to the settings above take effect without requiring a full window reload.

## Related Reading

- [VS Code Extension](/vscode-extension)
- [Troubleshooting](/guides/troubleshooting)
