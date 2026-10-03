using UnityEngine;

namespace GrassRun
{
    [CreateAssetMenu(menuName = "GrassRun/Character Sprite Set", fileName = "CharacterSpriteSet")]
    public class CharacterSpriteSet : ScriptableObject
    {
        [SerializeField] Sprite[] defaultFrames;
        [SerializeField] Sprite[] toughnessFrames;
        [SerializeField] Sprite[] speedFrames;
        [SerializeField] Sprite[] wetFrames;
        [SerializeField] Sprite[] dryFrames;

        public Sprite[] GetFrames(CharacterAppearance appearance)
        {
            switch (appearance)
            {
                case CharacterAppearance.Toughness: return toughnessFrames;
                case CharacterAppearance.Speed: return speedFrames;
                case CharacterAppearance.Wet: return wetFrames;
                case CharacterAppearance.Dry: return dryFrames;
                default: return defaultFrames;
            }
        }
    }
}
