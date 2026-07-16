using SpaceMaintenance.Core;
using SpaceMaintenance.Core.Data;
using UnityEngine;

namespace SpaceMaintenance.Effects
{
    public class GameOverEffects : MonoBehaviour
    {
        [Header("Explosion Effects")]
        [SerializeField] private GameObject _explosionVfxPrefab;
        [SerializeField] private AudioClip _explosionSound;

        private void OnEnable()
        {
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        }

        private void OnGameOver(GameOverEvent evt)
        {
            if (!evt.IsVictory)
            {
                // Determine if this is a catastrophic failure
                bool isCatastrophic = evt.Reason.Contains("integrity") || evt.Reason.Contains("Meltdown") || evt.Reason.Contains("destroyed");

                if (isCatastrophic)
                {
                    TriggerExplosionEffect();
                }
            }
        }

        private void TriggerExplosionEffect()
        {
            // Shake the camera intensely
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.TriggerShake(duration: 2.5f, magnitude: 1.5f);
            }
            else
            {
                // Fallback if CameraShake is not a singleton but is on the main camera
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    var shake = mainCam.GetComponent<CameraShake>();
                    if (shake == null)
                    {
                        shake = mainCam.gameObject.AddComponent<CameraShake>();
                    }
                    shake.TriggerShake(duration: 2.5f, magnitude: 1.5f);
                }
            }

            // Play explosion sound if available
            if (_explosionSound != null)
            {
                // Play at camera position
                AudioSource.PlayClipAtPoint(_explosionSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 1f);
            }

            // Instantiate VFX if available
            if (_explosionVfxPrefab != null && Camera.main != null)
            {
                // Spawn a bit in front of the camera so the player sees it
                Instantiate(_explosionVfxPrefab, Camera.main.transform.position + Camera.main.transform.forward * 2f, Quaternion.identity);
            }

            Debug.Log("[GameOverEffects] Triggered catastrophic explosion effect!");
        }
    }
}
