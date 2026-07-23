// ============================================================================
// Space Maintenance — HeavyFuse.cs
// A massive physics object that slows down a single carrier significantly.
// When two players carry it simultaneously, speed improves.
// If dropped with too much force, it shatters and respawns/is destroyed.
// ============================================================================

using UnityEngine;
using Unity.Netcode;
using SpaceMaintenance.Core;
using SpaceMaintenance.Player;

namespace SpaceMaintenance.ShipSystems
{
    [RequireComponent(typeof(Rigidbody))]
    public class HeavyFuse : NetworkBehaviour, IGrabbable
    {
        public float Weight => 10f; // Extremely heavy
        public Rigidbody Rigidbody { get; private set; }

        public NetworkList<ulong> GrabberClientIds;

        [Header("Effects")]
        [SerializeField] private ParticleSystem _shatterParticles;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _shatterClip;

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            GrabberClientIds = new NetworkList<ulong>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            GrabberClientIds.OnListChanged += OnGrabberListChanged;
        }

        public override void OnNetworkDespawn()
        {
            GrabberClientIds.OnListChanged -= OnGrabberListChanged;
            base.OnNetworkDespawn();
        }

        private void OnGrabberListChanged(NetworkListEvent<ulong> changeEvent)
        {
            // Only care if we are a client and our local player exists
            var localClientId = NetworkManager.Singleton.LocalClientId;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(localClientId, out var localClient) || localClient.PlayerObject == null)
                return;

            var localPlayer = localClient.PlayerObject.GetComponent<PlayerController>();
            if (localPlayer == null) return;

            if (GrabberClientIds.Count == 2)
            {
                if (GrabberClientIds[1] == localClientId)
                {
                    // We are the follower
                    if (NetworkManager.Singleton.ConnectedClients.TryGetValue(GrabberClientIds[0], out var leaderClient) && leaderClient.PlayerObject != null)
                    {
                        var leaderPlayer = leaderClient.PlayerObject.GetComponent<PlayerController>();
                        localPlayer.GluedState.Leader = leaderPlayer;
                        localPlayer.ChangeState(localPlayer.GluedState);
                    }
                }
            }
            else
            {
                // If we are glued but list dropped below 2, exit glued state
                if (localPlayer.GluedState.Leader != null)
                {
                    localPlayer.GluedState.Leader = null;
                    if (localPlayer.GetComponent<PhysicsGrabController>().GrabbedObject == this)
                        localPlayer.ChangeState(localPlayer.CarryingState);
                    else
                        localPlayer.ChangeState(localPlayer.IdleState);
                }
            }
        }

        public bool CanBeGrabbed(GameObject grabber)
        {
            return GrabberClientIds.Count < 2 && !GrabberClientIds.Contains(NetworkManager.Singleton.LocalClientId);
        }

        public void OnGrab(GameObject grabber)
        {
            if (NetworkManager.Singleton.IsClient)
            {
                RequestGrabServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        public void OnRelease(GameObject grabber)
        {
            if (NetworkManager.Singleton.IsClient)
            {
                RequestReleaseServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        public void OnThrow(GameObject grabber, Vector3 force)
        {
            if (NetworkManager.Singleton.IsClient)
            {
                // Can't throw the heavy fuse, it just drops!
                RequestReleaseServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestGrabServerRpc(ulong clientId)
        {
            if (GrabberClientIds.Count < 2 && !GrabberClientIds.Contains(clientId))
            {
                GrabberClientIds.Add(clientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestReleaseServerRpc(ulong clientId)
        {
            if (GrabberClientIds.Contains(clientId))
            {
                GrabberClientIds.Remove(clientId);
            }
        }

        private void Update()
        {
            if (IsServer)
            {
                if (GrabberClientIds.Count > 0)
                {
                    Rigidbody.isKinematic = true;

                    Vector3 avgPos = Vector3.zero;
                    int validGrabbers = 0;

                    for (int i = GrabberClientIds.Count - 1; i >= 0; i--)
                    {
                        ulong clientId = GrabberClientIds[i];
                        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                        {
                            var playerObj = client.PlayerObject;
                            if (playerObj != null)
                            {
                                // Hold point is slightly in front and up from player
                                avgPos += playerObj.transform.position + Vector3.up * 1.5f + playerObj.transform.forward * 1.5f;
                                validGrabbers++;
                            }
                            else GrabberClientIds.RemoveAt(i);
                        }
                        else GrabberClientIds.RemoveAt(i);
                    }

                    if (validGrabbers > 0)
                    {
                        avgPos /= validGrabbers;
                        // Smoothly move the fuse between the players
                        transform.position = Vector3.Lerp(transform.position, avgPos, Time.deltaTime * 10f);

                        // If 2 grabbers, check if they walked too far apart
                        if (validGrabbers == 2)
                        {
                            var p1 = NetworkManager.Singleton.ConnectedClients[GrabberClientIds[0]].PlayerObject;
                            var p2 = NetworkManager.Singleton.ConnectedClients[GrabberClientIds[1]].PlayerObject;
                            if (p1 != null && p2 != null)
                            {
                                float dist = Vector3.Distance(p1.transform.position, p2.transform.position);
                                if (dist > 5f)
                                {
                                    // Too far apart! Force drop.
                                    ForceDropAllClientRpc();
                                    GrabberClientIds.Clear();
                                    Rigidbody.isKinematic = false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Rigidbody.isKinematic = false;
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsServer) return;

            // If it hits the ground/wall hard while NO ONE is holding it
            if (GrabberClientIds.Count == 0 && collision.relativeVelocity.magnitude > 6f)
            {
                BreakFuseClientRpc();
                
                // Despawn after a tiny delay so RPC goes through
                Invoke(nameof(DespawnSelf), 0.1f);
            }
        }

        private void DespawnSelf()
        {
            var netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(true);
            }
        }

        [ClientRpc]
        private void BreakFuseClientRpc()
        {
            if (_shatterParticles != null)
            {
                _shatterParticles.transform.SetParent(null);
                _shatterParticles.Play();
                Destroy(_shatterParticles.gameObject, 3f);
            }
            if (_audioSource != null && _shatterClip != null)
            {
                AudioSource.PlayClipAtPoint(_shatterClip, transform.position);
            }
        }

        [ClientRpc]
        private void ForceDropAllClientRpc()
        {
            var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                var grabCtrl = localPlayer.GetComponent<PhysicsGrabController>();
                if (grabCtrl != null && grabCtrl.GrabbedObject as Object == this)
                {
                    grabCtrl.ForceRelease();
                }
            }
        }
    }
}
