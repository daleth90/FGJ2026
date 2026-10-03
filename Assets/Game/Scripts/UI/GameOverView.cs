using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 結算畫面：稱號和重新開始。
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [Tooltip("顯示稱號的文字。")]
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] Button restartButton;

        Action onRestart;

        void Awake()
        {
            restartButton.onClick.AddListener(() => onRestart?.Invoke());
        }

        public void Show(string title, Action onRestart)
        {
            this.onRestart = onRestart;
            root.SetActive(true);
            titleLabel.text = title;
        }

        public void Hide() => root.SetActive(false);
    }
}
