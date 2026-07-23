using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace IDEncoder.Tests;

// Regression tests: [Salt] on Nullable<EncodedId> must behave exactly like on EncodedId.
// Unsets/sets the ambient static — keep in the serialized collection.
[Collection("GlobalEncoder")]
public class NullableEncodedIdTests {
    public record NullableSalted([property: Salt("video")] EncodedId? Id, string Title);


    [Fact]
    public void Serialize_NullableSaltedProperty_AppliesSalt() {
        EncodedIdConverter.Encoder = null;
        var encoder = new IDEncoder("nullable-salt-secret");
        var options = new JsonSerializerOptions().UseIDEncoder(encoder);

        string json = JsonSerializer.Serialize(new NullableSalted(42, "x"), options);

        Assert.Contains(encoder.Encode(42, "video"), json);
    }


    [Fact]
    public void Deserialize_NullableSaltedProperty_RoundTrips() {
        EncodedIdConverter.Encoder = null;
        var encoder = new IDEncoder("nullable-salt-secret");
        var options = new JsonSerializerOptions().UseIDEncoder(encoder);

        string json = JsonSerializer.Serialize(new NullableSalted(42, "x"), options);
        var back = JsonSerializer.Deserialize<NullableSalted>(json, options)!;

        Assert.Equal(42L, back.Id!.Value.Value);
    }


    [Fact]
    public void Serialize_NullableSaltedProperty_MatchesNonNullableSaltedEncoding() {
        EncodedIdConverter.Encoder = null;
        var encoder = new IDEncoder("nullable-salt-secret");
        var options = new JsonSerializerOptions().UseIDEncoder(encoder);

        string json = JsonSerializer.Serialize(new NullableSalted(42, "x"), options);

        // Must NOT contain the unsalted encoding — that was the bug.
        Assert.DoesNotContain(encoder.Encode(42), json);
    }


    [Fact]
    public void Serialize_NullableSaltedProperty_Null_WritesNull() {
        EncodedIdConverter.Encoder = null;
        var encoder = new IDEncoder("nullable-salt-secret");
        var options = new JsonSerializerOptions().UseIDEncoder(encoder);

        string json = JsonSerializer.Serialize(new NullableSalted(null, "x"), options);

        Assert.Contains("null", json);
    }


    [Fact]
    public void Deserialize_NullableSaltedProperty_Null_ReadsNull() {
        EncodedIdConverter.Encoder = null;
        var encoder = new IDEncoder("nullable-salt-secret");
        var options = new JsonSerializerOptions().UseIDEncoder(encoder);

        var back = JsonSerializer.Deserialize<NullableSalted>("{\"Id\":null,\"Title\":\"x\"}", options)!;

        Assert.Null(back.Id);
    }


    [Fact]
    public void UseIDEncoderSalts_NullableSaltedProperty_AppliesSaltViaAmbient() {
        var encoder = new IDEncoder("nullable-ambient-secret");
        EncodedIdConverter.Encoder = encoder;
        var options = new JsonSerializerOptions().UseIDEncoderSalts();

        string json = JsonSerializer.Serialize(new NullableSalted(42, "x"), options);

        Assert.Contains(encoder.Encode(42, "video"), json);
    }


    [Fact]
    public void GetBinder_NullableEncodedId_ReturnsBinder() {
        var provider = new EncodedIdModelBinderProvider();

        Assert.NotNull(provider.GetBinder(new FakeProviderContext(typeof(EncodedId?))));
    }


    [Fact]
    public async Task BindModel_NullableModel_DecodesWithSalt() {
        EncodedIdConverter.Encoder = null;
        var encoder = new IDEncoder("nullable-binder-secret");
        var services = new ServiceCollection();
        services.AddSingleton(encoder);
        await using var provider = services.BuildServiceProvider();

        var context = new DefaultModelBindingContext {
            ActionContext = new ActionContext {
                HttpContext = new DefaultHttpContext {
                    RequestServices = provider
                }
            },
            ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(EncodedId?)),
            ModelName = "id",
            ModelState = new ModelStateDictionary(),
            ValueProvider = new RouteValueProvider(
                BindingSource.Path,
                new RouteValueDictionary { ["id"] = encoder.Encode(42, "video") }
            )
        };
        await new EncodedIdModelBinder("video").BindModelAsync(context);

        Assert.True(context.Result.IsModelSet);
        Assert.Equal(42L, ((EncodedId)context.Result.Model!).Value);
    }


    private sealed class FakeProviderContext : ModelBinderProviderContext {
        private readonly ModelMetadata metadata;

        public FakeProviderContext(Type modelType) {
            metadata = new EmptyModelMetadataProvider().GetMetadataForType(modelType);
        }

        public override BindingInfo BindingInfo => new();

        public override ModelMetadata Metadata => metadata;

        public override IModelMetadataProvider MetadataProvider => new EmptyModelMetadataProvider();

        public override IModelBinder CreateBinder(ModelMetadata metadata) => throw new NotSupportedException();
    }
}
