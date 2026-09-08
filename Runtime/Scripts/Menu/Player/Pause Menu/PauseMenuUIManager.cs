using UnityEngine;
using DocNet;
using DocNet.Events;
using DocNet.Steam;
using DocNet.Utility;
using Unity.Netcode;

namespace DocNet.Menus.Player
{
    public class PauseMenuUIManager : MonoBehaviour
    {

        [SerializeField] private GameObject _optionsMenu;

        #region Events
        private void OnEnable()
        {

        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }
        #endregion

        public void ResumeGame()
        {
            // if (NetworkManager.Singleton.IsServer) gameObject.transform.root.GetComponent<PlayerNetworkedController>().AskTogglePauseRPC(); // TODO: Make this an event?
            // if (gameObject.transform.root.GetComponent<PlayerNetworkedController>().hostForcedPause) return;
            gameObject.SetActive(false);

        }

        public void Options()
        {
            // GUARD: Prevent nulls
            if (_optionsMenu == null) { Debug.LogWarning($"Options menu is null"); return; }
            _optionsMenu?.SetActive(true);

        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1;

            if (SteamManager.Instance.connectedToSteam) { NetworkUtilEventManager.OnSteamClientDisconnect?.Invoke(); return; }
            // !! Unity handling
            if (BootstrapManager.Instance.selectedTransport == BootstrapManager.Transport.Unity) NetworkUtilEventManager.OnStopUnityClient?.Invoke();

        }

        public void QuitGame()
        {
            if (SteamManager.Instance.connectedToSteam) NetworkUtilEventManager.OnSteamClientDisconnect?.Invoke();

            // !! Unity handling
            if (BootstrapManager.Instance.selectedTransport == BootstrapManager.Transport.Unity) NetworkUtilEventManager.OnStopUnityClient?.Invoke();

#if UNITY_EDITOR
            Debug.Log($"<color={LogColours.Unity}>[UNITY]</color> Sike this is the editor!</color>");
            ReturnToMainMenu();

#endif
            Application.Quit();

        }


    }
}