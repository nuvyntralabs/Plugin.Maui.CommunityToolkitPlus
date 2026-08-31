namespace Plugin.Maui.CommunityToolkitPlus;

/// <summary>
/// Atomic, versioned persistence used by CommunityToolkitPlus modules.
/// </summary>
public interface IPlusStore
{
    /// <summary>Reads and deserializes a named document, or returns the default when missing.</summary>
    Task<T?> LoadAsync<T>(string name, CancellationToken cancellationToken = default);

    /// <summary>Atomically writes a named document.</summary>
    Task SaveAsync<T>(string name, T value, CancellationToken cancellationToken = default);

    /// <summary>Deletes a named document when it exists.</summary>
    Task DeleteAsync(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional protection for sensitive persisted payloads.
/// </summary>
public interface IPlusDataProtector
{
    /// <summary>Protects a UTF-8 payload before it is written to disk.</summary>
    byte[] Protect(ReadOnlySpan<byte> plaintext);

    /// <summary>Reverses <see cref="Protect"/>.</summary>
    byte[] Unprotect(ReadOnlySpan<byte> protectedBytes);
}
