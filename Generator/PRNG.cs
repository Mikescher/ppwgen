using System.Security.Cryptography;

namespace PronouncablePasswordGenerator.Generator;

public class PRNG
{
    private readonly RandomNumberGenerator _stream;
    public PRNG(RandomNumberGenerator stream)
    {
        this._stream = stream;
    }

    public int Next(int max)
    {
        return Next(0, max);
    }

    public int Next(int min, int max)
    {
        var mod = max - min;
        return (int)(GetRandomUInt() % mod) + min;
    }
    
    private UInt32 GetRandomUInt()
    {
        var randomBytes = new byte[4];
        _stream.GetBytes(randomBytes);
        return BitConverter.ToUInt32(randomBytes, 0);
    }
}
