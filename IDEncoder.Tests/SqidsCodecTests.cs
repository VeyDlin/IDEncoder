namespace IDEncoder.Tests;

public class SqidsCodecTests {
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(42)]
    [InlineData(1234567890123456789)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void RoundTrip_Boundary(long id) {
        var codec = new SqidsIdCodec();
        Assert.Equal(id, codec.Decode(codec.Encode(id)));
    }

    [Fact]
    public void RoundTrip_Random() {
        var codec = new SqidsIdCodec();
        var random = new Random(123);
        for (int i = 0; i < 10000; i++) {
            long id = random.NextInt64();
            Assert.Equal(id, codec.Decode(codec.Encode(id)));
        }
    }

    [Fact]
    public void SmallIds_AreShorterThanMax() {
        var codec = new SqidsIdCodec();
        Assert.True(codec.Encode(1).Length < codec.Encode(long.MaxValue).Length);
    }

    [Fact]
    public void MaxEncodedLength_IsExact() {
        var codec = new SqidsIdCodec();
        long[] boundary = [0, 1, -1, long.MaxValue, long.MinValue];
        int observedMax = boundary.Max(id => codec.Encode(id).Length);
        Assert.Equal(codec.MaxEncodedLength, observedMax);
    }

    [Fact]
    public void Decode_InvalidChars_Throws() {
        var codec = new SqidsIdCodec();
        Assert.Throws<ArgumentException>(() => codec.Decode("!!"));
    }

    [Fact]
    public void Decode_TooShort_Throws() {
        var codec = new SqidsIdCodec();
        Assert.Throws<ArgumentException>(() => codec.Decode("x"));
    }

    [Fact]
    public void Decode_TooLong_Throws() {
        var codec = new SqidsIdCodec();
        Assert.Throws<ArgumentException>(() => codec.Decode(new string('z', 13)));
    }

    [Fact]
    public void Decode_LeadingZeroPadding_Throws() {
        var codec = new SqidsIdCodec();
        string s = codec.Encode(42);

        int offset = codec.ShuffledAlphabet.IndexOf(s[0]);
        char zeroDigit = codec.ShuffledAlphabet[offset % 62];   // rotated[0]
        string padded = s[0] + zeroDigit.ToString() + s[1..];

        Assert.Throws<ArgumentException>(() => codec.Decode(padded));
    }

    [Fact]
    public void Salt_DifferentSalts_DifferentOutput() {
        var codec = new SqidsIdCodec();
        string none = codec.Encode(42);
        string video = codec.WithSalt("video").Encode(42);
        string gallery = codec.WithSalt("gallery").Encode(42);

        Assert.NotEqual(none, video);
        Assert.NotEqual(none, gallery);
        Assert.NotEqual(video, gallery);
    }

    [Fact]
    public void Salt_RoundTrip() {
        var salted = new SqidsIdCodec().WithSalt("video");
        Assert.Equal(42, salted.Decode(salted.Encode(42)));
    }

    [Fact]
    public void TwoInstances_ProduceSameOutput() {
        Assert.Equal(new SqidsIdCodec().Encode(42), new SqidsIdCodec().Encode(42));
    }

    [Fact]
    public void Golden_DefaultSalt_Encode42() {
        // Captured to freeze the wire format. A change here means the format changed.
        Assert.Equal("qln", new SqidsIdCodec().Encode(42));
    }

    [Fact]
    public void Decode_WrongSalt_DoesNotReturnOriginal() {
        var codec = new SqidsIdCodec();
        string encoded = codec.Encode(42);
        var salted = codec.WithSalt("video");

        // Decoding a default-alphabet string under a different salt must not yield the
        // original value: the canonical re-encode check rejects it, or it maps elsewhere.
        try {
            Assert.NotEqual(42L, salted.Decode(encoded));
        }
        catch (ArgumentException) {
            // Rejected as non-canonical — also acceptable.
        }
    }
}
