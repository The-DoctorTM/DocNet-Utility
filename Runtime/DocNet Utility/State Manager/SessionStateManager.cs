using System;
using Unity.Netcode;
using UnityEngine;
using DocNet.Enums;
using DocNet.Unity;

namespace DocNet.Session
{
    public class SessionStateManager : NetworkBehaviour
    {
        #region Singleton
        public static SessionStateManager Instance;
        #endregion

        [field: SerializeField]
        public NetworkVariable<GameState> currentSessionState { get; private set; } = new NetworkVariable<GameState>
        (
            default,

NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server

        );

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

        #region Events
        private void OnEnable()
        {
            currentSessionState.OnValueChanged += SessionStateChanged;

        }

        private void OnDisable()
        {
            currentSessionState.OnValueChanged -= SessionStateChanged;

        }
        #endregion

        private void SessionStateChanged(GameState previousValue, GameState newValue)
        {
            if (IsServer) { Debug.Log($"{UnityNetworkManager.CheckPrivilege()} Game state has been changed to {newValue}"); return; }
            Debug.Log($"{UnityNetworkManager.CheckPrivilege()} Syncing game state from host ({newValue})");

        }

        public void UpdateSessionState(GameState newValue)
        {
            if (!IsServer) return;
            currentSessionState.Value = newValue;

        }

    }
}