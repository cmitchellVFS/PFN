using System;

namespace PFN
{
    /// <summary>
    /// One game card: a short word worth fewer points and a harder phrase worth more.
    /// </summary>
    [Serializable]
    public class Card
    {
        public string word;
        public string phrase;

        public Card(string word, string phrase)
        {
            this.word = word;
            this.phrase = phrase;
        }

        public override string ToString() => $"{word} / {phrase}";
    }
}
