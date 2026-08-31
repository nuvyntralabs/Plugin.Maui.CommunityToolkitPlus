namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// Registers CommunityToolkitPlus with a .NET MAUI application.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers the selected CommunityToolkitPlus modules.
    /// </summary>
    /// <remarks>
    /// Call <c>UseMauiCommunityToolkit</c> before this method. This package does
    /// not initialize the official toolkit a second time. Disabled modules are
    /// not registered and perform no I/O.
    /// </remarks>
    public static MauiAppBuilder UseMauiCommunityToolkitPlus(
        this MauiAppBuilder builder,
        Action<CommunityToolkitPlusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IPopupService)))
        {
            throw new InvalidOperationException(
                "CommunityToolkit.Maui must be initialized first. " +
                "Call UseMauiCommunityToolkit() before UseMauiCommunityToolkitPlus().");
        }

        var options = new CommunityToolkitPlusOptions();
        configure?.Invoke(options);
        options.Validate();

        var implementation = new CommunityToolkitPlusImplementation(options);
        builder.Services.TryAddSingleton(options);
        builder.Services.TryAddSingleton<ICommunityToolkitPlus>(implementation);
        CommunityToolkitPlus.SetDefault(implementation);

        if (implementation.EnabledFeatures.Count == 0)
            return builder;

        RegisterSharedInfrastructure(builder, options, implementation);
        RegisterEnabledModules(builder, options);
        return builder;
    }

    static void RegisterSharedInfrastructure(
        MauiAppBuilder builder,
        CommunityToolkitPlusOptions options,
        ICommunityToolkitPlus plus)
    {
        builder.Services.TryAddSingleton(options.TimeProvider);
        builder.Services.TryAddSingleton(options.AppIntegrity);
        builder.Services.TryAddSingleton(options.AccessibilityAudit);
        builder.Services.TryAddSingleton(options.StateRestoration);
        builder.Services.TryAddSingleton(options.UpgradeGuard);
        builder.Services.TryAddSingleton(options.TrustedTime);
        builder.Services.TryAddSingleton(options.WalletPasses);
        builder.Services.TryAddSingleton(options.PrivacyConsent);

        if (NeedsStore(plus))
        {
            builder.Services.TryAddSingleton<IPlusStore>(services =>
                new AtomicVersionedStore(
                    PlusStorage.ResolveDirectory(options),
                    options.DataProtector ?? services.GetService<IPlusDataProtector>(),
                    services.GetService<ILogger<AtomicVersionedStore>>()));
        }
    }

    static void RegisterEnabledModules(MauiAppBuilder builder, CommunityToolkitPlusOptions options)
    {
        if (options.AppIntegrity.Enabled)
        {
            builder.Services.TryAddSingleton<IIntegrityChallengeProvider>(
                services => new MemoryIntegrityChallengeProvider(services.GetRequiredService<TimeProvider>()));
            builder.Services.TryAddSingleton<IIntegrityPlatformAdapter>(CreateIntegrityAdapter);
            builder.Services.TryAddSingleton<IAppIntegrityService>(services => new AppIntegrityService(
                services.GetRequiredService<IIntegrityChallengeProvider>(),
                services.GetRequiredService<IIntegrityPlatformAdapter>(),
                options.AppIntegrity,
                services.GetRequiredService<TimeProvider>()));
        }

        if (options.AccessibilityAudit.Enabled)
        {
            builder.Services.TryAddSingleton<IAccessibilityAuditService>(services =>
                new AccessibilityAuditService(
                    options.AccessibilityAudit,
                    services.GetRequiredService<TimeProvider>()));
        }

        if (options.StateRestoration.Enabled)
        {
            builder.Services.TryAddSingleton<IStateRestorationService>(services =>
                new StateRestorationService(
                    services.GetRequiredService<IPlusStore>(),
                    options.StateRestoration,
                    services.GetRequiredService<TimeProvider>(),
                    services.GetService<ILogger<StateRestorationService>>()
                        ?? NullLogger<StateRestorationService>.Instance));
        }

        if (options.UpgradeGuard.Enabled)
        {
            builder.Services.TryAddSingleton(services => new UpgradeGuardService(
                services.GetRequiredService<IPlusStore>(),
                options.UpgradeGuard,
                services.GetService<IUpgradeBackupProvider>(),
                services.GetService<ILogger<UpgradeGuardService>>()
                    ?? NullLogger<UpgradeGuardService>.Instance));
            builder.Services.TryAddSingleton<IUpgradeGuard>(services =>
                services.GetRequiredService<UpgradeGuardService>());
            builder.Services.TryAddSingleton<IStartupHealthTracker>(services =>
                services.GetRequiredService<UpgradeGuardService>());
        }

        if (options.TrustedTime.Enabled)
        {
            foreach (var source in options.TrustedTime.Sources)
            {
                var uri = source;
                builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITimeSource>(
                    _ => new HttpDateTimeSource(uri, options.HttpMessageHandler)));
            }

            builder.Services.TryAddSingleton<ITrustedTimeService>(services =>
                new TrustedTimeService(
                    services.GetServices<ITimeSource>(),
                    options.TrustedTime,
                    services.GetRequiredService<TimeProvider>(),
                    services.GetRequiredService<IPlusStore>(),
                    services.GetService<ILogger<TrustedTimeService>>()
                        ?? NullLogger<TrustedTimeService>.Instance));
        }

        if (options.WalletPasses.Enabled)
        {
            builder.Services.TryAddSingleton<IWalletPassPayloadProvider, MissingWalletPayloadProvider>();
            builder.Services.TryAddSingleton<IWalletPlatformAdapter>(CreateWalletAdapter);
            builder.Services.TryAddSingleton<IWalletPassService>(services =>
                new WalletPassService(
                    services.GetRequiredService<IWalletPassPayloadProvider>(),
                    services.GetRequiredService<IWalletPlatformAdapter>()));
        }

        if (options.PrivacyConsent.Enabled)
        {
            builder.Services.TryAddSingleton<IConsentRegionProvider, StaticConsentRegionProvider>();
            builder.Services.TryAddSingleton<IConsentPlatformAdapter>(CreateConsentAdapter);
            builder.Services.TryAddSingleton<IPrivacyConsentService>(services =>
                new PrivacyConsentService(
                    services.GetRequiredService<IPlusStore>(),
                    options.PrivacyConsent,
                    services.GetRequiredService<TimeProvider>(),
                    services.GetService<IConsentPlatformAdapter>(),
                    services.GetService<IPopupService>(),
                    services.GetService<ILogger<PrivacyConsentService>>()
                        ?? NullLogger<PrivacyConsentService>.Instance));
        }
    }

    static bool NeedsStore(ICommunityToolkitPlus plus) =>
        plus.IsEnabled(CommunityToolkitPlusFeature.AppIntegrity)
        || plus.IsEnabled(CommunityToolkitPlusFeature.StateRestoration)
        || plus.IsEnabled(CommunityToolkitPlusFeature.UpgradeGuard)
        || plus.IsEnabled(CommunityToolkitPlusFeature.TrustedTime)
        || plus.IsEnabled(CommunityToolkitPlusFeature.PrivacyConsent);

    static IIntegrityPlatformAdapter CreateIntegrityAdapter(IServiceProvider services)
    {
#if ANDROID
        return new AndroidIntegrityAdapter(services.GetRequiredService<IPlusStore>());
#elif IOS
        return new IosIntegrityAdapter(services.GetRequiredService<IPlusStore>());
#else
        return new UnsupportedIntegrityAdapter();
#endif
    }

    static IWalletPlatformAdapter CreateWalletAdapter(IServiceProvider _)
    {
#if ANDROID
        return new AndroidWalletAdapter();
#elif IOS
        return new IosWalletAdapter();
#else
        return new UnsupportedWalletAdapter();
#endif
    }

    static IConsentPlatformAdapter CreateConsentAdapter(IServiceProvider _)
    {
#if ANDROID
        return new AndroidConsentAdapter();
#elif IOS
        return new IosAttConsentAdapter();
#else
        return new NoOpConsentPlatformAdapter();
#endif
    }
}
