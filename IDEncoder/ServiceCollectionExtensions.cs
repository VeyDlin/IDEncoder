using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IDEncoder;


/// <summary>
/// DI registration extensions for <see cref="IDEncoder"/>.
/// </summary>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// Registers <see cref="IDEncoder"/> as a singleton with a known secret key.
    /// Also configures <see cref="EncodedIdConverter"/> for JSON serialization
    /// and <see cref="EncodedId"/> route binding.
    /// Use when the key is available at startup.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="secretKey">
    /// Secret key for Blowfish encryption (1–56 bytes in UTF-8). Keys longer than 56 bytes are truncated.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="secretKey"/> is null or empty.
    /// </exception>
    public static IServiceCollection AddIDEncoder(this IServiceCollection services, string secretKey) {
        var encoder = new IDEncoder(secretKey);
        EncodedIdConverter.Encoder = encoder;
        services.AddSingleton(encoder);
        return services;
    }


    /// <summary>
    /// Registers <see cref="IDEncoder"/> as a singleton with a key resolved from DI.
    /// The encoder is created lazily on first resolve from the container.
    /// Also configures <see cref="EncodedIdConverter"/> for JSON serialization
    /// and <see cref="EncodedId"/> route binding.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="secretKeyFactory">
    /// Factory that receives <see cref="IServiceProvider"/> and returns the secret key string.
    /// Called once when the encoder is first resolved.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddIDEncoder(this IServiceCollection services, Func<IServiceProvider, string> secretKeyFactory) {
        services.AddSingleton(provider => {
            string secretKey = secretKeyFactory(provider);
            var encoder = new IDEncoder(secretKey);
            EncodedIdConverter.Encoder = encoder;
            return encoder;
        });
        return services;
    }


    /// <summary>
    /// Registers <see cref="IDEncoderProvider"/> as a singleton for deferred initialization.
    /// Use when the secret key is not available at startup (e.g. loaded from a database at runtime).
    /// Call <see cref="IDEncoderProvider.Configure"/> when the key becomes available —
    /// after that, JSON serialization and route binding start working automatically.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddIDEncoderProvider(this IServiceCollection services) {
        services.TryAddSingleton<IDEncoderProvider>();
        return services;
    }
}
