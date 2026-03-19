using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.UI
{
    public class MinimapUI : MonoBehaviour
    {
        [Header("Grid")]
        public RectTransform gridContainer;

        private Image[,] cells;
        private Dictionary<Vector2Int, RoomData> roomMap;

        private Color emptyColor = new Color(0.1f, 0.1f, 0.15f, 0.3f);
        private Color undiscoveredColor = new Color(0.2f, 0.2f, 0.25f, 0.5f); // Gray for unseen rooms
        private Color npcColor = new Color(0.3f, 0.3f, 0.4f, 0.8f);
        private Color treasureColor = new Color(0.9f, 0.8f, 0.2f, 0.8f); // Gold
        private Color eventColor = new Color(0.6f, 0.3f, 0.9f, 0.8f); // Purple
        private Color bossColor = new Color(0.9f, 0.15f, 0.15f, 0.9f); // Red — always visible
        private Color clearedColor = new Color(0.25f, 0.5f, 0.25f, 0.7f); // Green
        private Color currentColor = new Color(0.83f, 0.66f, 0.26f, 1f); // Gold highlight

        private const int GRID_SIZE = 5;

        public void Initialize(Dictionary<Vector2Int, RoomData> rooms)
        {
            roomMap = rooms;

            // Find or create cells from gridContainer children
            if (cells == null)
            {
                cells = new Image[GRID_SIZE, GRID_SIZE];
                int childIndex = 0;
                // Grid layout: row 4 (top) first, row 0 (bottom) last
                for (int y = GRID_SIZE - 1; y >= 0; y--)
                {
                    for (int x = 0; x < GRID_SIZE; x++)
                    {
                        if (childIndex < gridContainer.childCount)
                        {
                            cells[x, y] = gridContainer.GetChild(childIndex).GetComponent<Image>();
                            childIndex++;
                        }
                    }
                }
            }

            // Initial coloring: all rooms gray except boss (always visible)
            for (int y = 0; y < GRID_SIZE; y++)
            {
                for (int x = 0; x < GRID_SIZE; x++)
                {
                    if (cells[x, y] == null) continue;
                    var pos = new Vector2Int(x, y);

                    if (!rooms.ContainsKey(pos))
                    {
                        cells[x, y].color = emptyColor;
                    }
                    else if (rooms[pos].Type == RoomType.Boss)
                    {
                        // Boss room always visible from start
                        cells[x, y].color = bossColor;
                    }
                    else
                    {
                        cells[x, y].color = undiscoveredColor;
                    }
                }
            }
        }

        public void UpdatePlayerPosition(int gridX, int gridY)
        {
            if (cells == null || roomMap == null) return;

            // Recolor all cells based on room state
            for (int y = 0; y < GRID_SIZE; y++)
            {
                for (int x = 0; x < GRID_SIZE; x++)
                {
                    if (cells[x, y] == null) continue;
                    var pos = new Vector2Int(x, y);
                    cells[x, y].color = GetCellColor(pos);
                }
            }

            // Highlight current cell
            if (gridX >= 0 && gridX < GRID_SIZE && gridY >= 0 && gridY < GRID_SIZE)
            {
                if (cells[gridX, gridY] != null)
                    cells[gridX, gridY].color = currentColor;
            }
        }

        private Color GetCellColor(Vector2Int pos)
        {
            if (!roomMap.ContainsKey(pos))
                return emptyColor;

            var room = roomMap[pos];

            // Boss always visible
            if (room.Type == RoomType.Boss)
                return room.IsCleared ? clearedColor : bossColor;

            // Undiscovered rooms stay gray
            if (!room.IsDiscovered)
                return undiscoveredColor;

            // Cleared rooms turn green
            if (room.IsCleared)
                return clearedColor;

            // Discovered rooms show type color
            switch (room.Type)
            {
                case RoomType.NPC: return npcColor;
                case RoomType.Treasure: return treasureColor;
                case RoomType.Event: return eventColor;
                case RoomType.Empty: return new Color(0.25f, 0.25f, 0.3f, 0.6f);
                default: return npcColor;
            }
        }
    }
}
