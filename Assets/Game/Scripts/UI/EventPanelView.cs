using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 事件面板：情境敘事加三張選項卡。全部選項都不可選時，改顯示「無路可走」的繼續鈕。
    /// </summary>
    public class EventPanelView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Image kindTag;
        [SerializeField] TMP_Text kindLabel;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text descriptionLabel;
        [SerializeField] OptionCardView[] cards;
        [SerializeField] Button deadEndButton;

        [Header("顏色")]
        [SerializeField] Color randomColor = new Color(0.56f, 0.69f, 0.48f);
        [SerializeField] Color checkpointColor = new Color(0.90f, 0.65f, 0.27f);

        Action onDeadEnd;

        void Awake()
        {
            deadEndButton.onClick.AddListener(() => onDeadEnd?.Invoke());
        }

        public void Show(EventDefinition e, OptionCheck[] checks, string[] lockTexts, string[] designerTexts,
            bool designer, Action<int> onChoose, Action onDeadEnd)
        {
            this.onDeadEnd = onDeadEnd;
            root.SetActive(true);

            kindLabel.text = "事件";
            kindTag.color = randomColor;
            titleLabel.text = e.DisplayTitle;
            descriptionLabel.text = e.description;

            bool anyAvailable = false;
            for (int i = 0; i < cards.Length; i++)
            {
                bool exists = i < e.options.Length;
                cards[i].gameObject.SetActive(exists);
                if (!exists) continue;

                int index = i;
                anyAvailable |= checks[i].available;
                cards[i].Setup(i, e.options[i], checks[i].available, lockTexts[i], designerTexts[i], designer,
                    () => onChoose?.Invoke(index));
            }

            deadEndButton.gameObject.SetActive(!anyAvailable);
        }

        public void SetDesigner(bool on)
        {
            foreach (var card in cards) card.SetDesigner(on);
        }

        public void Hide() => root.SetActive(false);
    }
}
