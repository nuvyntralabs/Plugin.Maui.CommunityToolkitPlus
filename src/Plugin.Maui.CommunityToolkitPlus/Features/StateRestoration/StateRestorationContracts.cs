namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// Persists transient UI and workflow state. This is not a domain-record sync engine.
/// </summary>
public interface IStateRestorationService
{
    /// <summary>Registers a contributor. Registration is explicit so the module stays trim and AOT safe.</summary>
    void Register(IStateContributor contributor);

    /// <summary>Registers a schema migration.</summary>
    void Register(IStateMigration migration);

    /// <summary>Writes an explicit checkpoint.</summary>
    Task<StateCheckpoint> CheckpointAsync(
        string? route = null,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the last valid checkpoint when it has not expired.</summary>
    Task<StateRestoreContext?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Applies a previously loaded context after Shell is ready.</summary>
    Task ApplyAsync(StateRestoreContext context, CancellationToken cancellationToken = default);

    /// <summary>Deletes the persisted checkpoint.</summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>Captures and restores one named slice of UI or workflow state.</summary>
public interface IStateContributor
{
    /// <summary>Stable contributor key.</summary>
    string Key { get; }

    /// <summary>Schema version written by this contributor.</summary>
    int SchemaVersion { get; }

    /// <summary>Captures state as a JSON object.</summary>
    Task<JsonElement> CaptureAsync(CancellationToken cancellationToken = default);

    /// <summary>Restores previously captured state.</summary>
    Task RestoreAsync(JsonElement payload, CancellationToken cancellationToken = default);
}

/// <summary>Migrates a contributor payload between schema versions.</summary>
public interface IStateMigration
{
    /// <summary>Contributor key this migration applies to.</summary>
    string ContributorKey { get; }

    /// <summary>Schema version the payload is written as.</summary>
    int FromVersion { get; }

    /// <summary>Schema version the payload becomes.</summary>
    int ToVersion { get; }

    /// <summary>Transforms the payload.</summary>
    JsonElement Migrate(JsonElement payload);
}

/// <summary>Optional protection for sensitive restoration values.</summary>
public interface IStateProtector : IPlusDataProtector;

/// <summary>A persisted restoration checkpoint.</summary>
/// <param name="SavedAt">UTC timestamp.</param>
/// <param name="ExpiresAt">UTC expiry.</param>
/// <param name="Route">Optional Shell route.</param>
public sealed record StateCheckpoint(DateTimeOffset SavedAt, DateTimeOffset ExpiresAt, string? Route);

/// <summary>Validated state ready to apply after Shell is ready.</summary>
/// <param name="Checkpoint">The checkpoint metadata.</param>
/// <param name="Route">Shell route to navigate to, when present.</param>
/// <param name="Payloads">Contributor payloads after migration.</param>
public sealed record StateRestoreContext(
    StateCheckpoint Checkpoint,
    string? Route,
    IReadOnlyDictionary<string, JsonElement> Payloads);

sealed class RestorationSnapshot
{
    public DateTimeOffset SavedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string? Route { get; set; }
    public List<ContributorSnapshot> Contributors { get; set; } = [];
}

sealed class ContributorSnapshot
{
    public string Key { get; set; } = "";
    public int SchemaVersion { get; set; }
    public JsonElement Payload { get; set; }
}
