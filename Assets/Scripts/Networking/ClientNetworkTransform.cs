using Unity.Netcode.Components;
using UnityEngine;

namespace SpaceMaintenance.Networking
{
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
