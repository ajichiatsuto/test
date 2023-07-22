using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Unity.Services.Samples.ServerlessMultiplayerGame
{
    public class JoinLobbyPanelView : LobbyPanelViewBase
    {
        [SerializeField]
        TextMeshProUGUI titleText;

        // ロビーの名前とプレイヤーを設定
        public void SetLobby(Lobby lobby)
        {
            Debug.Log($"JoinLobbyPanelView.SetLobby({lobby.Name})");
            titleText.text = lobby.Name;

            m_IsReady = false;

            SetPlayers(lobby.Players);
        }
    }
}
