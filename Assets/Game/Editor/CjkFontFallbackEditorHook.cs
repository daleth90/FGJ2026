using UnityEditor;

namespace GrassRun.EditorTools
{
    /// <summary>
    /// 讓編輯模式下的 TextMeshPro 也能顯示中文（不用進 Play 才看得到）。
    /// Play 模式由 <see cref="CjkFontFallback"/> 自己在啟動時處理。
    /// </summary>
    [InitializeOnLoad]
    static class CjkFontFallbackEditorHook
    {
        static CjkFontFallbackEditorHook()
        {
            EditorApplication.delayCall += InstallInEditMode;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 離開 Play 後檢查一次：字型如果在 Play 期間壞了，這裡會重建。
            if (state == PlayModeStateChange.EnteredEditMode) InstallInEditMode();
        }

        static void InstallInEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            CjkFontFallback.Install();
        }
    }
}
