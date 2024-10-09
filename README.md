# washington

Azure Cost Estimator

## Cloning the repository

```bash
git clone
git submodule update --init --recursive
```

## Executing using example bicep and bicepparam files

```bash
dotnet run --project ./src/cli/cli.csproj --verbosity quiet -- --file ./samples/all.bicep --params-file ./samples/all.bicepparam
```
