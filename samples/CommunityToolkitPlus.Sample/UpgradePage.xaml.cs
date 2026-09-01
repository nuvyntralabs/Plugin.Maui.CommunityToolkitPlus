using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class UpgradePage : ContentPage
{
    public UpgradePage()
    {
        InitializeComponent();
        OutputLabel.Text = FormatStartup();
    }

    async void OnRunClicked(object? sender, EventArgs e)
    {
        try
        {
            var guard = SampleServices.Get<IUpgradeGuard>();
            if (guard is null)
                return;
            var decision = await guard.RunAsync();
            if (decision == UpgradeDecision.Continue)
                await guard.MarkStartupHealthyAsync();
            OutputLabel.Text = await FormatAsync(guard, decision);
        }
        catch (Exception ex)
        {
            OutputLabel.Text = ex.Message;
        }
    }

    async void OnJournalClicked(object? sender, EventArgs e)
    {
        try
        {
            var guard = SampleServices.Get<IUpgradeGuard>();
            if (guard is null)
                return;
            OutputLabel.Text = await FormatAsync(guard, DemoStartup.LastUpgradeDecision);
        }
        catch (Exception ex)
        {
            OutputLabel.Text = ex.Message;
        }
    }

    static async Task<string> FormatAsync(IUpgradeGuard guard, UpgradeDecision? decision)
    {
        var journal = await guard.GetJournalAsync();
        var health = SampleServices.Get<IStartupHealthTracker>();
        var attempts = health is null ? 0 : await health.GetAttemptCountAsync();
        var backup = SampleServices.Get<DemoBackupProvider>();
        var migration = SampleServices.Get<DemoMigration>();
        return
            $"{FormatStartup()}\n" +
            $"Decision: {decision}\n" +
            $"Version: {journal.FromVersion} → {journal.ToVersion}\n" +
            $"Failed startups: {attempts}\n" +
            $"Backup / rollback: {backup?.Backups ?? 0} / {backup?.Rollbacks ?? 0}\n" +
            $"Last migration context: {migration?.LastContext ?? "(none)"}\n" +
            string.Join('\n', journal.Migrations.Select(entry => $"{entry.Id}: {entry.Status}"));
    }

    static string FormatStartup() =>
        $"Startup decision: {DemoStartup.LastUpgradeDecision}\n" +
        $"Startup restore: {DemoStartup.RestoredRoute ?? "(none)"}\n" +
        (DemoStartup.LastError is null ? "" : $"Startup error: {DemoStartup.LastError}\n");
}
