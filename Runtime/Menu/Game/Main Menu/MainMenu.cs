using UnityEngine;
using Unity.Netcode;
using TMPro;
using DocNet.Menus.Interfaces;
using DocNet.Enums;
using DocNet.Events;
using DocNet.Utility;

namespace DocNet.Menus
{
    public class MainMenu : MonoBehaviour, IMenu
    {
        private BootstrapManager _bootstrapManager => BootstrapManager.Instance;
        private BootstrapNetworkManager _bootstrapNetworkManager => BootstrapNetworkManager.Instance;

        [Header("Sub Menus")]
        [SerializeField] private GameObject _hostGameMenu;
        [SerializeField] private GameObject _joinGameMenu;
        [SerializeField] private GameObject _optionsMenu;
        private MenuState _localCurrentGameSate = MenuState.MainMenu;

        [Header("Debugging")]
        [SerializeField] private TextMeshProUGUI _debugTXT;

        #region Events
        private void OnEnable()
        {
            NetworkUtilEventManager.OnStartUnityHost += LobbyCreated;

        }

        private void OnDisable()
        {
            NetworkUtilEventManager.OnStartUnityHost -= LobbyCreated;
            CloseMenu();


        }
        #endregion

        private void Start()
        {
            if (_hostGameMenu != null) _hostGameMenu.SetActive(false);
            if (_joinGameMenu != null) _joinGameMenu.SetActive(false);
            if (_optionsMenu != null) _optionsMenu.SetActive(false);

        }

        private void Update()
        {
            if (_debugTXT != null && BootstrapNetworkManager.Instance.sessionStateManager)
                _debugTXT.text = $"{BootstrapNetworkManager.Instance.sessionStateManager.currentSessionState.Value}";

        }

        #region Sub Menus
        public void HostGame()
        {
            if (_hostGameMenu == null) { Debug.LogWarning($"Host game menu is null!"); return; }
            HandleMenuSwitching(null, MenuState.HostGame);
            _hostGameMenu.SetActive(true);

        }

        public void JoinGame()
        {
            if (_joinGameMenu == null) { Debug.LogWarning($"Join game menu is null!"); return; }
            _joinGameMenu.SetActive(true);
            _localCurrentGameSate = MenuState.JoinGame;

        }

        public void Options()
        {
            if (_optionsMenu == null) return;

            _optionsMenu.SetActive(true);
            _localCurrentGameSate = MenuState.Options;

        }
        #endregion

        // INFO: Lobby Created, lets go!
        private void LobbyCreated()
        {
            if (!NetworkManager.Singleton.IsServer) return;
            _bootstrapNetworkManager.sessionStateManager.UpdateSessionState(GameState.Lobby);
            BootstrapNetworkManager.Instance.ChangeNetworkScene(_bootstrapManager.lobbyScene, _bootstrapManager.mainMenuScene);

        }

        public void OpenMenu() { ResetMenu(); }
        public void CloseMenu()
        {
            ResetMenu();
        }

        public void ResetMenu()
        {
            _joinGameMenu?.SetActive(false);
            _hostGameMenu?.SetActive(false);
        }

        public void Refresh() => ResetMenu();

        #region Utility
        // INFO: Prevent switching to null UI
        private void HandleMenuSwitching(GameObject menuToDisable, MenuState menuStateToSwitchTo)
        {
            if (menuStateToSwitchTo == _localCurrentGameSate) { Debug.LogWarning($"Already on this state, enabling object!"); }
            if (menuToDisable != null) menuToDisable.SetActive(false);
            _localCurrentGameSate = menuStateToSwitchTo;

        }


        public void BackButton()
        {
            switch (_localCurrentGameSate)
            {
                case MenuState.HostGame:
                    HandleMenuSwitching(_hostGameMenu, MenuState.MainMenu);
                    break;
                case MenuState.Options:
                    HandleMenuSwitching(_optionsMenu, MenuState.MainMenu);
                    break;
                case MenuState.JoinGame:
                    HandleMenuSwitching(_joinGameMenu, MenuState.MainMenu);
                    break;
                default:
                    Debug.LogWarning($"Don't have logic for Game State: {_localCurrentGameSate}");
                    break;

            }

        }
        #endregion

        #region Rage Quitting
        // INFO: Quit Game
        public void QuitGame()
        {
            NetworkUtilEventManager.OnQuitGame?.Invoke(); // INFO: Allow for saving in the future
            Application.Quit();

#if UNITY_EDITOR
            Debug.LogWarning($"Doesn't work in the editor!");
#endif

        }

        // INFO: If the game was closed
        private void OnApplicationQuit()
        {
            NetworkUtilEventManager.OnQuitGame?.Invoke();

        }
        #endregion

        private enum MenuState
        {
            MainMenu,
            HostGame,
            Options,
            JoinGame,
        }

    }
}
