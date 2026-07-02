using System.Text.Json;

namespace IDEncoder.Tests;

// These tests touch the process-wide static EncodedIdConverter.Encoder — keep them in
// the serialized collection.
[Collection("GlobalEncoder")]
public class EncodedIdConverterTests {
    private const string TestSecret = "converter-test-secret";

    private static void EnsureEncoder() {
        EncodedIdConverter.Encoder = new IDEncoder(TestSecret);
    }

    [Fact]
    public void Read_NumericToken_RejectedByDefault() {
        EnsureEncoder();

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EncodedId>("42"));
    }

    [Fact]
    public void Read_MalformedString_ThrowsJsonException() {
        EnsureEncoder();

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EncodedId>("\"!!!\""));
    }

    [Fact]
    public void Read_ValidString_RoundTrips() {
        EnsureEncoder();

        string json = JsonSerializer.Serialize(new EncodedId(42));
        var back = JsonSerializer.Deserialize<EncodedId>(json);

        Assert.Equal(42L, back.Value);
    }
}
