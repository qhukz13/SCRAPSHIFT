using Unity.Netcode;
using SpaceMaintenance.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceMaintenance.Player
{
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PhysicsGrabController : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _grabRange = 3f;
        [SerializeField] private float _grabForce = 15f;
        [SerializeField] private float _throwForce = 10f;
        [SerializeField] private Transform _holdPoint;
        [SerializeField] private LayerMask _grabbableLayer;

        private PlayerInputHandler _input;
        private IGrabbable _grabbedObject;
        private Camera _mainCamera;

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            _mainCamera = GetComponentInChildren<Camera>();
            if (_mainCamera == null) _mainCamera = Camera.main;
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner && _grabbedObject != null)
            {
                Release();
            }
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (Mouse.current != null)
            {
                if (Mouse.current.rightButton.wasPressedThisFrame) // Right click to grab
                {
                    if (_grabbedObject == null) TryGrab();
                    else Release();
                }
                else if (Mouse.current.leftButton.wasPressedThisFrame) // Left click to throw
                {
                    if (_grabbedObject != null) Throw();
                }
            }
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            if (_grabbedObject != null && _grabbedObject.Rigidbody != null)
            {
                MoveGrabbedObject();
            }
        }

        private void TryGrab()
        {
            if (_mainCamera == null) return;

            if (Physics.Raycast(_mainCamera.transform.position, _mainCamera.transform.forward, out RaycastHit hit, _grabRange, _grabbableLayer))
            {
                var grabbable = hit.collider.GetComponentInParent<IGrabbable>();
                if (grabbable != null && !grabbable.IsGrabbed)
                {
                    _grabbedObject = grabbable;
                    
                    var rb = _grabbedObject.Rigidbody;
                    if (rb != null)
                    {
                        rb.useGravity = false;
                        rb.linearDamping = 10f;
                    }

                    _grabbedObject.OnGrab(gameObject);
                }
            }
        }

        private void MoveGrabbedObject()
        {
            var rb = _grabbedObject.Rigidbody;
            Vector3 direction = _holdPoint.position - rb.position;
            
            // Calculate velocity needed to reach hold point
            Vector3 force = direction * _grabForce;
            rb.linearVelocity = force;
        }

        private void Release()
        {
            if (_grabbedObject != null)
            {
                var rb = _grabbedObject.Rigidbody;
                if (rb != null)
                {
                    rb.useGravity = true;
                    rb.linearDamping = 0f;
                }

                _grabbedObject.OnRelease();
                _grabbedObject = null;
            }
        }

        private void Throw()
        {
            if (_grabbedObject != null)
            {
                var rb = _grabbedObject.Rigidbody;
                if (rb != null)
                {
                    rb.useGravity = true;
                    rb.linearDamping = 0f;
                }

                Vector3 force = _mainCamera.transform.forward * _throwForce;
                _grabbedObject.OnThrow(force);
                
                // For safety if OnThrow doesn't apply the physics force
                if (rb != null)
                {
                    rb.AddForce(force, ForceMode.Impulse);
                }

                _grabbedObject = null;
            }
        }
    }
}
