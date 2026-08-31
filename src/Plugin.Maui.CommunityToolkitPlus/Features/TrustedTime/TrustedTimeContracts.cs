namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// Supplies tamper-aware time. This service never replaces <see cref="DateTime.UtcNow"/> globally.
/// </summary>
public interface ITrustedTimeService
{
    /// <summary>Raised when the trusted-time snapshot changes.</summary>
    event EventHandler<TrustedTimeChangedEventArgs>? Changed;

    /// <summary>Gets the last computed snapshot, or <see langword="null"/> before the first successful read.</summary>
    TrustedTimeSnapshot? LastSnapshot { get; }

    /// <summary>Computes current trusted time from a synchronized UTC anchor plus monotonic elapsed time.</summary>
    Task<PlusResult<TrustedTimeSnapshot>> GetUtcNowAsync(CancellationToken cancellationToken = default);

    /// <summary>Forces a refresh from configured time sources.</summary>
    Task<PlusResult<TrustedTimeSnapshot>> SynchronizeAsync(CancellationToken cancellationToken = default);
}

/// <summary>A single trusted-time source.</summary>
public interface ITimeSource
{
    /// <summary>Source display name.</summary>
    string Name { get; }

    /// <summary>Reads UTC now from the source.</summary>
    Task<DateTimeOffset?> GetUtcAsync(CancellationToken cancellationToken = default);
}

/// <summary>A computed trusted-time value.</summary>
/// <param name="UtcNow">Trusted UTC instant.</param>
/// <param name="Confidence">How strongly the value can be trusted.</param>
/// <param name="SourceCount">How many sources contributed to the last sync.</param>
/// <param name="SynchronizedAt">When the UTC anchor was last established.</param>
public sealed record TrustedTimeSnapshot(
    DateTimeOffset UtcNow,
    TrustedTimeConfidence Confidence,
    int SourceCount,
    DateTimeOffset SynchronizedAt);

/// <summary>Confidence in a trusted-time snapshot.</summary>
public enum TrustedTimeConfidence
{
    /// <summary>No usable anchor exists.</summary>
    None,

    /// <summary>The device is offline and using a persisted offset within the grace period.</summary>
    Degraded,

    /// <summary>Multiple sources agreed within the configured skew.</summary>
    High
}

/// <summary>Raised when trusted time is recomputed.</summary>
/// <param name="snapshot">The new snapshot.</param>
public sealed class TrustedTimeChangedEventArgs(TrustedTimeSnapshot snapshot) : EventArgs
{
    /// <summary>Gets the new snapshot.</summary>
    public TrustedTimeSnapshot Snapshot { get; } = snapshot;
}

sealed class TrustedTimeAnchor
{
    public DateTimeOffset UtcAnchor { get; set; }
    public long MonotonicTimestamp { get; set; }
    public DateTimeOffset WallClockAtSync { get; set; }
    public int SourceCount { get; set; }
}
