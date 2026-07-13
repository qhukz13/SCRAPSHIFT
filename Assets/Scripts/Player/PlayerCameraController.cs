// ============================================================================
// Space Maintenance — PlayerCameraController.cs
// First-person camera: horizontal rotation on body, vertical on camera.
// Supports dynamic camera height for crouch transitions.
// ============================================================================

using UnityEngine;

namespace SpaceMaintenance.Player
{
    public class PlayerCameraController : MonoBehaviour
    {
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private PlayerMovementConfig _config;
        
        private float _xRotation = 0f;
        private PlayerInputHandler _inputHandler;

        /// <summary>Current local Y of the camera (used by crouch lerp).</summary>
        public float CameraLocalY => _cameraTransform != null ? _cameraTransform.localPosition.y : 0f;

        public void Initialize(PlayerInputHandler input, PlayerMovementConfig config)
        {
            _inputHandler = input;
            _config = config;
            Cursor.lockState = CursorLockMode.Locked;
            
            if (_cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) _cameraTransform = cam.transform;
            }
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Always disable the default scene camera if it exists so our local player camera is used.
            if (_cameraTransform != null && Camera.main != null && Camera.main.gameObject != _cameraTransform.gameObject)
            {
                Camera.main.gameObject.SetActive(false);
            }
        }

        public void HandleCameraRotation()
        {
            if (_inputHandler == null || _config == null) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;

            float globalSens = SpaceMaintenance.Core.SettingsManager.Instance != null ? SpaceMaintenance.Core.SettingsManager.Instance.Sensitivity : 2f;
            float mouseX = _inputHandler.LookInput.x * _config.MouseSensitivity * globalSens * 0.1f;
            float mouseY = _inputHandler.LookInput.y * _config.MouseSensitivity * globalSens * 0.1f;

            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

            if (_cameraTransform != null)
            {
                _cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            }
            
            transform.Rotate(Vector3.up * mouseX);
        }

        /// <summary>Set the camera's local Y position (for smooth crouch transitions).</summary>
        public void SetTargetLocalY(float y)
        {
            if (_cameraTransform == null) return;
            var pos = _cameraTransform.localPosition;
            pos.y = y;
            _cameraTransform.localPosition = pos;
        }
    }
}
