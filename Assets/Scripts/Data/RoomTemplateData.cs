using UnityEngine;

namespace ArcaneAtlas.Data
{
    [CreateAssetMenu(fileName = "RoomTemplate", menuName = "Arcane Atlas/Room Template")]
    public class RoomTemplateData : ScriptableObject
    {
        public string TemplateName;
        public string BiomeName;            // "AncientForest", "VolcanicWastes", etc.
        public RoomType[] AllowedTypes;     // Which room types can use this template
        public int MinDifficultyTier = 1;   // Minimum tier (1-6)
        public int MaxDifficultyTier = 6;   // Maximum tier (1-6)

        [Header("Exits")]
        public bool HasExitUp = true;
        public bool HasExitDown = true;
        public bool HasExitLeft = true;
        public bool HasExitRight = true;

        [Header("Prefab")]
        public GameObject Prefab;           // The actual tilemap prefab

        [Header("Spawn Points")]
        public Vector2[] NpcSpawnPoints;    // Where NPCs can stand
        public Vector2 PlayerSpawnDown;     // Entry from bottom
        public Vector2 PlayerSpawnUp;       // Entry from top
        public Vector2 PlayerSpawnLeft;     // Entry from left
        public Vector2 PlayerSpawnRight;    // Entry from right

        /// <summary>
        /// Whether this template's exits are compatible with a given room's exits.
        /// A template is compatible if it has at least the exits the room needs.
        /// </summary>
        public bool MatchesExits(RoomData room)
        {
            if (room.ExitUp && !HasExitUp) return false;
            if (room.ExitDown && !HasExitDown) return false;
            if (room.ExitLeft && !HasExitLeft) return false;
            if (room.ExitRight && !HasExitRight) return false;
            return true;
        }
    }
}
