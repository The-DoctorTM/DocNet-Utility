using UnityEngine;
using Steamworks.Data;
using Steamworks;
using UnityEngine.UI;
using TMPro;
using DocNet.Menus.Interfaces;
using Unity.Netcode;
using DocNet.Events;
using DocNet.Steam;
using DocNet.Data;
using System.Threading.Tasks;
using DocNet.Utility;

namespace DocNet.Menus.Game
{
    public class LobbyUIManager : NetworkBehaviour, IMenu
    {

        [Header("Player Panel")]
        [SerializeField] protected GameObject _playerPanelContentGO;
        [SerializeField] protected GameObject _playerInfoPanelPrefab;

        [Header("Buttons")]
        [SerializeField] protected Button _startGameBtn;
        [SerializeField] protected Button _leaveGameBtn;

        [Header("Lobby Info Text")]
        [SerializeField] private TextMeshProUGUI _roundsTxt;
        [SerializeField] protected TextMeshProUGUI _lobbyCodeTxt;

        #region Events
        protected virtual void OnEnable()
        {
            NetworkManager.Singleton.OnConnectionEvent += OnUnityClientDisconnect;

        }

        protected virtual void OnDisable()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.OnConnectionEvent -= OnUnityClientDisconnect;

        }
        #endregion

        public override void OnNetworkSpawn()
        {
            if (_startGameBtn != null)
            {
                _startGameBtn.interactable = NetworkManager.Singleton.IsServer ? true : false;
                if (NetworkManager.Singleton.IsServer) _startGameBtn.onClick.AddListener(StartGame);

            }

            if (_leaveGameBtn != null) _leaveGameBtn.onClick.AddListener(LeaveGame);
            if (IsServer) Refresh();

        }


        #region IMenu Components
        public void OpenMenu() { ResetMenu(); }
        public void CloseMenu()
        {
            ResetMenu();
        }

        public void ResetMenu()
        {
            _lobbyCodeTxt.text = "";
            ClearPlayerPanel();

        }
        public void Refresh()
        {
            RefreshUI();
        }
        #endregion

        #region Networking
        #region Get Lobby Info
        [Rpc(SendTo.Server)]
        protected virtual void AskForLobbyInfoRPC()
        {
            string code = "Code: ";
            string lobby = SteamManager.myLobby.HasValue ? SteamManager.myLobby.Value.Id.ToString() : "UNITY (NO CODE)";
            string lobbyCode = code + lobby;
            string numberOfRounds = $"ROUND 1 OF {BootstrapNetworkManager.Instance.lobbyData.numberOfRounds}";

            TellLobbyInfoRPC(lobbyCode, numberOfRounds);

        }

        [Rpc(SendTo.ClientsAndHost)]
        // INFO: Tell the client the lobby info
        protected virtual void TellLobbyInfoRPC(string lobbyCode, string numberOfRounds)
        {
            // GUARD: Prevent unnecessary refresh
            if (_lobbyCodeTxt.text == lobbyCode || _roundsTxt.text == numberOfRounds) return;

            if (_lobbyCodeTxt != null) _lobbyCodeTxt.text = lobbyCode;
            if (_roundsTxt != null) _roundsTxt.text = numberOfRounds;

        }

        #endregion

        #region Get Player List
        [Rpc(SendTo.Server)]
        protected virtual void AskForPlayerListRPC()
        {
            ClearPlayerPanel();

            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                bool isHost = clientId == 0;
                string displayName = SteamManager.Instance.connectedToSteam ? BootstrapNetworkManager.Instance.GetPlayerSteamClient(clientId).Name : clientId.ToString();
                TellPlayerListRPC(displayName, isHost);

            }

        }

        [Rpc(SendTo.ClientsAndHost)]
        protected virtual void TellPlayerListRPC(string playerName, bool isHost)
        {
            // INFO: Display all connected members
            CreatePlayerCard($"{playerName}", isHost, $"{-1}ms");


        }
        #endregion

        protected virtual async void OnUnityClientDisconnect(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {

            if (connectionEventData.EventType == ConnectionEvent.PeerConnected)
            {
                if (networkManager.IsServer)
                {
                    await Task.Delay(500); // INFO: Allow time to info to update
                    Refresh();

                }

                return;

            }

            if (connectionEventData.EventType != ConnectionEvent.ClientDisconnected) return;
            if (connectionEventData.ClientId == networkManager.LocalClientId)
                return;

            if (!IsServer) return;
            Debug.Log($"<color={LogColours.Lobby}>[LOBBY]</color> {connectionEventData.ClientId} has left!");
            Refresh();

        }

        #endregion

        // INFO: Client
        protected virtual PlayerUIInfo CreatePlayerCard(string playerName, bool host, string playerPing)
        {
            GameObject playerInfoGO = Instantiate(_playerInfoPanelPrefab);
            playerInfoGO.transform.SetParent(_playerPanelContentGO.transform);

            playerInfoGO.transform.localPosition = Vector3.one;
            playerInfoGO.transform.localScale = Vector3.one;

            // INFO: Set Display
            PlayerUIInfo playerInfo = playerInfoGO.GetComponent<PlayerUIInfo>();
            playerInfo.playerName = playerInfo.playerName = $"{playerName} {(host ? "[HOST]" : "")}";
            playerInfo.playerPing = playerPing;

            return playerInfo;

        }

        #region Buttons
        protected virtual void StartGame()
        {
            BootstrapManager bootstrapManager = BootstrapManager.Instance;
            if (NetworkManager.Singleton.ConnectedClients.Count < bootstrapManager.minimumPlayers && bootstrapManager.selectedTransport == BootstrapManager.Transport.Facepunch) { Debug.LogWarning($"Need {bootstrapManager.minimumPlayers} players to start"); return; }
            BootstrapNetworkManager.Instance.ChangeNetworkScene(bootstrapManager.gameplayScenes[0], bootstrapManager.lobbyScene);

        }

        protected virtual void LeaveGame()
        {
            if (SteamManager.Instance.connectedToSteam)
            {
                NetworkUtilEventManager.OnSteamClientDisconnect?.Invoke();
                return;
            }

            // !! Unity handling
            NetworkUtilEventManager.OnStopUnityClient?.Invoke();

        }

        #endregion

        #region Utility
        protected virtual void ClearPlayerPanel()
        {
            foreach (Transform child in _playerPanelContentGO.transform)
                Destroy(child.gameObject);

        }

        protected virtual void RefreshUI()
        {
            // GUARD: Prevent Nulls
            if (_playerPanelContentGO == null) { Debug.LogError($"Player panel content is null!"); return; }
            if (_playerInfoPanelPrefab == null) { Debug.LogError($"Player info panel is null, cannot display player"); return; }

            AskForLobbyInfoRPC();
            AskForPlayerListRPC();

        }
        #endregion

    }
}