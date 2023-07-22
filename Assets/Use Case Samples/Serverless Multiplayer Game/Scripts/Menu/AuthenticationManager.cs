using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Unity.Services.Samples.ServerlessMultiplayerGame
{
    public static class AuthenticationManager
    {
        // このサインインメソッドは起動時およびプレイヤーがプロファイルを切り替えた場合に呼び出される
        // ここで使用されるプロファイルは、1つの匿名サインインアカウントのみを使用してマルチプレイヤーゲームのテストを許可するために必要
        // 別のプロファイルに切り替えることで、UGSは他のプロファイル名から現在のユーザーを完全に別のユーザーとみなす
        // 私たちは使用したいプロファイルを渡し、このメソッドは必要に応じてUnity Servicesを初期化し、
        // 認証サービスに現在のユーザーを別のプロファイルに切り替えるよう要求
        // これにより、クラウドセーブデータやプレイヤーIDを含むUGS上の異ななデータにアクセスすることができる
        public static async Task SignInAnonymously(string profileName, int profileIndex)
        {
            Debug.Log($"AuthenticationManager.SignInAnonymously({profileName}, {profileIndex})");
            try
            {
                SwitchProfileIfNecessary(profileName);

                await InitialzeUnityServices(profileName);

                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                // 最後に使用したプロファイルインデックスを保存しておくと、起動時にこのプロファイルインデックスをデフォルトにすることができる
                ProfileManager.SaveLatestProfileIndexForProjectPath(profileIndex);

                Debug.Log($"Profile: {profileName} PlayerId: {AuthenticationService.Instance.PlayerId} " +
                    $"playerStats: [{CloudSaveManager.instance.playerStats}]");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        static void SwitchProfileIfNecessary(string profileName)
        {
            Debug.Log($"AuthenticationManager.SwitchProfileIfNecessary({profileName})");
            try
            {
                if (UnityServices.State == ServicesInitializationState.Initialized)
                {
                    if (AuthenticationService.Instance.IsSignedIn)
                    {
                        AuthenticationService.Instance.SignOut();
                    }

                    AuthenticationService.Instance.SwitchProfile(profileName);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        static async Task InitialzeUnityServices(string profileName)
        {
            Debug.Log($"AuthenticationManager.InitialzeUnityServices({profileName})");
            try
            {
                var unityAuthenticationInitOptions = new InitializationOptions();
                unityAuthenticationInitOptions.SetProfile(profileName);
                await UnityServices.InitializeAsync(unityAuthenticationInitOptions);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
