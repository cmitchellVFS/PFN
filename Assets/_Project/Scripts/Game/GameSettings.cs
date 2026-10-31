using UnityEngine;

namespace PFN
{
    /// <summary>
    /// Tunable rules. Defaults are placeholders: confirm them against the rulebook.
    /// </summary>
    [CreateAssetMenu(menuName = "PFN/Game Settings", fileName = "GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Teams")]
        public string[] teamNames = { "Team Rock", "Team Stick" };

        [Header("Timing")]
        [Min(5)] public float turnSeconds = 90f;
        [Tooltip("Seconds left when the timer turns to its warning colour.")]
        [Min(0)] public float warningSeconds = 10f;

        [Header("Scoring")]
        [Tooltip("Points to the poet's team when the guessers get the word.")]
        public int wordPoints = 1;
        [Tooltip("Points to the poet's team when the guessers get the phrase.")]
        public int phrasePoints = 3;
        [Tooltip("Points to the OTHER team when the poet breaks the one-syllable rule.")]
        public int bonkPointsToOpponent = 1;
        [Tooltip("Points to the poet's team for skipping a card (usually negative or zero).")]
        public int skipPoints = -1;

        [Header("Winning")]
        [Tooltip("Game ends at the end of a full round once a team reaches this score.")]
        [Min(1)] public int targetScore = 30;
    }
}
