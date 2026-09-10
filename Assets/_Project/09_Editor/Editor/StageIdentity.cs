/// <summary>
/// Computes every stage-specific scene/prefab/art/audio path from a stage number, so the
/// multi-stage builder tools never hardcode a path string directly - they always ask
/// "StageIdentity.For(N).XxxPath" instead. This is the one place the "스테이지+역할번호"
/// naming convention (2026-08-17 design discussion) lives.
///
/// Stage 1 is a deliberate special case: it returns the project's ORIGINAL (pre-multi-stage)
/// paths byte-for-byte (e.g. "BackgroundTest.unity", "Monsters/MonsterA", the shared root BGM
/// folder) - the class's original single-stage curriculum content is never renamed, moved, or
/// re-pathed by this system. Stage 2 and up follow the new "Stage{N}" convention. This is what
/// lets the multi-stage tooling exist in the same project as the original content without the
/// original content visibly changing at all for a student who never touches stage expansion.
///
/// Each stage always has exactly 3 monster "role slots" (MonsterSlot 1/2/3), permanently mapped
/// to the same 3 behavior scripts as Stage 1's MonsterA/B/C respectively (역할 재사용, 외형만
/// 교체 - 사용자 확정 2026-08-17). Slot 1 always reuses MonsterAActionTestController, slot 2
/// always reuses MonsterBActionTestController, slot 3 always reuses MonsterCActionTestController.
/// </summary>
public class StageIdentity
{
    public const int MonsterSlotCount = 3;

    public int StageNumber { get; }
    public bool IsLegacyStage1 => StageNumber == 1;

    // ---- Scenes -------------------------------------------------------------------------
    public string BackgroundTestScenePath { get; }
    public string ActionTestScenePath { get; }

    // ---- Art roots ------------------------------------------------------------------------
    public string ArtRoot { get; }
    public string GroundArtFolder { get; }
    public string BackgroundArtFolder { get; }
    public string WeatherArtFolder { get; }
    public string StageStartArtFolder { get; }
    public string StageClearArtFolder { get; }
    public string LevelClearObjectArtFolder { get; }

    // ---- Prefab roots (stage-namespaced so Stage2's Ground/BG/Weather prefabs never
    // collide with or overwrite Stage1's) --------------------------------------------------
    public string GroundPrefabFolder { get; }
    public string BackgroundPrefabFolder { get; }
    public string WeatherPrefabFolder { get; }
    public string LevelClearObjectPrefabPath { get; }

    // ---- Audio --------------------------------------------------------------------------
    public string BgmFolder { get; }
    public string LevelClearBgmFolder { get; }

    // ---- Display name for menu items / logs -----------------------------------------------
    public string DisplayName { get; }

    private StageIdentity(int stageNumber)
    {
        StageNumber = stageNumber;

        if (stageNumber == 1)
        {
            DisplayName = "스테이지1 (기존)";
            BackgroundTestScenePath = "Assets/_Project/00_Scenes/Stages/BackgroundTest.unity";
            ActionTestScenePath = "Assets/_Project/00_Scenes/Stages/ActionTest.unity";

            ArtRoot = "Assets/_Project/04_Art/StudentReplace";
            GroundArtFolder = ArtRoot + "/StageGround";
            BackgroundArtFolder = ArtRoot + "/StageBackground";
            WeatherArtFolder = ArtRoot + "/StageWeather";
            StageStartArtFolder = ArtRoot + "/UI/StageStart";
            StageClearArtFolder = ArtRoot + "/UI/StageClear";
            LevelClearObjectArtFolder = ArtRoot + "/Environment/LevelClearObject";

            GroundPrefabFolder = "Assets/_Project/02_Prefabs/Environment/Ground";
            BackgroundPrefabFolder = "Assets/_Project/02_Prefabs/Environment/Background";
            WeatherPrefabFolder = "Assets/_Project/02_Prefabs/Environment/Weather";
            LevelClearObjectPrefabPath = "Assets/_Project/02_Prefabs/Environment/LevelClearObject.prefab";

            // Legacy quirk (found 2026-08-17): BackgroundTest's BGM was never given its own
            // subfolder like Title/Intro/Ending/LevelClear were - it scans the BGM root
            // directly. Left as-is on purpose so Stage1's existing BGM setup/file is untouched.
            BgmFolder = "Assets/_Project/06_Audio/BGM";
            LevelClearBgmFolder = "Assets/_Project/06_Audio/BGM/LevelClear";
        }
        else
        {
            DisplayName = $"스테이지{stageNumber}";
            BackgroundTestScenePath = $"Assets/_Project/00_Scenes/Stages/Stage{stageNumber}_BackgroundTest.unity";
            ActionTestScenePath = $"Assets/_Project/00_Scenes/Stages/Stage{stageNumber}_ActionTest.unity";

            ArtRoot = $"Assets/_Project/04_Art/StudentReplace/Stage{stageNumber}";
            GroundArtFolder = ArtRoot + "/StageGround";
            BackgroundArtFolder = ArtRoot + "/StageBackground";
            WeatherArtFolder = ArtRoot + "/StageWeather";
            StageStartArtFolder = ArtRoot + "/UI/StageStart";
            StageClearArtFolder = ArtRoot + "/UI/StageClear";
            LevelClearObjectArtFolder = ArtRoot + "/Environment/LevelClearObject";

            GroundPrefabFolder = $"Assets/_Project/02_Prefabs/Environment/Stage{stageNumber}/Ground";
            BackgroundPrefabFolder = $"Assets/_Project/02_Prefabs/Environment/Stage{stageNumber}/Background";
            WeatherPrefabFolder = $"Assets/_Project/02_Prefabs/Environment/Stage{stageNumber}/Weather";
            LevelClearObjectPrefabPath = $"Assets/_Project/02_Prefabs/Environment/Stage{stageNumber}/LevelClearObject.prefab";

            // 2026-08-17 사용자 확정: 스테이지마다 다른 BGM 사용 가능하게.
            BgmFolder = $"Assets/_Project/06_Audio/BGM/Stage{stageNumber}";
            LevelClearBgmFolder = $"Assets/_Project/06_Audio/BGM/Stage{stageNumber}LevelClear";
        }
    }

