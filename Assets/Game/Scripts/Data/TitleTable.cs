using UnityEngine;

namespace GrassRun
{
    [CreateAssetMenu(menuName = "GrassRun/Title Table", fileName = "TitleTable")]
    public class TitleTable : ScriptableObject
    {
        [Tooltip("依企劃表順序排列；結算時由最後一筆往前找第一個符合條件的稱號。")]
        public TitleDefinition[] titles;

        [Tooltip("由事件選項的 endingTitleId 直接指定，不參與一般稱號的倒序條件判定。")]
        public TitleDefinition[] specialEndingTitles;
    }
}
