using System.Text.Json;
using System.Text.Json.Serialization;

namespace IDEncoder;


/// <summary>
/// JSON converter for <see cref="EncodedId"/>.
/// Writes long values as Base62 strings, reads both Base62 strings and raw numbers.
/// Configured automatically by <see cref="ServiceCollectionExtensions.AddIDEncoder"/>
/// or <see cref="IDEncoderProvider.Configure"/>.
/// </summary>
public sealed class EncodedIdConverter : JsonConverter<EncodedId> {
    internal static IDEncoder? Encoder { get; set; }

    private readonly string? salt;


    /// <summary>
    /// Creates a converter with no salt (default behavior).
    /// </summary>
    public EncodedIdConverter() : this(null) {
    }

    /// <summary>
    /// Creates a converter with a specific salt for per-property encoding.
    /// </summary>
    /// <param name="salt">The salt string, or null for no salt.</param>
    internal EncodedIdConverter(string? salt) {
        this.salt = salt;
    }


    /// <summary>
    /// Reads an <see cref="EncodedId"/> from JSON.
    /// Accepts both Base62 strings (e.g. <c>"xK9mQ3bPl2a"</c>) and numeric values (e.g. <c>42</c>).
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the token to read.</param>
    /// <param name="typeToConvert">The target type (always <see cref="EncodedId"/>).</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>An <see cref="EncodedId"/> with the decoded numeric value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IDEncoder"/> is not configured via DI.
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown when the JSON token is null or an unexpected type (not string or number).
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the string value is not a valid 11-character Base62 encoded ID.
    /// </exception>
    public override EncodedId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var encoder = Encoder
            ?? throw new InvalidOperationException("IDEncoder is not configured. Call services.AddIDEncoder() first.");

        if (reader.TokenType == JsonTokenType.String) {
            string encoded = reader.GetString()
                ?? throw new JsonException("Expected non-null string for EncodedId.");
            return new EncodedId(encoder.Decode(encoded, salt));
        }

        if (reader.TokenType == JsonTokenType.Number) {
            return new EncodedId(reader.GetInt64());
        }

        throw new JsonException($"Unexpected token {reader.TokenType} for EncodedId.");
    }


    /// <summary>
    /// Writes an <see cref="EncodedId"/> to JSON as a Base62-encoded string.
    /// For example, a value of 42 might be written as <c>"xK9mQ3bPl2a"</c>.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The <see cref="EncodedId"/> to serialize.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IDEncoder"/> is not configured via DI.
    /// </exception>
    public override void Write(Utf8JsonWriter writer, EncodedId value, JsonSerializerOptions options) {
        var encoder = Encoder
            ?? throw new InvalidOperationException("IDEncoder is not configured. Call services.AddIDEncoder() first.");

        writer.WriteStringValue(encoder.Encode(value.Value, salt));
    }
}
