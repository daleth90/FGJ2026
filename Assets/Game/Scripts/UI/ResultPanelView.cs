using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 選完之後的回饋：微敘事、後設提示、數值變化。
    /// </summary>
    public class ResultPanelView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text headingLabel;
        [SerializeField] TMP_Text resultLabel;
        [SerializeField] TMP_Text hintLabel;
        [SerializeField] TMP_Text changeLabel;
        [SerializeField] Button continueButton;

        Action onContinue;

        void Awake()
        {
            continueButton.onClick.AddListener(() => onContinue?.Invoke());
        }

        public void Show(string heading, string result, string hint, string changes, Action onContinue)
        {
            this.onContinue = onContinue;
            root.SetActive(true);

            headingLabel.text = heading;
            resultLabel.text = result;
            hintLabel.text = hint;
            changeLabel.text = changes;
        }

        public void Hide() => root.SetActive(false);
    }
}
