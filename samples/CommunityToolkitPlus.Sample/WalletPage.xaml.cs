using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class WalletPage : ContentPage
{
    public WalletPage()
    {
        InitializeComponent();

        var wallet = SampleServices.Get<IWalletPassService>();
        if (wallet is not null)
        {
            var capability = wallet.GetCapability();
            OutputLabel.Text =
                $"Platform: {capability.Platform}\nAdd: {capability.CanAdd}  List: {capability.CanList}  Update: {capability.CanUpdate}  Remove: {capability.CanRemove}";
        }
    }

    async void OnAddPassClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: string passId })
            return;

        var wallet = SampleServices.Get<IWalletPassService>();
        if (wallet is null)
            return;
        var result = await wallet.AddAsync(passId);
        OutputLabel.Text = result.Succeeded ? $"Presented {passId}." : $"{result.Code}: {result.Message}";
    }
}
