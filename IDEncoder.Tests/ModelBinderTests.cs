using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace IDEncoder.Tests;

// Some tests unset the ambient static to prove DI-first resolution — serialize with the rest.
[Collection("GlobalEncoder")]
public class ModelBinderTests {
    private static DefaultModelBindingContext CreateContext(string value, IServiceProvider services) {
        return new DefaultModelBindingContext {
            ActionContext = new ActionContext {
                HttpContext = new DefaultHttpContext {
                    RequestServices = services
                }
            },
            ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(EncodedId)),
            ModelName = "id",
            ModelState = new ModelStateDictionary(),
            ValueProvider = new RouteValueProvider(
                BindingSource.Path,
                new RouteValueDictionary { ["id"] = value }
            )
        };
    }


    [Fact]
    public async Task BindModel_ResolvesEncoderFromRequestServices() {
        EncodedIdConverter.Encoder = null;
        var encoder = new IDEncoder("binder-di-secret");
        var services = new ServiceCollection();
        services.AddSingleton(encoder);
        await using var provider = services.BuildServiceProvider();

        var context = CreateContext(encoder.Encode(42), provider);
        await new EncodedIdModelBinder(null).BindModelAsync(context);

        Assert.True(context.Result.IsModelSet);
        Assert.Equal(42L, ((EncodedId)context.Result.Model!).Value);
    }


    [Fact]
    public async Task BindModel_ResolvesDeferredProviderAfterConfigure() {
        EncodedIdConverter.Encoder = null;
        var services = new ServiceCollection();
        services.AddIDEncoderProvider();
        await using var provider = services.BuildServiceProvider();

        var encoderProvider = provider.GetRequiredService<IDEncoderProvider>();
        encoderProvider.Configure("binder-deferred-secret");
        // Configure() sets the ambient static as a side effect; clear it again so this test
        // proves the binder found the encoder through DI, not through the static.
        EncodedIdConverter.Encoder = null;

        var context = CreateContext(encoderProvider.Encoder.Encode(42), provider);
        await new EncodedIdModelBinder(null).BindModelAsync(context);

        Assert.True(context.Result.IsModelSet);
        Assert.Equal(42L, ((EncodedId)context.Result.Model!).Value);
    }


    [Fact]
    public async Task BindModel_NoEncoderAnywhere_AddsModelError() {
        EncodedIdConverter.Encoder = null;
        var services = new ServiceCollection();
        await using var provider = services.BuildServiceProvider();

        var context = CreateContext("xK9mQ3bPl2a", provider);
        await new EncodedIdModelBinder(null).BindModelAsync(context);

        Assert.False(context.Result.IsModelSet);
        Assert.False(context.ModelState.IsValid);
    }


    [Fact]
    public async Task BindModel_SaltedBinder_DecodesWithSalt() {
        EncodedIdConverter.Encoder = null;
        var encoder = new IDEncoder("binder-salt-secret");
        var services = new ServiceCollection();
        services.AddSingleton(encoder);
        await using var provider = services.BuildServiceProvider();

        var context = CreateContext(encoder.Encode(42, "video"), provider);
        await new EncodedIdModelBinder("video").BindModelAsync(context);

        Assert.True(context.Result.IsModelSet);
        Assert.Equal(42L, ((EncodedId)context.Result.Model!).Value);
    }
}
