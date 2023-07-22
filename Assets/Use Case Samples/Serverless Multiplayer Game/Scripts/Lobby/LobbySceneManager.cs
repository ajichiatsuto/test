using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Services.Samples.ServerlessMultiplayerGame
{
    public class LobbySceneManager : MonoBehaviour
    {
        [SerializeField]
        LobbySceneView sceneView;

        LobbyManager lobbyManager => LobbyManager.instance;
        bool isHost => lobbyManager.isHost;

        // Awakeの直後かシーン内のオブジェクトがインスタンス化されたときに呼び出される
        void Start()
        {
            Debug.Log("LobbySceneManager.Start()");
            if (ServerlessMultiplayerGameSampleManager.instance == null)
            {
                Debug.LogError("Please be sure to start Play mode on the ServerlessMultiplayerGameSample scene.");

// Unityエディターで実行している場合は、エディターを終了
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif

                return;
            }

            // LobbyManagerのOnLobbyChangedイベントにOnLobbyChangedメソッドを登録
            LobbyManager.OnLobbyChanged += OnLobbyChanged;
            // LobbyManagerのOnPlayerNotInLobbyEventイベントにOnPlayerNotInLobbyメソッドを登録
            LobbyManager.OnPlayerNotInLobbyEvent += OnPlayerNotInLobby;

            JoinLobby(LobbyManager.instance.activeLobby);
        }

        // プレイヤーがホストかどうかを判定し、ロビーのUIを更新する
        public void JoinLobby(Lobby lobbyJoined)
        {
            Debug.Log($"LobbySceneManager.JoinLobby({lobbyJoined.Name})");
            if (isHost)
            {
                // ホスト用のロビーUIを初期化
                sceneView.InitializeHostLobbyPanel();
                // ホスト用のロビーUIを表示
                sceneView.ShowHostLobbyPanel();

                // ホスト用のロビーUIにロビーの情報を設定
                sceneView.SetLobbyCode(lobbyJoined.IsPrivate, lobbyJoined.LobbyCode);
                sceneView.SetHostLobbyPlayers(lobbyJoined.Players);
            }
            else
            {
                // クライアント用のロビーUIを表示
                sceneView.ShowJoinLobbyPanel();
            }

            // 参加したロビーの情報を設定
            sceneView.SetJoinedLobby(lobbyJoined);

            // プロフィールのドロップダウンリストの初期選択位置を設定
            sceneView.SetProfileDropdownIndex(ServerlessMultiplayerGameSampleManager.instance.profileDropdownIndex);

            // プレイヤー名を設定
            sceneView.SetPlayerName(CloudSaveManager.instance.playerStats.playerName);

            // ロビーのUIを有効化(ボタンなど)
            sceneView.SetInteractable(true);
        }

        // LobbyManagerのOnLobbyChangedイベントに登録されているメソッド
        void OnLobbyChanged(Lobby updatedLobby, bool isGameReady)
        {
            Debug.Log($"LobbySceneManager.OnLobbyChanged({updatedLobby.Name}, {isGameReady})");
            if (isHost)
            {
                OnHostLobbyChanged(updatedLobby, isGameReady);
            }
            else
            {
                OnJoinLobbyChanged(updatedLobby, isGameReady);
            }
        }

        public void OnHostLobbyChanged(Lobby updatedLobby, bool isGameReady)
        {
            Debug.Log($"LobbySceneManager.OnHostLobbyChanged({updatedLobby.Name}, {isGameReady})");
            // ロビーに参加したプレイヤーの情報を更新
            sceneView.SetHostLobbyPlayers(updatedLobby.Players);

            // ロビーがゲームの開始準備ができている場合は、ゲームシーンに遷移
            if (isGameReady)
            {
                NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            }
        }

        public void OnJoinLobbyChanged(Lobby updatedLobby, bool isGameReady)
        {
            Debug.Log($"LobbySceneManager.OnJoinLobbyChanged({updatedLobby.Name}, {isGameReady})");
            // ロビーに参加したプレイヤーの情報を更新
            sceneView.SetJoinLobbyPlayers(updatedLobby.Players);
        }

        // 離脱ボタンを押したときに呼び出される
        public async void OnLobbyLeaveButtonPressed()
        {
            Debug.Log("LobbySceneManager.OnLobbyLeaveButtonPressed()");
            try
            {
                // ロビーから離脱
                await LeaveLobby();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void OnPlayerNotInLobby()
        {
            Debug.Log("LobbySceneManager.OnPlayerNotInLobby()");
            Debug.Log($"This player is no longer in the lobby so returning to main menu.");

            // メインメニューに戻る
            ReturnToMainMenu();
        }

        // Readyボタンを押したときに呼び出される
        public async void OnReadyButtonPressed()
        {
            Debug.Log("LobbySceneManager.OnReadyButtonPressed()");
            try
            {
                // ロビーのUIを無効化(ボタンなど)
                sceneView.SetInteractable(false);

                // プレイヤーの準備状態を切り替え
                // 注：この変更はロビーマネージャの状態変化としても捉えられ、プレイヤーの状態が正しい状態に強制的に変更されることになりますが、すでにチェックマークを正しく予測された最終状態に変更しているため、影響はありません。
                sceneView.ToggleReadyState(AuthenticationService.Instance.PlayerId);

                // プレイヤーの準備状態を切り替え
                await LobbyManager.instance.ToggleReadyState();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (this != null)
                {
                    // ロビーのUIを有効化(ボタンなど)
                    sceneView.SetInteractable(true);
                }
            }
        }

        public async void OnBootPlayerButtonPressed(PlayerIconView playerIcon)
        {
            Debug.Log($"LobbySceneManager.OnBootPlayerButtonPressed({playerIcon.playerId})");
            try
            {
                // ロビーのUIを無効化(ボタンなど)
                sceneView.SetInteractable(false);

                var playerId = playerIcon.playerId;

                Debug.Log($"Booting player {playerId}");
                await LobbyManager.instance.RemovePlayer(playerIcon.playerId);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (this != null)
                {
                    // ロビーのUIを有効化(ボタンなど)
                    sceneView.SetInteractable(true);
                }
            }
        }

        public async Task LeaveLobby()
        {
            Debug.Log("LobbySceneManager.LeaveLobby()");
            try
            {
                // ロビーのUIを無効化(ボタンなど)
                sceneView.SetInteractable(false);

                if (LobbyManager.instance.isHost)
                {
                    await LobbyManager.instance.DeleteAnyActiveLobbyWithNotify();
                }
                else
                {
                    await LobbyManager.instance.LeaveJoinedLobby();
                }
                if (this == null) return;

                // メインメニューに戻る
                ReturnToMainMenu();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // メインメニューに戻る
        void ReturnToMainMenu()
        {
            Debug.Log("LobbySceneManager.ReturnToMainMenu()");
            SceneManager.LoadScene("ServerlessMultiplayerGameSample");
        }

        void OnDestroy()
        {
            Debug.Log("LobbySceneManager.OnDestroy()");
            // イベントの登録解除
            LobbyManager.OnLobbyChanged -= OnLobbyChanged;
            LobbyManager.OnPlayerNotInLobbyEvent -= OnPlayerNotInLobby;
        }
    }
}
