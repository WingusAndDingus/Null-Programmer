using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;

namespace NullProgrammer.Audio
{
    public sealed class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Current { get; private set; }
        public AudioCatalog Catalog { get; private set; }

        public float Master { get; private set; }
        public float Music { get; private set; }
        public float Effects { get; private set; }
        public bool CallActive { get; private set; }

        private AudioSource[] music, effects;

        private readonly float[] gain = new float[2];
        private readonly float[] from = new float[2];

        private readonly Dictionary<SfxCue, float> played =
            new Dictionary<SfxCue, float>();

        private readonly HashSet<Component> activities =
            new HashSet<Component>();

        private readonly HashSet<int> emailDays = new HashSet<int>();
        private readonly HashSet<int> announcedDays = new HashSet<int>();

        private GameManager manager;
        private MusicCue manual = MusicCue.Auto;
        private AudioClip desired;

        private int target = -1;
        private int voice;
        private int lastDay = -1;
        private int lastSuspicion;
        private int lastSfxFrame = -1;

        private float fade = 1f;
        private float nextScan;
        private float duck = 1f;
        private float previousListenerVolume;

        private bool videoPlaying;
        private bool warnedMissing;
        private string sceneName;

        public static void ClearStatic()
        {
            Current = null;
        }

        public static AudioDirector Ensure()
        {
            if (Current != null)
                return Current;

            var existing = FindAnyObjectByType<AudioDirector>();

            if (existing != null)
                return Current = existing;

            var catalog = Resources.Load<AudioCatalog>(
                "NullProgrammerAudio/AudioCatalog");

            if (catalog == null)
                return null;

            return new GameObject("Null Programmer Audio")
                .AddComponent<AudioDirector>();
        }

        private void Awake()
        {
            if (Current != null && Current != this)
            {
                Destroy(gameObject);
                return;
            }

            Current = this;
            DontDestroyOnLoad(gameObject);

            Catalog = Resources.Load<AudioCatalog>(
                "NullProgrammerAudio/AudioCatalog");

            if (Catalog == null)
            {
                Debug.LogWarning(
                    "Audio catalog missing; audio update is inactive.");

                enabled = false;
                return;
            }

            Master = LoadVolume("Master", .8f);

            // Unity starts with the Inspector default each Play session.
            // Builds respect the player's saved music volume.
            Music = Application.isEditor
                ? Mathf.Clamp01(Catalog.defaultMusicVolume)
                : LoadVolume("Music", Catalog.defaultMusicVolume);

            Effects = LoadVolume("SFX", .65f);

            previousListenerVolume = AudioListener.volume;
            AudioListener.volume = Master;

            music = new[]
            {
                Source("Music A", true),
                Source("Music B", true)
            };

            effects = new[]
            {
                Source("Effects A", false),
                Source("Effects B", false),
                Source("Effects C", false),
                Source("Effects D", false)
            };

            SceneManager.sceneLoaded += SceneLoaded;
            GameplayEvents.OnZoomCallStarted += CallStarted;
            GameplayEvents.OnZoomCallEnded += CallEnded;

            sceneName = SceneManager.GetActiveScene().name;

            gameObject.AddComponent<AudioSettingsPanel>();

            ResolveMusic();
        }

