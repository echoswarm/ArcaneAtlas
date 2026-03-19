using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Core
{
    public static class RoomGenerator
    {
        private const int GRID_SIZE = 5;

        public static Dictionary<Vector2Int, RoomData> Generate(int targetRoomCount = 10)
        {
            var rooms = new Dictionary<Vector2Int, RoomData>();
            var rng = new System.Random();

            // Start at center
            var start = new Vector2Int(2, 2);
            rooms[start] = CreateRoom(start, rng);

            var current = start;

            // Random walk to populate rooms
            int attempts = 0;
            while (rooms.Count < targetRoomCount && attempts < 200)
            {
                attempts++;
                var dirs = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                var dir = dirs[rng.Next(dirs.Length)];
                var next = current + dir;

                if (next.x >= 0 && next.x < GRID_SIZE && next.y >= 0 && next.y < GRID_SIZE)
                {
                    if (!rooms.ContainsKey(next))
                    {
                        rooms[next] = CreateRoom(next, rng);
                        current = next;
                    }
                    else
                    {
                        current = next;
                    }
                }

                if (attempts % 20 == 0)
                {
                    var keys = new List<Vector2Int>(rooms.Keys);
                    current = keys[rng.Next(keys.Count)];
                }
            }

            // Set exits based on neighbor connectivity
            foreach (var kvp in rooms)
            {
                var pos = kvp.Key;
                var room = kvp.Value;
                room.ExitUp = rooms.ContainsKey(pos + Vector2Int.up);
                room.ExitDown = rooms.ContainsKey(pos + Vector2Int.down);
                room.ExitLeft = rooms.ContainsKey(pos + Vector2Int.left);
                room.ExitRight = rooms.ContainsKey(pos + Vector2Int.right);
            }

            // BFS difficulty + room type assignment
            AssignDifficultyTiers(rooms, start);
            AssignRoomTypes(rooms, start, rng);

            return rooms;
        }

        /// <summary>
        /// BFS from entry room to assign difficulty tiers based on distance.
        /// Distance 0-1 = T1, 2 = T2, 3 = T3, 4 = T4, 5+ = T5. Boss = T6.
        /// </summary>
        private static void AssignDifficultyTiers(Dictionary<Vector2Int, RoomData> rooms, Vector2Int start)
        {
            var distances = new Dictionary<Vector2Int, int>();
            var queue = new Queue<Vector2Int>();

            queue.Enqueue(start);
            distances[start] = 0;

            while (queue.Count > 0)
            {
                var pos = queue.Dequeue();
                int dist = distances[pos];

                var neighbors = new Vector2Int[]
                {
                    pos + Vector2Int.up, pos + Vector2Int.down,
                    pos + Vector2Int.left, pos + Vector2Int.right
                };

                foreach (var n in neighbors)
                {
                    if (rooms.ContainsKey(n) && !distances.ContainsKey(n))
                    {
                        distances[n] = dist + 1;
                        queue.Enqueue(n);
                    }
                }
            }

            foreach (var kvp in distances)
            {
                var room = rooms[kvp.Key];
                int dist = kvp.Value;

                if (dist <= 1) room.DifficultyTier = 1;
                else if (dist == 2) room.DifficultyTier = 2;
                else if (dist == 3) room.DifficultyTier = 3;
                else if (dist == 4) room.DifficultyTier = 4;
                else room.DifficultyTier = 5;
            }
        }

        /// <summary>
        /// Boss = farthest room from entry. Entry = NPC T1.
        /// Others: 60% NPC, 15% Treasure, 10% Event, 15% Empty.
        /// </summary>
        private static void AssignRoomTypes(Dictionary<Vector2Int, RoomData> rooms, Vector2Int start, System.Random rng)
        {
            // Find boss room: highest difficulty tier (farthest from start)
            Vector2Int bossPos = start;
            int maxTier = 0;
            foreach (var kvp in rooms)
            {
                if (kvp.Value.DifficultyTier > maxTier)
                {
                    maxTier = kvp.Value.DifficultyTier;
                    bossPos = kvp.Key;
                }
            }

            rooms[bossPos].Type = RoomType.Boss;
            rooms[bossPos].DifficultyTier = 6;
            rooms[bossPos].NpcCount = 1;

            // Entry room is always NPC T1
            rooms[start].Type = RoomType.NPC;
            rooms[start].DifficultyTier = 1;

            // Assign remaining rooms
            foreach (var kvp in rooms)
            {
                if (kvp.Key == bossPos || kvp.Key == start) continue;

                int roll = rng.Next(100);
                if (roll < 60)
                    kvp.Value.Type = RoomType.NPC;
                else if (roll < 75)
                    kvp.Value.Type = RoomType.Treasure;
                else if (roll < 85)
                    kvp.Value.Type = RoomType.Event;
                else
                    kvp.Value.Type = RoomType.Empty;
            }

            // Assign NPC counts based on difficulty tier
            foreach (var kvp in rooms)
            {
                var room = kvp.Value;
                if (room.Type == RoomType.Boss)
                    room.NpcCount = 1;
                else if (room.Type == RoomType.NPC)
                    room.NpcCount = GetNpcCountForTier(room.DifficultyTier, rng);
                else
                    room.NpcCount = 0;
            }
        }

        /// <summary>
        /// T1: 1, T2: 1-2, T3: 2, T4: 2-3, T5: 3
        /// </summary>
        private static int GetNpcCountForTier(int tier, System.Random rng)
        {
            switch (tier)
            {
                case 1: return 1;
                case 2: return 1 + rng.Next(2);
                case 3: return 2;
                case 4: return 2 + rng.Next(2);
                case 5: return 3;
                default: return 1;
            }
        }

        private static RoomData CreateRoom(Vector2Int pos, System.Random rng)
        {
            return new RoomData
            {
                GridX = pos.x,
                GridY = pos.y,
                RoomVariant = rng.Next(3),
                NpcCount = 0,
            };
        }
    }
}
