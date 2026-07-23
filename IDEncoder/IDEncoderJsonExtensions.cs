using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace IDEncoder;


/// <summary>
/// Extensions for <see cref="JsonSerializerOptions"/>: bind <see cref="EncodedId"/> serialization
/// to a specific <see cref="IDEncoder"/> and enable <see cref="SaltAttribute"/> support.
/// </summary>
public static class IDEncoderJsonExtensions {
    /// <summary>
    /// Binds <see cref="EncodedId"/> serialization in these options to the given encoder and
    /// enables <see cref="SaltAttribute"/> support. Options configured this way never consult
    /// the ambient process-wide encoder. Must be called before first serialization.
    /// </summary>
    /// <param name="options">The JSON serializer options to configure.</param>
    /// <param name="encoder">The encoder to use for all <see cref="EncodedId"/> properties.</param>
    /// <param name="allowNumericInput">
    /// Whether raw JSON numbers are accepted as already-decoded IDs. Leave off (default) unless
    /// migrating clients that still send plain numbers — accepting numbers lets callers bypass
    /// ID encoding entirely.
    /// </param>
    /// <returns>The same <see cref="JsonSerializerOptions"/> for chaining.</returns>
    public static JsonSerializerOptions UseIDEncoder(
        this JsonSerializerOptions options,
        IDEncoder encoder,
        bool allowNumericInput = false
    ) {
        ArgumentNullException.ThrowIfNull(encoder);
        return options.UseIDEncoder(() => encoder, allowNumericInput);
    }


    /// <summary>
    /// Binds <see cref="EncodedId"/> serialization in these options to a lazy encoder source and
    /// enables <see cref="SaltAttribute"/> support. The source is consulted on every read/write,
    /// so it may return null until the encoder becomes available (deferred initialization).
    /// Must be called before first serialization.
    /// </summary>
    /// <param name="options">The JSON serializer options to configure.</param>
    /// <param name="encoderSource">
    /// Returns the encoder to use, or null if not yet available (serialization then fails with
    /// <see cref="InvalidOperationException"/> until it is).
    /// </param>
    /// <param name="allowNumericInput">
    /// Whether raw JSON numbers are accepted as already-decoded IDs. Leave off (default) unless
    /// migrating clients that still send plain numbers.
    /// </param>
    /// <returns>The same <see cref="JsonSerializerOptions"/> for chaining.</returns>
    public static JsonSerializerOptions UseIDEncoder(
        this JsonSerializerOptions options,
        Func<IDEncoder?> encoderSource,
        bool allowNumericInput = false
    ) {
        ArgumentNullException.ThrowIfNull(encoderSource);
        options.Converters.Add(new EncodedIdConverter(encoderSource, null, allowNumericInput));
        return options.ApplySaltSupport(encoderSource, allowNumericInput);
    }


    /// <summary>
    /// Enables <see cref="SaltAttribute"/> support for <see cref="EncodedId"/> properties using
    /// the ambient process-wide encoder. Prefer <see cref="UseIDEncoder(JsonSerializerOptions, IDEncoder, bool)"/>,
    /// which binds the options to a specific encoder instance.
    /// Must be called before first serialization.
    /// </summary>
    /// <param name="options">The JSON serializer options to configure.</param>
    /// <param name="allowNumericInput">
    /// Whether raw JSON numbers are accepted as already-decoded IDs. Leave off (default) unless
    /// migrating clients that still send plain numbers.
    /// </param>
    /// <returns>The same <see cref="JsonSerializerOptions"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// // In ASP.NET Core:
    /// services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.UseIDEncoderSalts());
    ///
    /// // Standalone:
    /// var options = new JsonSerializerOptions();
    /// options.UseIDEncoderSalts();
    /// </code>
    /// </example>
    public static JsonSerializerOptions UseIDEncoderSalts(
        this JsonSerializerOptions options,
        bool allowNumericInput = false
    ) {
        if (allowNumericInput) {
            options.Converters.Add(new EncodedIdConverter(null, null, allowNumericInput: true));
        }
        return options.ApplySaltSupport(null, allowNumericInput);
    }


    private static JsonSerializerOptions ApplySaltSupport(
        this JsonSerializerOptions options,
        Func<IDEncoder?>? encoderSource,
        bool allowNumericInput
    ) {
        var resolver = options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();
        options.TypeInfoResolver = resolver.WithAddedModifier(
            typeInfo => ApplySaltModifier(typeInfo, encoderSource, allowNumericInput)
        );
        return options;
    }


    private static void ApplySaltModifier(JsonTypeInfo typeInfo, Func<IDEncoder?>? encoderSource, bool allowNumericInput) {
        foreach (var property in typeInfo.Properties) {
            if (property.PropertyType != typeof(EncodedId) && property.PropertyType != typeof(EncodedId?)) {
                continue;
            }

            var saltAttr = property.AttributeProvider?
                .GetCustomAttributes(typeof(SaltAttribute), false)
                .OfType<SaltAttribute>()
                .FirstOrDefault();

            if (saltAttr is not null) {
                var converter = new EncodedIdConverter(encoderSource, saltAttr.Salt, allowNumericInput);
                property.CustomConverter = property.PropertyType == typeof(EncodedId?)
                    ? new NullableEncodedIdConverter(converter)
                    : converter;
            }
        }
    }
}
