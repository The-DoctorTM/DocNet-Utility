using DocNet.Menus.Interfaces;
using Steamworks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using DocNet.Events;
using DocNet.Unity;
using DocNet.Utility;

namespace DocNet.Menus.Game
{
    public class JoinGameMenu : MonoBehaviour, IMenu
    {

        [SerializeField] private TMP_InputField _joinCodeInputField;
        [SerializeField] private TextMeshProUGUI _errorTXT;

        #region Events
        private void OnEnable()
        {

        }

        private void OnDisable()
        {
            CloseMenu();

        }
        #endregion

        private void Start()
        {
            _errorTXT?.gameObject.SetActive(false);

        }

        public void OpenMenu() { ResetMenu(); }
        public void CloseMenu()
        {
            ResetMenu();
        }

        public void ResetMenu()
        {
            _joinCodeInputField.text = "";
            _errorTXT?.gameObject.SetActive(false);

        }

        public void Refresh() { }

        public async void JoinGame()
        {
            if (_joinCodeInputField == null) { Debug.LogError($"Input field null!"); return; }

            // DEBUG: For Testing
            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport)
            {
                Debug.Log($"<color={LogColours.Debug}>[DEBUG]</color> <color={LogColours.Unity}>[UNITY]</color> Bypassing Facepunch transport, starting client!");
                string sceneName = SceneManager.GetActiveScene().name;
                NetworkUtilEventManager.OnSteamClientConnect?.Invoke();
                return;

            }

            if (!ulong.TryParse(_joinCodeInputField.text, out ulong lobbyId))
            {
                Debug.LogWarning($"{UnityNetworkManager.CheckPrivilege()} Invalid join code: {_joinCodeInputField.text}");
                _joinCodeInputField.text = "";
                _errorTXT?.gameObject.SetActive(true);
                return;

            }

            _errorTXT?.gameObject.SetActive(false);

            NetworkUtilEventManager.OnSteamClientConnect?.Invoke();
            Steamworks.Data.Lobby? lobby = await SteamMatchmaking.JoinLobbyAsync(lobbyId);

        }
    }
}