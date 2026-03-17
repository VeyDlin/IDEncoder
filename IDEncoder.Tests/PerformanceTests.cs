using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using System.Diagnostics;
using System.Text;

namespace IDEncoder.Tests;

public class PerformanceTests {
    private const string Base62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const string TestSecret = "my-test-secret-key-123";
    private const int Iterations = 100_000;
    private const double MaxSlowerRatio = 1.5;

    [Fact]
    public void Encode_NotSlowerThanBouncyCastle() {
        var encoder = new IDEncoder(TestSecret);

        byte[] keyBytes = Encoding.UTF8.GetBytes(TestSecret);
        var bcEngine = new BlowfishEngine();
        bcEngine.Init(true, new KeyParameter(keyBytes));

        // Warmup
        for (int i = 0; i < 1000; i++) {
            encoder.Encode(i);
            EncodeWithBouncyCastle(bcEngine, i);
        }

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++) {
            encoder.Encode(i);
        }
        long ourTime = sw.ElapsedMilliseconds;

        sw.Restart();
        for (int i = 0; i < Iterations; i++) {
            EncodeWithBouncyCastle(bcEngine, i);
        }
        long bcTime = sw.ElapsedMilliseconds;

        double ratio = bcTime == 0 ? 1.0 : (double)ourTime / bcTime;

        Assert.True(
            ratio <= MaxSlowerRatio,
            $"Our implementation is {ratio:F2}x vs BouncyCastle " +
            $"(ours: {ourTime}ms, BC: {bcTime}ms, max allowed: {MaxSlowerRatio}x)"
        );
    }

    [Fact]
    public void Decode_NotSlowerThanBouncyCastle() {
        var encoder = new IDEncoder(TestSecret);

        byte[] keyBytes = Encoding.UTF8.GetBytes(TestSecret);
        var bcEncryptEngine = new BlowfishEngine();
        bcEncryptEngine.Init(true, new KeyParameter(keyBytes));
        var bcDecryptEngine = new BlowfishEngine();
        bcDecryptEngine.Init(false, new KeyParameter(keyBytes));

        string[] encoded = new string[Iterations];
        for (int i = 0; i < Iterations; i++) {
            encoded[i] = encoder.Encode(i);
        }

        // Warmup
        for (int i = 0; i < 1000; i++) {
            encoder.Decode(encoded[i]);
            DecodeWithBouncyCastle(bcDecryptEngine, encoded[i]);
        }

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < Iterations; i++) {
            encoder.Decode(encoded[i]);
        }
        long ourTime = sw.ElapsedMilliseconds;

        sw.Restart();
        for (int i = 0; i < Iterations; i++) {
            DecodeWithBouncyCastle(bcDecryptEngine, encoded[i]);
        }
        long bcTime = sw.ElapsedMilliseconds;

        double ratio = bcTime == 0 ? 1.0 : (double)ourTime / bcTime;

        Assert.True(
            ratio <= MaxSlowerRatio,
            $"Our implementation is {ratio:F2}x vs BouncyCastle " +
            $"(ours: {ourTime}ms, BC: {bcTime}ms, max allowed: {MaxSlowerRatio}x)"
        );
    }

    private static string EncodeWithBouncyCastle(BlowfishEngine engine, long number) {
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

    private static long DecodeWithBouncyCastle(BlowfishEngine engine, string encoded) {
        ulong value = 0;
        foreach (char c in encoded) {
            value = (value * 62) + (ulong)Base62.IndexOf(c);
        }

        byte[] encrypted = BitConverter.GetBytes(value);
        byte[] output = new byte[8];
        engine.ProcessBlock(encrypted, 0, output, 0);

        ulong unsignedResult = BitConverter.ToUInt64(output, 0);
        return unchecked((long)unsignedResult);
    }
}
