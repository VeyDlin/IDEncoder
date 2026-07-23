using System.Text.Json;
using System.Text.Json.Serialization;

namespace IDEncoder;


/// <summary>
/// JSON converter for nullable <see cref="EncodedId"/> properties with per-property
/// <see cref="SaltAttribute"/> support. System.Text.Json requires a property's custom converter
/// to match the property type exactly, so the salted <see cref="EncodedIdConverter"/> is wrapped.
/// Null tokens and null values are handled by the serializer itself.
/// </summary>
internal sealed class NullableEncodedIdConverter : JsonConverter<EncodedId?> {
    private readonly EncodedIdConverter inner;


    public NullableEncodedIdConverter(EncodedIdConverter inner) {
        this.inner = inner;
    }


    public override EncodedId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return inner.Read(ref reader, typeof(EncodedId), options);
    }


    public override void Write(Utf8JsonWriter writer, EncodedId? value, JsonSerializerOptions options) {
        inner.Write(writer, value!.Value, options);
    }
}
