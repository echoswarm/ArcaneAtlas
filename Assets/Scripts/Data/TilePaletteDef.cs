using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ArcaneAtlas.Data
{
    /// <summary>
    /// Maps abstract TileKeys to concrete Tile assets for a specific biome.
    /// Swap the TilePaletteDef and the same room blueprint renders with different art.
    /// Create one per biome (e.g. AncientForest, GloomHollows, Placeholder).
    /// </summary>
    [CreateAssetMenu(fileName = "NewTilePalette", menuName = "Arcane Atlas/Tile Palette Def")]
    public class TilePaletteDef : ScriptableObject
    {
        public string PaletteName;

        [Header("Tile Mappings")]
        public TileMapping[] Mappings;

        [Header("Character Sprites (legacy — use CharacterDefs when available)")]
        public Sprite PlayerSprite;
        public Sprite NpcSprite;
        public Sprite BossSprite;

        [Header("Character Definitions (animated)")]
        public CharacterDef PlayerCharacter;
        public CharacterDef NpcCharacter;
        public CharacterDef BossCharacter;

        /// <summary>
        /// Resolves a TileKey to a Tile asset. Returns null if unmapped.
        /// For runtime performance, call BuildLookup() once and use the dictionary.
        /// </summary>
        public TileBase GetTile(TileKey key)
        {
            if (Mappings == null) return null;
            for (int i = 0; i < Mappings.Length; i++)
            {
                if (Mappings[i].Key == key)
                    return Mappings[i].Tile;
            }
            return null;
        }

        /// <summary>
        /// Builds a fast lookup dictionary. Call once at room load, then use the dict.
        /// </summary>
        public System.Collections.Generic.Dictionary<TileKey, TileBase> BuildLookup()
        {
            var dict = new System.Collections.Generic.Dictionary<TileKey, TileBase>();
            if (Mappings == null) return dict;
            foreach (var m in Mappings)
            {
                if (m.Tile != null && !dict.ContainsKey(m.Key))
                    dict[m.Key] = m.Tile;
            }
            return dict;
        }

        /// <summary>
        /// Returns which tilemap sorting layer a TileKey should be painted on.
        /// </summary>
        public static TileLayer GetLayer(TileKey key)
        {
            switch (key)
            {
                // Ground layer
                case TileKey.Ground:
                case TileKey.GroundAlt:
                case TileKey.Path:
                case TileKey.WallN:
                case TileKey.WallS:
                case TileKey.WallE:
                case TileKey.WallW:
                case TileKey.WallCornerNW:
                case TileKey.WallCornerNE:
                case TileKey.WallCornerSW:
                case TileKey.WallCornerSE:
                case TileKey.WallInnerNW:
                case TileKey.WallInnerNE:
                case TileKey.WallInnerSW:
                case TileKey.WallInnerSE:
                case TileKey.DoorN:
                case TileKey.DoorS:
                case TileKey.DoorE:
                case TileKey.DoorW:
                case TileKey.Water:
                case TileKey.WaterEdge:
                    return TileLayer.Ground;

                // Detail layer
                case TileKey.GrassDetail:
                case TileKey.FlowerDetail:
                case TileKey.CrackDetail:
                case TileKey.MossDetail:
                    return TileLayer.Detail;

                // Shadow layer
                case TileKey.ShadowWallN:
                case TileKey.ShadowWallW:
                case TileKey.ShadowCornerNW:
                case TileKey.ShadowFull:
                    return TileLayer.Shadow;

                // Props below player
                case TileKey.TreeBase:
                case TileKey.RockSmall:
                case TileKey.RockLarge:
                case TileKey.BushBase:
                case TileKey.Crate:
                case TileKey.Chest:
                    return TileLayer.PropsBelow;

                // Props above player (walk-behind)
                case TileKey.TreeCrown:
                case TileKey.BushTop:
                case TileKey.RockTop:
                    return TileLayer.PropsAbove;

                // Overlay
                case TileKey.OverlayVines:
                case TileKey.OverlayFog:
                    return TileLayer.Overlay;

                // Collision
                case TileKey.CollisionSolid:
                case TileKey.CollisionWater:
                    return TileLayer.Collision;

                // Markers don't render
                default:
                    return TileLayer.None;
            }
        }
    }

    [Serializable]
    public struct TileMapping
    {
        public TileKey Key;
        public TileBase Tile;
    }

    /// <summary>
    /// Which tilemap layer to paint a tile on. Matches the sorting layers from SortingLayerSetup.
    /// </summary>
    public enum TileLayer
    {
        None = -1,
        Ground = 0,
        Detail = 1,
        Shadow = 2,
        PropsBelow = 3,
        PropsAbove = 4,
        Overlay = 5,
        Collision = 6,
    }
}
