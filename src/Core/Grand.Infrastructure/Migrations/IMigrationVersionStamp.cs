namespace Grand.Infrastructure.Migrations;

/// <summary>
///     Marks the migration that records a database version as reached.
/// </summary>
/// <remarks>
///     A version stamp must be the last migration to run for its <see cref="IBaseMigration.Version" />, and it must only
///     run when every other migration of that version succeeded. <see cref="MigrationManager.GetCurrentMigrations" />
///     filters by version, so a version recorded before its own migrations completed makes them unreachable forever —
///     they compare equal to the installed version and are never selected again.
/// </remarks>
public interface IMigrationVersionStamp : IMigration;
