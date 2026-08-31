using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;

namespace Plugin.Maui.CommunityToolkitPlus.Tests;

static class TestHarness
{
    public static MauiAppBuilder CreatePlusBuilder(Action<CommunityToolkitPlusOptions>? configure = null)
    {
        CommunityToolkitPlus.Reset();

        return MauiApp
            .CreateBuilder()
            .UseMauiApp<TestApplication>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitPlus(options =>
            {
                options.StorageDirectory = CreateTempDirectory();
                configure?.Invoke(options);
            });
    }

    public static ICommunityToolkitPlus CreateFacade(Action<CommunityToolkitPlusOptions>? configure = null)
    {
        var options = new CommunityToolkitPlusOptions();
        configure?.Invoke(options);
        return new CommunityToolkitPlusImplementation(options);
    }

    public static IReadOnlyList<CommunityToolkitPlusFeature> AllFeatures { get; } =
    [
        CommunityToolkitPlusFeature.AppIntegrity,
        CommunityToolkitPlusFeature.AccessibilityAudit,
        CommunityToolkitPlusFeature.StateRestoration,
        CommunityToolkitPlusFeature.UpgradeGuard,
        CommunityToolkitPlusFeature.TrustedTime,
        CommunityToolkitPlusFeature.WalletPasses,
        CommunityToolkitPlusFeature.PrivacyConsent
    ];

    public static string CreateTempDirectory() =>
        Directory.CreateTempSubdirectory("ctp-tests-").FullName;

    sealed class TestApplication : Application;
}

sealed class TestTimeProvider : TimeProvider
{
    DateTimeOffset _utc;
    long _timestamp;

    public TestTimeProvider(DateTimeOffset utc)
    {
        _utc = utc;
        _timestamp = 0;
    }

    public override DateTimeOffset GetUtcNow() => _utc;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => _timestamp;

    public void Advance(TimeSpan delta)
    {
        _utc += delta;
        _timestamp += delta.Ticks;
    }

    public void JumpWallClock(TimeSpan delta) => _utc += delta;
}
