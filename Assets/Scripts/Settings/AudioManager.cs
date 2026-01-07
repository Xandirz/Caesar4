using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip[] buildSounds;
    [SerializeField] private AudioClip[] demolishSounds;
    [SerializeField] private AudioClip[] researchSounds;

    [Header("Base Volumes")]
    [SerializeField] private float buildVolume = 1f;
    [SerializeField] private float demolishVolume = 1f;
    [SerializeField] private float researchVolume = 1f;

    // === SETTINGS ===
    private float masterVolume = 1f; // 0..1
    private float musicVolume = 1f;  // 0..1

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
    }

    // ================= SETTINGS API =================

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);

        // ГЛОБАЛЬНО для SFX
        if (sfxSource != null)
            sfxSource.volume = masterVolume;

        UpdateMusicVolume();
    }

    private void PlayRandom(AudioClip[] clips, float baseVolume)
    {
        if (sfxSource == null) return;
        if (clips == null || clips.Length == 0) return;

        if (masterVolume <= 0.0001f) return; // гарантированный mute

        var clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        // master уже учтён в sfxSource.volume
        sfxSource.PlayOneShot(clip, baseVolume);
    }


    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        UpdateMusicVolume();
    }

    private void UpdateMusicVolume()
    {
        if (musicSource != null)
            musicSource.volume = masterVolume * musicVolume;
    }

    // ================= MUSIC =================

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;

        musicSource.clip = clip;
        UpdateMusicVolume();
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    // ================= SFX =================

    public void PlayBuild()
    {
        PlayRandom(buildSounds, buildVolume);
    }

    public void PlayDemolish()
    {
        PlayRandom(demolishSounds, demolishVolume);
    }

    public void PlayResearch()
    {
        PlayRandom(researchSounds, researchVolume);
    }


}
