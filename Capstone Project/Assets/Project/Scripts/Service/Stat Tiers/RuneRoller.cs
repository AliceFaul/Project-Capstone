public static class RuneRoller
{
    public static RuneInstance Roll(RuneData def, System.Random rng = null)
    {
        if(def == null) return null;
        rng ??= new System.Random();

        var instance = new RuneInstance { instanceId = LootRoller.NewId(), runeId = def.id };

        if (def.kind == RuneKind.Stat)
        {
            foreach (var line in def.statLines)
            {
                instance.rolls.Add(new RollValue { id = line.Id, value = line.range.Roll(rng) });
            }
        }
        else
        {
            instance.rolls.Add(new RollValue { id = RuneData.DamagePerTickId, value = def.damagePerTick.Roll(rng) });
            instance.rolls.Add(new RollValue { id = RuneData.DurationId, value = def.duration.Roll(rng) });
        }
        
        return instance;
    }
}