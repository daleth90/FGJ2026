using System;
using UnityEngine;

namespace GrassRun
{
    [Serializable]
    public class TitleDefinition
    {
        public int titleId;
        public string titleName;
        public string imageId;
        [TextArea(2, 5)]
        public string description;
        [TextArea(2, 5)]
        public string conditionDescription;
        [Tooltip("稱號條件，格式與事件 requirement 相同；空字串代表保底稱號。")]
        public string requirement;
    }
}
