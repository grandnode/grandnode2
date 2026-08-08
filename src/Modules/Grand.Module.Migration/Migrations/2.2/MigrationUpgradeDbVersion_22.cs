using Grand.Infrastructure.Migrations;

namespace Grand.Module.Migration.Migrations._2._2;

public class MigrationUpgradeDbVersion_22 : MigrationUpgradeDbVersionBase
{
    public override DbVersion Version => new(2, 2);

    public override Guid Identity => new("9B9FD138-7E67-44AA-913B-273F3D5B5DE9");
}
