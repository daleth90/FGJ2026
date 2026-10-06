using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 開始畫面：按下開始鈕（或空白鍵）就載入主遊戲場景。場景要先加進 Build Settings 才能載入。
    /// 開始鈕的圖是整張畫面大小、只有按鈕部分不透明，所以只讓不透明的地方接受點擊。
    /// </summary>
    public class StartMenu : MonoBehaviour
    {
        [SerializeField] Button startButton;
        [SerializeField] string gameSceneName = "Prototype";
        [Tooltip("開始鈕的圖片透明度高於這個值的地方才算點到按鈕（圖片需開啟 Read/Write）。0 = 整個矩形都算。")]
        [SerializeField, Range(0f, 1f)] float clickAlphaThreshold = 0.5f;

        private bool _isEntered;

        void Awake()
        {
            startButton.onClick.AddListener(StartGame);

            var image = startButton.image;
            if (image != null && clickAlphaThreshold > 0f && image.sprite != null && image.sprite.texture.isReadable)
                image.alphaHitTestMinimumThreshold = clickAlphaThreshold;
        }

        public void StartGame()
        {
            if (_isEntered)
            {
                return;
            }

            _isEntered = true;
            SceneManager.LoadScene(gameSceneName);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.isPressed)
            {
                StartGame();
            }
        }
    }
}
