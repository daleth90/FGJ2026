using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 一張選項卡。不可選的選項照樣顯示，只是不能點，並說明原因。
    /// </summary>
    public class OptionCardView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] TMP_Text titleLabel;
        [Tooltip("不可選時顯示原因的文字。可以不接，不接就不顯示原因。")]
        [SerializeField] TMP_Text lockLabel;
        [SerializeField] TMP_Text designerLabel;

        [Header("顏色")]
        [SerializeField] Color titleColor = new Color(0.93f, 0.94f, 0.89f);
        [SerializeField] Color lockedTextColor = new Color(0.50f, 0.52f, 0.47f);

        Action onClick;

        void Awake()
        {
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        public void Setup(int index, EventOption option, bool available, string lockText, string designerText,
            bool designer, Action onClick)
        {
            this.onClick = onClick;

            titleLabel.text = option.description;

            button.interactable = available;
            titleLabel.color = available ? titleColor : lockedTextColor;

            if (lockLabel != null)
            {
                lockLabel.gameObject.SetActive(!available);
                lockLabel.text = lockText;
            }

            designerLabel.text = designerText;
            SetDesigner(designer);
        }

        public void SetDesigner(bool on) => designerLabel.gameObject.SetActive(on);
    }
}
