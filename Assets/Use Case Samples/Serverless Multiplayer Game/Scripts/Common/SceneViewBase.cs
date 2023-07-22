using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Services.Samples.ServerlessMultiplayerGame
{
    public class SceneViewBase : MonoBehaviour
    {
        [SerializeField]
        TMP_Dropdown profileSelectDropdown;

        [SerializeField]
        TextMeshProUGUI playerNameText;

        [SerializeField]
        MessagePopup messagePopup;

        protected PanelViewBase m_CurrentPanelView;

        public void SetProfileDropdownIndex(int profileDropdownIndex)
        {
            Debug.Log($"SceneViewBase.SetProfileDropdownIndex({profileDropdownIndex})");
            profileSelectDropdown.SetValueWithoutNotify(profileDropdownIndex);
        }

        public virtual void SetPlayerName(string playerName)
        {
            Debug.Log($"SceneViewBase.SetPlayerName({playerName})");
            playerNameText.text = $"{playerName}";
        }

        public void SetInteractable(bool isInteractable)
        {
            Debug.Log($"SceneViewBase.SetInteractable({isInteractable})");
            m_CurrentPanelView.SetInteractable(isInteractable);
        }

        public void ShowPopup(string title, string text)
        {
            Debug.Log($"SceneViewBase.ShowPopup({title}, {text})");
            messagePopup.Show(title, text);
        }

        public bool IsPanelVisible(PanelViewBase panelView)
        {
            Debug.Log($"SceneViewBase.IsPanelVisible({panelView})");
            return m_CurrentPanelView == panelView;
        }

        protected void ShowPanel(PanelViewBase panelView)
        {
            Debug.Log($"SceneViewBase.ShowPanel({panelView})");
            HideCurrentPanel();

            panelView.Show();

            m_CurrentPanelView = panelView;
        }

        protected void HideCurrentPanel()
        {
            Debug.Log($"SceneViewBase.HideCurrentPanel({m_CurrentPanelView})");
            if (m_CurrentPanelView != null)
            {
                m_CurrentPanelView.SetInteractable(false);

                m_CurrentPanelView.Hide();

                m_CurrentPanelView = null;
            }
        }
    }
}
