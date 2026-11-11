public enum RollBias { Random, Min, Max }

public static class RuneRoller
{
    public static RuneInstance Roll(RuneData def, System.Random rng = null, RollBias bias = RollBias.Random)
    {
        if(def == null) return null;
        rng ??= new System.Random();

        var instance = new RuneInstance { instanceId = LootRoller.NewId(), runeId = def.id };

        if (def.kind == RuneKind.Stat)
        {
            foreach (var line in def.statLines)
            {
                instance.rolls.Add(new RollValue { id = line.Id, value = Pick(line.range, rng, bias) });
            }
        }
        else
        {
            instance.rolls.Add(new RollValue { id = RuneData.DamagePerTickId, value = Pick(def.damagePerTick, rng, bias) });
            instance.rolls.Add(new RollValue { id = RuneData.DurationId, value = Pick(def.duration, rng, bias) });
        }
        
        return instance;
    }

    private static float Pick(RollRange range, System.Random rng, RollBias bias)
    {
        switch (bias)
        {
            case RollBias.Min: return range.min;
            case RollBias.Max: return range.max;
            default: return range.Roll(rng);
        }
    }
}