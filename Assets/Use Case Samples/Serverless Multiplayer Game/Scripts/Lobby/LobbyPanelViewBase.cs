using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Services.Samples.ServerlessMultiplayerGame
{
    public class LobbyPanelViewBase : PanelViewBase
    {
        [SerializeField]
        LobbySceneView sceneView;

        [SerializeField]
        Transform playersContainer;

        [SerializeField]
        PlayerIconView playerIconPrefab;

        [SerializeField]
        Button readyButton;

        [SerializeField]
        Button notReadyButton;

        protected List<PlayerIconView> m_PlayerIcons = new List<PlayerIconView>();

        protected bool m_IsReady = false;

        // This sets the players visible in the view by removing any existing players and re-adding those listed.
        // Note that this method is virtual because we override it for the Host View so the boot buttons can be
        // manually activated for all joining players.
        public virtual void SetPlayers(List<Player> players)
        {
            Debug.Log($"LobbyPanelViewBase.SetPlayers({players})");
            // 初期化
            RemoveAllPlayers();

            foreach (var player in players)
            {
                AddPlayer(player);
            }
        }

        public void TogglePlayerReadyState(string playerId)
        {
            Debug.Log($"LobbyPanelViewBase.TogglePlayerReadyState({playerId})");
            foreach (var playerIcon in m_PlayerIcons)
            {
                if (playerIcon.playerId == playerId)
                {
                    // 準備状態を切り替える
                    m_IsReady = playerIcon.ToggleReadyState();
                }
            }
        }

        // パネル内の全てのUIのインタラクティブ性を切り替える
        public override void SetInteractable(bool isInteractable)
        {
            Debug.Log($"LobbyPanelViewBase.SetInteractable({isInteractable})");
            // 継承しているクラスのSetInteractableを呼び出す
            base.SetInteractable(isInteractable);

            if (isInteractable)
            {
                readyButton.gameObject.SetActive(!m_IsReady);
                notReadyButton.gameObject.SetActive(m_IsReady);
            }
        }

        // 参加したプレイヤー情報をもとにプレイヤーアイコンを追加
        void AddPlayer(Player player)
        {
            Debug.Log($"LobbyPanelViewBase.AddPlayer({player})");
            // playerIconPrefabを複製してplayersContainerの子オブジェクトとして追加する
            // 全てのプレイヤーを管理しているplayerContainerに新しいプレイヤーを追加
            var playerIcon = GameObject.Instantiate(playerIconPrefab, playersContainer);

            var playerId = player.Id;
            var playerName = player.Data[LobbyManager.k_PlayerNameKey].Value;
            var playerIndex = m_PlayerIcons.Count;
            var isReady = bool.Parse(player.Data[LobbyManager.k_IsReadyKey].Value);
            var color = sceneView.playerColors[playerIndex];
            var backgroundColor = sceneView.playerBackgroundColors[playerIndex];

            // プレイヤー名が不敬でないことを確認し、不敬である場合はアスタリスクを使用してサニタイズする。
            playerName = ProfanityManager.SanitizePlayerName(playerName);

            playerIcon.Initialize(playerId, playerName, playerIndex, isReady, color, backgroundColor);

            m_PlayerIcons.Add(playerIcon);
        }

        // ゲームに参加しているプレイヤーのアイコンを全て削除する
        void RemoveAllPlayers()
        {
            Debug.Log("LobbyPanelViewBase.RemoveAllPlayers()");
            foreach (var playerIcon in m_PlayerIcons)
            {
                Destroy(playerIcon.gameObject);
            }
            m_PlayerIcons.Clear();
        }
    }
}
