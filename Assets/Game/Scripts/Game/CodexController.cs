using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 圖鑑的開關與按鍵：Open() 由外部呼叫（HUD 的圖鑑按鈕經 RunController），關閉鈕關閉，
    /// 左右方向鍵翻頁，C 重置（只在 Editor 和開發版有效）。
    /// 開著的時候暫停遊戲（Time.timeScale = 0），RunController 也會略過自己的輸入。
    /// </summary>
    public class CodexController : MonoBehaviour
    {
        [SerializeField] CodexView view;
        [Tooltip("關閉圖鑑的按鈕（圖鑑裡的關閉鈕）。")]
        [SerializeField] Button closeButton;

        /// <summary>目前是否有圖鑑開著；RunController 用來暫停輸入與計時。</summary>
        public static bool IsOpen { get; private set; }

        bool open;
        float timeScaleBeforeOpen = 1f;

        void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        void Start()
        {
            if (view != null) view.Hide();
        }

        void Update()
        {
            if (!open || view == null) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.leftArrowKey.wasPressedThisFrame) view.TurnPage(-1);
            else if (keyboard.rightArrowKey.wasPressedThisFrame) view.TurnPage(1);
            else if (keyboard.cKey.wasPressedThisFrame && CodexStorage.ResetProgress()) view.Refresh();
        }

        public void Open()
        {
            if (open || view == null) return;
            open = true;
            IsOpen = true;
            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
            view.Show();
        }

        public void Close()
        {
            if (!open) return;
            view.Hide();
            Restore();
        }

        void OnDisable() => Restore();

        void Restore()
        {
            if (!open) return;
            open = false;
            IsOpen = false;
            Time.timeScale = timeScaleBeforeOpen;
        }
    }
}
