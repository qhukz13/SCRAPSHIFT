using UnityEngine;

namespace SpaceMaintenance.Effects
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private float _shakeDuration = 0f;
        private float _shakeMagnitude = 0.7f;
        private float _dampingSpeed = 1.0f;

        private Vector3 _initialPosition;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            _initialPosition = transform.localPosition;
        }

        private void Update()
        {
            if (_shakeDuration > 0)
            {
                transform.localPosition = _initialPosition + Random.insideUnitSphere * _shakeMagnitude;
                _shakeDuration -= Time.deltaTime * _dampingSpeed;
            }
            else
            {
                _shakeDuration = 0f;
                transform.localPosition = _initialPosition;
            }
        }

        public void TriggerShake(float duration, float magnitude)
        {
            _shakeDuration = duration;
            _shakeMagnitude = magnitude;
        }
    }
}
