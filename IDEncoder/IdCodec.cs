using System.Collections.Concurrent;

namespace IDEncoder;


/// <summary>
/// Base class for <see cref="IIdCodec"/> implementations. Provides a thread-safe cache of
/// salted variations; subclasses implement <see cref="CreateWithSalt"/> plus the codec members.
/// </summary>
public abstract class IdCodec : IIdCodec {
    private readonly ConcurrentDictionary<string, IIdCodec> saltedVariations = new();

    /// <inheritdoc/>
    public abstract int MaxEncodedLength { get; }

    /// <inheritdoc/>
    public abstract string Encode(long id);

    /// <inheritdoc/>
    public abstract int Encode(long id, Span<char> destination);

    /// <inheritdoc/>
    public abstract long Decode(ReadOnlySpan<char> encoded);

    /// <inheritdoc/>
    public IIdCodec WithSalt(string salt) {
        if (string.IsNullOrEmpty(salt)) {
            return this;
        }
        return saltedVariations.GetOrAdd(salt, CreateWithSalt);
    }

    /// <summary>
    /// Creates a salted variation derived from the base (unsalted) codec.
    /// Implementations must derive from the base parameters, not chain onto an already-salted state.
    /// </summary>
    protected abstract IIdCodec CreateWithSalt(string salt);
}
