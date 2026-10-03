using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 舞台：一張會向左循環捲動的背景圖，加上站在原地跳動的主角。
    /// 背景：把圖拖到 Background 的 RawImage.Texture。圖會以高度貼齊畫面、寬度照比例，左右接縫要畫成能接起來的。
    /// 主角：把走路的圖依序拖進 Character Frames，跑的時候會輪播；停下時顯示第一張。
    /// 速度只影響捲動快慢，不影響任何規則。
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
        [Tooltip("走路動畫的圖，依播放順序排。第一張同時是停下時顯示的圖。")]
        [SerializeField] Sprite[] characterFrames;
        [SerializeField] float framesPerSecond = 8f;

        int speedStat;
        bool moving;
        float timeScale = 1f;

        float motion;   // 0 = 停下，1 = 奔跑中
        float backgroundOffset;
        float stride;
        float characterBaseY;
        Image characterImage;
        float frameTime;

        void Awake()
        {
            if (character == null) return;
            characterBaseY = character.anchoredPosition.y;
            characterImage = character.GetComponent<Image>();
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

            if (character != null && character.gameObject.activeInHierarchy)
            {
                stride += dt * motion * Mathf.Min(timeScale, 3f) * hopsPerSecond * Mathf.PI;
                var position = character.anchoredPosition;
                position.y = characterBaseY + Mathf.Abs(Mathf.Sin(stride)) * hopHeight * motion;
                character.anchoredPosition = position;

                if (characterImage != null && characterFrames != null && characterFrames.Length > 0)
                {
                    // 停下時回到第一張，再起跑時從頭播。
                    frameTime = motion > 0f ? frameTime + dt * motion * Mathf.Min(timeScale, 3f) * framesPerSecond : 0f;
                    characterImage.sprite = characterFrames[(int)frameTime % characterFrames.Length];
                }
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
    }
}
