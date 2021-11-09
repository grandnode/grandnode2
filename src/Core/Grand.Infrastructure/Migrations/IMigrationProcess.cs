namespace Grand.Infrastructure.Migrations
{
    public interface IMigrationProcess
    {
        void RunMigrationProcess();
        void InstallApplication();
        MigrationResult RunProcess(IMigration migration);
    }
}
