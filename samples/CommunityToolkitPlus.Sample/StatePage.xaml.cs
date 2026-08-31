using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class StatePage : ContentPage
{
    readonly DemoDraftContributor? _contributor;

    public StatePage()
    {
        InitializeComponent();
        _contributor = SampleServices.Get<DemoDraftContributor>();
        if (_contributor is not null)
            DraftEditor.Text = _contributor.Text;
    }

    async void OnCheckpointClicked(object? sender, EventArgs e)
    {
        if (_contributor is not null)
            _contributor.Text = DraftEditor.Text ?? "";
        var state = SampleServices.Get<IStateRestorationService>();
        if (state is null)
            return;
        var checkpoint = await state.CheckpointAsync("//State");
        OutputLabel.Text = $"Saved until {checkpoint.ExpiresAt:u}";
    }

    async void OnRestoreClicked(object? sender, EventArgs e)
    {
        var state = SampleServices.Get<IStateRestorationService>();
        if (state is null)
            return;
        var context = await state.LoadAsync();
        if (context is null)
        {
            OutputLabel.Text = "No checkpoint.";
            return;
        }

        await state.ApplyAsync(context);
        if (_contributor is not null)
            DraftEditor.Text = _contributor.Text;
        OutputLabel.Text = $"Restored route {context.Route}";
    }
}
