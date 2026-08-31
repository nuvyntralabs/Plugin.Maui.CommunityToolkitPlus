using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class IntegrityPage : ContentPage
{
    public IntegrityPage()
    {
        InitializeComponent();
    }

    async void OnCreateProofClicked(object? sender, EventArgs e)
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
            $"Supported: {capability.IsSupported} ({capability.Platform})\n" +
            $"Challenge: {challenge.Id}\n" +
            $"Expires: {challenge.ExpiresAt:u}\n" +
            $"Proof: {(proof.Succeeded ? proof.Proof!.Platform + " / " + proof.Proof.Payload[..Math.Min(24, proof.Proof.Payload.Length)] + "…" : proof.Code + " — " + proof.Message)}";
    }
}
