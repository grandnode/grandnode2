namespace Grand.Infrastructure.Migrations;

public interface IMigrationProcess
{
    Task RunMigrationProcess();
    Task<MigrationResult> RunProcess(IMigration migration);
}