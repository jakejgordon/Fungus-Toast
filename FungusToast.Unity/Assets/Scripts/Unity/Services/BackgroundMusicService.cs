using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace FungusToast.Unity
{
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

        private float baseVolume = 0.28f;
        private float initialDelaySeconds = 1.5f;
        private float replayDelaySeconds = 4f;
        private float fadeInSeconds = 1f;
        private bool shouldPlay;
        private bool audioSourcePaused;

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
            playbackRoutine = coroutineHost.StartCoroutine(RunGameplayMusicLoop());
        }

        public void StopGameplayMusic()
        {
            shouldPlay = false;
            audioSourcePaused = false;

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

                while (shouldPlay)
                {
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

            if (nextGameplayClipIndex >= shuffledGameplayClips.Count)
            {
                RefillShuffleBag();
            }

            AudioClip nextClip = shuffledGameplayClips[nextGameplayClipIndex];
            nextGameplayClipIndex++;
            lastPlayedClip = nextClip;
            return nextClip;
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