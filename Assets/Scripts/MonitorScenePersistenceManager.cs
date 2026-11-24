using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the entire center-monitor DesktopUI scene loaded while the player
/// visits the Left Monitor or Right Monitor scenes.
///
/// This deliberately preserves the existing Unity hierarchy and every
/// serialized reference inside DesktopUI. Nothing is reparented and no
/// application window is moved to DontDestroyOnLoad.
///
/// While DesktopUI is not the active monitor, its presentation/input is
/// suppressed WITHOUT deactivating its GameObjects. That is important because
/// MaximizeWindow restores itself from OnDisable, and because window buttons may
/// reference controller objects elsewhere in the DesktopUI hierarchy.
///
/// No Inspector setup is required. SceneSwitcher routes monitor loads here.
/// </summary>
[DefaultExecutionOrder(-950)]
public sealed class MonitorScenePersistenceManager : MonoBehaviour
{
    private static MonitorScenePersistenceManager instance;

    private Scene centerScene;
    private int centerSceneDay = -1;
    private bool centerSuppressed;

    private bool monitorLoadPending;
    private string pendingMonitorScene;
    private string monitorTargetAfterCenterReload;

    private readonly Dictionary<Renderer, bool> rendererForceOff =
        new Dictionary<Renderer, bool>();

    private readonly Dictionary<Canvas, bool> canvasEnabled =
        new Dictionary<Canvas, bool>();

    private readonly Dictionary<BaseRaycaster, bool> raycasterEnabled =
        new Dictionary<BaseRaycaster, bool>();

    private readonly Dictionary<Collider, bool> colliderEnabled =
        new Dictionary<Collider, bool>();

    private readonly Dictionary<Collider2D, bool> collider2DEnabled =
        new Dictionary<Collider2D, bool>();

    private readonly Dictionary<Camera, bool> cameraEnabled =
        new Dictionary<Camera, bool>();

    private readonly Dictionary<AudioListener, bool> audioListenerEnabled =
        new Dictionary<AudioListener, bool>();

    // =========================================================
    // Bootstrap / lifetime
    // =========================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static MonitorScenePersistenceManager EnsureInstance()
    {
        if (instance != null)
            return instance;

        GameObject host = new GameObject("[Monitor Scene Persistence Manager]");
        instance = host.AddComponent<MonitorScenePersistenceManager>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    // =========================================================
    // Public entry point used by SceneSwitcher
    // =========================================================

    public static void LoadScene(string sceneName)
    {
        EnsureInstance().LoadSceneInternal(sceneName);
    }

    private void LoadSceneInternal(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        if (!SceneSwitcher.IsMonitorScene(sceneName))
        {
            // Leaving the monitor group: use ordinary single-scene loading.
            // DesktopUI is allowed to unload normally so the next day/section
            // receives a completely fresh authored scene.
            monitorLoadPending = false;
            pendingMonitorScene = null;
            monitorTargetAfterCenterReload = null;

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            return;
        }

        // If the retained DesktopUI belongs to another day, reload it before
        // honoring the requested monitor. This keeps day-authored content fresh.
        if (IsCenterSceneLoaded() && CenterDayHasChanged())
        {
            monitorTargetAfterCenterReload = sceneName;
            monitorLoadPending = false;
            pendingMonitorScene = null;

            // A single load intentionally destroys the stale retained center
            // scene and any currently loaded side-monitor scene.
            SceneManager.LoadScene(
                SceneSwitcher.CenterMonitorScene,
                LoadSceneMode.Single);
            return;
        }

        if (sceneName == SceneSwitcher.CenterMonitorScene)
        {
            SwitchToCenterMonitor();
            return;
        }

        SwitchToSideMonitor(sceneName);
    }

    // =========================================================
    // Monitor switching
    // =========================================================

    private void SwitchToSideMonitor(string targetSceneName)
    {
        Scene current = SceneManager.GetActiveScene();
        if (current.IsValid() && current.name == targetSceneName)
            return;

        Scene loadedTarget = FindLoadedScene(targetSceneName);
        if (loadedTarget.IsValid() && loadedTarget.isLoaded)
        {
            FinalizeSideMonitorSwitch(loadedTarget);
            return;
        }

        if (monitorLoadPending)
        {
            Debug.LogWarning(
                $"[MonitorScenePersistence] Ignoring monitor request '{targetSceneName}' " +
                $"because '{pendingMonitorScene}' is still loading.",
                this);
            return;
        }

        monitorLoadPending = true;
        pendingMonitorScene = targetSceneName;

        // Additive loading is the key change: DesktopUI stays alive with every
        // existing hierarchy/reference intact while the side monitor is loaded.
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Additive);
    }

