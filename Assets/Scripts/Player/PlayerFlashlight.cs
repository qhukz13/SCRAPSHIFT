// ============================================================================
// SCRAPSHIFT — PlayerFlashlight.cs
// Handles the synchronized flashlight for the player.
// Includes a battery mechanic that drains when on and recharges when off.
// ============================================================================

using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Player
{
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerFlashlight : NetworkBehaviour
    {
        [Header("References")]
        [Tooltip("The Unity Light component to toggle. Should be a child of the camera.")]
        [SerializeField] private Light _flashlight;
        
        [Header("Battery Settings")]
        public float MaxBattery = 100f;
        public float DrainRate = 5f;       // Units drained per second when ON
        public float RechargeRate = 3f;    // Units recharged per second when OFF
        public float MinBatteryToTurnOn = 10f; // Cannot turn on if battery is too low
        
        // Synced state
        public NetworkVariable<bool> IsOn = new NetworkVariable<bool>(false, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server);

        // Local state
        public float CurrentBattery { get; private set; }
        private PlayerInputHandler _inputHandler;

        private void Awake()
        {
            _inputHandler = GetComponent<PlayerInputHandler>();
            CurrentBattery = MaxBattery;
        }

        public override void OnNetworkSpawn()
        {
            IsOn.OnValueChanged += OnFlashlightStateChanged;
            
            // Sync initial state
            if (_flashlight != null)
            {
                _flashlight.enabled = IsOn.Value;
            }
        }

        public override void OnNetworkDespawn()
        {
            IsOn.OnValueChanged -= OnFlashlightStateChanged;
        }

        private void Update()
        {
            if (IsOwner)
            {
                HandleInput();
            }

            HandleBattery();
        }

        private void HandleInput()
        {
            if (_inputHandler.FlashlightInput)
            {
                _inputHandler.ConsumeFlashlightInput();
                
                // Only allow turning on if we have enough juice
                if (!IsOn.Value && CurrentBattery < MinBatteryToTurnOn)
                {
                    // Maybe play a "click" fail sound here
                    return;
                }

                ToggleFlashlightServerRpc(!IsOn.Value);
            }
        }

        private void HandleBattery()
        {
            // Only the server is authoritative over the state, but we can simulate battery locally for UI
            // Actually, battery is entirely local to the player (doesn't strictly need syncing to others)
            // But if it drains to 0, the server needs to force it off.
            
            if (IsOn.Value)
            {
                CurrentBattery -= DrainRate * Time.deltaTime;
                
                if (CurrentBattery <= 0)
                {
                    CurrentBattery = 0;
                    if (IsOwner)
                    {
                        // Force turn off via RPC
                        ToggleFlashlightServerRpc(false);
                    }
                }
                
                // Flicker effect when battery is low
                if (_flashlight != null && CurrentBattery < MaxBattery * 0.2f)
                {
                    _flashlight.intensity = Mathf.Lerp(0.5f, 1.5f, Mathf.PerlinNoise(Time.time * 10f, 0f));
                }
                else if (_flashlight != null)
                {
                    _flashlight.intensity = 1.5f; // default intensity
                }
            }
            else
            {
                if (CurrentBattery < MaxBattery)
                {
                    CurrentBattery += RechargeRate * Time.deltaTime;
                    if (CurrentBattery > MaxBattery) CurrentBattery = MaxBattery;
                }
            }
        }

        [ServerRpc]
        private void ToggleFlashlightServerRpc(bool turnOn)
        {
            IsOn.Value = turnOn;
        }

        private void OnFlashlightStateChanged(bool previousValue, bool newValue)
        {
            if (_flashlight != null)
            {
                _flashlight.enabled = newValue;
                // Play a click sound effect here
            }
        }
    }
}
