using Plugin.Maui.CommunityToolkitPlus;
using Plus = Plugin.Maui.CommunityToolkitPlus.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		var plus = SampleServices.Get<ICommunityToolkitPlus>() ?? Plus.Default;
		EnabledFeaturesLabel.Text =
			string.Join(Environment.NewLine, plus.EnabledFeatures.OrderBy(feature => feature)) +
			Environment.NewLine +
			$"Startup: {DemoStartup.LastUpgradeDecision?.ToString() ?? "pending"}" +
			(DemoStartup.RestoredRoute is null ? "" : $"{Environment.NewLine}Restored: {DemoStartup.RestoredRoute}");
	}
}
