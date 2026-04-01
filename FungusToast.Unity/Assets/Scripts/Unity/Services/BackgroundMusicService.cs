using System;
using System.Collections;
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

        private AudioSource musicAudioSource;
        private AudioClip gameplayClip;
        private AudioMixerGroup mixerGroup;
        private Coroutine playbackRoutine;

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
            AudioClip clip,
            float volume,
            float initialDelay,
            float replayDelay,
            float fadeInDuration,
            AudioMixerGroup outputMixerGroup)
        {
            gameplayClip = clip;
            baseVolume = Mathf.Clamp01(volume);
            initialDelaySeconds = Mathf.Max(0f, initialDelay);
            replayDelaySeconds = Mathf.Max(0f, replayDelay);
            fadeInSeconds = Mathf.Max(0f, fadeInDuration);
            mixerGroup = outputMixerGroup;

            EnsureAudioSource();
            musicAudioSource.outputAudioMixerGroup = mixerGroup;
            musicAudioSource.volume = shouldPlay ? GetCurrentVolume() : 0f;

            if (gameplayClip != null && gameplayClip.loadState == AudioDataLoadState.Unloaded)
            {
                gameplayClip.LoadAudioData();
            }
        }

        public void StartGameplayMusic()
        {
            if (gameplayClip == null)
            {
                Debug.LogWarning("[BackgroundMusicService] Gameplay music clip is not assigned.");
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
                if (gameplayClip == null)
                {
                    playbackRoutine = null;
                    yield break;
                }

                EnsureAudioSource();

                if (gameplayClip.loadState == AudioDataLoadState.Unloaded)
                {
                    gameplayClip.LoadAudioData();
                }

                while (shouldPlay && gameplayClip.loadState == AudioDataLoadState.Loading)
                {
                    yield return null;
                }

                if (gameplayClip.loadState == AudioDataLoadState.Failed)
                {
                    Debug.LogWarning("[BackgroundMusicService] Gameplay music audio data failed to load.");
                    playbackRoutine = null;
                    yield break;
                }

                musicAudioSource.clip = gameplayClip;
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
            if (musicAudioSource == null || gameplayClip == null)
            {
                return false;
            }

            if (musicAudioSource.clip == null || musicAudioSource.clip.length <= 0f)
            {
                return false;
            }

            return musicAudioSource.time >= musicAudioSource.clip.length - EndOfTrackGraceSeconds;
        }
    }
}