using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 事件面板：事件圖片、敘事加三張選項卡。全部選項都不可選時，改顯示「無路可走」的繼續鈕。
    /// </summary>
    public class EventPanelView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] GameObject imageRoot;
        [SerializeField] UnityEngine.UI.Image eventImage;
        [SerializeField] TMP_Text descriptionLabel;
        [SerializeField] OptionCardView[] cards;
        [SerializeField] Button deadEndButton;

        [Header("進場演出")]
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] RectTransform animationRoot;
        [SerializeField, Min(0f)] float enterDuration = 0.28f;
        [SerializeField] float enterOffset = 24f;
        [SerializeField, Range(0.8f, 1f)] float enterStartScale = 0.97f;

        Action onDeadEnd;
        Coroutine entranceRoutine;
        Vector2 restingPosition;

        void Awake()
        {
            deadEndButton.onClick.AddListener(() => onDeadEnd?.Invoke());
            if (animationRoot != null) restingPosition = animationRoot.anchoredPosition;
        }

        public void Show(EventDefinition e, Sprite image, OptionCheck[] checks, Action<int> onChoose, Action onDeadEnd)
        {
            this.onDeadEnd = onDeadEnd;
            root.SetActive(true);

            eventImage.sprite = image;
            imageRoot.SetActive(image != null);
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
            PlayEntrance();
        }

        public void Hide()
        {
            StopEntrance();
            root.SetActive(false);
        }

        void PlayEntrance()
        {
            StopEntrance();

            if (canvasGroup == null || animationRoot == null || enterDuration <= 0f)
            {
                SetEntranceState(1f);
                return;
            }

            entranceRoutine = StartCoroutine(AnimateEntrance());
        }

        IEnumerator AnimateEntrance()
        {
            canvasGroup.blocksRaycasts = false;
            float elapsed = 0f;

            while (elapsed < enterDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / enterDuration);
                SetEntranceState(Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
            }

            SetEntranceState(1f);
            canvasGroup.blocksRaycasts = true;
            entranceRoutine = null;
        }

        void StopEntrance()
        {
            if (entranceRoutine != null)
            {
                StopCoroutine(entranceRoutine);
                entranceRoutine = null;
            }

            SetEntranceState(1f);
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        }

        void SetEntranceState(float progress)
        {
            if (canvasGroup != null) canvasGroup.alpha = progress;
            if (animationRoot == null) return;

            animationRoot.anchoredPosition = restingPosition + Vector2.down * enterOffset * (1f - progress);
            float scale = Mathf.Lerp(enterStartScale, 1f, progress);
            animationRoot.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
