using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using ArcaneAtlas.Data;
using ArcaneAtlas.UI;

namespace ArcaneAtlas.Core
{
    public class ExplorationManager : MonoBehaviour
    {
        public static ExplorationManager Instance { get; private set; }

        [Header("World Objects")]
        public GameObject explorationRoot;
        public PlayerController player;
        public SpriteRenderer roomBackground; // Fallback when no template
        public CameraController cameraController;

        [Header("NPC Container")]
        public Transform npcContainer;

        [Header("Room Templates")]
        public Transform roomContainer; // Parent for instantiated room template prefabs

        [Header("Blueprint Painting")]
        public TilePaletteDef tilePalette; // Assign a TilePaletteDef (Placeholder or biome-specific)

        [Header("Room Config")]
        public Color[] roomColors;

        [Header("Exit Indicators")]
        public GameObject exitUp;
        public GameObject exitDown;
        public GameObject exitLeft;
        public GameObject exitRight;

        private Dictionary<Vector2Int, RoomData> roomMap;
        private Vector2Int currentGridPos;
        private bool isExploring = false;
        private bool isPaused = false;

        private const float ROOM_WIDTH = 17f;
        private const float ROOM_HEIGHT = 10f;

        // Cached sprite for NPC spawning
        private Sprite npcSprite;

        // Room discovery counter
        private int roomsDiscovered = 0;

        // Current room template (null when using fallback)
        private GameObject currentRoomInstance;
        private RoomTemplateData currentTemplate;
        private RoomBlueprint currentBlueprint;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void EnterExploration()
        {
            isExploring = true;
            explorationRoot.SetActive(true);

            // Initialize room template system
            RoomTemplateLoader.Initialize();

            // Find or add CameraController on main camera
            if (cameraController == null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    cameraController = cam.GetComponent<CameraController>();
                    if (cameraController == null)
                        cameraController = cam.gameObject.AddComponent<CameraController>();
                }
            }

            // Apply character sprites/animations from palette
            if (tilePalette != null && player != null && player.spriteRenderer != null)
            {
                // Player: prefer CharacterDef (animated) over static sprite
                if (tilePalette.PlayerCharacter != null)
                {
                    var animator = player.GetComponent<CharacterAnimator>();
                    if (animator == null) animator = player.gameObject.AddComponent<CharacterAnimator>();
                    animator.SetCharacter(tilePalette.PlayerCharacter);
                    player.transform.localScale = new Vector3(2f, 2f, 1f);
                }
                else if (tilePalette.PlayerSprite != null)
                {
                    player.spriteRenderer.sprite = tilePalette.PlayerSprite;
                }

                // NPC sprite: prefer CharacterDef preview, fall back to static
                if (tilePalette.NpcCharacter != null)
                    npcSprite = tilePalette.NpcCharacter.PreviewSprite;
                else if (tilePalette.NpcSprite != null)
                    npcSprite = tilePalette.NpcSprite;
            }

            // Fall back to player sprite if no NPC sprite set
            if (npcSprite == null && player != null && player.spriteRenderer != null)
                npcSprite = player.spriteRenderer.sprite;

            // Generate room map (fresh each zone entry — run resets on re-entry)
            roomMap = RoomGenerator.Generate(10);
            roomsDiscovered = 0;

            // Reset run-based NPC defeat tracking for quests
            QuestManager.OnZoneEntered(GameState.CurrentZone);

            // Find start room (center of grid)
            currentGridPos = new Vector2Int(2, 2);
            if (!roomMap.ContainsKey(currentGridPos))
                currentGridPos = roomMap.Keys.First();

            // Snap camera to start room
            Vector3 roomWorldPos = GridToWorld(currentGridPos);
            if (cameraController != null)
                cameraController.SnapTo(roomWorldPos);

            // Update player room center and position
            player.roomCenter = (Vector2)roomWorldPos;
            player.SetPosition(roomWorldPos);

            // Mark entry room as discovered (no event trigger for entry room)
            DiscoverRoom(currentGridPos);

            SetupRoom(currentGridPos);

            // Entry room is always NPC type, no special action needed

