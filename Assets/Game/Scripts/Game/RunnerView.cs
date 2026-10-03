using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrassRun
{
    /// <summary>
    /// 灰盒世界：小草在左邊原地跑，背景向左捲。全部用色塊在執行期產生，沒有美術素材。
    /// 這裡只負責「看起來」——速度只影響捲動快慢，不影響任何規則。
    /// </summary>
    public class RunnerView : MonoBehaviour
    {
        [Serializable]
        public struct ZonePalette
        {
            public Color sky;
            public Color turf;
            public Color soil;
            public Color far;
            public Color near;
        }

        [SerializeField] Camera worldCamera;

        [Header("構圖")]
        [Tooltip("地平線的高度（世界座標）。下方留給事件面板。")]
        [SerializeField] float groundY = 0.5f;
        [SerializeField] float grassX = -5.5f;

        [Header("捲動（純視覺）")]
        [SerializeField] float baseScrollSpeed = 3f;
        [SerializeField] float scrollPerSpeed = 0.2f;
        [SerializeField] float maxScrollSpeed = 12f;

        [Header("區域配色")]
        [SerializeField] ZonePalette meadow = new ZonePalette
        {
            sky = new Color(0.75f, 0.89f, 0.94f), turf = new Color(0.43f, 0.67f, 0.31f),
            soil = new Color(0.17f, 0.18f, 0.14f), far = new Color(0.58f, 0.78f, 0.52f), near = new Color(0.33f, 0.56f, 0.25f),
        };
        [SerializeField] ZonePalette dry = new ZonePalette
        {
            sky = new Color(0.95f, 0.85f, 0.65f), turf = new Color(0.78f, 0.61f, 0.35f),
            soil = new Color(0.22f, 0.17f, 0.12f), far = new Color(0.86f, 0.73f, 0.50f), near = new Color(0.62f, 0.46f, 0.26f),
        };
        [SerializeField] ZonePalette wet = new ZonePalette
        {
            sky = new Color(0.60f, 0.70f, 0.75f), turf = new Color(0.25f, 0.48f, 0.38f),
            soil = new Color(0.11f, 0.16f, 0.16f), far = new Color(0.40f, 0.57f, 0.55f), near = new Color(0.18f, 0.37f, 0.31f),
        };

        const int BladeCount = 5;
        const int FarCount = 9;
        const int NearCount = 18;
        const float ColorLerpSpeed = 1.5f;

        class Prop
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public float width;
        }

        Sprite pixel;
        Transform grassRoot;
        readonly Transform[] blades = new Transform[BladeCount];
        readonly SpriteRenderer[] bladeRenderers = new SpriteRenderer[BladeCount];
        readonly float[] bladeHeights = new float[BladeCount];
        SpriteRenderer turf;
        SpriteRenderer soil;
        readonly List<Prop> farProps = new List<Prop>();
        readonly List<Prop> nearProps = new List<Prop>();
        System.Random rng;

        StatBlock stats;
        ZoneType zone;
        bool moving;
        float timeScale = 1f;

        ZonePalette current;
        float motion;      // 0 = 停下，1 = 奔跑中
        float stride;      // 跑步動畫的相位

        void Awake()
        {
            rng = new System.Random(12345);
            pixel = CreatePixelSprite();
            current = meadow;
            Build();
            ApplyColors();
        }

        /// <summary>每幀由 RunController 餵入目前狀態。</summary>
        public void SetState(StatBlock stats, ZoneType zone, bool moving, float timeScale)
        {
            this.stats = stats;
            this.zone = zone;
            this.moving = moving;
            this.timeScale = timeScale;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            motion = Mathf.MoveTowards(motion, moving ? 1f : 0f, dt * 3f);
            float speed = Mathf.Min(maxScrollSpeed, baseScrollSpeed + scrollPerSpeed * stats.speed);
            float scroll = speed * motion * Mathf.Min(timeScale, 3f) * dt;

            Scroll(farProps, scroll * 0.25f, 1.5f, 4f, 0.6f, 2.6f, 0.4f, 2.5f);
            Scroll(nearProps, scroll, 0.08f, 0.32f, 0.08f, 0.4f, 0.3f, 2.2f);

            var target = PaletteFor(zone);
            float t = dt * ColorLerpSpeed;
            current.sky = Color.Lerp(current.sky, target.sky, t);
            current.turf = Color.Lerp(current.turf, target.turf, t);
            current.soil = Color.Lerp(current.soil, target.soil, t);
            current.far = Color.Lerp(current.far, target.far, t);
            current.near = Color.Lerp(current.near, target.near, t);
            ApplyColors();

            AnimateGrass(dt, speed);
        }

        ZonePalette PaletteFor(ZoneType z)
        {
            switch (z)
            {
                case ZoneType.Dry: return dry;
                case ZoneType.Wet: return wet;
                default: return meadow;
            }
        }

        void Build()
        {
            turf = CreateBlock("Turf", transform, 1);
            turf.transform.localScale = new Vector3(80f, 0.4f, 1f);
            turf.transform.position = new Vector3(0f, groundY - 0.4f, 0f);

            soil = CreateBlock("Soil", transform, 0);
            soil.transform.localScale = new Vector3(80f, 20f, 1f);
            soil.transform.position = new Vector3(0f, groundY - 20.2f, 0f);

            float x = -HalfWidth() - 2f;
            for (int i = 0; i < FarCount; i++)
            {
                var prop = CreateProp("Far", -10);
                Resize(prop, 1.5f, 4f, 0.6f, 2.6f);
                x += prop.width * 0.5f + Range(0.4f, 2.5f);
                prop.transform.position = new Vector3(x, groundY, 0f);
                x += prop.width * 0.5f;
                farProps.Add(prop);
            }

            x = -HalfWidth() - 1f;
            for (int i = 0; i < NearCount; i++)
            {
                var prop = CreateProp("Near", 5);
                Resize(prop, 0.08f, 0.32f, 0.08f, 0.4f);
                x += Range(0.3f, 2.2f);
                prop.transform.position = new Vector3(x, groundY, 0f);
                nearProps.Add(prop);
            }

            grassRoot = new GameObject("Grass").transform;
            grassRoot.SetParent(transform, false);
            grassRoot.position = new Vector3(grassX, groundY, 0f);
            for (int i = 0; i < BladeCount; i++)
            {
                var blade = CreateBlock("Blade" + i, grassRoot, 10);
                blades[i] = blade.transform;
                bladeRenderers[i] = blade;
                bladeHeights[i] = Range(0.9f, 1.45f);
                blade.transform.localPosition = new Vector3((i - (BladeCount - 1) * 0.5f) * 0.07f, 0f, 0f);
            }
        }

        void Scroll(List<Prop> props, float distance, float minWidth, float maxWidth, float minHeight, float maxHeight,
            float minGap, float maxGap)
        {
            float half = HalfWidth();
            float rightmost = float.NegativeInfinity;
            foreach (var prop in props)
                rightmost = Mathf.Max(rightmost, prop.transform.position.x + prop.width * 0.5f);

            foreach (var prop in props)
            {
                var position = prop.transform.position;
                position.x -= distance;

                // 捲出畫面左邊後，換個大小接到最右邊。
                if (position.x + prop.width * 0.5f < -half - 0.5f)
                {
                    Resize(prop, minWidth, maxWidth, minHeight, maxHeight);
                    position.x = Mathf.Max(rightmost, half) + Range(minGap, maxGap) + prop.width * 0.5f;
                    rightmost = position.x + prop.width * 0.5f;
                }
                prop.transform.position = position;
            }
        }

        void AnimateGrass(float dt, float scrollSpeed)
        {
            // 用各數值占總量的比例來決定外觀：誰占得多，誰就顯現在身上。
            float total = Mathf.Max(1, stats.Total);
            float speedShare = Mathf.InverseLerp(0.15f, 0.6f, stats.speed / total);
            float moistureShare = Mathf.InverseLerp(0.1f, 0.6f, stats.moisture / total);
            float toughnessShare = Mathf.InverseLerp(0.15f, 0.6f, stats.toughness / total);

            stride += dt * motion * Mathf.Min(timeScale, 3f) * (4f + scrollSpeed * 0.9f);
            float hop = Mathf.Abs(Mathf.Sin(stride)) * 0.2f * motion;
            grassRoot.position = new Vector3(grassX, groundY + hop, 0f);

            // 速度：身體往前傾。溼度：從乾黃到深綠。韌度：葉片變粗。
            float lean = Mathf.Lerp(-3f, -30f, speedShare) * motion;
            float width = Mathf.Lerp(0.09f, 0.26f, toughnessShare);
            var dryColor = new Color(0.80f, 0.74f, 0.33f);
            var lushColor = new Color(0.20f, 0.62f, 0.36f);
            var color = Color.Lerp(dryColor, lushColor, moistureShare);

            for (int i = 0; i < BladeCount; i++)
            {
                float fan = (i - (BladeCount - 1) * 0.5f) * 13f;
                float sway = Mathf.Sin(Time.time * 2.2f + i * 1.3f) * 3f;
                float bounce = Mathf.Sin(stride * 2f + i) * 4f * motion;
                blades[i].localRotation = Quaternion.Euler(0f, 0f, -fan + lean + sway + bounce);
                blades[i].localScale = new Vector3(width, bladeHeights[i], 1f);
                bladeRenderers[i].color = Color.Lerp(color, Color.white, i % 2 == 0 ? 0f : 0.12f);
            }
        }

        void ApplyColors()
        {
            if (worldCamera != null) worldCamera.backgroundColor = current.sky;
            turf.color = current.turf;
            soil.color = current.soil;
            foreach (var prop in farProps) prop.renderer.color = current.far;
            foreach (var prop in nearProps) prop.renderer.color = current.near;
        }

        float HalfWidth()
        {
            if (worldCamera == null) return 9f;
            return worldCamera.orthographicSize * worldCamera.aspect;
        }

        Prop CreateProp(string name, int order)
        {
            var renderer = CreateBlock(name, transform, order);
            return new Prop { transform = renderer.transform, renderer = renderer };
        }

        void Resize(Prop prop, float minWidth, float maxWidth, float minHeight, float maxHeight)
        {
            prop.width = Range(minWidth, maxWidth);
            prop.transform.localScale = new Vector3(prop.width, Range(minHeight, maxHeight), 1f);
        }

        SpriteRenderer CreateBlock(string name, Transform parent, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = pixel;
            renderer.sortingOrder = order;
            return renderer;
        }

        float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);

        /// <summary>1×1 單位、軸心在底部中央的白色方塊。</summary>
        static Sprite CreatePixelSprite()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0f), 4f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
