using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    public enum CodexSection
    {
        Titles = 0,
        Forms = 1,
    }

    /// <summary>圖鑑裡的一個項目（顯示用資料）。</summary>
    public class CodexEntry
    {
        public CodexSection section;
        public int id;
        public Sprite image;
        public Sprite overlay;
        public string name;
        public string description;
        public string condition;
    }

    /// <summary>
    /// 圖鑑本體：一本書兩個分頁，先排完稱號頁、再排型態頁，左右方向鍵連續翻。
    /// 每頁固定格數（預設 3×3），格子由 slot prefab 產生在 slotContainer 底下（排版交給 Grid Layout Group）。
    /// 被動 View：解鎖狀態從 CodexStorage 讀，不自己改。
    /// </summary>
    public class CodexView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] RectTransform slotContainer;
        [SerializeField] CodexSlotView slotPrefab;
        [SerializeField, Min(1)] int slotsPerPage = 9;
        [SerializeField] CodexDetailView detail;

        [Header("翻頁按鈕（第一頁不顯示左鈕、最後一頁不顯示右鈕）")]
        [SerializeField] Button leftButton;
        [SerializeField] Button rightButton;

        [Header("文字（可留空）")]
        [SerializeField] TMP_Text sectionLabel;
        [SerializeField] TMP_Text pageLabel;

        [Header("資料")]
        [SerializeField] TitleTable titleTable;
        [SerializeField] CharacterSpriteSet characterSprites;
        [SerializeField] CodexFormTable formTable;

        readonly List<CodexEntry> titleEntries = new List<CodexEntry>();
        readonly List<CodexEntry> formEntries = new List<CodexEntry>();
        readonly List<CodexSlotView> slots = new List<CodexSlotView>();
        int titlePages;
        int formPages;
        int page;

        int PageCount => titlePages + formPages;

        void Awake()
        {
            BuildEntries();
            for (int i = 0; i < slotsPerPage; i++)
                slots.Add(Instantiate(slotPrefab, slotContainer));
            if (leftButton != null) leftButton.onClick.AddListener(() => TurnPage(-1));
            if (rightButton != null) rightButton.onClick.AddListener(() => TurnPage(1));
        }

        public void Show()
        {
            root.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (detail != null) detail.Hide();
            root.SetActive(false);
        }

        public void TurnPage(int delta)
        {
            if (PageCount == 0) return;
            page = Mathf.Clamp(page + delta, 0, PageCount - 1);
            Refresh();
        }

        public void Refresh()
        {
            if (detail != null) detail.Hide();

            bool titles = page < titlePages;
            var entries = titles ? titleEntries : formEntries;
            int pageInSection = titles ? page : page - titlePages;
            var progress = CodexStorage.Progress;

            for (int i = 0; i < slots.Count; i++)
            {
                int index = pageInSection * slotsPerPage + i;
                bool exists = index < entries.Count;
                slots[i].gameObject.SetActive(exists);
                if (!exists) continue;

                var entry = entries[index];
                bool unlocked = entry.section == CodexSection.Titles
                    ? progress.IsTitleUnlocked(entry.id)
                    : progress.IsFormUnlocked(entry.id);
                slots[i].Setup(entry, unlocked, () => ShowDetail(entry));
            }

            if (leftButton != null) leftButton.gameObject.SetActive(page > 0);
            if (rightButton != null) rightButton.gameObject.SetActive(page < PageCount - 1);

            if (sectionLabel != null) sectionLabel.text = titles ? "稱號圖鑑" : "型態圖鑑";
            if (pageLabel != null)
            {
                int sectionPages = titles ? titlePages : formPages;
                pageLabel.text = $"{pageInSection + 1} / {Mathf.Max(1, sectionPages)}";
            }
        }

        void ShowDetail(CodexEntry entry)
        {
            if (detail != null) detail.Show(entry);
        }

        void BuildEntries()
        {
            titleEntries.Clear();
            formEntries.Clear();

            if (titleTable != null)
            {
                AddTitles(titleTable.titles);
                AddTitles(titleTable.specialEndingTitles);
            }

            foreach (var form in CodexRules.FormEntries())
            {
                var info = formTable != null ? formTable.Find(form.appearance) : null;
                formEntries.Add(new CodexEntry
                {
                    section = CodexSection.Forms,
                    id = form.id,
                    image = FirstFrame(characterSprites != null ? characterSprites.GetFrames(form.appearance) : null),
                    overlay = DecorationFrame(form.decoration),
                    name = GameText.CodexFormName(
                        info != null && !string.IsNullOrEmpty(info.displayName) ? info.displayName : form.appearance.ToString(),
                        form.decoration),
                    description = info != null ? info.description : string.Empty,
                    condition = WithDecorationCondition(info != null ? info.conditionDescription : string.Empty, form.decoration),
                });
            }

            titlePages = PagesFor(titleEntries.Count);
            formPages = PagesFor(formEntries.Count);
        }

        void AddTitles(TitleDefinition[] titles)
        {
            if (titles == null) return;
            foreach (var title in titles)
            {
                if (title == null) continue;
                titleEntries.Add(new CodexEntry
                {
                    section = CodexSection.Titles,
                    id = title.titleId,
                    image = title.image,
                    name = title.titleName,
                    description = title.description,
                    condition = title.conditionDescription,
                });
            }
        }

        static string WithDecorationCondition(string condition, MoralityDecoration decoration)
        {
            string extra = decoration == MoralityDecoration.Angel ? $"善良 ≥ {RunRules.AngelMoralityThreshold}"
                : decoration == MoralityDecoration.Devil ? $"善良 ≤ {RunRules.DevilMoralityThreshold}"
                : null;
            if (extra == null) return condition;
            return string.IsNullOrEmpty(condition) ? extra : condition + "\n" + extra;
        }

        Sprite DecorationFrame(MoralityDecoration decoration)
        {
            if (characterSprites == null) return null;
            switch (decoration)
            {
                case MoralityDecoration.Angel: return FirstFrame(characterSprites.AngelFrames);
                case MoralityDecoration.Devil: return FirstFrame(characterSprites.DevilFrames);
                default: return null;
            }
        }

        static Sprite FirstFrame(Sprite[] frames) => frames != null && frames.Length > 0 ? frames[0] : null;

        int PagesFor(int count) => count == 0 ? 0 : (count + slotsPerPage - 1) / slotsPerPage;
    }
}
