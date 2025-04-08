# Azure Deployments Cost Estimator

_Azure Deployments Cost Estimator_ project is a `FinOps` solution.

`FinOps` combines financial management, operations, and engineering best practices to help organizations understand, control, and optimize their cloud expenses. It encourages a culture where teams take ownership of their cloud usage, enabling faster product delivery while staying within budget.

## Key Concepts

_Visibility_: Understand who is spending what in the cloud.

_Accountability_: Encourage teams to take responsibility for their usage.

_Optimization_: Find ways to reduce waste and save costs.

_Collaboration_: Finance, engineering, and product teams work together.

## Cloning the repository

```bash
git clone
git submodule update --init --recursive
```

## Using example bicep and bicepparam files, without Grand Total line

```bash
dotnet run --project ./src/cli/cli.csproj --verbosity quiet -- --file ./samples/all.bicep --params-file ./samples/all.bicepparam
```

## Using example bicep and bicepparam files, with Grand Total line

```bash
dotnet run --project ./src/cli/cli.csproj --verbosity quiet -- --file ./samples/all.bicep --params-file ./samples/all.bicepparam --grand-total
```

## Using example bicep and bicepparam files, with Grand Total line, saved to a Markdown file

```bash
dotnet run --project ./src/cli/cli.csproj --verbosity quiet -- --file ./samples/all.bicep --params-file ./samples/all.bicepparam --grand-total --output-file ./samples/all.md
```
