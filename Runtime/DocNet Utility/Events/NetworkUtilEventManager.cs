using UnityEngine;
using DocNet.Data;
using Steamworks.Data;

namespace DocNet.Events
{
    public static class NetworkUtilEventManager
    {
        public delegate void NoArgs();
        public delegate void OneArg<T1>(T1 t1);
        public delegate void TwoArgs<T1, T2>(T1 t1, T2 t2);
        public delegate void ThreeArgs<T1, T2, T3>(T1 t1, T2 t2, T3 t3);
        public delegate void FourArgs<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4);

        #region Main Menu Events
        public static OneArg<LobbyInfo> OnCreateLobbyRequest;
        public static NoArgs OnQuitGame;
        #endregion

        #region Network Events

        #region Steam
        public static NoArgs OnConnectedToSteam;
        public static NoArgs OnSteamHostConnect;
        public static NoArgs OnSteamHostDisconnect;
        public static NoArgs OnSteamClientConnect;
        public static NoArgs OnSteamClientDisconnect;
        public static OneArg<Lobby> OnSteamLobbyCreated;
        #endregion

        #region Unity
        #region Host
        public static NoArgs OnStartUnityHost;
        public static NoArgs OnStopUnityHost;
        #endregion

        #region Client
        // public static NoArgs OnStartUnityClient;
        public static NoArgs OnStopUnityClient;
        public static NoArgs OnUnityClientStarted;

        #endregion

        #endregion

        #endregion

    }
}