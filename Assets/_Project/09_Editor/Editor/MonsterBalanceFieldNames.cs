using System.Collections.Generic;

/// <summary>
/// Serialized field names on MonsterA/B/C that are meant to be tuned per PLACED INSTANCE
/// (e.g. one placement respawns twice, another five times) and must never be swept up by
/// "Apply Monster X Settings To Prefab" - that button otherwise applies every serialized
/// field on the instance unconditionally (no per-field choice) and then also force-reverts
/// ActionTest.unity's own instance to match, so without this exclusion list, clicking Apply
/// from ANY placement of a monster - in BackgroundTest.unity or anywhere else - would wipe
/// out every other placement's individual tuning the next time that data got read.
///
/// Keeping these names out of both the apply sweep and the revert sweep (see
/// MonsterAPrefabApplyUtility/MonsterBPrefabApplyUtility/MonsterCPrefabApplyUtility) is what
/// makes per-instance balance tuning actually safe - see chat/ClassDocs (2026-08-07) for the
/// full reasoning behind this design.
/// </summary>
internal static class MonsterBalanceFieldNames
{
    public static readonly HashSet<string> PerInstanceOnly = new HashSet<string>
    {
        "detectRange",
        "maxRespawnCount",
        "respawnDelay",
        // 2026-08-24: maxHp joined the list after an Apply silently reset BackgroundTest's three
        // MonsterC placements from their tuned 1/3/2 back to the prefab's 3. Stage 1 tunes HP per
        // placement throughout (MonsterA runs 2/2/2/4/2), so difficulty pacing lived in exactly the
        // values the sweep was overwriting - the same failure this list was created to stop, just
        // on a field nobody had added yet. Consequence to keep in mind: a monster type's HP is now
        // set on each placement rather than pushed from the prefab.
        "maxHp",
    };
}
