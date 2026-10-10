/// <summary>
/// Represents a single stat modification (addition or subtraction) applied to the player, 
/// originating from various sources (such as equipment, artifact buffs, etc.) within PlayerRuntime.
/// </summary>
public readonly struct StatModifier
{
    public readonly BonusStat Stat;
    public readonly float Value;

    public StatModifier(BonusStat stat, float value)
    {
        Stat = stat;
        Value = value;
    }
}