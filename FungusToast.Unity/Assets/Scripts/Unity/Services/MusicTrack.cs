using UnityEngine;

namespace FungusToast.Unity
{
    /// <summary>
    /// Player-facing metadata for one gameplay music clip. The clip's asset name
    /// (<c>track_05_break_from_the_mold</c>) is an internal key; this asset carries the
    /// title and track number the UI shows instead. Assets live under
    /// <c>Resources/Audio/MusicTracks</c> so <see cref="MusicTrackCatalog"/> can find them
    /// by clip without any scene wiring - the playlist itself still references raw clips.
    /// </summary>
    [CreateAssetMenu(menuName = "Configs/MusicTrack", fileName = "MusicTrack")]
    public class MusicTrack : ScriptableObject
    {
        [SerializeField] private AudioClip? clip = null;
        [SerializeField, Min(1)] private int trackNumber = 1;
        [SerializeField] private string title = string.Empty;

        public AudioClip? Clip => clip;
        public int TrackNumber => trackNumber;
        public string Title => title;
    }
}
