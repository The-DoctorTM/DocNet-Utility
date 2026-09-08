using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using DocNet.Events;
using DocNet.Data;
using TMPro;
using DocNet.Utility;

namespace DocNet.Menus.Game
{
    public class HostGame : MonoBehaviour
    {
        private BootstrapNetworkManager _bootstrapNetworkManager => BootstrapNetworkManager.Instance;

        [Header("Menu Components")]
        [SerializeField] private Button _friendsOnly;

        [Header("Max Players")]
        [SerializeField] private Slider _maxPlayerSlider;
        [SerializeField] private TextMeshProUGUI _playerValueTxt;

        [Header("Max Rounds")]
        [SerializeField] private Slider _numberOfRoundsSlide;
        [SerializeField] private TextMeshProUGUI _roundsValueTxt;

        private void Start()
        {
            if (_playerValueTxt != null) _playerValueTxt.text = $"{_maxPlayerSlider.value}/{_maxPlayerSlider.maxValue}";
            if (_roundsValueTxt != null) _roundsValueTxt.text = $"{_numberOfRoundsSlide.value}/{_numberOfRoundsSlide.maxValue}";

            if (_maxPlayerSlider != null) _maxPlayerSlider.onValueChanged.AddListener(value => _playerValueTxt.text = value.ToString() + $"/{_maxPlayerSlider.maxValue}");
            if (_numberOfRoundsSlide != null) _numberOfRoundsSlide.onValueChanged.AddListener(value => _roundsValueTxt.text = value.ToString() + $"/{_numberOfRoundsSlide.maxValue}");

        }

        public void CreateLobby()
        {
            if (_maxPlayerSlider == null) { Debug.LogError($"Player count slider is null!"); return; }
            if (_numberOfRoundsSlide == null) { Debug.LogError($"Number of rounds slider is null!"); return; }

            LobbyInfo lobbyData = new LobbyInfo();
            lobbyData.maxPlayers = (int)_maxPlayerSlider.value;
            lobbyData.numberOfRounds = (int)_numberOfRoundsSlide.value;

            Debug.Log(lobbyData);

            _bootstrapNetworkManager.SetLobbyData(lobbyData);

            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport)
            {
                Debug.Log($"<color={LogColours.Debug}>[DEBUG]</color> <color={LogColours.Unity}>[UNITY]</color> Bypassing Facepunch transport, starting host!");
                NetworkUtilEventManager.OnSteamHostConnect?.Invoke();
                return;

            }

            NetworkUtilEventManager.OnSteamHostConnect?.Invoke();
            NetworkUtilEventManager.OnCreateLobbyRequest?.Invoke(lobbyData);

        }

    }
}