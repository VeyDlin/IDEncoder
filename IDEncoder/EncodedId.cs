using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace IDEncoder;


/// <summary>
/// A wrapper around long that automatically encodes/decodes to Base62 strings
/// at JSON and route-binding boundaries. Inside the application it behaves as a plain long.
/// Requires <see cref="IDEncoder"/> to be configured via DI before serialization.
/// </summary>
[JsonConverter(typeof(EncodedIdConverter))]
public readonly struct EncodedId : IEquatable<EncodedId>, IParsable<EncodedId> {
    /// <summary>
    /// The underlying numeric ID.
    /// </summary>
    public long Value { get; }


    /// <summary>
    /// Creates an <see cref="EncodedId"/> from a raw numeric value.
    /// </summary>
    /// <param name="value">The underlying long ID.</param>
    public EncodedId(long value) {
        Value = value;
    }


    /// <summary>
    /// Implicit conversion from long — allows assigning database IDs directly to EncodedId fields.
    /// </summary>
    /// <param name="value">The long value to wrap.</param>
    public static implicit operator EncodedId(long value) => new(value);

    /// <summary>
    /// Implicit conversion to long — allows using EncodedId in queries and comparisons without <see cref="Value"/>.
    /// </summary>
    /// <param name="id">The EncodedId to unwrap.</param>
    public static implicit operator long(EncodedId id) => id.Value;


    /// <inheritdoc/>
    public static bool operator ==(EncodedId left, EncodedId right) => left.Value == right.Value;

    /// <inheritdoc/>
    public static bool operator !=(EncodedId left, EncodedId right) => left.Value != right.Value;


    /// <inheritdoc/>
    public bool Equals(EncodedId other) => Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is EncodedId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();


    /// <summary>
    /// Returns the encoded Base62 string if the encoder is configured,
    /// otherwise falls back to the numeric value as a string.
    /// </summary>
    /// <returns>
    /// An 11-character Base62 string when the encoder is configured,
    /// or the decimal representation of <see cref="Value"/> otherwise.
    /// </returns>
    public override string ToString() {
        var encoder = EncodedIdConverter.Encoder;
        if (encoder is null) {
            return Value.ToString();
        }
        return encoder.Encode(Value);
    }


    /// <summary>
    /// Parses a Base62-encoded string into an <see cref="EncodedId"/>.
    /// Used by ASP.NET Core for route and query parameter binding.
    /// </summary>
    /// <param name="s">An 11-character Base62 string to decode.</param>
    /// <param name="provider">Ignored. Present to satisfy <see cref="IParsable{TSelf}"/>.</param>
    /// <returns>An <see cref="EncodedId"/> containing the decoded long value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IDEncoder"/> is not configured via DI.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="s"/> is not a valid 11-character Base62 string.
    /// </exception>
    public static EncodedId Parse(string s, IFormatProvider? provider) {
        var encoder = EncodedIdConverter.Encoder
            ?? throw new InvalidOperationException("IDEncoder is not configured. Call services.AddIDEncoder() first.");
        return new EncodedId(encoder.Decode(s));
    }


    /// <summary>
    /// Tries to parse a Base62-encoded string into an <see cref="EncodedId"/>.
    /// Returns false if the string is invalid or the encoder is not configured.
    /// </summary>
    /// <param name="s">An 11-character Base62 string to decode, or null.</param>
    /// <param name="provider">Ignored. Present to satisfy <see cref="IParsable{TSelf}"/>.</param>
    /// <param name="result">
    /// When this method returns true, contains the decoded <see cref="EncodedId"/>.
    /// When false, contains <c>default</c> (value 0).
    /// </param>
    /// <returns>
    /// <c>true</c> if <paramref name="s"/> was successfully decoded; <c>false</c> otherwise.
    /// </returns>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out EncodedId result) {
        result = default;
        if (string.IsNullOrEmpty(s) || s.Length != IDEncoder.EncodedLength) {
            return false;
        }

        var encoder = EncodedIdConverter.Encoder;
        if (encoder is null) {
            return false;
        }

        try {
            result = new EncodedId(encoder.Decode(s));
            return true;
        }
        catch {
            return false;
        }
    }
}
