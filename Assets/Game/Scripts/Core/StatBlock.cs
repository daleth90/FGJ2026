using System;

namespace GrassRun
{
    [Serializable]
    public struct StatBlock
    {
        public const int StatCount = 4;

        public int morality;
        public int moisture;
        public int speed;
        public int toughness;

        public StatBlock(int morality, int moisture, int speed, int toughness)
        {
            this.morality = morality;
            this.moisture = moisture;
            this.speed = speed;
            this.toughness = toughness;
        }

        /// <summary>世界表現沿用的三項身體數值總和；善良不屬於身體能力。</summary>
        public int Total => moisture + speed + toughness;

        public int this[StatType stat]
        {
            get
            {
                switch (stat)
                {
                    case StatType.Morality: return morality;
                    case StatType.Moisture: return moisture;
                    case StatType.Speed: return speed;
                    case StatType.Toughness: return toughness;
                    default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
                }
            }
            set
            {
                switch (stat)
                {
                    case StatType.Morality: morality = value; break;
                    case StatType.Moisture: moisture = value; break;
                    case StatType.Speed: speed = value; break;
                    case StatType.Toughness: toughness = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
                }
            }
        }

        public override string ToString() => $"{morality}/{moisture}/{speed}/{toughness}";
    }
}
