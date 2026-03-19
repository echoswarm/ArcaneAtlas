using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcaneAtlas.Data
{
    public static class CardDatabase
    {
        private static List<CardData> allCards;

        public static List<CardData> GetAllCards()
        {
            if (allCards == null) GenerateCards();
            return allCards;
        }

        public static List<CardData> GetCardsByElement(ElementType element)
        {
            return GetAllCards().Where(c => c.Element == element).ToList();
        }

        public static List<CardData> GetCardsByRarity(CardRarity maxRarity)
        {
            return GetAllCards().Where(c => c.Rarity <= maxRarity).ToList();
        }

        /// <summary>
        /// Returns cards from the player's active collection pool, filtered by max tier.
        /// If the active pool has fewer than minPool cards, pads with default tier-1 commons.
        /// </summary>
        public static List<CardData> GetActivePool(int maxTier, int minPool = 10)
        {
            var all = GetAllCards();
            // Build pool from player's active collection
            var activeNames = new HashSet<string>();
            foreach (var owned in PlayerCollection.Cards)
            {
                if (!owned.IsActive) continue;
                var data = all.Find(d => d.CardName == owned.CardName);
                if (data == null) continue;
                if (!PlayerCollection.IsElementActive(data.Element)) continue;
                activeNames.Add(owned.CardName);
            }

            var pool = all.Where(c => activeNames.Contains(c.CardName) && c.Tier <= maxTier).ToList();

            // Pad with owned-but-inactive cards at valid tiers (player owns them, just unchecked)
            if (pool.Count < minPool)
            {
                var ownedNames = new HashSet<string>();
                foreach (var owned in PlayerCollection.Cards)
                    ownedNames.Add(owned.CardName);

                var ownedPad = all.Where(c => ownedNames.Contains(c.CardName) && c.Tier <= maxTier && !activeNames.Contains(c.CardName) && PlayerCollection.IsElementActive(c.Element)).ToList();
                int needed = minPool - pool.Count;
                for (int i = 0; i < needed && i < ownedPad.Count; i++)
                    pool.Add(ownedPad[i]);
            }

            // Last resort: pad with T1 commons from active elements to hit minimum
            if (pool.Count < minPool)
            {
                var defaults = all.Where(c => c.Tier == 1 && c.Rarity == CardRarity.Common && PlayerCollection.IsElementActive(c.Element) && !pool.Any(p => p.CardName == c.CardName)).ToList();
                int needed = minPool - pool.Count;
                for (int i = 0; i < needed && i < defaults.Count; i++)
                    pool.Add(defaults[i]);
            }

            return pool;
        }

        /// <summary>
        /// Returns the Resources-relative path for a card's creature sprite.
        /// Sprites must be placed under Assets/Resources/CardArt/{element}/creature_{element}_{index:D2}
        /// </summary>
        public static string GetSpritePath(CardData card)
        {
            string elem = card.Element.ToString().ToLower();
            return $"CardArt/{elem}/creature_{elem}_{card.SpriteIndex:D2}";
        }

        private static void GenerateCards()
        {
            allCards = new List<CardData>();

            // ═══════════════════════════════════════
            //  FIRE — 25 cards (high attack, low health)
            //  Tier distribution: T1(5) T2(5) T3(5) T4(5) T5(3) T6(2)
            // ═══════════════════════════════════════
            // Commons — T1 and T2
            Add("Ember Sprite",     ElementType.Fire, CardRarity.Common, RowPreference.Front,  1, 3, 2, "",  1);
            Add("Flame Wisp",       ElementType.Fire, CardRarity.Common, RowPreference.Front,  1, 2, 3, "",  2);
            Add("Cinder Rat",       ElementType.Fire, CardRarity.Common, RowPreference.Front,  1, 3, 1, "",  4);
            Add("Spark Fox",        ElementType.Fire, CardRarity.Common, RowPreference.Either, 1, 2, 2, "",  5);
            Add("Heat Lizard",      ElementType.Fire, CardRarity.Common, RowPreference.Front,  1, 2, 2, "",  9);
            Add("Blaze Hound",      ElementType.Fire, CardRarity.Common, RowPreference.Front,  2, 4, 3, "",  3);
            Add("Ash Beetle",       ElementType.Fire, CardRarity.Common, RowPreference.Front,  2, 3, 3, "",  6);
            Add("Flare Bat",        ElementType.Fire, CardRarity.Common, RowPreference.Either, 2, 3, 2, "",  7);
            Add("Smolder Cat",      ElementType.Fire, CardRarity.Common, RowPreference.Front,  2, 4, 2, "",  8);
            Add("Coal Crawler",     ElementType.Fire, CardRarity.Common, RowPreference.Front,  2, 3, 4, "", 10);
            // Uncommons — T3 and T4
            Add("Pyro Archer",      ElementType.Fire, CardRarity.Uncommon, RowPreference.Back,   3, 4, 3, "Attacks a random enemy",  11);
            Add("Flame Dancer",     ElementType.Fire, CardRarity.Uncommon, RowPreference.Front,  3, 5, 3, "Gains +1 attack when ally dies",  12);
            Add("Soot Devil",       ElementType.Fire, CardRarity.Uncommon, RowPreference.Either, 3, 4, 3, "Deals 1 on death",  17);
            Add("Magma Toad",       ElementType.Fire, CardRarity.Uncommon, RowPreference.Front,  3, 5, 4, "Burns attacker for 1",  13);
            Add("Scorch Hawk",      ElementType.Fire, CardRarity.Uncommon, RowPreference.Either, 3, 5, 3, "Ignores back row",  14);
            Add("Lava Worm",        ElementType.Fire, CardRarity.Uncommon, RowPreference.Front,  4, 5, 5, "Taunt",  15);
            Add("Ember Shaman",     ElementType.Fire, CardRarity.Uncommon, RowPreference.Back,   4, 4, 4, "Gives front row +1 attack",  16);
            Add("Blaze Wolf",       ElementType.Fire, CardRarity.Uncommon, RowPreference.Front,  4, 6, 3, "First strike",  18);
            // Rares — T4 and T5
            Add("Dragon Whelp",     ElementType.Fire, CardRarity.Rare, RowPreference.Front, 4, 5, 4, "Grows +1/+1 each round",  23);
            Add("Hellfire Mage",    ElementType.Fire, CardRarity.Rare, RowPreference.Back,  4, 5, 4, "Hits all enemies for 2",  22);
            Add("Inferno Knight",   ElementType.Fire, CardRarity.Rare, RowPreference.Front, 5, 7, 5, "Deals 2 damage to all enemies on attack",  19);
            Add("Phoenix Hatchling",ElementType.Fire, CardRarity.Rare, RowPreference.Front, 5, 6, 6, "Revives once at half health",  20);
            Add("Eruption Golem",   ElementType.Fire, CardRarity.Rare, RowPreference.Front, 5, 5, 7, "Deals 3 damage to opposing slot",  21);
            // Legendary — T5 and T6
            Add("Volcanic Core",    ElementType.Fire, CardRarity.Legendary, RowPreference.Front, 5, 8, 6, "Deals 3 damage to all enemies on play",  24);
            Add("Phoenix Eternal",  ElementType.Fire, CardRarity.Legendary, RowPreference.Front, 6, 9, 9, "Fully revives once per match",  25);

            // ═══════════════════════════════════════
            //  WATER — 25 cards (control, balanced)
            //  Tier distribution: T1(5) T2(5) T3(5) T4(5) T5(3) T6(2)
            // ═══════════════════════════════════════
            // Commons — T1 and T2
            Add("Tide Pool",        ElementType.Water, CardRarity.Common, RowPreference.Front,  1, 2, 3, "",  1);
            Add("Coral Guard",      ElementType.Water, CardRarity.Common, RowPreference.Front,  1, 1, 4, "",  2);
            Add("Kelp Snapper",     ElementType.Water, CardRarity.Common, RowPreference.Front,  1, 2, 2, "",  4);
            Add("Drip Sprite",      ElementType.Water, CardRarity.Common, RowPreference.Either, 1, 1, 3, "",  5);
            Add("Stream Fish",      ElementType.Water, CardRarity.Common, RowPreference.Either, 1, 2, 2, "",  7);
            Add("Wave Runner",      ElementType.Water, CardRarity.Common, RowPreference.Either, 2, 3, 3, "",  3);
            Add("Bubble Crab",      ElementType.Water, CardRarity.Common, RowPreference.Front,  2, 2, 4, "",  6);
            Add("Shore Turtle",     ElementType.Water, CardRarity.Common, RowPreference.Front,  2, 1, 5, "",  8);
            Add("Mist Frog",        ElementType.Water, CardRarity.Common, RowPreference.Either, 2, 2, 3, "",  9);
            Add("Pond Lurker",      ElementType.Water, CardRarity.Common, RowPreference.Front,  2, 3, 3, "", 10);
            // Uncommons — T3 and T4
            Add("Tidal Shaman",     ElementType.Water, CardRarity.Uncommon, RowPreference.Back,   3, 2, 4, "Heals adjacent ally by 2",  12);
            Add("Hail Striker",     ElementType.Water, CardRarity.Uncommon, RowPreference.Either, 3, 3, 3, "Freezes target for 1 round",  14);
            Add("Rain Dancer",      ElementType.Water, CardRarity.Uncommon, RowPreference.Back,   3, 2, 4, "Heals all allies by 1",  16);
            Add("Fog Phantom",      ElementType.Water, CardRarity.Uncommon, RowPreference.Either, 3, 3, 3, "50% dodge chance",  18);
            Add("Frost Mage",       ElementType.Water, CardRarity.Uncommon, RowPreference.Back,   3, 3, 4, "Reduces enemy attack by 1",  11);
            Add("Riptide Eel",      ElementType.Water, CardRarity.Uncommon, RowPreference.Front,  4, 5, 4, "Swaps position with target",  13);
            Add("Whirlpool Djinn",  ElementType.Water, CardRarity.Uncommon, RowPreference.Back,   4, 3, 5, "Pulls back-row enemy to front",  15);
            Add("Ice Crab",         ElementType.Water, CardRarity.Uncommon, RowPreference.Front,  4, 4, 6, "Taunt. Takes 1 less damage",  17);
            // Rares — T4 and T5
            Add("Depth Charger",    ElementType.Water, CardRarity.Rare, RowPreference.Front, 4, 4, 5, "Deals 2 to all on death",  22);
            Add("Leviathan",        ElementType.Water, CardRarity.Rare, RowPreference.Front, 4, 5, 7, "Takes reduced damage",  19);
            Add("Tsunami Serpent",   ElementType.Water, CardRarity.Rare, RowPreference.Front, 5, 6, 6, "Pushes all enemies back 1 slot",  20);
            Add("Glacier Wyrm",     ElementType.Water, CardRarity.Rare, RowPreference.Front, 5, 4, 9, "Freezes attacker for 1 round",  21);
            Add("Kraken Spawn",     ElementType.Water, CardRarity.Rare, RowPreference.Front, 5, 6, 7, "Grabs and damages 2 enemies",  23);
            // Legendary — T5 and T6
            Add("Abyssal Trident",  ElementType.Water, CardRarity.Legendary, RowPreference.Front, 5, 6, 9, "Reduces all enemy attack by 2",  24);
            Add("Maelstrom Titan",  ElementType.Water, CardRarity.Legendary, RowPreference.Front, 6, 8, 11, "Heals all allies fully each round",  25);

            // ═══════════════════════════════════════
            //  EARTH — 25 cards (high health, tanky)
            //  Tier distribution: T1(5) T2(5) T3(5) T4(5) T5(3) T6(2)
            // ═══════════════════════════════════════
            // Commons — T1 and T2
            Add("Stone Golem",      ElementType.Earth, CardRarity.Common, RowPreference.Front,  1, 1, 4, "",  1);
            Add("Root Walker",      ElementType.Earth, CardRarity.Common, RowPreference.Front,  1, 2, 3, "",  2);
            Add("Pebble Imp",       ElementType.Earth, CardRarity.Common, RowPreference.Either, 1, 1, 3, "",  4);
            Add("Mud Crawler",      ElementType.Earth, CardRarity.Common, RowPreference.Front,  1, 2, 3, "",  5);
            Add("Thorn Rat",        ElementType.Earth, CardRarity.Common, RowPreference.Front,  1, 2, 2, "",  9);
            Add("Bark Shield",      ElementType.Earth, CardRarity.Common, RowPreference.Front,  2, 1, 5, "Taunt",  3);
            Add("Boulder Beetle",   ElementType.Earth, CardRarity.Common, RowPreference.Front,  2, 2, 4, "",  6);
            Add("Sprout Kin",       ElementType.Earth, CardRarity.Common, RowPreference.Either, 2, 2, 3, "",  7);
            Add("Clay Sentinel",    ElementType.Earth, CardRarity.Common, RowPreference.Front,  2, 2, 5, "",  8);
            Add("Fungal Spore",     ElementType.Earth, CardRarity.Common, RowPreference.Either, 2, 1, 4, "", 10);
            // Uncommons — T3 and T4
            Add("Moss Healer",      ElementType.Earth, CardRarity.Uncommon, RowPreference.Back,   3, 2, 4, "Heals front row by 1",  11);
            Add("Sand Shaper",      ElementType.Earth, CardRarity.Uncommon, RowPreference.Back,   3, 3, 4, "Gives adjacent ally +2 health",  13);
            Add("Vine Snare",       ElementType.Earth, CardRarity.Uncommon, RowPreference.Back,   3, 2, 5, "Roots target in place",  15);
            Add("Mushroom Sage",    ElementType.Earth, CardRarity.Uncommon, RowPreference.Back,   3, 2, 4, "Heals all allies by 1 on death",  17);
            Add("Quake Mole",       ElementType.Earth, CardRarity.Uncommon, RowPreference.Front,  3, 4, 5, "Stuns target for 1 round",  14);
            Add("Iron Root",        ElementType.Earth, CardRarity.Uncommon, RowPreference.Front,  4, 3, 7, "Taunt. Gains +1 health each round",  12);
            Add("Granite Bear",     ElementType.Earth, CardRarity.Uncommon, RowPreference.Front,  4, 4, 6, "Takes 1 less damage",  16);
            Add("Cliff Charger",    ElementType.Earth, CardRarity.Uncommon, RowPreference.Front,  4, 5, 5, "Deals damage equal to missing health",  18);
            // Rares — T4 and T5
            Add("Druid Elder",      ElementType.Earth, CardRarity.Rare, RowPreference.Back,  4, 3, 6, "Heals all allies by 3",  22);
            Add("Ancient Treant",   ElementType.Earth, CardRarity.Rare, RowPreference.Front, 4, 4, 9, "Gains +1 health each round",  19);
            Add("Crystal Tortoise", ElementType.Earth, CardRarity.Rare, RowPreference.Front, 5, 3, 11, "Reflects 2 damage to attacker",  20);
            Add("Living Mountain",  ElementType.Earth, CardRarity.Rare, RowPreference.Front, 5, 5, 8, "Taunt. Immune to abilities",  21);
            Add("Earthquake Wyrm",  ElementType.Earth, CardRarity.Rare, RowPreference.Front, 5, 6, 7, "Deals 2 to all on play",  23);
            // Legendary — T5 and T6
            Add("Heart of the Forest", ElementType.Earth, CardRarity.Legendary, RowPreference.Front, 5, 5, 11, "Heals all allies by 2 each round",  24);
            Add("World Tortoise",   ElementType.Earth, CardRarity.Legendary, RowPreference.Front, 6, 4, 15, "All allies take 2 less damage",  25);

            // ═══════════════════════════════════════
            //  WIND — 25 cards (speed, utility)
            //  Tier distribution: T1(5) T2(5) T3(5) T4(5) T5(3) T6(2)
            // ═══════════════════════════════════════
            // Commons — T1 and T2
            Add("Breeze Sprite",    ElementType.Wind, CardRarity.Common, RowPreference.Either, 1, 2, 2, "",  1);
            Add("Gust Hawk",        ElementType.Wind, CardRarity.Common, RowPreference.Front,  1, 3, 2, "",  2);
            Add("Dust Devil",       ElementType.Wind, CardRarity.Common, RowPreference.Either, 1, 2, 2, "",  4);
            Add("Cloud Puff",       ElementType.Wind, CardRarity.Common, RowPreference.Back,   1, 1, 3, "",  5);
            Add("Wisp Moth",        ElementType.Wind, CardRarity.Common, RowPreference.Either, 1, 2, 1, "",  7);
            Add("Zephyr Dancer",    ElementType.Wind, CardRarity.Common, RowPreference.Either, 2, 2, 3, "",  3);
            Add("Gale Rat",         ElementType.Wind, CardRarity.Common, RowPreference.Front,  2, 3, 2, "",  6);
            Add("Draft Sparrow",    ElementType.Wind, CardRarity.Common, RowPreference.Either, 2, 3, 3, "",  8);
            Add("Air Jelly",        ElementType.Wind, CardRarity.Common, RowPreference.Either, 2, 2, 3, "",  9);
            Add("Sky Hopper",       ElementType.Wind, CardRarity.Common, RowPreference.Front,  2, 3, 2, "", 10);
            // Uncommons — T3 and T4
            Add("Tornado Imp",      ElementType.Wind, CardRarity.Uncommon, RowPreference.Either, 3, 4, 3, "Swaps two enemy positions",  12);
            Add("Feather Blade",    ElementType.Wind, CardRarity.Uncommon, RowPreference.Front,  3, 4, 3, "Attacks twice at half damage",  14);
            Add("Mist Weaver",      ElementType.Wind, CardRarity.Uncommon, RowPreference.Back,   3, 2, 4, "Gives ally 50% dodge for 1 round",  15);
            Add("Storm Caller",     ElementType.Wind, CardRarity.Uncommon, RowPreference.Back,   3, 3, 3, "Deals 1 damage to all enemies",  11);
            Add("Cyclone Djinn",    ElementType.Wind, CardRarity.Uncommon, RowPreference.Back,   3, 3, 5, "Bounces 1 damage back to attacker",  18);
            Add("Lightning Fox",    ElementType.Wind, CardRarity.Uncommon, RowPreference.Front,  4, 5, 3, "Attacks before all others",  13);
            Add("Squall Knight",    ElementType.Wind, CardRarity.Uncommon, RowPreference.Front,  4, 5, 4, "Charges: deals double first attack",  16);
            Add("Updraft Eagle",    ElementType.Wind, CardRarity.Uncommon, RowPreference.Either, 4, 4, 4, "Can attack any slot",  17);
            // Rares — T4 and T5
            Add("Vortex Elemental", ElementType.Wind, CardRarity.Rare, RowPreference.Either, 4, 5, 5, "Shuffles all enemy positions",  21);
            Add("Sky Lancer",       ElementType.Wind, CardRarity.Rare, RowPreference.Front, 4, 5, 4, "Ignores taunt",  22);
            Add("Tempest Lord",     ElementType.Wind, CardRarity.Rare, RowPreference.Front, 5, 6, 6, "Attacks twice",  19);
            Add("Thunder Roc",      ElementType.Wind, CardRarity.Rare, RowPreference.Front, 5, 7, 5, "Stuns target for 1 round on hit",  20);
            Add("Storm Dragon",     ElementType.Wind, CardRarity.Rare, RowPreference.Front, 5, 5, 6, "Hits all enemies for 1 each round",  23);
            // Legendary — T5 and T6
            Add("Eye of the Storm", ElementType.Wind, CardRarity.Legendary, RowPreference.Either, 5, 7, 7, "Attacks all enemies simultaneously",  24);
            Add("Typhoon Sovereign",ElementType.Wind, CardRarity.Legendary, RowPreference.Front, 6, 9, 9, "All allies attack twice",  25);
        }

        private static void Add(string name, ElementType element, CardRarity rarity,
            RowPreference row, int tier, int attack, int health, string ability, int spriteIndex)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.CardName = name;
            card.Element = element;
            card.Rarity = rarity;
            card.Row = row;
            card.Tier = tier;
            card.Cost = tier; // Cost always equals tier
            card.Attack = attack;
            card.Health = health;
            card.AbilityText = ability;
            card.SpriteIndex = spriteIndex;
            allCards.Add(card);
        }
    }
}
