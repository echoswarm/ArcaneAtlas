using System.Collections.Generic;
using System.Linq;

namespace ArcaneAtlas.Data
{
    public class NpcData
    {
        public string Name;
        public ElementType Element;
        public int Difficulty; // 1-4, affects combat AI and rewards
        public string[] DialogueLines; // Lines shown before choices
        public bool IsDefeated; // Tracks defeat within current exploration run
        public bool IsBoss; // Boss NPCs are in specific rooms, tougher
        public string BossCardName; // Legendary card that only this boss can drop

        // Curated card pool for this NPC (generated at spawn, persists within run)
        public List<CardData> CardPool;

        /// <summary>
        /// Generates a curated card pool based on difficulty tier and element.
        /// Pool persists for the run so rematches feel consistent.
        /// Boss (T6) draws from faction + adjacent faction.
        /// </summary>
        public void GenerateOpponentPool(int difficultyTier)
        {
            var all = CardDatabase.GetAllCards();
            var factionCards = all.Where(c => c.Element == Element).ToList();
            CardPool = new List<CardData>();

            switch (difficultyTier)
            {
                case 1: // Faction T1 only, 10-12 cards
                    CardPool = factionCards.Where(c => c.Tier == 1).ToList();
                    PadPool(factionCards, 1, 1, 10);
                    TrimPool(12);
                    break;

                case 2: // Faction T1-T2, 12-15 cards
                    CardPool = factionCards.Where(c => c.Tier <= 2).ToList();
                    PadPool(factionCards, 1, 2, 12);
                    TrimPool(15);
                    break;

                case 3: // Faction T1-T3, 15-18 cards
                    CardPool = factionCards.Where(c => c.Tier <= 3).ToList();
                    PadPool(factionCards, 1, 3, 15);
                    TrimPool(18);
                    break;

                case 4: // Faction T1-T4 + some T5, 18-22 cards
                    CardPool = factionCards.Where(c => c.Tier <= 4).ToList();
                    // Sprinkle in some T5
                    var t5 = factionCards.Where(c => c.Tier == 5).ToList();
                    if (t5.Count > 0)
                        CardPool.Add(t5[UnityEngine.Random.Range(0, t5.Count)]);
                    PadPool(factionCards, 1, 4, 18);
                    TrimPool(22);
                    break;

                case 5: // Faction all tiers, 25 cards
                    CardPool = new List<CardData>(factionCards);
                    PadPool(factionCards, 1, 6, 25);
                    TrimPool(25);
                    break;

                case 6: // Boss: faction + adjacent faction all tiers, 30+ cards
                    CardPool = new List<CardData>(factionCards);
                    var adjacentElement = GetAdjacentElement(Element);
                    var adjacentCards = all.Where(c => c.Element == adjacentElement).ToList();
                    CardPool.AddRange(adjacentCards);
                    PadPool(factionCards, 1, 6, 30);
                    break;

                default:
                    CardPool = factionCards.Where(c => c.Tier == 1).ToList();
                    break;
            }
        }

        /// <summary>
        /// Boss dual-element mapping: Earth→Fire, Fire→Wind, Wind→Water, Water→Earth
        /// </summary>
        private static ElementType GetAdjacentElement(ElementType element)
        {
            switch (element)
            {
                case ElementType.Earth: return ElementType.Fire;
                case ElementType.Fire: return ElementType.Wind;
                case ElementType.Wind: return ElementType.Water;
                case ElementType.Water: return ElementType.Earth;
                default: return ElementType.Fire;
            }
        }

        private void PadPool(List<CardData> factionCards, int minTier, int maxTier, int minCount)
        {
            // If pool is too small, duplicate existing cards to reach minimum
            if (CardPool.Count >= minCount) return;
            var eligible = factionCards.Where(c => c.Tier >= minTier && c.Tier <= maxTier).ToList();
            int idx = 0;
            while (CardPool.Count < minCount && eligible.Count > 0)
            {
                CardPool.Add(eligible[idx % eligible.Count]);
                idx++;
            }
        }

