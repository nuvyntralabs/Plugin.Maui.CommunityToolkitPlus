namespace Plugin.Maui.CommunityToolkitPlus;

sealed class TrustedTimeService : ITrustedTimeService
{
    const string StoreName = "trusted-time";

    readonly IReadOnlyList<ITimeSource> _sources;
    readonly TrustedTimeOptions _options;
    readonly TimeProvider _time;
    readonly IPlusStore _store;
    readonly ILogger<TrustedTimeService> _logger;
    readonly SemaphoreSlim _gate = new(1, 1);
    TrustedTimeAnchor? _anchor;

    public TrustedTimeService(
        IEnumerable<ITimeSource> sources,
        TrustedTimeOptions options,
        TimeProvider time,
        IPlusStore store,
        ILogger<TrustedTimeService> logger)
    {
        _sources = sources.ToArray();
        _options = options;
        _time = time;
        _store = store;
        _logger = logger;
    }

    public event EventHandler<TrustedTimeChangedEventArgs>? Changed;

    public TrustedTimeSnapshot? LastSnapshot { get; private set; }

    public async Task<PlusResult<TrustedTimeSnapshot>> GetUtcNowAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureAnchorLoadedAsync(cancellationToken).ConfigureAwait(false);
            if (_anchor is null)
                return await SynchronizeCoreAsync(cancellationToken).ConfigureAwait(false);

            if (HasWallClockJump(_anchor))
            {
                _logger.LogWarning("Detected a device wall-clock jump. Re-synchronizing trusted time.");
                _anchor = null;
                return await SynchronizeCoreAsync(cancellationToken).ConfigureAwait(false);
            }

            var snapshot = CreateSnapshot(_anchor, TrustedTimeConfidence.High);
            if (_time.GetUtcNow() - _anchor.UtcAnchor > _options.OfflineGracePeriod && _sources.Count == 0)
                snapshot = snapshot with { Confidence = TrustedTimeConfidence.Degraded };

            Publish(snapshot);
            return PlusResult<TrustedTimeSnapshot>.Ok(snapshot);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<PlusResult<TrustedTimeSnapshot>> SynchronizeAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureAnchorLoadedAsync(cancellationToken).ConfigureAwait(false);
            return await SynchronizeCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    async Task EnsureAnchorLoadedAsync(CancellationToken cancellationToken)
    {
        if (_anchor is not null)
            return;

        _anchor = await _store.LoadAsync<TrustedTimeAnchor>(StoreName, cancellationToken)
            .ConfigureAwait(false);
    }

    async Task<PlusResult<TrustedTimeSnapshot>> SynchronizeCoreAsync(CancellationToken cancellationToken)
    {
        if (_sources.Count == 0)
        {
            if (_anchor is not null && !HasExpired(_anchor) && !HasWallClockJump(_anchor))
            {
                var degraded = CreateSnapshot(_anchor, TrustedTimeConfidence.Degraded);
                Publish(degraded);
                return PlusResult<TrustedTimeSnapshot>.Ok(degraded);
            }

            return PlusResult<TrustedTimeSnapshot>.Fail(
                PlusErrorCodes.InvalidConfiguration,
                "Trusted Time has no sources configured and no persisted anchor.");
        }

        var readings = new List<DateTimeOffset>();
        foreach (var source in _sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var value = await source.GetUtcAsync(cancellationToken).ConfigureAwait(false);
                if (value is not null)
                    readings.Add(value.Value.ToUniversalTime());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Trusted Time source {Source} failed.", source.Name);
            }
        }

        var accepted = RejectOutliers(readings);
        if (accepted.Count == 0)
        {
            if (_anchor is not null && !HasExpired(_anchor) && !HasWallClockJump(_anchor))
            {
                var degraded = CreateSnapshot(_anchor, TrustedTimeConfidence.Degraded);
                Publish(degraded);
                return PlusResult<TrustedTimeSnapshot>.Ok(degraded);
            }

            return PlusResult<TrustedTimeSnapshot>.Fail(
                PlusErrorCodes.TransientFailure,
                "No trusted-time source produced a usable reading.");
        }

        var sorted = accepted.OrderBy(value => value).ToArray();
        var median = sorted[sorted.Length / 2];
        _anchor = new TrustedTimeAnchor
        {
            UtcAnchor = median,
            MonotonicTimestamp = _time.GetTimestamp(),
            WallClockAtSync = _time.GetUtcNow(),
            SourceCount = accepted.Count
        };
        await _store.SaveAsync(StoreName, _anchor, cancellationToken).ConfigureAwait(false);

        var snapshot = CreateSnapshot(_anchor, TrustedTimeConfidence.High);
        Publish(snapshot);
        return PlusResult<TrustedTimeSnapshot>.Ok(snapshot);
    }

    List<DateTimeOffset> RejectOutliers(List<DateTimeOffset> readings)
    {
        if (readings.Count <= 1)
            return readings;

        var sorted = readings.OrderBy(value => value).ToArray();
        var median = sorted[sorted.Length / 2];
        return readings
            .Where(value => (value - median).Duration() <= _options.MaxClockSkew)
            .ToList();
    }

    bool HasExpired(TrustedTimeAnchor anchor) =>
        _time.GetUtcNow() - anchor.UtcAnchor > _options.OfflineGracePeriod;

    bool HasWallClockJump(TrustedTimeAnchor anchor)
    {
        var expectedWall = anchor.WallClockAtSync +
            _time.GetElapsedTime(anchor.MonotonicTimestamp);
        return (_time.GetUtcNow() - expectedWall).Duration() > _options.MaxClockSkew;
    }

    TrustedTimeSnapshot CreateSnapshot(TrustedTimeAnchor anchor, TrustedTimeConfidence confidence)
    {
        var elapsed = _time.GetElapsedTime(anchor.MonotonicTimestamp);
        return new TrustedTimeSnapshot(
            anchor.UtcAnchor + elapsed,
            confidence,
            anchor.SourceCount,
            anchor.UtcAnchor);
    }

    void Publish(TrustedTimeSnapshot snapshot)
    {
        LastSnapshot = snapshot;
        Changed?.Invoke(this, new TrustedTimeChangedEventArgs(snapshot));
    }
}

sealed class HttpDateTimeSource : ITimeSource
{
    readonly Uri _uri;
    readonly HttpMessageHandler? _handler;

    public HttpDateTimeSource(Uri uri, HttpMessageHandler? handler)
    {
        _uri = uri;
        _handler = handler;
        Name = uri.Host;
    }

    public string Name { get; }

    public async Task<DateTimeOffset?> GetUtcAsync(CancellationToken cancellationToken = default)
    {
        using var client = _handler is null ? new HttpClient() : new HttpClient(_handler, disposeHandler: false);
        using var request = new HttpRequestMessage(HttpMethod.Head, _uri);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.Headers.Date is DateTimeOffset date)
            return date.ToUniversalTime();

        using var get = await client.GetAsync(_uri, cancellationToken).ConfigureAwait(false);
        if (get.Headers.Date is DateTimeOffset fallback)
            return fallback.ToUniversalTime();

        var text = await get.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in new[] { "utc", "utcNow", "timestamp", "date" })
            {
                if (document.RootElement.TryGetProperty(name, out var property) &&
                    DateTimeOffset.TryParse(property.GetString(), CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out var parsed))
                {
                    return parsed.ToUniversalTime();
                }
            }
        }

        return null;
    }
}
