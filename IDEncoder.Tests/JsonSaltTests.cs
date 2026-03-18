using System.Text.Json;

namespace IDEncoder.Tests;

public class JsonSaltTests {
    private const string TestSecret = "my-test-secret-key-123";

    public record UnsaltedResult(EncodedId Id, string Title);

    public record VideoResult([property: Salt("video")] EncodedId Id, string Title);

    public record GalleryResult([property: Salt("gallery")] EncodedId Id, string Title);

    private static JsonSerializerOptions CreateOptions() {
        var options = new JsonSerializerOptions();
        options.UseIDEncoderSalts();
        return options;
    }

    private static void EnsureEncoder() {
        EncodedIdConverter.Encoder ??= new IDEncoder(TestSecret);
    }

    [Fact]
    public void Serialize_WithSalt_ProducesDifferentOutput() {
        EnsureEncoder();
        var options = CreateOptions();

        string videoJson = JsonSerializer.Serialize(new VideoResult(42, "vid"), options);
        string galleryJson = JsonSerializer.Serialize(new GalleryResult(42, "gal"), options);

        Assert.NotEqual(videoJson, galleryJson);
    }

    [Fact]
    public void Serialize_WithSalt_DiffersFromUnsalted() {
        EnsureEncoder();
        var options = CreateOptions();

        string unsaltedJson = JsonSerializer.Serialize(new UnsaltedResult(42, "x"), options);
        string videoJson = JsonSerializer.Serialize(new VideoResult(42, "x"), options);

        Assert.NotEqual(unsaltedJson, videoJson);
    }

    [Fact]
    public void Deserialize_WithSalt_RoundTrips() {
        EnsureEncoder();
        var options = CreateOptions();

        var original = new VideoResult(42, "test");
        string json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<VideoResult>(json, options)!;

        Assert.Equal(original.Id.Value, deserialized.Id.Value);
        Assert.Equal(original.Title, deserialized.Title);
    }

    [Fact]
    public void Deserialize_WrongSaltType_ProducesWrongValue() {
        EnsureEncoder();
        var options = CreateOptions();

        var original = new VideoResult(42, "test");
        string json = JsonSerializer.Serialize(original, options);

        // Deserialize the video-salted JSON as gallery-salted — should produce a different ID
        var wrongResult = JsonSerializer.Deserialize<GalleryResult>(json, options)!;
        Assert.NotEqual(42L, wrongResult.Id.Value);
    }

    [Fact]
    public void Serialize_MultipleSalts_AllRoundTrip() {
        EnsureEncoder();
        var options = CreateOptions();
        var random = new Random(777);

        for (int i = 0; i < 100; i++) {
            long id = random.NextInt64();

            var video = new VideoResult(id, "v");
            var gallery = new GalleryResult(id, "g");

            string videoJson = JsonSerializer.Serialize(video, options);
            string galleryJson = JsonSerializer.Serialize(gallery, options);

            var videoBack = JsonSerializer.Deserialize<VideoResult>(videoJson, options)!;
            var galleryBack = JsonSerializer.Deserialize<GalleryResult>(galleryJson, options)!;

            Assert.Equal(id, videoBack.Id.Value);
            Assert.Equal(id, galleryBack.Id.Value);
        }
    }

    [Fact]
    public void Serialize_SameSalt_ProducesSameJson() {
        EnsureEncoder();
        var options = CreateOptions();

        string json1 = JsonSerializer.Serialize(new VideoResult(42, "x"), options);
        string json2 = JsonSerializer.Serialize(new VideoResult(42, "x"), options);

        Assert.Equal(json1, json2);
    }
}
