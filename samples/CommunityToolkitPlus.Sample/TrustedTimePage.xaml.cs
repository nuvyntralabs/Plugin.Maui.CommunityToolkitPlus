using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class TrustedTimePage : ContentPage
{
    ITrustedTimeService? _time;

    public TrustedTimePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _time = SampleServices.Get<ITrustedTimeService>();
        if (_time is not null)
            _time.Changed += OnTrustedTimeChanged;
    }

    protected override void OnDisappearing()
    {
        if (_time is not null)
            _time.Changed -= OnTrustedTimeChanged;
        _time = null;
        base.OnDisappearing();
    }

    async void OnGetUtcClicked(object? sender, EventArgs e)
    {
        try
        {
            var time = SampleServices.Get<ITrustedTimeService>();
            if (time is null)
                return;
            OutputLabel.Text = Format(await time.GetUtcNowAsync());
        }
        catch (Exception ex)
        {
            OutputLabel.Text = ex.Message;
        }
    }

    async void OnSynchronizeClicked(object? sender, EventArgs e)
    {
        try
        {
            var time = SampleServices.Get<ITrustedTimeService>();
            if (time is null)
                return;
            OutputLabel.Text = Format(await time.SynchronizeAsync());
        }
        catch (Exception ex)
        {
            OutputLabel.Text = ex.Message;
        }
    }

    void OnLastSnapshotClicked(object? sender, EventArgs e)
    {
        var time = SampleServices.Get<ITrustedTimeService>();
        OutputLabel.Text = time?.LastSnapshot is { } snapshot
            ? Format(snapshot)
            : "No snapshot yet. Get or synchronize trusted time first.";
    }

    void OnTrustedTimeChanged(object? sender, TrustedTimeChangedEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() => OutputLabel.Text = "Changed event\n" + Format(e.Snapshot));

    static string Format(PlusResult<TrustedTimeSnapshot> result) =>
        result.Succeeded ? Format(result.Value!) : $"{result.Code}: {result.Message}";

    static string Format(TrustedTimeSnapshot snapshot) =>
        $"{snapshot.UtcNow:u}\nConfidence: {snapshot.Confidence}\nSources: {snapshot.SourceCount}\nSynchronized: {snapshot.SynchronizedAt:u}";
}
