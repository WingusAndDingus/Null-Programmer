using System;
using UnityEngine;
using UnityEngine.Video;

[Serializable]
public sealed class FinalEvaluationMedia
{
    public Sprite managerPortrait;
    public Sprite stevePortrait;
    [Tooltip("Steve's independent build, shown when he takes over the live review.")]
    public VideoClip steveBackupVideo;
    [Tooltip("Enable if the presentation clips have audio you want the player to hear.")]
    public bool playVideoAudio;

    public RouteMedia obsoleteAndDeprecated = new RouteMedia();
    public RouteMedia obsoleteSoftRelaunch = new RouteMedia();
    public RouteMedia imposterProtocol = new RouteMedia();
    public RouteMedia perfectionistTrap = new RouteMedia();
    public RouteMedia ultimateMentor = new RouteMedia();
    public RouteMedia architectRedemption = new RouteMedia();
    public MemoryMedia segmentationFaultOfSoul = new MemoryMedia();

    [Serializable]
    public sealed class RouteMedia
    {
        public VideoClip projectVideo;
        [Tooltip("Optional stills, one per epilogue scene, in script order. Empty entries use the scene's title card.")]
        public Sprite[] epilogueStills;
    }

    [Serializable]
    public sealed class MemoryMedia
    {
        [Tooltip("Secret-ending stills in script order. Empty entries show a plain backdrop.")]
        public Sprite[] epilogueStills;
    }

    public RouteMedia ForEnding(GameEndingType ending)
    {
        switch (ending)
        {
            case GameEndingType.ObsoleteAndDeprecated: return obsoleteAndDeprecated;
            case GameEndingType.ObsoleteSoftRelaunch: return obsoleteSoftRelaunch;
            case GameEndingType.ImposterProtocol: return imposterProtocol;
            case GameEndingType.PerfectionistTrap: return perfectionistTrap;
            case GameEndingType.UltimateMentor: return ultimateMentor;
            case GameEndingType.ArchitectRedemption: return architectRedemption;
            default: throw new ArgumentOutOfRangeException(nameof(ending), ending, null);
        }
    }
}
