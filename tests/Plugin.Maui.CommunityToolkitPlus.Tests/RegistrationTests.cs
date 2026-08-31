using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;

namespace Plugin.Maui.CommunityToolkitPlus.Tests;

public sealed class RegistrationTests : IDisposable
{
    public RegistrationTests() => CommunityToolkitPlus.Reset();

    public void Dispose() => CommunityToolkitPlus.Reset();

    [Fact]
    public void Registration_Requires_Official_Toolkit()
    {
        var builder = MauiApp.CreateBuilder();

        var exception = Assert.Throws<InvalidOperationException>(
            () => builder.UseMauiCommunityToolkitPlus());

        Assert.Contains("UseMauiCommunityToolkit()", exception.Message);
        Assert.DoesNotContain(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(ICommunityToolkitPlus));
    }

    [Fact]
    public void Registration_Throws_When_Builder_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(
            () => MauiAppBuilderExtensions.UseMauiCommunityToolkitPlus(null!));
    }

    [Fact]
    public void Registration_Returns_Same_Builder()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiApp<TestApplication>()
            .UseMauiCommunityToolkit();

        var returned = builder.UseMauiCommunityToolkitPlus();

        Assert.Same(builder, returned);
    }

    [Fact]
    public void Registration_Keeps_Official_Toolkit_Services()
    {
        var builder = TestHarness.CreatePlusBuilder();

        Assert.Contains(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(IPopupService));
    }

    [Fact]
    public void Registration_Adds_Options_And_Facade_As_Singletons()
    {
        var builder = TestHarness.CreatePlusBuilder();

        Assert.Contains(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(CommunityToolkitPlusOptions)
                && descriptor.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(ICommunityToolkitPlus)
                && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void Registration_Resolves_Configured_Facade()
    {
        using var provider = TestHarness
            .CreatePlusBuilder(options =>
            {
                options.AccessibilityAudit.Enabled = true;
                options.StateRestoration.Enabled = true;
            })
            .Services
            .BuildServiceProvider();

        var plus = provider.GetRequiredService<ICommunityToolkitPlus>();
        var options = provider.GetRequiredService<CommunityToolkitPlusOptions>();

        Assert.True(plus.IsEnabled(CommunityToolkitPlusFeature.AccessibilityAudit));
        Assert.True(plus.IsEnabled(CommunityToolkitPlusFeature.StateRestoration));
        Assert.False(plus.IsEnabled(CommunityToolkitPlusFeature.AppIntegrity));
        Assert.True(options.AccessibilityAudit.Enabled);
        Assert.True(options.StateRestoration.Enabled);
    }

    [Fact]
    public void Registration_Allows_Null_Configure()
    {
        using var provider = TestHarness
            .CreatePlusBuilder()
            .Services
            .BuildServiceProvider();

        var plus = provider.GetRequiredService<ICommunityToolkitPlus>();

        Assert.Empty(plus.EnabledFeatures);
        Assert.All(TestHarness.AllFeatures, feature => Assert.False(plus.IsEnabled(feature)));
    }

    [Fact]
    public void Registration_Enables_All_Modules_When_Configured()
    {
        using var provider = TestHarness
            .CreatePlusBuilder(EnableAll)
            .Services
            .BuildServiceProvider();

        var plus = provider.GetRequiredService<ICommunityToolkitPlus>();

        Assert.Equal(TestHarness.AllFeatures.Count, plus.EnabledFeatures.Count);
        Assert.All(TestHarness.AllFeatures, feature => Assert.True(plus.IsEnabled(feature)));
    }

    [Fact]
    public void Registration_Does_Not_Call_Official_Toolkit_Twice()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiApp<TestApplication>()
            .UseMauiCommunityToolkit();

        var popupRegistrations = builder.Services.Count(
            descriptor => descriptor.ServiceType == typeof(IPopupService));

        builder.UseMauiCommunityToolkitPlus();

        Assert.Equal(
            popupRegistrations,
            builder.Services.Count(descriptor => descriptor.ServiceType == typeof(IPopupService)));
    }

    [Fact]
    public void Registration_Sets_Static_Default()
    {
        TestHarness.CreatePlusBuilder(options => options.TrustedTime.Enabled = true);

        Assert.True(CommunityToolkitPlus.Default.IsEnabled(CommunityToolkitPlusFeature.TrustedTime));
        Assert.False(CommunityToolkitPlus.Default.IsEnabled(CommunityToolkitPlusFeature.WalletPasses));
    }

    static void EnableAll(CommunityToolkitPlusOptions options)
    {
        options.AppIntegrity.Enabled = true;
        options.AccessibilityAudit.Enabled = true;
        options.StateRestoration.Enabled = true;
        options.UpgradeGuard.Enabled = true;
        options.TrustedTime.Enabled = true;
        options.WalletPasses.Enabled = true;
        options.PrivacyConsent.Enabled = true;
    }

    sealed class TestApplication : Application;
}
