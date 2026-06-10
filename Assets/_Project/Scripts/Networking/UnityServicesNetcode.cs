using System;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_SERVICES_ENABLED
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#endif

namespace FarmEmpire.Networking
{
    public class UnityServicesNetcode : MonoBehaviour, INetworkService
    {
        public static UnityServicesNetcode Instance { get; private set; }

        public event Action<string> OnStatusMessage;
        public event Action<bool> OnConnectionStatus;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task<bool> CreateRoomAsync(string roomName, int maxPlayers, bool isPrivate)
        {
#if UNITY_SERVICES_ENABLED
            try
            {
                OnStatusMessage?.Invoke("Initializing Unity Services...");
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    OnStatusMessage?.Invoke("Signing in anonymously...");
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                OnStatusMessage?.Invoke("Allocating Relay Service...");
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                OnStatusMessage?.Invoke("Creating Lobby room...");
                CreateLobbyOptions options = new CreateLobbyOptions
                {
                    IsPrivate = isPrivate,
                    Data = new System.Collections.Generic.Dictionary<string, DataObject>
                    {
                        { "JoinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                    }
                };

                var lobby = await Lobbies.Instance.CreateLobbyAsync(roomName, maxPlayers, options);

                OnStatusMessage?.Invoke($"Lobby Room Created: Code = {joinCode}");

                // Setup Transport and Start Host
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetHostRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );

                NetworkManager.Singleton.StartHost();
                OnConnectionStatus?.Invoke(true);
                return true;
            }
            catch (Exception e)
            {
                OnStatusMessage?.Invoke($"Failed to create room: {e.Message}");
                Debug.LogError(e);
                return false;
            }
#else
            OnStatusMessage?.Invoke("Unity Services not enabled in compiler directives. (Define UNITY_SERVICES_ENABLED in Project Settings -> Player -> Scripting Define Symbols)");
            await Task.Yield();
            return false;
#endif
        }

        public async Task<bool> JoinRoomAsync(string joinCode)
        {
#if UNITY_SERVICES_ENABLED
            try
            {
                OnStatusMessage?.Invoke("Initializing Unity Services...");
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                OnStatusMessage?.Invoke($"Joining Relay with code {joinCode}...");
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetClientRelayData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData
                );

                NetworkManager.Singleton.StartClient();
                OnConnectionStatus?.Invoke(true);
                return true;
            }
            catch (Exception e)
            {
                OnStatusMessage?.Invoke($"Failed to join room: {e.Message}");
                Debug.LogError(e);
                return false;
            }
#else
            OnStatusMessage?.Invoke("Unity Services not enabled in compiler directives. (Define UNITY_SERVICES_ENABLED in Project Settings -> Player -> Scripting Define Symbols)");
            await Task.Yield();
            return false;
#endif
        }

        public async Task LeaveRoomAsync()
        {
#if UNITY_SERVICES_ENABLED
            try
            {
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.Shutdown();
                }
                OnConnectionStatus?.Invoke(false);
                OnStatusMessage?.Invoke("Disconnected from room.");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
#endif
            await Task.Yield();
        }
    }
}
