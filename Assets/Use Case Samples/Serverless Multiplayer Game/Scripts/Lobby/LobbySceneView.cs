using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Unity.Services.Samples.ServerlessMultiplayerGame
{
    public class LobbySceneView : SceneViewBase
    {
        [SerializeField]
        HostLobbyPanelView hostLobbyPanelView;

        [SerializeField]
        JoinLobbyPanelView joinLobbyPanelView;

        [field: SerializeField]
        public Color[] playerColors { get; private set; }

        [field: SerializeField]
        public Color[] playerBackgroundColors { get; private set; }

        public void InitializeHostLobbyPanel()
        {
            Debug.Log("LobbySceneView.InitializeHostLobbyPanel()");
            hostLobbyPanelView.InitializeHostLobbyPanel();
        }

        public void ShowHostLobbyPanel()
        {
            Debug.Log("LobbySceneView.ShowHostLobbyPanel()");
            ShowPanel(hostLobbyPanelView);
        }

        public void SetLobbyCode(bool isVisible, string lobbyCode)
        {
            Debug.Log($"LobbySceneView.SetLobbyCode({isVisible}, {lobbyCode})");
            hostLobbyPanelView.SetLobbyCode(isVisible, lobbyCode);
        }

        public void SetHostLobbyPlayers(List<Player> players)
        {
            Debug.Log($"LobbySceneView.SetHostLobbyPlayers({players})");
            hostLobbyPanelView.SetPlayers(players);
        }

        public void ShowJoinLobbyPanel()
        {
            Debug.Log("LobbySceneView.ShowJoinLobbyPanel()");
            ShowPanel(joinLobbyPanelView);
        }

        public void SetJoinedLobby(Lobby lobby)
        {
            Debug.Log($"LobbySceneView.SetJoinedLobby({lobby.Name})");
            joinLobbyPanelView.SetLobby(lobby);
        }

        public void SetJoinLobbyPlayers(List<Player> players)
        {
            Debug.Log($"LobbySceneView.SetJoinLobbyPlayers({players})");
            joinLobbyPanelView.SetPlayers(players);
        }

        public void ToggleReadyState(string playerId)
        {
            Debug.Log($"LobbySceneView.ToggleReadyState({playerId})");
            if (m_CurrentPanelView == hostLobbyPanelView)
            {
                hostLobbyPanelView.TogglePlayerReadyState(playerId);
            }
            else
            {
                joinLobbyPanelView.TogglePlayerReadyState(playerId);
            }
        }
    }
}
