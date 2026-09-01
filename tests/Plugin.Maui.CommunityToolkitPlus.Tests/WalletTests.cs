namespace Plugin.Maui.CommunityToolkitPlus.Tests;

public sealed class WalletTests
{
    [Fact]
    public void Net_Reference_Assembly_Reports_No_Wallet_Capabilities()
    {
        var capability = new UnsupportedWalletAdapter().GetCapability();

        Assert.Equal("unsupported", capability.Platform);
        Assert.False(capability.CanAdd);
        Assert.False(capability.CanList);
        Assert.False(capability.CanUpdate);
        Assert.False(capability.CanRemove);
    }

    [Fact]
    public async Task Missing_Payload_Fails_With_Stable_Code()
    {
        var service = new WalletPassService(
            new MissingWalletPayloadProvider(),
            new FakeWalletAdapter());

        var result = await service.AddAsync("ticket-1");

        Assert.False(result.Succeeded);
        Assert.Equal(WalletErrorCodes.MissingPayload, result.Code);
    }

    [Fact]
    public async Task Payload_Is_Passed_To_Platform_Adapter()
    {
        var payload = new WalletPassPayload(
            "loyalty-1",
            "loyalty",
            PkPass: [1, 2, 3],
            SaveUrl: new Uri("https://pay.google.com/gp/v/save/demo"));
        var adapter = new FakeWalletAdapter();
        var service = new WalletPassService(new FixedPayloadProvider(payload), adapter);

        var result = await service.AddAsync("loyalty-1");

        Assert.True(result.Succeeded);
        Assert.Same(payload, adapter.LastPayload);
    }

    [Fact]
    public void Android_Capability_Mapping_Allows_Add_Only()
    {
        var capability = new WalletPassService(
            new MissingWalletPayloadProvider(),
            new MappedWalletAdapter(new WalletCapability(true, false, false, false, "android")))
            .GetCapability();

        Assert.Equal("android", capability.Platform);
        Assert.True(capability.CanAdd);
        Assert.False(capability.CanList);
        Assert.False(capability.CanUpdate);
        Assert.False(capability.CanRemove);
    }

    [Fact]
    public void Ios_Capability_Mapping_Allows_Add_And_List()
    {
        var capability = new WalletPassService(
            new MissingWalletPayloadProvider(),
            new MappedWalletAdapter(new WalletCapability(true, true, false, false, "ios")))
            .GetCapability();

        Assert.Equal("ios", capability.Platform);
        Assert.True(capability.CanAdd);
        Assert.True(capability.CanList);
        Assert.False(capability.CanUpdate);
        Assert.False(capability.CanRemove);
    }

    [Fact]
    public async Task Invalid_Payload_And_Cancelled_Use_Stable_Codes()
    {
        var invalid = await new WalletPassService(
            new FixedPayloadProvider(new WalletPassPayload("x", "ticket", null, null)),
            new MappedWalletAdapter(new WalletCapability(true, false, false, false, "android"), WalletErrorCodes.InvalidPayload))
            .AddAsync("x");
        var cancelled = await new WalletPassService(
            new FixedPayloadProvider(new WalletPassPayload("x", "ticket", null, new Uri("https://example.test"))),
            new MappedWalletAdapter(new WalletCapability(true, false, false, false, "android"), WalletErrorCodes.Cancelled))
            .AddAsync("x");
        var unsupported = await new WalletPassService(
            new FixedPayloadProvider(new WalletPassPayload("x", "ticket", null, null)),
            new UnsupportedWalletAdapter())
            .AddAsync("x");

        Assert.Equal(WalletErrorCodes.InvalidPayload, invalid.Code);
        Assert.Equal(WalletErrorCodes.Cancelled, cancelled.Code);
        Assert.Equal(WalletErrorCodes.Unsupported, unsupported.Code);
    }

    sealed class FixedPayloadProvider(WalletPassPayload payload) : IWalletPassPayloadProvider
    {
        public Task<WalletPassPayload?> GetPayloadAsync(
            string passId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<WalletPassPayload?>(payload);
    }

    sealed class FakeWalletAdapter : IWalletPlatformAdapter
    {
        public WalletPassPayload? LastPayload { get; private set; }

        public WalletCapability GetCapability() => new(true, false, false, false, "test");

        public Task<WalletOperationResult> AddAsync(
            WalletPassPayload payload,
            CancellationToken cancellationToken = default)
        {
            LastPayload = payload;
            return Task.FromResult(WalletOperationResult.Ok());
        }
    }

    sealed class MappedWalletAdapter(WalletCapability capability, string? failCode = null) : IWalletPlatformAdapter
    {
        public WalletCapability GetCapability() => capability;

        public Task<WalletOperationResult> AddAsync(
            WalletPassPayload payload,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(failCode is null
                ? WalletOperationResult.Ok()
                : WalletOperationResult.Fail(failCode, failCode));
    }
}