            // Initialize minimap
            var minimapUI = FindFirstObjectByType<MinimapUI>();
            if (minimapUI != null)
            {
                minimapUI.Initialize(roomMap);
                minimapUI.UpdatePlayerPosition(currentGridPos.x, currentGridPos.y);
            }

            // Update zone card count on HUD
            UpdateHUD();

            // Show zone entry narrative text
            ShowZoneEntryText();
        }

        public void ExitExploration()
        {
            isExploring = false;
            isPaused = false;
            ClearNpcs();

            // Clean up room template instance
            if (currentRoomInstance != null)
                Destroy(currentRoomInstance);
            currentTemplate = null;

            explorationRoot.SetActive(false);

            // Reset camera for Canvas UI
            if (cameraController != null)
                cameraController.SnapTo(new Vector3(0, 0, -10));

            // Auto-save on zone exit
            SaveSystem.Save();

            ScreenManager.Instance.GoBack();
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
            if (player != null)
                player.enabled = !paused;
        }

        void Update()
        {
            if (!isExploring || isPaused) return;
            if (cameraController != null && cameraController.IsSliding) return;

            if (player.IsAtExit(out string direction))
            {
                TryTransitionRoom(direction);
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ExitExploration();
            }
        }

        private void TryTransitionRoom(string direction)
        {
            if (!roomMap.ContainsKey(currentGridPos)) return;
            var currentRoom = roomMap[currentGridPos];
            if (!currentRoom.HasExitInDirection(direction)) return;

            Vector2Int offset = DirectionToOffset(direction);
            Vector2Int newGridPos = currentGridPos + offset;
            if (!roomMap.ContainsKey(newGridPos)) return;

            currentGridPos = newGridPos;

            // Camera slide to new room
            Vector3 targetWorld = GridToWorld(currentGridPos);
            if (cameraController != null)
                cameraController.SlideTo(targetWorld);

            // Update player room center
            player.roomCenter = (Vector2)targetWorld;

            // Reposition player at opposite edge of new room
            Vector2 entryPos = GetEntryPosition(direction, targetWorld);
            player.SetPosition(entryPos);

            // Discover room on first visit
            bool isFirstVisit = !roomMap[currentGridPos].IsDiscovered;
            DiscoverRoom(currentGridPos);

            SetupRoom(currentGridPos);

            // Update minimap
            var minimapUI = FindFirstObjectByType<MinimapUI>();
            if (minimapUI != null)
                minimapUI.UpdatePlayerPosition(currentGridPos.x, currentGridPos.y);

            UpdateHUD();

            // Trigger treasure/event on first visit
            if (isFirstVisit)
                OnFirstVisitRoomAction(roomMap[currentGridPos]);
        }

        /// <summary>
        /// Marks a room as discovered. On first visit, generates NPCs for NPC/Boss rooms.
        /// </summary>
        private void DiscoverRoom(Vector2Int gridPos)
        {
            var room = roomMap[gridPos];
            if (room.IsDiscovered) return;

            room.IsDiscovered = true;
            roomsDiscovered++;

            // Generate persistent NPCs on first visit
            if (room.Type == RoomType.NPC || room.Type == RoomType.Boss)
            {
                GenerateRoomNpcs(room);
            }
        }

        /// <summary>
        /// Generates NPC data for a room based on zone and difficulty tier.
        /// NPCs persist within the run via room.SpawnedNpcs.
        /// </summary>
        private void GenerateRoomNpcs(RoomData room)
        {
            room.SpawnedNpcs = new List<NpcData>();
            var zoneNpcs = NpcData.GetNpcsForZone(GameState.CurrentZone);
            var rng = new System.Random(room.GridX * 100 + room.GridY);

            if (room.Type == RoomType.Boss)
            {
                // Boss room: spawn the zone boss
                var boss = zoneNpcs.FirstOrDefault(n => n.IsBoss);
                if (boss != null)
                {
                    // Clone the boss data so run state is isolated
                    var bossClone = CloneNpc(boss);
                    bossClone.GenerateOpponentPool(6);
                    room.SpawnedNpcs.Add(bossClone);
                }
            }
            else
            {
                // NPC room: pick non-boss NPCs matching difficulty
                var eligible = zoneNpcs.Where(n => !n.IsBoss).ToArray();

                for (int i = 0; i < room.NpcCount; i++)
                {
                    var template = eligible[rng.Next(eligible.Length)];
                    var npc = CloneNpc(template);
                    npc.GenerateOpponentPool(room.DifficultyTier);
                    room.SpawnedNpcs.Add(npc);
                }
            }
        }

