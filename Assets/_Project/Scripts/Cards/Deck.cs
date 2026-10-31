using System.Collections.Generic;

namespace PFN
{
    /// <summary>
    /// Shuffled draw pile. When it runs out, every card is reshuffled back in,
    /// avoiding an immediate repeat of the last card drawn when possible.
    /// </summary>
    public class Deck
    {
        readonly List<Card> allCards;
        readonly List<Card> drawPile = new List<Card>();
        readonly System.Random rng;
        Card lastDrawn;

        public int Count => allCards.Count;
        public int Remaining => drawPile.Count;

        public Deck(IEnumerable<Card> cards, int? seed = null)
        {
            allCards = new List<Card>(cards);
            rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            Refill();
        }

        public Card Draw()
        {
            if (allCards.Count == 0) return null;
            if (drawPile.Count == 0) Refill();

            int last = drawPile.Count - 1;
            var card = drawPile[last];
            drawPile.RemoveAt(last);
            lastDrawn = card;
            return card;
        }

        void Refill()
        {
            drawPile.Clear();
            drawPile.AddRange(allCards);

            // Fisher-Yates shuffle
            for (int i = drawPile.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (drawPile[i], drawPile[j]) = (drawPile[j], drawPile[i]);
            }

            // Draw takes from the end; don't let the reshuffle hand back the card we just saw.
            int top = drawPile.Count - 1;
            if (drawPile.Count > 1 && drawPile[top] == lastDrawn)
                (drawPile[top], drawPile[0]) = (drawPile[0], drawPile[top]);
        }
    }
}
