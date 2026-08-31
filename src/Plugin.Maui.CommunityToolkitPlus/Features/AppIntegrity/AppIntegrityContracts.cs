namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// Issues and attests integrity challenges. The client never reports a local trusted verdict.
/// </summary>
public interface IAppIntegrityService
{
    /// <summary>Reports what the current platform can do.</summary>
    IntegrityCapability GetCapability();

    /// <summary>Creates a challenge that a backend can later verify with the platform proof.</summary>
    Task<IntegrityChallenge> CreateChallengeAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates an opaque platform proof for a challenge. Only a backend can verify it.</summary>
    Task<IntegrityOperationResult> CreateProofAsync(
        IntegrityChallenge challenge,
        CancellationToken cancellationToken = default);
}

/// <summary>Supplies integrity challenges. Hosts may replace the default in-memory provider.</summary>
public interface IIntegrityChallengeProvider
{
    /// <summary>Creates a challenge with expiry and a replay identifier.</summary>
    Task<IntegrityChallenge> CreateAsync(TimeSpan lifetime, CancellationToken cancellationToken = default);
}

/// <summary>Creates platform-specific integrity material.</summary>
public interface IIntegrityPlatformAdapter
{
    /// <summary>Reports platform capability.</summary>
    IntegrityCapability GetCapability();

    /// <summary>Creates an attestation or assertion for the supplied challenge.</summary>
    Task<IntegrityOperationResult> CreateProofAsync(
        IntegrityChallenge challenge,
        CancellationToken cancellationToken = default);
}

/// <summary>A nonce and replay identifier that must be verified by a backend.</summary>
/// <param name="Id">Replay identifier.</param>
/// <param name="Nonce">Opaque challenge bytes encoded as Base64Url.</param>
/// <param name="ExpiresAt">UTC expiry.</param>
public sealed record IntegrityChallenge(string Id, string Nonce, DateTimeOffset ExpiresAt);

/// <summary>Opaque platform proof. The payload is not a local trust decision.</summary>
/// <param name="ChallengeId">The challenge this proof answers.</param>
/// <param name="Platform">android or ios.</param>
/// <param name="Payload">Opaque platform token or attestation.</param>
/// <param name="KeyId">Optional App Attest key identifier.</param>
public sealed record IntegrityProof(string ChallengeId, string Platform, string Payload, string? KeyId);

/// <summary>What the current device can do for app integrity.</summary>
/// <param name="IsSupported">Whether any integrity API is available.</param>
/// <param name="CanAttest">Whether a first-time attestation can be created.</param>
/// <param name="CanAssert">Whether a later assertion can be created.</param>
/// <param name="Platform">android, ios, or unsupported.</param>
public sealed record IntegrityCapability(bool IsSupported, bool CanAttest, bool CanAssert, string Platform);

/// <summary>Outcome of an integrity proof request.</summary>
/// <param name="Proof">The opaque proof when successful.</param>
/// <param name="Code">Stable error code, or <see langword="null"/> on success.</param>
/// <param name="Message">Human-readable detail that must not contain secrets.</param>
public sealed record IntegrityOperationResult(IntegrityProof? Proof, string? Code, string? Message)
{
    /// <summary>Gets whether a proof was created.</summary>
    public bool Succeeded => Proof is not null && Code is null;

    /// <summary>Creates a successful result.</summary>
    public static IntegrityOperationResult Ok(IntegrityProof proof) => new(proof, null, null);

    /// <summary>Creates a failed result.</summary>
    public static IntegrityOperationResult Fail(string code, string message) => new(null, code, message);
}

/// <summary>Stable integrity error codes.</summary>
public static class IntegrityErrorCodes
{
    /// <summary>The challenge is missing, expired, or already consumed.</summary>
    public const string ChallengeExpired = "integrity_challenge_expired";

    /// <summary>The platform cannot create proofs on this device.</summary>
    public const string Unsupported = PlusErrorCodes.Unsupported;

    /// <summary>The App Attest key was lost and must be regenerated.</summary>
    public const string KeyLost = "integrity_key_lost";

    /// <summary>A transient platform or network failure occurred.</summary>
    public const string TransientFailure = PlusErrorCodes.TransientFailure;

    /// <summary>The caller cancelled the operation.</summary>
    public const string Cancelled = PlusErrorCodes.Cancelled;
}

sealed class IntegrityKeyRecord
{
    public string? KeyId { get; set; }
    public string? Platform { get; set; }
}
