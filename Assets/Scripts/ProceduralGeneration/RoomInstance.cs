using UnityEngine;

namespace ProceduralGeneration
{
    /// <summary>
    /// Runtime component attached to instantiated rooms to hold their dynamic state.
    /// </summary>
    public class RoomInstance : MonoBehaviour
    {
        [Header("Runtime Identity")]
        public RoomDefinition Definition { get; private set; }
        public int Floor { get; private set; }
        public RoomTags CurrentTags { get; private set; }

        public void Initialize(RoomDefinition definition, int floor)
        {
            Definition = definition;
            Floor = floor;
            CurrentTags = definition.RoomTags;
        }

        public bool HasTag(RoomTags tag)
        {
            return (CurrentTags & tag) == tag;
        }

        public void AddTag(RoomTags tag)
        {
            CurrentTags |= tag;
        }

        public void RemoveTag(RoomTags tag)
        {
            CurrentTags &= ~tag;
        }
    }
}
