using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 一張選項卡。不可選的選項照樣顯示，只是變灰、不能點，並在 Tips 顯示需要的條件。
    /// </summary>
    public class OptionCardView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] TMP_Text titleLabel;
        [Tooltip("選項不可選時顯示需要的條件（例如「速度>=10」）；可選時隱藏。")]
        [SerializeField] TMP_Text tipsLabel;

        [Header("顏色")]
        [SerializeField] Color titleColor = new Color(0.93f, 0.94f, 0.89f);
        [SerializeField] Color lockedTextColor = new Color(0.50f, 0.52f, 0.47f);

        Action onClick;

        void Awake()
        {
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        public void Setup(EventOption option, OptionCheck check, Action onClick)
        {
            this.onClick = onClick;

            bool available = check.available;
            titleLabel.text = option.description;

            button.interactable = available;
            titleLabel.color = available ? titleColor : lockedTextColor;

            if (tipsLabel != null)
            {
                tipsLabel.gameObject.SetActive(!available);
                tipsLabel.text = available ? string.Empty : GameText.RequirementTip(option, check);
            }
        }
    }
}
