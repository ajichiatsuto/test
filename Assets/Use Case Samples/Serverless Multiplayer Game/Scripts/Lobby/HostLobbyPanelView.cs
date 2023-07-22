using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Unity.Services.Samples.ServerlessMultiplayerGame
{
    public class HostLobbyPanelView : LobbyPanelViewBase
    {
        [SerializeField]
        LobbySceneManager lobbySceneManager;

        [SerializeField]
        GameObject lobbyTextCodeContainer;

        [SerializeField]
        TextMeshProUGUI lobbyCodeText;

        public void InitializeHostLobbyPanel()
        {
            Debug.Log("HostLobbyPanelView.InitializeHostLobbyPanel()");
            m_IsReady = false;
        }

        public void SetLobbyCode(bool isVisible, string lobbyCode)
        {
            Debug.Log($"HostLobbyPanelView.SetLobbyCode({isVisible}, {lobbyCode})");
            lobbyTextCodeContainer.SetActive(isVisible);
            lobbyCodeText.text = lobbyCode;
        }

        public override void SetPlayers(List<Player> players)
        {
            Debug.Log($"HostLobbyPanelView.SetPlayers({players})");
            // 以前のホストの「起動」ボタンをすべて切断
            DisableBootButtons();

            // すべてのプレイヤーアイコンをリフレッシュ
            base.SetPlayers(players);

            // すべての新しいホストの「起動」ボタンを接続する（ボタンを有効にする、リスナーを追加する、すべての選択可能リストに追加する）
            EnableBootButtons();
        }

        void EnableBootButtons()
        {
            Debug.Log("HostLobbyPanelView.EnableBootButtons()");
            // 最初のプレイヤーは常にブートできないホストであるため、プレイヤー1からスタート
            for (var i = 1; i < m_PlayerIcons.Count; i++)
            {
                var playerIcon = m_PlayerIcons[i];
                var bootButton = playerIcon.BootButton;

                AddSelectable(bootButton);

                bootButton.onClick.AddListener(() =>
                    lobbySceneManager.OnBootPlayerButtonPressed(playerIcon));

                playerIcon.EnableHostBootButton();
            }
        }

        void DisableBootButtons()
        {
            Debug.Log("HostLobbyPanelView.DisableBootButtons()");
            for (var i = 1; i < m_PlayerIcons.Count; i++)
            {
                var playerIcon = m_PlayerIcons[i];
                var bootButton = playerIcon.BootButton;

                RemoveSelectable(bootButton);

                bootButton.onClick.RemoveListener(() =>
                    lobbySceneManager.OnBootPlayerButtonPressed(playerIcon));
            }
        }
    }
}
