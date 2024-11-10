using Grand.Data;

namespace Grand.Module.Installer.Interfaces;
public interface IDatabaseFactoryContext
{
    IDatabaseContext GetDatabaseContext(string? connectionString = null, DbProvider? dbProvider = null);
}
