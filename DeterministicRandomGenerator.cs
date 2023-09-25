namespace PronouncablePasswordGenerator;

public class DeterministicRandomGenerator : System.Security.Cryptography.RandomNumberGenerator
{
    private readonly Random _r;
    public DeterministicRandomGenerator(int seed)
    {
        _r = new Random(seed);
    }

    public override void GetBytes(byte[] data)
    {
        _r.NextBytes(data);
    }
    public override void GetNonZeroBytes(byte[] data)
    {
        for (int i = 0; i < data.Length; i++) data[i] = (byte)_r.Next(1, 256);
    }
}