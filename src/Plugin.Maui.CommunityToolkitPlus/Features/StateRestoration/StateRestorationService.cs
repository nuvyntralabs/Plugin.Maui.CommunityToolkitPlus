namespace Plugin.Maui.CommunityToolkitPlus;

sealed class StateRestorationService : IStateRestorationService
{
    const string StoreName = "state-restoration";

    readonly IPlusStore _store;
    readonly StateRestorationOptions _options;
    readonly TimeProvider _time;
    readonly ILogger<StateRestorationService> _logger;
    readonly List<IStateContributor> _contributors = [];
    readonly List<IStateMigration> _migrations = [];
    readonly object _gate = new();

    public StateRestorationService(
        IPlusStore store,
        StateRestorationOptions options,
        TimeProvider time,
        ILogger<StateRestorationService> logger)
    {
        _store = store;
        _options = options;
        _time = time;
        _logger = logger;
    }

    public void Register(IStateContributor contributor)
    {
        ArgumentNullException.ThrowIfNull(contributor);
        ArgumentException.ThrowIfNullOrWhiteSpace(contributor.Key);
        lock (_gate)
        {
            _contributors.RemoveAll(existing => existing.Key == contributor.Key);
            _contributors.Add(contributor);
        }
    }

    public void Register(IStateMigration migration)
    {
        ArgumentNullException.ThrowIfNull(migration);
        lock (_gate)
            _migrations.Add(migration);
    }

    public async Task<StateCheckpoint> CheckpointAsync(
        string? route = null,
        CancellationToken cancellationToken = default)
    {
        IStateContributor[] contributors;
        lock (_gate)
            contributors = _contributors.ToArray();

        var snapshot = new RestorationSnapshot
        {
            SavedAt = _time.GetUtcNow(),
            ExpiresAt = _time.GetUtcNow().Add(_options.DefaultTimeToLive),
            Route = route ?? TryGetCurrentRoute()
        };

        foreach (var contributor in contributors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            snapshot.Contributors.Add(new ContributorSnapshot
            {
                Key = contributor.Key,
                SchemaVersion = contributor.SchemaVersion,
                Payload = await contributor.CaptureAsync(cancellationToken).ConfigureAwait(false)
            });
        }

        await _store.SaveAsync(StoreName, snapshot, cancellationToken).ConfigureAwait(false);
        return new StateCheckpoint(snapshot.SavedAt, snapshot.ExpiresAt, snapshot.Route);
    }

    public async Task<StateRestoreContext?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _store.LoadAsync<RestorationSnapshot>(StoreName, cancellationToken)
            .ConfigureAwait(false);
        if (snapshot is null)
            return null;

        if (snapshot.ExpiresAt <= _time.GetUtcNow())
        {
            _logger.LogInformation("Discarded an expired state-restoration checkpoint.");
            await _store.DeleteAsync(StoreName, cancellationToken).ConfigureAwait(false);
            return null;
        }

        IStateMigration[] migrations;
        lock (_gate)
            migrations = _migrations.ToArray();

        var payloads = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var contributor in snapshot.Contributors)
        {
            var payload = contributor.Payload;
            var version = contributor.SchemaVersion;
            while (true)
            {
                var migration = migrations.FirstOrDefault(candidate =>
                    candidate.ContributorKey == contributor.Key && candidate.FromVersion == version);
                if (migration is null)
                    break;

                payload = migration.Migrate(payload);
                version = migration.ToVersion;
            }

            payloads[contributor.Key] = payload;
        }

        var checkpoint = new StateCheckpoint(snapshot.SavedAt, snapshot.ExpiresAt, snapshot.Route);
        return new StateRestoreContext(checkpoint, snapshot.Route, payloads);
    }

    public async Task ApplyAsync(StateRestoreContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        IStateContributor[] contributors;
        lock (_gate)
            contributors = _contributors.ToArray();

        foreach (var contributor in contributors)
        {
            if (!context.Payloads.TryGetValue(contributor.Key, out var payload))
                continue;

            cancellationToken.ThrowIfCancellationRequested();
            await contributor.RestoreAsync(payload, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        _store.DeleteAsync(StoreName, cancellationToken);

    static string? TryGetCurrentRoute()
    {
        try
        {
            return Shell.Current?.CurrentState?.Location?.ToString();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
