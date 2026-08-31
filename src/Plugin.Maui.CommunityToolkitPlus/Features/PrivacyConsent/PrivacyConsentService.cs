namespace Plugin.Maui.CommunityToolkitPlus;

sealed class PrivacyConsentService : IPrivacyConsentService
{
    const string StoreName = "privacy-consent";

    readonly IPlusStore _store;
    readonly PrivacyConsentOptions _options;
    readonly TimeProvider _time;
    readonly IConsentPlatformAdapter? _platform;
    readonly IPopupService? _popups;
    readonly ILogger<PrivacyConsentService> _logger;
    readonly List<SdkGate> _gates = [];
    readonly HashSet<string> _activated = new(StringComparer.Ordinal);
    readonly object _gate = new();

    public PrivacyConsentService(
        IPlusStore store,
        PrivacyConsentOptions options,
        TimeProvider time,
        IConsentPlatformAdapter? platform,
        IPopupService? popups,
        ILogger<PrivacyConsentService> logger)
    {
        _store = store;
        _options = options;
        _time = time;
        _platform = platform;
        _popups = popups;
        _logger = logger;
        Policy = options.Policy;
    }

    public ConsentPolicy Policy { get; }

    public async Task<ConsentReceipt> RecordAsync(
        string purposeId,
        ConsentDecision decision,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purposeId);
        var ledger = await LoadAsync(cancellationToken).ConfigureAwait(false);
        var now = _time.GetUtcNow();
        var receipt = new ConsentReceipt(
            purposeId,
            decision,
            Policy.Version,
            now,
            Policy.DefaultLifetime is { } lifetime ? now.Add(lifetime) : null);

        ledger.PolicyVersion = Policy.Version;
        ledger.Receipts.RemoveAll(existing => existing.PurposeId == purposeId);
        ledger.Receipts.Add(ToRecord(receipt));
        await _store.SaveAsync(StoreName, ledger, cancellationToken).ConfigureAwait(false);
        return receipt;
    }

    public async Task<ConsentReceipt?> GetAsync(
        string purposeId,
        CancellationToken cancellationToken = default)
    {
        var ledger = await LoadAsync(cancellationToken).ConfigureAwait(false);
        var record = ledger.Receipts.LastOrDefault(item => item.PurposeId == purposeId);
        return record is null ? null : ToReceipt(record);
    }

    public async Task<bool> HasConsentAsync(
        string purposeId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await GetAsync(purposeId, cancellationToken).ConfigureAwait(false);
        if (receipt is null)
            return false;
        if (!string.Equals(receipt.PolicyVersion, Policy.Version, StringComparison.Ordinal))
            return false;
        if (receipt.Decision != ConsentDecision.Accepted)
            return false;
        return receipt.ExpiresAt is null || receipt.ExpiresAt > _time.GetUtcNow();
    }

    public void RegisterSdk(
        string sdkId,
        IReadOnlyList<string> requiredPurposes,
        Func<CancellationToken, Task> initialize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sdkId);
        ArgumentNullException.ThrowIfNull(requiredPurposes);
        ArgumentNullException.ThrowIfNull(initialize);
        lock (_gate)
        {
            _gates.RemoveAll(existing => existing.SdkId == sdkId);
            _gates.Add(new SdkGate(sdkId, requiredPurposes.ToArray(), initialize));
        }
    }

    public async Task<IReadOnlyList<string>> ActivateReadySdksAsync(
        CancellationToken cancellationToken = default)
    {
        SdkGate[] gates;
        lock (_gate)
            gates = _gates.ToArray();

        var activated = new List<string>();
        foreach (var gate in gates)
        {
            if (_activated.Contains(gate.SdkId))
                continue;

            var ready = true;
            foreach (var purpose in gate.RequiredPurposes)
            {
                if (!await HasConsentAsync(purpose, cancellationToken).ConfigureAwait(false))
                {
                    ready = false;
                    break;
                }
            }

            if (!ready)
                continue;

            await gate.Initialize(cancellationToken).ConfigureAwait(false);
            _activated.Add(gate.SdkId);
            activated.Add(gate.SdkId);
        }

        return activated;
    }

    public async Task<PlusResult> PresentAsync(CancellationToken cancellationToken = default)
    {
        if (_platform is not null)
        {
            var decision = await _platform.RequestAsync(cancellationToken).ConfigureAwait(false);
            if (decision is { } platformDecision && Policy.Purposes.Count > 0)
                await RecordAsync(Policy.Purposes[0].Id, platformDecision, cancellationToken).ConfigureAwait(false);
        }

        if (_popups is null)
        {
            return PlusResult.Fail(
                PlusErrorCodes.InvalidConfiguration,
                "No IPopupService is available. Call UseMauiCommunityToolkit before presenting consent UI.");
        }

        _logger.LogInformation(
            "Default consent presenter is ready for policy {PolicyVersion} with {PurposeCount} purposes.",
            Policy.Version,
            Policy.Purposes.Count);
        return PlusResult.Success;
    }

    async Task<ConsentLedger> LoadAsync(CancellationToken cancellationToken)
    {
        var ledger = await _store.LoadAsync<ConsentLedger>(StoreName, cancellationToken).ConfigureAwait(false)
            ?? new ConsentLedger { PolicyVersion = Policy.Version };

        if (!string.Equals(ledger.PolicyVersion, Policy.Version, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Consent policy changed from {Previous} to {Current}. Previous receipts require renewal.",
                ledger.PolicyVersion,
                Policy.Version);
            ledger.PolicyVersion = Policy.Version;
        }

        return ledger;
    }

    static ConsentReceiptRecord ToRecord(ConsentReceipt receipt) => new()
    {
        PurposeId = receipt.PurposeId,
        Decision = receipt.Decision.ToString(),
        PolicyVersion = receipt.PolicyVersion,
        RecordedAt = receipt.RecordedAt,
        ExpiresAt = receipt.ExpiresAt
    };

    static ConsentReceipt ToReceipt(ConsentReceiptRecord record) => new(
        record.PurposeId,
        Enum.Parse<ConsentDecision>(record.Decision),
        record.PolicyVersion,
        record.RecordedAt,
        record.ExpiresAt);

    sealed record SdkGate(string SdkId, IReadOnlyList<string> RequiredPurposes, Func<CancellationToken, Task> Initialize);
}

sealed class StaticConsentRegionProvider : IConsentRegionProvider
{
    public string? GetRegion() => null;
}

sealed class NoOpConsentPlatformAdapter : IConsentPlatformAdapter
{
    public string Name => "none";

    public Task<ConsentDecision?> RequestAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<ConsentDecision?>(null);
}