        private void TrimPool(int maxCount)
        {
            if (CardPool.Count <= maxCount) return;
            // Shuffle then trim — keep variety
            for (int i = CardPool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = CardPool[i];
                CardPool[i] = CardPool[j];
                CardPool[j] = temp;
            }
            CardPool = CardPool.Take(maxCount).ToList();
        }

        // Pre-built NPC pools per zone
        public static NpcData[] GetAncientForestNpcs()
        {
            return new NpcData[]
            {
                new NpcData { Name = "Forest Sprite", Element = ElementType.Earth, Difficulty = 1,
                    DialogueLines = new[] { "The forest whispers of your arrival...", "You carry the Atlas? I haven't seen one of those in years.", "Care to test your cards against mine?" } },
                new NpcData { Name = "Moss Guardian", Element = ElementType.Earth, Difficulty = 1,
                    DialogueLines = new[] { "I protect these ancient trees.", "The cartographer came through here once. He mapped every path. Every creature. Nobody believed him.", "You shall not pass without a duel!" } },
                new NpcData { Name = "Vine Weaver", Element = ElementType.Earth, Difficulty = 2,
                    DialogueLines = new[] { "My roots run deep here.", "Let's see what your collection holds." } },
                new NpcData { Name = "Thorn Knight", Element = ElementType.Earth, Difficulty = 2,
                    DialogueLines = new[] { "The thorns answer to me alone.", "They say six Champions guard the path beyond the four regions. But that's just a legend... right?", "Prepare yourself, challenger." } },
                new NpcData { Name = "Bark Elder", Element = ElementType.Earth, Difficulty = 3,
                    DialogueLines = new[] { "I have guarded this forest for centuries.", "Few have bested my ancient cards." } },
                new NpcData { Name = "Ancient Treant", Element = ElementType.Earth, Difficulty = 3, IsBoss = true,
                    BossCardName = "Heart of the Forest",
                    DialogueLines = new[] { "You've made it to the heart of the forest.", "I am its final guardian. Defeat me, and the forest's secrets are yours." } },
            };
        }

        public static NpcData[] GetVolcanicWastesNpcs()
        {
            return new NpcData[]
            {
                new NpcData { Name = "Ember Imp", Element = ElementType.Fire, Difficulty = 1,
                    DialogueLines = new[] { "Hehe, new here? The heat gets to everyone!", "Fire burns everything eventually. Even knowledge. Even maps.", "Let's play with fire!" } },
                new NpcData { Name = "Lava Hound", Element = ElementType.Fire, Difficulty = 1,
                    DialogueLines = new[] { "*growls menacingly*", "Another challenger approaches!" } },
                new NpcData { Name = "Flame Dancer", Element = ElementType.Fire, Difficulty = 2,
                    DialogueLines = new[] { "Dance with me through the flames!", "The old cartographer's wife \u2014 they say she lives in a small town to the east. Never speaks of what happened.", "My cards burn bright." } },
                new NpcData { Name = "Magma Shaman", Element = ElementType.Fire, Difficulty = 2,
                    DialogueLines = new[] { "The volcano speaks through me.", "The Champions don't use normal cards. They hold something... older.", "You dare challenge fire itself?" } },
                new NpcData { Name = "Cinder Lord", Element = ElementType.Fire, Difficulty = 3,
                    DialogueLines = new[] { "I am forged in the heart of the mountain.", "Prepare to be reduced to ash." } },
                new NpcData { Name = "Inferno Drake", Element = ElementType.Fire, Difficulty = 3, IsBoss = true,
                    BossCardName = "Volcanic Core",
                    DialogueLines = new[] { "You've reached the caldera's edge.", "Face the fury of the volcano!" } },
            };
        }