    private void SwitchToCenterMonitor()
    {
        if (!IsCenterSceneLoaded())
        {
            if (monitorLoadPending)
                return;

            monitorLoadPending = true;
            pendingMonitorScene = SceneSwitcher.CenterMonitorScene;
            SceneManager.LoadScene(
                SceneSwitcher.CenterMonitorScene,
                LoadSceneMode.Additive);
            return;
        }

        Scene previous = SceneManager.GetActiveScene();

        // The previous side-monitor scene is disposable. Hide its roots before
        // the center is restored so there can be no one-frame overlap.
        if (previous.IsValid() &&
            previous.name != SceneSwitcher.CenterMonitorScene &&
            SceneSwitcher.IsMonitorScene(previous.name))
        {
            SetSceneRootsActive(previous, false);
        }

        RestoreCenterScene();
        SceneManager.SetActiveScene(centerScene);

        UnloadSideMonitorScenesExcept(null);
    }

    private void FinalizeSideMonitorSwitch(Scene targetScene)
    {
        Scene previous = SceneManager.GetActiveScene();

        // Suppress DesktopUI before the new frame is rendered. The sceneLoaded
        // callback occurs as the additive scene is activated, so the player does
        // not see the center content disappear early and does not see it bleed
        // into the side monitor afterward.
        SuppressCenterScene();

        if (targetScene.IsValid() && targetScene.isLoaded)
            SceneManager.SetActiveScene(targetScene);

        // If moving from Left <-> Right, the old side-monitor scene is no longer
        // needed. Hide it immediately, then unload it asynchronously.
        if (previous.IsValid() &&
            previous.name != SceneSwitcher.CenterMonitorScene &&
            previous.name != targetScene.name &&
            SceneSwitcher.IsMonitorScene(previous.name))
        {
            SetSceneRootsActive(previous, false);
            SceneManager.UnloadSceneAsync(previous);
        }

        UnloadSideMonitorScenesExcept(targetScene.name);
    }

    // =========================================================
    // Scene callbacks
    // =========================================================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneSwitcher.CenterMonitorScene)
        {
            centerScene = scene;
            centerSceneDay = GetCurrentDay();
            centerSuppressed = false;
            ClearSuppressionSnapshot();

            Debug.Log(
                $"[MonitorScenePersistence] Registered DesktopUI for day {centerSceneDay}.",
                this);

            if (monitorLoadPending &&
                string.Equals(
                    pendingMonitorScene,
                    SceneSwitcher.CenterMonitorScene,
                    StringComparison.Ordinal))
            {
                monitorLoadPending = false;
                pendingMonitorScene = null;
                SwitchToCenterMonitor();
            }

            if (!string.IsNullOrWhiteSpace(monitorTargetAfterCenterReload))
            {
                string target = monitorTargetAfterCenterReload;
                monitorTargetAfterCenterReload = null;

                if (target != SceneSwitcher.CenterMonitorScene)
                    SwitchToSideMonitor(target);
            }

            return;
        }

        if (monitorLoadPending &&
            string.Equals(scene.name, pendingMonitorScene, StringComparison.Ordinal))
        {
            monitorLoadPending = false;
            pendingMonitorScene = null;
            FinalizeSideMonitorSwitch(scene);
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name != SceneSwitcher.CenterMonitorScene)
            return;

