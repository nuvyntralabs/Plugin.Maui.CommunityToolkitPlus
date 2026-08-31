namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// A structured outcome for expected CommunityToolkitPlus failures.
/// </summary>
/// <param name="Code">Stable error code, or <see langword="null"/> on success.</param>
/// <param name="Message">Human-readable detail that must not contain secrets.</param>
public readonly record struct PlusResult(string? Code, string? Message)
{
    /// <summary>Gets a successful result.</summary>
    public static PlusResult Success { get; } = new(null, null);

    /// <summary>Gets whether the operation succeeded.</summary>
    public bool Succeeded => Code is null;

    /// <summary>Creates a failed result.</summary>
    public static PlusResult Fail(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(code, message);
    }

    /// <summary>Gets a result that reports the current platform cannot perform the operation.</summary>
    public static PlusResult Unsupported(string operation) =>
        Fail(PlusErrorCodes.Unsupported, $"{operation} is not supported on this platform.");
}

/// <summary>
/// Stable error codes shared across CommunityToolkitPlus modules.
/// </summary>
public static class PlusErrorCodes
{
    /// <summary>The current platform or build cannot perform the operation.</summary>
    public const string Unsupported = "unsupported";

    /// <summary>The caller cancelled the operation.</summary>
    public const string Cancelled = "cancelled";

    /// <summary>A required host configuration value is missing or invalid.</summary>
    public const string InvalidConfiguration = "invalid_configuration";

    /// <summary>Stored data is missing, expired, or failed schema validation.</summary>
    public const string InvalidState = "invalid_state";

    /// <summary>A platform or network call failed transiently.</summary>
    public const string TransientFailure = "transient_failure";

    /// <summary>The operation was denied by policy or the user.</summary>
    public const string Denied = "denied";
}

/// <summary>
/// A structured outcome that may include a value.
/// </summary>
/// <typeparam name="T">The success payload type.</typeparam>
public readonly record struct PlusResult<T>(T? Value, string? Code, string? Message)
{
    /// <summary>Gets whether the operation succeeded.</summary>
    public bool Succeeded => Code is null;

    /// <summary>Creates a successful result.</summary>
    public static PlusResult<T> Ok(T value) => new(value, null, null);

    /// <summary>Creates a failed result.</summary>
    public static PlusResult<T> Fail(string code, string message) =>
        new(default, PlusResult.Fail(code, message).Code, message);

    /// <summary>Gets a result that reports the current platform cannot perform the operation.</summary>
    public static PlusResult<T> Unsupported(string operation) =>
        Fail(PlusErrorCodes.Unsupported, $"{operation} is not supported on this platform.");
}
