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
        builder.Services.AddSingleton<DemoWalletPayloadProvider>();
        builder.Services.AddSingleton<IWalletPassPayloadProvider>(services =>
            services.GetRequiredService<DemoWalletPayloadProvider>());
        builder.Services.AddSingleton<ITimeSource, DemoTimeSource>();
        builder.Services.AddSingleton<DemoBackupProvider>();
        builder.Services.AddSingleton<IUpgradeBackupProvider>(services =>
            services.GetRequiredService<DemoBackupProvider>());
        builder.Services.AddSingleton<DemoDraftContributor>();
        builder.Services.AddSingleton<DemoMigration>();
        builder.Services.AddSingleton<IAppMigration>(services =>
            services.GetRequiredService<DemoMigration>());
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitPlus(options =>
            {
                options.AppIntegrity.Enabled = true;
                options.AppIntegrity.ChallengeLifetime = TimeSpan.FromMinutes(2);
                options.AccessibilityAudit.Enabled = true;
                options.AccessibilityAudit.MinimumTargetSize = 44;
                options.AccessibilityAudit.AccessibilityFontScale = 1.3;
                options.AccessibilityAudit.ShowDebugOverlay = true;
                options.StateRestoration.Enabled = true;
                options.StateRestoration.DefaultTimeToLive = TimeSpan.FromDays(7);
                options.UpgradeGuard.Enabled = true;
                options.UpgradeGuard.CurrentVersion = AppInfo.Current.VersionString;
                options.UpgradeGuard.SafeModeFailureThreshold = 3;
                options.TrustedTime.Enabled = true;
                options.TrustedTime.Sources.Add(new Uri("https://www.google.com"));
                options.TrustedTime.MaxClockSkew = TimeSpan.FromSeconds(30);
                options.TrustedTime.OfflineGracePeriod = TimeSpan.FromHours(24);
                options.WalletPasses.Enabled = true;
                options.PrivacyConsent.Enabled = true;
                options.PrivacyConsent.Policy = new ConsentPolicy(
                    "1",
                    [
                        new PrivacyPurpose("analytics", "Analytics", "Usage diagnostics"),
                        new PrivacyPurpose("personalization", "Personalization")
                    ],
                    TimeSpan.FromDays(365));
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
