public class DamageReduceCal
{
    // Calculate final damage after reduce by defense (for player and enemy)
    public float Calculate(float rawDamage, float rawDefense)
    {
        float safeDefense = rawDefense < 0f ? 0f : rawDefense;
        float finalDamage = rawDamage * (100f / (100f + safeDefense));
        return finalDamage;
    }
}