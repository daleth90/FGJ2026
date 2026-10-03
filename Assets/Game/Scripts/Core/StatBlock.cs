using System;

namespace GrassRun
{
    [Serializable]
    public struct StatBlock
    {
        public const int StatCount = 3;

        public int speed;
        public int moisture;
        public int toughness;

        public StatBlock(int speed, int moisture, int toughness)
        {
            this.speed = speed;
            this.moisture = moisture;
            this.toughness = toughness;
        }

        public int Total => speed + moisture + toughness;

        public int this[StatType stat]
        {
            get
            {
                switch (stat)
                {
                    case StatType.Speed: return speed;
                    case StatType.Moisture: return moisture;
                    default: return toughness;
                }
            }
            set
            {
                switch (stat)
                {
                    case StatType.Speed: speed = value; break;
                    case StatType.Moisture: moisture = value; break;
                    default: toughness = value; break;
                }
            }
        }

        public override string ToString() => $"{speed}/{moisture}/{toughness}";
    }
}
