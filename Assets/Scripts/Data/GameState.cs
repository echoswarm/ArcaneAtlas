using System.Collections.Generic;

namespace ArcaneAtlas.Data
{
    public static class GameState
    {
        public static int Gold = 40;
        public static int Packs = 3;
        public static int RerollTokens = 0;
        public static string CurrentZone = "Town";

        // Zone unlock state (indexed by zone order: 0=Ancient Forest, 1=Volcanic, 2=Coral, 3=Sky)
        public static bool[] ZonesUnlocked = new bool[] { true, false, false, false };

        // Zone completion tracking
        public static Dictionary<string, bool> ZoneCompleted = new Dictionary<string, bool>();

        // Current opponent for combat (set by EncounterManager before transitioning)
        public static NpcData CurrentOpponent;

        // Pity-roll tracking: boss name → defeats without legendary drop
        public static Dictionary<string, int> BossDefeatCounts = new Dictionary<string, int>();

        // Intro/tutorial state
        public static bool HasSeenIntro = false;

        // Pity-roll tuning constants
        public const float PITY_BASE_CHANCE = 0.005f;      // 0.5% base
        public const int PITY_THRESHOLD = 20;               // Pity starts ramping after this many defeats
        public const float PITY_INCREMENT = 0.001f;         // +0.1% per defeat past threshold
        public const float PITY_MAX_CHANCE = 1f;            // 100% guaranteed cap
    }
}
