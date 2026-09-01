namespace Plugin.Maui.CommunityToolkitPlus.Tests;

public sealed class UpgradeGuardTests : IDisposable
{
    readonly string _directory = TestHarness.CreateTempDirectory();

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task Run_Completes_Pending_Migrations()
    {
        var migration = new TrackingMigration();
        var guard = CreateGuard();
        guard.Register(migration);

        var decision = await guard.RunAsync();
        var journal = await guard.GetJournalAsync();

        Assert.Equal(UpgradeDecision.Continue, decision);
        Assert.Equal(1, migration.Runs);
        Assert.Equal("Completed", journal.Migrations.Single().Status);
    }

    [Fact]
    public async Task Run_Resumes_Interrupted_Migration()
    {
        var first = new TrackingMigration { ThrowOnce = true };
        var failing = CreateGuard();
        failing.Register(first);
        Assert.Equal(UpgradeDecision.SafeMode, await failing.RunAsync());

        first.ThrowOnce = false;
        var decision = await failing.RunAsync();

        Assert.Equal(UpgradeDecision.Continue, decision);
        Assert.Equal(2, first.Runs);
        Assert.Equal("Completed", (await failing.GetJournalAsync()).Migrations.Single().Status);
    }

    [Fact]
    public async Task Completed_Migration_Is_Not_Run_Again()
    {
        var migration = new TrackingMigration();
        var guard = CreateGuard();
        guard.Register(migration);

        await guard.RunAsync();
        await guard.MarkStartupHealthyAsync();
        await guard.RunAsync();

        Assert.Equal(1, migration.Runs);
    }

    [Fact]
    public async Task Startup_Loop_Enters_Safe_Mode()
    {
        var guard = CreateGuard(threshold: 3);
        guard.Register(new TrackingMigration());

        Assert.Equal(UpgradeDecision.Continue, await guard.RunAsync());
        Assert.Equal(UpgradeDecision.Continue, await guard.RunAsync());
        Assert.Equal(UpgradeDecision.SafeMode, await guard.RunAsync());
    }

    [Fact]
    public async Task Marking_Startup_Healthy_Resets_Failure_Count()
    {
        var guard = CreateGuard(threshold: 2);
        guard.Register(new TrackingMigration());

        Assert.Equal(UpgradeDecision.Continue, await guard.RunAsync());
        await guard.MarkStartupHealthyAsync();
        Assert.Equal(0, await ((IStartupHealthTracker)guard).GetAttemptCountAsync());
        Assert.Equal(UpgradeDecision.Continue, await guard.RunAsync());
    }

    [Fact]
    public async Task Journal_Left_Running_Resumes_On_Next_Run()
    {
        var store = new AtomicVersionedStore(_directory, null, null);
        await store.SaveAsync("upgrade-journal", new UpgradeJournalState
        {
            FromVersion = "1.0.0",
            ToVersion = "2.0.0",
            Migrations = [new UpgradeMigrationState { Id = "schema-2", Status = "Running" }]
        });

        var migration = new TrackingMigration();
        var guard = CreateGuard();
        guard.Register(migration);

        var decision = await guard.RunAsync();

        Assert.Equal(UpgradeDecision.Continue, decision);
        Assert.Equal(1, migration.Runs);
        Assert.Equal("Completed", (await guard.GetJournalAsync()).Migrations.Single().Status);
    }

    [Fact]
    public async Task Backup_And_Rollback_Are_Invoked_On_Failure()
    {
        var backup = new TrackingBackup();
        var guard = CreateGuard(backup: backup);
        var migration = new TrackingMigration { ThrowOnce = true };
        guard.Register(migration);

        var decision = await guard.RunAsync();

        Assert.Equal(UpgradeDecision.SafeMode, decision);
        Assert.Equal(1, backup.Backups);
        Assert.Equal(1, backup.Rollbacks);
        Assert.Equal("Failed", (await guard.GetJournalAsync()).Migrations.Single().Status);
    }

    UpgradeGuardService CreateGuard(int threshold = 5, IUpgradeBackupProvider? backup = null) =>
        new(
            new AtomicVersionedStore(_directory, null, null),
            new UpgradeGuardOptions
            {
                Enabled = true,
                CurrentVersion = "2.0.0",
                SafeModeFailureThreshold = threshold
            },
            backup,
            NullLogger<UpgradeGuardService>.Instance);

    sealed class TrackingMigration : IAppMigration
    {
        public string Id => "schema-2";
        public int Runs { get; private set; }
        public bool ThrowOnce { get; set; }

        public Task MigrateAsync(UpgradeContext context, CancellationToken cancellationToken = default)
        {
            Runs++;
            if (ThrowOnce)
                throw new InvalidOperationException("interrupted");
            return Task.CompletedTask;
        }
    }

    sealed class TrackingBackup : IUpgradeBackupProvider
    {
        public int Backups { get; private set; }
        public int Rollbacks { get; private set; }

        public Task BackupAsync(UpgradeContext context, CancellationToken cancellationToken = default)
        {
            Backups++;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(UpgradeContext context, CancellationToken cancellationToken = default)
        {
            Rollbacks++;
            return Task.CompletedTask;
        }
    }
}
