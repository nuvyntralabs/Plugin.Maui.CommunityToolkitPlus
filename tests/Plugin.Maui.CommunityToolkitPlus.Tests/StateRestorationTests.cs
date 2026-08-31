namespace Plugin.Maui.CommunityToolkitPlus.Tests;

public sealed class StateRestorationTests : IDisposable
{
    readonly string _directory = TestHarness.CreateTempDirectory();
    readonly TestTimeProvider _time = new(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task Checkpoint_And_Restore_Contributor_State()
    {
        var service = CreateService();
        var contributor = new MemoryContributor();
        service.Register(contributor);

        contributor.Value = "draft-1";
        await service.CheckpointAsync("///checkout");

        contributor.Value = "changed";
        var context = await service.LoadAsync();

        Assert.NotNull(context);
        Assert.Equal("///checkout", context.Route);
        await service.ApplyAsync(context);
        Assert.Equal("draft-1", contributor.Value);
    }

    [Fact]
    public async Task Expired_Checkpoint_Is_Discarded()
    {
        var service = CreateService();
        service.Register(new MemoryContributor { Value = "old" });
        await service.CheckpointAsync();

        _time.Advance(TimeSpan.FromDays(8));
        Assert.Null(await service.LoadAsync());
    }

    [Fact]
    public async Task Migration_Is_Applied_Before_Restore()
    {
        var first = CreateService();
        first.Register(new VersionedContributor { SchemaVersion = 1, Value = "v1" });
        await first.CheckpointAsync();

        var second = CreateService();
        var contributor = new VersionedContributor { SchemaVersion = 2 };
        second.Register(contributor);
        second.Register(new PrefixMigration());

        var context = await second.LoadAsync();
        Assert.NotNull(context);
        await second.ApplyAsync(context);
        Assert.Equal("migrated:v1", contributor.Value);
    }

    IStateRestorationService CreateService() =>
        new StateRestorationService(
            new AtomicVersionedStore(_directory, null, null),
            new StateRestorationOptions { Enabled = true },
            _time,
            NullLogger<StateRestorationService>.Instance);

    sealed class MemoryContributor : IStateContributor
    {
        public string Key => "draft";
        public int SchemaVersion => 1;
        public string Value { get; set; } = "";

        public Task<JsonElement> CaptureAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(JsonSerializer.SerializeToElement(Value));

        public Task RestoreAsync(JsonElement payload, CancellationToken cancellationToken = default)
        {
            Value = payload.GetString() ?? "";
            return Task.CompletedTask;
        }
    }

    sealed class VersionedContributor : IStateContributor
    {
        public string Key => "draft";
        public int SchemaVersion { get; set; } = 1;
        public string Value { get; set; } = "";

        public Task<JsonElement> CaptureAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(JsonSerializer.SerializeToElement(Value));

        public Task RestoreAsync(JsonElement payload, CancellationToken cancellationToken = default)
        {
            Value = payload.GetString() ?? "";
            return Task.CompletedTask;
        }
    }

    sealed class PrefixMigration : IStateMigration
    {
        public string ContributorKey => "draft";
        public int FromVersion => 1;
        public int ToVersion => 2;

        public JsonElement Migrate(JsonElement payload) =>
            JsonSerializer.SerializeToElement("migrated:" + payload.GetString());
    }
}
