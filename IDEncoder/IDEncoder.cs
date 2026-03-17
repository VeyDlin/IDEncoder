using System.Text;

namespace IDEncoder;


/// <summary>
/// Encodes and decodes long IDs into short Base62 strings using Blowfish encryption.
/// Thread-safe after construction. Create once and reuse.
/// </summary>
public sealed class IDEncoder {
    private const string Base62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Length of any encoded ID string (always 11 characters).
    /// </summary>
    public const int EncodedLength = 11;

    private readonly Blowfish encryptCipher;
    private readonly Blowfish decryptCipher;


    /// <summary>
    /// Creates a new encoder with the given secret key.
    /// The same key must be used for encoding and decoding.
    /// </summary>
    /// <param name="secretKey">
    /// Secret key string (1–56 bytes in UTF-8). Keys longer than 56 bytes are silently truncated.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="secretKey"/> is null or empty.</exception>
    public IDEncoder(string secretKey) {
        if (string.IsNullOrEmpty(secretKey)) {
            throw new ArgumentException("Secret key must not be null or empty.", nameof(secretKey));
        }

        byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
        if (keyBytes.Length > 56) {
            Array.Resize(ref keyBytes, 56);
        }

        encryptCipher = new Blowfish(keyBytes);
        decryptCipher = new Blowfish(keyBytes);
    }


    /// <summary>
    /// Encodes a nullable long. Returns null if input is null.
    /// </summary>
    /// <param name="number">The ID to encode, or null.</param>
    /// <returns>An 11-character Base62 string, or null if <paramref name="number"/> is null.</returns>
    public string? EncodeNull(long? number) {
        return number is null ? null : Encode(number.Value);
    }


    /// <summary>
    /// Encodes a long ID into an 11-character Base62 string.
    /// </summary>
    /// <param name="number">The ID to encode. Any long value is valid, including negative.</param>
    /// <returns>An 11-character Base62 string (characters 0-9, A-Z, a-z).</returns>
    public string Encode(long number) {
        Span<char> buffer = stackalloc char[EncodedLength];
        Encode(number, buffer);
        return new string(buffer);
    }


    /// <summary>
    /// Encodes a long ID directly into the destination buffer (zero-alloc).
    /// The encoded result is written to the first <see cref="EncodedLength"/> characters of <paramref name="destination"/>.
    /// </summary>
    /// <param name="number">The ID to encode. Any long value is valid, including negative.</param>
    /// <param name="destination">
    /// Buffer to write the result into. Must have at least <see cref="EncodedLength"/> characters.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination"/> length is less than <see cref="EncodedLength"/>.
    /// </exception>
    public void Encode(long number, Span<char> destination) {
        if (destination.Length < EncodedLength) {
            throw new ArgumentException(
                $"Destination must be at least {EncodedLength} characters.",
                nameof(destination)
            );
        }

        ulong unsignedNumber = unchecked((ulong)number);
        Span<byte> input = stackalloc byte[8];
        Span<byte> output = stackalloc byte[8];

        BitConverter.TryWriteBytes(input, unsignedNumber);
        encryptCipher.Encrypt(input, output);

        ulong value = BitConverter.ToUInt64(output);

        for (int i = EncodedLength - 1; i >= 0; i--) {
            destination[i] = Base62[(int)(value % 62)];
            value /= 62;
        }
    }


    /// <summary>
    /// Decodes a nullable string. Returns null if input is null.
    /// </summary>
    /// <param name="encoded">An 11-character Base62 string, or null.</param>
    /// <returns>The original long ID, or null if <paramref name="encoded"/> is null.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="encoded"/> is not exactly <see cref="EncodedLength"/> characters
    /// or contains characters outside the Base62 alphabet.
    /// </exception>
    public long? DecodeNull(string? encoded) {
        return encoded is null ? null : Decode(encoded);
    }


    /// <summary>
    /// Decodes an 11-character Base62 string back to the original long ID.
    /// </summary>
    /// <param name="encoded">
    /// An 11-character Base62 string previously produced by <see cref="Encode(long)"/>.
    /// </param>
    /// <returns>The original long ID.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="encoded"/> is not exactly <see cref="EncodedLength"/> characters
    /// or contains characters outside the Base62 alphabet (0-9, A-Z, a-z).
    /// </exception>
    public long Decode(string encoded) {
        return Decode(encoded.AsSpan());
    }


    /// <summary>
    /// Decodes a Base62 span back to the original long ID (zero-alloc).
    /// </summary>
    /// <param name="encoded">
    /// A span of exactly <see cref="EncodedLength"/> Base62 characters
    /// previously produced by <see cref="Encode(long, Span{char})"/>.
    /// </param>
    /// <returns>The original long ID.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="encoded"/> length is not exactly <see cref="EncodedLength"/>
    /// or contains characters outside the Base62 alphabet (0-9, A-Z, a-z).
    /// </exception>
    public long Decode(ReadOnlySpan<char> encoded) {
        if (encoded.Length != EncodedLength) {
            throw new ArgumentException("Invalid encoded ID format.", nameof(encoded));
        }

        ulong value = 0;
        foreach (char c in encoded) {
            int index = Base62.IndexOf(c);
            if (index < 0) {
                throw new ArgumentException($"Invalid character in encoded ID: {c}", nameof(encoded));
            }
            value = (value * 62) + (ulong)index;
        }

        Span<byte> encrypted = stackalloc byte[8];
        Span<byte> output = stackalloc byte[8];

        BitConverter.TryWriteBytes(encrypted, value);
        decryptCipher.Decrypt(encrypted, output);

        ulong unsignedResult = BitConverter.ToUInt64(output);
        return unchecked((long)unsignedResult);
    }
}
