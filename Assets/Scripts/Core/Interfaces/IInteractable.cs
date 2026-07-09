// ============================================================================
// Space Maintenance — IInteractable.cs
// Core interface for objects the player can interact with.
// ============================================================================

using UnityEngine;

namespace SpaceMaintenance.Core
{
    /// <summary>
    /// Implement on any object the player can interact with:
    /// doors, repair stations, buttons, items, etc.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>UI prompt shown when player looks at this object.</summary>
        string InteractionPrompt { get; }

        /// <summary>Whether this object supports hold-to-interact.</summary>
        bool RequiresHold { get; }

        /// <summary>Duration of hold in seconds (only if RequiresHold is true).</summary>
        float HoldDuration { get; }

        /// <summary>Check if the given player can interact right now.</summary>
        bool CanInteract(GameObject player);

        /// <summary>Called on single press interaction.</summary>
        void OnInteract(GameObject player);

        /// <summary>Called each frame while the player holds the interact button.</summary>
        void OnInteractHold(GameObject player, float holdTime);

        /// <summary>Called when the player releases the interact button.</summary>
        void OnInteractRelease(GameObject player);
    }
}
