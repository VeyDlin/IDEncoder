namespace IDEncoder;


/// <summary>
/// Specifies a salt for <see cref="EncodedId"/> to produce different encodings
/// for the same numeric ID across different entity types.
/// Works with:
/// <list type="bullet">
/// <item>JSON serialization — requires <see cref="IDEncoderJsonExtensions.UseIDEncoderSalts"/> on <c>JsonSerializerOptions</c>.</item>
/// <item>Route/query binding — requires <see cref="IDEncoderMvcExtensions.UseIDEncoderModelBinding"/> on <c>MvcOptions</c>.</item>
/// </list>
/// </summary>
/// <example>
/// <code>
/// // On DTO properties (JSON serialization):
/// public record VideoResult(
///     [property: Salt("video")] EncodedId Id,
///     string Title
/// );
///
/// // On controller parameters (route binding):
/// [HttpGet("{id}")]
/// public IActionResult Get([Salt("video")] EncodedId id) { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class SaltAttribute : Attribute {
    /// <summary>
    /// The salt string used to differentiate encodings.
    /// </summary>
    public string Salt { get; }

    /// <summary>
    /// Creates a new <see cref="SaltAttribute"/> with the given salt.
    /// </summary>
    /// <param name="salt">
    /// A string that differentiates this entity type's ID encoding from others (e.g. "video", "gallery").
    /// </param>
    public SaltAttribute(string salt) {
        Salt = salt;
    }
}
