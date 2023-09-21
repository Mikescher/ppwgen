using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;

namespace PronouncablePasswordGenerator;
/*
    KeePass Pronounceable Password Generator Plugin
    Copyright (C) 2009 Jan Benjamin Engracia <jaybz.e@gmail.com>
    Based on FIPS-181 <http://www.itl.nist.gov/fipspubs/fip181.htm>

    This file is part of KeePass Pronounceable Password Generator Plugin.

    KeePass Pronounceable Password Generator Plugin is free software:
    you can redistribute it and/or modify it under the terms of the GNU
    General Public License as published by the Free Software Foundation,
    either version 3 of the License, or (at your option) any later
    version.

    KeePass Pronounceable Password Generator Plugin is distributed in
    the hope that it will be useful, but WITHOUT ANY WARRANTY; without
    even the implied warranty of MERCHANTABILITY or FITNESS FOR A
    PARTICULAR PURPOSE.  See the GNU Leser General Public License for
    more details.

    You should have received a copy of the GNU General Public License
    along with KeePass Pronounceable Password Generator Plugin.  If not,
    see <http://www.gnu.org/licenses/>.
*/

public enum CharacterSubstitutionMode
{
    NoSubstitution = 0,
    RandomSubstitution = 1,
    SubstituteAll = 2
}

public enum CaseMode
{
    LowerCase = 0,
    UpperCase = 1,
    MixedCase = 2,
    RandomCase = 3,
    RandomMixedCase = 4
}

public class PRNG
{
    RandomNumberGenerator? stream = null;
    public PRNG(RandomNumberGenerator? stream)
    {
        this.stream = stream;
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
        stream.GetBytes(randomBytes);
        return BitConverter.ToUInt32(randomBytes, 0);
    }
}

/// <summary>
/// Generates pronounceable passwords based on FIPS 181.
/// </summary>
public class PronounceablePassword
{
    #region Private property flags
    /// <summary>
    /// Unit property flags.
    /// </summary>
    private enum UnitFlags
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

    /// <summary>
    /// Digram property flags
    /// </summary>
    private enum DigramFlags
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
    #endregion

    #region Private classes

    #region Separator class
    /// <summary>
    /// Represents a separator
    /// </summary>
    private class Separator : Unit
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
    #endregion

    #region Digit class
    /// <summary>
    /// Represents a single digit
    /// </summary>
    private class Digit : Unit
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
    #endregion

    #region Symbol class
    private class Symbol : Unit
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
    #endregion

    #region Unit class
    /// <summary>
    /// Represents a single unit
    /// </summary>
    private class Unit
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
                foreach (XmlNode node in UnitsData["units"].ChildNodes)
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
                randomunit = UnitsData["units"].ChildNodes[prng.Next(0, UnitsData["units"].ChildNodes.Count)];
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
                flagtext = UnitsData.SelectSingleNode("/units/unit[@text='" + Text + "']").InnerText;
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
    #endregion

    #region Digram class
    /// <summary>
    /// Represents a pair of units
    /// </summary>
    private class Digram
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

            XmlNode candidates = DigramsData.SelectSingleNode("/digrams/unit[@text='" + FirstUnit.Text + "']");
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
                flagtext = DigramsData.SelectSingleNode("/digrams/unit[@text='" + FirstUnit.Text + "']/unit[@text='" + SecondUnit.Text + "']").InnerText;
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
    #endregion

    #region Syllable class
    /// <summary>
    /// Represents a group of units forming a syllable
    /// </summary>
    private class Syllable
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
    #endregion

    #region Word class
    /// <summary>
    /// Represents a group of syllables forming a word
    /// </summary>
    private class Word
    {
        private ArrayList _syllables;

        public Word()
        {
            _syllables = new ArrayList();
        }

        public Word(Syllable[] syllables)
        {
            _syllables = new ArrayList(syllables);
        }

        public int Add(Syllable syllable)
        {
            return _syllables.Add(syllable);
        }

        public void AddRange(Syllable[] syllables)
        {
            _syllables.AddRange(syllables);
        }

        public string Text
        {
            get
            {
                string text = "";
                foreach (Syllable syllable in _syllables)
                {
                    text += syllable.Text;
                }
                return text;
            }
        }

        public string UpperCaseSyllableStartText
        {
            get
            {
                string text = "";
                foreach (Syllable syllable in _syllables)
                {
                    text += syllable.Text.Substring(0, 1).ToUpper() + syllable.Text.Substring(1, syllable.Text.Length - 1);
                }
                return text;
            }
        }

        public string UpperCaseSyllableStartTextRandomize(PRNG prng)
        {
            string text = "";
            foreach (Syllable syllable in _syllables)
            {
                //if (syllable.Text.Length == 0) continue;
                if (prng.Next(2) > 0)
                    text += syllable.Text.Substring(0, 1).ToUpper() + syllable.Text.Substring(1, syllable.Text.Length - 1);
                else
                    text += syllable.Text;
            }
            return text;
        }

        public string HyphenedText
        {
            get
            {
                string text = "";
                foreach (Syllable syllable in _syllables)
                {
                    text += syllable.Text + "-";
                }
                text = text.Substring(0, text.Length - 1); // remove that last hyphen, yes it's dirty
                return text;
            }
        }
    }
    #endregion

    #endregion

    #region XML Data Helpers
    private static XmlDocument _unitsdata;
    private static XmlDocument _digramsdata;

