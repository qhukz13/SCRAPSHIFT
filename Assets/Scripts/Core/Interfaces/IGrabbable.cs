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

        /// <summary>Whether this object is currently held by a player.</summary>
        bool IsGrabbed { get; }

        /// <summary>The player currently holding this object (null if not grabbed).</summary>
        GameObject GrabbedBy { get; }

        /// <summary>The Rigidbody of this grabbable object.</summary>
        Rigidbody Rigidbody { get; }

        /// <summary>Called when a player grabs this object.</summary>
        void OnGrab(GameObject grabber);

        /// <summary>Called when the object is released without throwing.</summary>
        void OnRelease();

        /// <summary>Called when the object is thrown with force.</summary>
        void OnThrow(Vector3 force);
    }
}
