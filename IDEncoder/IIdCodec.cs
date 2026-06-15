namespace IDEncoder;


/// <summary>
/// Encodes and decodes long IDs into short strings. Implementations must be thread-safe
/// after construction. Salt is applied via <see cref="WithSalt"/>.
/// </summary>
public interface IIdCodec {
    /// <summary>Exact upper bound on the length of any encoded string (for buffer sizing).</summary>
    int MaxEncodedLength { get; }

    /// <summary>Encodes an ID into a new string.</summary>
    string Encode(long id);

    /// <summary>Encodes an ID into <paramref name="destination"/>; returns the number of characters written.</summary>
    int Encode(long id, Span<char> destination);

    /// <summary>Decodes a previously encoded value back to the original ID.</summary>
    /// <exception cref="ArgumentException">Thrown when the input is not a valid encoding.</exception>
    long Decode(ReadOnlySpan<char> encoded);

    /// <summary>Returns a salted variation of this codec. Implementations should cache variations.</summary>
    IIdCodec WithSalt(string salt);
}
