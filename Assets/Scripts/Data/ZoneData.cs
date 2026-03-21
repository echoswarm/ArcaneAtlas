namespace ArcaneAtlas.Data
{
    public class ZoneData
    {
        public string Name;
        public string Description;
        public ElementType Element;
        public int Difficulty;
        public bool IsUnlocked;
        public string ZoneEntryText;
        public string NextZoneName;
        public ElementType AdjacentElement;

        /// <summary>
        /// Name of the TilePaletteDef in Resources/TilePalettes/ to use for this zone.
        /// If null/empty, falls back to ExplorationManager's default palette.
        /// </summary>
        public string BiomePalette;

        public static ZoneData[] GetAllZones()
        {
            return new ZoneData[]
            {
                new ZoneData
                {
                    Name = "Ancient Forest",
                    Description = "A primordial woodland thick with moss and mystery. Earth-aligned creatures roam its tangled paths.",
                    Element = ElementType.Earth,
                    Difficulty = 1,
                    IsUnlocked = true,
                    NextZoneName = "Volcanic Wastes",
                    AdjacentElement = ElementType.Fire,
                    BiomePalette = "AncientForest",
                    ZoneEntryText = "The Atlas stirs. Its pages turn to a chapter thick with moss and vine. 'The Forest remembers,' it whispers. 'Gather its guardians, and I will show you the way deeper.'"
                },
                new ZoneData
                {
                    Name = "Volcanic Wastes",
                    Description = "Scorched badlands where fire elementals patrol rivers of molten rock.",
                    Element = ElementType.Fire,
                    Difficulty = 2,
                    IsUnlocked = false,
                    NextZoneName = "Sky Peaks",
                    AdjacentElement = ElementType.Wind,
                    BiomePalette = "VolcanicWastes",
                    ZoneEntryText = "The Atlas burns warm in your hands. New pages reveal themselves \u2014 charred at the edges, glowing with ember-script. 'Fire tests all who seek passage.'"
                },
                new ZoneData
                {
                    Name = "Coral Depths",
                    Description = "Submerged ruins teeming with aquatic creatures and ancient coral formations.",
                    Element = ElementType.Water,
                    Difficulty = 3,
                    IsUnlocked = false,
                    NextZoneName = null,
                    AdjacentElement = ElementType.Earth,
                    BiomePalette = "CoralDepths",
                    ZoneEntryText = "The Atlas dampens. Ink bleeds into new patterns \u2014 currents and tides mapped in impossible detail. 'The deep does not give up its treasures easily.'"
                },
                new ZoneData
                {
                    Name = "Sky Peaks",
                    Description = "Floating mountain islands battered by winds, home to the most dangerous foes.",
                    Element = ElementType.Wind,
                    Difficulty = 4,
                    IsUnlocked = false,
                    NextZoneName = "Coral Depths",
                    AdjacentElement = ElementType.Water,
                    BiomePalette = "SkyPeaks",
                    ZoneEntryText = "The Atlas grows light, its pages fluttering as if caught in a wind that isn't there. 'The high places hold secrets only the bold may claim.'"
                }
            };
        }

        public static ZoneData GetByName(string name)
        {
            foreach (var zone in GetAllZones())
                if (zone.Name == name) return zone;
            return null;
        }
    }
}
