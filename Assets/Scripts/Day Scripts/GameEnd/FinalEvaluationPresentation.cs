using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class FinalEvaluationPresentation : MonoBehaviour
{
    private static readonly Color Background = new Color32(10, 17, 27, 252);
    private static readonly Color Panel = new Color32(22, 32, 46, 255);
    private static readonly Color Stage = new Color32(14, 24, 37, 255);
    private static readonly Color Ink = new Color32(236, 242, 247, 255);
    private static readonly Color Accent = new Color32(129, 209, 229, 255);
    private static readonly Color Muted = new Color32(169, 194, 208, 255);

    private RectTransform call;
    private RectTransform scene;
    private RectTransform stage;
    private TMP_Text callTitle;
    private TMP_Text stageTitle;
    private TMP_Text stageDetail;
    private TMP_Text sceneLabel;
    private TMP_Text sceneTitle;
    private TMP_Text sceneDetail;
    private Image still;
    private RawImage footage;
    private Button join;
    private VideoPlayer player;
    private RenderTexture target;
    private AudioSource videoAudio;
    private bool preparing;
    private bool pausedForSettings;
    private float prepareTime;

    public bool Joined { get; private set; }

    internal static FinalEvaluationPresentation Create(Transform parent, FinalEvaluationMedia media,
        bool includeCall)
    {
        var root = new GameObject("Final Evaluation", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(parent, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Above desktop windows and below the existing dialogue (1400) and settings (5000).
        canvas.sortingOrder = 1200;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 0.5f;

        var view = root.AddComponent<FinalEvaluationPresentation>();
        view.Build((RectTransform)root.transform, media, includeCall);
        return view;
    }

    private void Build(RectTransform root, FinalEvaluationMedia media, bool includeCall)
    {
        Image background = Surface(root, "Backdrop", Background, 0, 0, 1, 1);
        background.raycastTarget = true;

        scene = Surface(root, "Epilogue", Panel, .05f, .28f, .95f, .94f).rectTransform;
        still = Surface(scene, "Scene still", Color.white, .03f, .18f, .97f, .86f);
        still.preserveAspect = true;
        scene.gameObject.SetActive(false);

        if (!includeCall)
        {
            Anchor(still.rectTransform, 0, 0, 1, 1);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        sceneLabel = Label(scene, "", 23, Accent, .04f, .88f, .96f, .98f, TextAlignmentOptions.MidlineLeft);
        sceneTitle = Label(scene, "", 44, Ink, .06f, .53f, .94f, .80f);
        sceneDetail = Label(scene, "", 30, Muted, .06f, .11f, .94f, .49f);

        BuildCall(root, media);
    }

    private void BuildCall(RectTransform root, FinalEvaluationMedia media)
    {
        call = Surface(root, "Zoom", Panel, .05f, .28f, .95f, .94f).rectTransform;
        callTitle = Label(call, "Zoom / Incoming call", 32, Accent,
            .03f, .87f, .97f, .98f, TextAlignmentOptions.MidlineLeft);

        stage = Surface(call, "Shared screen", Stage, .03f, .05f, .73f, .85f).rectTransform;
        stageTitle = Label(stage, "", 42, Ink, .07f, .60f, .93f, .82f);
        stageDetail = Label(stage, "", 29, Muted, .10f, .25f, .90f, .59f);
        MakeParticipant(call, "Manager", "M", media.managerPortrait, .46f, .85f);
        MakeParticipant(call, "Steve", "S", media.stevePortrait, .05f, .44f);

        Image joinImage = Surface(stage, "Join call", new Color32(36, 100, 125, 255), .32f, .12f, .68f, .29f);
        joinImage.raycastTarget = true;
        join = joinImage.gameObject.AddComponent<Button>();
        join.targetGraphic = joinImage;
        Label(joinImage.rectTransform, "Join call", 29, Ink, .05f, .08f, .95f, .92f);
        join.onClick.AddListener(() =>
        {
            Joined = true;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            join.gameObject.SetActive(false);
        });

        if (EventSystem.current != null) join.Select();

        stageTitle.text = "Battleship project review";
        stageDetail.text = "Manager and Steve";

        var imageObject = new GameObject("Playback", typeof(RectTransform), typeof(RawImage),
            typeof(AspectRatioFitter));
        var rect = (RectTransform)imageObject.transform;
        rect.SetParent(stage, false);
        Anchor(rect, 0, 0, 1, 1);
        var fitter = imageObject.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;
        footage = imageObject.GetComponent<RawImage>();
        footage.raycastTarget = false;
        footage.gameObject.SetActive(false);

        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.source = VideoSource.VideoClip;
        player.isLooping = false;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.aspectRatio = VideoAspectRatio.FitInside;
        player.audioOutputMode = media.playVideoAudio ? VideoAudioOutputMode.AudioSource : VideoAudioOutputMode.None;
        if (media.playVideoAudio)
        {
            videoAudio = gameObject.AddComponent<AudioSource>();
            videoAudio.playOnAwake = false;
            videoAudio.spatialBlend = 0f;
            player.controlledAudioTrackCount = 1;
            player.SetTargetAudioSource(0, videoAudio);
        }
        player.errorReceived += OnVideoError;
    }

    internal void ShowCall(string title, string detail)
    {
        StopVideo();
        scene.gameObject.SetActive(false);
        call.gameObject.SetActive(true);
        callTitle.text = "Zoom / Battleship project review";
        stageTitle.text = title;
        stageDetail.text = detail;
    }

    internal void ShowDemo(string title, string detail, VideoClip clip)
    {
        ShowCall(title, detail);
        callTitle.text = "Zoom / " + title;
        if (clip == null) return;

        if (target == null)
        {
            target = new RenderTexture(1280, 720, 0);
            target.name = "Final evaluation video";
            footage.texture = target;
        }
        player.targetTexture = target;
        player.clip = clip;
        prepareTime = 0f;
        preparing = true;
        player.Prepare();
    }

    internal void ShowScene(string title, string detail, Sprite sprite)
    {
        scene.gameObject.SetActive(true);
        still.sprite = sprite;
        still.gameObject.SetActive(sprite != null);
        if (call == null) return;

        StopVideo();
        call.gameObject.SetActive(false);
        sceneLabel.text = sprite == null ? "" : title;
        sceneTitle.text = sprite == null ? title : "";
        sceneTitle.gameObject.SetActive(sprite == null);
        sceneTitle.alignment = TextAlignmentOptions.Center;
        Anchor(sceneTitle.rectTransform, .06f, .53f, .94f, .80f);
        sceneDetail.text = detail;
        sceneDetail.alignment = TextAlignmentOptions.Center;
        Anchor(sceneDetail.rectTransform, .06f, .04f, .94f, sprite == null ? .49f : .17f);
    }

    internal void ShowEmail(string subject, string message)
    {
        ShowScene(subject, message, null);
        sceneLabel.text = "Inbox / Management";
        sceneTitle.alignment = TextAlignmentOptions.MidlineLeft;
        Anchor(sceneTitle.rectTransform, .07f, .68f, .93f, .83f);
        sceneDetail.alignment = TextAlignmentOptions.TopLeft;
        Anchor(sceneDetail.rectTransform, .07f, .09f, .93f, .64f);
    }

    private void Update()
    {
        if (player == null) return;

        if (videoAudio != null) videoAudio.volume = NullProgrammer.Audio.GameAudio.EffectsLevel;

        bool settingsOpen = NullProgrammer.Audio.AudioSettingsPanel.IsOpen;
        if (settingsOpen)
        {
            if (player.isPlaying)
            {
                pausedForSettings = true;
                player.Pause();
            }
            return;
        }

        if (pausedForSettings)
        {
            pausedForSettings = false;
            player.Play();
        }

        if (!preparing) return;
        if (player.isPrepared)
        {
            preparing = false;
            footage.gameObject.SetActive(true);
            player.Play();
            return;
        }

        prepareTime += Time.deltaTime;
        if (prepareTime >= 10f)
        {
            Debug.LogWarning("[Final Evaluation] Demo video preparation timed out. Using the title card.", this);
            StopVideo();
        }
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning("[Final Evaluation] Demo video could not play. " + message, this);
        StopVideo();
    }

    private void StopVideo()
    {
        preparing = false;
        pausedForSettings = false;
        player.Stop();
        player.clip = null;
        footage.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.errorReceived -= OnVideoError;
            player.Stop();
            player.targetTexture = null;
        }
        if (target != null)
        {
            target.Release();
            Destroy(target);
        }
    }

    private static void MakeParticipant(RectTransform parent, string name, string initial, Sprite portrait,
        float bottom, float top)
    {
        var tile = Surface(parent, name, Stage, .75f, bottom, .97f, top).rectTransform;
        if (portrait == null)
        {
            Label(tile, initial, 56, Accent, .08f, .24f, .92f, .95f);
        }
        else
        {
            Image avatar = Surface(tile, "Portrait", Color.white, .07f, .23f, .93f, .96f);
            avatar.sprite = portrait;
            avatar.preserveAspect = true;
        }
        Label(tile, name, 26, Ink, .05f, .01f, .95f, .23f);
    }

    private static Image Surface(RectTransform parent, string name, Color color,
        float left, float bottom, float right, float top)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        Anchor(rect, left, bottom, right, top);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Label(RectTransform parent, string text, float size, Color color,
        float left, float bottom, float right, float top, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        Anchor(rect, left, bottom, right, top);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = 16f;
        label.fontSizeMax = size;
        label.color = color;
        label.alignment = alignment;
        label.richText = false;
        label.raycastTarget = false;
        return label;
    }

    private static void Anchor(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
