namespace IDEncoder.Tests;

[Collection("GlobalEncoder")]
public class EncodedIdTests {
    [Fact]
    public void TryParse_SqidsVariableLength_RoundTrips() {
        EncodedIdConverter.Encoder = new IDEncoder(new SqidsIdCodec());

        string encoded = EncodedIdConverter.Encoder.Encode(42);
        bool ok = EncodedId.TryParse(encoded, provider: null, out var result);

        Assert.True(ok);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void TryParse_TooLong_ReturnsFalse() {
        EncodedIdConverter.Encoder = new IDEncoder(new SqidsIdCodec());

        Assert.False(EncodedId.TryParse(new string('z', 1000), provider: null, out _));
    }

    [Fact]
    public void TryParse_Garbage_ReturnsFalse() {
        EncodedIdConverter.Encoder = new IDEncoder(new SqidsIdCodec());

        Assert.False(EncodedId.TryParse("!", provider: null, out _));
    }
}