    public static StageIdentity For(int stageNumber)
    {
        return new StageIdentity(stageNumber);
    }

    /// <summary>True for "ActionTest"/"BackgroundTest"(스테이지1) or any "Stage{N}_ActionTest"/
    /// "Stage{N}_BackgroundTest" - shared by PauseMenuSceneTools/MobileControlsSceneTools's menu
    /// validators (2026-08-17) so those "works on whatever scene is open" tools stop being
    /// disabled just because their scene-name allowlist predates the multi-stage naming.</summary>
    public static bool IsRecognizedGameplayScene(string sceneName)
    {
        if (sceneName == "ActionTest" || sceneName == "BackgroundTest")
        {
            return true;
        }

        return sceneName.StartsWith("Stage") && (sceneName.EndsWith("_ActionTest") || sceneName.EndsWith("_BackgroundTest"));
    }

    /// <summary>
    /// Per-monster-slot paths. slot is 1-based (1..MonsterSlotCount), always mapped to the
    /// same underlying mold: 1=MonsterA's script, 2=MonsterB's script, 3=MonsterC's script.
    /// </summary>
    public MonsterSlotIdentity Monster(int slot)
    {
        return new MonsterSlotIdentity(this, slot);
    }

    public readonly struct MonsterSlotIdentity
    {
        public int Slot { get; }
        public string ArtFolder { get; }
        public string PrefabPath { get; }
        public string SfxFolder { get; }
        public string ActionTestScenePath { get; }
        /// <summary>Which of the 3 permanent behavior molds this slot reuses - "A", "B", or "C".</summary>
        public string Mold { get; }

        public MonsterSlotIdentity(StageIdentity stage, int slot)
        {
            Slot = slot;
            switch (slot)
            {
                case 1:
                    Mold = "A";
                    break;
                case 2:
                    Mold = "B";
                    break;
                case 3:
                    Mold = "C";
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(slot), slot, "Monster slot must be 1, 2, or 3 - every stage has exactly 3 role slots (2026-08-17 확정).");
            }

            if (stage.IsLegacyStage1)
            {
                // Legacy stage 1 slots ARE MonsterA/B/C themselves - same folders/prefabs/
                // scenes the project always had, nothing renamed.
                var letter = Mold;
                ArtFolder = $"{stage.ArtRoot}/Monsters/Monster{letter}";
                PrefabPath = $"Assets/_Project/02_Prefabs/Monsters/Monster{letter}.prefab";
                SfxFolder = $"Assets/_Project/06_Audio/SFX/Monster{letter}";
                ActionTestScenePath = $"Assets/_Project/00_Scenes/Stages/Monster{letter}_ActionTest.unity";
            }
            else
            {
                ArtFolder = $"{stage.ArtRoot}/Monsters/Monster{slot}";
                PrefabPath = $"Assets/_Project/02_Prefabs/Monsters/Stage{stage.StageNumber}Monster{slot}.prefab";
                SfxFolder = $"Assets/_Project/06_Audio/SFX/Stage{stage.StageNumber}Monster{slot}";
                ActionTestScenePath = $"Assets/_Project/00_Scenes/Stages/Stage{stage.StageNumber}_Monster{slot}_ActionTest.unity";
            }
        }
    }
}
