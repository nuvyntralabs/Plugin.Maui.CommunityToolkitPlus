namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// Runs ordered, journaled migrations and tracks startup health after an application update.
/// </summary>
public interface IUpgradeGuard
{
    /// <summary>Registers an idempotent migration.</summary>
    void Register(IAppMigration migration);

    /// <summary>Runs or resumes migrations. Hosts must await this gate during startup.</summary>
    Task<UpgradeDecision> RunAsync(CancellationToken cancellationToken = default);

    /// <summary>Records that a stable page rendered after startup.</summary>
    Task MarkStartupHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the current journal snapshot.</summary>
    Task<UpgradeJournal> GetJournalAsync(CancellationToken cancellationToken = default);
}

/// <summary>An ordered, idempotent application migration.</summary>
public interface IAppMigration
{
    /// <summary>Stable migration identifier.</summary>
    string Id { get; }

    /// <summary>Runs the migration. Implementations must tolerate being called again after interruption.</summary>
    Task MigrateAsync(UpgradeContext context, CancellationToken cancellationToken = default);
}

/// <summary>Optional backup or rollback hooks supplied by the host. Generic store rollback is never promised.</summary>
public interface IUpgradeBackupProvider
{
    /// <summary>Creates a host-defined backup before migrations run.</summary>
    Task BackupAsync(UpgradeContext context, CancellationToken cancellationToken = default);

    /// <summary>Attempts a host-defined rollback after a failed migration.</summary>
    Task RollbackAsync(UpgradeContext context, CancellationToken cancellationToken = default);
}

/// <summary>Tracks consecutive failed startups.</summary>
public interface IStartupHealthTracker
{
    /// <summary>Increments the failed-startup counter and returns the updated count.</summary>
    Task<int> RecordAttemptAsync(CancellationToken cancellationToken = default);

    /// <summary>Clears the failed-startup counter.</summary>
    Task MarkHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the current failed-startup count.</summary>
    Task<int> GetAttemptCountAsync(CancellationToken cancellationToken = default);
}

/// <summary>Context supplied to migrations and backup hooks.</summary>
/// <param name="FromVersion">Previously healthy application version, when known.</param>
/// <param name="ToVersion">Version being started.</param>
public sealed record UpgradeContext(string? FromVersion, string ToVersion);

/// <summary>Durable upgrade journal.</summary>
/// <param name="FromVersion">Previous version.</param>
/// <param name="ToVersion">Target version.</param>
/// <param name="Migrations">Per-migration journal entries.</param>
public sealed record UpgradeJournal(
    string? FromVersion,
    string ToVersion,
    IReadOnlyList<UpgradeMigrationEntry> Migrations);

/// <summary>One journaled migration.</summary>
/// <param name="Id">Migration identifier.</param>
/// <param name="Status">Pending, Running, Completed, or Failed.</param>
public sealed record UpgradeMigrationEntry(string Id, string Status);

/// <summary>Startup decision after migrations and health checks.</summary>
public enum UpgradeDecision
{
    /// <summary>Startup may continue.</summary>
    Continue,

    /// <summary>Migrations failed or startup has crashed too many times.</summary>
    SafeMode
}

sealed class UpgradeJournalState
{
    public string? FromVersion { get; set; }
    public string ToVersion { get; set; } = "";
    public List<UpgradeMigrationState> Migrations { get; set; } = [];
}

sealed class UpgradeMigrationState
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "Pending";
}

sealed class StartupHealthState
{
    public string Version { get; set; } = "";
    public int FailedAttempts { get; set; }
}
