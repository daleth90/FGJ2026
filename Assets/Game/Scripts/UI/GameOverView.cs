using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text bodyLabel;
        [SerializeField] TMP_Text summaryLabel;
        [SerializeField] Button restartButton;

        Action onRestart;

        void Awake()
        {
            restartButton.onClick.AddListener(() => onRestart?.Invoke());
        }

        public void Show(string title, string body, string summary, Action onRestart)
        {
            this.onRestart = onRestart;
            root.SetActive(true);

            titleLabel.text = title;
            bodyLabel.text = body;
            summaryLabel.text = summary;
        }

        public void Hide() => root.SetActive(false);
    }
}
