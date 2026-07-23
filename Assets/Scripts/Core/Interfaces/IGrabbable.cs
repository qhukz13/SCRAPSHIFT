// ============================================================================
// Space Maintenance — IGrabbable.cs
// Core interface for physics-based grabbable objects.
// ============================================================================

using UnityEngine;

namespace SpaceMaintenance.Core
{
    /// <summary>
    /// Implement on any object that can be picked up and thrown
    /// using the physics grab system. Interacts with Rigidbody.
    /// </summary>
    public interface IGrabbable
    {
        /// <summary>Weight affects carry speed and throw distance.</summary>
        float Weight { get; }

        /// <summary>Checks if this object can be grabbed by the specified player.</summary>
        bool CanBeGrabbed(GameObject grabber);

        /// <summary>The Rigidbody of this grabbable object.</summary>
        Rigidbody Rigidbody { get; }

        /// <summary>Called when a player grabs this object.</summary>
        void OnGrab(GameObject grabber);

        /// <summary>Called when a specific grabber releases the object without throwing.</summary>
        void OnRelease(GameObject grabber);

        /// <summary>Called when a specific grabber throws the object with force.</summary>
        void OnThrow(GameObject grabber, Vector3 force);
    }
}
