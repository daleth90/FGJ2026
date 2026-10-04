using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 結算畫面：稱號、稱號描述、死因和重新開始。
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [Tooltip("顯示稱號圖片。")]
        [SerializeField] UnityEngine.UI.Image titleIcon;
        [Tooltip("顯示稱號的文字。")]
        [SerializeField] TMP_Text titleLabel;
        [Tooltip("顯示稱號結局描述的文字（title_desc）。")]
        [SerializeField] TMP_Text titleDescLabel;
        [Tooltip("顯示死因的文字（dead_desc）。")]
        [SerializeField] TMP_Text deadDescLabel;
        [SerializeField] Button restartButton;

        Action onRestart;

        void Awake()
        {
            restartButton.onClick.AddListener(() => onRestart?.Invoke());
        }

        public void Show(string title, Sprite titleImage, string titleDescription, string deathCause, Action onRestart)
        {
            this.onRestart = onRestart;
            root.SetActive(true);
            titleIcon.sprite = titleImage;
            titleIcon.enabled = titleImage != null;
            titleLabel.text = title;
            if (titleDescLabel != null) titleDescLabel.text = titleDescription;
            if (deadDescLabel != null) deadDescLabel.text = deathCause;
        }

        public void Hide() => root.SetActive(false);
    }
}
