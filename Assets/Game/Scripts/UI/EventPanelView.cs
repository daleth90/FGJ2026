using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 事件面板：事件標題、敘事加三張選項卡。全部選項都不可選時，改顯示「無路可走」的繼續鈕。
    /// </summary>
    public class EventPanelView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text descriptionLabel;
        [SerializeField] OptionCardView[] cards;
        [SerializeField] Button deadEndButton;

        Action onDeadEnd;

        void Awake()
        {
            deadEndButton.onClick.AddListener(() => onDeadEnd?.Invoke());
        }

        public void Show(EventDefinition e, OptionCheck[] checks, Action<int> onChoose, Action onDeadEnd)
        {
            this.onDeadEnd = onDeadEnd;
            root.SetActive(true);

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
                cards[i].Setup(e.options[i], checks[i], () => onChoose?.Invoke(index));
            }

            deadEndButton.gameObject.SetActive(!anyAvailable);
        }

        public void Hide() => root.SetActive(false);
    }
}
