using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 常駐資訊：四項數值（善良值顯示在 Camp）、旅程紀錄。
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [Header("數值")]
        [SerializeField] TMP_Text speedValue;
        [SerializeField] TMP_Text moistureValue;
        [SerializeField] TMP_Text toughnessValue;
        [Tooltip("善良值（StatType.Morality）。")]
        [SerializeField] TMP_Text campValue;
        [SerializeField] RectTransform speedChip;
        [SerializeField] RectTransform moistureChip;
        [SerializeField] RectTransform toughnessChip;
        [SerializeField] RectTransform campChip;

        [Header("Camp 圖示（依善良值切換；沒指定的就維持原圖）")]
        [SerializeField] Image campIcon;
        [Tooltip("善良值 > 0")]
        [SerializeField] Sprite campPositiveSprite;
        [Tooltip("善良值 = 0")]
        [SerializeField] Sprite campNeutralSprite;
        [Tooltip("善良值 < 0")]
        [SerializeField] Sprite campNegativeSprite;

        [Header("其他")]
        [Tooltip("只在奔跑時顯示的東西（旅程紀錄、操作說明）。事件面板開著時會被蓋住，所以直接藏起來。")]
        [SerializeField] GameObject runningRoot;
        [SerializeField] TMP_Text journalLabel;

        const float PunchDecay = 4f;
        const float PunchScale = 0.3f;

        readonly float[] punch = new float[StatBlock.StatCount];
        readonly List<string> journal = new List<string>();
        StatBlock shown;
        bool hasShown;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            ApplyPunch(speedChip, (int)StatType.Speed, dt);
            ApplyPunch(moistureChip, (int)StatType.Moisture, dt);
            ApplyPunch(toughnessChip, (int)StatType.Toughness, dt);
            ApplyPunch(campChip, (int)StatType.Morality, dt);
        }

        void ApplyPunch(RectTransform chip, int index, float dt)
        {
            if (chip == null) return;
            punch[index] = Mathf.MoveTowards(punch[index], 0f, PunchDecay * dt);
            chip.localScale = Vector3.one * (1f + PunchScale * punch[index]);
        }

        public void Render(StatBlock stats)
        {
            if (hasShown)
            {
                bool changed = false;
                for (int i = 0; i < StatBlock.StatCount; i++)
                {
                    if (stats[(StatType)i] == shown[(StatType)i]) continue;
                    punch[i] = 1f;
                    changed = true;
                }
                if (!changed) return;
            }

            shown = stats;
            hasShown = true;
            speedValue.text = stats.speed.ToString();
            moistureValue.text = stats.moisture.ToString();
            toughnessValue.text = stats.toughness.ToString();
            if (campValue != null) campValue.text = stats.morality.ToString();
            UpdateCampIcon(stats.morality);
        }

        void UpdateCampIcon(int morality)
        {
            if (campIcon == null) return;
            var sprite = morality > 0 ? campPositiveSprite : morality < 0 ? campNegativeSprite : campNeutralSprite;
            if (sprite != null && campIcon.sprite != sprite) campIcon.sprite = sprite;
        }

        /// <summary>重新開始時呼叫，避免數值重設被當成一次變化。</summary>
        public void ResetState()
        {
            hasShown = false;
            for (int i = 0; i < punch.Length; i++) punch[i] = 0f;
        }

        public void SetRunningVisible(bool visible)
        {
            if (runningRoot.activeSelf != visible) runningRoot.SetActive(visible);
        }

        /// <summary>旅程紀錄：最新的一行最亮，越舊越淡。</summary>
        public void SetJournal(IReadOnlyList<string> lines)
        {
            journal.Clear();
            for (int i = 0; i < lines.Count; i++) journal.Add(lines[i]);
            RebuildJournal();
        }

        void RebuildJournal()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < journal.Count; i++)
            {
                int age = journal.Count - 1 - i;
                string alpha = age == 0 ? "FF" : age == 1 ? "99" : age == 2 ? "66" : "40";
                if (sb.Length > 0) sb.Append('\n');
                sb.Append("<alpha=#").Append(alpha).Append('>').Append(journal[i]);
            }
            journalLabel.text = sb.ToString();
        }
    }
}