        public static NpcData[] GetCoralDepthsNpcs()
        {
            return new NpcData[]
            {
                new NpcData { Name = "Tide Wisp", Element = ElementType.Water, Difficulty = 1,
                    DialogueLines = new[] { "The currents brought you here...", "The deep remembers what the surface forgets. Every page that entered this water is still here, somewhere.", "Shall we see who commands the tides?" } },
                new NpcData { Name = "Shell Sentry", Element = ElementType.Water, Difficulty = 1,
                    DialogueLines = new[] { "None pass without my approval.", "Show me your strength!" } },
                new NpcData { Name = "Reef Witch", Element = ElementType.Water, Difficulty = 2,
                    DialogueLines = new[] { "The coral sings ancient songs.", "The cartographer's son \u2014 have you heard? They say he found the draft. The incomplete Atlas.", "My cards flow like the ocean." } },
                new NpcData { Name = "Abyssal Knight", Element = ElementType.Water, Difficulty = 2,
                    DialogueLines = new[] { "From the deep I rise.", "Your cards cannot match the ocean's power." } },
                new NpcData { Name = "Tidal Sage", Element = ElementType.Water, Difficulty = 3,
                    DialogueLines = new[] { "I have studied the currents for eons.", "The sea shall judge you." } },
                new NpcData { Name = "Leviathan", Element = ElementType.Water, Difficulty = 3, IsBoss = true,
                    BossCardName = "Abyssal Trident",
                    DialogueLines = new[] { "You've descended to the abyss.", "Face the guardian of the deep!" } },
            };
        }

        public static NpcData[] GetSkyPeaksNpcs()
        {
            return new NpcData[]
            {
                new NpcData { Name = "Breeze Spirit", Element = ElementType.Wind, Difficulty = 1,
                    DialogueLines = new[] { "The winds carry your scent...", "The wind carries pages sometimes. Old parchment, covered in ink. I wonder where they come from.", "A gentle breeze... or a storm?" } },
                new NpcData { Name = "Cloud Walker", Element = ElementType.Wind, Difficulty = 1,
                    DialogueLines = new[] { "Step carefully up here.", "The sky has eyes everywhere." } },
                new NpcData { Name = "Storm Caller", Element = ElementType.Wind, Difficulty = 2,
                    DialogueLines = new[] { "I command the tempest!", "Ascendency. That's what the old texts call it. When all four cornerstones align on a single board.", "Thunder and lightning at my call." } },
                new NpcData { Name = "Gale Monk", Element = ElementType.Wind, Difficulty = 2,
                    DialogueLines = new[] { "Balance in all things.", "The wind teaches patience... and fury." } },
                new NpcData { Name = "Zephyr Sage", Element = ElementType.Wind, Difficulty = 3,
                    DialogueLines = new[] { "The highest peaks hold the deepest wisdom.", "Your cards whisper to the wind." } },
                new NpcData { Name = "Tempest Dragon", Element = ElementType.Wind, Difficulty = 3, IsBoss = true,
                    BossCardName = "Eye of the Storm",
                    DialogueLines = new[] { "You've reached the summit of the world.", "Face the master of storms!" } },
            };
        }

        public static NpcData[] GetNpcsForZone(string zoneName)
        {
            switch (zoneName)
            {
                case "Ancient Forest": return GetAncientForestNpcs();
                case "Volcanic Wastes": return GetVolcanicWastesNpcs();
                case "Coral Depths": return GetCoralDepthsNpcs();
                case "Sky Peaks": return GetSkyPeaksNpcs();
                default: return GetAncientForestNpcs();
            }
        }

        /// <summary>
        /// Returns the zone element for the current zone name.
        /// </summary>
        public static ElementType GetZoneElement(string zoneName)
        {
            switch (zoneName)
            {
                case "Ancient Forest": return ElementType.Earth;
                case "Volcanic Wastes": return ElementType.Fire;
                case "Coral Depths": return ElementType.Water;
                case "Sky Peaks": return ElementType.Wind;
                default: return ElementType.Earth;
            }
        }
    }
}