        private NpcData CloneNpc(NpcData source)
        {
            return new NpcData
            {
                Name = source.Name,
                Element = source.Element,
                Difficulty = source.Difficulty,
                DialogueLines = source.DialogueLines,
                IsBoss = source.IsBoss,
                BossCardName = source.BossCardName,
                IsDefeated = false,
            };
        }

        private void SetupRoom(Vector2Int gridPos)
        {
            var room = roomMap[gridPos];
            Vector3 roomCenter = GridToWorld(gridPos);

            // Destroy previous room instance
            if (currentRoomInstance != null)
                Destroy(currentRoomInstance);
            currentTemplate = null;
            currentBlueprint = null;

            bool roomRendered = false;

            // Priority 1: Blueprint painting (if a TilePaletteDef is assigned)
            if (tilePalette != null && roomContainer != null)
            {
                currentBlueprint = RoomBlueprintGenerator.Generate(room);
                currentRoomInstance = RoomPainter.Paint(currentBlueprint, tilePalette, roomCenter, roomContainer);
                roomRendered = currentRoomInstance != null;
            }

            // Priority 2: Prefab room templates
            if (!roomRendered)
            {
                string zoneName = GameState.CurrentZone;
                var template = RoomTemplateLoader.SelectTemplate(room, zoneName);

                if (template != null && roomContainer != null)
                {
                    currentRoomInstance = RoomTemplateLoader.InstantiateRoom(template, roomCenter, roomContainer);
                    currentTemplate = template;
                    roomRendered = true;
                }
            }

            // Show/hide fallback background
            if (roomBackground != null)
            {
                if (roomRendered)
                {
                    roomBackground.gameObject.SetActive(false);
                }
                else
                {
                    // Priority 3: Colored background (original fallback)
                    roomBackground.gameObject.SetActive(true);
                    if (roomColors != null && roomColors.Length > 0)
                        roomBackground.color = GetRoomBackgroundColor(room);
                    roomBackground.transform.position = roomCenter;
                }
            }

            // Show/hide exit indicators and position them relative to room
            if (exitUp != null) { exitUp.SetActive(room.ExitUp); exitUp.transform.position = roomCenter + new Vector3(0, 4.7f, 0); }
            if (exitDown != null) { exitDown.SetActive(room.ExitDown); exitDown.transform.position = roomCenter + new Vector3(0, -4.7f, 0); }
            if (exitLeft != null) { exitLeft.SetActive(room.ExitLeft); exitLeft.transform.position = roomCenter + new Vector3(-8.2f, 0, 0); }
            if (exitRight != null) { exitRight.SetActive(room.ExitRight); exitRight.transform.position = roomCenter + new Vector3(8.2f, 0, 0); }

            // Spawn NPCs from persistent data
            SpawnNpcs(room, roomCenter);

            // Update HUD room label with room type info
            var hud = FindFirstObjectByType<ExplorationHUD>();
            if (hud != null)
            {
                int roomIndex = new List<Vector2Int>(roomMap.Keys).IndexOf(gridPos) + 1;
                string typeLabel = GetRoomTypeLabel(room);
                hud.SetRoomLabel($"Room {roomIndex} of {roomMap.Count} - {typeLabel}");
            }
        }

