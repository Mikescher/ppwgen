using System.Collections;

namespace PronouncablePasswordGenerator.Generator;

public class Word
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