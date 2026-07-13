using Unity.Netcode;
using UnityEngine;

namespace SpaceMaintenance.Core
{
    public class ProgressionManager : NetworkBehaviour
    {
        public static ProgressionManager Instance { get; private set; }

        public NetworkVariable<bool> HasProFlashlight = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> HasAdrenaline = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> HasWrenchUpgrade = new NetworkVariable<bool>(false);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void UnlockUpgrade(string upgradeId)
        {
            if (!IsServer) return;

            switch (upgradeId)
            {
                case "ProFlashlight":
                    HasProFlashlight.Value = true;
                    Debug.Log("[Progression] Unlocked Pro Flashlight!");
                    break;
                case "Adrenaline":
                    HasAdrenaline.Value = true;
                    Debug.Log("[Progression] Unlocked Adrenaline Injector!");
                    break;
                case "WrenchUpgrade":
                    HasWrenchUpgrade.Value = true;
                    Debug.Log("[Progression] Unlocked Wrench Upgrade!");
                    break;
            }
        }
    }
}
