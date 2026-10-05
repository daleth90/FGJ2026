using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>圖鑑詳細頁：點已解鎖的格子時顯示圖、名稱、描述與取得條件。</summary>
    public class CodexDetailView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Image image;
        [SerializeField] Image overlay;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text descriptionLabel;
        [SerializeField] TMP_Text conditionLabel;
        [Tooltip("關閉詳細頁的按鈕（可留空；翻頁或關閉圖鑑時也會自動關）。")]
        [SerializeField] Button closeButton;

        void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public void Show(CodexEntry entry)
        {
            root.SetActive(true);
            image.sprite = entry.image;
            image.enabled = entry.image != null;
            if (overlay != null)
            {
                overlay.sprite = entry.overlay;
                overlay.enabled = entry.overlay != null;
            }
            if (nameLabel != null) nameLabel.text = entry.name;
            if (descriptionLabel != null) descriptionLabel.text = entry.description;
            if (conditionLabel != null) conditionLabel.text = entry.condition;
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
