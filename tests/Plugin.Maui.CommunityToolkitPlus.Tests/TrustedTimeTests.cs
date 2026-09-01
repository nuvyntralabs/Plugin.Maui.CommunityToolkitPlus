namespace Plugin.Maui.CommunityToolkitPlus.Tests;

public sealed class TrustedTimeTests : IDisposable
{
    readonly string _directory = TestHarness.CreateTempDirectory();
    readonly TestTimeProvider _time = new(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task Synchronize_Uses_Median_And_Rejects_Outliers()
    {
        var service = CreateService(
            new FixedTimeSource("a", _time.GetUtcNow()),
            new FixedTimeSource("b", _time.GetUtcNow().AddSeconds(2)),
            new FixedTimeSource("c", _time.GetUtcNow().AddHours(3)));

        var result = await service.SynchronizeAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(TrustedTimeConfidence.High, result.Value!.Confidence);
        Assert.Equal(2, result.Value.SourceCount);
        Assert.Equal(_time.GetUtcNow().AddSeconds(2), result.Value.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetUtcNow_Advances_With_Monotonic_Clock()
    {
        var service = CreateService(new FixedTimeSource("a", _time.GetUtcNow()));
        Assert.True((await service.SynchronizeAsync()).Succeeded);

        _time.Advance(TimeSpan.FromMinutes(5));
        var result = await service.GetUtcNowAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(_time.GetUtcNow(), result.Value!.UtcNow, TimeSpan.FromSeconds(1));
        Assert.Equal(TrustedTimeConfidence.High, result.Value.Confidence);
    }

    [Fact]
    public async Task Wall_Clock_Jump_Forces_Resynchronization()
    {
        var source = new FixedTimeSource("a", _time.GetUtcNow());
        var service = CreateService(source);
        await service.SynchronizeAsync();

        _time.JumpWallClock(TimeSpan.FromHours(2));
        source.Utc = _time.GetUtcNow();
        var result = await service.GetUtcNowAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(source.Utc, result.Value!.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Failed_Sources_Fall_Back_To_Persisted_Anchor()
    {
        var service = CreateService(new FixedTimeSource("a", _time.GetUtcNow()));
        await service.SynchronizeAsync();

        var offline = CreateService(new FailingTimeSource());
        _time.Advance(TimeSpan.FromMinutes(3));
        var result = await offline.SynchronizeAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(TrustedTimeConfidence.Degraded, result.Value!.Confidence);
    }

    [Fact]
    public async Task Missing_Sources_And_Anchor_Fail()
    {
        var result = await CreateService().SynchronizeAsync();
        Assert.False(result.Succeeded);
        Assert.Equal(PlusErrorCodes.InvalidConfiguration, result.Code);
    }

    [Fact]
    public async Task Stale_Anchor_Degrades_After_Offline_Grace_Period()
    {
        var service = CreateService(TimeSpan.FromMinutes(5), new FixedTimeSource("a", _time.GetUtcNow()));
        Assert.True((await service.SynchronizeAsync()).Succeeded);

        var offline = CreateService(TimeSpan.FromMinutes(5));
        _time.Advance(TimeSpan.FromMinutes(6));
        var result = await offline.GetUtcNowAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(TrustedTimeConfidence.Degraded, result.Value!.Confidence);
    }

    [Fact]
    public async Task All_Sources_Fail_Without_Anchor_Returns_TransientFailure()
    {
        var result = await CreateService(new FailingTimeSource()).SynchronizeAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(PlusErrorCodes.TransientFailure, result.Code);
    }

    [Fact]
    public async Task Changed_Event_And_LastSnapshot_Are_Updated()
    {
        var service = CreateService(new FixedTimeSource("a", _time.GetUtcNow()));
        TrustedTimeSnapshot? observed = null;
        service.Changed += (_, args) => observed = args.Snapshot;

        var result = await service.SynchronizeAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(observed);
        Assert.Equal(result.Value, service.LastSnapshot);
        Assert.Equal(result.Value!.SynchronizedAt, observed!.SynchronizedAt);
    }

    [Fact]
    public async Task HttpDateTimeSource_Uses_Response_Date_Header()
    {
        var expected = new DateTimeOffset(2026, 8, 31, 15, 0, 0, TimeSpan.Zero);
        var source = new HttpDateTimeSource(
            new Uri("https://example.test/time"),
            new DateHeaderHandler(expected));

        var utc = await source.GetUtcAsync();

        Assert.Equal(expected, utc);
    }

    TrustedTimeService CreateService(params ITimeSource[] sources) =>
        CreateService(TimeSpan.FromHours(24), sources);

    TrustedTimeService CreateService(TimeSpan grace, params ITimeSource[] sources) =>
        new(
            sources,
            new TrustedTimeOptions
            {
                Enabled = true,
                MaxClockSkew = TimeSpan.FromMinutes(1),
                OfflineGracePeriod = grace
            },
            _time,
            new AtomicVersionedStore(_directory, null, null),
            NullLogger<TrustedTimeService>.Instance);

    sealed class FixedTimeSource(string name, DateTimeOffset utc) : ITimeSource
    {
        public string Name { get; } = name;
        public DateTimeOffset Utc { get; set; } = utc;

        public Task<DateTimeOffset?> GetUtcAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<DateTimeOffset?>(Utc);
    }

    sealed class FailingTimeSource : ITimeSource
    {
        public string Name => "down";

        public Task<DateTimeOffset?> GetUtcAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<DateTimeOffset?>(null);
    }

    sealed class DateHeaderHandler(DateTimeOffset date) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Headers.Date = date;
            return Task.FromResult(response);
        }
    }
}
