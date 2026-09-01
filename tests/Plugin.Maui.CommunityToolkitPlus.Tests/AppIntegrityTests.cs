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

    [Fact]
    public async Task Key_Is_Created_Persisted_And_Reused()
    {
        var directory = TestHarness.CreateTempDirectory();
        try
        {
            var store = new AtomicVersionedStore(directory, null, null);
            var adapter = new StoreBackedIntegrityAdapter(store);
            var service = CreateService(adapter);

            var first = await service.CreateProofAsync(await service.CreateChallengeAsync());
            var second = await service.CreateProofAsync(await service.CreateChallengeAsync());

            Assert.True(first.Succeeded);
            Assert.True(second.Succeeded);
            Assert.Equal("created-key", first.Proof!.KeyId);
            Assert.Equal(first.Proof.KeyId, second.Proof!.KeyId);
            Assert.Equal(1, adapter.Creations);

            await store.DeleteAsync("app-integrity-key");
            var regenerated = await service.CreateProofAsync(await service.CreateChallengeAsync());
            Assert.True(regenerated.Succeeded);
            Assert.Equal(2, adapter.Creations);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task IntegrityDelegatingHandler_Adds_Header_When_Protected()
    {
        var service = CreateService(new FakeIntegrityAdapter());
        var inner = new CaptureHandler();
        var handler = new IntegrityDelegatingHandler(
            service,
            request => request.RequestUri!.AbsolutePath.Contains("secure", StringComparison.Ordinal),
            time: _time)
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);

        await client.GetAsync("https://example.test/secure");
        await client.GetAsync("https://example.test/public");

        Assert.Equal(2, inner.Requests.Count);
        Assert.True(inner.Requests[0].Headers.Contains("X-App-Integrity"));
        Assert.False(inner.Requests[1].Headers.Contains("X-App-Integrity"));
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

    sealed class StoreBackedIntegrityAdapter(IPlusStore store) : IIntegrityPlatformAdapter
    {
        public int Creations { get; private set; }

        public IntegrityCapability GetCapability() => new(true, true, true, "test");

        public async Task<IntegrityOperationResult> CreateProofAsync(
            IntegrityChallenge challenge,
            CancellationToken cancellationToken = default)
        {
            var record = await store.LoadAsync<IntegrityKeyRecord>("app-integrity-key", cancellationToken);
            if (record?.KeyId is null)
            {
                Creations++;
                record = new IntegrityKeyRecord { KeyId = "created-key", Platform = "test" };
                await store.SaveAsync("app-integrity-key", record, cancellationToken);
            }

            return IntegrityOperationResult.Ok(
                new IntegrityProof(challenge.Id, "test", "opaque-proof", record.KeyId));
        }
    }

    sealed class CaptureHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
