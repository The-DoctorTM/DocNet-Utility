using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using DocNet.Events;
using DocNet.Data;
using DocNet.Session;
using Steamworks;
using DocNet.Steam;
using DocNet.Utility;

namespace DocNet
{
    public partial class BootstrapNetworkManager : NetworkBehaviour
    {
        #region Singleton
        public static BootstrapNetworkManager Instance;
        #endregion

        #region Networking
        public SessionStateManager sessionStateManager => SessionStateManager.Instance;
        #endregion

        public LobbyInfo lobbyData { get; protected set; }

        [field: SerializeField, DictionaryDisplay(keyLabel = "Client ID", valueLabel = "Steam ID")] public Dictionary<ulong, ulong> connectedPlayers { get; protected set; } = new();

        protected virtual void Awake()
        {
            #region Singleton
            if (Instance == null)
            {
                Instance = this;

            }
            else
            {
                Destroy(gameObject);

            }

            #endregion
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) enabled = false;

        }

        #region Change Scene

        #region SERVER
        public virtual void ChangeNetworkScene(string sceneToLoad, string sceneToClose)
        {
            List<string> sceneList = new List<string> { sceneToClose };
            ChangeNetworkScene(sceneToLoad, sceneList);

        }

        public virtual void ChangeNetworkScene(string sceneToLoad, List<string> scenesToClose)
        {
            if (!IsSceneInBuildSettings(sceneToLoad) || string.IsNullOrEmpty(sceneToLoad))
            {
                Debug.LogError($"<color={LogColours.Error}>[ERROR]</color> Scene {sceneToLoad} is invalid or not in build settings, cannot load!");
                return;

            }

            foreach (string sceneName in scenesToClose)
            {
                if (string.IsNullOrEmpty(sceneName)) continue;
                Instance.CloseSceneObserverRPC(sceneName);

            }

            SceneEventProgressStatus sceneLoadStatus = NetworkManager.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
            if (sceneLoadStatus == SceneEventProgressStatus.Started)
            {
                Debug.Log($"<color={LogColours.Unity}>[NETWORK]</color> Scene transition complete: {sceneToLoad}");
            }
            else
            {
                Debug.LogError($"<color={LogColours.Error}>[ERROR]</color> Scene failed to load! {sceneToLoad}!");
            }
        }
        #endregion

        #region CLIENT 

        [Rpc(SendTo.ClientsAndHost)]
        protected virtual void CloseSceneObserverRPC(string scenesToClose)
        {
            SceneManager.UnloadSceneAsync(scenesToClose);

        }
        #endregion

        #endregion

        #region Return To Lobby
        [Rpc(SendTo.Server)]
        public virtual void ReturnToLobbyRPC()
        {
            ChangeNetworkScene(BootstrapManager.Instance.lobbyScene, BootstrapManager.Instance.gameplayScenes);

        }

        #endregion

        #region Util
        public void ForEachPlayer(Action<NetworkObject> forPlayer, bool includeHost = true)
        {
            foreach (NetworkClient netObj in NetworkManager.ConnectedClients.Values)
            {
                NetworkObject playerObject = netObj.PlayerObject;
                if (playerObject == null) continue;

                bool isHost = netObj.ClientId == NetworkManager.LocalClientId;

                // GUARD: Skip host if not included
                if (isHost && !includeHost) continue;

                forPlayer?.Invoke(playerObject);
            }
        }

        public void SetLobbyData(LobbyInfo newLobbyData) => lobbyData = newLobbyData;

        private bool IsSceneInBuildSettings(string sceneName)
        {
            bool isValid = Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
                .Any(i => System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)) == sceneName);

            return isValid;
        }

        #endregion

        #region Player Tracking
        public virtual void RegisterPlayer(ulong clientId, ulong steamId = default)
        {
            if (connectedPlayers.ContainsKey(clientId))
            {
                ulong previousSteamId = connectedPlayers[clientId];
                connectedPlayers[clientId] = steamId;
                Debug.Log($"<color={LogColours.Unity}>[UNITY]</color>{clientId} edited {previousSteamId} -> {steamId}");
                return;

            }

            connectedPlayers.Add(clientId, steamId);
            Debug.Log($"<color={LogColours.Unity}>[UNITY]</color> {clientId} ({steamId}) added");


        }

        public virtual void RemovePlayer(ulong clientId)
        {
            if (!connectedPlayers.ContainsKey(clientId)) { Debug.LogWarning($"Player: {clientId} is not in the list"); return; }
            connectedPlayers.Remove(clientId);

            Debug.Log($"<color={LogColours.Unity}>[UNITY]</color> {clientId} removed");

        }


        public Friend GetPlayerSteamClient(ulong clientId)
        {
            if (!SteamManager.Instance.connectedToSteam) return default;

            List<Friend> lobbyMembers = SteamManager.myLobby.Value.Members.ToList();
            for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++)
            {
                if (lobbyMembers[i].Id == connectedPlayers[clientId])
                    return lobbyMembers[i];

            }

            return default;

        }

        public ulong GetPlayerUnityId(ulong steamId)
        {
            foreach (var player in connectedPlayers)
            {
                ulong playerId = player.Key;
                ulong playerSteamId = player.Value;

                if (playerSteamId == steamId) return playerId;

            }

            return 69;

        }
        #endregion

    }
}