using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PFN
{
    /// <summary>
    /// View layer: switches screens, writes text, and forwards button presses to GameManager.
    /// References are wired by the PFN > Build Game Scene editor menu.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("Screens")]
        public GameObject menuScreen;
        public GameObject readyScreen;
        public GameObject turnScreen;
        public GameObject summaryScreen;
        public GameObject gameOverScreen;

        [Header("Menu")]
        public Button startButton;

        [Header("Ready")]
        public TMP_Text readyTitle;
        public TMP_Text readyScores;
        public Button readyStartTurnButton;

        [Header("Turn")]
        public TMP_Text turnTeamLabel;
        public TMP_Text timerText;
        public TMP_Text turnScores;
        public TMP_Text wordText;
        public TMP_Text phraseText;
        public TMP_Text wordPointsLabel;
        public TMP_Text phrasePointsLabel;
        public Button wordButton;
        public Button phraseButton;
        public Button bonkButton;
        public Button skipButton;
        public TMP_Text skipButtonLabel;
        public Button endTurnButton;
        public Image bonkFlash;

        [Header("Summary")]
        public TMP_Text summaryText;
        public Button summaryContinueButton;

        [Header("Game Over")]
        public TMP_Text gameOverText;
        public Button gameOverMenuButton;

        [Header("Colours")]
        public Color timerNormal = Color.white;
        public Color timerWarning = new Color(1f, 0.35f, 0.25f);

        GameManager game;
        Coroutine flashRoutine;

        public void Bind(GameManager manager)
        {
            game = manager;
            startButton.onClick.AddListener(game.StartGame);
            readyStartTurnButton.onClick.AddListener(game.StartTurn);
            wordButton.onClick.AddListener(game.ScoreWord);
            phraseButton.onClick.AddListener(game.ScorePhrase);
            bonkButton.onClick.AddListener(game.Bonk);
            skipButton.onClick.AddListener(game.Skip);
            endTurnButton.onClick.AddListener(game.EndTurnEarly);
            summaryContinueButton.onClick.AddListener(game.ContinueAfterSummary);
            gameOverMenuButton.onClick.AddListener(game.BackToMenu);
            if (bonkFlash != null) bonkFlash.gameObject.SetActive(false);
        }

        // ---- Screens ----

        public void ShowMenu() => Show(menuScreen);

        public void ShowReady(GameState state, GameSettings s)
        {
            Show(readyScreen);
            readyTitle.text = $"Round {state.Round}\n<b>{s.teamNames[state.CurrentTeam]}</b>\n\nPass the phone to your poet.\nOnly the poet looks!";
            readyScores.text = ScoreLine(state, s);
        }

        public void ShowTurn(GameState state, GameSettings s)
        {
            Show(turnScreen);
            if (flashRoutine != null) { StopCoroutine(flashRoutine); flashRoutine = null; }
            if (bonkFlash != null) bonkFlash.gameObject.SetActive(false);
            turnTeamLabel.text = s.teamNames[state.CurrentTeam];
            wordPointsLabel.text = $"{s.wordPoints} pt";
            phrasePointsLabel.text = $"{s.phrasePoints} pts";
            wordButton.GetComponentInChildren<TMP_Text>().text = $"Got word  +{s.wordPoints}";
            phraseButton.GetComponentInChildren<TMP_Text>().text = $"Got phrase  +{s.phrasePoints}";
            RefreshScores(state);
            SetTimer(s.turnSeconds, false);
        }

        public void ShowSummary(GameState state, GameSettings s, int turnPoints, int opponentPoints)
        {
            Show(summaryScreen);
            string sign = turnPoints >= 0 ? "+" : "";
            string bonks = opponentPoints > 0
                ? $"\n{s.teamNames[state.OtherTeam]} got +{opponentPoints} from bonks"
                : "";
            summaryText.text = $"Time's up!\n\n<b>{s.teamNames[state.CurrentTeam]}</b> {sign}{turnPoints}{bonks}\n\n{ScoreLine(state, s)}";
        }

        public void ShowGameOver(GameState state, GameSettings s, int winner)
        {
            Show(gameOverScreen);
            gameOverText.text = $"<b>{s.teamNames[winner]}</b>\nwins!\n\n{ScoreLine(state, s)}";
        }

        // ---- Turn updates ----

        public void ShowCard(Card card, bool wordScored)
        {
            wordText.text = card != null ? card.word : "(no cards)";
            phraseText.text = card != null ? card.phrase : "";
            wordText.fontStyle = wordScored ? FontStyles.Strikethrough : FontStyles.Bold;
            wordButton.interactable = !wordScored;
            skipButtonLabel.text = wordScored ? "Next card" : "Skip";
            PowerSaver.Wake(0.2f);
        }

public void SetTimer(float seconds, bool warning)
        {
            int s = Mathf.CeilToInt(seconds);
            timerText.text = $"{s / 60}:{s % 60:00}";
            timerText.color = warning ? timerWarning : timerNormal;
            PowerSaver.Wake(0.1f); // get the new second on screen promptly
        }

public void RefreshScores(GameState state)
        {
            turnScores.text = $"{state.Scores[0]} : {state.Scores[1]}";
            PowerSaver.Wake(0.1f);
        }

        public void FlashBonk()
        {
            if (bonkFlash == null) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            PowerSaver.Wake(0.45f); // render the whole flash animation at full rate
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            bonkFlash.gameObject.SetActive(true);
            var c = bonkFlash.color;
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                c.a = Mathf.Lerp(0.6f, 0f, t / 0.35f);
                bonkFlash.color = c;
                yield return null;
            }
            bonkFlash.gameObject.SetActive(false);
        }

        // ---- Helpers ----

        void Show(GameObject screen)
        {
            menuScreen.SetActive(screen == menuScreen);
            readyScreen.SetActive(screen == readyScreen);
            turnScreen.SetActive(screen == turnScreen);
            summaryScreen.SetActive(screen == summaryScreen);
            gameOverScreen.SetActive(screen == gameOverScreen);
            PowerSaver.Wake(0.3f);
        }

        static string ScoreLine(GameState state, GameSettings s) =>
            $"{s.teamNames[0]}  {state.Scores[0]}\n{s.teamNames[1]}  {state.Scores[1]}";
    }
}
