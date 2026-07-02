using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IDEncoder.Tests;

// Unsets the ambient static to prove the wiring is DI-first — serialize with the rest.
[Collection("GlobalEncoder")]
public class MvcJsonWiringTests {
    [Fact]
    public void AddIDEncoderJson_BindsMvcJsonOptionsToDiEncoder() {
        EncodedIdConverter.Encoder = null;
        var encoder = new IDEncoder("mvc-json-secret");
        var services = new ServiceCollection();
        services.AddSingleton(encoder);
        services.AddMvcCore().AddIDEncoderJson();
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
        string json = JsonSerializer.Serialize(new EncodedId(42), options);

        Assert.Equal("\"" + encoder.Encode(42) + "\"", json);
    }


    [Fact]
    public void AddIDEncoderJson_DeferredProvider_WorksAfterConfigure() {
        EncodedIdConverter.Encoder = null;
        var services = new ServiceCollection();
        services.AddIDEncoderProvider();
        services.AddMvcCore().AddIDEncoderJson();
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
        Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new EncodedId(42), options));

        var encoderProvider = provider.GetRequiredService<IDEncoderProvider>();
        encoderProvider.Configure("mvc-deferred-secret");
        EncodedIdConverter.Encoder = null; // prove resolution goes through DI, not the static

        string json = JsonSerializer.Serialize(new EncodedId(42), options);
        Assert.Equal("\"" + encoderProvider.Encoder.Encode(42) + "\"", json);
    }
}