    private static XmlDocument UnitsData
    {
        get
        {
            if (_unitsdata == null)
            {
                string data = Data.UNITS;
                _unitsdata = new XmlDocument();
                _unitsdata.InnerXml = data;
            }

            return _unitsdata;
        }
    }

    private static XmlDocument DigramsData
    {
        get
        {
            if (_digramsdata == null)
            {
                string data = Data.DIGRAMS;
                _digramsdata = new XmlDocument();
                _digramsdata.InnerXml = data;
            }

            return _digramsdata;
        }
    }
    #endregion

    #region Generator functions
    public static string Generate(RandomNumberGenerator? stream, int minlength, bool digits, string symbols, CaseMode mode, bool morepronounceable)
    {
        bool hyphened = false; // not implemented here

        PRNG prng = new PRNG(stream);
        Word randomword = new Word();

        if (digits || (symbols.Length > 0))
        {
            minlength--;
            hyphened = false;
        }

        string generated = "";

        Syllable leftovers = null;
        Syllable prevsyllable = null;
        while (randomword.Text.Length < minlength)
        {
            if (randomword.Text.Length > 0 && (digits || (symbols.Length > 0)) && (prng.Next(0, minlength - randomword.Text.Length) != 0))
            {
                if (digits && (symbols.Length == 0)) randomword.Add(new Syllable(new Digit(prng)));
                else if (!digits && (symbols.Length > 0)) randomword.Add(new Syllable(new Symbol(prng, symbols)));
                else if (prng.Next(2) == 1) randomword.Add(new Syllable(new Digit(prng)));
                else randomword.Add(new Syllable(new Symbol(prng, symbols)));
            }
            Unit? prevunit1 = null;
            Unit? prevunit2 = null;
            if (prevsyllable != null && prevsyllable.Count > 0)
            {
                prevunit2 = prevsyllable[prevsyllable.Count - 1];
                if (prevsyllable.Count > 1) prevunit1 = prevsyllable[prevsyllable.Count - 2];
            }
            Syllable newsyllable = Syllable.Random(prng, prevunit1, prevunit2, ref leftovers, morepronounceable);
            prevsyllable = newsyllable;
            randomword.Add(newsyllable);
        }
        if (digits && (symbols.Length == 0)) randomword.Add(new Syllable(new Digit(prng)));
        else if (!digits && (symbols.Length > 0)) randomword.Add(new Syllable(new Symbol(prng, symbols)));
        else if (digits && (symbols.Length > 0))
        {
            if (prng.Next(2) == 1) randomword.Add(new Syllable(new Digit(prng)));
            else randomword.Add(new Syllable(new Symbol(prng, symbols)));
        }

        switch (mode)
        {
            case CaseMode.UpperCase:
                generated = randomword.Text.ToUpper();
                break;
            case CaseMode.MixedCase:
                generated = randomword.UpperCaseSyllableStartText;
                break;
            case CaseMode.RandomCase:
                foreach (char ch in randomword.Text.ToCharArray())
                {
                    generated += prng.Next(2) > 0 ? ch.ToString().ToUpper() : ch.ToString();
                }
                break;
            case CaseMode.RandomMixedCase:
                generated = randomword.UpperCaseSyllableStartTextRandomize(prng);
                break;
            default:
                generated = hyphened ? randomword.HyphenedText : randomword.Text;
                break;
        }

        return generated;
    }
    #endregion
}

public class PronouncePwGenSubstitutionProfile
{
    Hashtable ProfileData = new Hashtable();

    /// <summary>
    /// PronouncePwGenSubstitutionProfile Constructor
    /// </summary>
    /// <param name="profilepath">File to load profile from</param>
    public PronouncePwGenSubstitutionProfile(string profilepath)
    {
        this.LoadProfile(profilepath);
    } 

    /// <summary>
    /// Load profile from file
    /// </summary>
    /// <param name="profilepath">File to load profile from</param>
    public void LoadProfile(string profilepath)
    {
        using (StreamReader profilereader = new StreamReader(profilepath))
        {
            string line = profilereader.ReadLine();
            while (line != null)
            {
                if (line.Length > 1 && line[1] == '=')
                {
                    char item = char.ToLower(line[0]);
                    if (!ProfileData.ContainsKey(item)) ProfileData[item] = line.Remove(0, 2);

                }
                line = profilereader.ReadLine();
            }
        }
    }

    /// <summary>
    /// Perform substitution lookup on character
    /// </summary>
    /// <param name="item">Character to look up a substitution for</param>
    /// <returns>substitute for item or item if substitution does not exist.</returns>
    public string Lookup(char item)
    {
        if (ProfileData.ContainsKey(char.ToLower(item))) return ProfileData[char.ToLower(item)].ToString();
        else return item.ToString();
    }

    /// <summary>
    /// Substitute all substituteable characters in subject string
    /// </summary>
    /// <param name="subject">Subject string</param>
    public string Substitute(string subject)
    {
        return Substitute(subject, null);
    }

    /// <summary>
    /// Substitutes a random set of substituteable characters in a subject string
    /// </summary>
    /// <param name="subject">Subject string</param>
    /// <param name="prng">Random number generator</param>
    public string Substitute(string subject, RandomNumberGenerator? randomstream)
    {
        PRNG prng = null;
        if (randomstream != null) prng = new PRNG(randomstream);

        string result = "";
        foreach (char item in subject.ToCharArray())
        {
            string sub = Lookup(item);
            if (!item.ToString().Equals(sub) &&
                ((prng == null) ||
                 (prng.Next(2)==1)))
                result += sub;
            else result += item;
        }
        return result;
    }
}