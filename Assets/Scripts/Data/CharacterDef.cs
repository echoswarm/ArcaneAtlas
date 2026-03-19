using UnityEngine;

namespace ArcaneAtlas.Data
{
    /// <summary>
    /// Defines a character's sprite animations. Stores references to sliced sub-sprites
    /// loaded from Resources/CharacterFrames/{name}/ at runtime.
    /// Idle and Walk sheets are imported as Multiple-mode sprites, auto-sliced.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Arcane Atlas/Character Def")]
    public class CharacterDef : ScriptableObject
    {
        public string CharacterName;
        public string PackSource;

        [Header("Frame Timing")]
        public float IdleFrameRate = 4f;  // Slower idle for relaxed feel
        public float WalkFrameRate = 8f;

        // Runtime-loaded sprites (cached)
        [System.NonSerialized] private Sprite[] cachedIdleFrames;
        [System.NonSerialized] private Sprite[] cachedWalkFrames;

        public Sprite PreviewSprite
        {
            get
            {
                var idle = GetIdleFrames();
                if (idle != null && idle.Length > 0) return idle[0];
                var walk = GetWalkFrames();
                if (walk != null && walk.Length > 0) return walk[0];
                return null;
            }
        }

        public Sprite[] GetIdleFrames()
        {
            if (cachedIdleFrames != null) return cachedIdleFrames;
            cachedIdleFrames = LoadSubSprites("Idle");
            return cachedIdleFrames;
        }

        public Sprite[] GetWalkFrames()
        {
            if (cachedWalkFrames != null) return cachedWalkFrames;
            cachedWalkFrames = LoadSubSprites("Walk");
            return cachedWalkFrames;
        }

        /// <summary>
        /// Loads sub-sprites from a Multiple-mode sprite sheet, filtered to a single
        /// direction row. Minifantasy sheets have multiple rows (one per facing direction).
        /// We pick one row based on Y position to avoid the character cycling through all directions.
        /// </summary>
        private Sprite[] LoadSubSprites(string animName)
        {
            if (string.IsNullOrEmpty(CharacterName)) return new Sprite[0];

            string path = $"CharacterFrames/{CharacterName}/{CharacterName}_{animName}";
            var allSprites = Resources.LoadAll<Sprite>(path);

            if (allSprites == null || allSprites.Length == 0)
            {
                Debug.LogWarning($"[CharacterDef] No sprites at Resources/{path}");
                return new Sprite[0];
            }

            return FilterToSingleRow(allSprites);
        }

        /// <summary>
        /// Groups sprites by their Y position in the sheet (each row = one facing direction).
        /// Returns only sprites from one row.
        /// Minifantasy typical layout (bottom to top): Down, Right, Up, Left
        /// We pick row index 1 (second from bottom = side-facing) when available.
        /// </summary>
        private Sprite[] FilterToSingleRow(Sprite[] allSprites)
        {
            if (allSprites.Length <= 4) return allSprites; // Too few to be multi-directional

            // Group sprites by Y position (rounded to handle sub-pixel variations)
            var rows = new System.Collections.Generic.SortedDictionary<int, System.Collections.Generic.List<Sprite>>();
            foreach (var sprite in allSprites)
            {
                int y = Mathf.RoundToInt(sprite.rect.y);
                if (!rows.ContainsKey(y))
                    rows[y] = new System.Collections.Generic.List<Sprite>();
                rows[y].Add(sprite);
            }

            if (rows.Count <= 1) return allSprites; // Single row, use all

            // Convert to list sorted by Y (ascending = bottom row first)
            var sortedRows = new System.Collections.Generic.List<Sprite[]>();
            foreach (var kvp in rows)
            {
                // Sort sprites within each row by X position (left to right = animation order)
                kvp.Value.Sort((a, b) => a.rect.x.CompareTo(b.rect.x));
                sortedRows.Add(kvp.Value.ToArray());
            }

            // Pick side-facing row: second from top is typically right-facing
            // Minifantasy layout (bottom to top): Down, Left, Up, Right (varies by pack)
            int targetRow = sortedRows.Count >= 4 ? sortedRows.Count - 1 :
                            sortedRows.Count >= 2 ? sortedRows.Count - 2 : 0;
            return sortedRows[targetRow];
        }

        public void ClearCache()
        {
            cachedIdleFrames = null;
            cachedWalkFrames = null;
        }
    }
}
