using System.Text.Json;
using System.Text.Json.Serialization;

namespace IDEncoder;


/// <summary>
/// JSON converter for <see cref="EncodedId"/>.
/// Writes long values as Base62 strings; reads Base62 strings, and raw JSON numbers only when
/// numeric input was explicitly enabled at configuration time.
/// Instances created via <c>JsonSerializerOptions.UseIDEncoder(...)</c>
/// are bound to that encoder and never consult the ambient static; the parameterless instance
/// used by the <c>[JsonConverter]</c> attribute falls back to the ambient encoder configured by
/// <c>AddIDEncoder</c> or <see cref="IDEncoderProvider.Configure(string)"/>.
/// </summary>
public sealed class EncodedIdConverter : JsonConverter<EncodedId> {
    private static IDEncoder? ambientEncoder;

    private readonly Func<IDEncoder?>? encoderSource;
    private readonly string? salt;
    private readonly bool allowNumericInput;

    /// <summary>
    /// The ambient (process-wide) encoder, used only by converter instances without a bound
    /// encoder source. Volatile: deferred <see cref="IDEncoderProvider.Configure(string)"/> may
    /// race with in-flight serialization.
    /// </summary>
    internal static IDEncoder? Encoder {
        get => Volatile.Read(ref ambientEncoder);
        set => Volatile.Write(ref ambientEncoder, value);
    }


    /// <summary>
    /// Creates a converter that uses the ambient encoder, without salt, rejecting numeric input.
    /// This is the instance the <c>[JsonConverter]</c> attribute on <see cref="EncodedId"/> creates.
    /// </summary>
    public EncodedIdConverter() : this(null, null, false) {
    }


    /// <summary>
    /// Creates a converter bound to an encoder source with an optional salt.
    /// </summary>
    /// <param name="encoderSource">
    /// Lazy encoder source consulted on every read/write, or null to use the ambient encoder.
    /// </param>
    /// <param name="salt">The salt string, or null for no salt.</param>
    /// <param name="allowNumericInput">Whether raw JSON numbers are accepted as already-decoded IDs.</param>
    internal EncodedIdConverter(Func<IDEncoder?>? encoderSource, string? salt, bool allowNumericInput) {
        this.encoderSource = encoderSource;
        this.salt = salt;
        this.allowNumericInput = allowNumericInput;
    }


    /// <summary>
    /// Reads an <see cref="EncodedId"/> from JSON.
    /// Accepts Base62 strings (e.g. <c>"xK9mQ3bPl2a"</c>); raw numbers only when enabled.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the token to read.</param>
    /// <param name="typeToConvert">The target type (always <see cref="EncodedId"/>).</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>An <see cref="EncodedId"/> with the decoded numeric value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no encoder is available (neither bound nor ambient).
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown when the token is not a valid encoding, is a number while numeric input is
    /// disabled, or is an unexpected token type.
    /// </exception>
    public override EncodedId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var encoder = ResolveEncoder();

        if (reader.TokenType == JsonTokenType.String) {
            string encoded = reader.GetString()
                ?? throw new JsonException("Expected non-null string for EncodedId.");
            try {
                return new EncodedId(encoder.Decode(encoded, salt));
            }
            catch (ArgumentException ex) {
                throw new JsonException($"Invalid encoded ID '{encoded}'.", ex);
            }
        }

        if (reader.TokenType == JsonTokenType.Number) {
            if (!allowNumericInput) {
                throw new JsonException(
                    "Raw numeric input for EncodedId is disabled. " +
                    "Enable it explicitly via allowNumericInput: true if clients send plain numbers."
                );
            }
            return new EncodedId(reader.GetInt64());
        }

        throw new JsonException($"Unexpected token {reader.TokenType} for EncodedId.");
    }


    /// <summary>
    /// Writes an <see cref="EncodedId"/> to JSON as a Base62-encoded string.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The <see cref="EncodedId"/> to serialize.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no encoder is available (neither bound nor ambient).
    /// </exception>
    public override void Write(Utf8JsonWriter writer, EncodedId value, JsonSerializerOptions options) {
        var encoder = ResolveEncoder();
        writer.WriteStringValue(encoder.Encode(value.Value, salt));
    }


    private IDEncoder ResolveEncoder() {
        var encoder = encoderSource?.Invoke() ?? Encoder;
        return encoder
            ?? throw new InvalidOperationException("IDEncoder is not configured. Call services.AddIDEncoder() first.");
    }
}