        centerScene = default;
        centerSceneDay = -1;
        centerSuppressed = false;
        ClearSuppressionSnapshot();
    }

    private void LateUpdate()
    {
        // DesktopUI scripts remain alive while the player is on another monitor.
        // If one of them instantiates/enables new visual or input components while
        // hidden, suppress those too before rendering the frame.
        if (centerSuppressed && IsCenterSceneLoaded())
            CaptureAndSuppressCenterComponents();
    }

    // =========================================================
    // Center-scene visibility
    // =========================================================

    private void SuppressCenterScene()
    {
        if (!IsCenterSceneLoaded())
            return;

        centerSuppressed = true;
        CaptureAndSuppressCenterComponents();

        Debug.Log(
            "[MonitorScenePersistence] DesktopUI remains loaded but is visually/input hidden.",
            this);
    }

    private void CaptureAndSuppressCenterComponents()
    {
        if (!IsCenterSceneLoaded())
            return;

        GameObject[] roots = centerScene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            GameObject root = roots[r];
            if (root == null)
                continue;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer component = renderers[i];
                if (component == null)
                    continue;

                if (!rendererForceOff.ContainsKey(component))
                    rendererForceOff.Add(component, component.forceRenderingOff);

                component.forceRenderingOff = true;
            }

            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas component = canvases[i];
                if (component == null)
                    continue;

                if (!canvasEnabled.ContainsKey(component))
                    canvasEnabled.Add(component, component.enabled);

                component.enabled = false;
            }

            BaseRaycaster[] raycasters =
                root.GetComponentsInChildren<BaseRaycaster>(true);
            for (int i = 0; i < raycasters.Length; i++)
            {
                BaseRaycaster component = raycasters[i];
                if (component == null)
                    continue;

                if (!raycasterEnabled.ContainsKey(component))
                    raycasterEnabled.Add(component, component.enabled);

                component.enabled = false;
            }

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider component = colliders[i];
                if (component == null)
                    continue;

                if (!colliderEnabled.ContainsKey(component))
                    colliderEnabled.Add(component, component.enabled);

                component.enabled = false;
            }

            Collider2D[] colliders2D =
                root.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders2D.Length; i++)
            {
                Collider2D component = colliders2D[i];
                if (component == null)
                    continue;

                if (!collider2DEnabled.ContainsKey(component))
                    collider2DEnabled.Add(component, component.enabled);

                component.enabled = false;
            }

            Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera component = cameras[i];
                if (component == null)
                    continue;

                if (!cameraEnabled.ContainsKey(component))
                    cameraEnabled.Add(component, component.enabled);

                component.enabled = false;
            }

            AudioListener[] listeners =
                root.GetComponentsInChildren<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener component = listeners[i];
                if (component == null)
                    continue;

                if (!audioListenerEnabled.ContainsKey(component))
                    audioListenerEnabled.Add(component, component.enabled);

                component.enabled = false;
            }
        }
    }

    private void RestoreCenterScene()
    {
        if (!centerSuppressed)
            return;

        foreach (KeyValuePair<Renderer, bool> pair in rendererForceOff)
            if (pair.Key != null)
                pair.Key.forceRenderingOff = pair.Value;

        foreach (KeyValuePair<Canvas, bool> pair in canvasEnabled)
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;

        foreach (KeyValuePair<BaseRaycaster, bool> pair in raycasterEnabled)
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;

        foreach (KeyValuePair<Collider, bool> pair in colliderEnabled)
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;

        foreach (KeyValuePair<Collider2D, bool> pair in collider2DEnabled)
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;

        foreach (KeyValuePair<Camera, bool> pair in cameraEnabled)
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;

        foreach (KeyValuePair<AudioListener, bool> pair in audioListenerEnabled)
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;

        ClearSuppressionSnapshot();
        centerSuppressed = false;

        Debug.Log(
            "[MonitorScenePersistence] DesktopUI revealed; its original live hierarchy was never unloaded.",
            this);
    }

    private void ClearSuppressionSnapshot()
    {
        rendererForceOff.Clear();
        canvasEnabled.Clear();
        raycasterEnabled.Clear();
        colliderEnabled.Clear();
        collider2DEnabled.Clear();
        cameraEnabled.Clear();
        audioListenerEnabled.Clear();
    }

    // =========================================================
    // Scene helpers
    // =========================================================

    private bool IsCenterSceneLoaded()
    {
        return centerScene.IsValid() && centerScene.isLoaded;
    }

    private bool CenterDayHasChanged()
    {
        int currentDay = GetCurrentDay();
        return centerSceneDay >= 0 &&
               currentDay >= 0 &&
               currentDay != centerSceneDay;
    }

    private static int GetCurrentDay()
    {
        return GameManager.Instance != null
            ? GameManager.Instance.CurrentDay
            : -1;
    }

    private static Scene FindLoadedScene(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.IsValid() &&
                scene.isLoaded &&
                string.Equals(scene.name, sceneName, StringComparison.Ordinal))
            {
                return scene;
            }
        }

        return default;
    }

    private static void SetSceneRootsActive(Scene scene, bool active)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null)
                roots[i].SetActive(active);
        }
    }

    private void UnloadSideMonitorScenesExcept(string sceneNameToKeep)
    {
        var unload = new List<Scene>();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            if (!SceneSwitcher.IsMonitorScene(scene.name) ||
                scene.name == SceneSwitcher.CenterMonitorScene)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(sceneNameToKeep) &&
                string.Equals(scene.name, sceneNameToKeep, StringComparison.Ordinal))
            {
                continue;
            }

            unload.Add(scene);
        }

        for (int i = 0; i < unload.Count; i++)
        {
            SetSceneRootsActive(unload[i], false);
            SceneManager.UnloadSceneAsync(unload[i]);
        }
    }
}
