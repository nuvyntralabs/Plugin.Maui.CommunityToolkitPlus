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
        try
        {
            if (_contributor is not null)
                _contributor.Text = DraftEditor.Text ?? "";
            var state = SampleServices.Get<IStateRestorationService>();
            if (state is null)
                return;
            var route = Shell.Current?.CurrentState?.Location?.ToString() ?? "//State";
            var checkpoint = await state.CheckpointAsync(route);
            OutputLabel.Text = $"Saved route {checkpoint.Route} until {checkpoint.ExpiresAt:u}";
        }
        catch (Exception ex)
        {
            OutputLabel.Text = ex.Message;
        }
    }

    async void OnRestoreClicked(object? sender, EventArgs e)
    {
        try
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
        catch (Exception ex)
        {
            OutputLabel.Text = ex.Message;
        }
    }

    async void OnClearClicked(object? sender, EventArgs e)
    {
        try
        {
            var state = SampleServices.Get<IStateRestorationService>();
            if (state is null)
                return;
            await state.ClearAsync();
            OutputLabel.Text = "Checkpoint cleared.";
        }
        catch (Exception ex)
        {
            OutputLabel.Text = ex.Message;
        }
    }
}
