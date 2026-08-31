using Microsoft.Extensions.DependencyInjection;
using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var services = IPlatformApplication.Current?.Services;
		var state = services?.GetService<IStateRestorationService>();
		var draft = services?.GetService<DemoDraftContributor>();
		if (state is not null && draft is not null)
			state.Register(draft);

		var upgrade = services?.GetService<IUpgradeGuard>();
		var migration = services?.GetService<IAppMigration>();
		if (upgrade is not null && migration is not null)
			upgrade.Register(migration);

		return new Window(new AppShell());
	}
}