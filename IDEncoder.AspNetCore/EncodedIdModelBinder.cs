using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace IDEncoder;


/// <summary>
/// Model binder for <see cref="EncodedId"/> that supports <see cref="SaltAttribute"/> on controller parameters.
/// Decodes Base62 route/query values with the correct salt.
/// </summary>
/// <example>
/// <code>
/// [HttpPost("{id}/test")]
/// public async Task&lt;IActionResult&gt; TestConnection([Salt("s3")] EncodedId id) { ... }
/// </code>
/// </example>
internal sealed class EncodedIdModelBinder : IModelBinder {
    private readonly string? salt;

    /// <summary>
    /// Creates a model binder with an optional salt for decoding.
    /// </summary>
    /// <param name="salt">The salt to use during decoding, or null for unsalted decoding.</param>
    public EncodedIdModelBinder(string? salt) {
        this.salt = salt;
    }

    /// <inheritdoc/>
    public Task BindModelAsync(ModelBindingContext bindingContext) {
        var modelName = bindingContext.ModelName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None) {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrEmpty(value)) {
            return Task.CompletedTask;
        }

        var encoder = EncodedIdConverter.Encoder;
        if (encoder is null) {
            bindingContext.ModelState.TryAddModelError(modelName, "IDEncoder is not configured.");
            return Task.CompletedTask;
        }

        try {
            long decoded = encoder.Decode(value, salt);
            bindingContext.Result = ModelBindingResult.Success(new EncodedId(decoded));
        }
        catch (ArgumentException ex) {
            bindingContext.ModelState.TryAddModelError(modelName, ex.Message);
        }

        return Task.CompletedTask;
    }
}


/// <summary>
/// Model binder provider that creates <see cref="EncodedIdModelBinder"/> for <see cref="EncodedId"/> parameters.
/// Reads <see cref="SaltAttribute"/> from controller action parameters to determine the salt.
/// Register via <see cref="IDEncoderMvcExtensions.UseIDEncoderModelBinding"/>.
/// </summary>
internal sealed class EncodedIdModelBinderProvider : IModelBinderProvider {
    /// <inheritdoc/>
    public IModelBinder? GetBinder(ModelBinderProviderContext context) {
        if (context.Metadata.ModelType != typeof(EncodedId)) {
            return null;
        }

        string? salt = null;

        if (context.Metadata is DefaultModelMetadata defaultMetadata) {
            var attrs = defaultMetadata.Attributes.ParameterAttributes;
            if (attrs is not null) {
                foreach (var attr in attrs) {
                    if (attr is SaltAttribute saltAttr) {
                        salt = saltAttr.Salt;
                        break;
                    }
                }
            }
        }

        return new EncodedIdModelBinder(salt);
    }
}
