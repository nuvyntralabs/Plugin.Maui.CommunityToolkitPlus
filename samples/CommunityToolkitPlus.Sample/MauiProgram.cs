using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugin.Maui.CommunityToolkitPlus;

namespace CommunityToolkitPlus.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<IWalletPassPayloadProvider, DemoWalletPayloadProvider>();
        builder.Services.AddSingleton<ITimeSource, DemoTimeSource>();
        builder.Services.AddSingleton<DemoDraftContributor>();
        builder.Services.AddSingleton<IAppMigration, DemoMigration>();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitPlus(options =>
            {
                options.AppIntegrity.Enabled = true;
                options.AccessibilityAudit.Enabled = true;
                options.StateRestoration.Enabled = true;
                options.UpgradeGuard.Enabled = true;
                options.UpgradeGuard.CurrentVersion = AppInfo.Current.VersionString;
                options.TrustedTime.Enabled = true;
                options.WalletPasses.Enabled = true;
                options.PrivacyConsent.Enabled = true;
                options.PrivacyConsent.Policy = new ConsentPolicy(
                    "1",
                    [
                        new PrivacyPurpose("analytics", "Analytics", "Usage diagnostics"),
                        new PrivacyPurpose("personalization", "Personalization")
                    ]);
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
