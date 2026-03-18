using Microsoft.AspNetCore.Mvc;

namespace IDEncoder;


/// <summary>
/// Extensions for <see cref="MvcOptions"/> to enable <see cref="EncodedId"/> model binding
/// with <see cref="SaltAttribute"/> support on controller parameters.
/// </summary>
public static class IDEncoderMvcExtensions {
    /// <summary>
    /// Registers <see cref="EncodedIdModelBinder"/> so that <see cref="EncodedId"/> parameters
    /// in controller actions are decoded from Base62 strings automatically.
    /// Supports <see cref="SaltAttribute"/> on parameters for per-entity-type decoding.
    /// Must be called in addition to <see cref="IDEncoderJsonExtensions.UseIDEncoderSalts"/>
    /// if salt support is needed for both JSON and route binding.
    /// </summary>
    /// <param name="options">The MVC options to configure.</param>
    /// <returns>The same <see cref="MvcOptions"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddControllers(o => o.UseIDEncoderModelBinding());
    ///
    /// // Then in controller:
    /// [HttpGet("{id}")]
    /// public IActionResult Get([Salt("video")] EncodedId id) {
    ///     long dbId = id; // correctly decoded with "video" salt
    /// }
    /// </code>
    /// </example>
    public static MvcOptions UseIDEncoderModelBinding(this MvcOptions options) {
        options.ModelBinderProviders.Insert(0, new EncodedIdModelBinderProvider());
        return options;
    }
}
