using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace NullProgrammer.Audio
{
    public sealed class AudioSettingsPanel : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private AudioDirector owner;
        private GameObject root, overlay, panel, credits, ownedEvents;
        private readonly List<Collider2D> blocked2D = new List<Collider2D>();
        private readonly List<Collider> blocked3D = new List<Collider>();

        private void Start()
        {
            owner = GetComponent<AudioDirector>();
            if (owner == null || owner.Catalog == null) return;
            root = new GameObject("Audio Controls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5000;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 1000); scaler.matchWidthOrHeight = .5f;
            var button = Button(root.transform, "Audio", Toggle, new Vector2(100, 38));
            button.anchorMin = button.anchorMax = button.pivot = Vector2.zero;
            button.anchoredPosition = new Vector2(16, 16); button.gameObject.SetActive(owner.Catalog.showAudioButton);
            overlay = Box(root.transform, "Settings Overlay", Vector2.zero);
            var overlayRect = (RectTransform)overlay.transform;
            overlayRect.anchorMin = Vector2.zero; overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0, 0, 0, .6f);
            panel = Box(overlay.transform, "Sound Settings", new Vector2(580, 510));
            var rt = (RectTransform)panel.transform; rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
            Label(panel.transform, "SOUND", 30, new Vector2(0, 205), new Vector2(510, 48));
            Volume("Master", owner.Master, 125, v => owner.SetVolumes(v, owner.Music, owner.Effects));
            Volume("Music", owner.Music, 30, v => owner.SetVolumes(owner.Master, v, owner.Effects));
            Volume("Effects", owner.Effects, -65, v => owner.SetVolumes(owner.Master, owner.Music, v));
            var c = Button(panel.transform, "Audio credits", () => credits.SetActive(!credits.activeSelf), new Vector2(240, 48));
            c.anchoredPosition = new Vector2(-125, -185);
            var close = Button(panel.transform, "Close", () => Open(false), new Vector2(240, 48)); close.anchoredPosition = new Vector2(125, -185);
            credits = Box(overlay.transform, "Audio Credits", new Vector2(850, 640));
            Label(credits.transform, "AUDIO CREDITS", 28, new Vector2(0, 265), new Vector2(770, 45));
            Label(credits.transform, owner.Catalog.credits, 21, new Vector2(0, 0), new Vector2(770, 460));
            var back = Button(credits.transform, "Back", () => credits.SetActive(false), new Vector2(200, 48)); back.anchoredPosition = new Vector2(0, -270);
            overlay.SetActive(false); credits.SetActive(false);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame) Toggle();
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Open(false);
            // Supply an EventSystem only in scenes that do not already have one.
            if (root != null && EventSystem.current == null && ownedEvents == null)
            {
                ownedEvents = new GameObject("Audio UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                ownedEvents.transform.SetParent(transform, false);
            }
            else if (ownedEvents != null)
            {
                foreach (var system in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                    if (system.gameObject != ownedEvents) { Destroy(ownedEvents); ownedEvents = null; break; }
            }
        }
        public void Toggle() { if (panel != null) Open(!IsOpen); }
        private void Open(bool value)
        {
            IsOpen = value; overlay.SetActive(value); credits.SetActive(false);
            if (value)
            {
                foreach (var c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
                    if (c.enabled) { blocked2D.Add(c); c.enabled = false; }
                foreach (var c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                    if (c.enabled) { blocked3D.Add(c); c.enabled = false; }
            }
            else
            {
                foreach (var c in blocked2D) if (c != null) c.enabled = true;
                foreach (var c in blocked3D) if (c != null) c.enabled = true;
                blocked2D.Clear(); blocked3D.Clear();
            }
            if (!value) owner.SaveSettings();
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { if (IsOpen) Open(false); }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (IsOpen && panel != null) Open(false);
            IsOpen = false;
        }
        private void Volume(string title, float value, float y, UnityEngine.Events.UnityAction<float> changed)
        {
            Label(panel.transform, title, 23, new Vector2(0, y + 28), new Vector2(500, 36));
            var go = Box(panel.transform, title + " Slider", new Vector2(500, 28));
            ((RectTransform)go.transform).anchoredPosition = new Vector2(0, y - 10);
            var fill = Box(go.transform, "Level", new Vector2(1, 28));
            var fillRect = (RectTransform)fill.transform;
            fill.GetComponent<Image>().color = new Color(.16f, .65f, .74f);
            var handle = Box(go.transform, "Handle", new Vector2(22, 36));
            var slider = go.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
            slider.fillRect = fillRect; slider.handleRect = (RectTransform)handle.transform;
            slider.targetGraphic = handle.GetComponent<Image>(); slider.value = value;
            slider.onValueChanged.AddListener(changed);
        }
        private static GameObject Box(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(.04f, .09f, .15f, .98f); return go;
        }
        private static TMP_Text Label(Transform parent, string text, int size, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>(); t.font = TMP_Settings.defaultFontAsset; t.fontSize = size;
            t.text = text; t.color = Color.white; t.raycastTarget = false; t.alignment = TextAlignmentOptions.Center;
            var r = (RectTransform)go.transform; r.sizeDelta = dimensions; r.anchoredPosition = position; return t;
        }
        private static RectTransform Button(Transform parent, string text, UnityEngine.Events.UnityAction action, Vector2 size)
        {
            var go = Box(parent, text, size); go.GetComponent<Image>().color = new Color(.1f, .25f, .32f);
            var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); button.onClick.AddListener(action);
            Label(go.transform, text, 22, Vector2.zero, size); return (RectTransform)go.transform;
        }
    }
}
