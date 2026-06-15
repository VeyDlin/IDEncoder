using System.Buffers.Binary;
using System.Text;

namespace IDEncoder;


/// <summary>
/// ID codec using Blowfish encryption + Base62. Produces a fixed 11-character string for any long.
/// Thread-safe after construction.
/// </summary>
public sealed class BlowfishIdCodec : IdCodec {
    private const string Base62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private readonly string baseKey;
    private readonly Blowfish cipher;

    /// <summary>Creates a codec from a secret key (1-56 UTF-8 bytes; longer keys are truncated).</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="secretKey"/> is null or empty.</exception>
    public BlowfishIdCodec(string secretKey) {
        if (string.IsNullOrEmpty(secretKey)) {
            throw new ArgumentException("Secret key must not be null or empty.", nameof(secretKey));
        }
        baseKey = secretKey;
        cipher = new Blowfish(MakeKeyBytes(secretKey));
    }

    private BlowfishIdCodec(string baseKey, string effectiveKey) {
        this.baseKey = baseKey;
        cipher = new Blowfish(MakeKeyBytes(effectiveKey));
    }

    /// <summary>Always 11 for Blowfish.</summary>
    public override int MaxEncodedLength => 11;

    protected override IIdCodec CreateWithSalt(string salt) {
        return new BlowfishIdCodec(baseKey, baseKey + ":" + salt);
    }

    public override string Encode(long id) {
        Span<char> buffer = stackalloc char[MaxEncodedLength];
        int written = Encode(id, buffer);
        return new string(buffer[..written]);
    }

    public override int Encode(long id, Span<char> destination) {
        if (destination.Length < MaxEncodedLength) {
            throw new ArgumentException(
                $"Destination must be at least {MaxEncodedLength} characters.",
                nameof(destination)
            );
        }

        ulong unsignedNumber = unchecked((ulong)id);
        Span<byte> input = stackalloc byte[8];
        Span<byte> output = stackalloc byte[8];

        BinaryPrimitives.WriteUInt64LittleEndian(input, unsignedNumber);
        cipher.Encrypt(input, output);

        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(output);

        for (int i = MaxEncodedLength - 1; i >= 0; i--) {
            destination[i] = Base62[(int)(value % 62)];
            value /= 62;
        }
        return MaxEncodedLength;
    }

    public override long Decode(ReadOnlySpan<char> encoded) {
        if (encoded.Length != MaxEncodedLength) {
            throw new ArgumentException("Invalid encoded ID format.", nameof(encoded));
        }

        ulong value = 0;
        foreach (char c in encoded) {
            int index = Base62.IndexOf(c);
            if (index < 0) {
                throw new ArgumentException($"Invalid character in encoded ID: {c}", nameof(encoded));
            }
            if (value > (ulong.MaxValue - (ulong)index) / 62) {
                throw new ArgumentException("Encoded ID is out of range.", nameof(encoded));
            }
            value = (value * 62) + (ulong)index;
        }

        Span<byte> encrypted = stackalloc byte[8];
        Span<byte> output = stackalloc byte[8];

        BinaryPrimitives.WriteUInt64LittleEndian(encrypted, value);
        cipher.Decrypt(encrypted, output);

        ulong unsignedResult = BinaryPrimitives.ReadUInt64LittleEndian(output);
        return unchecked((long)unsignedResult);
    }

    private static byte[] MakeKeyBytes(string key) {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        if (keyBytes.Length > 56) {
            Array.Resize(ref keyBytes, 56);
        }
        return keyBytes;
    }
}
