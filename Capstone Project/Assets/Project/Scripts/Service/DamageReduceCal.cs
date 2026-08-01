public class DamageReduceCal
{
    // Calculate final damage after reduce by defense (for player and enemy)
    public float Calculate(float rawDamage, float rawDefense)
    {
        float finalDamage = rawDamage * (100f / (100f + rawDefense));
        return finalDamage;
    }
}