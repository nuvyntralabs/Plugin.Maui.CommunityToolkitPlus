#if ANDROID
namespace Plugin.Maui.CommunityToolkitPlus;

sealed class AndroidIntegrityAdapter : IIntegrityPlatformAdapter
{
    readonly IPlusStore _store;

    public AndroidIntegrityAdapter(IPlusStore store) => _store = store;

    public IntegrityCapability GetCapability() => new(true, true, true, "android");

    public async Task<IntegrityOperationResult> CreateProofAsync(
        IntegrityChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = await _store.LoadAsync<IntegrityKeyRecord>("app-integrity-key", cancellationToken)
            .ConfigureAwait(false);
        if (record?.KeyId is null)
        {
            record = new IntegrityKeyRecord
            {
                KeyId = Guid.NewGuid().ToString("N"),
                Platform = "android"
            };
            await _store.SaveAsync("app-integrity-key", record, cancellationToken).ConfigureAwait(false);
        }

        var payload = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{challenge.Id}:{challenge.Nonce}:{record.KeyId}"));
        return IntegrityOperationResult.Ok(
            new IntegrityProof(challenge.Id, "android", payload, record.KeyId));
    }
}

sealed class AndroidWalletAdapter : IWalletPlatformAdapter
{
    public WalletCapability GetCapability() => new(true, false, false, false, "android");

    public async Task<WalletOperationResult> AddAsync(
        WalletPassPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (payload.SaveUrl is null)
        {
            return WalletOperationResult.Fail(
                WalletErrorCodes.InvalidPayload,
                "Android wallet handoff requires a backend-issued Google Wallet save URL.");
        }

        var opened = await Browser.Default.OpenAsync(payload.SaveUrl, BrowserLaunchMode.SystemPreferred)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        return opened
            ? WalletOperationResult.Ok()
            : WalletOperationResult.Fail(WalletErrorCodes.Cancelled, "The Google Wallet save URL was not opened.");
    }
}

sealed class AndroidConsentAdapter : IConsentPlatformAdapter
{
    public string Name => "android";

    public Task<ConsentDecision?> RequestAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<ConsentDecision?>(null);
}
#endif
