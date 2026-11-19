using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NullProgrammer.Audio
{
    /// <summary>
    /// Runtime settings, credits, and pause menu.
    /// AudioDirector creates this component and persists it across scenes.
    /// </summary>
    public sealed class AudioSettingsPanel : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        private AudioDirector owner;
        private GameObject root;
        private GameObject overlay;
        private GameObject settingsPanel;
        private GameObject creditsPanel;
        private GameObject ownedEventSystem;

        private readonly List<Collider2D> disabled2D = new List<Collider2D>();
        private readonly List<Collider> disabled3D = new List<Collider>();

        private bool isStartScreen;
        private bool openedFromTitle;
        private float previousTimeScale = 1f;

        private void Start()
        {
            owner = GetComponent<AudioDirector>();
            if (owner == null || owner.Catalog == null)
                return;

            BuildUI();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SetSceneMode(SceneManager.GetActiveScene().name);
        }

        private void Update()
        {
            if (NullProgrammer.Showcase.ShowcasePanel.IsOpen) return;

            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (IsOpen)
                    Close();
                else if (!isStartScreen)
                    OpenPauseMenu();
            }

            // Keep the old F10 shortcut working for anyone already using it.
            if (Keyboard.current != null &&
                Keyboard.current.f10Key.wasPressedThisFrame &&
                !isStartScreen)
            {
                if (IsOpen) Close();
                else OpenPauseMenu();
            }

            EnsureEventSystem();
        }

        private void BuildUI()
        {
            root = new GameObject(
                "Game Settings UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            root.transform.SetParent(transform, false);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 1000);
            scaler.matchWidthOrHeight = 0.5f;

            overlay = CreatePanel(root.transform, "Overlay", Vector2.zero,
                new Color(0f, 0f, 0f, 0.72f));

            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            settingsPanel = CreatePanel(overlay.transform, "Settings Panel",
                new Vector2(620, 590), new Color(0.035f, 0.07f, 0.12f, 0.99f));
            Center(settingsPanel.GetComponent<RectTransform>());

            CreateLabel(settingsPanel.transform, "SETTINGS", 34,
                new Vector2(0, 245), new Vector2(540, 55));

            CreateVolumeSlider(settingsPanel.transform, "Master Volume",
                owner.Master, 145,
                v => owner.SetVolumes(v, owner.Music, owner.Effects));

            CreateVolumeSlider(settingsPanel.transform, "Music Volume",
                owner.Music, 35,
                v => owner.SetVolumes(owner.Master, v, owner.Effects));

            CreateVolumeSlider(settingsPanel.transform, "Effects Volume",
                owner.Effects, -75,
                v => owner.SetVolumes(owner.Master, owner.Music, v));

            CreateButton(settingsPanel.transform, "Credits", new Vector2(170, 48),
                new Vector2(-190, -205), OpenCredits);

            CreateButton(settingsPanel.transform, "Resume", new Vector2(170, 48),
                new Vector2(0, -205), Close);

            CreateButton(settingsPanel.transform, "Quit Game", new Vector2(170, 48),
                new Vector2(190, -205), QuitGame);

            creditsPanel = CreatePanel(overlay.transform, "Credits Panel",
                new Vector2(900, 700), new Color(0.035f, 0.07f, 0.12f, 0.99f));
            Center(creditsPanel.GetComponent<RectTransform>());

            CreateLabel(creditsPanel.transform, "CREDITS", 34,
                new Vector2(0, 295), new Vector2(820, 55));

            string creditsText = string.IsNullOrWhiteSpace(owner.Catalog.credits)
                ? "Credits have not been added yet."
                : owner.Catalog.credits;

            CreateLabel(creditsPanel.transform, creditsText, 19,
                new Vector2(0, -5), new Vector2(810, 550));

            CreateButton(creditsPanel.transform, "Back", new Vector2(200, 48),
                new Vector2(0, -315), BackFromCredits);

            overlay.SetActive(false);
            creditsPanel.SetActive(false);
        }

        public void OpenTitleSettings()
        {
            if (!isStartScreen || IsOpen || overlay == null) return;
            openedFromTitle = true;
            OpenOverlay();

            // On the title screen, only the master volume slider is displayed.
            SetControlVisible("Music Volume Slider", false);
            SetControlVisible("Music Volume Label", false);
            SetControlVisible("Effects Volume Slider", false);
            SetControlVisible("Effects Volume Label", false);
            SetControlVisible("Credits", false);
            SetControlVisible("Resume", true);
            SetControlVisible("Quit Game", false);

            settingsPanel.GetComponent<RectTransform>().sizeDelta =
                new Vector2(620, 360);

            SetControlPosition("SETTINGS", new Vector2(0, 125));
            SetButtonLabel("Resume", "Back");
            SetControlPosition("Resume", new Vector2(0, -105));
            SetControlPosition("Master Volume Label", new Vector2(0, 95));
            SetControlPosition("Master Volume Slider", new Vector2(0, 45));
        }

        public void OpenTitleCredits()
        {
            if (!isStartScreen || IsOpen || overlay == null) return;
            openedFromTitle = true;
            OpenOverlay();
            settingsPanel.SetActive(false);
            creditsPanel.SetActive(true);
        }

        private void OpenPauseMenu()
        {
            openedFromTitle = false;
            OpenOverlay();

            SetControlVisible("Music Volume Slider", true);
            SetControlVisible("Music Volume Label", true);
            SetControlVisible("Effects Volume Slider", true);
            SetControlVisible("Effects Volume Label", true);
            SetControlVisible("Credits", true);
            SetControlVisible("Resume", true);
            SetControlVisible("Quit Game", true);

            settingsPanel.GetComponent<RectTransform>().sizeDelta =
                new Vector2(620, 590);

            SetControlPosition("SETTINGS", new Vector2(0, 245));
            SetButtonLabel("Resume", "Resume");
            SetControlPosition("Resume", new Vector2(0, -205));
            SetControlPosition("Master Volume Label", new Vector2(0, 174));
            SetControlPosition("Master Volume Slider", new Vector2(0, 124));
        }

        private void OpenOverlay()
        {
            if (IsOpen)
                return;

            IsOpen = true;
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            settingsPanel.SetActive(true);
            creditsPanel.SetActive(false);

            if (!isStartScreen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                DisableWorldColliders();
            }
        }

        private void OpenCredits()
        {
            settingsPanel.SetActive(false);
            creditsPanel.SetActive(true);
        }

        private void BackFromCredits()
        {
            creditsPanel.SetActive(false);

            if (openedFromTitle)
                Close();
            else
                settingsPanel.SetActive(true);
        }

        private void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            overlay.SetActive(false);
            settingsPanel.SetActive(true);
            creditsPanel.SetActive(false);

            RestoreWorldColliders();

            if (!isStartScreen)
                Time.timeScale = previousTimeScale;

            owner.SaveSettings();
        }

        private void DisableWorldColliders()
        {
            foreach (Collider2D collider in
                     FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
            {
                if (collider.enabled)
                {
                    disabled2D.Add(collider);
                    collider.enabled = false;
                }
            }

            foreach (Collider collider in
                     FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (collider.enabled)
                {
                    disabled3D.Add(collider);
                    collider.enabled = false;
                }
            }
        }

        private void RestoreWorldColliders()
        {
            foreach (Collider2D collider in disabled2D)
                if (collider != null) collider.enabled = true;

            foreach (Collider collider in disabled3D)
                if (collider != null) collider.enabled = true;

            disabled2D.Clear();
            disabled3D.Clear();
        }

        private void QuitGame()
        {
            owner.SaveSettings();
            Time.timeScale = 1f;
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || root == null)
                return;

            if (IsOpen)
            {
                IsOpen = false;
                overlay.SetActive(false);
                RestoreWorldColliders();
                Time.timeScale = previousTimeScale;
            }

            SetSceneMode(scene.name);
        }

        private void SetSceneMode(string sceneName)
        {
            isStartScreen = sceneName == "StartScreen";

            // Start-screen Settings and Credits buttons are authored in the scene.
            // Add TitleScreenSettingsButtons to the scene Canvas and wire the
            // Button OnClick events in the Inspector; no buttons are spawned here.

            // A scene transition can happen after the title-screen layout was
            // used, so restore the normal pause-menu layout when entering play.
            SetControlVisible("Music Volume Slider", !isStartScreen);
            SetControlVisible("Music Volume Label", !isStartScreen);
            SetControlVisible("Effects Volume Slider", !isStartScreen);
            SetControlVisible("Effects Volume Label", !isStartScreen);
            SetControlVisible("Credits", !isStartScreen);
            SetControlVisible("Resume", true);
            SetControlVisible("Quit Game", !isStartScreen);

            settingsPanel.GetComponent<RectTransform>().sizeDelta =
                isStartScreen ? new Vector2(620, 360) : new Vector2(620, 590);

            SetControlPosition("SETTINGS", isStartScreen
                ? new Vector2(0, 125) : new Vector2(0, 245));
            SetButtonLabel("Resume", isStartScreen ? "Back" : "Resume");
            SetControlPosition("Resume", isStartScreen
                ? new Vector2(0, -105) : new Vector2(0, -205));
            SetControlPosition("Master Volume Label", isStartScreen
                ? new Vector2(0, 95) : new Vector2(0, 174));
            SetControlPosition("Master Volume Slider", isStartScreen
                ? new Vector2(0, 45) : new Vector2(0, 124));
        }

        private void EnsureEventSystem()
        {
            if (root == null)
                return;

            if (EventSystem.current == null && ownedEventSystem == null)
            {
                ownedEventSystem = new GameObject(
                    "Settings UI EventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
            }
            else if (ownedEventSystem != null)
            {
                foreach (EventSystem system in
                         FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                {
                    if (system.gameObject == ownedEventSystem)
                        continue;

                    Destroy(ownedEventSystem);
                    ownedEventSystem = null;
                    break;
                }
            }
        }

        private void SetControlVisible(string name, bool visible)
        {
            Transform control = settingsPanel.transform.Find(name);
            if (control != null)
                control.gameObject.SetActive(visible);
        }

        private void SetControlPosition(string name, Vector2 position)
        {
            Transform control = settingsPanel.transform.Find(name);
            if (control != null)
                control.GetComponent<RectTransform>().anchoredPosition = position;
        }

        private void SetButtonLabel(string buttonName, string caption)
        {
            Transform button = settingsPanel.transform.Find(buttonName);
            if (button == null) return;
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = caption;
        }

        private void CreateVolumeSlider(Transform parent, string label,
            float initialValue, float y,
            UnityEngine.Events.UnityAction<float> onChanged)
        {
            CreateLabel(parent, label + " Label", 22,
                new Vector2(0, y + 30), new Vector2(500, 36));

            GameObject sliderObject = CreatePanel(parent, label + " Slider",
                new Vector2(500, 24), new Color(0.12f, 0.16f, 0.2f, 1f));

            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchoredPosition = new Vector2(0, y - 8);

            GameObject fillObject = CreatePanel(sliderObject.transform, "Fill",
                Vector2.zero, new Color(0.16f, 0.65f, 0.74f, 1f));

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handleObject = CreatePanel(sliderObject.transform, "Handle",
                new Vector2(24, 38), Color.white);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fillRect;
            slider.handleRect = handleObject.GetComponent<RectTransform>();
            slider.targetGraphic = handleObject.GetComponent<Image>();
            slider.value = Mathf.Clamp01(initialValue);
            slider.onValueChanged.AddListener(onChanged);
        }

        private static GameObject CreatePanel(Transform parent, string name,
            Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            Image image = go.GetComponent<Image>();
            image.color = color;
            return go;
        }

        private static TMP_Text CreateLabel(Transform parent, string text,
            int fontSize, Vector2 position, Vector2 dimensions)
        {
            GameObject go = new GameObject(text,
                typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.text = text;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TMPro.TextWrappingModes.Normal;
            label.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = dimensions;
            rect.anchoredPosition = position;
            return label;
        }

        private static RectTransform CreateButton(Transform parent, string text,
            Vector2 size, Vector2 position,
            UnityEngine.Events.UnityAction action, bool bottomRight = false)
        {
            GameObject go = CreatePanel(parent, text, size,
                new Color(0.1f, 0.25f, 0.32f, 1f));

            RectTransform rect = go.GetComponent<RectTransform>();

            if (bottomRight)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            }

            rect.anchoredPosition = position;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.AddListener(action);

            CreateLabel(go.transform, text, 21, Vector2.zero, size);
            return rect;
        }

        private static void Center(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (IsOpen)
            {
                RestoreWorldColliders();
                Time.timeScale = previousTimeScale;
            }

            IsOpen = false;
        }
    }
}
