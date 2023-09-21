using System.Security.Cryptography;

namespace PronouncablePasswordGenerator;

public static class Program
{
    public static void Main()
    {
        // var minLength = 16;
        // var useDigits = true;
        // var caseMode = CaseMode.MixedCase;
        // var morepronounceable = false;
        // var symbols = "!@#$%^&*()_+[]{}~`;:,./?<>'\"\\|"; // all typeable characters on a standard 101-key keyboard

        var prof = GetProfile();

        var rng = new RNGCryptoServiceProvider();

        for (int i = 0; i < prof.Count; i++)
        {
            var gen = PronounceablePassword.Generate(rng, prof.MinLength, prof.UseDigits, prof.Symbols, prof.CaseMode, prof.Morepronounceable);
            
            if (!Console.IsOutputRedirected || prof.Count > 1) Console.WriteLine(gen); else Console.Write(gen);
        }
    }

    public static Profile GetProfile()
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

                if (arg == "--count=" && int.TryParse(arg.Substring("--count=".Length), out var countnum))
                {
                    p.Count = countnum;
                    continue;
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