using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class IntegrityPage : ContentPage
{
    public IntegrityPage()
    {
        InitializeComponent();
    }

    void OnCapabilityClicked(object? sender, EventArgs e)
    {
        var integrity = SampleServices.Get<IAppIntegrityService>();
        if (integrity is null)
        {
            OutputLabel.Text = "App Integrity is not enabled.";
            return;
        }

        OutputLabel.Text = Format(integrity.GetCapability());
    }

    async void OnCreateProofClicked(object? sender, EventArgs e)
    {
        try
        {
            var integrity = SampleServices.Get<IAppIntegrityService>();
            if (integrity is null)
            {
                OutputLabel.Text = "App Integrity is not enabled.";
                return;
            }

            var capability = integrity.GetCapability();
            var challenge = await integrity.CreateChallengeAsync();
            var proof = await integrity.CreateProofAsync(challenge);
            OutputLabel.Text =
                $"{Format(capability)}\n" +
                $"Challenge: {challenge.Id}\n" +
                $"Nonce length: {challenge.Nonce.Length}\n" +
                $"Expires: {challenge.ExpiresAt:u}\n" +
                (proof.Succeeded
                    ? $"Proof platform: {proof.Proof!.Platform}\nKey: {proof.Proof.KeyId}\nPayload: {proof.Proof.Payload[..Math.Min(24, proof.Proof.Payload.Length)]}…"
                    : $"{proof.Code} — {proof.Message}");
        }
        catch (Exception ex)
        {
            OutputLabel.Text = ex.Message;
        }
    }

    static string Format(IntegrityCapability capability) =>
        $"Supported: {capability.IsSupported} ({capability.Platform})\nAttest: {capability.CanAttest}  Assert: {capability.CanAssert}";
}