        private AudioSource Source(string label, bool loop)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform, false);

            var source = child.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0;
            source.volume = 0;
            source.priority = loop ? 160 : 80;

            return source;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
            {
                sceneName = scene.name;

                if (AudioPolicy.IsNight(sceneName) ||
                    sceneName == "StartScreen")
                {
                    manual = MusicCue.Auto;
                }
            }

            nextScan = 0;
            ResolveMusic();
        }

        private void Update()
        {
            if (Catalog == null || music == null)
                return;

            if (manager != GameManager.Instance)
            {
                if (manager != null)
                    manager.SuspicionChanged -= SuspicionChanged;

                manager = GameManager.Instance;

                if (manager != null)
                {
                    lastSuspicion = manager.SteveSuspicion;
                    manager.SuspicionChanged += SuspicionChanged;
                }
            }

            if (manager != null && manager.CurrentDay != lastDay)
            {
                lastDay = manager.CurrentDay;
                manual = MusicCue.Auto;
                CallActive = false;

                ResolveMusic();
            }

            if (manager != null &&
                AudioPolicy.IsDesktop(sceneName) &&
                announcedDays.Add(manager.CurrentDay))
            {
                Play(SfxCue.DayStart);
            }

            activities.RemoveWhere(c => c == null);

            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + .5f;

                foreach (var button in
                         FindObjectsByType<Button>(
                             FindObjectsSortMode.None))
                {
                    if (button.GetComponent<AudioButtonFeedback>() == null)
                        button.gameObject.AddComponent<AudioButtonFeedback>();
                }

                foreach (var trigger in
                         FindObjectsByType<EventTrigger>(
                             FindObjectsSortMode.None))
                {
                    if (trigger.GetComponent<Button>() == null &&
                        trigger.GetComponent<AudioIconFeedback>() == null &&
                        trigger.triggers.Exists(
                            e => e.eventID == EventTriggerType.PointerClick))
                    {
                        trigger.gameObject.AddComponent<AudioIconFeedback>();
                    }
                }

                videoPlaying = false;

                foreach (var video in
                         FindObjectsByType<VideoPlayer>(
                             FindObjectsSortMode.None))
                {
                    if (video.isPlaying &&
                        video.audioOutputMode != VideoAudioOutputMode.None &&
                        video.audioTrackCount > 0)
                    {
                        videoPlaying = true;
                        break;
                    }
                }

                ResolveMusic();
            }

            // Day 4's existing rhythm track owns its playback and timing.
            bool battle =
                SceneManager.GetSceneByName("RefactoFight").isLoaded;

            float attenuation = battle || videoPlaying
                ? 0
                : CallActive ? .35f : 1f;

            duck = Mathf.MoveTowards(
                duck,
                attenuation,
                Time.unscaledDeltaTime * 3f);

            fade = Mathf.Min(
                1f,
                fade + Time.unscaledDeltaTime /
                Mathf.Max(.1f, Catalog.crossfadeSeconds));

            for (int i = 0; i < 2; i++)
            {
                gain[i] = Mathf.Lerp(
                    from[i],
                    i == target ? 1f : 0f,
                    fade);

                music[i].volume = Music * gain[i] * duck;

                if (fade >= 1f &&
                    i != target &&
                    music[i].isPlaying)
                {
                    music[i].Stop();
                }
            }

            foreach (var source in effects)
                source.volume = Effects;
        }

        public void SetVolumes(
            float master,
            float musicLevel,
            float sfx)
        {
            Master = Mathf.Clamp01(master);
            Music = Mathf.Clamp01(musicLevel);
            Effects = Mathf.Clamp01(sfx);

            AudioListener.volume = Master;

            PlayerPrefs.SetFloat("NP.Audio.Master", Master);
            PlayerPrefs.SetFloat("NP.Audio.Music", Music);
            PlayerPrefs.SetFloat("NP.Audio.SFX", Effects);
        }

        private static float LoadVolume(string key, float fallback)
        {
            return Mathf.Clamp01(
                PlayerPrefs.GetFloat("NP.Audio." + key, fallback));
        }

        public void SaveSettings()
        {
            PlayerPrefs.Save();
        }

        public void Activity(Component owner, bool active)
        {
            if (owner == null)
                return;

            if (active)
                activities.Add(owner);
            else
                activities.Remove(owner);

            ResolveMusic();
        }

        public void SetMusic(MusicCue cue)
        {
            manual = cue;
            ResolveMusic();
        }

        private void ResolveMusic()
        {
            if (Catalog == null || music == null)
                return;

            var cue = AudioPolicy.Resolve(
                sceneName,
                activities.Count > 0,
                manual);

            var clip = cue == MusicCue.Activity
                ? Catalog.activity
                : cue == MusicCue.Night
                    ? Catalog.night
                    : cue == MusicCue.Silence
                        ? null
                        : Catalog.desktop;

            if (desired == clip)
                return;

            desired = clip;

            for (int i = 0; i < 2; i++)
                from[i] = gain[i];

            target = -1;

            if (clip != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (music[i].clip == clip && music[i].isPlaying)
                        target = i;
                }

                if (target < 0)
                {
                    target = gain[0] <= gain[1] ? 0 : 1;

                    music[target].Stop();
                    music[target].clip = clip;
                    music[target].volume = 0;
                    music[target].Play();

                    gain[target] = from[target] = 0;
                }
            }

            fade = 0;
        }

        public void Play(SfxCue cue)
        {
            if (Catalog == null ||
                effects == null ||
                cue == SfxCue.None)
            {
                return;
            }

            if (cue == SfxCue.Click &&
                lastSfxFrame == Time.frameCount)
            {
                return;
            }

            var sound = Catalog.Find(cue);

            if (sound == null || sound.clip == null)
            {
                if (!warnedMissing)
                {
                    Debug.LogWarning(
                        "Audio catalog has a missing sound; use " +
                        "Tools > Null Programmer > Audio > Validate Audio.");

                    warnedMissing = true;
                }

                return;
            }

            if (played.TryGetValue(cue, out float when) &&
                Time.unscaledTime - when < sound.cooldown)
            {
                return;
            }

            played[cue] = Time.unscaledTime;
            lastSfxFrame = Time.frameCount;

            var source = effects[voice++ % effects.Length];

            source.volume = Effects;
            source.pitch = 1;
            source.PlayOneShot(sound.clip, sound.gain);
        }

        public void EmailAvailable(int day)
        {
            if (emailDays.Add(day))
                Play(SfxCue.Email);
        }

        private void CallStarted()
        {
            CallActive = true;
            Play(SfxCue.CallAccept);
        }

        private void CallEnded()
        {
            CallActive = false;
        }

        private void SuspicionChanged(int current)
        {
            if (current - lastSuspicion >= 10)
                Play(SfxCue.Suspicion);

            lastSuspicion = current;
        }

        private void OnDestroy()
        {
            if (Current != this)
                return;

            SceneManager.sceneLoaded -= SceneLoaded;
            GameplayEvents.OnZoomCallStarted -= CallStarted;
            GameplayEvents.OnZoomCallEnded -= CallEnded;

            if (manager != null)
                manager.SuspicionChanged -= SuspicionChanged;

            AudioListener.volume = previousListenerVolume;
            Current = null;
        }
    }
}