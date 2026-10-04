namespace GrassRun
{
    public enum StatType
    {
        Morality = 0,
        Moisture = 1,
        Speed = 2,
        Toughness = 3,
    }

    public enum Comparison
    {
        /// <summary>數值要夠高。</summary>
        AtLeast = 0,
        /// <summary>數值要夠低（例如乾區的「溼度不能太高」）。</summary>
        AtMost = 1,
    }

    public enum ZoneType
    {
        Meadow = 0,
        Dry = 1,
        Wet = 2,
    }

    /// <summary>依善良值疊在主角身上的裝飾；不影響主角外觀。</summary>
    public enum MoralityDecoration
    {
        None = 0,
        Angel = 1,
        Devil = 2,
    }

    public enum CharacterAppearance
    {
        Default = 0,
        Toughness = 1,
        Speed = 2,
        Wet = 3,
        Dry = 4,
        DrySpeed = 5,
        WetToughness = 6,
        WetToughnessSpeed = 7,
        DryToughnessSpeed = 8,
        DryToughness = 9,
        ToughnessSpeed = 10,
    }

    public enum RunEndReason
    {
        None = 0,
        NoAvailableOption = 1,
        MoistureDepleted = 2,
        MoistureSaturated = 3,
        SpecialEnding = 4,
    }
}
