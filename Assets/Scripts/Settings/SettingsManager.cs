using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Persistent singleton that manages saving and loading player settings via PlayerPrefs.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [Tooltip("Reference to the main AudioMixer to control volume groups.")]
    public AudioMixer mainMixer;

    // Keys for PlayerPrefs
    private const string PREF_MASTER_VOL = "MasterVolume";
    private const string PREF_MUSIC_VOL = "MusicVolume";
    private const string PREF_SFX_VOL = "SFXVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        LoadSettings();
    }

    public void LoadSettings()
    {
        // Default to 0.75 (75%) if no setting is saved
        float masterVol = PlayerPrefs.GetFloat(PREF_MASTER_VOL, 0.75f);
        float musicVol = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.75f);
        float sfxVol = PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.75f);

        SetMasterVolume(masterVol);
        SetMusicVolume(musicVol);
        SetSFXVolume(sfxVol);
    }

    // ─────────────────────────────────────────────
    //  Volume Setters (0.0f to 1.0f)
    // ─────────────────────────────────────────────

    public void SetMasterVolume(float volume)
    {
        PlayerPrefs.SetFloat(PREF_MASTER_VOL, volume);
        if (mainMixer != null)
        {
            // Convert linear 0-1 to logarithmic -80dB to 0dB
            float db = volume > 0.001f ? Mathf.Log10(volume) * 20 : -80f;
            mainMixer.SetFloat("MasterVolume", db);
        }
    }

    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(PREF_MUSIC_VOL, volume);
        if (mainMixer != null)
        {
            float db = volume > 0.001f ? Mathf.Log10(volume) * 20 : -80f;
            mainMixer.SetFloat("MusicVolume", db);
        }
    }

    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat(PREF_SFX_VOL, volume);
        if (mainMixer != null)
        {
            float db = volume > 0.001f ? Mathf.Log10(volume) * 20 : -80f;
            mainMixer.SetFloat("SFXVolume", db);
        }
    }

    // ─────────────────────────────────────────────
    //  Getters for UI Sliders
    // ─────────────────────────────────────────────

    public float GetMasterVolume() => PlayerPrefs.GetFloat(PREF_MASTER_VOL, 0.75f);
    public float GetMusicVolume() => PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.75f);
    public float GetSFXVolume() => PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.75f);

    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }
}