        /// <summary>
        /// Returns a tinted background color based on room type.
        /// </summary>
        private Color GetRoomBackgroundColor(RoomData room)
        {
            if (roomColors == null || roomColors.Length == 0)
                return Color.gray;

            Color baseColor = roomColors[room.RoomVariant % roomColors.Length];

            switch (room.Type)
            {
                case RoomType.Boss:
                    return Color.Lerp(baseColor, new Color(0.8f, 0.1f, 0.1f), 0.3f); // Red tint
                case RoomType.Treasure:
                    return Color.Lerp(baseColor, new Color(0.9f, 0.8f, 0.2f), 0.2f); // Gold tint
                case RoomType.Event:
                    return Color.Lerp(baseColor, new Color(0.4f, 0.2f, 0.8f), 0.2f); // Purple tint
                case RoomType.Empty:
                    return Color.Lerp(baseColor, Color.gray, 0.3f); // Dimmed
                default:
                    return baseColor;
            }
        }

        private string GetRoomTypeLabel(RoomData room)
        {
            switch (room.Type)
            {
                case RoomType.NPC: return $"NPC (T{room.DifficultyTier})";
                case RoomType.Boss: return "BOSS";
                case RoomType.Treasure: return "Treasure";
                case RoomType.Event: return "Event";
                case RoomType.Empty: return "Empty";
                default: return "";
            }
        }

        private void SpawnNpcs(RoomData room, Vector3 roomCenter)
        {
            ClearNpcs();
            if (npcContainer == null || npcSprite == null) return;

            // Use persistent SpawnedNpcs if available
            if (room.SpawnedNpcs == null || room.SpawnedNpcs.Count == 0) return;

            // Get spawn positions: blueprint markers > template > fallback
            Vector2[] spawnPositions;
            if (currentBlueprint != null && currentBlueprint.NpcSpawns.Count > 0)
            {
                // Use blueprint marker positions (converted to world offsets from center)
                spawnPositions = new Vector2[room.SpawnedNpcs.Count];
                for (int i = 0; i < room.SpawnedNpcs.Count; i++)
                {
                    if (i < currentBlueprint.NpcSpawns.Count)
                    {
                        Vector3 worldPos = RoomPainter.TileToWorld(currentBlueprint.NpcSpawns[i], Vector3.zero);
                        spawnPositions[i] = new Vector2(worldPos.x, worldPos.y);
                    }
                    else
                    {
                        // More NPCs than markers — offset from last marker
                        float angle = i * 1.2f;
                        spawnPositions[i] = new Vector2(Mathf.Cos(angle) * 1.5f, Mathf.Sin(angle) * 1.0f);
                    }
                }
            }
            else
            {
                spawnPositions = RoomTemplateLoader.GetNpcSpawnPositions(currentTemplate, room.SpawnedNpcs.Count);
            }

            for (int i = 0; i < room.SpawnedNpcs.Count; i++)
            {
                var data = room.SpawnedNpcs[i];

                var npcGO = new GameObject($"NPC_{data.Name}_{i}");
                npcGO.transform.SetParent(npcContainer, false);
                var sr = npcGO.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Player";
                sr.sortingOrder = 0;

                // Boss gets a distinct sprite/character and larger scale
                if (data.IsBoss)
                {
                    if (tilePalette != null && tilePalette.BossCharacter != null)
                    {
                        var bossAnim = npcGO.AddComponent<CharacterAnimator>();
                        bossAnim.SetCharacter(tilePalette.BossCharacter);
                    }
                    else
                    {
                        sr.sprite = (tilePalette != null && tilePalette.BossSprite != null) ? tilePalette.BossSprite : npcSprite;
                    }
                    npcGO.transform.localScale = new Vector3(3f, 3f, 1f); // Boss larger
                }
                else
                {
                    if (tilePalette != null && tilePalette.NpcCharacter != null)
                    {
                        var npcAnim = npcGO.AddComponent<CharacterAnimator>();
                        npcAnim.SetCharacter(tilePalette.NpcCharacter);
                    }
                    else
                    {
                        sr.sprite = npcSprite;
                    }
                    npcGO.transform.localScale = new Vector3(2f, 2f, 1f);
                }

                // Position: Boss gets special placement
                if (data.IsBoss)
                {
                    if (currentBlueprint != null)
                    {
                        Vector3 bossWorld = RoomPainter.TileToWorld(currentBlueprint.BossSpawn, roomCenter);
                        npcGO.transform.position = bossWorld;
                    }
                    else
                    {
                        npcGO.transform.position = new Vector3(roomCenter.x, roomCenter.y + 0.5f, 0f);
                    }
                }
                else
                {
                    Vector2 spawnPos = spawnPositions[i];
                    npcGO.transform.position = new Vector3(roomCenter.x + spawnPos.x, roomCenter.y + spawnPos.y, 0f);
                }

                var npcCtrl = npcGO.AddComponent<NpcController>();
                npcCtrl.spriteRenderer = sr;
                npcCtrl.Initialize(data, sr.sprite);
            }
        }

