using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class UpgradePage : ContentPage
{
    public UpgradePage()
    {
        InitializeComponent();
    }

    async void OnRunClicked(object? sender, EventArgs e)
    {
        var guard = SampleServices.Get<IUpgradeGuard>();
        if (guard is null)
            return;
        var decision = await guard.RunAsync();
        if (decision == UpgradeDecision.Continue)
            await guard.MarkStartupHealthyAsync();
        var journal = await guard.GetJournalAsync();
        OutputLabel.Text =
            $"Decision: {decision}\n" +
            $"Version: {journal.ToVersion}\n" +
            string.Join('\n', journal.Migrations.Select(entry => $"{entry.Id}: {entry.Status}"));
    }
}
