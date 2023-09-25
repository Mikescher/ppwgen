using System.Collections;
using System.Xml;

namespace PronouncablePasswordGenerator.Generator;

public enum UnitFlags
{
    IS_SEPARATOR = 0x40,
    IS_A_DIGIT = 0x20,
    IS_DOUBLE_CHAR = 0x10,
    NOT_BEGIN_SYLLABLE = 0x08,
    NO_FINAL_SPLIT = 0x04,
    VOWEL = 0x02,
    ALTERNATE_VOWEL = 0x01,
    NO_SPECIAL_RULE = 0x00
}

public class Unit
{
    public Unit() { } // should not be used

    public string Text { get; protected set; }
    public UnitFlags Flags { get; protected set; }

    /// <summary>
    /// Gets the current number of characters in the current unit.
    /// </summary>
    public int Length
    {
        get
        {
            return Text.Length;
        }
    }

    /// <summary>
    /// Generates a random unit.
    /// </summary>
    public Unit(PRNG prng)
    {
        Init(prng, UnitFlags.NO_SPECIAL_RULE, UnitFlags.NO_SPECIAL_RULE);
    }

    /// <summary>
    /// Creates a new unit.
    /// </summary>
    /// <param name="text">New unit's text representation.</param>
    public Unit(string text)
    {
        Init(text);
    }

    /// <summary>
    /// Generates a random unit.
    /// </summary>
    /// <param name="flags">Flags required for new unit.</param>
    public Unit(PRNG prng, UnitFlags flags)
    {
        Init(prng, flags, UnitFlags.NO_SPECIAL_RULE);
    }

    /// <summary>
    /// Generates a random unit.
    /// </summary>
    /// <param name="flags">Flags required for new unit.</param>
    /// <param name="reverse">Specifies whether the flags are reversed.</param>
    public Unit(PRNG prng, UnitFlags flags, bool reverse)
    {
        if (reverse)
        {
            Init(prng, UnitFlags.NO_SPECIAL_RULE, flags);
        }
        else
        {
            Init(prng, flags, UnitFlags.NO_SPECIAL_RULE);
        }
    }

    /// <summary>
    /// Generates a random unit.
    /// </summary>
    /// <param name="flags">Flags required for the new unit.</param>
    /// <param name="notflags">Flags which must not be present on the new unit.</param>
    public Unit(PRNG prng, UnitFlags flags, UnitFlags notflags)
    {
        Init(prng, flags, notflags);
    }

    /// <summary>
    /// Called by constructor to initialize the object with values from a random entry in the unit database.
    /// </summary>
    /// <param name="flags">Flags required for the new unit.</param>
    /// <param name="notflags">Flags which must not be present on the new unit.</param>
    private void Init(PRNG prng, UnitFlags flags, UnitFlags notflags)
    {
        XmlNode randomunit = null;
        if (flags > 0 || notflags > 0)
        {
            ArrayList candidates = new ArrayList();
            foreach (XmlNode node in Data.UnitsData["units"].ChildNodes)
            {
                int unitflags;
                if (!int.TryParse(node.InnerText, out unitflags)) throw new Exception("Generation of random unit failed due to data inconsistencies!");
                if ((flags == 0 || ((int)flags & unitflags) > 0) && (notflags == 0 || ((int)notflags & unitflags) == 0)) candidates.Add(node);
            }

            if (candidates.Count == 0) throw new Exception("No units were found fitting given criteria.");
            randomunit = (XmlNode)candidates[prng.Next(0, candidates.Count)];
        }
        else
        {
            randomunit = Data.UnitsData["units"].ChildNodes[prng.Next(0, Data.UnitsData["units"].ChildNodes.Count)];
        }
        Text = randomunit.Attributes["text"].Value;
        int randomunitflags;
        if (int.TryParse(randomunit.InnerText, out randomunitflags)) Flags = (UnitFlags)randomunitflags;
        else throw new Exception("Generation of random unit failed due to data inconsistencies!");
    }

