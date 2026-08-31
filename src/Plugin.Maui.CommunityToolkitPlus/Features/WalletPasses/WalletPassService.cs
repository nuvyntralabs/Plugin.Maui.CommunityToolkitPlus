namespace Plugin.Maui.CommunityToolkitPlus;

sealed class WalletPassService : IWalletPassService
{
    readonly IWalletPassPayloadProvider _payloads;
    readonly IWalletPlatformAdapter _platform;

    public WalletPassService(IWalletPassPayloadProvider payloads, IWalletPlatformAdapter platform)
    {
        _payloads = payloads;
        _platform = platform;
    }

    public WalletCapability GetCapability() => _platform.GetCapability();

    public async Task<WalletOperationResult> AddAsync(
        string passId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passId);
        var payload = await _payloads.GetPayloadAsync(passId, cancellationToken).ConfigureAwait(false);
        if (payload is null)
        {
            return WalletOperationResult.Fail(
                WalletErrorCodes.MissingPayload,
                $"No wallet payload was supplied for '{passId}'.");
        }

        return await _platform.AddAsync(payload, cancellationToken).ConfigureAwait(false);
    }
}

sealed class UnsupportedWalletAdapter : IWalletPlatformAdapter
{
    public WalletCapability GetCapability() => new(false, false, false, false, "unsupported");

    public Task<WalletOperationResult> AddAsync(
        WalletPassPayload payload,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(WalletOperationResult.Fail(
            WalletErrorCodes.Unsupported,
            "Wallet passes require iOS PassKit or an Android Google Wallet save URL."));
    }
}

sealed class MissingWalletPayloadProvider : IWalletPassPayloadProvider
{
    public Task<WalletPassPayload?> GetPayloadAsync(
        string passId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<WalletPassPayload?>(null);
}
