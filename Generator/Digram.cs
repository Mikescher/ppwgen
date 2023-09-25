using System.Collections;
using System.Xml;

namespace PronouncablePasswordGenerator.Generator;

public enum DigramFlags
{
    BEGIN = 0x80,
    NOT_BEGIN = 0x40,
    BREAK = 0x20,
    PREFIX = 0x10,
    //ILLEGAL_PAIR = 0x08, // these were not included in the digram table so this will not be used at all, attempts to instantiate a Digram object that isn't valid will result in an exception
    SUFFIX = 0x04,
    END = 0x02,
    NOT_END = 0x01,
    ANY_COMBINATION = 0x00
}

public class Digram
{
    public Unit? FirstUnit { get; private set; }
    public Unit? SecondUnit { get; private set; }
    public DigramFlags Flags { get; private set; }

    /// <summary>
    /// Gets the current number of characters in the current digram.
    /// </summary>
    public int Length
    {
        get
        {
            return FirstUnit.Text.Length + SecondUnit.Text.Length;
        }
    }

    public string Text
    {
        get
        {
            return FirstUnit.Text + SecondUnit.Text;
        }
    }

    /// <summary>
    /// Randomly selects from possible digrams with the specified first unit.
    /// </summary>
    /// <param name="first">The unit to use as the first unit in the digram.</param>
    public Digram(PRNG prng, Unit? first)
    {
        Init(prng, first, DigramFlags.ANY_COMBINATION, DigramFlags.ANY_COMBINATION, UnitFlags.NO_SPECIAL_RULE, UnitFlags.NO_SPECIAL_RULE);
    }

    /// <summary>
    /// Creates a new digram.
    /// </summary>
    /// <param name="first">The first unit in the digram.</param>
    /// <param name="second">The second unit in the digram.</param>
    public Digram(Unit? first, Unit? second)
    {
        Init(first, second);
    }

    /// <summary>
    /// Randomly selects from possible digrams with the specified first unit.
    /// </summary>
    /// <param name="first">The unit to use as the first unit in the digram.</param>
    /// <param name="flags">Flags required for the resulting digram.</param>
    public Digram(PRNG prng, Unit? first, DigramFlags flags)
    {
        Init(prng, first, flags, DigramFlags.ANY_COMBINATION, UnitFlags.NO_SPECIAL_RULE, UnitFlags.NO_SPECIAL_RULE);
    }

    /// <summary>
    /// Randomly selects from possible digrams with the specified first unit.
    /// </summary>
    /// <param name="first">The unit to use as the first unit in the digram.</param>
    /// <param name="flags">Flags required for the resulting digram.</param>
    /// <param name="reverse">Specifies whether the flags are reversed.</param>
    public Digram(PRNG prng, Unit? first, DigramFlags flags, bool reverse)
    {
        if (reverse)
        {
            Init(prng, first, DigramFlags.ANY_COMBINATION, flags, UnitFlags.NO_SPECIAL_RULE, UnitFlags.NO_SPECIAL_RULE);
        }
        else
        {
            Init(prng, first, flags, DigramFlags.ANY_COMBINATION, UnitFlags.NO_SPECIAL_RULE, UnitFlags.NO_SPECIAL_RULE);
        }
    }

    /// <summary>
    /// Randomly selects from possible digrams with the specified first unit.
    /// </summary>
    /// <param name="first">The unit to use as the first unit in the digram.</param>
    /// <param name="flags">Flags required for the resulting digram.</param>
    /// <param name="notflags">Flags that should not be present on the resulting digram.</param>
    public Digram(PRNG prng, Unit? first, DigramFlags flags, DigramFlags notflags)
    {
        Init(prng, first, flags, notflags, UnitFlags.NO_SPECIAL_RULE, UnitFlags.NO_SPECIAL_RULE);
    }

    /// <summary>
    /// Randomly selects from possible digrams with the specified first unit.
    /// </summary>
    /// <param name="first">The unit to use as the first unit in the digram.</param>
    /// <param name="flags">Flags required for the resulting digram.</param>
    /// <param name="notflags">Flags that should not be present on the resulting digram.</param>
    /// <param name="unitflags">Flags required for the resulting second unit.</param>
    /// <param name="unitnotflags">Flags that should not be present on the resulting second unit.</param>
    public Digram(PRNG prng, Unit? first, DigramFlags flags, DigramFlags notflags, UnitFlags unitflags, UnitFlags unitnotflags)
    {
        Init(prng, first, flags, notflags, unitflags, unitnotflags);
    }

