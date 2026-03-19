using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Core
{
    /// <summary>
    /// Selects and instantiates room templates at runtime based on biome, room type, and difficulty.
    /// Falls back to colored SpriteRenderer rooms when no template matches.
    /// </summary>
    public static class RoomTemplateLoader
    {
        private static Dictionary<string, RoomTemplateData[]> templateCache;

        /// <summary>
        /// Loads all RoomTemplateData assets from Resources and caches them by biome.
        /// </summary>
        public static void Initialize()
        {
            templateCache = new Dictionary<string, RoomTemplateData[]>();

            var allTemplates = Resources.LoadAll<RoomTemplateData>("RoomTemplates");
            foreach (var group in allTemplates.GroupBy(t => t.BiomeName))
            {
                templateCache[group.Key] = group.ToArray();
            }

            Debug.Log($"[RoomTemplateLoader] Loaded {allTemplates.Length} templates across {templateCache.Count} biomes");
        }

        /// <summary>
        /// Selects a room template for the given room and biome.
        /// Returns null if no template matches (caller should use fallback).
        /// Uses a seeded random based on grid position for consistent results within a run.
        /// </summary>
        public static RoomTemplateData SelectTemplate(RoomData room, string biomeName)
        {
            if (templateCache == null) Initialize();

            // Convert zone name to biome key
            string biomeKey = ZoneNameToBiomeKey(biomeName);

            if (!templateCache.ContainsKey(biomeKey) || templateCache[biomeKey].Length == 0)
                return null;

            var templates = templateCache[biomeKey];

            // Filter by room type, difficulty tier, and exits
            var valid = templates.Where(t =>
                t.AllowedTypes != null &&
                t.AllowedTypes.Contains(room.Type) &&
                room.DifficultyTier >= t.MinDifficultyTier &&
                room.DifficultyTier <= t.MaxDifficultyTier &&
                t.MatchesExits(room) &&
                t.Prefab != null
            ).ToArray();

            if (valid.Length == 0)
            {
                // Try relaxed match: ignore difficulty tier
                valid = templates.Where(t =>
                    t.AllowedTypes != null &&
                    t.AllowedTypes.Contains(room.Type) &&
                    t.MatchesExits(room) &&
                    t.Prefab != null
                ).ToArray();
            }

            if (valid.Length == 0)
            {
                // Last resort: any template with matching exits
                valid = templates.Where(t =>
                    t.MatchesExits(room) &&
                    t.Prefab != null
                ).ToArray();
            }

            if (valid.Length == 0)
                return null;

            // Seeded random for consistency within a run
            int seed = room.GridX * 73 + room.GridY * 127;
            return valid[Mathf.Abs(seed) % valid.Length];
        }

        /// <summary>
        /// Instantiates a room template at the given world position.
        /// Returns the instantiated GameObject, or null if template/prefab is missing.
        /// </summary>
        public static GameObject InstantiateRoom(RoomTemplateData template, Vector3 position, Transform parent)
        {
            if (template == null || template.Prefab == null)
                return null;

            var instance = Object.Instantiate(template.Prefab, position, Quaternion.identity, parent);
            instance.name = $"Room_{template.TemplateName}";
            return instance;
        }

        /// <summary>
        /// Returns the player spawn position for a given entry direction.
        /// </summary>
        public static Vector2 GetPlayerSpawn(RoomTemplateData template, string entryDirection)
        {
            if (template == null)
                return Vector2.zero;

            switch (entryDirection)
            {
                case "up": return template.PlayerSpawnUp;
                case "down": return template.PlayerSpawnDown;
                case "left": return template.PlayerSpawnLeft;
                case "right": return template.PlayerSpawnRight;
                default: return template.PlayerSpawnDown; // Default entry from bottom
            }
        }

        /// <summary>
        /// Returns NPC spawn positions from the template.
        /// If more NPCs are needed than positions available, extras are placed at slight offsets.
        /// </summary>
        public static Vector2[] GetNpcSpawnPositions(RoomTemplateData template, int npcCount)
        {
            if (template == null || template.NpcSpawnPoints == null || template.NpcSpawnPoints.Length == 0)
            {
                // Fallback: spread NPCs across the room center area
                var fallback = new Vector2[npcCount];
                for (int i = 0; i < npcCount; i++)
                {
                    float angle = (i / (float)npcCount) * Mathf.PI * 2f;
                    fallback[i] = new Vector2(Mathf.Cos(angle) * 2f, Mathf.Sin(angle) * 1.5f);
                }
                return fallback;
            }

            var positions = new Vector2[npcCount];
            for (int i = 0; i < npcCount; i++)
            {
                if (i < template.NpcSpawnPoints.Length)
                {
                    positions[i] = template.NpcSpawnPoints[i];
                }
                else
                {
                    // Offset from an existing spawn point
                    var basePos = template.NpcSpawnPoints[i % template.NpcSpawnPoints.Length];
                    positions[i] = basePos + new Vector2((i * 0.7f) % 3f - 1.5f, (i * 0.5f) % 2f - 1f);
                }
            }
            return positions;
        }

        /// <summary>
        /// Converts zone display names to biome keys used in template data.
        /// </summary>
        private static string ZoneNameToBiomeKey(string zoneName)
        {
            switch (zoneName)
            {
                case "Ancient Forest": return "AncientForest";
                case "Volcanic Wastes": return "VolcanicWastes";
                case "Sky Peaks": return "SkyPeaks";
                case "Coral Depths": return "CoralDepths";
                default:
                    // Try removing spaces as fallback
                    return zoneName.Replace(" ", "");
            }
        }

        /// <summary>
        /// Returns true if any templates exist for the given biome.
        /// </summary>
        public static bool HasTemplatesForBiome(string biomeName)
        {
            if (templateCache == null) Initialize();
            string key = ZoneNameToBiomeKey(biomeName);
            return templateCache.ContainsKey(key) && templateCache[key].Length > 0;
        }

        /// <summary>
        /// Clears the cache. Call when templates are added/removed at runtime (editor only).
        /// </summary>
        public static void ClearCache()
        {
            templateCache = null;
        }
    }
}
