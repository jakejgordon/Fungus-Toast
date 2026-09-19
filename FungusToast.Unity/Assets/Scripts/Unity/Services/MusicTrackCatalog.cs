using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FungusToast.Unity
{
    /// <summary>
    /// Resolves the player-facing name of a gameplay clip, e.g. "Track 5 - Break From the Mold".
    /// Titles come from the <see cref="MusicTrack"/> assets under Resources/Audio/MusicTracks.
    /// A clip with no asset falls back to a name derived from the <c>track_NN_slug</c> file
    /// convention so an unregistered track still reads as a title rather than an asset key.
    /// </summary>
    public static class MusicTrackCatalog
    {
        private const string ResourceFolder = "Audio/MusicTracks";

        private static readonly Regex TrackFilePattern = new(
            @"^track_(?<number>\d+)_(?<slug>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Title-case exceptions: kept lowercase unless they open the title.
        private static readonly HashSet<string> SmallWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "and", "as", "at", "but", "by", "for", "in", "nor", "of", "on", "or", "the", "to",
        };

        private static Dictionary<AudioClip, MusicTrack>? tracksByClip;

        public static string GetDisplayName(AudioClip? clip)
        {
            if (clip == null)
            {
                return string.Empty;
            }

            MusicTrack? track = FindTrack(clip);
            if (track != null && !string.IsNullOrWhiteSpace(track.Title))
            {
                return FormatDisplayName(track.TrackNumber, track.Title.Trim());
            }

            return FormatFallbackName(clip.name);
        }

        /// <summary>
        /// Builds a display name from a clip's asset name alone: <c>track_05_break_from_the_mold</c>
        /// becomes "Track 5 - Break From the Mold". Names outside the convention are title-cased as-is.
        /// </summary>
        public static string FormatFallbackName(string? clipName)
        {
            if (string.IsNullOrWhiteSpace(clipName))
            {
                return string.Empty;
            }

            Match match = TrackFilePattern.Match(clipName.Trim());
            if (match.Success && int.TryParse(match.Groups["number"].Value, out int number))
            {
                return FormatDisplayName(number, TitleCaseSlug(match.Groups["slug"].Value));
            }

            return TitleCaseSlug(clipName);
        }

        private static string FormatDisplayName(int trackNumber, string title)
        {
            return trackNumber > 0 ? $"Track {trackNumber} - {title}" : title;
        }

        private static string TitleCaseSlug(string slug)
        {
            string[] words = slug.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder builder = new();
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i].ToLowerInvariant();
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                if (i > 0 && SmallWords.Contains(word))
                {
                    builder.Append(word);
                }
                else
                {
                    builder.Append(char.ToUpperInvariant(word[0])).Append(word, 1, word.Length - 1);
                }
            }

            return builder.ToString();
        }

        private static MusicTrack? FindTrack(AudioClip clip)
        {
            tracksByClip ??= LoadTracks();
            return tracksByClip.TryGetValue(clip, out MusicTrack track) ? track : null;
        }

        private static Dictionary<AudioClip, MusicTrack> LoadTracks()
        {
            Dictionary<AudioClip, MusicTrack> byClip = new();
            foreach (MusicTrack track in Resources.LoadAll<MusicTrack>(ResourceFolder))
            {
                if (track == null || track.Clip == null)
                {
                    continue;
                }

                if (byClip.TryGetValue(track.Clip, out MusicTrack existing) && existing != track)
                {
                    Debug.LogWarning($"MusicTrackCatalog: clip '{track.Clip.name}' is described by both '{existing.name}' and '{track.name}'; keeping '{existing.name}'.");
                    continue;
                }

                byClip[track.Clip] = track;
            }

            return byClip;
        }
    }
}
