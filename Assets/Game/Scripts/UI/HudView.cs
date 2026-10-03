using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace GrassRun
{
    /// <summary>
    /// 常駐資訊：三項數值、旅程紀錄。
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [Header("數值")]
        [SerializeField] TMP_Text speedValue;
        [SerializeField] TMP_Text moistureValue;
        [SerializeField] TMP_Text toughnessValue;
        [SerializeField] RectTransform speedChip;
        [SerializeField] RectTransform moistureChip;
        [SerializeField] RectTransform toughnessChip;

        [Header("其他")]
        [Tooltip("只在奔跑時顯示的東西（旅程紀錄、操作說明）。事件面板開著時會被蓋住，所以直接藏起來。")]
        [SerializeField] GameObject runningRoot;
        [SerializeField] TMP_Text journalLabel;
        [SerializeField] GameObject designerRoot;
        [SerializeField] TMP_Text designerLabel;

        const float PunchDecay = 4f;
        const float PunchScale = 0.3f;

        readonly float[] punch = new float[StatBlock.StatCount];
        StatBlock shown;
        bool hasShown;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            ApplyPunch(speedChip, 0, dt);
            ApplyPunch(moistureChip, 1, dt);
            ApplyPunch(toughnessChip, 2, dt);
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
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                int age = lines.Count - 1 - i;
                string alpha = age == 0 ? "FF" : age == 1 ? "99" : age == 2 ? "66" : "40";
                if (i > 0) sb.Append('\n');
                sb.Append("<alpha=#").Append(alpha).Append('>').Append(lines[i]);
            }
            journalLabel.text = sb.ToString();
        }

        public void SetDesigner(bool on, string text)
        {
            designerRoot.SetActive(on);
            if (on) designerLabel.text = text;
        }
    }
}
