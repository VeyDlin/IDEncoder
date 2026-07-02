namespace IDEncoder;


/// <summary>
/// Facade for encoding/decoding long IDs into short strings. Delegates to an <see cref="IIdCodec"/>
/// (default <see cref="BlowfishIdCodec"/>). Thread-safe after construction. Create once and reuse.
/// </summary>
public sealed class IDEncoder {
    /// <summary>
    /// Length of a Blowfish-encoded ID (always 11). Blowfish-specific — for general zero-alloc
    /// buffer sizing use <see cref="MaxEncodedLength"/>, which reflects the active codec.
    /// </summary>
    [Obsolete("Use MaxEncodedLength for buffer sizing, or BlowfishIdCodec.EncodedLength for the Blowfish-specific constant.")]
    public const int EncodedLength = BlowfishIdCodec.EncodedLength;

    private readonly IIdCodec codec;

    /// <summary>Creates an encoder using Blowfish with the given secret key.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="secretKey"/> is null or empty.</exception>
    public IDEncoder(string secretKey) : this(new BlowfishIdCodec(secretKey)) {
    }

    /// <summary>Creates an encoder using the given codec. The codec must be the base, unsalted codec.</summary>
    public IDEncoder(IIdCodec codec) {
        this.codec = codec ?? throw new ArgumentNullException(nameof(codec));
    }

    /// <summary>The codec this facade delegates to.</summary>
    internal IIdCodec Codec => codec;

    /// <summary>Exact upper bound on encoded length for the active codec (for zero-alloc buffers).</summary>
    public int MaxEncodedLength => codec.MaxEncodedLength;

    /// <summary>Encodes a nullable long. Returns null if input is null.</summary>
    public string? EncodeNull(long? number, string? salt = null) {
        return number is null ? null : Encode(number.Value, salt);
    }

    /// <summary>Encodes a long ID into a short string.</summary>
    public string Encode(long number, string? salt = null) {
        return Resolve(salt).Encode(number);
    }

    /// <summary>Encodes a long ID into <paramref name="destination"/> (zero-alloc); returns characters written.</summary>
    public int Encode(long number, Span<char> destination, string? salt = null) {
        return Resolve(salt).Encode(number, destination);
    }

    /// <summary>Decodes a nullable string. Returns null if input is null.</summary>
    public long? DecodeNull(string? encoded, string? salt = null) {
        return encoded is null ? null : Decode(encoded, salt);
    }

    /// <summary>Decodes an encoded string back to the original long ID.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="encoded"/> is not a valid encoding.</exception>
    public long Decode(string encoded, string? salt = null) {
        return Decode(encoded.AsSpan(), salt);
    }

    /// <summary>Decodes an encoded span back to the original long ID (zero-alloc).</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="encoded"/> is not a valid encoding.</exception>
    public long Decode(ReadOnlySpan<char> encoded, string? salt = null) {
        return Resolve(salt).Decode(encoded);
    }

    private IIdCodec Resolve(string? salt) {
        return string.IsNullOrEmpty(salt) ? codec : codec.WithSalt(salt);
    }
}
