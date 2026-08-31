using Plus = Plugin.Maui.CommunityToolkitPlus.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		EnabledFeaturesLabel.Text = string.Join(
			Environment.NewLine,
			Plus.Default.EnabledFeatures.OrderBy(feature => feature));
	}
}
