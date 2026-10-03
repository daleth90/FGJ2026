using System;
using UnityEngine;

namespace GrassRun
{
    [Serializable]
    public struct Requirement
    {
        public StatType stat;
        public Comparison comparison;
        public Tier tier;

        public Requirement(StatType stat, Comparison comparison, Tier tier)
        {
            this.stat = stat;
            this.comparison = comparison;
            this.tier = tier;
        }
    }

    [Serializable]
    public struct StatChange
    {
        public StatType stat;
        [Tooltip("正數是成長，負數是代價。")]
        public int amount;

        public StatChange(StatType stat, int amount)
        {
            this.stat = stat;
            this.amount = amount;
        }
    }

    [Serializable]
    public class EventOption
    {
        [Tooltip("選項標題：玩家要做的動作。")]
        public string title;

        [Tooltip("一行說明：用文字暗示需要什麼、會付出什麼。不要寫數字。")]
        [TextArea(1, 3)]
        public string description;

        [Tooltip("門檻。全部達成才可選；留空代表沒有門檻。")]
        public Requirement[] requirements = Array.Empty<Requirement>();

        [Tooltip("選後的數值增減。負數是代價；付不起（會扣到 0 以下）時選項不可選。\n容量滿時，成長依這裡的順序分配，排後面的先溢出。")]
        public StatChange[] changes = Array.Empty<StatChange>();

        [Tooltip("自訂的鎖定原因。留空則依未達成的門檻自動產生。")]
        public string lockedText;

        [Tooltip("選後的微敘事。")]
        [TextArea(2, 4)]
        public string resultText;

        [Tooltip("後設提示：這個選擇對之後意味著什麼。")]
        [TextArea(1, 3)]
        public string hintText;
    }
}