        /// <summary>
        /// Called when a combat in the current room ends. Checks if room is fully cleared.
        /// </summary>
        public void OnRoomNpcDefeated()
        {
            var room = roomMap[currentGridPos];
            if (room.SpawnedNpcs == null) return;

            bool allDefeated = room.SpawnedNpcs.All(n => n.IsDefeated);
            if (allDefeated)
                room.IsCleared = true;
        }

        private void ClearNpcs()
        {
            if (npcContainer == null) return;
            foreach (Transform child in npcContainer)
                Destroy(child.gameObject);
        }

        private void UpdateHUD()
        {
            var hud = FindFirstObjectByType<ExplorationHUD>();
            if (hud != null)
                hud.UpdateZoneCardCount();
        }

        // --- Zone Entry Narrative ---

        /// <summary>
        /// Shows a fading narrative text overlay on zone entry.
        /// </summary>
        private void ShowZoneEntryText()
        {
            var zone = ZoneData.GetByName(GameState.CurrentZone);
            if (zone == null || string.IsNullOrEmpty(zone.ZoneEntryText)) return;

            // Use the encounter UI to display the narrative briefly
            ShowRoomMessage(GameState.CurrentZone, zone.ZoneEntryText);
        }

        // --- Treasure Room ---

        /// <summary>
        /// Called when player enters a treasure room. Shows reward without combat.
        /// </summary>
        public void HandleTreasureRoom(RoomData room)
        {
            if (room.IsCleared) return; // Already collected
            room.IsCleared = true;

            int tier = room.DifficultyTier;
            var rng = new System.Random(room.GridX * 1000 + room.GridY);
            int roll = rng.Next(100);

            string rewardText;

            if (tier <= 2)
            {
                if (roll < 40) { int gold = 5; GameState.Gold += gold; rewardText = $"Found {gold} gold!"; }
                else if (roll < 75) { rewardText = GiveRandomCard(CardRarity.Common, rng); }
                else { GameState.Packs++; rewardText = "Found a Basic Pack!"; }
            }
            else if (tier <= 4)
            {
                if (roll < 40) { int gold = 10; GameState.Gold += gold; rewardText = $"Found {gold} gold!"; }
                else if (roll < 75) { rewardText = GiveRandomCard(CardRarity.Uncommon, rng); }
                else { GameState.Packs++; rewardText = "Found a Standard Pack!"; }
            }
            else
            {
                if (roll < 40) { int gold = 15; GameState.Gold += gold; rewardText = $"Found {gold} gold!"; }
                else if (roll < 75) { rewardText = GiveRandomCard(CardRarity.Rare, rng); }
                else { GameState.Packs++; rewardText = "Found a Premium Pack!"; }
            }

            // Show treasure reward via encounter UI as a simple dialogue
            ShowRoomMessage("Treasure!", rewardText);
        }

        private string GiveRandomCard(CardRarity maxRarity, System.Random rng)
        {
            var pool = CardDatabase.GetCardsByRarity(maxRarity);
            if (pool.Count == 0) return "The chest was empty...";

            var card = pool[rng.Next(pool.Count)];
            PlayerCollection.AddCard(card);
            QuestManager.OnCardAdded(card);
            return $"Found {card.CardName}!";
        }

        // --- Event Room ---

