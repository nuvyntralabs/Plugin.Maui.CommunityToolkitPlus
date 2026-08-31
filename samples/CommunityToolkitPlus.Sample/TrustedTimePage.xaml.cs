using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class TrustedTimePage : ContentPage
{
    public TrustedTimePage()
    {
        InitializeComponent();
    }

    async void OnGetUtcClicked(object? sender, EventArgs e)
    {
        var time = SampleServices.Get<ITrustedTimeService>();
        if (time is null)
            return;
        var result = await time.GetUtcNowAsync();
        OutputLabel.Text = result.Succeeded
            ? $"{result.Value!.UtcNow:u}\nConfidence: {result.Value.Confidence}\nSources: {result.Value.SourceCount}"
            : $"{result.Code}: {result.Message}";
    }
}
