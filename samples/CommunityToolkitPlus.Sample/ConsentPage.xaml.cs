using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class ConsentPage : ContentPage
{
    public ConsentPage()
    {
        InitializeComponent();
    }

    async void OnAcceptAnalyticsClicked(object? sender, EventArgs e) =>
        await RecordAsync("analytics", ConsentDecision.Accepted);

    async void OnDenyAnalyticsClicked(object? sender, EventArgs e) =>
        await RecordAsync("analytics", ConsentDecision.Denied);

    async void OnRevokeAnalyticsClicked(object? sender, EventArgs e) =>
        await RecordAsync("analytics", ConsentDecision.Revoked);

    async void OnAcceptPersonalizationClicked(object? sender, EventArgs e) =>
        await RecordAsync("personalization", ConsentDecision.Accepted);

    async void OnPresentClicked(object? sender, EventArgs e)
    {
        try
        {
            var consent = SampleServices.Get<IPrivacyConsentService>();
            if (consent is null)
                return;
            var result = await consent.PresentAsync();
            OutputLabel.Text = result.Succeeded
                ? $"Presenter ready for policy {consent.Policy.Version} ({consent.Policy.Purposes.Count} purposes)."
                : $"{result.Code}: {result.Message}";
        }
        catch (Exception ex)
        {
            OutputLabel.Text = ex.Message;
        }
    }

    async void OnActivateClicked(object? sender, EventArgs e)
    {
        try
        {
            var consent = SampleServices.Get<IPrivacyConsentService>();
            if (consent is null)
                return;
            var activated = await consent.ActivateReadySdksAsync();
            OutputLabel.Text =
                $"Activated: {(activated.Count == 0 ? "(none)" : string.Join(", ", activated))}\n" +
                $"Analytics SDK: {DemoSdkGate.AnalyticsReady}\n" +
                $"Personalization SDK: {DemoSdkGate.PersonalizationReady}";
        }
        catch (Exception ex)
        {
            OutputLabel.Text = ex.Message;
        }
    }

    async Task RecordAsync(string purposeId, ConsentDecision decision)
    {
        try
        {
            var consent = SampleServices.Get<IPrivacyConsentService>();
            if (consent is null)
                return;
            var receipt = await consent.RecordAsync(purposeId, decision);
            var stored = await consent.GetAsync(purposeId);
            var allowed = await consent.HasConsentAsync(purposeId);
            OutputLabel.Text =
                $"{receipt.Decision} for {receipt.PurposeId} (policy {receipt.PolicyVersion}).\n" +
                $"GetAsync: {stored?.Decision}\n" +
                $"Expires: {receipt.ExpiresAt:u}\n" +
                $"Active: {allowed}";
        }
        catch (Exception ex)
        {
            OutputLabel.Text = ex.Message;
        }
    }
}
