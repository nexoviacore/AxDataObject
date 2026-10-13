using System.Security.Cryptography;

namespace AxData.Application.Ports;

public sealed class CryptographicObjectIdGenerator(IClock clock) : IObjectIdGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public string NewObjectId()
    {
        Span<byte> bytes = stackalloc byte[16];
        var timestamp = clock.UtcNow.ToUnixTimeMilliseconds();
        if (timestamp is < 0 or > 0xFFFFFFFFFFFF)
        {
            throw new InvalidOperationException("The current timestamp cannot be represented as a ULID.");
        }

        for (var index = 5; index >= 0; index--)
        {
            bytes[index] = (byte)timestamp;
            timestamp >>= 8;
        }

        RandomNumberGenerator.Fill(bytes[6..]);
        Span<char> encoded = stackalloc char[26];
        for (var character = 0; character < encoded.Length; character++)
        {
            var value = 0;
            for (var bit = 0; bit < 5; bit++)
            {
                var sourceBit = (character * 5) + bit - 2;
                value <<= 1;
                if (sourceBit >= 0)
                {
                    value |= (bytes[sourceBit / 8] >> (7 - (sourceBit % 8))) & 1;
                }
            }

            encoded[character] = Alphabet[value];
        }

        return new string(encoded);
    }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
