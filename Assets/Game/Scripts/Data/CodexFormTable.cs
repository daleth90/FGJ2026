using System;
using UnityEngine;

namespace GrassRun
{
    /// <summary>型態圖鑑的文字資料。圖片沿用 CharacterSpriteSet（走路圖第一張、天使／惡魔第一張）。</summary>
    [CreateAssetMenu(menuName = "GrassRun/Codex Form Table", fileName = "CodexFormTable")]
    public class CodexFormTable : ScriptableObject
    {
        [Serializable]
        public class FormInfo
        {
            public CharacterAppearance appearance;
            [Tooltip("空白時顯示外觀的英文名稱。")]
            public string displayName;
            [TextArea(2, 5)]
            public string description;
            [TextArea(2, 5)]
            public string conditionDescription;
        }

        [Tooltip("每種外觀一筆；天使／惡魔版本共用同一筆文字。")]
        public FormInfo[] forms;

        public FormInfo Find(CharacterAppearance appearance)
        {
            if (forms == null) return null;
            foreach (var info in forms)
                if (info != null && info.appearance == appearance) return info;
            return null;
        }
    }
}
