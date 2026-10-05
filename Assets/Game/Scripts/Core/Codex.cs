using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GrassRun
{
    /// <summary>型態圖鑑的一格：外觀加上天使／惡魔版本。</summary>
    public readonly struct CodexFormEntry
    {
        public readonly int id;
        public readonly CharacterAppearance appearance;
        public readonly MoralityDecoration decoration;

        public CodexFormEntry(CharacterAppearance appearance, MoralityDecoration decoration)
        {
            this.appearance = appearance;
            this.decoration = decoration;
            id = CodexRules.FormEntryId(appearance, decoration);
        }
    }

    /// <summary>
    /// 圖鑑編號規則。稱號直接用 titleId；型態編號 = 外觀編號 × 10 + 版本（0 一般、1 天使、2 惡魔）。
    /// Default 只有一般版（編號 0），一開始就解鎖。
    /// </summary>
    public static class CodexRules
    {
        public const int DefaultFormId = 0;

        public static int FormEntryId(CharacterAppearance appearance, MoralityDecoration decoration) =>
            appearance == CharacterAppearance.Default ? DefaultFormId : (int)appearance * 10 + (int)decoration;

        /// <summary>型態圖鑑的全部格子，依編號由小到大：Default，接著每種外觀的一般、天使、惡魔。</summary>
        public static IReadOnlyList<CodexFormEntry> FormEntries()
        {
            var appearances = (CharacterAppearance[])Enum.GetValues(typeof(CharacterAppearance));
            Array.Sort(appearances);

            var entries = new List<CodexFormEntry> { new CodexFormEntry(CharacterAppearance.Default, MoralityDecoration.None) };
            foreach (var appearance in appearances)
            {
                if (appearance == CharacterAppearance.Default) continue;
                entries.Add(new CodexFormEntry(appearance, MoralityDecoration.None));
                entries.Add(new CodexFormEntry(appearance, MoralityDecoration.Angel));
                entries.Add(new CodexFormEntry(appearance, MoralityDecoration.Devil));
            }
            return entries;
        }
    }

    /// <summary>
    /// 圖鑑解鎖進度：已解鎖的稱號編號和型態編號。純資料，存讀交給外層（PlayerPrefs）。
    /// 型態 Default（編號 0）永遠視為已解鎖。
    /// </summary>
    public sealed class CodexProgress
    {
        readonly HashSet<int> titles = new HashSet<int>();
        readonly HashSet<int> forms = new HashSet<int>();

        public CodexProgress() => forms.Add(CodexRules.DefaultFormId);

        public bool IsTitleUnlocked(int titleId) => titles.Contains(titleId);
        public bool IsFormUnlocked(int formId) => forms.Contains(formId);
        public int UnlockedTitleCount => titles.Count;
        public int UnlockedFormCount => forms.Count;

        /// <summary>解鎖稱號；第一次解鎖回傳 true。</summary>
        public bool UnlockTitle(int titleId) => titles.Add(titleId);

        /// <summary>解鎖型態；第一次解鎖回傳 true。</summary>
        public bool UnlockForm(int formId) => forms.Add(formId);

        public void Clear()
        {
            titles.Clear();
            forms.Clear();
            forms.Add(CodexRules.DefaultFormId);
        }

        /// <summary>存檔字串，例如「t=0,2008;f=0,20,21」。</summary>
        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append("t=");
            AppendIds(sb, titles);
            sb.Append(";f=");
            AppendIds(sb, forms);
            return sb.ToString();
        }

        /// <summary>讀回存檔字串；看不懂的部分直接略過，不會丟例外。</summary>
        public static CodexProgress Parse(string text)
        {
            var progress = new CodexProgress();
            if (string.IsNullOrEmpty(text)) return progress;

            foreach (string part in text.Split(';'))
            {
                int equals = part.IndexOf('=');
                if (equals < 0) continue;
                string key = part.Substring(0, equals);
                HashSet<int> target = key == "t" ? progress.titles : key == "f" ? progress.forms : null;
                if (target == null) continue;

                foreach (string token in part.Substring(equals + 1).Split(','))
                {
                    if (int.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int id))
                        target.Add(id);
                }
            }
            return progress;
        }

        static void AppendIds(StringBuilder sb, HashSet<int> ids)
        {
            var sorted = new List<int>(ids);
            sorted.Sort();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(sorted[i].ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
