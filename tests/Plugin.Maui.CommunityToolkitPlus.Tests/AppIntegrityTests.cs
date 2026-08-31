namespace Plugin.Maui.CommunityToolkitPlus.Tests;

public sealed class AppIntegrityTests
{
    readonly TestTimeProvider _time = new(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Expired_Challenge_Is_Rejected()
    {
        var service = CreateService(new FakeIntegrityAdapter());
        var challenge = await service.CreateChallengeAsync();
        _time.Advance(TimeSpan.FromMinutes(5));

        var result = await service.CreateProofAsync(challenge);

        Assert.False(result.Succeeded);
        Assert.Equal(IntegrityErrorCodes.ChallengeExpired, result.Code);
        Assert.Null(result.Proof);
    }

    [Fact]
    public async Task Valid_Challenge_Returns_Opaque_Proof()
    {
        var service = CreateService(new FakeIntegrityAdapter());
        var challenge = await service.CreateChallengeAsync();
        var result = await service.CreateProofAsync(challenge);

        Assert.True(result.Succeeded);
        Assert.Equal(challenge.Id, result.Proof!.ChallengeId);
        Assert.Equal("test", result.Proof.Platform);
        Assert.False(string.IsNullOrWhiteSpace(result.Proof.Payload));
    }

    [Fact]
    public async Task Unsupported_Adapter_Does_Not_Invent_A_Trusted_Verdict()
    {
        var service = CreateService(new UnsupportedIntegrityAdapter());
        var challenge = await service.CreateChallengeAsync();
        var result = await service.CreateProofAsync(challenge);

        Assert.False(result.Succeeded);
        Assert.Equal(IntegrityErrorCodes.Unsupported, result.Code);
        Assert.False(service.GetCapability().IsSupported);
    }

    [Fact]
    public async Task Key_Loss_Is_Reported_As_A_Stable_Code()
    {
        var service = CreateService(new FakeIntegrityAdapter { LoseKey = true });
        var result = await service.CreateProofAsync(await service.CreateChallengeAsync());

        Assert.False(result.Succeeded);
        Assert.Equal(IntegrityErrorCodes.KeyLost, result.Code);
    }

    IAppIntegrityService CreateService(IIntegrityPlatformAdapter adapter) =>
        new AppIntegrityService(
            new MemoryIntegrityChallengeProvider(_time),
            adapter,
            new AppIntegrityOptions { Enabled = true, ChallengeLifetime = TimeSpan.FromMinutes(2) },
            _time);

    sealed class FakeIntegrityAdapter : IIntegrityPlatformAdapter
    {
        public bool LoseKey { get; set; }

        public IntegrityCapability GetCapability() => new(true, true, true, "test");

        public Task<IntegrityOperationResult> CreateProofAsync(
            IntegrityChallenge challenge,
            CancellationToken cancellationToken = default)
        {
            if (LoseKey)
            {
                return Task.FromResult(IntegrityOperationResult.Fail(
                    IntegrityErrorCodes.KeyLost,
                    "The test key was discarded."));
            }

            return Task.FromResult(IntegrityOperationResult.Ok(
                new IntegrityProof(challenge.Id, "test", "opaque-proof", "key-1")));
        }
    }
}
