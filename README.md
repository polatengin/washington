# washington

Azure Cost Estimator

## Cloning the repository

```bash
git clone
git submodule update --init --recursive
```

## Using example bicep and bicepparam files, without Grand Total line

```bash
dotnet run --project ./src/cli/cli.csproj --verbosity quiet -- --file ./samples/all.bicep --params-file ./samples/all.bicepparam
```
