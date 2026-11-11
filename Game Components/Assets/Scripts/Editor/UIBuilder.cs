using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor tool that builds all of the game's menus so nobody has to lay them
/// out by hand. Tools, Grappler, Build Game UI adds the timer, the pause
/// screen, the controls screen, and the victory screen to the open game scene.
/// Tools, Grappler, Build Main Menu Scene makes the MainMenu scene with its
/// controls, best times, and settings screens.
///
/// Every button is connected to its script method, the same as setting On
/// Click in the Inspector. Running a command again rebuilds that UI. It needs
/// the TextMeshPro Essentials, which come from Window, TextMeshPro, Import TMP
/// Essential Resources.
/// </summary>
public static class UIBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    private static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.1f, 0.85f);
    private static readonly Color ButtonColor = new Color(0.2f, 0.22f, 0.28f, 1f);
    private static readonly Color AccentColor = new Color(1f, 0.6f, 0.15f, 1f);

    // The controls screen text, shown on both the main menu and the pause menu (UC2).
    private const string ControlsText =
        "Move: A / D or Left / Right arrows\n" +
        "Fire grappling hook: hold left mouse button\n" +
        "Let go: release left mouse button\n" +
        "Swing: A / D while hooked\n" +
        "Reel in: hold W or Space\n" +
        "Let out rope: hold S\n" +
        "Pause: Esc";

    // Game scene UI

    /// <summary>Builds the in-game UI. Runs from Tools, Grappler, Build Game UI.</summary>
    [MenuItem("Tools/Grappler/Build Game UI")]
    public static void BuildGameUI()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Grappler",
                "No GameManager found in this scene. Open your game scene and run Build Test Level first.", "OK");
            return;
        }

        DestroyIfExists("Game UI");
        Canvas canvas = CreateCanvas("Game UI");
        EnsureEventSystem();
        Transform root = canvas.transform;

        CreateBrightnessOverlay(root);

        // The timer at the top of the screen
        CompletionTimer timer = Object.FindAnyObjectByType<CompletionTimer>();
        TMP_Text timerLabel = MakeLabel(root, "Timer", "0:00.00", 48, new Vector2(0, -60), new Vector2(400, 80));
        RectTransform timerRect = timerLabel.rectTransform;
        timerRect.anchorMin = timerRect.anchorMax = new Vector2(0.5f, 1f); // Pinned to the top center
        timerRect.anchoredPosition = new Vector2(0, -60);
        TimerDisplay timerDisplay = timerLabel.gameObject.AddComponent<TimerDisplay>();
        SetRef(timerDisplay, "timer", timer);
        SetRef(timerDisplay, "label", timerLabel);

        PauseMenu pauseMenu = canvas.gameObject.AddComponent<PauseMenu>();
        VictoryScreen victory = canvas.gameObject.AddComponent<VictoryScreen>();

        // The pause screen
        RectTransform pausePanel = MakePanel(root, "Pause Panel");
        MakeLabel(pausePanel, "Title", "Paused", 72, new Vector2(0, 260), new Vector2(800, 120));
        MakeButton(pausePanel, "Resume Button", "Resume", new Vector2(0, 100), pauseMenu.Resume);
        MakeButton(pausePanel, "Controls Button", "Controls", new Vector2(0, 0), pauseMenu.ShowControls);
        MakeButton(pausePanel, "Restart Button", "Restart", new Vector2(0, -100), pauseMenu.Restart);
        MakeButton(pausePanel, "Main Menu Button", "Main menu", new Vector2(0, -200), pauseMenu.MainMenu);

        // The controls screen, opened from the pause screen
        RectTransform controlsPanel = ControlsPanel(root, pauseMenu.HideControls);

        // The victory screen
        RectTransform victoryPanel = MakePanel(root, "Victory Panel");
        MakeLabel(victoryPanel, "Title", "You reached the top", 72, new Vector2(0, 260), new Vector2(1200, 120));
        TMP_Text timeText = MakeLabel(victoryPanel, "Time Text", "Time: 0:00.00", 56, new Vector2(0, 130), new Vector2(800, 90));
        TMP_Text newBest = MakeLabel(victoryPanel, "New Best Banner", "New best time", 48, new Vector2(0, 40), new Vector2(800, 80));
        newBest.color = AccentColor;
        MakeButton(victoryPanel, "Play Again Button", "Play again", new Vector2(0, -100), victory.PlayAgain);
        MakeButton(victoryPanel, "Main Menu Button", "Main menu", new Vector2(0, -200), victory.MainMenu);

        SetRef(pauseMenu, "pausePanel", pausePanel.gameObject);
        SetRef(pauseMenu, "controlsPanel", controlsPanel.gameObject);
        SetRef(victory, "panel", victoryPanel.gameObject);
        SetRef(victory, "timeText", timeText);
        SetRef(victory, "newBestBanner", newBest.gameObject);
        SetRef(manager, "victoryScreen", victory);

        // Hide the screens in the Editor so they don't cover the Scene view.
        pausePanel.gameObject.SetActive(false);
        controlsPanel.gameObject.SetActive(false);
        victoryPanel.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("Grappler: game UI built. Press Ctrl+S to save the scene.");
    }

    // Main menu scene

    /// <summary>Builds and saves the MainMenu scene and adds both scenes to the build list. Runs from Tools, Grappler, Build Main Menu Scene.</summary>
    [MenuItem("Tools/Grappler/Build Main Menu Scene")]
    public static void BuildMainMenuScene()
    {
        Scene gameScene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(gameScene.path))
        {
            EditorUtility.DisplayDialog("Grappler", "Save your game scene first, then run this again.", "OK");
            return;
        }
        if (gameScene.path == MainMenuScenePath)
        {
            EditorUtility.DisplayDialog("Grappler", "Open your game scene (not the main menu) and run this again.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string gameScenePath = gameScene.path;
        string gameSceneName = gameScene.name;

        Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Canvas canvas = CreateCanvas("Main Menu UI");
        EnsureEventSystem();
        Transform root = canvas.transform;

        CreateBrightnessOverlay(root);

        MainMenu menu = canvas.gameObject.AddComponent<MainMenu>();
        SettingsMenu settings = canvas.gameObject.AddComponent<SettingsMenu>();

        // The title screen
        RectTransform mainPanel = MakePanel(root, "Main Panel");
        TMP_Text title = MakeLabel(mainPanel, "Title", "GRAPPLER", 120, new Vector2(0, 330), new Vector2(1200, 160));
        title.color = AccentColor;
        TMP_Text bestLabel = MakeLabel(mainPanel, "Best Time Label", "Best: --", 40, new Vector2(0, 220), new Vector2(800, 70));
        MakeButton(mainPanel, "Start Button", "Start", new Vector2(0, 90), menu.StartGame);
        MakeButton(mainPanel, "Controls Button", "Controls", new Vector2(0, -10), menu.ShowControls);
        MakeButton(mainPanel, "Best Times Button", "Best times", new Vector2(0, -110), menu.ShowBestTimes);
        MakeButton(mainPanel, "Settings Button", "Settings", new Vector2(0, -210), menu.ShowSettings);
        MakeButton(mainPanel, "Quit Button", "Quit", new Vector2(0, -310), menu.QuitGame);

        // The controls screen
        RectTransform controlsPanel = ControlsPanel(root, menu.BackToMain);

        // The best times screen
        RectTransform bestPanel = MakePanel(root, "Best Times Panel");
        MakeLabel(bestPanel, "Title", "Best times", 72, new Vector2(0, 330), new Vector2(800, 120));
        TMP_Text bestList = MakeLabel(bestPanel, "Times List", "", 40, new Vector2(0, 20), new Vector2(800, 480));
        bestList.alignment = TextAlignmentOptions.Top;
        MakeButton(bestPanel, "Back Button", "Back", new Vector2(0, -330), menu.BackToMain);

        // The settings screen
        RectTransform settingsPanel = MakePanel(root, "Settings Panel");
        MakeLabel(settingsPanel, "Title", "Settings", 72, new Vector2(0, 330), new Vector2(800, 120));
        Slider music = LabeledSlider(settingsPanel, "Music", "Music volume", new Vector2(0, 160), 0f, 1f);
        Slider sfx = LabeledSlider(settingsPanel, "Sound", "Sound volume", new Vector2(0, 30), 0f, 1f);
        Slider bright = LabeledSlider(settingsPanel, "Brightness", "Brightness", new Vector2(0, -100), GameSettings.MinBrightness, 1f);
        MakeButton(settingsPanel, "Back Button", "Back", new Vector2(0, -330), menu.BackToMain);

        SetString(menu, "gameSceneName", gameSceneName);
        SetRef(menu, "mainPanel", mainPanel.gameObject);
        SetRef(menu, "controlsPanel", controlsPanel.gameObject);
        SetRef(menu, "bestTimesPanel", bestPanel.gameObject);
        SetRef(menu, "settingsPanel", settingsPanel.gameObject);
        SetRef(menu, "bestTimeLabel", bestLabel);
        SetRef(menu, "bestTimesList", bestList);
        SetRef(settings, "musicSlider", music);
        SetRef(settings, "sfxSlider", sfx);
        SetRef(settings, "brightnessSlider", bright);

        controlsPanel.gameObject.SetActive(false);
        bestPanel.gameObject.SetActive(false);
        settingsPanel.gameObject.SetActive(false);

        EditorSceneManager.SaveScene(menuScene, MainMenuScenePath);

        // The main menu goes first in the build list, so the built game opens on the menu.
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(gameScenePath, true),
        };
        scenes.AddRange(EditorBuildSettings.scenes.Where(s => s.path != MainMenuScenePath && s.path != gameScenePath));
        EditorBuildSettings.scenes = scenes.ToArray();

        EditorSceneManager.OpenScene(gameScenePath);
        Debug.Log($"Grappler: built {MainMenuScenePath} and added it to the build before {gameSceneName}. " +
                  "Open the MainMenu scene and press Play to try it.");
    }

    // Helpers

    // The controls screen with a Back button. Both menus use this.
    private static RectTransform ControlsPanel(Transform parent, UnityAction onBack)
    {
        RectTransform panel = MakePanel(parent, "Controls Panel");
        MakeLabel(panel, "Title", "Controls", 72, new Vector2(0, 330), new Vector2(800, 120));
        TMP_Text body = MakeLabel(panel, "Controls Text", ControlsText, 38, new Vector2(0, 30), new Vector2(1100, 460));
        body.alignment = TextAlignmentOptions.Top;
        MakeButton(panel, "Back Button", "Back", new Vector2(0, -330), onBack);
        return panel;
    }

    // A screen-space canvas that scales with the window size.
    private static Canvas CreateCanvas(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Build UI");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // Makes sure the scene has an EventSystem, since buttons don't work without one.
    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        // The project only uses the new Input System, so the UI has to use its input module too.
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(go, "Build UI");
    }

    // The full-screen black image that BrightnessOverlay controls (UC13).
    private static void CreateBrightnessOverlay(Transform parent)
    {
        RectTransform overlay = Stretch(new GameObject("Brightness Overlay", typeof(RectTransform)), parent);
        var image = overlay.gameObject.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0);
        image.raycastTarget = false;
        overlay.gameObject.AddComponent<BrightnessOverlay>();
        overlay.SetAsFirstSibling(); // Drawn behind the menus but in front of the game.
    }

    // A dark panel that covers the whole screen, used for each menu screen.
    private static RectTransform MakePanel(Transform parent, string name)
    {
        RectTransform rect = Stretch(new GameObject(name, typeof(RectTransform)), parent);
        rect.gameObject.AddComponent<Image>().color = PanelColor;
        return rect;
    }

    // Makes a UI object fill its whole parent.
    private static RectTransform Stretch(GameObject go, Transform parent)
    {
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    // A TextMeshPro text box at a position on the screen.
    private static TMP_Text MakeLabel(Transform parent, string name, string text, float size, Vector2 position, Vector2 boxSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = boxSize;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    // A button with a label that calls onClick when pressed.
    private static Button MakeButton(Transform parent, string name, string text, Vector2 position, UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(420, 80);

        var image = go.AddComponent<Image>();
        image.color = ButtonColor;
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text label = MakeLabel(rect, "Text", text, 40, Vector2.zero, Vector2.zero);
        Stretch(label.gameObject, rect);

        // A persistent listener saves with the scene, the same as setting On Click in the Inspector.
        UnityEventTools.AddPersistentListener(button.onClick, onClick);
        return button;
    }

    // A slider with a label above it, used on the settings screen.
    private static Slider LabeledSlider(Transform parent, string name, string text, Vector2 position, float min, float max)
    {
        TMP_Text label = MakeLabel(parent, name + " Label", text, 38, position + new Vector2(0, 45), new Vector2(600, 60));
        label.alignment = TextAlignmentOptions.Center;

        GameObject sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderGo.name = name + " Slider";
        var rect = (RectTransform)sliderGo.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(600, 30);

        // DefaultControls makes plain white images with no sprites, so they get tinted here.
        foreach (Image img in sliderGo.GetComponentsInChildren<Image>(true))
        {
            img.color = img.name switch
            {
                "Background" => ButtonColor,
                "Fill" => AccentColor,
                _ => Color.white, // Handle
            };
        }

        var slider = sliderGo.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = max;
        return slider;
    }

    // Sets a private [SerializeField] field so it saves with the scene.
    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogWarning($"Grappler: field \"{field}\" not found on {target.GetType().Name}.");
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    // Sets a private text field so it saves with the scene.
    private static void SetString(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null) return;
        prop.stringValue = value;
        so.ApplyModifiedProperties();
    }

    // Deletes the object with this name from the scene, if there is one.
    private static void DestroyIfExists(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) Undo.DestroyObjectImmediate(existing);
    }
}
