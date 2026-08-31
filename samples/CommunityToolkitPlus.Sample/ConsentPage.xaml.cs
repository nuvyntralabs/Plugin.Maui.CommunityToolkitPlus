using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class ConsentPage : ContentPage
{
    public ConsentPage()
    {
        InitializeComponent();
    }

    async void OnAcceptClicked(object? sender, EventArgs e) =>
        await RecordAsync(ConsentDecision.Accepted);

    async void OnDenyClicked(object? sender, EventArgs e) =>
        await RecordAsync(ConsentDecision.Denied);

    async void OnRevokeClicked(object? sender, EventArgs e) =>
        await RecordAsync(ConsentDecision.Revoked);

    async Task RecordAsync(ConsentDecision decision)
    {
        var consent = SampleServices.Get<IPrivacyConsentService>();
        if (consent is null)
            return;
        var receipt = await consent.RecordAsync("analytics", decision);
        var allowed = await consent.HasConsentAsync("analytics");
        OutputLabel.Text = $"{receipt.Decision} for {receipt.PurposeId} (policy {receipt.PolicyVersion}). Active: {allowed}";
    }
}
