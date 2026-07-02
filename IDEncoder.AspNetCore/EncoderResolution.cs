using Microsoft.Extensions.DependencyInjection;

namespace IDEncoder;


/// <summary>
/// Resolves the active <see cref="IDEncoder"/> from a request's DI container:
/// singleton <see cref="IDEncoder"/> first, then a configured <see cref="IDEncoderProvider"/>.
/// </summary>
internal static class EncoderResolution {
    public static IDEncoder? FromServices(IServiceProvider services) {
        var encoder = services.GetService<IDEncoder>();
        if (encoder is not null) {
            return encoder;
        }

        var provider = services.GetService<IDEncoderProvider>();
        if (provider is not null && provider.IsConfigured) {
            return provider.Encoder;
        }

        return null;
    }
}
