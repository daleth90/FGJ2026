using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GrassRun
{
    /// <summary>
    /// 舞台：一張會向左循環捲動的背景圖，加上站在原地跳動的主角。
    /// 背景：在 Stage 底下放 UI Image（指定 Sprite），拖進 Backgrounds 清單，每層可以設自己的捲動倍率（做視差）。
    /// 圖的大小、位置照 Image 的 RectTransform；遠景放在 Hierarchy 上面、主角放最下面才會畫在最前。
    /// 每層會在執行時自動複製成一整排、無縫循環往左捲，所以圖的左右接縫要畫成能接起來的。
    /// 主角：把走路的圖依序拖進 Character Frames，跑的時候會輪播；停下時顯示第一張。
    /// 速度只影響捲動快慢，不影響任何規則。
    /// </summary>
    public class StageView : MonoBehaviour
    {
        [Serializable]
        class BackgroundLayer
        {
            public Image image;
            [Tooltip("這一層捲動速度的倍率。1 = 和下面設定的速度一樣；遠景可以設小一點做視差。")]
            public float factor = 1f;

            [NonSerialized] public readonly List<Transform> tiles = new List<Transform>();
            [NonSerialized] public float baseX;
            [NonSerialized] public float offset;
            [NonSerialized] public float tileWidth;
            [NonSerialized] public int firstIndex;   // tiles[0] 是原本那張圖往左數第幾格（≤ 0）
        }

        [Header("背景")]
        [SerializeField] BackgroundLayer[] backgrounds;

        [Header("捲動速度（畫布單位／秒）")]
        [SerializeField] float baseScrollSpeed = 260f;
        [SerializeField] float scrollPerSpeed = 14f;
        [SerializeField] float maxScrollSpeed = 900f;

        [Header("主角")]
        [SerializeField] RectTransform character;
        [SerializeField] float hopHeight = 22f;
        [SerializeField] float hopsPerSecond = 2.4f;
        [Tooltip("五種角色狀態的走路動畫集合。每組第一張同時是停下時顯示的圖。")]
        [SerializeField] CharacterSpriteSet characterSprites;
        [SerializeField] float framesPerSecond = 8f;
        [Tooltip("疊在主角上的天使裝飾（Character 的子物件），和走路動畫同步換格。")]
        [SerializeField] Image angelImage;
        [Tooltip("疊在主角上的惡魔裝飾（Character 的子物件），和走路動畫同步換格。")]
        [SerializeField] Image devilImage;

        int speedStat;
        bool moving;
        float timeScale = 1f;

        float motion;   // 0 = 停下，1 = 奔跑中
        float tiledWidth;   // 上次排背景時 Stage 的寬度；畫面寬度變了就重排
        float stride;
        float characterBaseY;
        Image characterImage;
        float frameTime;
        Sprite[] activeCharacterFrames;
        MoralityDecoration decoration;

        void Awake()
        {
            if (character == null) return;
            characterBaseY = character.anchoredPosition.y;
            characterImage = character.GetComponent<Image>();
            activeCharacterFrames = GetCharacterFrames(CharacterAppearance.Default);
            SetMoralityDecoration(MoralityDecoration.None);
        }

        /// <summary>由 RunController 依善良值決定顯示天使、惡魔或都不顯示。</summary>
        public void SetMoralityDecoration(MoralityDecoration value)
        {
            decoration = value;
            ShowDecoration(angelImage, AngelFrames, value == MoralityDecoration.Angel);
            ShowDecoration(devilImage, DevilFrames, value == MoralityDecoration.Devil);
        }

        void ShowDecoration(Image image, Sprite[] frames, bool visible)
        {
            if (image == null) return;
            visible &= frames != null && frames.Length > 0;
            image.gameObject.SetActive(visible);
            if (visible) image.sprite = frames[CurrentFrameIndex(frames.Length)];
        }

        Sprite[] AngelFrames => characterSprites != null ? characterSprites.AngelFrames : null;
        Sprite[] DevilFrames => characterSprites != null ? characterSprites.DevilFrames : null;

        int CurrentFrameIndex(int frameCount) => frameCount > 0 ? (int)frameTime % frameCount : 0;

        /// <summary>每幀由 RunController 餵入目前狀態。</summary>
        public void SetState(int speedStat, bool moving, float timeScale)
        {
            this.speedStat = speedStat;
            this.moving = moving;
            this.timeScale = timeScale;
        }

        public void SetCharacterAppearance(CharacterAppearance appearance)
        {
            Sprite[] nextFrames = GetCharacterFrames(appearance);
            if ((nextFrames == null || nextFrames.Length == 0) && appearance != CharacterAppearance.Default)
                nextFrames = GetCharacterFrames(CharacterAppearance.Default);
            if (ReferenceEquals(activeCharacterFrames, nextFrames)) return;

            activeCharacterFrames = nextFrames;
            frameTime = 0f;
            if (characterImage != null && activeCharacterFrames != null && activeCharacterFrames.Length > 0)
                characterImage.sprite = activeCharacterFrames[0];
        }

        Sprite[] GetCharacterFrames(CharacterAppearance appearance) =>
            characterSprites != null ? characterSprites.GetFrames(appearance) : null;

        void Update()
        {
            float dt = Time.deltaTime;
            motion = Mathf.MoveTowards(motion, moving ? 1f : 0f, dt * 3f);

            float speed = Mathf.Min(maxScrollSpeed, baseScrollSpeed + scrollPerSpeed * speedStat);
            float distance = speed * motion * Mathf.Min(timeScale, 3f) * dt;

            ScrollBackgrounds(distance);

            if (character != null && character.gameObject.activeInHierarchy)
            {
                stride += dt * motion * Mathf.Min(timeScale, 3f) * hopsPerSecond * Mathf.PI;
                var position = character.anchoredPosition;
                position.y = characterBaseY + Mathf.Abs(Mathf.Sin(stride)) * hopHeight * motion;
                character.anchoredPosition = position;

                if (characterImage != null && activeCharacterFrames != null && activeCharacterFrames.Length > 0)
                {
                    // 停下時回到第一張，再起跑時從頭播。
                    frameTime = motion > 0f ? frameTime + dt * motion * Mathf.Min(timeScale, 3f) * framesPerSecond : 0f;
                    characterImage.sprite = activeCharacterFrames[CurrentFrameIndex(activeCharacterFrames.Length)];
                }

                // 天使／惡魔和走路動畫同一格。
                if (decoration == MoralityDecoration.Angel) ShowDecoration(angelImage, AngelFrames, true);
                else if (decoration == MoralityDecoration.Devil) ShowDecoration(devilImage, DevilFrames, true);
            }
        }

        void ScrollBackgrounds(float distance)
        {
            if (backgrounds == null || backgrounds.Length == 0) return;

            var view = ((RectTransform)transform).rect;
            if (view.width <= 0f) return;
            if (!Mathf.Approximately(view.width, tiledWidth))
            {
                tiledWidth = view.width;
                foreach (var layer in backgrounds) BuildTiles(layer, view);
            }

            foreach (var layer in backgrounds)
            {
                if (layer.tileWidth <= 0f) continue;
                layer.offset = Mathf.Repeat(layer.offset + distance * layer.factor, layer.tileWidth);
                for (int k = 0; k < layer.tiles.Count; k++)
                {
                    var position = layer.tiles[k].localPosition;
                    position.x = layer.baseX + (k + layer.firstIndex) * layer.tileWidth - layer.offset;
                    layer.tiles[k].localPosition = position;
                }
            }
        }

        /// <summary>把這層的圖複製成一整排，排到能蓋滿 Stage 的寬度（含捲動時多出的一格）。</summary>
        void BuildTiles(BackgroundLayer layer, Rect view)
        {
            var source = layer.image;
            if (source == null) return;

            var sourceTransform = source.rectTransform;
            if (layer.tiles.Count == 0) layer.baseX = sourceTransform.localPosition.x;

            // 圖在 Stage 座標裡的寬度與左緣（相對於 pivot）。
            float scaleX = Mathf.Abs(sourceTransform.localScale.x);
            var rect = sourceTransform.rect;
            layer.tileWidth = rect.width * scaleX;
            if (layer.tileWidth <= 0f) return;
            float left = rect.xMin * scaleX;

            int first = Mathf.Min(0, Mathf.FloorToInt((view.xMin - layer.baseX - left) / layer.tileWidth));
            int last = Mathf.Max(0, Mathf.CeilToInt((view.xMax - layer.baseX - left) / layer.tileWidth));
            layer.firstIndex = first;

            // 原本那張圖留著當第 0 格，其他的用複製的；先清掉之前多複製的。
            for (int k = 0; k < layer.tiles.Count; k++)
                if (layer.tiles[k] != sourceTransform) Destroy(layer.tiles[k].gameObject);
            layer.tiles.Clear();

            for (int k = first; k <= last; k++)
            {
                var tile = k == 0
                    ? sourceTransform
                    : Instantiate(source.gameObject, sourceTransform.parent).transform;
                if (k != 0)
                {
                    tile.name = source.name + " (" + k + ")";
                    // 複製的排在原圖旁邊，維持同一層的前後順序（不會蓋到主角）。
                    tile.SetSiblingIndex(sourceTransform.GetSiblingIndex() + 1);
                }
                layer.tiles.Add(tile);
            }
        }
    }
}
