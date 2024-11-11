namespace Grand.Infrastructure.Migrations;

public interface IMigrationProcess
{
    void RunMigrationProcess();
    MigrationResult RunProcess(IMigration migration);
}