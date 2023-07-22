using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Services.Samples.ServerlessMultiplayerGame
{
    // インスタンス化出来ず、継承して使う
    public abstract class PanelViewBase : MonoBehaviour
    {
        [SerializeField]
        //パネル内の全てのUIを保持
        List<Selectable> allSelectables;

        // パネルがインタラクティブであるかどうか
        protected bool isInteractable { get; private set; }

        // パネルを表示するときに実行
        public virtual void Show()
        {
            Debug.Log("PanelViewBase.Show()");
            gameObject.SetActive(true);
        }

        // パネルを非表示にするときに実行
        public virtual void Hide()
        {
            Debug.Log("PanelViewBase.Hide()");
            gameObject.SetActive(false);
        }

        // パネル内の全てのUIをインタラクティブにするかどうか
        public virtual void SetInteractable(bool isInteractable)
        {
            Debug.Log($"PanelViewBase.SetInteractable({isInteractable})");
            foreach (var selectable in allSelectables)
            {
                selectable.interactable = isInteractable;
            }

            this.isInteractable = isInteractable;
        }

        // パネルにselectableを追加する
        protected void AddSelectable(Selectable selectable)
        {
            Debug.Log($"PanelViewBase.AddSelectable({selectable})");
            allSelectables.Add(selectable);
        }

        // パネルからselectableを削除する
        protected void RemoveSelectable(Selectable selectableToRemove)
        {
            Debug.Log($"PanelViewBase.RemoveSelectable({selectableToRemove})");
            allSelectables.RemoveAll(selectable => selectable == selectableToRemove);
        }
    }
}
