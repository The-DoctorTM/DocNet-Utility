using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using DocNet.Events;
using DocNet.Data;
using Unity.Scripting.LifecycleManagement;
using DocNet.Utility;

namespace DocNet.Steam
{

    public partial class SteamManager : NetworkBehaviour
    {

        #region Singleton
        public static SteamManager Instance;

        #endregion

        [field: Header("Steam Settings")]
        [field: SerializeField] public uint appID { get; protected set; } = 480;

        public bool connectedToSteam => SteamClient.IsValid;

        [AutoStaticsCleanup]
        public static Lobby? myLobby { get; protected set; }


        [Header("Events")]
        public UnityEvent EvtSteamInitialised = new UnityEvent();
        public UnityEvent EvtSteamInitialisedError = new UnityEvent();

        protected FacepunchTransport _facepunchTransport => NetworkManager.Singleton.GetComponent<FacepunchTransport>();

        private void Awake()
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

        private void OnNetworkClientConnected(ulong clientId)
        {
            AskServerToRegisterRPC(SteamClient.SteamId, clientId);

        }

        #region Events
        private void OnEnable()
        {
            SubscribeToEvents();


        }

        private void OnDisable()
        {
            UnSubscribeToEvents();


        }


        private void SubscribeToEvents()
        {
            #region Host
            // INFO: Host
            NetworkUtilEventManager.OnCreateLobbyRequest += StartSteamServer;
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
            SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;
            SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
            SteamFriends.OnGameRichPresenceJoinRequested += OnGameRichPresenceJoinRequested;

            #endregion

            #region Client
            // INFO: Client
            NetworkUtilEventManager.OnSteamClientDisconnect += OnSteamClientLeave;
            SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
            NetworkManager.OnClientConnectedCallback += OnNetworkClientConnected;

            #endregion

        }

        private void UnSubscribeToEvents()
        {
            #region Host
            // INFO: Host
            NetworkUtilEventManager.OnCreateLobbyRequest -= StartSteamServer;
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
            SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberDisconnected;
            SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
            SteamFriends.OnGameRichPresenceJoinRequested -= OnGameRichPresenceJoinRequested;

            #endregion

            #region Client
            // INFO: Client
            SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
            NetworkUtilEventManager.OnSteamClientDisconnect -= OnSteamClientLeave;

            if (NetworkManager == null) return;
            NetworkManager.OnClientConnectedCallback -= OnNetworkClientConnected;
            #endregion

        }
        #endregion

        #region Steam Connection
        #region Establish Connection
        // INFO: Establish connection to steam servers
        public virtual bool EstablishSteamConnection()
        {
            // if (connectedToSteam) { Debug.Log($"Attempted to initialise Steam but already connected?"); return false; }

            try
            {
                SteamClient.Init(appID);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{ex}");
                EvtSteamInitialisedError?.Invoke();

                return false;
            }

            Debug.Log($"<color={LogColours.Steamworks}>[STEAM]</color> Successfully Connected to steam! | {SteamClient.Name} ({SteamClient.AppId})</color>");
            _facepunchTransport.steamAppId = appID;
            EvtSteamInitialised?.Invoke();

            return true;

        }
        #endregion

        #region Terminate Connection
        // INFO: Disconnect from steam
        protected virtual void TerminateSteamConnection()
        {
            if (!connectedToSteam) return;

            try
            {
                SteamClient.Shutdown();

            }
            catch (System.Exception e)
            {
                if (connectedToSteam) Debug.LogError($"{e.Message}");

            }

            if (!connectedToSteam) Debug.Log($"<color={LogColours.Steamworks}>[STEAM]</color> Connection terminated successfully!");
            SteamFriends.SetRichPresence("connect", null);

        }

        // INFO: Ensure correct termination
        protected void OnApplicationQuit()
        {
            OnSteamClientLeave();
            TerminateSteamConnection();

        }

        #endregion

        protected void VerifySteamConnection()
        {
            if (connectedToSteam) return;

            // INFO: Not Connected
            Debug.LogWarning($"Not connected to steam, disabling {name}");
            gameObject.SetActive(false);

        }
        #endregion

        #region Lobbies

        // INFO: DO NOT EDIT!
        #region Steamworks
        #region Create Server
        private async void StartSteamServer(LobbyInfo lobbyData)
        {
            if (!connectedToSteam) EstablishSteamConnection();
            Debug.Log($"<color={LogColours.Steamworks}>[STEAM]</color> Lobby request received creating lobby!");
            Lobby? lobby = await SteamMatchmaking.CreateLobbyAsync(lobbyData.maxPlayers);
            lobby.Value.SetGameServer(lobby.Value.Owner.Id);
            lobby.Value.SetJoinable(true);

            if (lobbyData.friendsOnly)
            {
                lobby.Value.SetFriendsOnly();

            }
            else
            {
                lobby.Value.SetPrivate();
            }


        }