        /// <summary>
        /// Generates a random event for an event room.
        /// Types: Wandering Merchant, Lore NPC, Card Shrine, Rest Point
        /// </summary>
        public void HandleEventRoom(RoomData room)
        {
            if (room.IsCleared) return;

            var rng = new System.Random(room.GridX * 1000 + room.GridY);
            int eventType = rng.Next(4);

            switch (eventType)
            {
                case 0: // Wandering Merchant — offers 3 cards at 50% discount
                    ShowRoomMessage("Wandering Merchant", "A merchant offers rare wares at half price.\n(Merchant shop coming in a future update!)");
                    room.IsCleared = true;
                    break;

                case 1: // Lore NPC — gives small gold tip
                    int gold = 2 + rng.Next(4); // 2-5 gold
                    GameState.Gold += gold;
                    ShowRoomMessage("Traveler's Tale", $"An old traveler shares stories of the cartographer.\n\"He mapped this very path decades ago...\"\n\nReceived {gold} gold.");
                    room.IsCleared = true;
                    break;

                case 2: // Card Shrine — placeholder for sacrifice mechanic
                    ShowRoomMessage("Card Shrine", "A glowing shrine hums with power.\n\"Sacrifice a card to receive something greater...\"\n(Card sacrifice coming in a future update!)");
                    room.IsCleared = true;
                    break;

                case 3: // Rest Point — gives 5 gold
                    GameState.Gold += 5;
                    ShowRoomMessage("Rest Point", "A peaceful clearing offers respite.\nYou rest and find 5 gold among the roots.");
                    room.IsCleared = true;
                    break;
            }
        }

        /// <summary>
        /// Shows a simple message popup using the encounter UI system.
        /// </summary>
        private void ShowRoomMessage(string title, string message)
        {
            SetPaused(true);
            var encounterUI = FindFirstObjectByType<EncounterUI>();
            if (encounterUI != null)
            {
                // Create a temporary NpcData for display
                var fakeNpc = new NpcData
                {
                    Name = title,
                    Element = NpcData.GetZoneElement(GameState.CurrentZone),
                    DialogueLines = new[] { message }
                };
                encounterUI.Show(fakeNpc);
                encounterUI.ShowDialogueLine(message);

                // Show only the Leave button (hide Duel/Trade)
                if (encounterUI.choicePanel != null) encounterUI.choicePanel.SetActive(true);
                if (encounterUI.btnNext != null) encounterUI.btnNext.gameObject.SetActive(false);
                if (encounterUI.btnDuel != null) encounterUI.btnDuel.gameObject.SetActive(false);
                if (encounterUI.btnTrade != null) encounterUI.btnTrade.gameObject.SetActive(false);
                if (encounterUI.btnLeave != null) encounterUI.btnLeave.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Called on first visit to a room — triggers treasure/event if applicable.
        /// </summary>
        public void OnFirstVisitRoomAction(RoomData room)
        {
            switch (room.Type)
            {
                case RoomType.Treasure:
                    HandleTreasureRoom(room);
                    break;
                case RoomType.Event:
                    HandleEventRoom(room);
                    break;
            }
        }

        // --- Helpers ---

        private Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * ROOM_WIDTH, gridPos.y * ROOM_HEIGHT, 0f);
        }

        private Vector2Int DirectionToOffset(string direction)
        {
            switch (direction)
            {
                case "up": return Vector2Int.up;
                case "down": return Vector2Int.down;
                case "left": return Vector2Int.left;
                case "right": return Vector2Int.right;
                default: return Vector2Int.zero;
            }
        }

        private Vector2 GetEntryPosition(string exitDirection, Vector3 roomWorldPos)
        {
            switch (exitDirection)
            {
                case "up":    return new Vector2(roomWorldPos.x, roomWorldPos.y + player.roomMin.y + 1f);
                case "down":  return new Vector2(roomWorldPos.x, roomWorldPos.y + player.roomMax.y - 1f);
                case "left":  return new Vector2(roomWorldPos.x + player.roomMax.x - 1f, roomWorldPos.y);
                case "right": return new Vector2(roomWorldPos.x + player.roomMin.x + 1f, roomWorldPos.y);
                default:      return (Vector2)roomWorldPos;
            }
        }

        /// <summary>
        /// Public access to current room map for minimap and other systems.
        /// </summary>
        public Dictionary<Vector2Int, RoomData> GetRoomMap() => roomMap;
    }
}
