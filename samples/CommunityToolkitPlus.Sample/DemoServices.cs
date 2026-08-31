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
                PkPass: null,
                SaveUrl: new Uri("https://pay.google.com/gp/v/save/demo-ticket")),
            "loyalty" => new WalletPassPayload(
                passId,
                "loyalty",
                PkPass: null,
                SaveUrl: new Uri("https://pay.google.com/gp/v/save/demo-loyalty")),
            "coupon" => new WalletPassPayload(
                passId,
                "coupon",
                PkPass: null,
                SaveUrl: new Uri("https://pay.google.com/gp/v/save/demo-coupon")),
            _ => null
        };

        return Task.FromResult(payload);
    }
}

sealed class DemoMigration : IAppMigration
{
    public string Id => "sample-1";

    public Task MigrateAsync(UpgradeContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
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