        private void OnLobbyCreated(Result result, Lobby lobby)
        {
            if (result != Result.OK) { Debug.LogError($"Lobby failed to create!"); return; }

            Debug.Log($"<color={LogColours.Steamworks}>[STEAM]</color> Lobby created! | {lobby.Owner.Name} ({lobby.Id}) | {lobby.MemberCount}/{lobby.MaxMembers}");
            GUIUtility.systemCopyBuffer = lobby.Id.ToString(); // INFO: Copies lobby code to peoples keyboard
            myLobby = lobby;

        }

        #endregion

        #region Invite Player
        private void OnLobbyInvite(Friend friend, Lobby lobby)
        {
            Debug.Log($"{friend.Name} was invited to {lobby.Id}");

        }

        private async void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
        {
            if (myLobby.HasValue) { Debug.LogWarning("Already in a lobby"); return; }

            RoomEnter joinedLobby = await lobby.Join();
            if (joinedLobby != RoomEnter.Success) { Debug.LogError($"Failed to join {lobby}"); return; }

        }

        private async void OnGameRichPresenceJoinRequested(Friend friend, string s)
        {
            if (myLobby.HasValue) { Debug.LogWarning("Already in a lobby"); return; }

            if (!ulong.TryParse(s, out ulong seshID)) return;
            Lobby? joinedLobby = await SteamMatchmaking.JoinLobbyAsync(seshID);
            if (joinedLobby == null) { Debug.LogError($"Failed to join lobby!"); return; }


        }
        #endregion

        #region Player Joining/Joined
        private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} is joining!");

        }


        private void OnLobbyEntered(Lobby lobby)
        {
            if (SteamClient.SteamId == lobby.Owner.Id)
            {
                OnSteamHostEntered();

            }
            else
            {
                OnSteamClientEntered(lobby);
                Debug.Log($"<color={LogColours.Steamworks}>[STEAM]</color> <color={LogColours.Client}>[CLIENT]</color> You entered {lobby.Owner.Name}'s lobby!");

            }
        }

        #endregion

        #region Player Left/Disconnected
        private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} left!");

        }

        private void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
        {
            Debug.Log($"{friend.Name} disconnected!");

        }
        #endregion
        #endregion

        #region Host
        protected virtual void OnSteamHostEntered()
        {
            Debug.Log($"{CheckPrivilege()} Oh herro mister Host!");
            SteamFriends.SetRichPresence("connect", myLobby.Value.Id.ToString());

        }

        protected virtual void OnSteamHostLeave()
        {
            Debug.Log($"{CheckPrivilege()} Goodbye mister Host!");
            NetworkUtilEventManager.OnStopUnityHost?.Invoke();

        }

        [Rpc(SendTo.Server)]
        private void AskServerToRegisterRPC(ulong steamId, ulong clientId)
        {
            BootstrapNetworkManager.Instance.RegisterPlayer(clientId, steamId); // INFO: Register Player
        }

        #endregion

        #region Client
        protected virtual void OnSteamClientEntered(Lobby lobby)
        {
            // INFO: Client
            myLobby = lobby;

            // INFO: Client
            _facepunchTransport.targetSteamId = lobby.Owner.Id;

            if (!NetworkManager.IsListening) NetworkUtilEventManager.OnSteamClientConnect?.Invoke();

        }

        protected virtual void OnSteamClientLeave()
        {
            if (!connectedToSteam) return;
            if (_facepunchTransport != null) _facepunchTransport.targetSteamId = 0;

            // INFO: Leave the lobby
            SteamFriends.SetRichPresence("connect", null);
            myLobby?.Leave();

            if (SteamClient.SteamId == myLobby.Value.Owner.Id)
            {
                OnSteamHostLeave();
            }
            else
            {
                NetworkUtilEventManager.OnStopUnityClient?.Invoke();

            }

            myLobby = null;

        }

        #endregion

        #region Utility
        protected virtual string CheckPrivilege()
        {
            if (!connectedToSteam) return $"<color={LogColours.Steamworks}>[STEAM]</color>";

            switch (NetworkManager.Singleton.IsHost)
            {
                case true:
                    return $"<color={LogColours.Steamworks}>[STEAM]</color> <color={LogColours.Host}>[HOST]</color>";
                case false:
                    return $"<color={LogColours.Steamworks}>[STEAM]</color> <color={LogColours.Client}>[CLIENT]</color>";
            }
        }

        private bool AreAllPlayersReady()
        {
            // int lobbyMemberCount = SteamMatchmaking.;

            // for (int i = 0; i < lobbyMemberCount; i++)
            // {
            //     CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
            //     string readyState = SteamMatchmaking.GetLobbyMemberData(lobbyID, memberID, "ready");

            //     if (readyState != "true")
            //         return false;
            // }

            return true;
        }
        #endregion
        #endregion


    }
}