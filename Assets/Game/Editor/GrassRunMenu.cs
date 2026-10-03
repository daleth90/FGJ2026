using UnityEditor;
using UnityEngine;

namespace GrassRun.EditorTools
{
    /// <summary>
    /// 企劃用的事件資料檢查選單。
    /// </summary>
    public static class GrassRunMenu
    {
        const string BalancePath = "Assets/Game/Data/Balance.asset";
        [MenuItem("GrassRun/檢查事件資料")]
        public static void ValidateEvents()
        {
            var balance = LoadBalance();
            var library = LoadLibrary();
            var issues = EventValidation.ValidateLibrary(library, balance);

            if (issues.Count == 0)
                Debug.Log($"事件資料沒有問題（共 {library.Length} 個事件）。");
            else
                Debug.LogWarning($"事件資料有 {issues.Count} 個問題：\n" + string.Join("\n", issues));
        }

        static GameBalance LoadBalance() => AssetDatabase.LoadAssetAtPath<GameBalance>(BalancePath);

        static EventDefinition[] LoadLibrary() => Resources.LoadAll<EventDefinition>("Events");
    }
}
