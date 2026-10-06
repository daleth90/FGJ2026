#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;

namespace GrassRun
{
    /// <summary>
    /// 只在 Editor 有效的作弊面板：按 T 開關，直接調整目前的四項數值。
    /// 套用後只會更新數值本身（HUD 會跟著顯示），外觀、結局等判定照常等到下一次選選項才算。
    /// 面板開著時暫停遊戲，避免在輸入框打 1/2/3、空白鍵時觸發遊戲操作。
    /// 進入遊戲場景時自動建立，不需要放進場景。
    /// </summary>
    public class StatsCheat : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        static readonly string[] Labels = { "善良 mor", "濕度 hmd", "速度 spd", "韌性 tgh" };

        RunController controller;
        readonly string[] inputs = new string[StatBlock.StatCount];
        Rect window = new Rect(20, 20, 300, 230);
        float timeScaleBeforeOpen = 1f;

        static StatsCheat instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (instance != null) return;
            var go = new GameObject("[StatsCheat]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<StatsCheat>();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tKey.wasPressedThisFrame) return;
            if (IsOpen) Close();
            else Open();
        }

        void Open()
        {
            if (CodexController.IsOpen) return;
            controller = FindAnyObjectByType<RunController>();
            if (controller == null || controller.Engine == null) return;

            var stats = controller.Engine.Stats;
            for (int i = 0; i < StatBlock.StatCount; i++) inputs[i] = stats[(StatType)i].ToString();
            IsOpen = true;
            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;
        }

        void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Time.timeScale = timeScaleBeforeOpen;
            GUI.FocusControl(null);
        }

        void OnDisable() => Close();

        void OnGUI()
        {
            if (!IsOpen) return;
            window = GUI.Window(GetInstanceID(), window, DrawWindow, "作弊：調整數值（T 開關）");
        }

        void DrawWindow(int id)
        {
            for (int i = 0; i < StatBlock.StatCount; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Labels[i], GUILayout.Width(80));
                if (GUILayout.Button("-", GUILayout.Width(28))) inputs[i] = (Parse(inputs[i]) - 1).ToString();
                inputs[i] = GUILayout.TextField(inputs[i] ?? "0", GUILayout.Width(70));
                if (GUILayout.Button("+", GUILayout.Width(28))) inputs[i] = (Parse(inputs[i]) + 1).ToString();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
            GUILayout.Label("套用後要等選下一個選項才會判定外觀、結局等。");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("套用")) Apply();
            if (GUILayout.Button("關閉")) Close();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        void Apply()
        {
            if (controller == null || controller.Engine == null) return;
            var value = new StatBlock(Parse(inputs[0]), Parse(inputs[1]), Parse(inputs[2]), Parse(inputs[3]));
            controller.Engine.DebugSetStats(value);

            // 顯示裁切後的實際數值
            var stats = controller.Engine.Stats;
            for (int i = 0; i < StatBlock.StatCount; i++) inputs[i] = stats[(StatType)i].ToString();
        }

        static int Parse(string text) => int.TryParse(text, out int value) ? value : 0;
    }
}
#endif
