using System.Collections.Generic;

namespace ArcaneAtlas.Data
{
    public enum RoomType { Empty, NPC, Treasure, Event, Boss }

    public class RoomData
    {
        public int GridX;
        public int GridY;
        public bool ExitUp;
        public bool ExitDown;
        public bool ExitLeft;
        public bool ExitRight;
        public int RoomVariant;
        public int NpcCount;

        // Session A: Zone run fields
        public RoomType Type;
        public int DifficultyTier;       // 1-6, based on BFS distance from entry
        public bool IsDiscovered;
        public bool IsCleared;           // All NPCs in this room defeated
        public List<NpcData> SpawnedNpcs; // NPCs generated for this room (persists within run)

        public bool HasExitInDirection(string direction)
        {
            switch (direction)
            {
                case "up": return ExitUp;
                case "down": return ExitDown;
                case "left": return ExitLeft;
                case "right": return ExitRight;
                default: return false;
            }
        }
    }
}
