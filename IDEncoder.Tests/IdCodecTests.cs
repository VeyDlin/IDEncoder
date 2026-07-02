namespace IDEncoder.Tests;

public class IdCodecTests {
    private sealed class FakeCodec : IdCodec {
        public int CreateWithSaltCalls;
        public override int MaxEncodedLength => 20;
        public override string Encode(long id) => id.ToString();
        public override int Encode(long id, Span<char> destination) {
            string s = id.ToString();
            s.AsSpan().CopyTo(destination);
            return s.Length;
        }
        public override long Decode(ReadOnlySpan<char> encoded) => long.Parse(encoded);
        protected override IIdCodec CreateWithSalt(string salt) {
            CreateWithSaltCalls++;
            return new FakeCodec();
        }
    }

    [Fact]
    public void WithSalt_NullOrEmpty_ReturnsSameInstance() {
        var codec = new FakeCodec();

        Assert.Same(codec, codec.WithSalt(null!));
        Assert.Same(codec, codec.WithSalt(""));
        Assert.Equal(0, codec.CreateWithSaltCalls);
    }

    [Fact]
    public void WithSalt_CachesPerSalt() {
        var codec = new FakeCodec();

        var first = codec.WithSalt("a");
        var second = codec.WithSalt("a");

        Assert.Same(first, second);
        Assert.Equal(1, codec.CreateWithSaltCalls);
    }

    [Fact]
    public void BlowfishEncodedLength_MatchesMaxEncodedLength() {
        Assert.Equal(11, BlowfishIdCodec.EncodedLength);
        Assert.Equal(BlowfishIdCodec.EncodedLength, new BlowfishIdCodec("k").MaxEncodedLength);
    }
}
