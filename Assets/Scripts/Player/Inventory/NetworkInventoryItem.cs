using System;
using Unity.Collections;
using Unity.Netcode;

namespace SpaceMaintenance.Player.Inventory
{
    public struct NetworkInventoryItem : INetworkSerializable, IEquatable<NetworkInventoryItem>
    {
        public FixedString32Bytes ItemID;
        public int Amount;

        public NetworkInventoryItem(string id, int amount = 1)
        {
            ItemID = new FixedString32Bytes(id);
            Amount = amount;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ItemID);
            serializer.SerializeValue(ref Amount);
        }

        public bool Equals(NetworkInventoryItem other)
        {
            return ItemID == other.ItemID && Amount == other.Amount;
        }
    }
}
