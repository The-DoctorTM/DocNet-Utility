using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using DocNet.Steam;
using DocNet.Utility;
namespace DocNet
{
    public class BootstrapManager : MonoBehaviour
    {
        #region Singleton
        public static BootstrapManager Instance;

        #endregion

        [Header("Default Menu")]
        [SerializeField] protected string defaultSceneToOpen = "MainMenuScene";

        [field: Header("Game Scenes")]
        [field: SerializeField] public string mainMenuScene { get; protected set; }
        [field: SerializeField] public string lobbyScene { get; protected set; }
        [field: SerializeField] public List<string> gameplayScenes { get; protected set; }

        [Header("Lobby Settings")]
        [field: SerializeField] public int minimumPlayers { get; protected set; }

        [field: Header("Transports")]
        [field: SerializeField] public Transport selectedTransport { get; protected set; } = Transport.Facepunch;

        // INFO: Debugging
        protected UnityTransport _unityTransport = null;

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

        protected virtual void Start()
        {
            Application.targetFrameRate = 60; // INFO: Fixes High GPU Usage

            if (selectedTransport == Transport.Unity) { EnableUnityTransport(); return; }
            SteamManager.Instance.EstablishSteamConnection();

        }

        public virtual void GoToDefaultMenu()
        {
            SceneManager.LoadScene(defaultSceneToOpen, LoadSceneMode.Additive);

        }

        #region Debugging
        // DEBUG: Use Unity Transport (For Testing)
        private void EnableUnityTransport()
        {
            if (selectedTransport != Transport.Unity) return;
            NetworkManager.Singleton.GetComponent<FacepunchTransport>().enabled = false;
            Destroy(FindAnyObjectByType<SteamManager>().gameObject);

            #region Create Unity Transport Object
            GameObject transportObject = new GameObject();
            transportObject.AddComponent<UnityTransport>();
            transportObject.name = "Unity Transport [DEBUG]";
            _unityTransport = transportObject.GetComponent<UnityTransport>();
            #endregion


            // INFO: Update the network manager
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = _unityTransport;
            Debug.Log($"<color={LogColours.Debug}>[DEBUG]</color> <color={LogColours.Unity}>[UNITY]</color> Using Unity Transport (Switch transport to use facepunch!)");

            GoToDefaultMenu();

        }

        #endregion

        public enum Transport
        {
            Facepunch,
            Unity

        }

    }
}