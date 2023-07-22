using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Services.Samples.ServerlessMultiplayerGame
{
    public class PlayerIconView : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI playerNameText;

        [SerializeField]
        TextMeshProUGUI playerNumberText;

        [SerializeField]
        GameObject checkMarkImage;

        [SerializeField]
        Button bootButton;

        [SerializeField]
        Image backgroundImage;

        [SerializeField]
        Image profileBorderImage;

        [SerializeField]
        Image namePlateImage;

        public Button BootButton => bootButton;

        public string playerId { get; private set; }

        public void Initialize(string playerId, string playerName, int playerNumber, bool isReady, Color color, Color backgroundColor)
        {
            Debug.Log($"PlayerIconView.Initialize({playerId}, {playerName}, {playerNumber}, {isReady}, {color}, {backgroundColor})");
            this.playerId = playerId;

            playerNameText.text = playerName;
            playerNumberText.text = $"PLAYER {playerNumber + 1}";

            backgroundImage.color = backgroundColor;
            profileBorderImage.color = color;
            namePlateImage.color = color;

            SetReady(isReady);
        }

        public bool ToggleReadyState()
        {
            Debug.Log("PlayerIconView.ToggleReadyState()");
            var isReady = !checkMarkImage.activeSelf;
            checkMarkImage.SetActive(isReady);

            return isReady;
        }

        public void SetReady(bool isReady)
        {
            Debug.Log($"PlayerIconView.SetReady({isReady})");
            checkMarkImage.SetActive(isReady);
        }

        public void EnableHostBootButton()
        {
            Debug.Log("PlayerIconView.EnableHostBootButton()");
            bootButton.gameObject.SetActive(true);
        }
    }
}
