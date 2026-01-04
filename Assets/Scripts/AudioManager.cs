using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip[] buildSounds;
    [SerializeField] private AudioClip[] demolishSounds;
    [SerializeField] private AudioClip[] researchSounds;

    [Header("Volumes")]
    [SerializeField] private float buildVolume = 1f;
    [SerializeField] private float demolishVolume = 1f;
    [SerializeField] private float researchVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D звук
        }
    }

    // ================= PUBLIC API =================

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

    // ================= INTERNAL =================

    private void PlayRandom(AudioClip[] clips, float volume)
    {
        if (audioSource == null) return;
        if (clips == null || clips.Length == 0) return;

        var clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        audioSource.PlayOneShot(clip, volume);
    }
}