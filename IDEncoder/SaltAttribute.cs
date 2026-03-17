namespace IDEncoder;


/// <summary>
/// Specifies a salt for <see cref="EncodedId"/> properties to produce different encodings
/// for the same numeric ID across different entity types.
/// Requires <see cref="IDEncoderJsonExtensions.UseIDEncoderSalts"/> to be configured on <c>JsonSerializerOptions</c>.
/// </summary>
/// <example>
/// <code>
/// public record VideoResult(
///     [property: Salt("video")] EncodedId Id,
///     string Title
/// );
///
/// public record GalleryResult(
///     [property: Salt("gallery")] EncodedId Id,
///     string Title
/// );
/// // Same numeric ID 42 produces different encoded strings for video vs gallery.
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
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
