using Washington.Models;

namespace Washington.Mappers;

public class MapperRegistry
{
    private readonly List<IResourceCostMapper> _mappers = new();

    public MapperRegistry()
    {
        Register(new VirtualMachineMapper());
        Register(new StorageAccountMapper());
        Register(new SqlDatabaseMapper());
        Register(new AppServicePlanMapper());
    }

    public void Register(IResourceCostMapper mapper) => _mappers.Add(mapper);

    public IResourceCostMapper? GetMapper(ResourceDescriptor resource) =>
        _mappers.FirstOrDefault(m => m.CanMap(resource));

    public IReadOnlyList<IResourceCostMapper> All => _mappers.AsReadOnly();
}
