using DocNet;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using DocNet.Events;
using System.Threading.Tasks;
using Steamworks;
using DocNet.Steam;
using DocNet.Utility;
using System.Collections;

/// <summary>
/// Handles Unity Netcode side for connecting and disconnecting clients
/// </summary>
namespace DocNet.Unity
{
    public class UnityNetworkManager : MonoBehaviour
    {

        #region Events
        protected virtual void OnEnable()
        {
            // INFO: Host
            NetworkUtilEventManager.OnSteamHostConnect += OnStartUnityHost;
            NetworkUtilEventManager.OnStopUnityHost += OnStopUnityHost;

            // INFO: Client
            NetworkUtilEventManager.OnSteamClientConnect += OnStartUnityClient;
            NetworkUtilEventManager.OnStopUnityClient += StopUnityClient;


        }

        protected virtual void OnDisable()
        {
            // INFO: Host
            NetworkUtilEventManager.OnSteamHostConnect -= OnStartUnityHost;
            NetworkUtilEventManager.OnStopUnityHost -= OnStopUnityHost;

            // INFO: Client
            NetworkUtilEventManager.OnSteamClientConnect -= OnStartUnityClient;
            NetworkUtilEventManager.OnStopUnityClient -= StopUnityClient;



        }
        #endregion

        #region Unity Client
        // INFO: Start Client Connection 
        protected virtual async void OnStartUnityClient()
        {
            try
            {
                NetworkManager.Singleton.StartClient();
                NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
                NetworkManager.Singleton.OnClientStopped += OnClientStopped;

                Debug.Log($"{CheckPrivilege()} Client has started");
                await SceneManager.UnloadSceneAsync(BootstrapManager.Instance.mainMenuScene);


            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{ex}");
            }

        }

        // INFO: Stop Client Connection
        protected virtual void StopUnityClient()
        {
            string privilege = NetworkManager.Singleton.IsServer ? "host" : "client";
            string color = NetworkManager.Singleton.IsServer ? LogColours.Host : LogColours.Client;

            Debug.Log($"<color={LogColours.Unity}>[UNITY]</color> <color={color}>[{privilege.ToUpper()}]</color> Shutting down {privilege}...");

            try
            {
                NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
                NetworkManager.Singleton.Shutdown();

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Shutdown error: {ex.Message}");

            }
        }

        private void OnClientStopped(bool isHost)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Debug.Log($"<color={LogColours.Lobby}>[LOBBY]</color> You left the lobby!");

        }

        #region Connection Events
        private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            Debug.Log($"{connectionEventData.EventType}");
            switch (connectionEventData.EventType)
            {
                case ConnectionEvent.ClientConnected:
                    OnClientConnected(networkManager, connectionEventData);
                    break;

            }
        }

        protected virtual void OnClientConnected(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            if (connectionEventData.EventType != ConnectionEvent.ClientConnected) return;
            if (!networkManager.IsServer) return;

            Debug.Log($"<color={LogColours.Lobby}>[LOBBY]</color> {connectionEventData.ClientId} has joined!");

        }

        #endregion

        #endregion

        #region Unity Host
        // INFO: Start host connection
        private void OnStartUnityHost()
        {
            StartCoroutine(StartUnityHostCoroutine());

        }

        private IEnumerator StartUnityHostCoroutine()
        {
            yield return StartUnityHostAsync();
        }


        protected virtual async Task StartUnityHostAsync()
        {
            try
            {

                NetworkManager.Singleton.StartHost();
                NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
                NetworkManager.Singleton.OnClientStopped += OnClientStopped;

                // INFO: Configure Network Manager
                NetworkManager.Singleton.SceneManager.ActiveSceneSynchronizationEnabled = true;
                NetworkManager.Singleton.SceneManager.PostSynchronizationSceneUnloading = true;

                if (SteamManager.Instance.connectedToSteam) await Task.Delay(500);
                Debug.Log($"{CheckPrivilege()} Unity Host has started");

                NetworkUtilEventManager.OnStartUnityHost?.Invoke(); // INFO: Host started let other scripts know

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{ex}");

            }

        }

        protected virtual void OnStopUnityHost()
        {
            if (!NetworkManager.Singleton.IsServer) return;
            StopUnityClient(); // INFO: Stop host client

        }

        #endregion

        #region Utility
        public static string CheckPrivilege()
        {
            if (!NetworkManager.Singleton.IsListening) return $"<color={LogColours.Unity}>[UNITY]</color>";

            switch (NetworkManager.Singleton.IsHost)
            {
                case true:
                    return $"<color={LogColours.Unity}>[UNITY]</color> <color={LogColours.Host}>[HOST]</color>";
                case false:
                    return $"<color={LogColours.Unity}>[UNITY]</color> <color={LogColours.Client}>[CLIENT]</color>";

            }
        }
        #endregion

    }

}
