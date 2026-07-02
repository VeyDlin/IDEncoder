using System.Text;

namespace IDEncoder;


/// <summary>
/// A simple, non-secure ID codec inspired by Sqids: short, variable-length strings where
/// sequential IDs look dissimilar, with no secret key. This is obfuscation, not cryptography —
/// encodings are reversible without any secret. Not wire-compatible with sqids.org.
/// </summary>
public sealed class SqidsIdCodec : IdCodec {
    private const string BaseAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int AlphabetLength = 62;

    private readonly char[] shuffled;

    /// <summary>Creates a codec with the default (unsalted) shuffled alphabet.</summary>
    public SqidsIdCodec() : this(null) {
    }

    private SqidsIdCodec(string? salt) {
        shuffled = ShuffleAlphabet(salt);
    }

    /// <summary>11 base-62 digits for ulong.MaxValue + 1 prefix character.</summary>
    public override int MaxEncodedLength => 12;

    /// <summary>For tests: the permuted alphabet of this instance.</summary>
    internal ReadOnlySpan<char> ShuffledAlphabet => shuffled;

    /// <inheritdoc/>
    protected override IIdCodec CreateWithSalt(string salt) {
        return new SqidsIdCodec(salt);
    }

    /// <inheritdoc/>
    public override string Encode(long id) {
        Span<char> buffer = stackalloc char[MaxEncodedLength];
        int written = Encode(id, buffer);
        return new string(buffer[..written]);
    }

    /// <inheritdoc/>
    public override int Encode(long id, Span<char> destination) {
        if (destination.Length < MaxEncodedLength) {
            throw new ArgumentException(
                $"Destination must be at least {MaxEncodedLength} characters.",
                nameof(destination)
            );
        }

        ulong u = Zigzag(id);
        int offset = Mix(u);

        destination[0] = shuffled[offset];   // prefix encodes the offset

        Span<char> digits = stackalloc char[MaxEncodedLength - 1];
        int count = 0;
        do {
            int rem = (int)(u % AlphabetLength);
            digits[count] = shuffled[(offset + rem) % AlphabetLength];
            count++;
            u /= AlphabetLength;
        } while (u > 0);

        for (int i = 0; i < count; i++) {
            destination[1 + i] = digits[count - 1 - i];
        }
        return 1 + count;
    }

    /// <inheritdoc/>
    public override long Decode(ReadOnlySpan<char> encoded) {
        if (encoded.Length < 2 || encoded.Length > MaxEncodedLength) {
            throw new ArgumentException("Invalid encoded ID format.", nameof(encoded));
        }

        int offset = IndexInShuffled(encoded[0]);
        if (offset < 0) {
            throw new ArgumentException($"Invalid character in encoded ID: {encoded[0]}", nameof(encoded));
        }

        ulong u = 0;
        for (int i = 1; i < encoded.Length; i++) {
            int digit = RotatedIndex(offset, encoded[i]);
            if (digit < 0) {
                throw new ArgumentException($"Invalid character in encoded ID: {encoded[i]}", nameof(encoded));
            }
            if (u > (ulong.MaxValue - (ulong)digit) / AlphabetLength) {
                throw new ArgumentException("Encoded ID is out of range.", nameof(encoded));
            }
            u = (u * AlphabetLength) + (ulong)digit;
        }

        long id = Unzigzag(u);

        Span<char> reencoded = stackalloc char[MaxEncodedLength];
        int written = Encode(id, reencoded);
        if (!encoded.SequenceEqual(reencoded[..written])) {
            throw new ArgumentException("Non-canonical encoded ID.", nameof(encoded));
        }

        return id;
    }

    private int RotatedIndex(int offset, char c) {
        int idx = IndexInShuffled(c);
        if (idx < 0) {
            return -1;
        }
        return ((idx - offset) % AlphabetLength + AlphabetLength) % AlphabetLength;
    }

    private int IndexInShuffled(char c) {
        for (int i = 0; i < AlphabetLength; i++) {
            if (shuffled[i] == c) {
                return i;
            }
        }
        return -1;
    }

    private static char[] ShuffleAlphabet(string? salt) {
        char[] alphabet = BaseAlphabet.ToCharArray();
        byte[] saltBytes = salt is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(salt);
        ulong state = Fnv1a(saltBytes);

        for (int i = alphabet.Length - 1; i > 0; i--) {
            int j = (int)(NextRandom(ref state) % (ulong)(i + 1));
            (alphabet[i], alphabet[j]) = (alphabet[j], alphabet[i]);
        }
        return alphabet;
    }

    private static ulong Fnv1a(ReadOnlySpan<byte> data) {
        ulong hash = 14695981039346656037UL;
        foreach (byte b in data) {
            hash ^= b;
            hash *= 1099511628211UL;
        }
        return hash;
    }

    private static ulong NextRandom(ref ulong state) {
        state += 0x9E3779B97F4A7C15UL;
        ulong z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    private static int Mix(ulong u) {
        ulong z = u + 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z = z ^ (z >> 31);
        return (int)(z % AlphabetLength);
    }

    private static ulong Zigzag(long n) {
        return unchecked((ulong)((n << 1) ^ (n >> 63)));
    }

    private static long Unzigzag(ulong u) {
        return unchecked((long)((u >> 1) ^ (0UL - (u & 1UL))));
    }
}
