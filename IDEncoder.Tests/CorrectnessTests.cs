using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text;

namespace IDEncoder.Tests;

public class CorrectnessTests {
    private const string Base62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const string TestSecret = "my-test-secret-key-123";

    private static string EncodeWithBouncyCastle(long number, string secretKey) {
        byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
        if (keyBytes.Length > 56) {
            Array.Resize(ref keyBytes, 56);
        }

        var engine = new BlowfishEngine();
        engine.Init(true, new KeyParameter(keyBytes));

        ulong unsignedNumber = unchecked((ulong)number);
        byte[] input = BitConverter.GetBytes(unsignedNumber);
        byte[] output = new byte[8];
        engine.ProcessBlock(input, 0, output, 0);

        ulong value = BitConverter.ToUInt64(output, 0);
        char[] result = new char[11];
        for (int i = 10; i >= 0; i--) {
            result[i] = Base62[(int)(value % 62)];
            value /= 62;
        }

        return new string(result);
    }

    private static long DecodeWithBouncyCastle(string encoded, string secretKey) {
        byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
        if (keyBytes.Length > 56) {
            Array.Resize(ref keyBytes, 56);
        }

        ulong value = 0;
        foreach (char c in encoded) {
            value = (value * 62) + (ulong)Base62.IndexOf(c);
        }

        byte[] encrypted = BitConverter.GetBytes(value);
        byte[] output = new byte[8];

        var engine = new BlowfishEngine();
        engine.Init(false, new KeyParameter(keyBytes));
        engine.ProcessBlock(encrypted, 0, output, 0);

        ulong unsignedResult = BitConverter.ToUInt64(output, 0);
        return unchecked((long)unsignedResult);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(42)]
    [InlineData(100)]
    [InlineData(999999)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    [InlineData(1234567890123456789)]
    public void Encode_MatchesBouncyCastle(long number) {
        var encoder = new IDEncoder(TestSecret);

        string expected = EncodeWithBouncyCastle(number, TestSecret);
        string actual = encoder.Encode(number);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(42)]
    [InlineData(100)]
    [InlineData(999999)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    [InlineData(1234567890123456789)]
    public void Decode_MatchesBouncyCastle(long number) {
        var encoder = new IDEncoder(TestSecret);

        string encoded = EncodeWithBouncyCastle(number, TestSecret);
        long expected = DecodeWithBouncyCastle(encoded, TestSecret);
        long actual = encoder.Decode(encoded);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RoundTrip_AllValues_Match() {
        var encoder = new IDEncoder(TestSecret);
        var random = new Random(12345);

        for (int i = 0; i < 10000; i++) {
            long original = ((long)random.Next() << 32) | (long)(uint)random.Next();
            string encoded = encoder.Encode(original);
            long decoded = encoder.Decode(encoded);

            Assert.Equal(original, decoded);
        }
    }

    [Fact]
    public void RoundTrip_MatchesBouncyCastle_RandomValues() {
        var encoder = new IDEncoder(TestSecret);
        var random = new Random(54321);

        for (int i = 0; i < 10000; i++) {
            long original = ((long)random.Next() << 32) | (long)(uint)random.Next();

            string ourEncoded = encoder.Encode(original);
            string bcEncoded = EncodeWithBouncyCastle(original, TestSecret);

            Assert.Equal(bcEncoded, ourEncoded);
        }
    }

    [Fact]
    public void Encode_ProducesCorrectLength() {
        var encoder = new IDEncoder(TestSecret);

        Assert.Equal(11, encoder.Encode(0).Length);
        Assert.Equal(11, encoder.Encode(long.MaxValue).Length);
        Assert.Equal(11, encoder.Encode(long.MinValue).Length);
    }

    [Fact]
    public void Decode_InvalidInput_Throws() {
        var encoder = new IDEncoder(TestSecret);

        Assert.Throws<ArgumentException>(() => encoder.Decode(""));
        Assert.Throws<ArgumentException>(() => encoder.Decode("short"));
        Assert.Throws<ArgumentException>(() => encoder.Decode("toolongstring"));
        Assert.Throws<ArgumentException>(() => encoder.Decode("12345678!0A"));
    }

    [Fact]
    public void EncodeNull_ReturnsNull_ForNull() {
        var encoder = new IDEncoder(TestSecret);

        Assert.Null(encoder.EncodeNull(null));
        Assert.NotNull(encoder.EncodeNull(42));
    }

    [Fact]
    public void DecodeNull_ReturnsNull_ForNull() {
        var encoder = new IDEncoder(TestSecret);

        Assert.Null(encoder.DecodeNull(null));
    }

    [Theory]
    [InlineData("short-key")]
    [InlineData("a-much-longer-secret-key-for-testing-purposes-here")]
    [InlineData("x")]
    public void DifferentKeys_ProduceDifferentResults(string key) {
        var encoder1 = new IDEncoder(TestSecret);
        var encoder2 = new IDEncoder(key);

        string encoded1 = encoder1.Encode(42);
        string encoded2 = encoder2.Encode(42);

        Assert.NotEqual(encoded1, encoded2);
    }
}
