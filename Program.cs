using System.Security.Cryptography;
using PronouncablePasswordGenerator.Generator;

namespace PronouncablePasswordGenerator;

public static class Program
{
    public const string VERSION = "1.1";
    
    public static void Main()
    {
        var prof = GetProfile();

        var rng = prof.Seed != null ? new DeterministicRandomGenerator(prof.Seed.Value) : RandomNumberGenerator.Create();
        
        for (int i = 0; i < prof.Count; i++)
        {
            var gen = PronounceablePassword.Generate(rng, prof.MinLength, prof.UseDigits, prof.Symbols, prof.CaseMode, prof.Morepronounceable);
            
            if (!Console.IsOutputRedirected || prof.Count > 1) Console.WriteLine(gen); else Console.Write(gen);
        }
    }

    private static Profile GetProfile()
    {
        var p = new Profile();

        var positional = 0;
        foreach (var arg in Environment.GetCommandLineArgs().Skip(1))
        {
            var isnumber = int.TryParse(arg, out var argnum);
            
            if (arg.StartsWith("--"))
            {
                positional = -1;

                if (arg.StartsWith("--length=") && int.TryParse(arg.Substring("--length=".Length), out var lengthnum))
                {
                    p.MinLength = lengthnum;
                    continue;
                }

                if (arg == "--digits")
                {
                    p.UseDigits = true;
                    continue;
                }

                if (arg == "--no-digits")
                {
                    p.UseDigits = false;
                    continue;
                }

                if (arg == "--mixed-case" || arg == "--mc")
                {
                    p.CaseMode = CaseMode.MixedCase;
                    continue;
                }

                if (arg == "--lower-case" || arg == "--lc")
                {
                    p.CaseMode = CaseMode.LowerCase;
                    continue;
                }

                if (arg == "--upper-case" || arg == "--uc")
                {
                    p.CaseMode = CaseMode.UpperCase;
                    continue;
                }

                if (arg == "--random-case" || arg == "--rc")
                {
                    p.CaseMode = CaseMode.RandomCase;
                    continue;
                }

                if (arg == "--random-mixed-case" || arg == "--rmc")
                {
                    p.CaseMode = CaseMode.RandomMixedCase;
                    continue;
                }

                if (arg == "--more-pronouncable" || arg == "--mp")
                {
                    p.Morepronounceable = true;
                    continue;
                }

                if (arg == "--symbols")
                {
                    p.Symbols = Profile.SYMBOLS_DEF;
                    continue;
                }

                if (arg.StartsWith("--count=") && int.TryParse(arg.Substring("--count=".Length), out var countnum))
                {
                    p.Count = countnum;
                    continue;
                }

                if (arg.StartsWith("--seed=") && int.TryParse(arg.Substring("--seed=".Length), out var seednum))
                {
                    p.Seed = seednum;
                    continue;
                }

                if (arg == "--help")
                {
                    Console.WriteLine("./ppwgen - PronouncablePasswordGenerator.");
                    Console.WriteLine("");
                    Console.WriteLine("# (forked from KeePass PronouncablePassword plugin)");
                    Console.WriteLine("");
                    Console.WriteLine("Usage: ppwgen");
                    Console.WriteLine("       ppwgen <minlength>");
                    Console.WriteLine("       ppwgen [minlength] --count=<c>");
                    Console.WriteLine("       ppwgen --version");
                    Console.WriteLine("       ppwgen --help");
                    Console.WriteLine("");
                    Console.WriteLine("Options:");
                    Console.WriteLine("  --length=<len>");
                    Console.WriteLine("  --digits");
                    Console.WriteLine("  --no-digits");
                    Console.WriteLine("  --mc,  --mixed-case");
                    Console.WriteLine("  --uc,  --upper-case");
                    Console.WriteLine("  --lc,  --lower-case");
                    Console.WriteLine("  --rc,  --random-case");
                    Console.WriteLine("  --rmc, --random-mixed-case");
                    Console.WriteLine("  --mp,  --more-pronouncable");
                    Console.WriteLine("  --symbols");
                    Console.WriteLine("  --count=<c>");
                    Console.WriteLine("  --seed=<s>");
                    Environment.Exit(0);
                    return new Profile();
                }

                if (arg == "--version")
                {
                    Console.WriteLine("ppwgen " + VERSION);
                    Environment.Exit(0);
                    return new Profile();
                }

            }
            else if (positional == 0 && isnumber)
            {
                p.MinLength = argnum;
                continue;
            }
            
            throw new Exception($"Unknown argument '{arg}'");
        }

        return p;
    }
}