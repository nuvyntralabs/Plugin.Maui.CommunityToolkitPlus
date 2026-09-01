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
        RegisterModules(services);

        var window = new Window(new AppShell());
        window.Created += OnWindowCreated;
        return window;
    }

    static void RegisterModules(IServiceProvider? services)
    {
        if (services is null)
            return;

        var state = services.GetService<IStateRestorationService>();
        var draft = services.GetService<DemoDraftContributor>();
        if (state is not null && draft is not null)
        {
            state.Register(draft);
            state.Register(new DemoDraftMigration());
        }

        var upgrade = services.GetService<IUpgradeGuard>();
        var migration = services.GetService<IAppMigration>();
        if (upgrade is not null && migration is not null)
            upgrade.Register(migration);

        var consent = services.GetService<IPrivacyConsentService>();
        if (consent is not null)
        {
            consent.RegisterSdk("sample-analytics", ["analytics"], _ =>
            {
                DemoSdkGate.AnalyticsReady = true;
                return Task.CompletedTask;
            });
            consent.RegisterSdk("sample-personalization", ["personalization"], _ =>
            {
                DemoSdkGate.PersonalizationReady = true;
                return Task.CompletedTask;
            });
        }
    }

    static async void OnWindowCreated(object? sender, EventArgs e)
    {
        if (sender is Window window)
            window.Created -= OnWindowCreated;

        var services = IPlatformApplication.Current?.Services;
        if (services is null)
            return;

        try
        {
            var upgrade = services.GetService<IUpgradeGuard>();
            if (upgrade is not null)
            {
                var decision = await upgrade.RunAsync();
                DemoStartup.LastUpgradeDecision = decision;
                if (decision == UpgradeDecision.SafeMode)
                    return;

                await upgrade.MarkStartupHealthyAsync();
            }

            var state = services.GetService<IStateRestorationService>();
            if (state is null)
                return;

            var context = await state.LoadAsync();
            if (context is null)
                return;

            await state.ApplyAsync(context);
            DemoStartup.RestoredRoute = context.Route;
            if (!string.IsNullOrWhiteSpace(context.Route) && Shell.Current is not null)
                await Shell.Current.GoToAsync(context.Route);
        }
        catch (Exception ex)
        {
            DemoStartup.LastError = ex.Message;
        }
    }
}
