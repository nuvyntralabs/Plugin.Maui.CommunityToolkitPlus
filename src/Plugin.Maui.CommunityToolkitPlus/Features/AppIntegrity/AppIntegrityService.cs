namespace Plugin.Maui.CommunityToolkitPlus;

sealed class AppIntegrityService : IAppIntegrityService
{
    readonly IIntegrityChallengeProvider _challenges;
    readonly IIntegrityPlatformAdapter _platform;
    readonly AppIntegrityOptions _options;
    readonly TimeProvider _time;

    public AppIntegrityService(
        IIntegrityChallengeProvider challenges,
        IIntegrityPlatformAdapter platform,
        AppIntegrityOptions options,
        TimeProvider time)
    {
        _challenges = challenges;
        _platform = platform;
        _options = options;
        _time = time;
    }

    public IntegrityCapability GetCapability() => _platform.GetCapability();

    public Task<IntegrityChallenge> CreateChallengeAsync(CancellationToken cancellationToken = default) =>
        _challenges.CreateAsync(_options.ChallengeLifetime, cancellationToken);

    public Task<IntegrityOperationResult> CreateProofAsync(
        IntegrityChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        cancellationToken.ThrowIfCancellationRequested();

        if (challenge.ExpiresAt <= _time.GetUtcNow())
        {
            return Task.FromResult(IntegrityOperationResult.Fail(
                IntegrityErrorCodes.ChallengeExpired,
                "The integrity challenge has expired and must be issued again."));
        }

        return _platform.CreateProofAsync(challenge, cancellationToken);
    }
}

sealed class MemoryIntegrityChallengeProvider : IIntegrityChallengeProvider
{
    readonly TimeProvider _time;

    public MemoryIntegrityChallengeProvider(TimeProvider time) => _time = time;

    public Task<IntegrityChallenge> CreateAsync(TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime));

        var nonce = new byte[32];
        Random.Shared.NextBytes(nonce);
        var challenge = new IntegrityChallenge(
            Guid.NewGuid().ToString("N"),
            Convert.ToBase64String(nonce),
            _time.GetUtcNow().Add(lifetime));
        return Task.FromResult(challenge);
    }
}

sealed class UnsupportedIntegrityAdapter : IIntegrityPlatformAdapter
{
    public IntegrityCapability GetCapability() => new(false, false, false, "unsupported");

    public Task<IntegrityOperationResult> CreateProofAsync(
        IntegrityChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(IntegrityOperationResult.Fail(
            IntegrityErrorCodes.Unsupported,
            "App Integrity proofs require Android Play Integrity or iOS App Attest."));
    }
}

/// <summary>
/// Adds an integrity proof header to selected HTTP requests. The header is not a local trust verdict.
/// </summary>
public sealed class IntegrityDelegatingHandler : DelegatingHandler
{
    readonly IAppIntegrityService _integrity;
    readonly Func<HttpRequestMessage, bool> _shouldProtect;
    readonly TimeSpan _challengeLifetime;
    readonly TimeProvider _time;

    /// <summary>Creates a handler that protects requests matching <paramref name="shouldProtect"/>.</summary>
    public IntegrityDelegatingHandler(
        IAppIntegrityService integrity,
        Func<HttpRequestMessage, bool> shouldProtect,
        TimeSpan? challengeLifetime = null,
        TimeProvider? time = null)
    {
        _integrity = integrity ?? throw new ArgumentNullException(nameof(integrity));
        _shouldProtect = shouldProtect ?? throw new ArgumentNullException(nameof(shouldProtect));
        _challengeLifetime = challengeLifetime ?? TimeSpan.FromMinutes(2);
        _time = time ?? TimeProvider.System;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_shouldProtect(request))
        {
            var challenge = new IntegrityChallenge(
                Guid.NewGuid().ToString("N"),
                Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                _time.GetUtcNow().Add(_challengeLifetime));
            var result = await _integrity.CreateProofAsync(challenge, cancellationToken).ConfigureAwait(false);
            if (result.Succeeded && result.Proof is not null)
                request.Headers.TryAddWithoutValidation("X-App-Integrity", result.Proof.Payload);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
