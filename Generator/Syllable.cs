using System.Collections;

namespace PronouncablePasswordGenerator.Generator;

public class Syllable
{
    private ArrayList _units;

    public string Text
    {
        get
        {
            string text = "";
            foreach (Unit unit in _units)
            {
                text += unit.Text;
            }
            return text;
        }
    }

    public int Count
    {
        get
        {
            return _units.Count;
        }
    }

    public Unit this[int i]
    {
        get
        {
            return (Unit)_units[i];
        }
    }

    private bool _hasVowel = false; // vowel search cache
    public bool HasVowel
    {
        get
        {
            if (_hasVowel) return true; // we already found a vowel previously
            foreach (Unit unit in _units)
            {
                if ((unit.Flags & UnitFlags.VOWEL) > 0) _hasVowel = true; // found a vowel
            }
            return _hasVowel;
        }
    }

    private bool _hasConsonant = false; // consonant search cache
    public bool HasConsonant
    {
        get
        {
            if (_hasConsonant) return true; // we already found a consonant previously
            foreach (Unit unit in _units)
            {
                if ((unit.Flags & UnitFlags.VOWEL) == 0) _hasConsonant = true; // found a consonant
            }
            return _hasConsonant;
        }
    }

    /// <summary>
    /// Creates an empty syllable.
    /// </summary>
    public Syllable()
    {
        _units = new ArrayList();
    }

    /// <summary>
    /// Creates a syllable.
    /// </summary>
    /// <param name="firstUnit">The first unit in the syllable.</param>
    public Syllable(Unit? firstUnit)
    {
        _units = new ArrayList(new Unit[] { firstUnit });
    }

    /// <summary>
    /// Creates a syllable.f
    /// </summary>
    /// <param name="digram">The digram composed of the first two units in the syllable.</param>
    public Syllable(Digram digram)
    {
        _units = new ArrayList(new Unit[] { digram.FirstUnit, digram.SecondUnit });
    }

    /// <summary>
    /// Creates a syllable.
    /// </summary>
    /// <param name="units">The units in the syllable.</param>
    public Syllable(Unit[] units)
    {
        _units = new ArrayList(units);
    }

    /// <summary>
    /// Adds a unit to the syllable.
    /// </summary>
    /// <param name="unit">The unit to add to the syllable.</param>
    /// <returns>The index of the added unit.</returns>
    public int Add(Unit? unit)
    {
        return _units.Add(unit);
    }

    /// <summary>
    /// Adds a digram to the syllable.
    /// </summary>
    /// <param name="digram">The digram to add to the syllable.</param>
    /// <returns>The index of the first unit in the added digram.</returns>
    public int Add(Digram digram)
    {
        int i = _units.Add(digram.FirstUnit);
        _units.Add(digram.SecondUnit);
        return i;
    }

    /// <summary>
    /// Adds a range of units to the syllable.
    /// </summary>
    /// <param name="units">The units to add to the syllable.</param>
    public void AddRange(Unit[] units)
    {
        _units.AddRange(units);
    }

    /// <summary>
    /// Removes last unit from syllable.
    /// </summary>
    public Unit RemoveLast()
    {
        Unit last = (Unit)_units[_units.Count - 1];
        _units.RemoveAt(_units.Count - 1);
        return last;
    }

    /// <summary>
    /// Performs a deep copy of the Syllable.
    /// </summary>
    /// <returns>Returns a deep copy of the Syllable</returns>
    public Syllable Copy()
    {
        Syllable copy = new Syllable();

        for (int i = 0; i < this.Count; i++)
        {
            copy.Add(this[i].Copy());
        }

        return copy;
    }

    /// <summary>
    /// Generates a random syllable.
    /// </summary>
    /// <param name="prevunit1">The first unit preceeding this syllable.</param>
    /// <param name="prevunit2">The second unit preceeding this syllable.</param>
    /// <returns>The randomly generated syllable.</returns>
    public static Syllable Random(PRNG prng, Unit? prevunit1, Unit? prevunit2, ref Syllable leftovers)
    {
        return Random(prng, prevunit1, prevunit2, ref leftovers, false);
    }

