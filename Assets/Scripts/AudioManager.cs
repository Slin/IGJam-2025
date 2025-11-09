using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music")]
    public AudioClip soundtrack;

    [Header("SFX")]
    public AudioClip laserPlayerSound;
    public AudioClip laserEnemySound;
    public AudioClip clickSound;
    public AudioClip placeBuildingSound;
    public AudioClip rocketLaunchSound;
    public AudioClip explosionSound;
    public AudioClip buildingEndSound;
    public AudioClip defenseEndSound;
    public AudioClip roundStartSound;
    public AudioClip gameOverSound;
    public AudioClip teleportSound;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    Dictionary<string, AudioClip> _sfxLibrary;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioSources();
        BuildSFXLibrary();
    }

    void InitializeAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        UpdateVolumes();
    }

    void BuildSFXLibrary()
    {
        _sfxLibrary = new Dictionary<string, AudioClip>
        {
            { "laser_player", laserPlayerSound },
            { "laser_enemy", laserEnemySound },
            { "click", clickSound },
            { "place_building", placeBuildingSound },
            { "rocket_launch", rocketLaunchSound },
            { "explosion", explosionSound },
            { "building_end", buildingEndSound },
            { "defense_end", defenseEndSound },
            { "round_start", roundStartSound },
            { "game_over", gameOverSound },
            { "teleport", teleportSound }
        };
    }

    public void PlayMusic(AudioClip clip, bool fadeIn = false)
    {
        if (clip == null || musicSource == null) return;

        if (musicSource.isPlaying && musicSource.clip == clip)
            return;

        if (fadeIn)
        {
            StartCoroutine(FadeMusic(clip));
        }
        else
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    public void PlaySoundtrack(bool fadeIn = false)
    {
        PlayMusic(soundtrack, fadeIn);
    }

    public void StopMusic(bool fadeOut = false)
    {
        if (musicSource == null) return;

        if (fadeOut)
        {
            StartCoroutine(FadeOutMusic());
        }
        else
        {
            musicSource.Stop();
        }
    }

    public void PlaySFX(string sfxName, float volumeMultiplier = 1f)
    {
        if (sfxSource == null) return;

        if (_sfxLibrary.TryGetValue(sfxName, out AudioClip clip) && clip != null)
        {
            PlaySFX(clip, volumeMultiplier);
        }
    }

    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null || sfxSource == null) return;

        float volume = masterVolume * sfxVolume * volumeMultiplier;
        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
    {
        if (clip == null) return;

        float volume = masterVolume * sfxVolume * volumeMultiplier;
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    void UpdateVolumes()
    {
        if (musicSource != null)
            musicSource.volume = masterVolume * musicVolume;

        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;
    }

    System.Collections.IEnumerator FadeMusic(AudioClip newClip, float fadeDuration = 1f)
    {
        // Fade out current music
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        // Switch to new clip
        musicSource.clip = newClip;
        musicSource.Play();

        // Fade in new music
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, masterVolume * musicVolume, elapsed / fadeDuration);
            yield return null;
        }

        musicSource.volume = masterVolume * musicVolume;
    }

    System.Collections.IEnumerator FadeOutMusic(float fadeDuration = 1f)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = masterVolume * musicVolume;
    }

    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
            musicSource.UnPause();
    }
}
