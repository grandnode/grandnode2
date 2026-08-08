using Grand.Infrastructure.Migrations;

namespace Grand.Module.Migration.Migrations._2._0;

public class MigrationUpgradeDbVersion_20 : MigrationUpgradeDbVersionBase
{
    public override DbVersion Version => new(2, 0);

    public override Guid Identity => new("AEC3CF1F-4443-474A-B932-4F91D08C8F61");
}
