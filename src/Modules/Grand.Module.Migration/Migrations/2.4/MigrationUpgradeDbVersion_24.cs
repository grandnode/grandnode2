using Grand.Infrastructure.Migrations;

namespace Grand.Module.Migration.Migrations._2._4;

public class MigrationUpgradeDbVersion_24 : MigrationUpgradeDbVersionBase
{
    public override DbVersion Version => new(2, 4);

    public override Guid Identity => new("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
}
