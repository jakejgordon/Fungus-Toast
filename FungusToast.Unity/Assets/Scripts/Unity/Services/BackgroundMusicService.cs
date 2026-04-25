using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace FungusToast.Unity
{
    public static class SoundEffectsSettings
    {
        private const string EnabledKey = "Audio.Sfx.Enabled";
        private const string VolumeKey = "Audio.Sfx.Volume";

        private static readonly float[] VolumeSteps = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        private static bool loaded;
        private static bool enabled = true;
        private static float volume = 1f;

        public static bool Enabled
        {
            get
            {
                EnsureLoaded();
                return enabled;
            }
        }

        public static float Volume
        {
            get
            {
                EnsureLoaded();
                return volume;
            }
        }

        public static void ToggleEnabled()
        {
            SetEnabled(!Enabled);
        }

        public static void SetEnabled(bool value)
        {
            EnsureLoaded();
            if (enabled == value)
            {
                return;
            }

            enabled = value;
            PlayerPrefs.SetInt(EnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void CycleVolumeForward()
        {
            EnsureLoaded();

            int currentIndex = 0;
            for (int index = 0; index < VolumeSteps.Length; index++)
            {
                if (Mathf.Approximately(VolumeSteps[index], volume))
                {
                    currentIndex = index;
                    break;
                }
            }

            int nextIndex = (currentIndex + 1) % VolumeSteps.Length;
            SetVolume(VolumeSteps[nextIndex]);
        }

        public static void SetVolume(float value)
        {
            EnsureLoaded();
            float clampedValue = Mathf.Clamp01(value);
            if (Mathf.Approximately(volume, clampedValue))
            {
                return;
            }

            volume = clampedValue;
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.Save();
        }

        public static float GetEffectiveVolume(float baseVolume)
        {
            EnsureLoaded();
            if (!enabled)
            {
                return 0f;
            }

            return Mathf.Clamp01(baseVolume) * volume;
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            enabled = PlayerPrefs.GetInt(EnabledKey, 1) != 0;
            volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
            loaded = true;
        }
    }

    public static class MusicSettings
    {
        private const string VolumeKey = "Audio.Music.Volume";

        private static readonly float[] VolumeSteps = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        private static bool loaded;
        private static float volume = 0.75f;

        public static float Volume
        {
            get
            {
                EnsureLoaded();
                return volume;
            }
        }

        public static void CycleVolumeForward()
        {
            EnsureLoaded();

            int currentIndex = 0;
            for (int index = 0; index < VolumeSteps.Length; index++)
            {
                if (Mathf.Approximately(VolumeSteps[index], volume))
                {
                    currentIndex = index;
                    break;
                }
            }

            int nextIndex = (currentIndex + 1) % VolumeSteps.Length;
            SetVolume(VolumeSteps[nextIndex]);
        }

        public static void SetVolume(float value)
        {
            EnsureLoaded();
            float clampedValue = Mathf.Clamp01(value);
            if (Mathf.Approximately(volume, clampedValue))
            {
                return;
            }

            volume = clampedValue;
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.Save();
        }

        public static float GetEffectiveVolume(float baseVolume)
        {
            EnsureLoaded();
            return Mathf.Clamp01(baseVolume) * volume;
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.75f));
            loaded = true;
        }
    }

    public class SoundEffectService
    {
        private readonly GameObject audioHost;

        private AudioSource soundEffectAudioSource;
        private AudioSource loopingAudioSource;
        private SoundEffectCoroutineHost coroutineHost;
        private Coroutine stopLoopCoroutine;

        public SoundEffectService(GameObject audioHost)
        {
            this.audioHost = audioHost;
        }

        public void PlayOneShot(AudioClip clip, float baseVolume)
        {
            if (clip == null)
            {
                return;
            }

            EnsureAudioSource();
            float effectiveVolume = SoundEffectsSettings.GetEffectiveVolume(baseVolume);
            if (effectiveVolume <= 0f)
            {
                return;
            }

            soundEffectAudioSource.PlayOneShot(clip, effectiveVolume);
        }

        public void PlayLoop(AudioClip clip, float baseVolume)
        {
            if (clip == null)
            {
                return;
            }

            EnsureLoopingAudioSource();
            StopPendingLoopFade();

            float effectiveVolume = SoundEffectsSettings.GetEffectiveVolume(baseVolume);
            if (effectiveVolume <= 0f)
            {
                loopingAudioSource.Stop();
                return;
            }

            loopingAudioSource.clip = clip;
            loopingAudioSource.volume = effectiveVolume;
            loopingAudioSource.loop = true;

            if (!loopingAudioSource.isPlaying)
            {
                loopingAudioSource.Play();
            }
        }

        public void StopLoop(float fadeOutSeconds = 0f)
        {
            if (loopingAudioSource == null)
            {
                return;
            }

            StopPendingLoopFade();

            if (fadeOutSeconds <= 0f || !loopingAudioSource.isPlaying)
            {
                loopingAudioSource.Stop();
                loopingAudioSource.clip = null;
                loopingAudioSource.volume = 0f;
                return;
            }

            EnsureCoroutineHost();
            stopLoopCoroutine = coroutineHost.StartCoroutine(FadeOutAndStopLoop(fadeOutSeconds));
        }

        private IEnumerator FadeOutAndStopLoop(float fadeOutSeconds)
        {
            float startingVolume = loopingAudioSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeOutSeconds && loopingAudioSource != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutSeconds);
                loopingAudioSource.volume = Mathf.Lerp(startingVolume, 0f, t);
                yield return null;
            }

            if (loopingAudioSource != null)
            {
                loopingAudioSource.Stop();
                loopingAudioSource.clip = null;
                loopingAudioSource.volume = 0f;
            }

            stopLoopCoroutine = null;
        }

        private void StopPendingLoopFade()
        {
            if (stopLoopCoroutine == null || coroutineHost == null)
            {
                return;
            }

            coroutineHost.StopCoroutine(stopLoopCoroutine);
            stopLoopCoroutine = null;
        }

        private void EnsureAudioSource()
        {
            if (soundEffectAudioSource != null)
            {
                return;
            }

            soundEffectAudioSource = audioHost.GetComponent<AudioSource>();
            if (soundEffectAudioSource == null)
            {
                soundEffectAudioSource = audioHost.AddComponent<AudioSource>();
            }

            soundEffectAudioSource.playOnAwake = false;
            soundEffectAudioSource.loop = false;
            soundEffectAudioSource.spatialBlend = 0f;
        }

        private void EnsureLoopingAudioSource()
        {
            if (loopingAudioSource != null)
            {
                return;
            }

            GameObject loopSourceObject = new("LoopingSoundEffectAudioSource");
            loopSourceObject.transform.SetParent(audioHost.transform, false);
            loopingAudioSource = loopSourceObject.AddComponent<AudioSource>();
            loopingAudioSource.playOnAwake = false;
            loopingAudioSource.loop = true;
            loopingAudioSource.spatialBlend = 0f;
        }

        private void EnsureCoroutineHost()
        {
            if (coroutineHost != null)
            {
                return;
            }

            coroutineHost = audioHost.GetComponent<SoundEffectCoroutineHost>();
            if (coroutineHost == null)
            {
                coroutineHost = audioHost.AddComponent<SoundEffectCoroutineHost>();
            }
        }

        private sealed class SoundEffectCoroutineHost : MonoBehaviour
        {
        }
    }

    public class BackgroundMusicService
    {
        private const string AudioSourceObjectName = "BackgroundMusicAudioSource";
        private const float EndOfTrackGraceSeconds = 0.1f;

        private readonly MonoBehaviour coroutineHost;
        private readonly Transform audioRoot;
        private readonly Func<bool> getIsPaused;
        private readonly List<AudioClip> shuffledGameplayClips = new();

        private AudioSource musicAudioSource;
        private AudioClip[] gameplayClips = Array.Empty<AudioClip>();
        private AudioClip lastPlayedClip;
        private AudioMixerGroup mixerGroup;
        private Coroutine playbackRoutine;
        private int nextGameplayClipIndex;
        private bool skipToNextRequested;

        private float baseVolume = 0.28f;
        private float initialDelaySeconds = 1.5f;
        private float replayDelaySeconds = 4f;
        private float fadeInSeconds = 1f;
        private bool shouldPlay;
        private bool audioSourcePaused;
        // Title track fields (main menu / sub-menus)
        private AudioClip titleClip;
        private AudioMixerGroup titleMixerGroup;
        private AudioSource titleAudioSource;
        private float titleBaseVolume = 0.28f;
        private Coroutine titlePlaybackRoutine;

        public BackgroundMusicService(MonoBehaviour coroutineHost, Transform audioRoot, Func<bool> getIsPaused)
        {
            this.coroutineHost = coroutineHost;
            this.audioRoot = audioRoot;
            this.getIsPaused = getIsPaused;
        }

        public void ConfigureGameplayMusic(
            AudioClip[] clips,
            float volume,
            float initialDelay,
            float replayDelay,
            float fadeInDuration,
            AudioMixerGroup outputMixerGroup)
        {
            gameplayClips = SanitizeGameplayClips(clips);
            shuffledGameplayClips.Clear();
            nextGameplayClipIndex = 0;
            lastPlayedClip = null;
            skipToNextRequested = false;
            baseVolume = Mathf.Clamp01(volume);
            initialDelaySeconds = Mathf.Max(0f, initialDelay);
            replayDelaySeconds = Mathf.Max(0f, replayDelay);
            fadeInSeconds = Mathf.Max(0f, fadeInDuration);
            mixerGroup = outputMixerGroup;

            EnsureAudioSource();
            musicAudioSource.outputAudioMixerGroup = mixerGroup;
            musicAudioSource.volume = shouldPlay ? GetCurrentVolume() : 0f;

            foreach (AudioClip clip in gameplayClips)
            {
                if (clip.loadState == AudioDataLoadState.Unloaded)
                {
                    clip.LoadAudioData();
                }
            }
        }

        public void StartGameplayMusic()
        {
            if (gameplayClips.Length == 0)
            {
                Debug.LogWarning("[BackgroundMusicService] Gameplay music playlist is empty.");
                StopGameplayMusic();
                return;
            }

            // If the title music is playing (main menu), stop it before starting gameplay music.
            StopTitleMusic();

            EnsureAudioSource();
            shouldPlay = true;
            audioSourcePaused = false;
            musicAudioSource.volume = GetCurrentVolume();

            if (playbackRoutine != null)
            {
                coroutineHost.StopCoroutine(playbackRoutine);
            }

            shuffledGameplayClips.Clear();
            nextGameplayClipIndex = 0;
            lastPlayedClip = null;
            skipToNextRequested = false;
            playbackRoutine = coroutineHost.StartCoroutine(RunGameplayMusicLoop());
        }

        public void ConfigureTitleTrack(AudioClip clip, float volume, AudioMixerGroup outputMixerGroup)
        {
            titleClip = clip;
            titleMixerGroup = outputMixerGroup;
            titleBaseVolume = Mathf.Clamp01(volume);

            if (titleClip != null && titleClip.loadState == AudioDataLoadState.Unloaded)
            {
                titleClip.LoadAudioData();
            }
        }

        public void StartTitleMusic()
        {
            if (titleClip == null)
            {
                return;
            }

            // Stop any gameplay music loop / coroutine first.
            StopGameplayMusic();
            // If clip data is not yet loaded, load and wait for it before playing.
            if (titleClip.loadState == AudioDataLoadState.Unloaded)
            {
                titleClip.LoadAudioData();
            }

            if (titleClip.loadState == AudioDataLoadState.Loading)
            {
                StopTitlePlaybackRoutine();
                titlePlaybackRoutine = coroutineHost.StartCoroutine(WaitForTitleAndPlay());
                return;
            }

            PlayTitleImmediate();
        }

        private void StopTitlePlaybackRoutine()
        {
            if (titlePlaybackRoutine != null)
            {
                coroutineHost.StopCoroutine(titlePlaybackRoutine);
                titlePlaybackRoutine = null;
            }
        }

        private IEnumerator WaitForTitleAndPlay()
        {
            while (titleClip != null && titleClip.loadState == AudioDataLoadState.Loading)
            {
                yield return null;
            }

            if (titleClip == null || titleClip.loadState == AudioDataLoadState.Failed)
            {
                Debug.LogWarning("[BackgroundMusicService] Title music failed to load or was null.");
                titlePlaybackRoutine = null;
                yield break;
            }

            PlayTitleImmediate();
            titlePlaybackRoutine = null;
        }

        private void PlayTitleImmediate()
        {
            if (titleAudioSource == null)
            {
                Transform child = audioRoot.Find("TitleMusicAudioSource");
                if (child == null)
                {
                    GameObject audioSourceObject = new GameObject("TitleMusicAudioSource");
                    child = audioSourceObject.transform;
                    child.SetParent(audioRoot, false);
                }

                titleAudioSource = child.GetComponent<AudioSource>();
                if (titleAudioSource == null)
                {
                    titleAudioSource = child.gameObject.AddComponent<AudioSource>();
                }

                titleAudioSource.playOnAwake = false;
                titleAudioSource.loop = true;
                titleAudioSource.spatialBlend = 0f;
            }

            titleAudioSource.clip = titleClip;
            titleAudioSource.outputAudioMixerGroup = titleMixerGroup;
            titleAudioSource.volume = MusicSettings.GetEffectiveVolume(titleBaseVolume);
            titleAudioSource.loop = true;
            titleAudioSource.Play();
        }

        public void StopTitleMusic()
        {
            StopTitlePlaybackRoutine();
            if (titleAudioSource == null)
            {
                return;
            }

            if (titleAudioSource.isPlaying)
            {
                titleAudioSource.Stop();
            }

            titleAudioSource.clip = null;
            titleAudioSource.volume = 0f;
        }

        public void StopGameplayMusic()
        {
            shouldPlay = false;
            audioSourcePaused = false;
            skipToNextRequested = false;

            if (playbackRoutine != null)
            {
                coroutineHost.StopCoroutine(playbackRoutine);
                playbackRoutine = null;
            }

            if (musicAudioSource == null)
            {
                return;
            }

            musicAudioSource.Stop();
            musicAudioSource.clip = null;
            musicAudioSource.volume = 0f;
        }

        public void SkipToNextTrack()
        {
            if (!shouldPlay || gameplayClips.Length == 0)
            {
                return;
            }

            skipToNextRequested = true;
            audioSourcePaused = false;

            if (musicAudioSource != null && musicAudioSource.isPlaying)
            {
                musicAudioSource.Stop();
            }
        }

        public AudioClip GetCurrentGameplayTrack()
        {
            return musicAudioSource != null ? musicAudioSource.clip : null;
        }

        public AudioClip GetNextGameplayTrack()
        {
            return PeekNextGameplayClip();
        }

        public void Pause()
        {
            if (!shouldPlay)
            {
                return;
            }

            EnsureAudioSource();
            if (musicAudioSource.isPlaying)
            {
                musicAudioSource.Pause();
                audioSourcePaused = true;
            }
        }

        public void Resume()
        {
            if (!shouldPlay || musicAudioSource == null)
            {
                return;
            }

            if (audioSourcePaused && !getIsPaused())
            {
                musicAudioSource.UnPause();
                musicAudioSource.volume = GetCurrentVolume();
                audioSourcePaused = false;
            }
        }

        private IEnumerator RunGameplayMusicLoop()
        {
            yield return WaitForUnpausedDelay(initialDelaySeconds);

            while (shouldPlay)
            {
                AudioClip nextClip = GetNextGameplayClip();
                if (nextClip == null)
                {
                    Debug.LogWarning("[BackgroundMusicService] Gameplay music playlist contains no playable clips.");
                    playbackRoutine = null;
                    yield break;
                }

                EnsureAudioSource();

                if (nextClip.loadState == AudioDataLoadState.Unloaded)
                {
                    nextClip.LoadAudioData();
                }

                while (shouldPlay && nextClip.loadState == AudioDataLoadState.Loading)
                {
                    yield return null;
                }

                if (nextClip.loadState == AudioDataLoadState.Failed)
                {
                    Debug.LogWarning($"[BackgroundMusicService] Gameplay music audio data failed to load for clip '{nextClip.name}'.");
                    yield return WaitForUnpausedDelay(replayDelaySeconds);
                    continue;
                }

                musicAudioSource.clip = nextClip;
                musicAudioSource.loop = false;
                musicAudioSource.volume = fadeInSeconds > 0f ? 0f : GetCurrentVolume();
                musicAudioSource.Play();
                audioSourcePaused = false;

                if (fadeInSeconds > 0f)
                {
                    yield return FadeInCurrentClip();
                }

                bool skippedCurrentTrack = false;
                while (shouldPlay)
                {
                    if (skipToNextRequested)
                    {
                        skipToNextRequested = false;
                        skippedCurrentTrack = true;
                        break;
                    }

                    if (getIsPaused())
                    {
                        if (!audioSourcePaused && musicAudioSource.isPlaying)
                        {
                            musicAudioSource.Pause();
                            audioSourcePaused = true;
                        }

                        yield return null;
                        continue;
                    }

                    if (audioSourcePaused)
                    {
                        musicAudioSource.UnPause();
                        musicAudioSource.volume = GetCurrentVolume();
                        audioSourcePaused = false;
                    }

                    musicAudioSource.volume = GetCurrentVolume();

                    if (!musicAudioSource.isPlaying)
                    {
                        if (audioSourcePaused || getIsPaused())
                        {
                            yield return null;
                            continue;
                        }

                        if (!HasTrackFinishedNaturally())
                        {
                            yield return null;
                            continue;
                        }

                        break;
                    }

                    yield return null;
                }

                if (!shouldPlay)
                {
                    break;
                }

                if (skippedCurrentTrack)
                {
                    continue;
                }

                yield return WaitForUnpausedDelay(replayDelaySeconds);
            }

            playbackRoutine = null;
        }

        private IEnumerator FadeInCurrentClip()
        {
            float elapsed = 0f;
            while (elapsed < fadeInSeconds)
            {
                if (!shouldPlay)
                {
                    yield break;
                }

                if (skipToNextRequested)
                {
                    yield break;
                }

                if (getIsPaused())
                {
                    if (!audioSourcePaused && musicAudioSource.isPlaying)
                    {
                        musicAudioSource.Pause();
                        audioSourcePaused = true;
                    }

                    yield return null;
                    continue;
                }

                if (audioSourcePaused)
                {
                    musicAudioSource.UnPause();
                    audioSourcePaused = false;
                }

                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / fadeInSeconds);
                musicAudioSource.volume = Mathf.Lerp(0f, GetCurrentVolume(), progress);
                yield return null;
            }

            musicAudioSource.volume = GetCurrentVolume();
        }

        private IEnumerator WaitForUnpausedDelay(float delaySeconds)
        {
            if (delaySeconds <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < delaySeconds)
            {
                if (!shouldPlay)
                {
                    yield break;
                }

                if (skipToNextRequested)
                {
                    skipToNextRequested = false;
                    yield break;
                }

                if (!getIsPaused())
                {
                    elapsed += Time.unscaledDeltaTime;
                }

                yield return null;
            }
        }

        private void EnsureAudioSource()
        {
            if (musicAudioSource != null)
            {
                return;
            }

            Transform child = audioRoot.Find(AudioSourceObjectName);
            if (child == null)
            {
                GameObject audioSourceObject = new GameObject(AudioSourceObjectName);
                child = audioSourceObject.transform;
                child.SetParent(audioRoot, false);
            }

            musicAudioSource = child.GetComponent<AudioSource>();
            if (musicAudioSource == null)
            {
                musicAudioSource = child.gameObject.AddComponent<AudioSource>();
            }

            musicAudioSource.playOnAwake = false;
            musicAudioSource.loop = false;
            musicAudioSource.spatialBlend = 0f;
            musicAudioSource.outputAudioMixerGroup = mixerGroup;
        }

        private float GetCurrentVolume()
        {
            return MusicSettings.GetEffectiveVolume(baseVolume);
        }

        private bool HasTrackFinishedNaturally()
        {
            if (musicAudioSource == null)
            {
                return false;
            }

            if (musicAudioSource.clip == null || musicAudioSource.clip.length <= 0f)
            {
                return false;
            }

            return musicAudioSource.time >= musicAudioSource.clip.length - EndOfTrackGraceSeconds;
        }

        private AudioClip[] SanitizeGameplayClips(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return Array.Empty<AudioClip>();
            }

            List<AudioClip> filteredClips = new();
            foreach (AudioClip clip in clips)
            {
                if (clip == null)
                {
                    continue;
                }

                filteredClips.Add(clip);
            }

            return filteredClips.ToArray();
        }

        private AudioClip GetNextGameplayClip()
        {
            if (gameplayClips.Length == 0)
            {
                return null;
            }

            if (gameplayClips.Length == 1)
            {
                lastPlayedClip = gameplayClips[0];
                return gameplayClips[0];
            }

            EnsureShuffleBagAvailable();

            AudioClip nextClip = shuffledGameplayClips[nextGameplayClipIndex];
            nextGameplayClipIndex++;
            lastPlayedClip = nextClip;

            if (nextGameplayClipIndex >= shuffledGameplayClips.Count)
            {
                RefillShuffleBag();
            }

            return nextClip;
        }

        private AudioClip PeekNextGameplayClip()
        {
            if (gameplayClips.Length == 0)
            {
                return null;
            }

            if (gameplayClips.Length == 1)
            {
                return gameplayClips[0];
            }

            EnsureShuffleBagAvailable();
            return shuffledGameplayClips[nextGameplayClipIndex];
        }

        private void EnsureShuffleBagAvailable()
        {
            if (nextGameplayClipIndex < shuffledGameplayClips.Count)
            {
                return;
            }

            RefillShuffleBag();
        }

        private void RefillShuffleBag()
        {
            shuffledGameplayClips.Clear();
            shuffledGameplayClips.AddRange(gameplayClips);

            for (int index = shuffledGameplayClips.Count - 1; index > 0; index--)
            {
                int swapIndex = UnityEngine.Random.Range(0, index + 1);
                (shuffledGameplayClips[index], shuffledGameplayClips[swapIndex]) = (shuffledGameplayClips[swapIndex], shuffledGameplayClips[index]);
            }

            if (lastPlayedClip != null && shuffledGameplayClips.Count > 1 && ReferenceEquals(shuffledGameplayClips[0], lastPlayedClip))
            {
                int swapIndex = UnityEngine.Random.Range(1, shuffledGameplayClips.Count);
                (shuffledGameplayClips[0], shuffledGameplayClips[swapIndex]) = (shuffledGameplayClips[swapIndex], shuffledGameplayClips[0]);
            }

            nextGameplayClipIndex = 0;
        }
    }
}