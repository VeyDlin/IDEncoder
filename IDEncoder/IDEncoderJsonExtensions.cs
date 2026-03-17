using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace IDEncoder;


/// <summary>
/// Extensions for <see cref="JsonSerializerOptions"/> to enable <see cref="SaltAttribute"/> support
/// on <see cref="EncodedId"/> properties.
/// </summary>
public static class IDEncoderJsonExtensions {
    /// <summary>
    /// Enables <see cref="SaltAttribute"/> support for <see cref="EncodedId"/> properties.
    /// Properties marked with <c>[Salt("...")]</c> will use a per-entity-type salt during encoding/decoding.
    /// Must be called before first serialization.
    /// </summary>
    /// <param name="options">The JSON serializer options to configure.</param>
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
    public static JsonSerializerOptions UseIDEncoderSalts(this JsonSerializerOptions options) {
        var resolver = options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver();
        options.TypeInfoResolver = resolver.WithAddedModifier(ApplySaltModifier);
        return options;
    }

    private static void ApplySaltModifier(JsonTypeInfo typeInfo) {
        foreach (var property in typeInfo.Properties) {
            if (property.PropertyType != typeof(EncodedId)) {
                continue;
            }

            var saltAttr = property.AttributeProvider?
                .GetCustomAttributes(typeof(SaltAttribute), false)
                .OfType<SaltAttribute>()
                .FirstOrDefault();

            if (saltAttr is not null) {
                property.CustomConverter = new EncodedIdConverter(saltAttr.Salt);
            }
        }
    }
}
