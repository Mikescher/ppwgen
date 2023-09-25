using PronouncablePasswordGenerator.Generator;

namespace PronouncablePasswordGenerator;

public class Profile
{
    public const string SYMBOLS_DEF = "!@#$%^&*()_+[]{}~`;:,./?<>'\"\\|"; // all typeable characters on a standard 101-key keyboard
    
    public int MinLength = 24;
    public bool UseDigits = false;
    public CaseMode CaseMode = CaseMode.MixedCase;
    public bool Morepronounceable = false;
    public string Symbols = "";
    public int Count = 1;
    public int? Seed = null;
}