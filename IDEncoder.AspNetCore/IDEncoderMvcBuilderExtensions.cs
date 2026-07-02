using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace IDEncoder;


/// <summary>
/// Extensions for <see cref="IMvcBuilder"/> and <see cref="IMvcCoreBuilder"/> that bind
/// <see cref="EncodedId"/> JSON serialization to the <see cref="IDEncoder"/> registered in DI.
/// </summary>
public static class IDEncoderMvcBuilderExtensions {
    /// <summary>
    /// Binds MVC JSON serialization of <see cref="EncodedId"/> (including <see cref="SaltAttribute"/>
    /// support) to the encoder registered in DI — works with all registration styles, including
    /// deferred <see cref="IDEncoderProvider"/>. Replaces the manual
    /// <c>AddJsonOptions(o =&gt; o.JsonSerializerOptions.UseIDEncoderSalts())</c> setup.
    /// </summary>
    /// <param name="builder">The MVC builder.</param>
    /// <param name="allowNumericInput">
    /// Whether raw JSON numbers are accepted as already-decoded IDs. Leave off (default) unless
    /// migrating clients that still send plain numbers — accepting numbers lets callers bypass
    /// ID encoding entirely.
    /// </param>
    /// <returns>The same <see cref="IMvcBuilder"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddIDEncoder("secret");
    /// services.AddControllers(o => o.UseIDEncoderModelBinding())
    ///     .AddIDEncoderJson();
    /// </code>
    /// </example>
    public static IMvcBuilder AddIDEncoderJson(this IMvcBuilder builder, bool allowNumericInput = false) {
        AddIDEncoderJsonCore(builder.Services, allowNumericInput);
        return builder;
    }


    /// <summary>
    /// Binds MVC JSON serialization of <see cref="EncodedId"/> (including <see cref="SaltAttribute"/>
    /// support) to the encoder registered in DI. Same behavior as the <see cref="IMvcBuilder"/>
    /// overload, for apps built on <c>AddMvcCore()</c>.
    /// </summary>
    /// <param name="builder">The MVC core builder.</param>
    /// <param name="allowNumericInput">
    /// Whether raw JSON numbers are accepted as already-decoded IDs. Leave off (default) unless
    /// migrating clients that still send plain numbers.
    /// </param>
    /// <returns>The same <see cref="IMvcCoreBuilder"/> for chaining.</returns>
    public static IMvcCoreBuilder AddIDEncoderJson(this IMvcCoreBuilder builder, bool allowNumericInput = false) {
        AddIDEncoderJsonCore(builder.Services, allowNumericInput);
        return builder;
    }


    private static void AddIDEncoderJsonCore(IServiceCollection services, bool allowNumericInput) {
        services.AddOptions<JsonOptions>().Configure<IServiceProvider>((options, provider) => {
            options.JsonSerializerOptions.UseIDEncoder(
                () => EncoderResolution.FromServices(provider),
                allowNumericInput
            );
        });
    }
}
