using Microsoft.Extensions.DependencyInjection;

namespace IDEncoder.Tests;

[Collection("GlobalEncoder")]
public class CustomCodecTests {
    private sealed class ReverseDigitsCodec : IdCodec {
        public override int MaxEncodedLength => 20;
        public override string Encode(long id) {
            char[] chars = id.ToString().ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }
        public override int Encode(long id, Span<char> destination) {
            string s = Encode(id);
            s.AsSpan().CopyTo(destination);
            return s.Length;
        }
        public override long Decode(ReadOnlySpan<char> encoded) {
            char[] chars = encoded.ToArray();
            Array.Reverse(chars);
            return long.Parse(chars);
        }
        protected override IIdCodec CreateWithSalt(string salt) => this;
    }

    [Fact]
    public void CustomCodec_RegisteredViaDI_UsedByFacade() {
        var services = new ServiceCollection();
        services.AddIDEncoder(new ReverseDigitsCodec());
        var provider = services.BuildServiceProvider();

        var encoder = provider.GetRequiredService<IDEncoder>();

        Assert.Equal("21", encoder.Encode(12));
        Assert.Equal(12, encoder.Decode("21"));
    }

    [Fact]
    public void CustomCodec_RegisteredViaDI_ResolvesIIdCodec() {
        var services = new ServiceCollection();
        services.AddIDEncoder(new ReverseDigitsCodec());
        var provider = services.BuildServiceProvider();

        Assert.IsType<ReverseDigitsCodec>(provider.GetRequiredService<IIdCodec>());
    }
}
