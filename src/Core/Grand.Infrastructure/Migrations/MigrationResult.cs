namespace Grand.Infrastructure.Migrations;

public class MigrationResult
{
    public required IBaseMigration Migration;
    public bool Success;
}