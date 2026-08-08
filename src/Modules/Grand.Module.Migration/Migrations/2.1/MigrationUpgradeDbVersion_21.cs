using Grand.Infrastructure.Migrations;

namespace Grand.Module.Migration.Migrations._2._1;

public class MigrationUpgradeDbVersion_21 : MigrationUpgradeDbVersionBase
{
    public override DbVersion Version => new(2, 1);

    public override Guid Identity => new("EA674DAA-66B2-4F21-9C68-008ACE752FBD");
}
