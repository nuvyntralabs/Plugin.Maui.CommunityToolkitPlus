namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// Presents Apple Wallet or Google Wallet payloads issued by a backend.
/// </summary>
public interface IWalletPassService
{
    /// <summary>Reports what the current platform can do.</summary>
    WalletCapability GetCapability();

    /// <summary>Adds a pass supplied by the host payload provider.</summary>
    Task<WalletOperationResult> AddAsync(
        string passId,
        CancellationToken cancellationToken = default);
}

/// <summary>Supplies a backend-issued wallet payload. Certificates stay on the server.</summary>
public interface IWalletPassPayloadProvider
{
    /// <summary>Loads the payload for a pass identifier.</summary>
    Task<WalletPassPayload?> GetPayloadAsync(string passId, CancellationToken cancellationToken = default);
}

/// <summary>Creates platform-specific wallet presentations.</summary>
public interface IWalletPlatformAdapter
{
    /// <summary>Reports platform capability.</summary>
    WalletCapability GetCapability();

    /// <summary>Presents a payload to the system wallet UI.</summary>
    Task<WalletOperationResult> AddAsync(WalletPassPayload payload, CancellationToken cancellationToken = default);
}

/// <summary>Platform wallet capability flags.</summary>
/// <param name="CanAdd">Whether a pass can be presented for addition.</param>
/// <param name="CanList">Whether the app can list passes it added.</param>
/// <param name="CanUpdate">Whether the app can update an added pass.</param>
/// <param name="CanRemove">Whether the app can remove an added pass.</param>
/// <param name="Platform">android, ios, or unsupported.</param>
public sealed record WalletCapability(
    bool CanAdd,
    bool CanList,
    bool CanUpdate,
    bool CanRemove,
    string Platform);

/// <summary>A backend-issued wallet payload.</summary>
/// <param name="Id">Host pass identifier.</param>
/// <param name="Kind">ticket, loyalty, coupon, or other.</param>
/// <param name="PkPass">Apple Wallet .pkpass bytes, when targeting iOS.</param>
/// <param name="SaveUrl">Google Wallet save URL or JWT save link, when targeting Android.</param>
public sealed record WalletPassPayload(string Id, string Kind, byte[]? PkPass, Uri? SaveUrl);

/// <summary>Outcome of a wallet operation.</summary>
/// <param name="Code">Stable error code, or <see langword="null"/> on success.</param>
/// <param name="Message">Human-readable detail.</param>
public sealed record WalletOperationResult(string? Code, string? Message)
{
    /// <summary>Gets whether the operation succeeded.</summary>
    public bool Succeeded => Code is null;

    /// <summary>Creates a successful result.</summary>
    public static WalletOperationResult Ok() => new(null, null);

    /// <summary>Creates a failed result.</summary>
    public static WalletOperationResult Fail(string code, string message) => new(code, message);
}

/// <summary>Stable wallet error codes.</summary>
public static class WalletErrorCodes
{
    /// <summary>The platform cannot add this pass type.</summary>
    public const string Unsupported = PlusErrorCodes.Unsupported;

    /// <summary>The payload provider returned nothing.</summary>
    public const string MissingPayload = "wallet_missing_payload";

    /// <summary>The payload is missing required platform bytes or URL.</summary>
    public const string InvalidPayload = "wallet_invalid_payload";

    /// <summary>The user cancelled the wallet UI.</summary>
    public const string Cancelled = PlusErrorCodes.Cancelled;
}