    /// <summary>
    /// Called by constructor to initialize the object with values from a specific entry in the unit database.
    /// </summary>
    /// <param name="text">Unit text to search the unit database for.</param>
    public void Init(string text)
    {
        Text = text.ToLower();
        string flagtext;
        try
        {
            flagtext = Data.UnitsData.SelectSingleNode("/units/unit[@text='" + Text + "']").InnerText;
        }
        catch (NullReferenceException)
        {
            throw new ArgumentException(Text + " is not a valid unit.");
        }
        int flags;
        if (int.TryParse(flagtext, out flags)) Flags = (UnitFlags)flags;
        else throw new ArgumentException(Text + " is not a valid unit.");
    }

    /// <summary>
    /// Check if a string of text is a valid unit
    /// </summary>
    /// <param name="text">Text to check.</param>
    /// <returns>Returns true if text is a valid unit.</returns>
    public static bool isValid(string text)
    {
        try
        {
            Unit temp = new Unit(text);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Performs a deep copy of the Unit.
    /// </summary>
    /// <returns>Returns a deep copy of the Unit.</returns>
    public Unit? Copy()
    {
        return new Unit(this.Text);
    }
}

public class Symbol : Unit
{
    /// <summary>
    /// Generates a random symbol.
    /// </summary>
    public Symbol(PRNG prng, string validsymbols)
    {
        Text = validsymbols[prng.Next(validsymbols.Length)].ToString();
        Flags = UnitFlags.IS_A_DIGIT; // treated like digits
    }

    
    /// <summary>
    /// Creates a unit representing the specified symbol.
    /// </summary>
    /// <param name="number">The number to be represented as a digit.</param>
    public Symbol(char symbol)
    {
        Text = symbol.ToString();
        Flags = UnitFlags.IS_A_DIGIT;
    }

    /// <summary>
    /// Creates a unit representing the specified symbol.
    /// </summary>
    /// <param name="number">The number to be represented as a digit.</param>
    protected Symbol(string symbol)
    {
        Text = symbol;
        Flags = UnitFlags.IS_A_DIGIT;
    }

    /// <summary>
    /// Performs a deep copy of the Digit.
    /// </summary>
    /// <returns>Returns a deep copy of the Digit.</returns>
    public new Symbol Copy()
    {
        return new Symbol(this.Text);
    }
}

public class Digit : Unit
{
    /// <summary>
    /// Generates a random digit.
    /// </summary>
    public Digit(PRNG prng)
    {
        Text = prng.Next(0, 10).ToString();
        Flags = UnitFlags.IS_A_DIGIT;
    }

    /// <summary>
    /// Creates a digit representing the specified number.
    /// </summary>
    /// <param name="number">The number to be represented as a digit.</param>
    public Digit(int number)
    {
        int digit = number % 10;
        Text = digit.ToString();
        Flags = UnitFlags.IS_A_DIGIT;
    }

    /// <summary>
    /// Creates a digit representing the specified number.
    /// </summary>
    /// <param name="number">The number to be represented as a digit.</param>
    protected Digit(string number)
    {
        Text = number;
        Flags = UnitFlags.IS_A_DIGIT;
    }

    /// <summary>
    /// Performs a deep copy of the Digit.
    /// </summary>
    /// <returns>Returns a deep copy of the Digit.</returns>
    public new Digit Copy()
    {
        return new Digit(this.Text);
    }
}

public class Separator : Unit
{
    /// <summary>
    /// Creates a new separator.
    /// </summary>
    /// <param name="text">Separator to use. (only first character is used)</param>
    public Separator(string text)
    {
        Text = text.Substring(0, 1); // one character separators only
        Flags = UnitFlags.IS_SEPARATOR;
    }

    /// <summary>
    /// Performs a deep copy of the Separator.
    /// </summary>
    /// <returns>Returns a deep copy of the Separator.</returns>
    public new Separator Copy()
    {
        return new Separator(this.Text);
    }
}