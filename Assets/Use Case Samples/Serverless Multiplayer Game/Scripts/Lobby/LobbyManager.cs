using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Unity.Services.Samples.ServerlessMultiplayerGame
{
    [DisallowMultipleComponent]
    public class LobbyManager : MonoBehaviour
    {
        // ロビーに表示される各選手の名前を調べるためのロビーデータキー
        public const string k_PlayerNameKey = "playerName";

        // 各プレイヤーが[Ready]ボタンをクリックしたかどうかを確認するためのロビーデータキー
        public const string k_IsReadyKey = "isReady";

        // ホスト名のロビーデータ
        public const string k_HostNameKey = "hostName";

        // ホストのRelay Join Codeのためのロビーデータ
        // 全プレイヤーがRelayを初期化できるようにするために使用され、NGO (Netcode for GameObjects)がプレイヤー間のマルチプレイヤーゲームプレイを同期させるために、全プレイヤーにRelayを初期化させるために使用される
        public const string k_RelayJoinCodeKey = "relayJoinCode";

        // GetLobbyAsyncを呼び出して、参加/離脱や準備完了状態などのプレーヤーの状態を更新する頻度
        // 頻繁に呼び出すと、レート制限の例外が発生するので注意すること
        const float k_UpdatePlayersFrequency = 1.5f;

        // ホストがロビーをアクティブに保つためにSendHeartbeatPingAsyncを呼び出す頻度
        // 頻繁に呼び出されると、レート制限の例外が発生するので注意すること
        const float k_HostHeartbeatFrequency = 15;

        public static LobbyManager instance { get; private set; }

        public List<Lobby> lobbiesList { get; private set; } = new List<Lobby>();

        public Lobby activeLobby { get; private set; }

        public static string playerId => AuthenticationService.Instance.PlayerId;

        public List<Player> players { get; private set; }
        
        public int numPlayers => players.Count;

        public bool isHost { get; private set; }

        // ロビーが変更された際にこの変数内に登録されたメソッドすべてにLobbyオブジェクトとbool値を渡して実行する
        public static event Action<Lobby, bool> OnLobbyChanged;

        public static event Action OnPlayerNotInLobbyEvent;

        float m_NextHostHeartbeatTime;

        float m_NextUpdatePlayersTime;

        string m_PlayerName;

        bool m_IsPlayerReady = false;

        bool m_WasGameStarted = false;

        // LobbyManagerクラスがインスタンス化された際に呼び出される
        // シングルトンパターンを使用しているため、既にインスタンスが存在する場合は破棄する
        void Awake()
        {
            Debug.Log("LobbyManager.Awake()");
            if (instance != null && instance != this)
            {
                Destroy(this);
            }
            else
            {
                instance = this;
            }
        }

        // フレームごとに呼び出される
        async void Update()
        {
            try
            {
                // アクティブなロビーが存在し、ゲームが開始されていない場合に実行
                if (activeLobby != null && !m_WasGameStarted)
                {
                    // Time.realtimeSinceStartupは起動してからの時間を保持している
                    // このプレイヤーが、ホストであり時間が次のホストハートビートの時間を超えた場合に実行
                    if (isHost && Time.realtimeSinceStartup >= m_NextHostHeartbeatTime)
                    {
                        await PeriodicHostHeartbeat();

                        // 1回のUpdate()で1つの項目（ハートビートまたはロビーの変更）しか更新しないように、この更新を今すぐ終了させる
                        return;
                    }

                    if (Time.realtimeSinceStartup >= m_NextUpdatePlayersTime)
                    {
                        await PeriodicUpdateLobby();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // 次のハートビート時間を設定し、Lobby Serviceを呼び出す
        async Task PeriodicHostHeartbeat()
        {
            Debug.Log("LobbyManager.PeriodicHostHeartbeat()");
            try
            {
                // 次のアップデートでもハートビートが発生し、スロットリングの問題が発生する可能性があるため、Lobby Serviceを呼び出す前に次のハートビート時間を設定する。
                m_NextHostHeartbeatTime = Time.realtimeSinceStartup + k_HostHeartbeatFrequency;
                
                // サーバーに対してハートビートメッセージを送信
                // このメッセージを定期的に送信することで、サーバーはロビーがアクティブであること確認
                await LobbyService.Instance.SendHeartbeatPingAsync(activeLobby.Id);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        async Task PeriodicUpdateLobby()
        {
            Debug.Log("LobbyManager.PeriodicUpdateLobby()");
            try
            {
                // 次のアップデートでもハートビートが発生し、スロットリングの問題が発生する可能性があるため、Lobby Serviceを呼び出す前に次のハートビート時間を設定する。
                m_NextUpdatePlayersTime = Time.realtimeSinceStartup + k_UpdatePlayersFrequency;

                // activeなロビーの情報を取得
                var updatedLobby = await LobbyService.Instance.GetLobbyAsync(activeLobby.Id);
                if (this == null) return;

                // ロビー情報を更新
                UpdateLobby(updatedLobby);
            }

            // ハンドルロビーが存在しない（ホストがゲームをキャンセルしてメインメニューに戻った）。
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                if (this == null) return;

                // メニューに戻る理由を設定 // task
                ServerlessMultiplayerGameSampleManager.instance.SetReturnToMenuReason(
                    ServerlessMultiplayerGameSampleManager.ReturnToMenuReason.LobbyClosed);

                // プレイヤーがロビーからいなくなったことを通知
                OnPlayerNotInLobby();
            }

            // ハンドルプレーヤーがロビーを見ることができなくなった（ホストがプレーヤーをブートしたため、プレーヤーがロビーにいなくなった）。
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.Forbidden)
            {
                if (this == null) return;

                // メニューに戻る理由を設定 // task
                ServerlessMultiplayerGameSampleManager.instance.SetReturnToMenuReason(
                    ServerlessMultiplayerGameSampleManager.ReturnToMenuReason.PlayerKicked);

                // プレイヤーがロビーからいなくなったことを通知
                OnPlayerNotInLobby();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }


        // 返り値がLobbyオブジェクトの非同期関数
        // 引数をもとにロビーを作成し、作成したロビーオブジェクトを返す
        public async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers, string hostName,
            bool isPrivate, string relayJoinCode)
        {
            Debug.Log($"LobbyManager.CreateLobby({lobbyName}, {maxPlayers}, {hostName}, {isPrivate}, {relayJoinCode})");
            try
            {
                isHost = true;
                m_PlayerName = hostName;
                m_WasGameStarted = false;
                m_IsPlayerReady = false;

                // 以前に作成したロビーがある場合は削除
                await DeleteAnyActiveLobbyWithNotify();

                // ロビー作成中にオブジェクトが破棄される場合に備える
                if (this == null) return default;

                var options = new CreateLobbyOptions();
                options.IsPrivate = isPrivate;
                options.Data = new Dictionary<string, DataObject>
                {
                    // 他のプレイヤーからホスト名、relayJoinCodeを見れるようにしてoptionsに設定 
                    // task
                    { k_HostNameKey, new DataObject(DataObject.VisibilityOptions.Public, hostName) },
                    { k_RelayJoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode) },
                };

                // プレイヤー情報を作成
                options.Player = CreatePlayerData();

                // optionに設定した情報とlobbyName, maxPlayersからロビーを作成し、作成したロビーをactiveLobbyに設定
                activeLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
                if (this == null) return default;

                players = activeLobby?.Players;

                Log(activeLobby);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            // 作成されたロビーを返す
            return activeLobby;
        }

        // 既存のロビーを削除し、プレイヤーがロビーから削除されたことを通知
        public async Task DeleteAnyActiveLobbyWithNotify()
        {
            Debug.Log("LobbyManager.DeleteAnyActiveLobbyWithNotify()");
            try
            {
                // 既存のロビーが存在しており、このプレイヤーがホストである場合にのみ、ロビーを削除する
                if (activeLobby != null && isHost)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(activeLobby.Id);
                    if (this == null) return;

                    // OnPlayerNotInLobbyEventを実行
                    OnPlayerNotInLobby();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // 既存のロビーを削除し、プレイヤーがロビーから削除されたことを通知しない
        public async Task DeleteActiveLobbyNoNotify()
        {
            Debug.Log("LobbyManager.DeleteActiveLobbyNoNotify()");
            try
            {
                if (activeLobby != null && isHost)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(activeLobby.Id);
                    if (this == null) return;

                    activeLobby = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }


        // 非同期的に更新されたロビーのリストを取得
        public async Task<List<Lobby>> GetUpdatedLobbiesList()
        {
            Debug.Log("LobbyManager.GetUpdatedLobbiesList()");
            try
            {
                // 非同期的にロビーのリストを取得
                var lobbiesQuery = await LobbyService.Instance.QueryLobbiesAsync();
                if (this == null) return default;

                lobbiesList = lobbiesQuery.Results;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return lobbiesList;
        }

        public async Task<Lobby> JoinLobby(string lobbyId, string playerName)
        {
            Debug.Log($"LobbyManager.JoinLobby({lobbyId}, {playerName})");
            try
            {
                // ロビー参加に必要な準備を行う　//task
                await PrepareToJoinLobby(playerName);
                if (this == null) return default;

                var options = new JoinLobbyByIdOptions();
                // プレイヤー情報を作成 //task
                options.Player = CreatePlayerData();

                // ロビーに参加し、参加したロビーをactiveLobbyに設定
                activeLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
                if (this == null) return default;

                // activelobbyがnullでない場合、playersにactiveLobbyのプレイヤー情報を設定
                players = activeLobby?.Players;
            }
            // 指定したロビーが見つからない場合
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                // lobby-not-found例外をキャッチしてrethrowし、呼び出し元がメッセージをポップできるようにします。
                if (this == null) return null;

                activeLobby = null;

                throw;
            }
            // ロビーが満員の場合
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyFull)
            {
                if (this == null) return null;

                activeLobby = null;

                throw;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return activeLobby;
        }

        public async Task<Lobby> JoinPrivateLobby(string lobbyJoinCode, string playerName)
        {
            Debug.Log($"LobbyManager.JoinPrivateLobby({lobbyJoinCode}, {playerName})");
            try
            {
                // ロビー参加に必要な準備を行う　//task
                await PrepareToJoinLobby(playerName);
                if (this == null) return default;

                var options = new JoinLobbyByCodeOptions();
                // プレイヤー情報を作成 //task
                options.Player = CreatePlayerData();

                // ロビーに参加し、参加したロビーをactiveLobbyに設定
                activeLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyJoinCode, options);
                if (this == null) return default;

                players = activeLobby?.Players;
            }
            // 指定したロビーが見つからない場合
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                if (this == null) return null;

                activeLobby = null;

                throw;
            }
            // ロビーが満員の場合
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyFull)
            {
                if (this == null) return null;

                activeLobby = null;

                throw;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return activeLobby;
        }

        public async Task LeaveJoinedLobby()
        {
            Debug.Log("LobbyManager.LeaveJoinedLobby()");
            try
            {
                //指定したIDのプレイヤーをロビーから削除
                await RemovePlayer(playerId);
                // ロビー退出中にオブジェクトが破棄された場合に備える
                if (this == null) return;

                // OnPlayerNotInLobbyEventを実行
                OnPlayerNotInLobby();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // 指定したプレイやをロビーから削除
        public async Task RemovePlayer(string playerId)
        {
            Debug.Log($"LobbyManager.RemovePlayer({playerId})");
            try
            {
                if (activeLobby != null)
                {
                    await LobbyService.Instance.RemovePlayerAsync(activeLobby.Id, playerId);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // プレイヤーのreadyStateを更新し、ロビー情報も更新する
        public async Task ToggleReadyState()
        {
            Debug.Log("LobbyManager.ToggleReadyState()");
            try
            {
                if (activeLobby == null)
                {
                    Debug.Log("Attempting to toggle ready state when not already in a lobby.");
                    return;
                }

                // 準備状態を反転
                m_IsPlayerReady = !m_IsPlayerReady;

                var lobbyId = activeLobby.Id;

                var options = new UpdatePlayerOptions();
                // プレイヤー情報を更新 //task
                options.Data = CreatePlayerDictionary();

                // プレイヤー情報を更新
                var updatedLobby = await LobbyService.Instance.UpdatePlayerAsync(lobbyId, playerId, options);
                if (this == null) return;

                // ロビー情報を更新
                UpdateLobby(updatedLobby);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // Gameが開始されたときに呼び出される
        // ホストは更新を停止し、すべてのクライアントはアクティブなロビーをクリア
        public void OnGameStarted()
        {
            Debug.Log("LobbyManager.OnGameStarted()");
            // 実際にゲームが始まると、ホストは更新を停止
            if (isHost)
            {
                m_WasGameStarted = true;
            }

            // 実際にゲームが始まると、すべてのクライアントがアクティブなロビーをクリア
            // すべてのクライアントが開始したことを認めたら、ホストがロビー自体を実際に削除するため
            else
            {
                activeLobby = null;
            }
        }

        // activeLobbyをnullに設定し、OnPlayerNotInLobbyイベントを実行
        public void OnPlayerNotInLobby()
        {
            Debug.Log("LobbyManager.OnPlayerNotInLobby()");
            if (activeLobby != null)
            {
                activeLobby = null;

                // OnPlayerNotInLobbyイベントがnullではない場合、イベントを実行
                OnPlayerNotInLobbyEvent?.Invoke();
            }
        }

        // インデックスからプレイヤーIDを取得
        public string GetPlayerId(int playerIndex)
        {
            Debug.Log($"LobbyManager.GetPlayerId({playerIndex})");
            return players[playerIndex].Id;
        }

        // インデックスからプレイヤー名を取得
        public string GetPlayerName(int playerIndex)
        {
            Debug.Log($"LobbyManager.GetPlayerName({playerIndex})");
            var player = players[playerIndex].Data;
            return player[k_PlayerNameKey].Value;
        }

        // ロビーに入室するための準備を行う
        async Task PrepareToJoinLobby(string playerName)
        {
            Debug.Log($"LobbyManager.PrepareToJoinLobby({playerName})");
            isHost = false;
            m_PlayerName = playerName;
            m_WasGameStarted = false;
            m_IsPlayerReady = false;

            if (activeLobby != null)
            {
                // すでにロビーに入っている場合は、古いロビーを離れる
                Debug.Log("Already in a lobby when attempting to join so leaving old lobby.");
                await LeaveJoinedLobby();
            }
        }

        Player CreatePlayerData()
        {
            Debug.Log("LobbyManager.CreatePlayerData()");
            var player = new Player();
            player.Data = CreatePlayerDictionary();

            return player;
        }

        Dictionary<string, PlayerDataObject> CreatePlayerDictionary()
        {
            Debug.Log("LobbyManager.CreatePlayerDictionary()");
            var playerDictionary = new Dictionary<string, PlayerDataObject>
            {
                { k_PlayerNameKey,  new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, m_PlayerName) },
                { k_IsReadyKey,  new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, m_IsPlayerReady.ToString()) },
            };
            Debug.Log($"playerDictionary: {playerDictionary}");

            return playerDictionary;
        }

        void UpdateLobby(Lobby updatedLobby)
        {
            Debug.Log($"LobbyManager.UpdateLobby({updatedLobby})");
            // awaitの後に呼び出されるので、待機中にLobbyが閉じていないことを確認
            if (activeLobby == null || updatedLobby == null) return;

            // ロビーのプレイヤーが変更された場合、ロビーを更新
            if (DidPlayersChange(activeLobby.Players, updatedLobby.Players))
            {
                activeLobby = updatedLobby;
                players = activeLobby?.Players;

                // updatedLobbyに参加しているプレイヤーの中に自身がいるかどうか確認
                if (updatedLobby.Players.Exists(player => player.Id == playerId))
                {
                    // ロビーがゲームの準備ができているかどうかを確認
                    var isGameReady = IsGameReady(updatedLobby);

                    OnLobbyChanged?.Invoke(updatedLobby, isGameReady);
                }
                else
                {
                    // menuに戻る理由を設定
                    ServerlessMultiplayerGameSampleManager.instance.SetReturnToMenuReason(
                        ServerlessMultiplayerGameSampleManager.ReturnToMenuReason.PlayerKicked);

                    OnPlayerNotInLobby();
                }
            }
        }

        // プレイヤーの数やID、準備状態が変更されたかどうかを確認
        static bool DidPlayersChange(List<Player> oldPlayers, List<Player> newPlayers)
        {
            Debug.Log($"LobbyManager.DidPlayersChange({oldPlayers}, {newPlayers})");
            if (oldPlayers.Count != newPlayers.Count)
            {
                return true;
            }

            for (int i = 0; i < newPlayers.Count; i++)
            {
                if (oldPlayers[i].Id != newPlayers[i].Id ||
                    oldPlayers[i].Data[k_IsReadyKey].Value != newPlayers[i].Data[k_IsReadyKey].Value)
                {
                    return true;
                }
            }

            return false;
        }

        // ロビーがゲームの準備ができているかどうかを確認
        static bool IsGameReady(Lobby lobby)
        {
            Debug.Log($"LobbyManager.IsGameReady({lobby})");
            // ロビーには少なくとも2人のプレイヤーが必要
            if (lobby.Players.Count <= 1)
            {
                return false;
            }

            // ロビーのすべてのプレイヤーが準備ができているかどうかを確認
            foreach (var player in lobby.Players)
            {
                var isReady = bool.Parse(player.Data[k_IsReadyKey].Value);
                if (!isReady)
                {
                    return false;
                }
            }

            return true;
        }

        public static void Log(Lobby lobby)
        {
            if (lobby is null)
            {
                Debug.Log("No active lobby.");

                return;
            }

            var lobbyData = lobby.Data.Select(kvp => $"{kvp.Key} is {kvp.Value.Value}" );
            var lobbyDataStr = string.Join(", ", lobbyData);

            Debug.Log($"Lobby Named:{lobby.Name}, " +
                $"Players:{lobby.Players.Count}/{lobby.MaxPlayers}, " +
                $"IsPrivate:{lobby.IsPrivate}, " +
                $"IsLocked:{lobby.IsLocked}, " +
                $"LobbyCode:{lobby.LobbyCode}, " +
                $"Id:{lobby.Id}, " +
                $"Created:{lobby.Created}, " +
                $"HostId:{lobby.HostId}, " +
                $"EnvironmentId:{lobby.EnvironmentId}, " +
                $"Upid:{lobby.Upid}, " +
                $"Lobby.Data:{lobbyDataStr}");
        }

        public static void Log(string message, List<Lobby> lobbies)
        {
            if (lobbies.Count == 0)
            {
                Debug.Log($"{message}: No Lobbies found.");
            }
            else
            {
                Debug.Log($"{message}: Lobbies list:");
                foreach (var lobby in lobbies)
                {
                    Debug.Log($"  Lobby: {lobby.Name}, " +
                        $"players: {lobby.Players.Count}/{lobby.MaxPlayers}, " +
                        $"id:{lobby.Id}");
                }
            }
        }

        void OnDestroy()
        {
            Debug.Log("LobbyManager.OnDestroy()");
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
