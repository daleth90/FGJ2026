using UnityEngine;

namespace GrassRun
{
    [CreateAssetMenu(menuName = "GrassRun/Character Sprite Set", fileName = "CharacterSpriteSet")]
    public class CharacterSpriteSet : ScriptableObject
    {
        [SerializeField] Sprite[] defaultFrames;
        [Tooltip("天使裝飾（善良值 ≥ 20），疊在主角上面，和走路動畫同步換格。")]
        [SerializeField] Sprite[] angelFrames;
        [Tooltip("惡魔裝飾（善良值 ≤ -10），疊在主角上面，和走路動畫同步換格。")]
        [SerializeField] Sprite[] devilFrames;
        [SerializeField] Sprite[] toughnessFrames;
        [SerializeField] Sprite[] speedFrames;
        [SerializeField] Sprite[] wetFrames;
        [SerializeField] Sprite[] dryFrames;
        [Tooltip("乾枯且速度高（濕度 ≤ 25 且 速度 ≥ 8）。")]
        [SerializeField] Sprite[] drySpeedFrames;
        [Tooltip("濕潤且韌性高（濕度 > 60 且 韌性 ≥ 8）。")]
        [SerializeField] Sprite[] wetToughnessFrames;
        [Tooltip("濕潤、韌性高且速度高（濕度 > 60 且 韌性 ≥ 8 且 速度 ≥ 8）。")]
        [SerializeField] Sprite[] wetToughnessSpeedFrames;
        [Tooltip("乾枯、韌性高且速度高（濕度 ≤ 25 且 韌性 ≥ 8 且 速度 ≥ 8）。")]
        [SerializeField] Sprite[] dryToughnessSpeedFrames;
        [Tooltip("乾枯且韌性高、速度不高（濕度 ≤ 25 且 韌性 ≥ 8 且 速度 < 8）。")]
        [SerializeField] Sprite[] dryToughnessFrames;
        [Tooltip("韌性和速度都高、濕度一般（濕度 26～60 且 韌性 ≥ 8 且 速度 ≥ 8）。")]
        [SerializeField] Sprite[] toughnessSpeedFrames;
        [Tooltip("濕潤且速度高（速度 > 10 且 濕度 ≥ 70 且 韌性 < 8）。")]
        [SerializeField] Sprite[] wetSpeedFrames;

        public Sprite[] AngelFrames => angelFrames;
        public Sprite[] DevilFrames => devilFrames;

        public Sprite[] GetFrames(CharacterAppearance appearance)
        {
            switch (appearance)
            {
                case CharacterAppearance.Toughness: return toughnessFrames;
                case CharacterAppearance.Speed: return speedFrames;
                case CharacterAppearance.Wet: return wetFrames;
                case CharacterAppearance.Dry: return dryFrames;
                case CharacterAppearance.DrySpeed: return drySpeedFrames;
                case CharacterAppearance.WetToughness: return wetToughnessFrames;
                case CharacterAppearance.WetToughnessSpeed: return wetToughnessSpeedFrames;
                case CharacterAppearance.DryToughnessSpeed: return dryToughnessSpeedFrames;
                case CharacterAppearance.DryToughness: return dryToughnessFrames;
                case CharacterAppearance.ToughnessSpeed: return toughnessSpeedFrames;
                case CharacterAppearance.WetSpeed: return wetSpeedFrames;
                default: return defaultFrames;
            }
        }
    }
}
