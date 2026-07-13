using UnityEngine;
using UnityEngine.Audio;

namespace SpaceMaintenance.Core
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("Audio Mixers (Optional)")]
        [SerializeField] private AudioMixer _mainMixer;

        // Settings values
        public float MasterVolume { get; private set; } = 1f;
        public float Sensitivity { get; private set; } = 2f;
        public bool IsFullscreen { get; private set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadSettings();
        }

        private void Start()
        {
            ApplyAllSettings();
        }

        public void LoadSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat("Settings_MasterVolume", 1f);
            Sensitivity = PlayerPrefs.GetFloat("Settings_Sensitivity", 2f);
            IsFullscreen = PlayerPrefs.GetInt("Settings_Fullscreen", 1) == 1;
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat("Settings_MasterVolume", MasterVolume);
            PlayerPrefs.SetFloat("Settings_Sensitivity", Sensitivity);
            PlayerPrefs.SetInt("Settings_Fullscreen", IsFullscreen ? 1 : 0);
            PlayerPrefs.Save();
            ApplyAllSettings();
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp(volume, 0.0001f, 1f);
            SaveSettings();
        }

        public void SetSensitivity(float sensitivity)
        {
            Sensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
            SaveSettings();
        }

        public void SetFullscreen(bool fullscreen)
        {
            IsFullscreen = fullscreen;
            SaveSettings();
        }

        private void ApplyAllSettings()
        {
            // Apply Audio
            if (_mainMixer != null)
            {
                // Convert linear volume to decibels
                float db = Mathf.Log10(MasterVolume) * 20f;
                _mainMixer.SetFloat("MasterVolume", db);
            }
            else
            {
                AudioListener.volume = MasterVolume;
            }

            // Apply Resolution
            if (IsFullscreen != Screen.fullScreen)
            {
                Screen.fullScreen = IsFullscreen;
            }
        }
    }
}
