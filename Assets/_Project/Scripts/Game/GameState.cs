namespace PFN
{
    /// <summary>
    /// Plain data for a game in progress: two teams, their scores, and whose turn it is.
    /// No Unity dependencies, so it is easy to unit test.
    /// </summary>
    public class GameState
    {
        public const int TeamCount = 2;

        public int[] Scores { get; } = new int[TeamCount];
        public int CurrentTeam { get; private set; }
        public int Round { get; private set; } = 1;

        public int OtherTeam => 1 - CurrentTeam;

        /// <summary>True once the last team in the round has played.</summary>
        public bool IsEndOfRound => CurrentTeam == TeamCount - 1;

        public void AddPoints(int team, int points) => Scores[team] += points;

        public void AdvanceTurn()
        {
            if (IsEndOfRound) Round++;
            CurrentTeam = (CurrentTeam + 1) % TeamCount;
        }

        /// <summary>
        /// Winning team index, or -1 if nobody has won yet (or the leaders are tied).
        /// Only meaningful at the end of a round so both teams get equal turns.
        /// </summary>
        public int GetWinner(int targetScore)
        {
            int best = Scores[0] >= Scores[1] ? 0 : 1;
            if (Scores[best] < targetScore) return -1;
            if (Scores[0] == Scores[1]) return -1;
            return best;
        }
    }
}
