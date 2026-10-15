using Grand.Infrastructure.Migrations;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationUpgradeDbVersion_25 : MigrationUpgradeDbVersionBase
{
    public override DbVersion Version => new(2, 5);

    public override Guid Identity => new("224B2827-8341-4FBD-B6EC-B0AD1FD336F4");
}
