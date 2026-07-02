using System.Text.Json;

namespace IDEncoder.Tests;

// Verifies instance-bound options work with the ambient static UNSET — must not run in
// parallel with tests that set it.
[Collection("GlobalEncoder")]
public class InstanceBindingTests {
    public record VideoResult([property: Salt("video")] EncodedId Id, string Title);

    [Fact]
    public void UseIDEncoder_WorksWithoutAmbientStatic() {
        EncodedIdConverter.Encoder = null;
        var encoder = new IDEncoder("instance-bound-secret");
        var options = new JsonSerializerOptions().UseIDEncoder(encoder);

        string json = JsonSerializer.Serialize(new EncodedId(42), options);
        var back = JsonSerializer.Deserialize<EncodedId>(json, options);

        Assert.Equal("\"" + encoder.Encode(42) + "\"", json);
        Assert.Equal(42L, back.Value);
    }

    [Fact]
    public void UseIDEncoder_SaltedProperties_WorkWithoutAmbientStatic() {
        EncodedIdConverter.Encoder = null;
        var encoder = new IDEncoder("instance-bound-secret");
        var options = new JsonSerializerOptions().UseIDEncoder(encoder);

        string json = JsonSerializer.Serialize(new VideoResult(42, "x"), options);
        var back = JsonSerializer.Deserialize<VideoResult>(json, options)!;

        Assert.Contains(encoder.Encode(42, "video"), json);
        Assert.Equal(42L, back.Id.Value);
    }

    [Fact]
    public void UseIDEncoder_LazySource_ResolvesPerCall() {
        EncodedIdConverter.Encoder = null;
        IDEncoder? current = null;
        var options = new JsonSerializerOptions().UseIDEncoder(() => current);

        Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new EncodedId(42), options));

        current = new IDEncoder("late-bound-secret");
        string json = JsonSerializer.Serialize(new EncodedId(42), options);
        Assert.Equal("\"" + current.Encode(42) + "\"", json);
    }

    [Fact]
    public void UseIDEncoder_AllowNumericInput_AcceptsNumbers() {
        EncodedIdConverter.Encoder = null;
        var options = new JsonSerializerOptions().UseIDEncoder(new IDEncoder("s"), allowNumericInput: true);

        Assert.Equal(42L, JsonSerializer.Deserialize<EncodedId>("42", options).Value);
    }

    [Fact]
    public void UseIDEncoderSalts_AllowNumericInput_AcceptsNumbersViaAmbient() {
        EncodedIdConverter.Encoder = new IDEncoder("ambient-secret");
        var options = new JsonSerializerOptions().UseIDEncoderSalts(allowNumericInput: true);

        Assert.Equal(42L, JsonSerializer.Deserialize<EncodedId>("42", options).Value);
    }
}
