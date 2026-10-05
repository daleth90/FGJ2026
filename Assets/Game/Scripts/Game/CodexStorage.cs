using UnityEngine;

namespace GrassRun
{
    /// <summary>
    /// 圖鑑進度的存讀（PlayerPrefs）。網頁版存在瀏覽器（IndexedDB），exe 版存在登錄檔，寫法相同。
    /// 每次有新解鎖就立刻存檔，避免直接關掉分頁時遺失。
    /// </summary>
    public static class CodexStorage
    {
        const string Key = "GrassRun.Codex.v1";

        static CodexProgress progress;

        public static CodexProgress Progress => progress ??= CodexProgress.Parse(PlayerPrefs.GetString(Key, string.Empty));

        /// <summary>只有 Editor 和開發版可以重置圖鑑。</summary>
        public static bool CanReset => Application.isEditor || Debug.isDebugBuild;

        public static void UnlockTitle(int titleId)
        {
            if (Progress.UnlockTitle(titleId)) Save();
        }

        public static void UnlockForm(int formId)
        {
            if (Progress.UnlockForm(formId)) Save();
        }

        public static bool ResetProgress()
        {
            if (!CanReset) return false;
            Progress.Clear();
            Save();
            return true;
        }

        static void Save()
        {
            PlayerPrefs.SetString(Key, Progress.Serialize());
            PlayerPrefs.Save();
        }
    }
}
