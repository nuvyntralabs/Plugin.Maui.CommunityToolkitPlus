namespace Plugin.Maui.CommunityToolkitPlus;

sealed class UpgradeGuardService : IUpgradeGuard, IStartupHealthTracker
{
    const string JournalName = "upgrade-journal";
    const string HealthName = "startup-health";

    readonly IPlusStore _store;
    readonly UpgradeGuardOptions _options;
    readonly IUpgradeBackupProvider? _backup;
    readonly ILogger<UpgradeGuardService> _logger;
    readonly List<IAppMigration> _migrations = [];
    readonly object _gate = new();

    public UpgradeGuardService(
        IPlusStore store,
        UpgradeGuardOptions options,
        IUpgradeBackupProvider? backup,
        ILogger<UpgradeGuardService> logger)
    {
        _store = store;
        _options = options;
        _backup = backup;
        _logger = logger;
    }

    public void Register(IAppMigration migration)
    {
        ArgumentNullException.ThrowIfNull(migration);
        ArgumentException.ThrowIfNullOrWhiteSpace(migration.Id);
        lock (_gate)
        {
            _migrations.RemoveAll(existing => existing.Id == migration.Id);
            _migrations.Add(migration);
        }
    }

    public async Task<UpgradeDecision> RunAsync(CancellationToken cancellationToken = default)
    {
        var attempts = await RecordAttemptAsync(cancellationToken).ConfigureAwait(false);
        if (attempts >= _options.SafeModeFailureThreshold)
        {
            _logger.LogWarning(
                "Upgrade Guard entered safe mode after {Attempts} failed startups.",
                attempts);
            return UpgradeDecision.SafeMode;
        }

        IAppMigration[] migrations;
        lock (_gate)
            migrations = _migrations.ToArray();

        var journal = await _store.LoadAsync<UpgradeJournalState>(JournalName, cancellationToken)
            .ConfigureAwait(false)
            ?? new UpgradeJournalState { ToVersion = _options.CurrentVersion };

        if (!string.Equals(journal.ToVersion, _options.CurrentVersion, StringComparison.Ordinal))
        {
            journal.FromVersion = journal.ToVersion;
            journal.ToVersion = _options.CurrentVersion;
        }

        var byId = new Dictionary<string, UpgradeMigrationState>(StringComparer.Ordinal);
        foreach (var entry in journal.Migrations)
            byId[entry.Id] = entry;

        foreach (var migration in migrations)
        {
            if (byId.ContainsKey(migration.Id))
                continue;

            var pending = new UpgradeMigrationState { Id = migration.Id, Status = "Pending" };
            journal.Migrations.Add(pending);
            byId[migration.Id] = pending;
        }

        var context = new UpgradeContext(journal.FromVersion, journal.ToVersion);
        if (_backup is not null && journal.Migrations.Exists(entry => entry.Status is "Pending" or "Failed"))
            await _backup.BackupAsync(context, cancellationToken).ConfigureAwait(false);

        foreach (var migration in migrations)
        {
            var entry = byId[migration.Id];
            if (entry.Status == "Completed")
                continue;

            entry.Status = "Running";
            await _store.SaveAsync(JournalName, journal, cancellationToken).ConfigureAwait(false);

            try
            {
                await migration.MigrateAsync(context, cancellationToken).ConfigureAwait(false);
                entry.Status = "Completed";
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                entry.Status = "Failed";
                await _store.SaveAsync(JournalName, journal, cancellationToken).ConfigureAwait(false);
                _logger.LogError(ex, "Upgrade migration {MigrationId} failed.", migration.Id);
                if (_backup is not null)
                    await _backup.RollbackAsync(context, cancellationToken).ConfigureAwait(false);
                return UpgradeDecision.SafeMode;
            }

            await _store.SaveAsync(JournalName, journal, cancellationToken).ConfigureAwait(false);
        }

        return UpgradeDecision.Continue;
    }

    public async Task MarkStartupHealthyAsync(CancellationToken cancellationToken = default)
    {
        await MarkHealthyAsync(cancellationToken).ConfigureAwait(false);
        var journal = await _store.LoadAsync<UpgradeJournalState>(JournalName, cancellationToken)
            .ConfigureAwait(false);
        if (journal is null)
        {
            journal = new UpgradeJournalState { ToVersion = _options.CurrentVersion };
            await _store.SaveAsync(JournalName, journal, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<UpgradeJournal> GetJournalAsync(CancellationToken cancellationToken = default)
    {
        var journal = await _store.LoadAsync<UpgradeJournalState>(JournalName, cancellationToken)
            .ConfigureAwait(false)
            ?? new UpgradeJournalState { ToVersion = _options.CurrentVersion };

        return new UpgradeJournal(
            journal.FromVersion,
            journal.ToVersion,
            journal.Migrations.Select(entry => new UpgradeMigrationEntry(entry.Id, entry.Status)).ToArray());
    }

    public async Task<int> RecordAttemptAsync(CancellationToken cancellationToken = default)
    {
        var state = await _store.LoadAsync<StartupHealthState>(HealthName, cancellationToken)
            .ConfigureAwait(false)
            ?? new StartupHealthState { Version = _options.CurrentVersion };

        if (!string.Equals(state.Version, _options.CurrentVersion, StringComparison.Ordinal))
        {
            state.Version = _options.CurrentVersion;
            state.FailedAttempts = 0;
        }

        state.FailedAttempts++;
        await _store.SaveAsync(HealthName, state, cancellationToken).ConfigureAwait(false);
        return state.FailedAttempts;
    }

    public async Task MarkHealthyAsync(CancellationToken cancellationToken = default)
    {
        var state = new StartupHealthState
        {
            Version = _options.CurrentVersion,
            FailedAttempts = 0
        };
        await _store.SaveAsync(HealthName, state, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetAttemptCountAsync(CancellationToken cancellationToken = default)
    {
        var state = await _store.LoadAsync<StartupHealthState>(HealthName, cancellationToken)
            .ConfigureAwait(false);
        return state?.FailedAttempts ?? 0;
    }
}
