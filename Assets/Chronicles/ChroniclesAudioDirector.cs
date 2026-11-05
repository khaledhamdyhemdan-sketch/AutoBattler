using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    public enum AudioScene { Menu, Story, Battle, Victory, Defeat }

    // Pure Unity music/SFX bridge: original licensed AudioClip assets can be added later.
    // Resources/Chronicles/Audio/Music/{menu,story_1,battle_1,victory,defeat}
    // Resources/Chronicles/Audio/SFX/{click,summon,hero_<id>_ultimate}
    public sealed class ChroniclesAudioDirector : MonoBehaviour
    {
        public static ChroniclesAudioDirector Instance { get; private set; }
        private AudioSource musicSource, effectSource;
        private readonly Dictionary<string, AudioClip> cache =
            new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private string currentTrack;
        public float MusicVolume { get; private set; } = .8f;
        public float EffectsVolume { get; private set; } = .9f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            effectSource = gameObject.AddComponent<AudioSource>();
            effectSource.playOnAwake = false;
            effectSource.spatialBlend = 0f;
            SetVolumes(MusicVolume, EffectsVolume);
        }

        private AudioClip Clip(string key)
        {
            AudioClip clip;
            if (cache.TryGetValue(key, out clip)) return clip;
            clip = Resources.Load<AudioClip>("Chronicles/Audio/" + key);
            cache.Add(key, clip);
            return clip;
        }

        public void SetVolumes(float music, float effects)
        {
            MusicVolume = Mathf.Clamp01(music);
            EffectsVolume = Mathf.Clamp01(effects);
            if (musicSource != null) musicSource.volume = MusicVolume;
            if (effectSource != null) effectSource.volume = EffectsVolume;
        }

        public void SetState(AudioScene scene, int chapter = 0)
        {
            string track;
            switch (scene)
            {
                case AudioScene.Story: track = "Music/story_" + Mathf.Clamp(chapter, 1, 6); break;
                case AudioScene.Battle: track = "Music/battle_" + Mathf.Clamp(chapter, 1, 6); break;
                case AudioScene.Victory: track = "Music/victory"; break;
                case AudioScene.Defeat: track = "Music/defeat"; break;
                default: track = "Music/menu"; break;
            }
            if (track == currentTrack) return;
            currentTrack = track;
            AudioClip clip = Clip(track);
            if (clip == null && (scene == AudioScene.Story || scene == AudioScene.Battle))
                clip = Clip("Music/" + (scene == AudioScene.Story ? "story" : "battle"));
            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.loop = scene != AudioScene.Victory && scene != AudioScene.Defeat;
            if (clip != null) musicSource.Play();
        }

        public void PlayEffect(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || fileName.Contains("/") ||
                fileName.Contains("\\") || fileName.Contains("..")) return;
            AudioClip clip = Clip("SFX/" + fileName);
            if (clip != null) effectSource.PlayOneShot(clip);
        }

        public void HeroUltimate(string heroId)
        {
            if (!string.IsNullOrEmpty(heroId)) PlayEffect("hero_" + heroId + "_ultimate");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
