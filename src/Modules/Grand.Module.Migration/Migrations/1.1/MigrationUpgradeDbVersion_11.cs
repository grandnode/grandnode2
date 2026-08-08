using Grand.Infrastructure.Migrations;

namespace Grand.Module.Migration.Migrations._1._1;

public class MigrationUpgradeDbVersion_11 : MigrationUpgradeDbVersionBase
{
    public override DbVersion Version => new(1, 1);

    public override Guid Identity => new("6BDB7093-4C31-4D78-9604-58188DF728D3");
}
