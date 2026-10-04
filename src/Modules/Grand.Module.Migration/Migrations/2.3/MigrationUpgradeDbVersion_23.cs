using Grand.Infrastructure.Migrations;

namespace Grand.Module.Migration.Migrations._2._3;

public class MigrationUpgradeDbVersion_23 : MigrationUpgradeDbVersionBase
{
    public override DbVersion Version => new(2, 3);

    public override Guid Identity => new("689E5BFA-7229-41A5-AF48-07CB58C0D608");
}
