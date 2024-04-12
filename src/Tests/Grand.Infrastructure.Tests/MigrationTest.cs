using Grand.Data;
using Grand.Infrastructure.Migrations;

namespace Grand.Infrastructure.Tests;

public class MigrationTest1 : IMigration
{
    public int Priority => 0;

    public DbVersion Version => new(int.Parse(GrandVersion.MajorVersion), int.Parse(GrandVersion.MinorVersion));

    public Guid Identity => Guid.NewGuid();

    public string Name => "SampleMigration";

    public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
    {
        return true;
    }
}

public class MigrationTest2 : IMigration
{
    public int Priority => 0;

    public DbVersion Version => new(int.Parse(GrandVersion.MajorVersion) + 1, int.Parse(GrandVersion.MinorVersion));

    public Guid Identity => Guid.NewGuid();

    public string Name => "SampleMigration";

    public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
    {
        return true;
    }
}