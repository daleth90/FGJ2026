using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 開始畫面：按下開始鈕就載入主遊戲場景。場景要先加進 Build Settings 才能載入。
    /// </summary>
    public class StartMenu : MonoBehaviour
    {
        [SerializeField] Button startButton;
        [SerializeField] string gameSceneName = "Prototype";

        void Awake()
        {
            startButton.onClick.AddListener(StartGame);
        }

        public void StartGame() => SceneManager.LoadScene(gameSceneName);
    }
}
