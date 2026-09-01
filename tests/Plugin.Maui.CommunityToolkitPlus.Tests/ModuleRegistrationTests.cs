using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;

namespace Plugin.Maui.CommunityToolkitPlus.Tests;

public sealed class ModuleRegistrationTests : IDisposable
{
    public ModuleRegistrationTests() => CommunityToolkitPlus.Reset();

    public void Dispose() => CommunityToolkitPlus.Reset();

    [Fact]
    public void Disabled_Modules_Are_Not_Registered()
    {
        using var provider = TestHarness.CreatePlusBuilder().Services.BuildServiceProvider();

        Assert.Null(provider.GetService<IAppIntegrityService>());
        Assert.Null(provider.GetService<IAccessibilityAuditService>());
        Assert.Null(provider.GetService<IStateRestorationService>());
        Assert.Null(provider.GetService<IUpgradeGuard>());
        Assert.Null(provider.GetService<ITrustedTimeService>());
        Assert.Null(provider.GetService<IWalletPassService>());
        Assert.Null(provider.GetService<IPrivacyConsentService>());
        Assert.Null(provider.GetService<IPlusStore>());
    }

    [Fact]
    public void Enabled_Module_Is_Resolved()
    {
        using var provider = TestHarness
            .CreatePlusBuilder(options => options.AccessibilityAudit.Enabled = true)
            .Services
            .BuildServiceProvider();

        Assert.NotNull(provider.GetService<IAccessibilityAuditService>());
        Assert.Null(provider.GetService<ITrustedTimeService>());
        Assert.Null(provider.GetService<IPlusStore>());
    }

    [Fact]
    public void Persistence_Modules_Share_One_Store()
    {
        using var provider = TestHarness
            .CreatePlusBuilder(options =>
            {
                options.StateRestoration.Enabled = true;
                options.PrivacyConsent.Enabled = true;
            })
            .Services
            .BuildServiceProvider();

        var store = provider.GetRequiredService<IPlusStore>();
        Assert.Same(store, provider.GetRequiredService<IPlusStore>());
        Assert.NotNull(provider.GetService<IStateRestorationService>());
        Assert.NotNull(provider.GetService<IPrivacyConsentService>());
    }

    [Fact]
    public void Invalid_Trusted_Time_Duration_Fails_Registration()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiApp<TestApplication>()
            .UseMauiCommunityToolkit();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            builder.UseMauiCommunityToolkitPlus(options =>
            {
                options.TrustedTime.Enabled = true;
                options.TrustedTime.MaxClockSkew = TimeSpan.Zero;
            });
        });

        Assert.Contains("MaxClockSkew", exception.Message);
    }

    [Fact]
    public void Invalid_Upgrade_Threshold_Fails_Registration()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiApp<TestApplication>()
            .UseMauiCommunityToolkit();

        Assert.Throws<InvalidOperationException>(() =>
        {
            builder.UseMauiCommunityToolkitPlus(options =>
            {
                options.UpgradeGuard.Enabled = true;
                options.UpgradeGuard.SafeModeFailureThreshold = 0;
            });
        });
    }

    [Fact]
    public void Invalid_Challenge_Lifetime_Fails_Registration()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MauiApp.CreateBuilder()
                .UseMauiApp<TestApplication>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitPlus(options =>
                {
                    options.AppIntegrity.Enabled = true;
                    options.AppIntegrity.ChallengeLifetime = TimeSpan.Zero;
                }));

        Assert.Contains("ChallengeLifetime", exception.Message);
    }

    [Fact]
    public void Invalid_State_Ttl_Fails_Registration()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MauiApp.CreateBuilder()
                .UseMauiApp<TestApplication>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitPlus(options =>
                {
                    options.StateRestoration.Enabled = true;
                    options.StateRestoration.DefaultTimeToLive = TimeSpan.Zero;
                }));
    }

    [Fact]
    public void Missing_Upgrade_Version_Fails_Registration()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MauiApp.CreateBuilder()
                .UseMauiApp<TestApplication>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitPlus(options =>
                {
                    options.UpgradeGuard.Enabled = true;
                    options.UpgradeGuard.CurrentVersion = " ";
                }));

        Assert.Contains("CurrentVersion", exception.Message);
    }

    [Fact]
    public void Invalid_Trusted_Time_Source_Fails_Registration()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MauiApp.CreateBuilder()
                .UseMauiApp<TestApplication>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitPlus(options =>
                {
                    options.TrustedTime.Enabled = true;
                    options.TrustedTime.Sources.Add(new Uri("ftp://clock.example"));
                }));

        Assert.Contains("HTTP", exception.Message);
    }

    [Fact]
    public void Missing_Consent_Policy_Version_Fails_Registration()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MauiApp.CreateBuilder()
                .UseMauiApp<TestApplication>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitPlus(options =>
                {
                    options.PrivacyConsent.Enabled = true;
                    options.PrivacyConsent.Policy = new ConsentPolicy("", []);
                }));
    }

    [Fact]
    public void Disabled_Modules_Write_No_Store_Files()
    {
        var directory = TestHarness.CreateTempDirectory();
        using var provider = TestHarness
            .CreatePlusBuilder(options => options.StorageDirectory = directory)
            .Services
            .BuildServiceProvider();

        _ = provider.GetService<ICommunityToolkitPlus>();
        _ = provider.GetService<IPlusStore>();

        Assert.Empty(Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void AppIntegrity_Enables_Shared_Store()
    {
        using var provider = TestHarness
            .CreatePlusBuilder(options => options.AppIntegrity.Enabled = true)
            .Services
            .BuildServiceProvider();

        Assert.NotNull(provider.GetService<IAppIntegrityService>());
        Assert.NotNull(provider.GetService<IPlusStore>());
    }

    sealed class TestApplication : Application;
}
