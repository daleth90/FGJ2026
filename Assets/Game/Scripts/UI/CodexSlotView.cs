using System;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 圖鑑一格。已解鎖顯示原圖、可點擊看詳細；未解鎖把同一張圖塗黑成剪影，不能點。
    /// </summary>
    public class CodexSlotView : MonoBehaviour
    {
        [SerializeField] Image image;
        [Tooltip("天使／惡魔版本疊在上面的圖；沒有時隱藏。")]
        [SerializeField] Image overlay;
        [SerializeField] Button button;
        [SerializeField] Color lockedColor = Color.black;

        Action onClick;

        void Awake()
        {
            if (button != null) button.onClick.AddListener(() => onClick?.Invoke());
        }

        public void Setup(CodexEntry entry, bool unlocked, Action onClick)
        {
            this.onClick = unlocked ? onClick : null;
            var color = unlocked ? Color.white : lockedColor;

            image.sprite = entry.image;
            image.enabled = entry.image != null;
            image.color = color;

            if (overlay != null)
            {
                overlay.sprite = entry.overlay;
                overlay.enabled = entry.overlay != null;
                overlay.color = color;
            }

            if (button != null) button.interactable = unlocked;
        }
    }
}
