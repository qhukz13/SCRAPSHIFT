using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;

namespace SpaceMaintenance.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [SerializeField] private SFXDatabase _database;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _ambientGroup;
        [SerializeField] private AudioMixerGroup _uiGroup;
        
        [Header("Pool Settings")]
        [SerializeField] private int _poolSize = 20;
        
        private List<AudioSource> _sfxPool;
        private AudioSource _ambientSource;
        private AudioSource _uiSource;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializePools();
        }
        
        private void InitializePools()
        {
            // SFX Pool
            _sfxPool = new List<AudioSource>();
            GameObject sfxParent = new GameObject("SFX_Pool");
            sfxParent.transform.SetParent(transform);
            
            for (int i = 0; i < _poolSize; i++)
            {
                var source = sfxParent.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.outputAudioMixerGroup = _sfxGroup;
                source.spatialBlend = 1f; // 3D by default
                _sfxPool.Add(source);
            }
            
            // Ambient Source
            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.loop = true;
            _ambientSource.outputAudioMixerGroup = _ambientGroup;
            _ambientSource.spatialBlend = 0f; // 2D by default
            
            // UI Source
            _uiSource = gameObject.AddComponent<AudioSource>();
            _uiSource.playOnAwake = false;
            _uiSource.outputAudioMixerGroup = _uiGroup;
            _uiSource.spatialBlend = 0f; // 2D
        }
        
        private AudioSource GetAvailableSFXSource()
        {
            foreach (var source in _sfxPool)
            {
                if (!source.isPlaying) return source;
            }
            
            // If all busy, create a new one to expand pool
            var newSource = _sfxPool[0].gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.outputAudioMixerGroup = _sfxGroup;
            newSource.spatialBlend = 1f;
            _sfxPool.Add(newSource);
            return newSource;
        }
        
        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            var source = GetAvailableSFXSource();
            source.transform.position = position;
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = 1f;
            source.Play();
        }

        public void PlaySFX2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            var source = GetAvailableSFXSource();
            source.clip = clip;
            source.volume = volume;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            source.Play();
        }

        public void PlayUISound(AudioClip clip, float volume = 1f)
        {
            if (clip == null || _uiSource == null) return;
            _uiSource.PlayOneShot(clip, volume);
        }

        public void PlayAmbient(AudioClip clip, float volume = 1f)
        {
            if (clip == null || _ambientSource == null) return;
            if (_ambientSource.clip == clip && _ambientSource.isPlaying) return;
            
            _ambientSource.clip = clip;
            _ambientSource.volume = volume;
            _ambientSource.Play();
        }

        public void StopAmbient()
        {
            if (_ambientSource != null) _ambientSource.Stop();
        }
        
        public SFXDatabase Database => _database;
    }
}
