using UnityEngine;

namespace PFN
{
    /// <summary>
    /// Runs the game flow for one shared phone (pass-and-play):
    /// Menu -> Ready (pass to poet) -> Turn (timer running) -> Summary -> Ready ... -> Game Over.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] GameSettings settings;
        [SerializeField] TextAsset cardsCsv;
        [SerializeField] GameUI ui;

        GameState state;
        Deck deck;
        Card currentCard;
        bool wordScored;
        float timeLeft;
        bool turnRunning;
        int turnPoints;       // earned by the poet's team this turn
        int opponentPoints;   // given to the other team via bonks this turn
        int cardsPlayed;
        bool paused;
        int shownSecond = -1;
        bool shownWarning;

        public GameSettings Settings => settings;
        public GameState State => state;

void Awake()
        {
            // Frame rate is handled by PowerSaver. Let the screen dim outside of turns.
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        void Start()
        {
            var cards = CardDatabase.Parse(cardsCsv != null ? cardsCsv.text : null);
            if (cards.Count == 0) Debug.LogError("[PFN] No cards loaded. Assign a CSV to GameManager.");
            deck = new Deck(cards);
            Debug.Log($"[PFN] Loaded {cards.Count} cards.");

            ui.Bind(this);
            ui.ShowMenu();
        }

void Update()
        {
            if (!turnRunning || paused) return;

            timeLeft -= Time.deltaTime;

            // Only touch the UI when the visible value changes: rewriting text every
            // frame forces TextMesh Pro and the canvas to rebuild 30-60 times a second.
            int second = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
            bool warning = timeLeft <= settings.warningSeconds;
            if (second != shownSecond || warning != shownWarning)
            {
                shownSecond = second;
                shownWarning = warning;
                ui.SetTimer(second, warning);
            }

            if (timeLeft <= 0f) EndTurn();
        }

/// <summary>Freeze the turn timer while the app is in the background.</summary>
        void OnApplicationPause(bool pauseStatus)
        {
            paused = pauseStatus;
        }


        // ---- Flow ----

        public void StartGame()
        {
            state = new GameState();
            ui.ShowReady(state, settings);
        }

public void StartTurn()
        {
            timeLeft = settings.turnSeconds;
            turnPoints = 0;
            opponentPoints = 0;
            cardsPlayed = 0;
            shownSecond = -1;
            paused = false;
            turnRunning = true;

            // Keep the screen awake only while the timer runs.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            ui.ShowTurn(state, settings);
            DrawCard();
        }

        public void ContinueAfterSummary()
        {
            if (state.IsEndOfRound)
            {
                int winner = state.GetWinner(settings.targetScore);
                if (winner >= 0)
                {
                    ui.ShowGameOver(state, settings, winner);
                    return;
                }
            }
            state.AdvanceTurn();
            ui.ShowReady(state, settings);
        }

public void BackToMenu()
        {
            turnRunning = false;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            ui.ShowMenu();
        }

        // ---- Turn actions (buttons) ----

        /// <summary>Guessers got the short word. Card stays up so they can try the phrase.</summary>
        public void ScoreWord()
        {
            if (!turnRunning || wordScored) return;
            wordScored = true;
            AwardPoetTeam(settings.wordPoints);
            ui.ShowCard(currentCard, wordScored);
        }

        /// <summary>Guessers got the phrase. Card is done.</summary>
        public void ScorePhrase()
        {
            if (!turnRunning) return;
            AwardPoetTeam(settings.phrasePoints);
            DrawCard();
        }

        /// <summary>Poet broke the one-syllable rule. Other team scores, card is done.</summary>
        public void Bonk()
        {
            if (!turnRunning) return;
            state.AddPoints(state.OtherTeam, settings.bonkPointsToOpponent);
            opponentPoints += settings.bonkPointsToOpponent;
            ui.FlashBonk();
            ui.RefreshScores(state);
            DrawCard();
        }

        /// <summary>Give up on this card (after the word, this just moves on).</summary>
        public void Skip()
        {
            if (!turnRunning) return;
            if (!wordScored) AwardPoetTeam(settings.skipPoints);
            DrawCard();
        }

        public void EndTurnEarly()
        {
            if (turnRunning) EndTurn();
        }

        // ---- Internals ----

        void DrawCard()
        {
            currentCard = deck.Draw();
            wordScored = false;
            cardsPlayed++;
            ui.ShowCard(currentCard, wordScored);
        }

        void AwardPoetTeam(int points)
        {
            state.AddPoints(state.CurrentTeam, points);
            turnPoints += points;
            ui.RefreshScores(state);
        }

void EndTurn()
        {
            turnRunning = false;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            ui.ShowSummary(state, settings, turnPoints, opponentPoints);
        }
    }
}
