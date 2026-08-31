namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// Provides optional static access to the configured CommunityToolkitPlus facade.
/// Prefer injecting <see cref="ICommunityToolkitPlus"/> in application services.
/// </summary>
public static class CommunityToolkitPlus
{
    static ICommunityToolkitPlus? current;

    /// <summary>
    /// Gets the configured facade.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>UseMauiCommunityToolkitPlus</c> has not been called.
    /// </exception>
    public static ICommunityToolkitPlus Default =>
        current ?? throw new InvalidOperationException(
            "CommunityToolkitPlus is not initialized. Call UseMauiCommunityToolkitPlus in MauiProgram.");

    internal static void SetDefault(ICommunityToolkitPlus implementation) =>
        current = implementation ?? throw new ArgumentNullException(nameof(implementation));

    internal static void Reset() => current = null;
}
