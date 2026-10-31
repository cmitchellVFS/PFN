using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace PFN.EditorTools
{
    /// <summary>
    /// Generates the Game scene, its UI and data assets from code, so the layout can be
    /// rebuilt at any time with PFN > Build Game Scene. Re-running overwrites Game.unity.
    /// </summary>
    public static class PFNSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Game.unity";
        const string SettingsPath = Root + "/Data/GameSettings.asset";
        const string CardsPath = Root + "/Data/cards.csv";

        const string TestCards =
            "word,phrase\n" +
            "cat,cat nap\n" +
            "dog,hot dog\n" +
            "horse,horse race\n";

        // Palette
        static readonly Color Cave = Hex("2B2118");
        static readonly Color Parchment = Hex("F2E3C6");
        static readonly Color Ink = Hex("2B2118");
        static readonly Color Green = Hex("4E8F3A");
        static readonly Color Blue = Hex("3A6E8F");
        static readonly Color Red = Hex("B23A2E");
        static readonly Color Stone = Hex("6B5E50");
        static readonly Color DarkStone = Hex("463B30");

        [MenuItem("PFN/Build Game Scene")]
        public static void Build()
        {
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Data");

            EnsureSettings();
            EnsureCards();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load after NewScene: opening a scene unloads freshly created, unreferenced assets.
            var settings = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            var cards = AssetDatabase.LoadAssetAtPath<TextAsset>(CardsPath);

            // Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Cave;
            cam.orthographic = true;
            // UI is a Screen Space Overlay canvas, so the camera only needs to clear the screen.
            cam.cullingMask = 0;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = false;
            camData.antialiasing = AntialiasingMode.None;
            camData.renderShadows = false;
            camData.requiresDepthTexture = false;
            camData.requiresColorTexture = false;

            // Event system (Input System package)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // Canvas
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var ui = canvasGo.AddComponent<GameUI>();
            var canvas = canvasGo.transform;

            // ---- Menu ----
            var menu = MakeScreen("MenuScreen", canvas);
            Spacer(menu);
            Text(menu, "Title", "POETRY FOR\nNEANDERTHALS", 110, 400, Parchment, FontStyles.Bold);
            Text(menu, "Subtitle", "Say it with small words.", 50, 120, Parchment, FontStyles.Italic);
            Spacer(menu);
            ui.startButton = MakeButton(menu, "StartButton", "Start game", Green, 220);
            Spacer(menu, 0.3f);

            // ---- Ready ----
            var ready = MakeScreen("ReadyScreen", canvas);
            Spacer(ready);
            ui.readyTitle = Text(ready, "ReadyTitle", "Team\nPass the phone", 70, 600, Parchment);
            ui.readyScores = Text(ready, "ReadyScores", "0 : 0", 50, 200, Parchment);
            Spacer(ready);
            ui.readyStartTurnButton = MakeButton(ready, "StartTurnButton", "I'm the poet. Go!", Green, 220);
            Spacer(ready, 0.3f);

            // ---- Turn ----
            var turn = MakeScreen("TurnScreen", canvas);
            var topBar = Row(turn, "TopBar", 140);
            ui.turnTeamLabel = Text(topBar, "TeamLabel", "Team", 46, 140, Parchment, FontStyles.Bold, TextAlignmentOptions.Left);
            ui.timerText = Text(topBar, "Timer", "1:30", 96, 140, Parchment, FontStyles.Bold);
            ui.turnScores = Text(topBar, "Scores", "0 : 0", 56, 140, Parchment, FontStyles.Normal, TextAlignmentOptions.Right);
            // Text that changes often: fixed size (auto-size re-measures on every change),
            // and the timer gets its own canvas so its ticks don't rebuild the rest of the UI.
            ui.timerText.enableAutoSizing = false;
            ui.turnScores.enableAutoSizing = false;
            ui.timerText.gameObject.AddComponent<Canvas>();

            var card = MakePanel(turn, "Card", Parchment, 1000);
            ui.wordPointsLabel = Text(card, "WordPoints", "1 pt", 40, 70, Stone, FontStyles.Bold);
            ui.wordText = Text(card, "Word", "word", 120, 220, Ink, FontStyles.Bold);
            Spacer(card, 0.5f);
            ui.phrasePointsLabel = Text(card, "PhrasePoints", "3 pts", 40, 70, Stone, FontStyles.Bold);
            ui.phraseText = Text(card, "Phrase", "phrase", 84, 320, Ink, FontStyles.Bold);

            Spacer(turn, 0.3f);
            var scoreRow = Row(turn, "ScoreRow", 200);
            ui.wordButton = MakeButton(scoreRow, "WordButton", "Got word +1", Green, 200);
            ui.phraseButton = MakeButton(scoreRow, "PhraseButton", "Got phrase +3", Blue, 200);

            var penaltyRow = Row(turn, "PenaltyRow", 170);
            ui.bonkButton = MakeButton(penaltyRow, "BonkButton", "BONK!", Red, 170);
            ui.skipButton = MakeButton(penaltyRow, "SkipButton", "Skip", Stone, 170);
            ui.skipButtonLabel = ui.skipButton.GetComponentInChildren<TMP_Text>();

            ui.endTurnButton = MakeButton(turn, "EndTurnButton", "End turn", DarkStone, 100, 40);

            var flash = NewUI("BonkFlash", turn);
            Stretch(flash);
            flash.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            ui.bonkFlash = flash.gameObject.AddComponent<Image>();
            ui.bonkFlash.color = new Color(Red.r, Red.g, Red.b, 0.6f);
            ui.bonkFlash.raycastTarget = false;

            // ---- Summary ----
            var summary = MakeScreen("SummaryScreen", canvas);
            Spacer(summary);
            ui.summaryText = Text(summary, "SummaryText", "Time's up!", 64, 800, Parchment);
            Spacer(summary);
            ui.summaryContinueButton = MakeButton(summary, "ContinueButton", "Next team", Green, 220);
            Spacer(summary, 0.3f);

            // ---- Game over ----
            var over = MakeScreen("GameOverScreen", canvas);
            Spacer(over);
            ui.gameOverText = Text(over, "GameOverText", "Team wins!", 90, 900, Parchment, FontStyles.Bold);
            Spacer(over);
            ui.gameOverMenuButton = MakeButton(over, "MenuButton", "Back to menu", Green, 220);
            Spacer(over, 0.3f);

            ui.menuScreen = menu.gameObject;
            ui.readyScreen = ready.gameObject;
            ui.turnScreen = turn.gameObject;
            ui.summaryScreen = summary.gameObject;
            ui.gameOverScreen = over.gameObject;
            ready.gameObject.SetActive(false);
            turn.gameObject.SetActive(false);
            summary.gameObject.SetActive(false);
            over.gameObject.SetActive(false);

            // ---- Game manager ----
            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<PowerSaver>();
            var gm = gmGo.AddComponent<GameManager>();
            var so = new SerializedObject(gm);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("cardsCsv").objectReferenceValue = cards;
            so.FindProperty("ui").objectReferenceValue = ui;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            Debug.Log($"[PFN] Built {ScenePath}");
        }

        // ---- Assets ----

        static GameSettings EnsureSettings()
        {
            var s = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            if (s != null) return s;
            s = ScriptableObject.CreateInstance<GameSettings>();
            AssetDatabase.CreateAsset(s, SettingsPath);
            AssetDatabase.SaveAssets();
            return s;
        }

        static TextAsset EnsureCards()
        {
            if (!File.Exists(CardsPath))
            {
                File.WriteAllText(CardsPath, TestCards);
                AssetDatabase.ImportAsset(CardsPath);
            }
            return AssetDatabase.LoadAssetAtPath<TextAsset>(CardsPath);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ---- UI helpers ----

        static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static RectTransform MakeScreen(string name, Transform parent)
        {
            var rt = NewUI(name, parent);
            Stretch(rt);
            rt.gameObject.AddComponent<Image>().color = Cave;
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(60, 60, 80, 60);
            v.spacing = 30;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return rt;
        }

        static RectTransform MakePanel(Transform parent, string name, Color color, float height)
        {
            var rt = NewUI(name, parent);
            rt.gameObject.AddComponent<Image>().color = color;
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(40, 40, 50, 50);
            v.spacing = 10;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return rt;
        }

        static RectTransform Row(Transform parent, string name, float height)
        {
            var rt = NewUI(name, parent);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 30;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return rt;
        }

        static void Spacer(Transform parent, float flex = 1f)
        {
            var rt = NewUI("Spacer", parent);
            rt.gameObject.AddComponent<LayoutElement>().flexibleHeight = flex;
        }

        static TMP_Text Text(Transform parent, string name, string text, float size, float height, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = NewUI(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.enableAutoSizing = true;
            t.fontSizeMin = size * 0.5f;
            t.fontSizeMax = size;
            t.raycastTarget = false;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;
            return t;
        }

        static Button MakeButton(Transform parent, string name, string label, Color color, float height, float fontSize = 56)
        {
            var rt = NewUI(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            b.colors = colors;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;

            var labelRt = NewUI("Label", rt);
            Stretch(labelRt);
            var t = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = label;
            t.fontSize = fontSize;
            t.fontStyle = FontStyles.Bold;
            t.color = Parchment;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return b;
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
