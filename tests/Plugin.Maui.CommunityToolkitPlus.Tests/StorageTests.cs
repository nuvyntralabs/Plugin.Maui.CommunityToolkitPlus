namespace Plugin.Maui.CommunityToolkitPlus.Tests;

public sealed class StorageTests : IDisposable
{
    readonly string _directory = TestHarness.CreateTempDirectory();

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task Store_Round_Trips_A_Document()
    {
        var store = CreateStore();
        var state = new StartupHealthState { Version = "1.2.3", FailedAttempts = 2 };

        await store.SaveAsync("health", state);
        var loaded = await store.LoadAsync<StartupHealthState>("health");

        Assert.NotNull(loaded);
        Assert.Equal("1.2.3", loaded.Version);
        Assert.Equal(2, loaded.FailedAttempts);
    }

    [Fact]
    public async Task Store_Returns_Default_When_Missing()
    {
        var loaded = await CreateStore().LoadAsync<StartupHealthState>("missing");
        Assert.Null(loaded);
    }

    [Fact]
    public async Task Store_Recovers_From_Corrupt_File_Using_Backup()
    {
        var store = CreateStore();
        await store.SaveAsync("health", new StartupHealthState { Version = "1.0.0", FailedAttempts = 1 });
        await store.SaveAsync("health", new StartupHealthState { Version = "2.0.0", FailedAttempts = 4 });

        await File.WriteAllTextAsync(Path.Combine(_directory, "health.json"), "{not-json");

        var loaded = await store.LoadAsync<StartupHealthState>("health");

        Assert.NotNull(loaded);
        Assert.Equal("1.0.0", loaded.Version);
        Assert.Equal(1, loaded.FailedAttempts);
    }

    [Fact]
    public async Task Store_Returns_Default_When_Both_Files_Are_Corrupt()
    {
        var store = CreateStore();
        await store.SaveAsync("health", new StartupHealthState { Version = "1.0.0" });
        await File.WriteAllTextAsync(Path.Combine(_directory, "health.json"), "{nope");
        await File.WriteAllTextAsync(Path.Combine(_directory, "health.json.bak"), "{nope");

        Assert.Null(await store.LoadAsync<StartupHealthState>("health"));
    }

    [Fact]
    public async Task Store_Delete_Removes_Document_And_Backup()
    {
        var store = CreateStore();
        await store.SaveAsync("health", new StartupHealthState { Version = "9" });
        await store.SaveAsync("health", new StartupHealthState { Version = "10" });
        await store.DeleteAsync("health");

        Assert.Null(await store.LoadAsync<StartupHealthState>("health"));
        Assert.False(File.Exists(Path.Combine(_directory, "health.json")));
        Assert.False(File.Exists(Path.Combine(_directory, "health.json.bak")));
    }

    IPlusStore CreateStore() => new AtomicVersionedStore(_directory, protector: null, logger: null);
}
