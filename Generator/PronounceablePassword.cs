using System.Security.Cryptography;

namespace PronouncablePasswordGenerator.Generator;
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

public enum CaseMode
{
    LowerCase = 0,
    UpperCase = 1,
    MixedCase = 2,
    RandomCase = 3,
    RandomMixedCase = 4
}

public static class PronounceablePassword
{
    public static string Generate(RandomNumberGenerator stream, int minlength, bool digits, string symbols, CaseMode mode, bool morepronounceable)
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
}