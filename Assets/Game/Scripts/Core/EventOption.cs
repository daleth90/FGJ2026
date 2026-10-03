using UnityEngine;

namespace GrassRun
{
    [System.Serializable]
    public struct Requirement
    {
        public StatType stat;
        public Comparison comparison;
        public int threshold;

        public Requirement(StatType stat, Comparison comparison, int threshold)
        {
            this.stat = stat;
            this.comparison = comparison;
            this.threshold = threshold;
        }
    }

    [System.Serializable]
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

    [System.Serializable]
    public class EventOption
    {
        [Tooltip("玩家看到的選項文案。只暗示需求與後果，不直接寫數字。")]
        [TextArea(1, 4)]
        public string description;

        [Tooltip("需求字串，例如 spd>=15 或 mor>=-10;mor<=10。留空代表沒有需求。")]
        public string requirement;

        [Tooltip("變動字串，例如 mor:+10;hmd:-10。固定依 mor、hmd、spd、tgh 排列。")]
        public string offset;

        [Tooltip("選完後顯示的微敘事與提示。")]
        [TextArea(2, 4)]
        public string resultText;

        [Tooltip("選完此選項後顯示的圖片 ID，不含副檔名。")]
        public string resultImageId;

        [Tooltip("特殊結局對應的稱號 ID。0 代表沒有；目前只保存資料，不處理結局流程。")]
        [Min(0)]
        public int endingTitleId;
    }
}