    /// <summary>
    /// Generates a random syllable.
    /// </summary>
    /// <param name="prevunit1">The first unit preceeding this syllable.</param>
    /// <param name="prevunit2">The second unit preceeding this syllable.</param>
    /// <param name="morepronounceable">Toggles generation of syllables that make the word more pronounceable.</param>
    /// <returns>The randomly generated syllable.</returns>
    public static Syllable Random(PRNG prng, Unit? prevunit1, Unit? prevunit2, ref Syllable leftovers, bool morepronounceable)
    {
        // Complex rules implemented here.  This function needs to be documented further.
        Syllable generated;
        if (leftovers == null) generated = new Syllable();
        else generated = leftovers.Copy();

        Syllable origgenerated = generated.Copy();

        if (prevunit2 != null && (prevunit2.Flags & UnitFlags.IS_A_DIGIT) > 0)
        {
            prevunit1 = null;
            prevunit2 = null;
        }
        else if (prevunit1 != null && (prevunit1.Flags & UnitFlags.IS_A_DIGIT) > 0)
        {
            prevunit1 = null;
        }

        Unit? origprevunit1 = prevunit1;
        Unit? origprevunit2 = prevunit2;

        if ((prevunit2 != null && (prevunit2.Flags & UnitFlags.IS_SEPARATOR) > 0) && (prevunit1 != null && (prevunit1.Flags & UnitFlags.IS_SEPARATOR) > 0))
            throw new ArgumentException("Separator units must not be passed to Syllable.PRNG()");

        if (leftovers != null && leftovers.Count > 0)
        {
            prevunit1 = prevunit2;
            prevunit2 = leftovers[leftovers.Count - 1];
            if (leftovers.Count > 1)
            {
                prevunit1 = leftovers[leftovers.Count - 2];
            }
        }

        leftovers = null;

        Unit? nextunit = null;

        int iterations = 0;
        while (true)
        {
            iterations++;
            Digram nextdigram = null;
            Digram prevdigram = null;
            DigramFlags digramflags = DigramFlags.ANY_COMBINATION;
            DigramFlags digramnotflags = DigramFlags.ANY_COMBINATION;
            UnitFlags unitflags = UnitFlags.NO_SPECIAL_RULE;
            UnitFlags unitnotflags = UnitFlags.NO_SPECIAL_RULE;

            if (prevunit2 == null) // assume first unit of the first word
            {
                nextunit = new Unit(prng, UnitFlags.NOT_BEGIN_SYLLABLE, true);
            }
            else
            {
                if (prevunit1 != null) prevdigram = new Digram(prevunit1, prevunit2);

                // beginning of syllable but not word
                if (generated.Count == 0)
                {
                    unitnotflags |= UnitFlags.NOT_BEGIN_SYLLABLE;
                }
                else if (generated.Count == 1) // we only have 1 unit, let's not end the syllable just yet
                {
                    digramnotflags |= DigramFlags.NOT_BEGIN; // filter out digrams that shouldn't be at the beginning of a syllable
                    if ((generated[0].Flags & UnitFlags.VOWEL) == 0) digramnotflags |= DigramFlags.BREAK; // we can't have a 1 consonant syllable

                    if (generated[0].Text == "y" && prevunit1 == null)
                    {
                        unitflags |= UnitFlags.VOWEL;
                        digramnotflags |= DigramFlags.BREAK;
                    }
                }
                else
                {
                    if (!generated.HasVowel)
                    {
                        if (prng.Next(0, 35) < 6) // 6 "vowels" out of 36 possible units, and no, we won't follow standard vowel distribution to strengthen generator against statistical attacks
                        {
                            digramnotflags |= DigramFlags.BREAK;
                            digramnotflags |= DigramFlags.BEGIN;
                            unitflags |= UnitFlags.VOWEL;
                        }
                        else
                        {
                            digramnotflags |= DigramFlags.BREAK | DigramFlags.BEGIN | DigramFlags.END;
                        }
                    }
                    if ((prevdigram.Flags & DigramFlags.NOT_END) > 0) digramnotflags |= DigramFlags.BREAK;
                    if (((prevunit1.Flags & UnitFlags.VOWEL) == 0) && ((prevunit2.Flags & UnitFlags.VOWEL) == 0)) unitflags |= UnitFlags.VOWEL; // 2 consecutive consonants, we want a vowel now
                    if (((prevunit1.Flags & UnitFlags.VOWEL) > 0) && ((prevunit2.Flags & UnitFlags.VOWEL) > 0)) unitnotflags |= UnitFlags.VOWEL; // 2 consecutive vowels, we want a consonant now
                    if (generated.Count > 2 && (new Digram(generated[generated.Count - 3], generated[generated.Count - 2]).Flags & DigramFlags.NOT_END) > 0) digramnotflags |= DigramFlags.BEGIN;
                }

                if (prevdigram != null)
                {
                    if (generated.Count > 1 && (prevdigram.Flags & DigramFlags.SUFFIX) > 0) unitflags |= UnitFlags.VOWEL;
                    if ((prevunit1.Flags & UnitFlags.VOWEL) == 0) digramnotflags |= DigramFlags.PREFIX;

                    if ((prevunit1.Flags & (UnitFlags.VOWEL | UnitFlags.ALTERNATE_VOWEL)) == UnitFlags.VOWEL && (prevunit2.Flags & (UnitFlags.VOWEL | UnitFlags.ALTERNATE_VOWEL)) == UnitFlags.VOWEL) 
                    { // no triple vowels please, pseudo-vowel y is ok however
                        if (Digram.isValid(prevunit2, new Unit("y")) && ((unitnotflags & UnitFlags.VOWEL) > 0 | prng.Next(0, 35) == 0))
                        {
                            unitflags |= UnitFlags.ALTERNATE_VOWEL;
                        }
                        else
                        {
                            unitnotflags |= UnitFlags.VOWEL;
                        }
                    }

                    if (morepronounceable && (prevunit2.Flags & UnitFlags.VOWEL) == 0)
                    { // no double consonants
                        unitflags |= UnitFlags.VOWEL;
                    }
                    else if ((prevunit1.Flags & UnitFlags.VOWEL) == 0 && (prevunit2.Flags & UnitFlags.VOWEL) == 0)
                    { // no triple consonants
                        unitflags |= UnitFlags.VOWEL;
                    }
                }

                try
                {
                    nextdigram = new Digram(prng, prevunit2, digramflags, digramnotflags, unitflags, unitnotflags);
                    nextunit = nextdigram.SecondUnit;
                }
                catch (ArgumentException) // could not find possible digram that fits, let's go back one unit
                {
                    nextunit = null;
                    nextdigram = null;

                    if (generated.Count > 0)
                    {
                        generated.RemoveLast();
                        if (generated.Count > 0)
                        {
                            prevunit2 = generated[generated.Count - 1];
                            if (generated.Count > 1) prevunit1 = generated[generated.Count - 2];
                            else prevunit1 = origprevunit2;
                        }
                        else
                        {
                            prevunit1 = origprevunit1;
                            prevunit2 = origprevunit2;
                        }
                    }
                    else throw new ArgumentException("Could not find a syllable to follow previous one.");
                }
            }

            // conflicting result flags, let's nuke the previous unit, should actually not be used anymore
            if ((digramflags & digramnotflags) > 0 || (unitflags & unitnotflags) > 0)
            {
                if (generated.Count > 0)
                {
                    generated.RemoveLast();

                    if (generated.Count > 0)
                    {
                        prevunit2 = generated[generated.Count - 1];
                        if (generated.Count > 1) prevunit1 = generated[generated.Count - 2];
                        else prevunit1 = origprevunit2;
                    }
                    else
                    {
                        prevunit1 = origprevunit1;
                        prevunit2 = origprevunit2;
                    }
                }
                else throw new ArgumentException("Could not find a syllable to follow previous one.");
            }
            else
            {
                if (nextdigram != null)
                {
                    if ((nextdigram.Flags & DigramFlags.BREAK) > 0 || ((nextdigram.FirstUnit.Flags & UnitFlags.VOWEL) == 0 && ((nextdigram.SecondUnit.Flags & UnitFlags.VOWEL)) == 0 && generated.HasVowel))
                    {
                        if (prevdigram != null && (prevdigram.Flags & DigramFlags.NOT_END) > 0)
                        {
                            nextunit = null;
                        }
                        else if (generated.Count > 0)
                        {
                            leftovers = new Syllable(nextunit);
                            break;
                        }
                    }
                    else if (((generated.Count > 1) && (nextdigram.Flags & DigramFlags.BEGIN) > 0) || ((nextdigram.FirstUnit.Flags & UnitFlags.VOWEL) == 0 && ((nextdigram.SecondUnit.Flags & UnitFlags.VOWEL)) > 0 && generated.HasVowel))
                    {
                        if ((generated.Count == 2) && ((generated[0].Flags & UnitFlags.VOWEL) == 0) && ((generated[1].Flags & UnitFlags.VOWEL) > 0))
                        {
                            nextunit = null;
                        }
                        else if (generated.Count > 2)
                        {
                            Digram lastdigram = new Digram(generated[generated.Count - 3], generated[generated.Count - 2]);
                            if ((lastdigram.Flags & DigramFlags.NOT_END) == 0)
                            {
                                generated.RemoveLast();
                                leftovers = new Syllable(nextdigram);
                                break;
                            }
                            //else
                            //{
                            //    nextunit = null;
                            //}
                        }
                        else
                        {
                            generated.RemoveLast();
                            leftovers = new Syllable(nextdigram);
                            break;
                        }
                    }
                    else if ((nextdigram.Flags & DigramFlags.END) > 0)
                    {
                        generated.Add(nextunit);
                        break;
                    }
                }

                if (nextunit != null)
                {
                    generated.Add(nextunit);
                    prevunit1 = prevunit2;
                    prevunit2 = nextunit;
                }
            }
        }

        return generated;
    }
}