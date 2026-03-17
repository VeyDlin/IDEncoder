using System.Buffers.Binary;

namespace IDEncoder;


internal sealed class Blowfish {
    private const int Rounds = 16;
    private const int PSize = Rounds + 2;
    private const int SSize = 256;

    private readonly uint[] P = new uint[PSize];
    private readonly uint[] S0 = new uint[SSize];
    private readonly uint[] S1 = new uint[SSize];
    private readonly uint[] S2 = new uint[SSize];
    private readonly uint[] S3 = new uint[SSize];

    public Blowfish(ReadOnlySpan<byte> key) {
        if (key.Length < 1 || key.Length > 56) {
            throw new ArgumentException("Key must be between 1 and 56 bytes.", nameof(key));
        }

        BlowfishTables.InitialP.AsSpan().CopyTo(P);
        BlowfishTables.InitialS0.AsSpan().CopyTo(S0);
        BlowfishTables.InitialS1.AsSpan().CopyTo(S1);
        BlowfishTables.InitialS2.AsSpan().CopyTo(S2);
        BlowfishTables.InitialS3.AsSpan().CopyTo(S3);

        int keyIndex = 0;
        for (int i = 0; i < PSize; i++) {
            uint data = 0;
            for (int k = 0; k < 4; k++) {
                data = (data << 8) | key[keyIndex];
                keyIndex++;
                if (keyIndex >= key.Length) {
                    keyIndex = 0;
                }
            }
            P[i] ^= data;
        }

        uint xL = 0, xR = 0;

        for (int i = 0; i < PSize; i += 2) {
            EncryptBlock(ref xL, ref xR);
            P[i] = xL;
            P[i + 1] = xR;
        }

        for (int i = 0; i < SSize; i += 2) {
            EncryptBlock(ref xL, ref xR);
            S0[i] = xL;
            S0[i + 1] = xR;
        }

        for (int i = 0; i < SSize; i += 2) {
            EncryptBlock(ref xL, ref xR);
            S1[i] = xL;
            S1[i + 1] = xR;
        }

        for (int i = 0; i < SSize; i += 2) {
            EncryptBlock(ref xL, ref xR);
            S2[i] = xL;
            S2[i + 1] = xR;
        }

        for (int i = 0; i < SSize; i += 2) {
            EncryptBlock(ref xL, ref xR);
            S3[i] = xL;
            S3[i + 1] = xR;
        }
    }


    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output) {
        uint xL = BinaryPrimitives.ReadUInt32BigEndian(input);
        uint xR = BinaryPrimitives.ReadUInt32BigEndian(input[4..]);

        EncryptBlock(ref xL, ref xR);

        BinaryPrimitives.WriteUInt32BigEndian(output, xL);
        BinaryPrimitives.WriteUInt32BigEndian(output[4..], xR);
    }


    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output) {
        uint xL = BinaryPrimitives.ReadUInt32BigEndian(input);
        uint xR = BinaryPrimitives.ReadUInt32BigEndian(input[4..]);

        DecryptBlock(ref xL, ref xR);

        BinaryPrimitives.WriteUInt32BigEndian(output, xL);
        BinaryPrimitives.WriteUInt32BigEndian(output[4..], xR);
    }


    private void EncryptBlock(ref uint xL, ref uint xR) {
        xL ^= P[0];
        for (int i = 1; i < Rounds; i += 2) {
            xR ^= F(xL) ^ P[i];
            xL ^= F(xR) ^ P[i + 1];
        }
        xR ^= P[Rounds + 1];

        (xL, xR) = (xR, xL);
    }


    private void DecryptBlock(ref uint xL, ref uint xR) {
        xL ^= P[Rounds + 1];
        for (int i = Rounds; i > 0; i -= 2) {
            xR ^= F(xL) ^ P[i];
            xL ^= F(xR) ^ P[i - 1];
        }
        xR ^= P[0];

        (xL, xR) = (xR, xL);
    }


    private uint F(uint x) {
        uint a = S0[(x >> 24) & 0xFF];
        uint b = S1[(x >> 16) & 0xFF];
        uint c = S2[(x >> 8) & 0xFF];
        uint d = S3[x & 0xFF];
        return ((a + b) ^ c) + d;
    }
}
