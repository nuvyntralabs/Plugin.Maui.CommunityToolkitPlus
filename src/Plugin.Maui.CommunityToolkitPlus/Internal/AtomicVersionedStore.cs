using System.Collections.Concurrent;

namespace Plugin.Maui.CommunityToolkitPlus;

sealed class AtomicVersionedStore : IPlusStore
{
    readonly string _directory;
    readonly IPlusDataProtector? _protector;
    readonly ILogger _logger;
    readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);

    public AtomicVersionedStore(
        string directory,
        IPlusDataProtector? protector,
        ILogger? logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
        _protector = protector;
        _logger = logger ?? NullLogger.Instance;
        Directory.CreateDirectory(_directory);
    }

    public async Task<T?> LoadAsync<T>(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var gate = await EnterAsync(name, cancellationToken).ConfigureAwait(false);
        try
        {
            var path = GetPath(name);
            var backup = GetBackupPath(name);

            if (TryRead(path, out T? value))
                return value;

            if (TryRead(backup, out value))
            {
                _logger.LogWarning("Recovered {Store} from the last valid snapshot.", name);
                return value;
            }

            return default;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SaveAsync<T>(string name, T value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);
        var gate = await EnterAsync(name, cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(_directory);
            var path = GetPath(name);
            var backup = GetBackupPath(name);
            var temp = path + ".tmp";

            var payload = JsonSerializer.SerializeToElement(value, typeof(T), (JsonSerializerContext)PlusJsonContext.Default);
            var document = new StoredDocument
            {
                SchemaVersion = 1,
                DataVersion = 1,
                Payload = payload
            };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(document, PlusJsonContext.Default.StoredDocument);
            if (_protector is not null)
                bytes = _protector.Protect(bytes);

            await File.WriteAllBytesAsync(temp, bytes, cancellationToken).ConfigureAwait(false);

            if (File.Exists(path))
                File.Copy(path, backup, overwrite: true);

            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var gate = await EnterAsync(name, cancellationToken).ConfigureAwait(false);
        try
        {
            var path = GetPath(name);
            var backup = GetBackupPath(name);
            if (File.Exists(path))
                File.Delete(path);
            if (File.Exists(backup))
                File.Delete(backup);
        }
        finally
        {
            gate.Release();
        }
    }

    bool TryRead<T>(string path, out T? value)
    {
        value = default;
        if (!File.Exists(path))
            return false;

        try
        {
            var bytes = File.ReadAllBytes(path);
            if (_protector is not null)
                bytes = _protector.Unprotect(bytes);

            var document = JsonSerializer.Deserialize(bytes, PlusJsonContext.Default.StoredDocument);
            if (document is null)
                return false;

            value = (T?)document.Payload.Deserialize(typeof(T), PlusJsonContext.Default);
            return value is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ignored a corrupt CommunityToolkitPlus store file.");
            return false;
        }
    }

    Task<SemaphoreSlim> EnterAsync(string name, CancellationToken cancellationToken)
    {
        var gate = _gates.GetOrAdd(name, static _ => new SemaphoreSlim(1, 1));
        return WaitAsync(gate, cancellationToken);
    }

    static async Task<SemaphoreSlim> WaitAsync(SemaphoreSlim gate, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return gate;
    }

    string GetPath(string name) => Path.Combine(_directory, name + ".json");

    string GetBackupPath(string name) => Path.Combine(_directory, name + ".json.bak");
}

static class PlusStorage
{
    public const string FolderName = "community-toolkit-plus";

    public static string ResolveDirectory(CommunityToolkitPlusOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.StorageDirectory))
            return options.StorageDirectory;

        return Path.Combine(FileSystem.AppDataDirectory, FolderName);
    }
}
