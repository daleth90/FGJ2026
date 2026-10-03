using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 舞台：一張會向左循環捲動的背景圖，加上站在原地跳動的主角。
    /// 背景：把圖拖到 Background 的 RawImage.Texture。圖會以高度貼齊畫面、寬度照比例，左右接縫要畫成能接起來的。
    /// 主角：換 Character 的 Image.Sprite。
    /// 速度只影響捲動快慢，不影響任何規則。
    /// 其他背景物件（Sky、FarLayer、Ground、Turf、NearLayer）目前在場景裡是關閉的，打開後這裡會照舊讓它們動。
    /// </summary>
    public class StageView : MonoBehaviour
    {
        [Header("背景")]
        [SerializeField] RawImage background;
        [Tooltip("背景捲動速度的倍率。1 = 和下面設定的速度一樣。")]
        [SerializeField] float backgroundFactor = 1f;

        [Header("捲動速度（畫布單位／秒）")]
        [SerializeField] float baseScrollSpeed = 260f;
        [SerializeField] float scrollPerSpeed = 14f;
        [SerializeField] float maxScrollSpeed = 900f;

        [Header("主角")]
        [SerializeField] RectTransform character;
        [SerializeField] float hopHeight = 22f;
        [SerializeField] float hopsPerSecond = 2.4f;

        [Header("目前關閉的背景物件")]
        [SerializeField] RectTransform[] farItems;
        [SerializeField] RectTransform[] nearItems;
        [Tooltip("物件完全捲出左邊後，往右搬多遠再回來。要比畫面寬。")]
        [SerializeField] float loopWidth = 2600f;
        [Tooltip("遠景的捲動速度是近景的幾倍。")]
        [SerializeField] float farFactor = 0.25f;

        int speedStat;
        bool moving;
        float timeScale = 1f;

        float motion;   // 0 = 停下，1 = 奔跑中
        float backgroundOffset;
        float stride;
        float characterBaseY;

        void Awake()
        {
            if (character != null) characterBaseY = character.anchoredPosition.y;
        }

        /// <summary>每幀由 RunController 餵入目前狀態。</summary>
        public void SetState(int speedStat, bool moving, float timeScale)
        {
            this.speedStat = speedStat;
            this.moving = moving;
            this.timeScale = timeScale;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            motion = Mathf.MoveTowards(motion, moving ? 1f : 0f, dt * 3f);

            float speed = Mathf.Min(maxScrollSpeed, baseScrollSpeed + scrollPerSpeed * speedStat);
            float distance = speed * motion * Mathf.Min(timeScale, 3f) * dt;

            ScrollBackground(distance * backgroundFactor);
            Scroll(farItems, distance * farFactor);
            Scroll(nearItems, distance);

            if (character != null && character.gameObject.activeInHierarchy)
            {
                stride += dt * motion * Mathf.Min(timeScale, 3f) * hopsPerSecond * Mathf.PI;
                var position = character.anchoredPosition;
                position.y = characterBaseY + Mathf.Abs(Mathf.Sin(stride)) * hopHeight * motion;
                character.anchoredPosition = position;
            }
        }

        void ScrollBackground(float distance)
        {
            if (background == null) return;

            var rect = background.rectTransform.rect;
            if (rect.height <= 0f) return;

            var texture = background.texture;
            float aspect = rect.width / rect.height;
            if (texture != null && texture.height > 0)
            {
                aspect = (float)texture.width / texture.height;
                // 循環捲動靠的是貼圖重複取樣；匯入設定是 Clamp 的話邊緣會被拉長，所以這裡直接改掉。
                if (texture.wrapMode != TextureWrapMode.Repeat) texture.wrapMode = TextureWrapMode.Repeat;
            }

            // 一張圖在畫面上佔的寬度：高度貼齊畫面，寬度照圖的比例。
            float tileWidth = rect.height * aspect;
            backgroundOffset = Mathf.Repeat(backgroundOffset + distance / tileWidth, 1f);
            background.uvRect = new Rect(backgroundOffset, 0f, rect.width / tileWidth, 1f);
        }

        void Scroll(RectTransform[] items, float distance)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                if (item == null || !item.gameObject.activeInHierarchy) continue;
                var position = item.anchoredPosition;
                position.x -= distance;
                if (position.x + item.rect.width < 0f) position.x += loopWidth;
                item.anchoredPosition = position;
            }
        }
    }
}
