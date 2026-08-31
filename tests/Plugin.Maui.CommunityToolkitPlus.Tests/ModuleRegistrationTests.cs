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

    sealed class TestApplication : Application;
}
