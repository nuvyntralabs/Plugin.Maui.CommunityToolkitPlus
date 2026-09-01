using System.Text.Json;
using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

sealed class DemoTimeSource : ITimeSource
{
    public string Name => "sample-clock";

    public Task<DateTimeOffset?> GetUtcAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<DateTimeOffset?>(DateTimeOffset.UtcNow);
}

sealed class DemoWalletPayloadProvider : IWalletPassPayloadProvider
{
    public Task<WalletPassPayload?> GetPayloadAsync(string passId, CancellationToken cancellationToken = default)
    {
        var payload = passId switch
        {
            "ticket" => new WalletPassPayload(
                passId,
                "ticket",
                PkPass: [0x50, 0x4B, 0x03, 0x04],
                SaveUrl: new Uri("https://pay.google.com/gp/v/save/demo-ticket")),
            "loyalty" => new WalletPassPayload(
                passId,
                "loyalty",
                PkPass: [0x50, 0x4B, 0x03, 0x04],
                SaveUrl: new Uri("https://pay.google.com/gp/v/save/demo-loyalty")),
            "coupon" => new WalletPassPayload(
                passId,
                "coupon",
                PkPass: [0x50, 0x4B, 0x03, 0x04],
                SaveUrl: new Uri("https://pay.google.com/gp/v/save/demo-coupon")),
            _ => null
        };

        return Task.FromResult(payload);
    }
}

sealed class DemoMigration : IAppMigration
{
    public string Id => "sample-1";
    public string? LastContext { get; private set; }

    public Task MigrateAsync(UpgradeContext context, CancellationToken cancellationToken = default)
    {
        LastContext = $"{context.FromVersion ?? "(none)"} → {context.ToVersion}";
        return Task.CompletedTask;
    }
}

sealed class DemoBackupProvider : IUpgradeBackupProvider
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

sealed class DemoDraftContributor : IStateContributor
{
    public string Key => "sample-draft";
    public int SchemaVersion => 1;
    public string Text { get; set; } = "";

    public Task<JsonElement> CaptureAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(JsonSerializer.SerializeToElement(Text));

    public Task RestoreAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        Text = payload.GetString() ?? "";
        return Task.CompletedTask;
    }
}

sealed class DemoDraftMigration : IStateMigration
{
    public string ContributorKey => "sample-draft";
    public int FromVersion => 0;
    public int ToVersion => 1;

    public JsonElement Migrate(JsonElement payload) => payload;
}

static class DemoSdkGate
{
    public static bool AnalyticsReady { get; set; }
    public static bool PersonalizationReady { get; set; }
}

static class DemoStartup
{
    public static UpgradeDecision? LastUpgradeDecision { get; set; }
    public static string? RestoredRoute { get; set; }
    public static string? LastError { get; set; }
}