    /// <summary>
    /// Called by constructor to initialize the object with values from a random entry in the digram database.
    /// </summary>
    /// <param name="first">The unit to use as the first unit in the digram.</param>
    /// <param name="flags">Flags required for the resulting digram.</param>
    /// <param name="notflags">Flags that should not be present on the resulting digram.</param>
    private void Init(PRNG prng, Unit? first, DigramFlags flags, DigramFlags notflags, UnitFlags unitflags, UnitFlags unitnotflags)
    {
        FirstUnit = first;

        XmlNode candidates = Data.DigramsData.SelectSingleNode("/digrams/unit[@text='" + FirstUnit.Text + "']");
        if (candidates == null || candidates.ChildNodes.Count == 0) throw new Exception(FirstUnit.Text + " does not start any valid digram.");

        XmlNode randomunit;
        if (flags > 0 || notflags > 0 || unitflags > 0 || unitnotflags > 0)
        {
            ArrayList candidateunits = new ArrayList();
            foreach (XmlNode node in candidates.ChildNodes)
            {
                int digramflagsint;
                if (!int.TryParse(node.InnerText, out digramflagsint)) throw new Exception("Generation of random unit failed due to data inconsistencies!");
                DigramFlags digramflags = (DigramFlags)digramflagsint;
                if (((flags == DigramFlags.ANY_COMBINATION) || ((flags & digramflags) > 0)) && ((notflags == DigramFlags.ANY_COMBINATION) || ((notflags & digramflags) == 0)))
                {
                    Unit candidate;
                    try
                    {
                        candidate = new Unit(node.Attributes["text"].Value);
                    }
                    catch (ArgumentException)
                    {
                        throw new Exception("Generation of random unit failed due to data inconsistencies!");
                    }

                    if ((unitflags == UnitFlags.NO_SPECIAL_RULE || (unitflags & candidate.Flags) > 0) && (unitnotflags == UnitFlags.NO_SPECIAL_RULE || (unitnotflags & candidate.Flags) == 0)) candidateunits.Add(node);
                }
            }

            if (candidateunits.Count == 0) throw new ArgumentException("No units were found fitting given criteria.");
            randomunit = (XmlNode)candidateunits[prng.Next(0, candidateunits.Count)];
        }
        else
        {
            randomunit = candidates.ChildNodes[prng.Next(0, candidates.ChildNodes.Count)];
        }


        try
        {
            SecondUnit = new Unit(randomunit.Attributes["text"].Value);
        }
        catch (ArgumentException)
        {
            throw new Exception("Generation of random unit failed due to data inconsistencies!");
        }
        int randomdigramflags;
        if (int.TryParse(randomunit.InnerText, out randomdigramflags)) Flags = (DigramFlags)randomdigramflags;
        else throw new Exception("Generation of random unit failed due to data inconsistencies!");
    }

    /// <summary>
    /// Called by constructor to initialize the object with values from a specific entry in the digram database.
    /// </summary>
    /// <param name="first">The first unit in the digram.</param>
    /// <param name="second">The second unit in the digram.</param>
    private void Init(Unit? first, Unit? second)
    {
        FirstUnit = first;
        SecondUnit = second;
        string flagtext;
        try
        {
            flagtext = Data.DigramsData.SelectSingleNode("/digrams/unit[@text='" + FirstUnit.Text + "']/unit[@text='" + SecondUnit.Text + "']").InnerText;
        }
        catch (NullReferenceException)
        {
            throw new ArgumentException(Text + " is not a valid digram.");
        }
        int flags;
        if (int.TryParse(flagtext, out flags)) Flags = (DigramFlags)flags;
        else throw new ArgumentException(Text + " is not a valid unit.");
    }

    /// <summary>
    /// Performs a deep copy of the Digram.
    /// </summary>
    /// <returns>Returns a deep copy of the Digram.</returns>
    public Digram Copy()
    {
        Digram copy = new Digram(FirstUnit.Copy(), SecondUnit.Copy());
        return copy;
    }

    /// <summary>
    /// Check if a pair of units form a valid digram.
    /// </summary>
    /// <param name="first">The first unit in the digram.</param>
    /// <param name="second">The second unit in the digram.</param>
    /// <returns>Returns true if the pair of units form a valid digram.</returns>
    public static bool isValid(Unit? first, Unit? second)
    {
        try
        {
            Digram temp = new Digram(first, second);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return true;
    }
}