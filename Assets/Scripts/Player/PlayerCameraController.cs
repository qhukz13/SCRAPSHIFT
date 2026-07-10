using UnityEngine;

namespace SpaceMaintenance.Player
{
    public class PlayerCameraController : MonoBehaviour
    {
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private PlayerMovementConfig _config;
        
        private float _xRotation = 0f;
        private PlayerInputHandler _inputHandler;

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

        public void HandleCameraRotation()
        {
            if (_inputHandler == null || _config == null) return;

            float mouseX = _inputHandler.LookInput.x * _config.MouseSensitivity;
            float mouseY = _inputHandler.LookInput.y * _config.MouseSensitivity;

            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

            if (_cameraTransform != null)
            {
                _cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            }
            
            transform.Rotate(Vector3.up * mouseX);
        }
    }
}
