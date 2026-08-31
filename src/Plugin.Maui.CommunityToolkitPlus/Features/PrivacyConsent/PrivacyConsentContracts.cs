namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// Coordinates purpose-based consent. This helps implement a flow; it does not guarantee legal compliance.
/// </summary>
public interface IPrivacyConsentService
{
    /// <summary>Gets the active policy.</summary>
    ConsentPolicy Policy { get; }

    /// <summary>Records a decision for a purpose.</summary>
    Task<ConsentReceipt> RecordAsync(
        string purposeId,
        ConsentDecision decision,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the current receipt for a purpose, or <see langword="null"/>.</summary>
    Task<ConsentReceipt?> GetAsync(string purposeId, CancellationToken cancellationToken = default);

    /// <summary>Returns whether a purpose is currently accepted and unexpired.</summary>
    Task<bool> HasConsentAsync(string purposeId, CancellationToken cancellationToken = default);

    /// <summary>Registers an SDK that must not initialize until the required purposes are accepted.</summary>
    void RegisterSdk(string sdkId, IReadOnlyList<string> requiredPurposes, Func<CancellationToken, Task> initialize);

    /// <summary>Initializes registered SDKs whose required purposes are accepted.</summary>
    Task<IReadOnlyList<string>> ActivateReadySdksAsync(CancellationToken cancellationToken = default);

    /// <summary>Shows the default CommunityToolkit consent popup when IPopupService is available.</summary>
    Task<PlusResult> PresentAsync(CancellationToken cancellationToken = default);
}

/// <summary>Supplies a legal region. The default does not infer region from IP.</summary>
public interface IConsentRegionProvider
{
    /// <summary>Returns a region identifier such as an ISO country code, or <see langword="null"/>.</summary>
    string? GetRegion();
}

/// <summary>Optional platform consent adapter, such as iOS App Tracking Transparency.</summary>
public interface IConsentPlatformAdapter
{
    /// <summary>Adapter name.</summary>
    string Name { get; }

    /// <summary>Requests a platform consent decision when the host has supplied the required usage description.</summary>
    Task<ConsentDecision?> RequestAsync(CancellationToken cancellationToken = default);
}

/// <summary>A purpose that can be accepted or denied.</summary>
/// <param name="Id">Stable purpose identifier.</param>
/// <param name="Title">Display title.</param>
/// <param name="Description">Optional explanation.</param>
/// <param name="Required">Whether the host considers the purpose required.</param>
public sealed record PrivacyPurpose(string Id, string Title, string? Description = null, bool Required = false);

/// <summary>Versioned consent policy.</summary>
/// <param name="Version">Policy version. A new version requires renewal.</param>
/// <param name="Purposes">Purposes covered by this policy.</param>
/// <param name="DefaultLifetime">Optional expiry applied to new receipts.</param>
public sealed record ConsentPolicy(
    string Version,
    IReadOnlyList<PrivacyPurpose> Purposes,
    TimeSpan? DefaultLifetime = null);

/// <summary>An immutable local consent receipt.</summary>
/// <param name="PurposeId">Purpose identifier.</param>
/// <param name="Decision">Recorded decision.</param>
/// <param name="PolicyVersion">Policy version at the time of the decision.</param>
/// <param name="RecordedAt">UTC timestamp.</param>
/// <param name="ExpiresAt">Optional UTC expiry.</param>
public sealed record ConsentReceipt(
    string PurposeId,
    ConsentDecision Decision,
    string PolicyVersion,
    DateTimeOffset RecordedAt,
    DateTimeOffset? ExpiresAt);

/// <summary>A recorded consent decision.</summary>
public enum ConsentDecision
{
    /// <summary>The user accepted the purpose.</summary>
    Accepted,

    /// <summary>The user denied the purpose.</summary>
    Denied,

    /// <summary>A previous acceptance was revoked.</summary>
    Revoked
}

sealed class ConsentLedger
{
    public string PolicyVersion { get; set; } = "";
    public List<ConsentReceiptRecord> Receipts { get; set; } = [];
}

sealed class ConsentReceiptRecord
{
    public string PurposeId { get; set; } = "";
    public string Decision { get; set; } = "";
    public string PolicyVersion { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
