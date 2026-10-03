using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace GrassRun
{
    /// <summary>
    /// 常駐資訊：三數值、容量條、區域與預告、檢驗點倒數、旅程紀錄。
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

        [Header("容量")]
        [SerializeField] GameObject capacityRoot;
        [SerializeField] RectTransform speedSegment;
        [SerializeField] RectTransform moistureSegment;
        [SerializeField] RectTransform toughnessSegment;
        [SerializeField] TMP_Text capacityLabel;

        [Header("區域與時間")]
        [SerializeField] TMP_Text zoneLabel;
        [SerializeField] TMP_Text nextZoneLabel;
        [SerializeField] TMP_Text checkpointLabel;
        [SerializeField] TMP_Text runTimeLabel;

        [Header("區域橫幅")]
        [SerializeField] CanvasGroup zoneBanner;
        [SerializeField] TMP_Text zoneBannerTitle;
        [SerializeField] TMP_Text zoneBannerBody;
        [SerializeField] float bannerDuration = 3.5f;

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
        float bannerTimer;

        void Awake()
        {
            if (zoneBanner != null) zoneBanner.alpha = 0f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            ApplyPunch(speedChip, 0, dt);
            ApplyPunch(moistureChip, 1, dt);
            ApplyPunch(toughnessChip, 2, dt);

            if (zoneBanner != null && bannerTimer > 0f)
            {
                bannerTimer -= dt;
                float elapsed = bannerDuration - bannerTimer;
                zoneBanner.alpha = Mathf.Clamp01(Mathf.Min(elapsed / 0.4f, bannerTimer / 0.8f));
            }
        }

        void ApplyPunch(RectTransform chip, int index, float dt)
        {
            if (chip == null) return;
            punch[index] = Mathf.MoveTowards(punch[index], 0f, PunchDecay * dt);
            chip.localScale = Vector3.one * (1f + PunchScale * punch[index]);
        }

        public void Render(StatBlock stats, int capacity, bool useCapacity, ZoneType zone, ZoneType nextZone,
            float secondsToCheckpoint, float runTime)
        {
            if (hasShown)
            {
                for (int i = 0; i < StatBlock.StatCount; i++)
                    if (stats[(StatType)i] != shown[(StatType)i]) punch[i] = 1f;
            }
            shown = stats;
            hasShown = true;

            speedValue.text = stats.speed.ToString();
            moistureValue.text = stats.moisture.ToString();
            toughnessValue.text = stats.toughness.ToString();

            capacityRoot.SetActive(useCapacity);
            if (useCapacity)
            {
                float cap = Mathf.Max(1, capacity);
                float a = stats.speed / cap;
                float b = a + stats.moisture / cap;
                float c = b + stats.toughness / cap;
                SetSegment(speedSegment, 0f, a);
                SetSegment(moistureSegment, a, b);
                SetSegment(toughnessSegment, b, c);
                capacityLabel.text = $"養分 {stats.Total} / {capacity}";
            }

            zoneLabel.text = GameText.ZoneName(zone);
            nextZoneLabel.text = $"下一區：{GameText.ZoneName(nextZone)}";
            checkpointLabel.text = $"檢驗點 {GameText.FormatTime(Mathf.Ceil(secondsToCheckpoint))}";
            runTimeLabel.text = $"已奔跑 {GameText.FormatTime(runTime)}";
        }

        static void SetSegment(RectTransform segment, float from, float to)
        {
            segment.anchorMin = new Vector2(Mathf.Clamp01(from), 0f);
            segment.anchorMax = new Vector2(Mathf.Clamp01(to), 1f);
            segment.offsetMin = Vector2.zero;
            segment.offsetMax = Vector2.zero;
        }

        /// <summary>重新開始時呼叫，避免數值重設被當成一次變化。</summary>
        public void ResetState()
        {
            hasShown = false;
            for (int i = 0; i < punch.Length; i++) punch[i] = 0f;
            bannerTimer = 0f;
            if (zoneBanner != null) zoneBanner.alpha = 0f;
        }

        public void ShowZoneBanner(ZoneType zone)
        {
            if (zoneBanner == null) return;
            zoneBannerTitle.text = GameText.ZoneName(zone);
            zoneBannerBody.text = GameText.ZoneIntro(zone);
            bannerTimer = bannerDuration;
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
