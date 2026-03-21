using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using ArcaneAtlas.Core;
using ArcaneAtlas.Data;
using ArcaneAtlas.UI;
using System.IO;

namespace ArcaneAtlas.Editor
{
    /// <summary>
    /// Generates the GameCanvas prefab containing all screens, HUD, and ScreenManager.
    /// Run via Arcane Atlas > Build Canvas (Ctrl+Shift+G).
    ///
    /// Pattern: Static Generate() method builds hierarchy in code, saves as prefab.
    /// Delete the prefab, re-run the tool, get the exact same game back.
    /// </summary>
    public static class CanvasBuilderTool
    {
        private const string PREFAB_PATH = "Assets/Prefabs/UI/GameCanvas.prefab";

        public static void Generate()
        {
            // 1. Ensure output folder exists
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/UI");

            // 2. Delete existing prefab
            if (File.Exists(PREFAB_PATH))
            {
                AssetDatabase.DeleteAsset(PREFAB_PATH);
                Debug.Log("[CanvasBuilderTool] Deleted existing GameCanvas.prefab");
            }

            // 3. Build hierarchy as temp scene objects
            GameObject root = BuildCanvasHierarchy();

            // 4. Save as prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);

            // 5. Clean up temp scene objects
            Object.DestroyImmediate(root);

            // 6. Refresh
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 7. Log summary
            if (prefab != null)
            {
                int childCount = CountChildren(prefab.transform);
                Debug.Log($"[CanvasBuilderTool] Generated GameCanvas.prefab ({childCount} objects)");
                Debug.Log("[CanvasBuilderTool] Screens: MainMenu, Town, WorldMap, Exploration, Combat, Collection, TownShop, Settings");
                Debug.Log("[CanvasBuilderTool] Overlays: HUD_Persistent");
                Debug.Log("[CanvasBuilderTool] Components: ScreenManager (wired), MainMenuUI, TownUI, WorldMapUI, ExplorationHUD, 4x PlaceholderScreenUI, HUD");
            }
            else
            {
                Debug.LogError("[CanvasBuilderTool] Failed to save prefab!");
            }
        }

        // ═══════════════════════════════════════
        //  Root hierarchy builder
        // ═══════════════════════════════════════

        private static GameObject BuildCanvasHierarchy()
        {
            var root = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Build all screens
            var mainMenu = BuildMainMenu(root.transform);
            var town = BuildTownScreen(root.transform);
            var worldMap = BuildWorldMap(root.transform);
            var exploration = BuildExplorationHUD(root.transform);
            var combat = BuildCombatScreen(root.transform);
            var collection = BuildCollectionScreen(root.transform);
            var townShop = BuildTownShopScreen(root.transform);
            var settings = BuildSettingsScreen(root.transform);
            var packOpening = BuildPackOpeningScreen(root.transform);
            var intro = BuildIntroScreen(root.transform);
            var hud = BuildHUD(root.transform);

            // Encounter overlay (not managed by ScreenManager — standalone modal)
            // Stays active so EncounterUI.Start() wires buttons and FindFirstObjectByType works.
            // Visually hidden via CanvasGroup until Show() is called.
            var encounter = BuildEncounterScreen(root.transform);
            var encounterCG = encounter.AddComponent<CanvasGroup>();
            encounterCG.alpha = 0f;
            encounterCG.blocksRaycasts = false;
            encounterCG.interactable = false;

            // Add and wire ScreenManager
            var sm = root.AddComponent<ScreenManager>();
            sm.screenMainMenu = mainMenu;
            sm.screenTown = town;
            sm.screenWorldMap = worldMap;
            sm.screenExploration = exploration;
            sm.screenCombat = combat;
            sm.screenCollection = collection;
            sm.screenTownShop = townShop;
            sm.screenSettings = settings;
            sm.screenPackOpening = packOpening;
            sm.screenIntro = intro;
            sm.hudPersistent = hud;

            // Set initial visibility: only MainMenu active, HUD hidden
            mainMenu.SetActive(true);
            town.SetActive(false);
            worldMap.SetActive(false);
            exploration.SetActive(false);
            combat.SetActive(false);
            collection.SetActive(false);
            townShop.SetActive(false);
            settings.SetActive(false);
            packOpening.SetActive(false);
            intro.SetActive(false);
            hud.SetActive(false);

            return root;
        }

        // ═══════════════════════════════════════
        //  Screen builders
        // ═══════════════════════════════════════

        private static GameObject BuildMainMenu(Transform parent)
        {
            var root = CreatePanel("Screen_MainMenu", parent);

            var bg = CreatePanel("BG_Title", root.transform);
            StretchFill(bg);

            // Each quadrant is a RectMask2D panel with a ParallaxBackgroundController.
            // MainMenuUI.OnEnable loads the biome config at runtime so layers are sized correctly.
            var plxFire  = CreateParallaxQuadrant("Quad_Fire",  bg.transform,
                new Vector2(0f,   0.5f), new Vector2(0.5f, 1f));
            var plxWind  = CreateParallaxQuadrant("Quad_Wind",  bg.transform,
                new Vector2(0.5f, 0.5f), new Vector2(1f,   1f));
            var plxWater = CreateParallaxQuadrant("Quad_Water", bg.transform,
                new Vector2(0f,   0f),   new Vector2(0.5f, 0.5f));
            var plxEarth = CreateParallaxQuadrant("Quad_Earth", bg.transform,
                new Vector2(0.5f, 0f),   new Vector2(1f,   0.5f));

            // Full-screen showcase overlay — sits above BG_Title (index 1) but below Logo
            // and ButtonGroup so title text and buttons always render on top.
            // Inactive by default; TitleScreenBiomeShowcase activates it per cycle.
            var overlayGo = new GameObject("Showcase_Overlay", typeof(RectTransform));
            overlayGo.transform.SetParent(root.transform, false);
            var overlayRt = overlayGo.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;
            overlayGo.AddComponent<RectMask2D>();
            var overlayCtrl = overlayGo.AddComponent<ParallaxBackgroundController>();
            overlayGo.SetActive(false);

            var logo = CreateTMP("Logo", root.transform, "ARCANE ATLAS", 72f, ElementColors.Gold,
                TextAlignmentOptions.Center, FontStyles.Bold);
            var logoRT = logo.GetComponent<RectTransform>();
            logoRT.anchorMin = new Vector2(0.1f, 0.55f);
            logoRT.anchorMax = new Vector2(0.9f, 0.85f);
            logoRT.offsetMin = logoRT.offsetMax = Vector2.zero;
            var outline = logo.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3f, -3f);

            var group = new GameObject("ButtonGroup", typeof(RectTransform), typeof(VerticalLayoutGroup));
            group.transform.SetParent(root.transform, false);
            var groupRT = group.GetComponent<RectTransform>();
            groupRT.anchorMin = new Vector2(0.02f, 0.02f);
            groupRT.anchorMax = new Vector2(0.02f, 0.92f);
            groupRT.sizeDelta = new Vector2(240f, 0f);
            groupRT.pivot = new Vector2(0f, 1f); // Top-left anchor

            var vlg = group.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var menuUI = root.AddComponent<MainMenuUI>();
            menuUI.btnNewJourney = CreateButton("Btn_NewJourney", group.transform, "New Journey", 44f);
            menuUI.btnContinue = CreateButton("Btn_Continue", group.transform, "Continue", 44f);
            menuUI.btnCollection = CreateButton("Btn_Collection", group.transform, "Collection", 44f);
            menuUI.btnOptions = CreateButton("Btn_Options", group.transform, "Options", 44f);
            menuUI.btnOpenPack = CreateButton("Btn_OpenPack", group.transform, "Open Pack", 44f);
            menuUI.btnQuickDuel = CreateButton("Btn_QuickDuel", group.transform, "Quick Duel", 44f);
            menuUI.btnTestMode = CreateButton("Btn_TestMode", group.transform, "Test Mode", 44f);

            // Wire parallax quadrant controllers
            menuUI.parallaxFire  = plxFire;
            menuUI.parallaxWind  = plxWind;
            menuUI.parallaxWater = plxWater;
            menuUI.parallaxEarth = plxEarth;

            // Style special buttons
            var packColors = menuUI.btnOpenPack.colors;
            packColors.normalColor = new Color(0.3f, 0.45f, 0.3f);
            packColors.highlightedColor = new Color(0.4f, 0.55f, 0.4f);
            menuUI.btnOpenPack.colors = packColors;

            var duelColors = menuUI.btnQuickDuel.colors;
            duelColors.normalColor = new Color(0.35f, 0.3f, 0.5f);
            duelColors.highlightedColor = new Color(0.45f, 0.4f, 0.6f);
            menuUI.btnQuickDuel.colors = duelColors;

            var testColors = menuUI.btnTestMode.colors;
            testColors.normalColor = new Color(0.6f, 0.3f, 0.3f);
            testColors.highlightedColor = new Color(0.7f, 0.4f, 0.4f);
            menuUI.btnTestMode.colors = testColors;

            menuUI.btnContinue.interactable = false;

            // Biome showcase — cycles quadrants to full-screen in order: Fire→Earth→Water→Wind
            var showcase = root.AddComponent<TitleScreenBiomeShowcase>();
            showcase.overlay = overlayCtrl;
            showcase.showcaseOrder = new ParallaxBackgroundController[]
            {
                plxFire,   // VolcanicWastes
                plxEarth,  // AncientForest
                plxWater,  // CoralDepths
                plxWind,   // SkyPeaks
            };

            return root;
        }

        // ───────────────────────────────────────
        //  Town Screen
        // ───────────────────────────────────────

        private static GameObject BuildTownScreen(Transform parent)
        {
            var root = CreatePanel("Screen_Town", parent);

            // Dark brown background
            var bg = CreateImage("BG_Town", root.transform, new Color(0.10f, 0.05f, 0.04f)); // #1A0E0A
            StretchFill(bg);

            // Building silhouettes — isolated for future art swap (Session 14)
            BuildTownVisuals(root.transform);

            // Nav buttons
            var nav = new GameObject("Buttons_Nav", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            nav.transform.SetParent(root.transform, false);
            var navRT = nav.GetComponent<RectTransform>();
            navRT.anchorMin = new Vector2(0.5f, 0f);
            navRT.anchorMax = new Vector2(0.5f, 0f);
            navRT.pivot = new Vector2(0.5f, 0f);
            navRT.anchoredPosition = new Vector2(0f, 80f);
            navRT.sizeDelta = new Vector2(800f, 56f);

            var hlg = nav.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 24f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var townUI = root.AddComponent<TownUI>();
            townUI.btnWorldMap = CreateButton("Btn_WorldMap", nav.transform, "World Map", 56f);
            townUI.btnCollection = CreateButton("Btn_Collection", nav.transform, "Collection", 56f);
            townUI.btnShop = CreateButton("Btn_Shop", nav.transform, "Shop", 56f);
            townUI.btnSettings = CreateButton("Btn_Settings", nav.transform, "Settings", 56f);

            return root;
        }

        /// <summary>
        /// Building silhouettes and town decor. Isolated in its own method so Session 14
        /// can swap this for real tilemap art with a single method replacement.
        /// </summary>
        private static void BuildTownVisuals(Transform parent)
        {
            // Overlay layer: ground bar + watermark
            var overlay = CreatePanel("BG_TownOverlay", parent);
            StretchFill(overlay);

            // Ground bar — bottom 30%
            var ground = CreateImage("GroundBar", overlay.transform, new Color(0.18f, 0.09f, 0.06f)); // #2D1810
            var groundRT = ground.GetComponent<RectTransform>();
            groundRT.anchorMin = Vector2.zero;
            groundRT.anchorMax = new Vector2(1f, 0.30f);
            groundRT.offsetMin = groundRT.offsetMax = Vector2.zero;

            // Town label watermark
            var label = CreateTMP("Label", overlay.transform, "\u2014 TOWN \u2014", 64f,
                new Color(0.83f, 0.66f, 0.26f, 0.2f), TextAlignmentOptions.Center);
            StretchFill(label);

            // Building silhouettes
            var decor = CreatePanel("TownDecor", parent);
            StretchFill(decor);

            // Building Left
            var buildingL = CreateImage("Building_Left", decor.transform, new Color(0.24f, 0.15f, 0.14f));
            var blRT = buildingL.GetComponent<RectTransform>();
            blRT.anchorMin = blRT.anchorMax = new Vector2(0.12f, 0.22f);
            blRT.pivot = new Vector2(0.5f, 0f);
            blRT.sizeDelta = new Vector2(200f, 300f);

            var windowL = CreateImage("Window", buildingL.transform, new Color(0.83f, 0.66f, 0.26f, 0.3f));
            var wlRT = windowL.GetComponent<RectTransform>();
            wlRT.anchorMin = wlRT.anchorMax = new Vector2(0.5f, 0.7f);
            wlRT.sizeDelta = new Vector2(40f, 40f);

            // Building Center (taller)
            var buildingC = CreateImage("Building_Center", decor.transform, new Color(0.31f, 0.20f, 0.18f));
            var bcRT = buildingC.GetComponent<RectTransform>();
            bcRT.anchorMin = bcRT.anchorMax = new Vector2(0.5f, 0.22f);
            bcRT.pivot = new Vector2(0.5f, 0f);
            bcRT.sizeDelta = new Vector2(280f, 350f);

            var wCL = CreateImage("Window_L", buildingC.transform, new Color(0.83f, 0.66f, 0.26f, 0.3f));
            var wclRT = wCL.GetComponent<RectTransform>();
            wclRT.anchorMin = wclRT.anchorMax = new Vector2(0.3f, 0.7f);
            wclRT.sizeDelta = new Vector2(40f, 40f);

            var wCR = CreateImage("Window_R", buildingC.transform, new Color(0.83f, 0.66f, 0.26f, 0.3f));
            var wcrRT = wCR.GetComponent<RectTransform>();
            wcrRT.anchorMin = wcrRT.anchorMax = new Vector2(0.7f, 0.7f);
            wcrRT.sizeDelta = new Vector2(40f, 40f);

            var door = CreateImage("Door", buildingC.transform, new Color(0.10f, 0.05f, 0.04f));
            var doorRT = door.GetComponent<RectTransform>();
            doorRT.anchorMin = doorRT.anchorMax = new Vector2(0.5f, 0f);
            doorRT.pivot = new Vector2(0.5f, 0f);
            doorRT.sizeDelta = new Vector2(60f, 80f);

            // Building Right
            var buildingR = CreateImage("Building_Right", decor.transform, new Color(0.24f, 0.15f, 0.14f));
            var brRT = buildingR.GetComponent<RectTransform>();
            brRT.anchorMin = brRT.anchorMax = new Vector2(0.85f, 0.22f);
            brRT.pivot = new Vector2(0.5f, 0f);
            brRT.sizeDelta = new Vector2(220f, 280f);

            var windowR = CreateImage("Window", buildingR.transform, new Color(0.83f, 0.66f, 0.26f, 0.3f));
            var wrRT = windowR.GetComponent<RectTransform>();
            wrRT.anchorMin = wrRT.anchorMax = new Vector2(0.5f, 0.7f);
            wrRT.sizeDelta = new Vector2(40f, 40f);
        }

        // ───────────────────────────────────────
        //  World Map Screen
        // ───────────────────────────────────────

        private static GameObject BuildWorldMap(Transform parent)
        {
            var root = CreatePanel("Screen_WorldMap", parent);

            // Background
            var bg = CreateImage("BG_Map", root.transform, ElementColors.Dark);
            StretchFill(bg);

            // Map decorations
            var mapDecor = CreatePanel("MapDecor", root.transform);
            StretchFill(mapDecor);

            // Grid lines (very dim)
            Color gridColor = new Color(1f, 1f, 1f, 0.05f);
            for (int i = 1; i <= 3; i++)
            {
                float t = i / 4f;
                var hLine = CreateImage($"Grid_H{i}", mapDecor.transform, gridColor);
                var hlRT = hLine.GetComponent<RectTransform>();
                hlRT.anchorMin = new Vector2(0f, t);
                hlRT.anchorMax = new Vector2(1f, t);
                hlRT.sizeDelta = new Vector2(0f, 1f);
                hLine.GetComponent<Image>().raycastTarget = false;

                var vLine = CreateImage($"Grid_V{i}", mapDecor.transform, gridColor);
                var vlRT = vLine.GetComponent<RectTransform>();
                vlRT.anchorMin = new Vector2(t, 0f);
                vlRT.anchorMax = new Vector2(t, 1f);
                vlRT.sizeDelta = new Vector2(1f, 0f);
                vLine.GetComponent<Image>().raycastTarget = false;
            }

            // Map title
            var mapTitle = CreateTMP("Title", mapDecor.transform, "THE KNOWN WORLD", 28f,
                new Color(0.83f, 0.66f, 0.26f, 0.4f), TextAlignmentOptions.Center, FontStyles.Italic);
            var mtRT = mapTitle.GetComponent<RectTransform>();
            mtRT.anchorMin = new Vector2(0.1f, 0.90f);
            mtRT.anchorMax = new Vector2(0.6f, 0.97f);
            mtRT.offsetMin = mtRT.offsetMax = Vector2.zero;

            // Zone markers
            var zoneMarkers = CreatePanel("ZoneMarkers", root.transform);
            StretchFill(zoneMarkers);

            var zones = ZoneData.GetAllZones();
            Vector2[] positions = {
                new Vector2(0.25f, 0.35f), // Ancient Forest
                new Vector2(0.20f, 0.70f), // Volcanic Wastes
                new Vector2(0.55f, 0.30f), // Coral Depths
                new Vector2(0.50f, 0.75f), // Sky Peaks
            };
            Color[] colors = {
                ElementColors.Earth,
                ElementColors.Fire,
                ElementColors.Water,
                ElementColors.Wind,
            };

            var markerButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                markerButtons[i] = CreateZoneMarker(zoneMarkers.transform, zones[i], positions[i], colors[i]);
            }

            // Info panel (right side, 360px)
            var infoPanel = CreateImage("InfoPanel", root.transform, new Color(0.05f, 0.05f, 0.10f, 0.9f));
            var ipRT = infoPanel.GetComponent<RectTransform>();
            ipRT.anchorMin = new Vector2(1f, 0f);
            ipRT.anchorMax = new Vector2(1f, 1f);
            ipRT.pivot = new Vector2(1f, 0.5f);
            ipRT.sizeDelta = new Vector2(360f, 0f);
            ipRT.anchoredPosition = Vector2.zero;

            var infoName = CreateTMP("InfoName", infoPanel.transform, "", 32f,
                ElementColors.Gold, TextAlignmentOptions.TopLeft);
            var inRT = infoName.GetComponent<RectTransform>();
            inRT.anchorMin = new Vector2(0f, 0.78f);
            inRT.anchorMax = new Vector2(1f, 0.92f);
            inRT.offsetMin = new Vector2(24f, 0f);
            inRT.offsetMax = new Vector2(-24f, -16f);

            var infoElement = CreateTMP("InfoElement", infoPanel.transform, "", 20f,
                Color.white, TextAlignmentOptions.TopLeft);
            var ieRT = infoElement.GetComponent<RectTransform>();
            ieRT.anchorMin = new Vector2(0f, 0.68f);
            ieRT.anchorMax = new Vector2(1f, 0.78f);
            ieRT.offsetMin = new Vector2(24f, 0f);
            ieRT.offsetMax = new Vector2(-24f, 0f);

            var infoDifficulty = CreateTMP("InfoDifficulty", infoPanel.transform, "", 20f,
                Color.white, TextAlignmentOptions.TopLeft);
            var idRT = infoDifficulty.GetComponent<RectTransform>();
            idRT.anchorMin = new Vector2(0f, 0.58f);
            idRT.anchorMax = new Vector2(1f, 0.68f);
            idRT.offsetMin = new Vector2(24f, 0f);
            idRT.offsetMax = new Vector2(-24f, 0f);

            var infoDescription = CreateTMP("InfoDescription", infoPanel.transform, "", 16f,
                new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.TopLeft);
            var idescRT = infoDescription.GetComponent<RectTransform>();
            idescRT.anchorMin = new Vector2(0f, 0.30f);
            idescRT.anchorMax = new Vector2(1f, 0.58f);
            idescRT.offsetMin = new Vector2(24f, 0f);
            idescRT.offsetMax = new Vector2(-24f, 0f);
            infoDescription.GetComponent<TextMeshProUGUI>().textWrappingMode = TMPro.TextWrappingModes.Normal;

            // Cards count
            var infoCards = CreateTMP("InfoCards", infoPanel.transform, "", 18f,
                new Color(0.8f, 0.8f, 0.7f), TextAlignmentOptions.TopLeft);
            var icRT = infoCards.GetComponent<RectTransform>();
            icRT.anchorMin = new Vector2(0f, 0.22f);
            icRT.anchorMax = new Vector2(0.5f, 0.30f);
            icRT.offsetMin = new Vector2(24f, 0f);
            icRT.offsetMax = new Vector2(0f, 0f);

            // Quests count
            var infoQuests = CreateTMP("InfoQuests", infoPanel.transform, "", 18f,
                new Color(0.8f, 0.8f, 0.7f), TextAlignmentOptions.TopLeft);
            var iqRT = infoQuests.GetComponent<RectTransform>();
            iqRT.anchorMin = new Vector2(0.5f, 0.22f);
            iqRT.anchorMax = new Vector2(1f, 0.30f);
            iqRT.offsetMin = new Vector2(0f, 0f);
            iqRT.offsetMax = new Vector2(-24f, 0f);

            // Enter Zone button (bottom of info panel)
            var btnEnterZone = CreateButton("Btn_EnterZone", infoPanel.transform, "Enter Zone", 48f);
            var ezRT = btnEnterZone.GetComponent<RectTransform>();
            ezRT.anchorMin = new Vector2(0.1f, 0.05f);
            ezRT.anchorMax = new Vector2(0.9f, 0.05f);
            ezRT.pivot = new Vector2(0.5f, 0f);
            ezRT.sizeDelta = new Vector2(0f, 48f);
            ezRT.anchoredPosition = new Vector2(0f, 16f);

            // Back button (bottom-left of screen)
            var btnBack = CreateButton("Btn_Back", root.transform, "Back", 48f);
            var bkRT = btnBack.GetComponent<RectTransform>();
            bkRT.anchorMin = bkRT.anchorMax = new Vector2(0f, 0f);
            bkRT.pivot = new Vector2(0f, 0f);
            bkRT.anchoredPosition = new Vector2(24f, 24f);
            bkRT.sizeDelta = new Vector2(160f, 48f);

            // Atlas progress text (top-right of screen, above info panel)
            var atlasProgress = CreateTMP("AtlasProgress", root.transform, "Atlas: 0/100", 20f,
                new Color(0.83f, 0.66f, 0.26f, 0.9f), TextAlignmentOptions.Center);
            var apRT = atlasProgress.GetComponent<RectTransform>();
            apRT.anchorMin = new Vector2(1f, 1f);
            apRT.anchorMax = new Vector2(1f, 1f);
            apRT.pivot = new Vector2(1f, 1f);
            apRT.anchoredPosition = new Vector2(-180f, -12f);
            apRT.sizeDelta = new Vector2(320f, 32f);

            // Zone labels (positioned near each zone marker)
            var zoneLabelsArr = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
            {
                var zoneLabelGO = CreateTMP($"ZoneLabel_{i}", zoneMarkers.transform, zones[i].Name, 14f,
                    Color.white, TextAlignmentOptions.Center);
                var zlRT = zoneLabelGO.GetComponent<RectTransform>();
                zlRT.anchorMin = zlRT.anchorMax = new Vector2(positions[i].x, positions[i].y - 0.08f);
                zlRT.sizeDelta = new Vector2(180f, 48f);
                var tmp = zoneLabelGO.GetComponent<TextMeshProUGUI>();
                tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
                tmp.richText = true;
                zoneLabelsArr[i] = tmp;
            }

            // Wire WorldMapUI
            var wmUI = root.AddComponent<WorldMapUI>();
            wmUI.btnAncientForest = markerButtons[0];
            wmUI.btnVolcanicWastes = markerButtons[1];
            wmUI.btnCoralDepths = markerButtons[2];
            wmUI.btnSkyPeaks = markerButtons[3];
            wmUI.infoName = infoName.GetComponent<TextMeshProUGUI>();
            wmUI.infoDescription = infoDescription.GetComponent<TextMeshProUGUI>();
            wmUI.infoDifficulty = infoDifficulty.GetComponent<TextMeshProUGUI>();
            wmUI.infoElement = infoElement.GetComponent<TextMeshProUGUI>();
            wmUI.infoCards = infoCards.GetComponent<TextMeshProUGUI>();
            wmUI.infoQuests = infoQuests.GetComponent<TextMeshProUGUI>();
            wmUI.infoPanel = infoPanel;
            wmUI.btnBack = btnBack;
            wmUI.btnEnterZone = btnEnterZone;
            wmUI.atlasProgressText = atlasProgress.GetComponent<TextMeshProUGUI>();
            wmUI.zoneLabels = zoneLabelsArr;

            return root;
        }

        /// <summary>
        /// Creates a zone marker: colored square button + label + element dot + optional lock.
        /// </summary>
        private static Button CreateZoneMarker(Transform parent, ZoneData zone, Vector2 position, Color color)
        {
            string safeName = zone.Name.Replace(" ", "");
            var root = new GameObject("Zone_" + safeName, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rootRT = root.GetComponent<RectTransform>();
            rootRT.anchorMin = rootRT.anchorMax = position;
            rootRT.sizeDelta = new Vector2(120f, 120f);

            // Marker square (button)
            Color markerColor = zone.IsUnlocked ? color : new Color(color.r, color.g, color.b, 0.4f);
            var marker = new GameObject("Marker", typeof(RectTransform), typeof(Image), typeof(Button));
            marker.transform.SetParent(root.transform, false);
            var markerRT = marker.GetComponent<RectTransform>();
            markerRT.anchorMin = markerRT.anchorMax = new Vector2(0.5f, 0.55f);
            markerRT.sizeDelta = new Vector2(80f, 80f);

            marker.GetComponent<Image>().color = Color.white;

            var btn = marker.GetComponent<Button>();
            var btnColors = btn.colors;
            btnColors.normalColor = markerColor;
            btnColors.highlightedColor = zone.IsUnlocked
                ? new Color(Mathf.Min(color.r * 1.3f, 1f), Mathf.Min(color.g * 1.3f, 1f), Mathf.Min(color.b * 1.3f, 1f))
                : markerColor;
            btnColors.pressedColor = zone.IsUnlocked
                ? new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f)
                : markerColor;
            btnColors.selectedColor = markerColor;
            btnColors.disabledColor = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f);
            btnColors.fadeDuration = 0.1f;
            btn.colors = btnColors;
            btn.interactable = zone.IsUnlocked;

            // Gold outline on unlocked markers
            if (zone.IsUnlocked)
            {
                var ol1 = marker.AddComponent<Outline>();
                ol1.effectColor = ElementColors.Gold;
                ol1.effectDistance = new Vector2(2f, -2f);
                var ol2 = marker.AddComponent<Outline>();
                ol2.effectColor = ElementColors.Gold;
                ol2.effectDistance = new Vector2(-2f, 2f);
            }

            // Label below marker
            Color labelColor = zone.IsUnlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            var label = CreateTMP("Label", root.transform, zone.Name, 16f,
                labelColor, TextAlignmentOptions.Center);
            var labelRT = label.GetComponent<RectTransform>();
            labelRT.anchorMin = labelRT.anchorMax = new Vector2(0.5f, 0.05f);
            labelRT.sizeDelta = new Vector2(150f, 24f);

            // Element dot above marker
            var dot = CreateImage("ElementDot", root.transform, color);
            var dotRT = dot.GetComponent<RectTransform>();
            dotRT.anchorMin = dotRT.anchorMax = new Vector2(0.5f, 0.95f);
            dotRT.sizeDelta = new Vector2(12f, 12f);
            dot.GetComponent<Image>().raycastTarget = false;

            // Lock text for locked zones
            if (!zone.IsUnlocked)
            {
                var lockIcon = CreateTMP("LockIcon", marker.transform, "LOCKED", 14f,
                    new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.Center, FontStyles.Bold);
                StretchFill(lockIcon);
            }

            return btn;
        }

        // ───────────────────────────────────────
        //  Exploration HUD (transparent overlay)
        // ───────────────────────────────────────

        private static GameObject BuildExplorationHUD(Transform parent)
        {
            var root = CreatePanel("Screen_Exploration", parent);
            // No background — transparent so world-space room shows through

            // Bottom bar
            var bar = CreateImage("BottomBar", root.transform, new Color(0.10f, 0.10f, 0.18f, 0.8f));
            var barRT = bar.GetComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0f, 0f);
            barRT.anchorMax = new Vector2(1f, 0f);
            barRT.pivot = new Vector2(0.5f, 0f);
            barRT.sizeDelta = new Vector2(0f, 56f);

            // Back button (left side of bottom bar)
            var btnBack = CreateButton("Btn_Back", bar.transform, "Leave Zone", 40f);
            var backRT = btnBack.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0f, 0.5f);
            backRT.anchorMax = new Vector2(0f, 0.5f);
            backRT.pivot = new Vector2(0f, 0.5f);
            backRT.anchoredPosition = new Vector2(16f, 0f);
            backRT.sizeDelta = new Vector2(180f, 40f);

            // Room label (center of bottom bar)
            var roomLabel = CreateTMP("RoomLabel", bar.transform, "Room 1 of 10", 18f,
                new Color(1f, 1f, 1f, 0.5f), TextAlignmentOptions.Center);
            var rlRT = roomLabel.GetComponent<RectTransform>();
            rlRT.anchorMin = new Vector2(0.3f, 0f);
            rlRT.anchorMax = new Vector2(0.7f, 1f);
            rlRT.offsetMin = rlRT.offsetMax = Vector2.zero;

            // Minimap panel (top-right corner)
            var minimapPanel = CreateImage("MinimapPanel", root.transform,
                new Color(0.05f, 0.05f, 0.10f, 0.6f));
            var mpRT = minimapPanel.GetComponent<RectTransform>();
            mpRT.anchorMin = new Vector2(1f, 1f);
            mpRT.anchorMax = new Vector2(1f, 1f);
            mpRT.pivot = new Vector2(1f, 1f);
            mpRT.anchoredPosition = new Vector2(-12f, -56f); // Below HUD top bar
            mpRT.sizeDelta = new Vector2(90f, 90f);

            // Minimap grid container
            var gridContainer = new GameObject("MinimapGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridContainer.transform.SetParent(minimapPanel.transform, false);
            var gcRT = gridContainer.GetComponent<RectTransform>();
            gcRT.anchorMin = Vector2.zero;
            gcRT.anchorMax = Vector2.one;
            gcRT.offsetMin = new Vector2(5f, 5f);
            gcRT.offsetMax = new Vector2(-5f, -5f);

            var glg = gridContainer.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(12f, 12f);
            glg.spacing = new Vector2(2f, 2f);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.MiddleCenter;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 5;

            // Create 25 minimap cells (5x5)
            Color emptyCellColor = new Color(0.1f, 0.1f, 0.15f, 0.3f);
            for (int i = 0; i < 25; i++)
            {
                var cell = new GameObject($"Cell_{i}", typeof(RectTransform), typeof(Image));
                cell.transform.SetParent(gridContainer.transform, false);
                cell.GetComponent<Image>().color = emptyCellColor;
                cell.GetComponent<Image>().raycastTarget = false;
            }

            // Zone card count label (below minimap)
            var zoneCardLabel = CreateTMP("ZoneCardCount", root.transform, "Zone\nCards: 0/25", 14f,
                new Color(0.8f, 0.8f, 0.7f, 0.8f), TextAlignmentOptions.Center);
            var zclRT = zoneCardLabel.GetComponent<RectTransform>();
            zclRT.anchorMin = new Vector2(1f, 1f);
            zclRT.anchorMax = new Vector2(1f, 1f);
            zclRT.pivot = new Vector2(1f, 1f);
            zclRT.anchoredPosition = new Vector2(-12f, -152f); // Below minimap (56 + 90 + 6)
            zclRT.sizeDelta = new Vector2(90f, 36f);

            // Quests button (right side of bottom bar)
            var btnQuests = CreateButton("Btn_Quests", bar.transform, "Quests", 32f);
            var qRT = btnQuests.GetComponent<RectTransform>();
            qRT.anchorMin = new Vector2(1f, 0.5f);
            qRT.anchorMax = new Vector2(1f, 0.5f);
            qRT.pivot = new Vector2(1f, 0.5f);
            qRT.anchoredPosition = new Vector2(-16f, 0f);
            qRT.sizeDelta = new Vector2(120f, 40f);

            // Quest Log panel (full-screen overlay, hidden by default)
            var questLogPanel = BuildQuestLog(root.transform);

            // Wire MinimapUI
            var minimapUI = minimapPanel.AddComponent<MinimapUI>();
            minimapUI.gridContainer = gcRT;

            // Wire ExplorationHUD
            var ehud = root.AddComponent<ExplorationHUD>();
            ehud.btnBack = btnBack;
            ehud.btnQuests = btnQuests;
            ehud.roomLabel = roomLabel.GetComponent<TextMeshProUGUI>();
            ehud.zoneCardCount = zoneCardLabel.GetComponent<TextMeshProUGUI>();
            ehud.minimap = minimapUI;
            ehud.questLog = questLogPanel.GetComponent<QuestLogUI>();

            return root;
        }

        // ───────────────────────────────────────
        //  Quest Log (overlay inside Exploration)
        // ───────────────────────────────────────

        private static GameObject BuildQuestLog(Transform parent)
        {
            // Full-screen dark overlay panel, hidden by default
            var panel = CreateImage("QuestLogPanel", parent, new Color(0.03f, 0.03f, 0.06f, 0.92f));
            StretchFill(panel);
            panel.SetActive(false);

            // Title
            var title = CreateTMP("Title", panel.transform, "QUEST LOG", 36f,
                ElementColors.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            var titleRT = title.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.1f, 0.88f);
            titleRT.anchorMax = new Vector2(0.9f, 0.96f);
            titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

            // Close button (top-right)
            var btnClose = CreateButton("Btn_Close", panel.transform, "X", 36f);
            var cRT = btnClose.GetComponent<RectTransform>();
            cRT.anchorMin = cRT.anchorMax = new Vector2(1f, 1f);
            cRT.pivot = new Vector2(1f, 1f);
            cRT.anchoredPosition = new Vector2(-16f, -16f);
            cRT.sizeDelta = new Vector2(48f, 48f);

            // Scrollable content area
            var scrollArea = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect));
            scrollArea.transform.SetParent(panel.transform, false);
            var saRT = scrollArea.GetComponent<RectTransform>();
            saRT.anchorMin = new Vector2(0.08f, 0.10f);
            saRT.anchorMax = new Vector2(0.92f, 0.86f);
            saRT.offsetMin = saRT.offsetMax = Vector2.zero;

            // Scroll viewport with mask
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollArea.transform, false);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.003f); // nearly invisible, needed for mask
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            var vpRT = viewport.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;

            // Content container with vertical layout
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.sizeDelta = new Vector2(0f, 0f);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Wire ScrollRect
            var scrollRect = scrollArea.GetComponent<ScrollRect>();
            scrollRect.viewport = vpRT;
            scrollRect.content = contentRT;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            // Atlas progress text (bottom of panel)
            var atlasText = CreateTMP("AtlasProgress", panel.transform, "Atlas Progress: 0/100 cards", 18f,
                new Color(0.83f, 0.66f, 0.26f, 0.9f), TextAlignmentOptions.Center);
            var atRT = atlasText.GetComponent<RectTransform>();
            atRT.anchorMin = new Vector2(0.1f, 0.02f);
            atRT.anchorMax = new Vector2(0.9f, 0.09f);
            atRT.offsetMin = atRT.offsetMax = Vector2.zero;

            // Wire QuestLogUI
            var questLogUI = panel.AddComponent<QuestLogUI>();
            questLogUI.panel = panel;
            questLogUI.contentContainer = contentRT;
            questLogUI.btnClose = btnClose;
            questLogUI.atlasProgress = atlasText.GetComponent<TextMeshProUGUI>();

            return panel;
        }

        // ───────────────────────────────────────
        //  Combat Screen
        // ───────────────────────────────────────

        private static GameObject BuildCombatScreen(Transform parent)
        {
            var root = CreatePanel("Screen_Combat", parent);

            // Background
            var bg = CreateImage("BG_Arena", root.transform, new Color(0.04f, 0.04f, 0.08f));
            StretchFill(bg);

            // ── Combat HUD (top bar — own HUD, persistent HUD hidden during combat) ──
            var hudBar = CreateImage("HUD_Combat", root.transform, new Color(0.10f, 0.10f, 0.18f, 0.9f));
            var hudRT = hudBar.GetComponent<RectTransform>();
            hudRT.anchorMin = new Vector2(0f, 1f);
            hudRT.anchorMax = new Vector2(1f, 1f);
            hudRT.pivot = new Vector2(0.5f, 1f);
            hudRT.sizeDelta = new Vector2(0f, 52f);

            var playerHP = CreateTMP("PlayerHP", hudBar.transform, "HP: 20", 22f, ElementColors.Earth, TextAlignmentOptions.Left, FontStyles.Bold);
            var phpRT = playerHP.GetComponent<RectTransform>();
            phpRT.anchorMin = new Vector2(0f, 0f); phpRT.anchorMax = new Vector2(0.22f, 1f);
            phpRT.offsetMin = new Vector2(20f, 0f); phpRT.offsetMax = Vector2.zero;

            var goldT = CreateTMP("GoldText", hudBar.transform, "Gold: 3", 20f, ElementColors.Gold, TextAlignmentOptions.Center);
            var gtRT = goldT.GetComponent<RectTransform>();
            gtRT.anchorMin = new Vector2(0.22f, 0f); gtRT.anchorMax = new Vector2(0.40f, 1f);
            gtRT.offsetMin = gtRT.offsetMax = Vector2.zero;

            var roundT = CreateTMP("RoundText", hudBar.transform, "Round 1", 26f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            var rtRT = roundT.GetComponent<RectTransform>();
            rtRT.anchorMin = new Vector2(0.40f, 0f); rtRT.anchorMax = new Vector2(0.60f, 1f);
            rtRT.offsetMin = rtRT.offsetMax = Vector2.zero;

            var oppHP = CreateTMP("OpponentHP", hudBar.transform, "HP: 20", 22f, ElementColors.Fire, TextAlignmentOptions.Right, FontStyles.Bold);
            var ohpRT = oppHP.GetComponent<RectTransform>();
            ohpRT.anchorMin = new Vector2(0.78f, 0f); ohpRT.anchorMax = new Vector2(1f, 1f);
            ohpRT.offsetMin = Vector2.zero; ohpRT.offsetMax = new Vector2(-20f, 0f);

            // ── Top zone: Shop (shop phase) / Opponent board (battle phase) ──
            // Both occupy the same screen region; CombatUI toggles visibility

            // Shop area (shown during shop phase)
            var shopArea = CreateImage("ShopArea", root.transform, new Color(0.06f, 0.06f, 0.12f, 0.7f));
            var saRT = shopArea.GetComponent<RectTransform>();
            saRT.anchorMin = new Vector2(0.02f, 0.52f);
            saRT.anchorMax = new Vector2(0.73f, 0.93f);
            saRT.offsetMin = saRT.offsetMax = Vector2.zero;

            // Shop cards row (3 cards, centered)
            var shopRow = new GameObject("ShopRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            shopRow.transform.SetParent(shopArea.transform, false);
            var srRT = shopRow.GetComponent<RectTransform>();
            srRT.anchorMin = new Vector2(0.05f, 0.20f);
            srRT.anchorMax = new Vector2(0.95f, 0.95f);
            srRT.offsetMin = srRT.offsetMax = Vector2.zero;
            var srHLG = shopRow.GetComponent<HorizontalLayoutGroup>();
            srHLG.spacing = 16f;
            srHLG.childAlignment = TextAnchor.MiddleCenter;
            srHLG.childControlWidth = true;
            srHLG.childControlHeight = true;
            srHLG.childForceExpandWidth = true;
            srHLG.childForceExpandHeight = true;

            var shopButtons = new Button[3];
            var shopTexts = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                var shopSlot = new GameObject($"ShopSlot_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                shopSlot.transform.SetParent(shopRow.transform, false);
                shopSlot.GetComponent<Image>().color = ElementColors.Dark;
                shopButtons[i] = shopSlot.GetComponent<Button>();
                var sbc = shopButtons[i].colors;
                sbc.normalColor = ElementColors.Dark;
                sbc.highlightedColor = new Color(0.2f, 0.2f, 0.3f);
                shopButtons[i].colors = sbc;

                var shopOutline = shopSlot.AddComponent<Outline>();
                shopOutline.effectColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                shopOutline.effectDistance = new Vector2(2f, -2f);

                var st = CreateTMP("Text", shopSlot.transform, "", 14f, Color.white, TextAlignmentOptions.Center);
                StretchFill(st);
                st.GetComponent<TextMeshProUGUI>().raycastTarget = false;
                shopTexts[i] = st.GetComponent<TextMeshProUGUI>();
            }

            // ── Right sidebar (full height, holds Reroll top + Battle bottom) ──
            var sidebar = new GameObject("RightSidebar", typeof(RectTransform), typeof(Image));
            sidebar.transform.SetParent(root.transform, false);
            var sbRT = sidebar.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(0.74f, 0.04f);
            sbRT.anchorMax = new Vector2(0.98f, 0.93f);
            sbRT.offsetMin = sbRT.offsetMax = Vector2.zero;
            sidebar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.5f);

            // Reroll button — top of sidebar
            var btnReroll = CreateButton("Btn_Reroll", sidebar.transform, "Reroll (1g)", 40f);
            var rerollRT = btnReroll.GetComponent<RectTransform>();
            rerollRT.anchorMin = new Vector2(0.05f, 0.88f);
            rerollRT.anchorMax = new Vector2(0.95f, 0.98f);
            rerollRT.offsetMin = rerollRT.offsetMax = Vector2.zero;
            var rerollText = btnReroll.GetComponentInChildren<TextMeshProUGUI>();

            // Pool viewer button — middle of sidebar
            var btnPool = CreateButton("Btn_Pool", sidebar.transform, "My Pool", 36f);
            var bpRT = btnPool.GetComponent<RectTransform>();
            bpRT.anchorMin = new Vector2(0.05f, 0.78f);
            bpRT.anchorMax = new Vector2(0.95f, 0.86f);
            bpRT.offsetMin = bpRT.offsetMax = Vector2.zero;
            btnPool.GetComponentInChildren<TextMeshProUGUI>().fontSize = 18f;

            // Battle button — bottom of sidebar
            var btnBattle = CreateButton("Btn_Battle", sidebar.transform, "BATTLE!", 56f);
            var bbRT = btnBattle.GetComponent<RectTransform>();
            bbRT.anchorMin = new Vector2(0.05f, 0.02f);
            bbRT.anchorMax = new Vector2(0.95f, 0.12f);
            bbRT.offsetMin = bbRT.offsetMax = Vector2.zero;
            btnBattle.GetComponentInChildren<TextMeshProUGUI>().fontSize = 32f;

            // Shop label
            var shopLabel = CreateTMP("ShopLabel", shopArea.transform, "DRAFT", 16f,
                new Color(1f, 1f, 1f, 0.4f), TextAlignmentOptions.Left);
            var slRT = shopLabel.GetComponent<RectTransform>();
            slRT.anchorMin = new Vector2(0.10f, 0.02f);
            slRT.anchorMax = new Vector2(0.50f, 0.18f);
            slRT.offsetMin = slRT.offsetMax = Vector2.zero;

            // Opponent board (shown during battle phase, starts hidden)
            var oppBoard = new GameObject("OpponentBoard", typeof(RectTransform));
            oppBoard.transform.SetParent(root.transform, false);
            var obRT = oppBoard.GetComponent<RectTransform>();
            obRT.anchorMin = new Vector2(0.02f, 0.52f);
            obRT.anchorMax = new Vector2(0.73f, 0.93f);
            obRT.offsetMin = obRT.offsetMax = Vector2.zero;

            // Opponent back row (2 slots) — TOP of section (farthest from player)
            var oppBack = new GameObject("OppBackRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            oppBack.transform.SetParent(oppBoard.transform, false);
            var obkRT = oppBack.GetComponent<RectTransform>();
            obkRT.anchorMin = new Vector2(0.20f, 0.55f);
            obkRT.anchorMax = new Vector2(0.80f, 0.95f);
            obkRT.offsetMin = obkRT.offsetMax = Vector2.zero;
            var obkHLG = oppBack.GetComponent<HorizontalLayoutGroup>();
            obkHLG.spacing = 16f;
            obkHLG.childAlignment = TextAnchor.MiddleCenter;
            obkHLG.childControlWidth = true; obkHLG.childControlHeight = true;
            obkHLG.childForceExpandWidth = true; obkHLG.childForceExpandHeight = true;

            // Opponent front row (3 slots) — BOTTOM of section (facing player's front row)
            var oppFront = new GameObject("OppFrontRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            oppFront.transform.SetParent(oppBoard.transform, false);
            var ofRT = oppFront.GetComponent<RectTransform>();
            ofRT.anchorMin = new Vector2(0.10f, 0.05f);
            ofRT.anchorMax = new Vector2(0.90f, 0.50f);
            ofRT.offsetMin = ofRT.offsetMax = Vector2.zero;
            var ofHLG = oppFront.GetComponent<HorizontalLayoutGroup>();
            ofHLG.spacing = 16f;
            ofHLG.childAlignment = TextAnchor.MiddleCenter;
            ofHLG.childControlWidth = true; ofHLG.childControlHeight = true;
            ofHLG.childForceExpandWidth = true; ofHLG.childForceExpandHeight = true;

            var opponentSlotTexts = new TextMeshProUGUI[5];
            for (int i = 0; i < 5; i++)
            {
                Color oppColor = new Color(0.18f, 0.05f, 0.05f, 0.85f);
                Transform oppParent = i < 3 ? oppFront.transform : oppBack.transform;
                var slot = CreateImage($"OppSlot_{i}", oppParent, oppColor);
                var outline = slot.AddComponent<Outline>();
                outline.effectColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                outline.effectDistance = new Vector2(2f, -2f);
                var st = CreateTMP("Text", slot.transform, "", 13f, Color.white, TextAlignmentOptions.Center);
                StretchFill(st);
                opponentSlotTexts[i] = st.GetComponent<TextMeshProUGUI>();
            }
            oppBoard.SetActive(false); // Hidden during shop phase

            // ── VS divider ──
            var vs = CreateTMP("VS", root.transform, "- VS -", 20f, new Color(1f, 1f, 1f, 0.3f), TextAlignmentOptions.Center, FontStyles.Bold);
            var vsRT = vs.GetComponent<RectTransform>();
            vsRT.anchorMin = new Vector2(0.30f, 0.48f);
            vsRT.anchorMax = new Vector2(0.70f, 0.54f);
            vsRT.offsetMin = vsRT.offsetMax = Vector2.zero;

            // ── Player board (bottom zone, front/back rows separate) ──
            var pBoardContainer = new GameObject("PlayerBoardArea", typeof(RectTransform));
            pBoardContainer.transform.SetParent(root.transform, false);
            var pbcRT = pBoardContainer.GetComponent<RectTransform>();
            pbcRT.anchorMin = new Vector2(0.02f, 0.04f);
            pbcRT.anchorMax = new Vector2(0.73f, 0.48f);
            pbcRT.offsetMin = pbcRT.offsetMax = Vector2.zero;

            // Player front row (3 slots)
            var pFront = new GameObject("PlayerFrontRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            pFront.transform.SetParent(pBoardContainer.transform, false);
            var pfRT = pFront.GetComponent<RectTransform>();
            pfRT.anchorMin = new Vector2(0.02f, 0.48f);
            pfRT.anchorMax = new Vector2(0.98f, 0.95f);
            pfRT.offsetMin = pfRT.offsetMax = Vector2.zero;
            var pfHLG = pFront.GetComponent<HorizontalLayoutGroup>();
            pfHLG.spacing = 14f;
            pfHLG.childAlignment = TextAnchor.MiddleCenter;
            pfHLG.childControlWidth = true; pfHLG.childControlHeight = true;
            pfHLG.childForceExpandWidth = true; pfHLG.childForceExpandHeight = true;

            // Player back row (2 slots)
            var pBack = new GameObject("PlayerBackRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            pBack.transform.SetParent(pBoardContainer.transform, false);
            var pbkRT = pBack.GetComponent<RectTransform>();
            pbkRT.anchorMin = new Vector2(0.10f, 0.02f);
            pbkRT.anchorMax = new Vector2(0.90f, 0.46f);
            pbkRT.offsetMin = pbkRT.offsetMax = Vector2.zero;
            var pbkHLG = pBack.GetComponent<HorizontalLayoutGroup>();
            pbkHLG.spacing = 14f;
            pbkHLG.childAlignment = TextAnchor.MiddleCenter;
            pbkHLG.childControlWidth = true; pbkHLG.childControlHeight = true;
            pbkHLG.childForceExpandWidth = true; pbkHLG.childForceExpandHeight = true;

            var playerSlots = new Button[5];
            var playerSlotTexts = new TextMeshProUGUI[5];
            for (int i = 0; i < 5; i++)
            {
                Color slotColor = i < 3
                    ? new Color(0.08f, 0.18f, 0.08f, 0.85f)
                    : new Color(0.06f, 0.12f, 0.18f, 0.85f);
                Transform slotParent = i < 3 ? pFront.transform : pBack.transform;

                var slot = new GameObject($"PlayerSlot_{i}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
                slot.transform.SetParent(slotParent, false);
                slot.GetComponent<Image>().color = slotColor;
                var slotOutline = slot.GetComponent<Outline>();
                slotOutline.effectColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                slotOutline.effectDistance = new Vector2(2f, -2f);
                var btn = slot.GetComponent<Button>();
                var bc = btn.colors;
                bc.normalColor = slotColor;
                bc.highlightedColor = new Color(slotColor.r + 0.12f, slotColor.g + 0.12f, slotColor.b + 0.12f);
                btn.colors = bc;
                playerSlots[i] = btn;

                string label = i < 3 ? "Front" : "Back";
                var st = CreateTMP("Text", slot.transform, label, 14f, Color.white, TextAlignmentOptions.Center);
                StretchFill(st);
                st.GetComponent<TextMeshProUGUI>().raycastTarget = false;
                playerSlotTexts[i] = st.GetComponent<TextMeshProUGUI>();
            }

            // ── Result panel (center overlay, hidden) ──
            var resultPanel = CreateImage("ResultPanel", root.transform, new Color(0f, 0f, 0f, 0.85f));
            StretchFill(resultPanel);

            var resultText = CreateTMP("ResultText", resultPanel.transform, "VICTORY!", 64f, ElementColors.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            var rrRT = resultText.GetComponent<RectTransform>();
            rrRT.anchorMin = new Vector2(0.1f, 0.5f);
            rrRT.anchorMax = new Vector2(0.9f, 0.75f);
            rrRT.offsetMin = rrRT.offsetMax = Vector2.zero;

            var btnContinue = CreateButton("Btn_Continue", resultPanel.transform, "Continue", 56f);
            var bcnRT = btnContinue.GetComponent<RectTransform>();
            bcnRT.anchorMin = new Vector2(0.35f, 0.30f);
            bcnRT.anchorMax = new Vector2(0.65f, 0.30f);
            bcnRT.pivot = new Vector2(0.5f, 0.5f);
            bcnRT.sizeDelta = new Vector2(0f, 56f);

            // ── Wire CombatUI ──
            var cui = root.AddComponent<CombatUI>();
            cui.playerHPText = playerHP.GetComponent<TextMeshProUGUI>();
            cui.opponentHPText = oppHP.GetComponent<TextMeshProUGUI>();
            cui.roundText = roundT.GetComponent<TextMeshProUGUI>();
            cui.goldText = goldT.GetComponent<TextMeshProUGUI>();
            cui.playerSlots = playerSlots;
            cui.playerSlotTexts = playerSlotTexts;
            cui.opponentSlotTexts = opponentSlotTexts;
            cui.shopArea = shopArea;
            cui.opponentBoard = oppBoard;
            cui.shopButtons = shopButtons;
            cui.shopTexts = shopTexts;
            cui.btnBattle = btnBattle;
            cui.btnReroll = btnReroll;
            cui.btnPool = btnPool;
            cui.rerollCostText = rerollText;
            cui.resultPanel = resultPanel;
            cui.resultText = resultText.GetComponent<TextMeshProUGUI>();
            cui.btnContinue = btnContinue;

            return root;
        }

        // ───────────────────────────────────────
        //  Pack Opening Screen
        // ───────────────────────────────────────

        private static GameObject BuildPackOpeningScreen(Transform parent)
        {
            var root = CreatePanel("Screen_PackOpening", parent);

            var bg = CreateImage("BG_Celebration", root.transform, new Color(0.06f, 0.04f, 0.12f));
            StretchFill(bg);

            var banner = CreateTMP("Banner", root.transform, "BOOSTER PACK", 48f,
                ElementColors.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            var bannerRT = banner.GetComponent<RectTransform>();
            bannerRT.anchorMin = new Vector2(0.1f, 0.80f);
            bannerRT.anchorMax = new Vector2(0.9f, 0.95f);
            bannerRT.offsetMin = bannerRT.offsetMax = Vector2.zero;

            // Card row
            var cardRow = new GameObject("CardRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cardRow.transform.SetParent(root.transform, false);
            var crRT = cardRow.GetComponent<RectTransform>();
            crRT.anchorMin = new Vector2(0.1f, 0.25f);
            crRT.anchorMax = new Vector2(0.9f, 0.78f);
            crRT.offsetMin = crRT.offsetMax = Vector2.zero;
            var crHLG = cardRow.GetComponent<HorizontalLayoutGroup>();
            crHLG.spacing = 24f;
            crHLG.childAlignment = TextAnchor.MiddleCenter;
            crHLG.childControlWidth = true;
            crHLG.childControlHeight = true;
            crHLG.childForceExpandWidth = true;
            crHLG.childForceExpandHeight = true;

            var cardImages = new Image[5];
            var cardTexts = new TextMeshProUGUI[5];
            var cardBacks = new Image[5];
            var newBadges = new TextMeshProUGUI[5];

            for (int i = 0; i < 5; i++)
            {
                var card = CreateImage($"Card_{i}", cardRow.transform, new Color(0.15f, 0.15f, 0.25f));
                cardImages[i] = card.GetComponent<Image>();

                var text = CreateTMP("CardText", card.transform, "", 14f, Color.white, TextAlignmentOptions.Center);
                StretchFill(text);
                cardTexts[i] = text.GetComponent<TextMeshProUGUI>();

                // Card back overlay
                var back = CreateImage("CardBack", card.transform, new Color(0.1f, 0.08f, 0.2f));
                StretchFill(back);
                cardBacks[i] = back.GetComponent<Image>();

                // NEW badge
                var badge = CreateTMP("NewBadge", card.transform, "NEW", 12f,
                    new Color(1f, 0.9f, 0.2f), TextAlignmentOptions.Center, FontStyles.Bold);
                var badgeRT = badge.GetComponent<RectTransform>();
                badgeRT.anchorMin = new Vector2(0.5f, 0.85f);
                badgeRT.anchorMax = new Vector2(0.5f, 0.95f);
                badgeRT.sizeDelta = new Vector2(60f, 20f);
                badge.SetActive(false);
                newBadges[i] = badge.GetComponent<TextMeshProUGUI>();
            }

            // Continue button
            var btnContinue = CreateButton("Btn_Continue", root.transform, "Continue", 56f);
            var bcRT2 = btnContinue.GetComponent<RectTransform>();
            bcRT2.anchorMin = new Vector2(0.35f, 0.05f);
            bcRT2.anchorMax = new Vector2(0.65f, 0.05f);
            bcRT2.pivot = new Vector2(0.5f, 0f);
            bcRT2.sizeDelta = new Vector2(0f, 56f);

            // Wire PackOpeningUI
            var poUI = root.AddComponent<PackOpeningUI>();
            poUI.cardImages = cardImages;
            poUI.cardTexts = cardTexts;
            poUI.cardBacks = cardBacks;
            poUI.newBadges = newBadges;
            poUI.bannerText = banner.GetComponent<TextMeshProUGUI>();
            poUI.btnContinue = btnContinue;

            return root;
        }

        // ───────────────────────────────────────
        //  Intro / Story Screen
        // ───────────────────────────────────────

        private static GameObject BuildIntroScreen(Transform parent)
        {
            var root = CreatePanel("Screen_Intro", parent);

            // Full-screen dark background
            var bg = CreateImage("BG", root.transform, new Color(0.02f, 0.02f, 0.05f, 1f));
            StretchFill(bg);

            // Scene image (placeholder — large center area with mood color)
            var sceneImg = CreateImage("SceneImage", root.transform, Color.black);
            var siRT = sceneImg.GetComponent<RectTransform>();
            siRT.anchorMin = new Vector2(0.10f, 0.35f);
            siRT.anchorMax = new Vector2(0.90f, 0.90f);
            siRT.offsetMin = siRT.offsetMax = Vector2.zero;

            // Image description (placeholder text describing what art goes here)
            var descText = CreateTMP("ImageDesc", root.transform, "", 12f,
                new Color(0.5f, 0.5f, 0.5f, 0.6f), TextAlignmentOptions.Center);
            var descRT = descText.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0.15f, 0.75f);
            descRT.anchorMax = new Vector2(0.85f, 0.88f);
            descRT.offsetMin = descRT.offsetMax = Vector2.zero;
            descText.GetComponent<TextMeshProUGUI>().textWrappingMode = TMPro.TextWrappingModes.Normal;
            descText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;

            // Scene narrative text (bottom area)
            var sceneText = CreateTMP("SceneText", root.transform, "", 20f,
                new Color(0.9f, 0.85f, 0.75f, 1f), TextAlignmentOptions.Center);
            var stRT = sceneText.GetComponent<RectTransform>();
            stRT.anchorMin = new Vector2(0.10f, 0.05f);
            stRT.anchorMax = new Vector2(0.90f, 0.33f);
            stRT.offsetMin = stRT.offsetMax = Vector2.zero;
            sceneText.GetComponent<TextMeshProUGUI>().textWrappingMode = TMPro.TextWrappingModes.Normal;

            // Continue button (bottom-right)
            var btnContinue = CreateButton("Btn_Continue", root.transform, "Continue >>", 40f);
            var bcRT = btnContinue.GetComponent<RectTransform>();
            bcRT.anchorMin = new Vector2(0.70f, 0.01f);
            bcRT.anchorMax = new Vector2(0.95f, 0.06f);
            bcRT.offsetMin = bcRT.offsetMax = Vector2.zero;
            btnContinue.GetComponentInChildren<TextMeshProUGUI>().fontSize = 16f;

            // Skip button (bottom-left)
            var btnSkip = CreateButton("Btn_Skip", root.transform, "[Skip]", 40f);
            var bsRT = btnSkip.GetComponent<RectTransform>();
            bsRT.anchorMin = new Vector2(0.05f, 0.01f);
            bsRT.anchorMax = new Vector2(0.25f, 0.06f);
            bsRT.offsetMin = bsRT.offsetMax = Vector2.zero;
            btnSkip.GetComponentInChildren<TextMeshProUGUI>().fontSize = 14f;
            btnSkip.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.6f, 0.6f, 0.6f);

            // ── Tutorial Choice Panel (hidden initially) ──
            var tutChoice = new GameObject("TutorialChoice", typeof(RectTransform), typeof(Image));
            tutChoice.transform.SetParent(root.transform, false);
            var tcRT = tutChoice.GetComponent<RectTransform>();
            tcRT.anchorMin = new Vector2(0.25f, 0.25f);
            tcRT.anchorMax = new Vector2(0.75f, 0.75f);
            tcRT.offsetMin = tcRT.offsetMax = Vector2.zero;
            tutChoice.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.12f, 0.95f);
            var tcOutline = tutChoice.AddComponent<Outline>();
            tcOutline.effectColor = ElementColors.Gold;
            tcOutline.effectDistance = new Vector2(2f, -2f);

            var tcTitle = CreateTMP("Title", tutChoice.transform, "The Atlas stirs in your hands...\n\nWould you like the Scholar to\nexplain the ways of card combat?", 20f,
                Color.white, TextAlignmentOptions.Center);
            var tcTitleRT = tcTitle.GetComponent<RectTransform>();
            tcTitleRT.anchorMin = new Vector2(0.05f, 0.45f);
            tcTitleRT.anchorMax = new Vector2(0.95f, 0.95f);
            tcTitleRT.offsetMin = tcTitleRT.offsetMax = Vector2.zero;
            tcTitle.GetComponent<TextMeshProUGUI>().textWrappingMode = TMPro.TextWrappingModes.Normal;

            var btnYes = CreateButton("Btn_TutYes", tutChoice.transform, "Yes, teach me", 40f);
            var byRT = btnYes.GetComponent<RectTransform>();
            byRT.anchorMin = new Vector2(0.10f, 0.10f);
            byRT.anchorMax = new Vector2(0.48f, 0.30f);
            byRT.offsetMin = byRT.offsetMax = Vector2.zero;
            btnYes.GetComponentInChildren<TextMeshProUGUI>().fontSize = 18f;

            var btnNo = CreateButton("Btn_TutNo", tutChoice.transform, "No, I know the way", 40f);
            var bnRT = btnNo.GetComponent<RectTransform>();
            bnRT.anchorMin = new Vector2(0.52f, 0.10f);
            bnRT.anchorMax = new Vector2(0.90f, 0.30f);
            bnRT.offsetMin = bnRT.offsetMax = Vector2.zero;
            btnNo.GetComponentInChildren<TextMeshProUGUI>().fontSize = 18f;

            // ── Tutorial Panel (hidden initially) ──
            var tutPanel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(Image));
            tutPanel.transform.SetParent(root.transform, false);
            var tpRT = tutPanel.GetComponent<RectTransform>();
            tpRT.anchorMin = new Vector2(0.10f, 0.10f);
            tpRT.anchorMax = new Vector2(0.90f, 0.90f);
            tpRT.offsetMin = tpRT.offsetMax = Vector2.zero;
            tutPanel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.10f, 0.95f);
            var tpOutline = tutPanel.AddComponent<Outline>();
            tpOutline.effectColor = ElementColors.Gold;
            tpOutline.effectDistance = new Vector2(2f, -2f);

            // Scholar portrait placeholder (left side)
            var portrait = CreateImage("ScholarPortrait", tutPanel.transform, new Color(0.15f, 0.12f, 0.20f));
            var ppRT = portrait.GetComponent<RectTransform>();
            ppRT.anchorMin = new Vector2(0.03f, 0.50f);
            ppRT.anchorMax = new Vector2(0.18f, 0.90f);
            ppRT.offsetMin = ppRT.offsetMax = Vector2.zero;

            // Scholar name
            var scholarNameTMP = CreateTMP("ScholarName", tutPanel.transform, "Atlas Scholar", 22f,
                ElementColors.Gold, TextAlignmentOptions.Left, FontStyles.Bold);
            var snRT = scholarNameTMP.GetComponent<RectTransform>();
            snRT.anchorMin = new Vector2(0.20f, 0.82f);
            snRT.anchorMax = new Vector2(0.80f, 0.92f);
            snRT.offsetMin = snRT.offsetMax = Vector2.zero;

            // Scholar dialogue text
            var scholarText = CreateTMP("ScholarText", tutPanel.transform, "", 18f,
                new Color(0.9f, 0.85f, 0.75f), TextAlignmentOptions.TopLeft);
            var sdRT = scholarText.GetComponent<RectTransform>();
            sdRT.anchorMin = new Vector2(0.05f, 0.18f);
            sdRT.anchorMax = new Vector2(0.95f, 0.80f);
            sdRT.offsetMin = sdRT.offsetMax = Vector2.zero;
            scholarText.GetComponent<TextMeshProUGUI>().textWrappingMode = TMPro.TextWrappingModes.Normal;

            // Next button
            var btnNext = CreateButton("Btn_TutNext", tutPanel.transform, "Next >>", 40f);
            var btNRT = btnNext.GetComponent<RectTransform>();
            btNRT.anchorMin = new Vector2(0.65f, 0.03f);
            btNRT.anchorMax = new Vector2(0.95f, 0.13f);
            btNRT.offsetMin = btNRT.offsetMax = Vector2.zero;
            btnNext.GetComponentInChildren<TextMeshProUGUI>().fontSize = 18f;

            // ── Wire IntroUI ──
            var introUI = root.AddComponent<IntroUI>();
            introUI.sceneImage = sceneImg.GetComponent<Image>();
            introUI.sceneText = sceneText.GetComponent<TextMeshProUGUI>();
            introUI.imageDescText = descText.GetComponent<TextMeshProUGUI>();
            introUI.btnContinue = btnContinue;
            introUI.btnSkip = btnSkip;
            introUI.tutorialChoicePanel = tutChoice;
            introUI.btnTutorialYes = btnYes;
            introUI.btnTutorialNo = btnNo;
            introUI.tutorialPanel = tutPanel;
            introUI.scholarPortrait = portrait.GetComponent<Image>();
            introUI.scholarName = scholarNameTMP.GetComponent<TextMeshProUGUI>();
            introUI.scholarText = scholarText.GetComponent<TextMeshProUGUI>();
            introUI.btnTutorialNext = btnNext;

            return root;
        }

        // ───────────────────────────────────────
        //  Collection Screen
        // ───────────────────────────────────────

        private static GameObject BuildCollectionScreen(Transform parent)
        {
            var root = CreatePanel("Screen_Collection", parent);

            var bg2 = CreateImage("BG", root.transform, ElementColors.DarkBg);
            StretchFill(bg2);

            // Filter bar (top)
            var filterBar = new GameObject("FilterBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            filterBar.transform.SetParent(root.transform, false);
            var fbRT = filterBar.GetComponent<RectTransform>();
            fbRT.anchorMin = new Vector2(0.05f, 0.90f);
            fbRT.anchorMax = new Vector2(0.70f, 0.98f);
            fbRT.offsetMin = fbRT.offsetMax = Vector2.zero;
            var fbHLG = filterBar.GetComponent<HorizontalLayoutGroup>();
            fbHLG.spacing = 12f;
            fbHLG.childAlignment = TextAnchor.MiddleCenter;
            fbHLG.childControlWidth = true;
            fbHLG.childControlHeight = true;
            fbHLG.childForceExpandWidth = true;
            fbHLG.childForceExpandHeight = true;

            var btnFire = CreateButton("Btn_Fire", filterBar.transform, "Fire", 40f);
            var btnWater = CreateButton("Btn_Water", filterBar.transform, "Water", 40f);
            var btnEarth = CreateButton("Btn_Earth", filterBar.transform, "Earth", 40f);
            var btnWind = CreateButton("Btn_Wind", filterBar.transform, "Wind", 40f);

            var activeCount = CreateTMP("ActiveCount", root.transform, "Active: 0 / 0", 18f,
                Color.white, TextAlignmentOptions.Right);
            var acRT = activeCount.GetComponent<RectTransform>();
            acRT.anchorMin = new Vector2(0.70f, 0.90f);
            acRT.anchorMax = new Vector2(0.95f, 0.98f);
            acRT.offsetMin = acRT.offsetMax = Vector2.zero;

            // Card grid (scroll rect)
            var scrollArea = new GameObject("CardGrid", typeof(RectTransform), typeof(ScrollRect));
            scrollArea.transform.SetParent(root.transform, false);
            var saRT = scrollArea.GetComponent<RectTransform>();
            saRT.anchorMin = new Vector2(0.05f, 0.10f);
            saRT.anchorMax = new Vector2(0.70f, 0.88f);
            saRT.offsetMin = saRT.offsetMax = Vector2.zero;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollArea.transform, false);
            StretchFill(viewport);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var gridContainer = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            gridContainer.transform.SetParent(viewport.transform, false);
            var gcRT2 = gridContainer.GetComponent<RectTransform>();
            gcRT2.anchorMin = new Vector2(0f, 1f);
            gcRT2.anchorMax = new Vector2(1f, 1f);
            gcRT2.pivot = new Vector2(0.5f, 1f);
            gcRT2.sizeDelta = new Vector2(0f, 0f);

            var glg2 = gridContainer.GetComponent<GridLayoutGroup>();
            glg2.cellSize = new Vector2(100f, 140f);
            glg2.spacing = new Vector2(8f, 8f);
            glg2.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg2.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg2.constraintCount = 5;

            var csf = gridContainer.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr2 = scrollArea.GetComponent<ScrollRect>();
            sr2.viewport = viewport.GetComponent<RectTransform>();
            sr2.content = gcRT2;
            sr2.horizontal = false;
            sr2.vertical = true;

            // Detail panel (right side)
            var detailPanel = CreateImage("DetailPanel", root.transform, new Color(0.08f, 0.08f, 0.14f, 0.95f));
            var dpRT = detailPanel.GetComponent<RectTransform>();
            dpRT.anchorMin = new Vector2(0.72f, 0.10f);
            dpRT.anchorMax = new Vector2(0.98f, 0.88f);
            dpRT.offsetMin = dpRT.offsetMax = Vector2.zero;

            var detailImage = CreateImage("DetailImage", detailPanel.transform, Color.gray);
            var diRT = detailImage.GetComponent<RectTransform>();
            diRT.anchorMin = new Vector2(0.5f, 0.65f);
            diRT.anchorMax = new Vector2(0.5f, 0.65f);
            diRT.sizeDelta = new Vector2(100f, 140f);

            var detailName = CreateTMP("DetailName", detailPanel.transform, "", 24f, ElementColors.Gold, TextAlignmentOptions.Center);
            var dnRT = detailName.GetComponent<RectTransform>();
            dnRT.anchorMin = new Vector2(0.05f, 0.48f);
            dnRT.anchorMax = new Vector2(0.95f, 0.58f);
            dnRT.offsetMin = dnRT.offsetMax = Vector2.zero;

            var detailStats = CreateTMP("DetailStats", detailPanel.transform, "", 16f, Color.white, TextAlignmentOptions.Center);
            var dsRT = detailStats.GetComponent<RectTransform>();
            dsRT.anchorMin = new Vector2(0.05f, 0.32f);
            dsRT.anchorMax = new Vector2(0.95f, 0.48f);
            dsRT.offsetMin = dsRT.offsetMax = Vector2.zero;

            var detailAbility = CreateTMP("DetailAbility", detailPanel.transform, "", 14f,
                new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.Center, FontStyles.Italic);
            var daRT = detailAbility.GetComponent<RectTransform>();
            daRT.anchorMin = new Vector2(0.05f, 0.15f);
            daRT.anchorMax = new Vector2(0.95f, 0.32f);
            daRT.offsetMin = daRT.offsetMax = Vector2.zero;

            // Back button
            var btnBack = CreateButton("Btn_Back", root.transform, "Back", 48f);
            var bbRT = btnBack.GetComponent<RectTransform>();
            bbRT.anchorMin = bbRT.anchorMax = new Vector2(0f, 0f);
            bbRT.pivot = new Vector2(0f, 0f);
            bbRT.anchoredPosition = new Vector2(24f, 24f);
            bbRT.sizeDelta = new Vector2(160f, 48f);

            // Wire CollectionUI
            var colUI = root.AddComponent<CollectionUI>();
            colUI.btnFilterFire = btnFire;
            colUI.btnFilterWater = btnWater;
            colUI.btnFilterEarth = btnEarth;
            colUI.btnFilterWind = btnWind;
            colUI.cardGridContainer = gridContainer.transform;
            colUI.detailPanel = detailPanel;
            colUI.detailName = detailName.GetComponent<TextMeshProUGUI>();
            colUI.detailStats = detailStats.GetComponent<TextMeshProUGUI>();
            colUI.detailAbility = detailAbility.GetComponent<TextMeshProUGUI>();
            colUI.detailImage = detailImage.GetComponent<Image>();
            colUI.activeCountText = activeCount.GetComponent<TextMeshProUGUI>();
            colUI.btnBack = btnBack;

            return root;
        }

        // ───────────────────────────────────────
        //  Town Shop Screen
        // ───────────────────────────────────────

        private static GameObject BuildTownShopScreen(Transform parent)
        {
            var root = CreatePanel("Screen_TownShop", parent);

            var bg3 = CreateImage("BG", root.transform, new Color(0.08f, 0.05f, 0.03f));
            StretchFill(bg3);

            var title = CreateTMP("Title", root.transform, "TOWN SHOP", 36f,
                ElementColors.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            var tRT = title.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0.2f, 0.88f);
            tRT.anchorMax = new Vector2(0.8f, 0.97f);
            tRT.offsetMin = tRT.offsetMax = Vector2.zero;

            // Gold display
            var goldDisplay = CreateTMP("GoldDisplay", root.transform, "Gold: 40", 24f,
                ElementColors.Gold, TextAlignmentOptions.Center);
            var gdRT = goldDisplay.GetComponent<RectTransform>();
            gdRT.anchorMin = new Vector2(0.3f, 0.80f);
            gdRT.anchorMax = new Vector2(0.7f, 0.88f);
            gdRT.offsetMin = gdRT.offsetMax = Vector2.zero;

            // Shop options
            var shopArea = new GameObject("ShopOptions", typeof(RectTransform), typeof(VerticalLayoutGroup));
            shopArea.transform.SetParent(root.transform, false);
            var soRT = shopArea.GetComponent<RectTransform>();
            soRT.anchorMin = new Vector2(0.2f, 0.25f);
            soRT.anchorMax = new Vector2(0.8f, 0.78f);
            soRT.offsetMin = soRT.offsetMax = Vector2.zero;
            var soVLG = shopArea.GetComponent<VerticalLayoutGroup>();
            soVLG.spacing = 16f;
            soVLG.childAlignment = TextAnchor.UpperCenter;
            soVLG.childControlWidth = true;
            soVLG.childControlHeight = false;
            soVLG.childForceExpandWidth = true;
            soVLG.childForceExpandHeight = false;

            var btnBuyPack = CreateButton("Btn_BuyPack", shopArea.transform, "Buy Booster Pack (10g)", 56f);
            var btnBuyReroll = CreateButton("Btn_BuyReroll", shopArea.transform, "Buy Reroll Token (5g)", 56f);
            var btnSellDupes = CreateButton("Btn_SellDuplicates", shopArea.transform, "Sell Duplicates", 56f);

            var feedbackText = CreateTMP("FeedbackText", root.transform, "", 20f,
                Color.white, TextAlignmentOptions.Center);
            var ftRT = feedbackText.GetComponent<RectTransform>();
            ftRT.anchorMin = new Vector2(0.2f, 0.16f);
            ftRT.anchorMax = new Vector2(0.8f, 0.24f);
            ftRT.offsetMin = ftRT.offsetMax = Vector2.zero;

            // Back button
            var btnBack2 = CreateButton("Btn_Back", root.transform, "Back", 48f);
            var bb2RT = btnBack2.GetComponent<RectTransform>();
            bb2RT.anchorMin = bb2RT.anchorMax = new Vector2(0f, 0f);
            bb2RT.pivot = new Vector2(0f, 0f);
            bb2RT.anchoredPosition = new Vector2(24f, 24f);
            bb2RT.sizeDelta = new Vector2(160f, 48f);

            // Wire TownShopUI
            var tsUI = root.AddComponent<TownShopUI>();
            tsUI.goldText = goldDisplay.GetComponent<TextMeshProUGUI>();
            tsUI.btnBuyPack = btnBuyPack;
            tsUI.btnBuyReroll = btnBuyReroll;
            tsUI.btnSellDuplicates = btnSellDupes;
            tsUI.feedbackText = feedbackText.GetComponent<TextMeshProUGUI>();
            tsUI.btnBack = btnBack2;

            return root;
        }

        // ───────────────────────────────────────
        //  Settings Screen
        // ───────────────────────────────────────

        private static GameObject BuildSettingsScreen(Transform parent)
        {
            var root = CreatePanel("Screen_Settings", parent);

            var bg4 = CreateImage("BG", root.transform, ElementColors.DarkBg);
            StretchFill(bg4);

            // Dim overlay
            var dim2 = CreateImage("DimOverlay", root.transform, new Color(0f, 0f, 0f, 0.6f));
            StretchFill(dim2);

            // Settings panel
            var panel2 = CreateImage("Panel", root.transform, new Color(0.10f, 0.10f, 0.18f, 0.95f));
            var p2RT = panel2.GetComponent<RectTransform>();
            p2RT.anchorMin = new Vector2(0.5f, 0.5f);
            p2RT.anchorMax = new Vector2(0.5f, 0.5f);
            p2RT.sizeDelta = new Vector2(600f, 500f);

            var settingsTitle = CreateTMP("Title", panel2.transform, "Settings", 32f,
                ElementColors.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            var stRT = settingsTitle.GetComponent<RectTransform>();
            stRT.anchorMin = new Vector2(0.1f, 0.85f);
            stRT.anchorMax = new Vector2(0.9f, 0.95f);
            stRT.offsetMin = stRT.offsetMax = Vector2.zero;

            // Volume section
            var volLabel = CreateTMP("VolumeLabel", panel2.transform, "Master Volume", 18f,
                Color.white, TextAlignmentOptions.Left);
            var vlRT = volLabel.GetComponent<RectTransform>();
            vlRT.anchorMin = new Vector2(0.1f, 0.65f);
            vlRT.anchorMax = new Vector2(0.5f, 0.75f);
            vlRT.offsetMin = vlRT.offsetMax = Vector2.zero;

            var sliderGO = new GameObject("Slider_Volume", typeof(RectTransform), typeof(Slider));
            sliderGO.transform.SetParent(panel2.transform, false);
            var slRT = sliderGO.GetComponent<RectTransform>();
            slRT.anchorMin = new Vector2(0.5f, 0.65f);
            slRT.anchorMax = new Vector2(0.9f, 0.75f);
            slRT.offsetMin = slRT.offsetMax = Vector2.zero;

            // Slider requires background + fill area + handle
            var sliderBG = CreateImage("Background", sliderGO.transform, new Color(0.2f, 0.2f, 0.3f));
            StretchFill(sliderBG);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGO.transform, false);
            var faRT = fillArea.GetComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0f, 0.25f);
            faRT.anchorMax = new Vector2(1f, 0.75f);
            faRT.offsetMin = new Vector2(5f, 0f);
            faRT.offsetMax = new Vector2(-5f, 0f);

            var fill = CreateImage("Fill", fillArea.transform, ElementColors.Gold);
            var fRT = fill.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero;
            fRT.anchorMax = new Vector2(0f, 1f);
            fRT.offsetMin = fRT.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGO.transform, false);
            var haRT = handleArea.GetComponent<RectTransform>();
            haRT.anchorMin = new Vector2(0f, 0f);
            haRT.anchorMax = new Vector2(1f, 1f);
            haRT.offsetMin = new Vector2(10f, 0f);
            haRT.offsetMax = new Vector2(-10f, 0f);

            var handle = CreateImage("Handle", handleArea.transform, Color.white);
            var hRT = handle.GetComponent<RectTransform>();
            hRT.sizeDelta = new Vector2(20f, 0f);

            var slider = sliderGO.GetComponent<Slider>();
            slider.fillRect = fRT;
            slider.handleRect = hRT;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            // Fullscreen toggle
            var fsLabel = CreateTMP("FullscreenLabel", panel2.transform, "Fullscreen", 18f,
                Color.white, TextAlignmentOptions.Left);
            var fsLRT = fsLabel.GetComponent<RectTransform>();
            fsLRT.anchorMin = new Vector2(0.1f, 0.50f);
            fsLRT.anchorMax = new Vector2(0.5f, 0.60f);
            fsLRT.offsetMin = fsLRT.offsetMax = Vector2.zero;

            var toggleGO = new GameObject("Toggle_Fullscreen", typeof(RectTransform), typeof(Toggle));
            toggleGO.transform.SetParent(panel2.transform, false);
            var tgRT = toggleGO.GetComponent<RectTransform>();
            tgRT.anchorMin = new Vector2(0.5f, 0.50f);
            tgRT.anchorMax = new Vector2(0.6f, 0.60f);
            tgRT.offsetMin = tgRT.offsetMax = Vector2.zero;

            var toggleBG = CreateImage("Background", toggleGO.transform, new Color(0.2f, 0.2f, 0.3f));
            StretchFill(toggleBG);
            var checkmark = CreateImage("Checkmark", toggleBG.transform, ElementColors.Gold);
            StretchFill(checkmark);
            var checkRT = checkmark.GetComponent<RectTransform>();
            checkRT.offsetMin = new Vector2(4f, 4f);
            checkRT.offsetMax = new Vector2(-4f, -4f);

            var toggle = toggleGO.GetComponent<Toggle>();
            toggle.graphic = checkmark.GetComponent<Image>();
            toggle.isOn = true;

            // Apply and Back buttons
            var btnApply = CreateButton("Btn_Apply", panel2.transform, "Apply", 48f);
            var baRT = btnApply.GetComponent<RectTransform>();
            baRT.anchorMin = new Vector2(0.55f, 0.08f);
            baRT.anchorMax = new Vector2(0.9f, 0.08f);
            baRT.pivot = new Vector2(0.5f, 0f);
            baRT.sizeDelta = new Vector2(0f, 48f);

            var btnBack3 = CreateButton("Btn_Back", panel2.transform, "Back", 48f);
            var bb3RT = btnBack3.GetComponent<RectTransform>();
            bb3RT.anchorMin = new Vector2(0.1f, 0.08f);
            bb3RT.anchorMax = new Vector2(0.45f, 0.08f);
            bb3RT.pivot = new Vector2(0.5f, 0f);
            bb3RT.sizeDelta = new Vector2(0f, 48f);

            // Reset Data button
            var btnReset = CreateButton("Btn_ResetData", panel2.transform, "Reset All Data", 48f);
            var resetRT = btnReset.GetComponent<RectTransform>();
            resetRT.anchorMin = new Vector2(0.1f, 0.20f);
            resetRT.anchorMax = new Vector2(0.9f, 0.20f);
            resetRT.pivot = new Vector2(0.5f, 0f);
            resetRT.sizeDelta = new Vector2(0f, 48f);
            var resetColors = btnReset.colors;
            resetColors.normalColor = new Color(0.5f, 0.2f, 0.2f);
            resetColors.highlightedColor = new Color(0.6f, 0.3f, 0.3f);
            btnReset.colors = resetColors;

            // Wire SettingsUI
            var sUI = root.AddComponent<SettingsUI>();
            sUI.volumeSlider = slider;
            sUI.fullscreenToggle = toggle;
            sUI.btnApply = btnApply;
            sUI.btnBack = btnBack3;
            sUI.btnResetData = btnReset;

            return root;
        }

        // ───────────────────────────────────────
        //  Encounter Screen (modal overlay)
        // ───────────────────────────────────────

        private static GameObject BuildEncounterScreen(Transform parent)
        {
            var root = CreatePanel("Screen_Encounter", parent);

            // Dim overlay
            var dim = CreateImage("DimOverlay", root.transform, new Color(0f, 0f, 0f, 0.65f));
            StretchFill(dim);
            dim.GetComponent<Image>().raycastTarget = true;

            // Dialogue panel (center, larger for readability)
            var panel = CreateImage("DialoguePanel", root.transform, new Color(0.08f, 0.08f, 0.16f, 0.96f));
            var panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(860f, 420f);
            // Panel border
            var panelOutline = panel.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.3f, 0.3f, 0.5f, 0.8f);
            panelOutline.effectDistance = new Vector2(3f, -3f);

            // Element accent stripe (top edge of panel — color set by EncounterUI.Show)
            var accentBar = CreateImage("AccentBar", panel.transform, ElementColors.Earth);
            var abRT = accentBar.GetComponent<RectTransform>();
            abRT.anchorMin = new Vector2(0f, 1f);
            abRT.anchorMax = new Vector2(1f, 1f);
            abRT.pivot = new Vector2(0.5f, 1f);
            abRT.sizeDelta = new Vector2(0f, 4f);

            // Portrait frame (element-colored border around portrait)
            var portraitFrame = CreateImage("PortraitFrame", panel.transform, ElementColors.Earth);
            var pfRT = portraitFrame.GetComponent<RectTransform>();
            pfRT.anchorMin = new Vector2(0f, 0.5f);
            pfRT.anchorMax = new Vector2(0f, 0.5f);
            pfRT.pivot = new Vector2(0f, 0.5f);
            pfRT.anchoredPosition = new Vector2(24f, 20f);
            pfRT.sizeDelta = new Vector2(136f, 136f);

            // NPC Portrait (inside frame)
            var portrait = CreateImage("NpcPortrait", portraitFrame.transform, new Color(0.15f, 0.15f, 0.25f));
            var portraitRT = portrait.GetComponent<RectTransform>();
            portraitRT.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRT.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRT.sizeDelta = new Vector2(120f, 120f);

            // NPC Name (larger, gold)
            var nameText = CreateTMP("NpcName", panel.transform, "NPC Name", 30f,
                ElementColors.Gold, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            var nameRT = nameText.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, 1f);
            nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.pivot = new Vector2(0f, 1f);
            nameRT.offsetMin = new Vector2(180f, -56f);
            nameRT.offsetMax = new Vector2(-20f, -14f);

            // NPC Element tag
            var elemText = CreateTMP("NpcElement", panel.transform, "Earth", 18f,
                new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.TopLeft, FontStyles.Italic);
            var elemRT = elemText.GetComponent<RectTransform>();
            elemRT.anchorMin = new Vector2(0f, 1f);
            elemRT.anchorMax = new Vector2(1f, 1f);
            elemRT.pivot = new Vector2(0f, 1f);
            elemRT.offsetMin = new Vector2(180f, -86f);
            elemRT.offsetMax = new Vector2(-20f, -56f);

            // Divider line below name/element
            var divider = CreateImage("Divider", panel.transform, new Color(1f, 1f, 1f, 0.15f));
            var divRT = divider.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0f, 1f);
            divRT.anchorMax = new Vector2(1f, 1f);
            divRT.pivot = new Vector2(0.5f, 1f);
            divRT.offsetMin = new Vector2(180f, -92f);
            divRT.offsetMax = new Vector2(-20f, -91f);

            // Dialogue text (more room, wrapping)
            var dialogue = CreateTMP("DialogueText", panel.transform, "...", 22f,
                Color.white, TextAlignmentOptions.TopLeft);
            var dRT = dialogue.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0f, 0f);
            dRT.anchorMax = new Vector2(1f, 1f);
            dRT.offsetMin = new Vector2(180f, 60f);
            dRT.offsetMax = new Vector2(-20f, -100f);
            dialogue.GetComponent<TextMeshProUGUI>().textWrappingMode = TMPro.TextWrappingModes.Normal;

            // Next button (bottom-right of panel)
            var btnNext = CreateButton("Btn_Next", panel.transform, "Next >>", 42f);
            var nextRT = btnNext.GetComponent<RectTransform>();
            nextRT.anchorMin = new Vector2(1f, 0f);
            nextRT.anchorMax = new Vector2(1f, 0f);
            nextRT.pivot = new Vector2(1f, 0f);
            nextRT.anchoredPosition = new Vector2(-20f, 12f);
            nextRT.sizeDelta = new Vector2(140f, 42f);

            // Choice panel (overlaps bottom of dialogue panel for seamless feel)
            var choicePanel = new GameObject("ChoicePanel", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            choicePanel.transform.SetParent(root.transform, false);
            var cpRT = choicePanel.GetComponent<RectTransform>();
            cpRT.anchorMin = new Vector2(0.5f, 0.5f);
            cpRT.anchorMax = new Vector2(0.5f, 0.5f);
            cpRT.anchoredPosition = new Vector2(0f, -250f);
            cpRT.sizeDelta = new Vector2(540f, 60f);

            var cpHLG = choicePanel.GetComponent<HorizontalLayoutGroup>();
            cpHLG.spacing = 20f;
            cpHLG.childAlignment = TextAnchor.MiddleCenter;
            cpHLG.childControlWidth = true;
            cpHLG.childControlHeight = true;
            cpHLG.childForceExpandWidth = true;
            cpHLG.childForceExpandHeight = true;

            var btnDuel = CreateButton("Btn_Duel", choicePanel.transform, "Duel", 60f);
            var btnTrade = CreateButton("Btn_Trade", choicePanel.transform, "Trade", 60f);
            var btnLeave = CreateButton("Btn_Leave", choicePanel.transform, "Leave", 60f);

            // Wire EncounterUI
            var eUI = root.AddComponent<EncounterUI>();
            eUI.dialoguePanel = panel;
            eUI.dimOverlay = dim;
            eUI.npcName = nameText.GetComponent<TextMeshProUGUI>();
            eUI.npcElement = elemText.GetComponent<TextMeshProUGUI>();
            eUI.dialogueText = dialogue.GetComponent<TextMeshProUGUI>();
            eUI.npcPortrait = portrait.GetComponent<Image>();
            eUI.portraitFrame = portraitFrame.GetComponent<Image>();
            eUI.accentBar = accentBar.GetComponent<Image>();
            eUI.btnNext = btnNext;
            eUI.btnDuel = btnDuel;
            eUI.btnTrade = btnTrade;
            eUI.btnLeave = btnLeave;
            eUI.choicePanel = choicePanel;

            return root;
        }

        // ───────────────────────────────────────
        //  Placeholder + HUD
        // ───────────────────────────────────────

        private static GameObject BuildPlaceholder(Transform parent, string goName, string screenLabel)
        {
            var root = CreatePanel(goName, parent);

            var bg = CreateImage("BG", root.transform, ElementColors.DarkBg);
            StretchFill(bg);

            var label = CreateTMP("Label", root.transform, "Coming Soon: " + screenLabel, 36f,
                ElementColors.Gold, TextAlignmentOptions.Center);
            StretchFill(label);

            var ph = root.AddComponent<PlaceholderScreenUI>();
            ph.screenLabel = screenLabel;
            ph.btnBack = CreateButton("Btn_Back", root.transform, "Back", 48f);
            var backRT = ph.btnBack.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0.5f, 0f);
            backRT.anchorMax = new Vector2(0.5f, 0f);
            backRT.pivot = new Vector2(0.5f, 0f);
            backRT.anchoredPosition = new Vector2(0f, 80f);
            backRT.sizeDelta = new Vector2(200f, 48f);

            return root;
        }

        private static GameObject BuildHUD(Transform parent)
        {
            var root = new GameObject("HUD_Persistent", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            StretchFill(root);

            var cg = root.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            // Top bar
            var bar = CreateImage("TopBar", root.transform,
                new Color(0.10f, 0.10f, 0.18f, 0.8f));
            var barRT = bar.GetComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0f, 1f);
            barRT.anchorMax = new Vector2(1f, 1f);
            barRT.pivot = new Vector2(0.5f, 1f);
            barRT.sizeDelta = new Vector2(0f, 48f);

            // Gold counter
            var goldGroup = new GameObject("GoldCounter", typeof(RectTransform));
            goldGroup.transform.SetParent(bar.transform, false);
            var goldGrpRT = goldGroup.GetComponent<RectTransform>();
            goldGrpRT.anchorMin = new Vector2(0f, 0f);
            goldGrpRT.anchorMax = new Vector2(0f, 1f);
            goldGrpRT.pivot = new Vector2(0f, 0.5f);
            goldGrpRT.anchoredPosition = new Vector2(16f, 0f);
            goldGrpRT.sizeDelta = new Vector2(100f, 0f);

            var goldIcon = CreateImage("Icon", goldGroup.transform, new Color(1f, 0.84f, 0f));
            var goldIconRT = goldIcon.GetComponent<RectTransform>();
            goldIconRT.anchorMin = goldIconRT.anchorMax = new Vector2(0f, 0.5f);
            goldIconRT.pivot = new Vector2(0f, 0.5f);
            goldIconRT.anchoredPosition = Vector2.zero;
            goldIconRT.sizeDelta = new Vector2(24f, 24f);

            var goldText = CreateTMP("Text", goldGroup.transform,
                GameState.Gold.ToString(), 20f, ElementColors.Gold, TextAlignmentOptions.Left);
            var goldTextRT = goldText.GetComponent<RectTransform>();
            goldTextRT.anchorMin = new Vector2(0f, 0f);
            goldTextRT.anchorMax = new Vector2(1f, 1f);
            goldTextRT.offsetMin = new Vector2(30f, 0f);
            goldTextRT.offsetMax = Vector2.zero;

            // Pack counter
            var packGroup = new GameObject("PackCounter", typeof(RectTransform));
            packGroup.transform.SetParent(bar.transform, false);
            var packGrpRT = packGroup.GetComponent<RectTransform>();
            packGrpRT.anchorMin = new Vector2(0f, 0f);
            packGrpRT.anchorMax = new Vector2(0f, 1f);
            packGrpRT.pivot = new Vector2(0f, 0.5f);
            packGrpRT.anchoredPosition = new Vector2(120f, 0f);
            packGrpRT.sizeDelta = new Vector2(100f, 0f);

            var packIcon = CreateImage("Icon", packGroup.transform, new Color(0.55f, 0.35f, 0.17f));
            var packIconRT = packIcon.GetComponent<RectTransform>();
            packIconRT.anchorMin = packIconRT.anchorMax = new Vector2(0f, 0.5f);
            packIconRT.pivot = new Vector2(0f, 0.5f);
            packIconRT.anchoredPosition = Vector2.zero;
            packIconRT.sizeDelta = new Vector2(24f, 24f);

            var packText = CreateTMP("Text", packGroup.transform,
                GameState.Packs.ToString(), 20f, Color.white, TextAlignmentOptions.Left);
            var packTextRT = packText.GetComponent<RectTransform>();
            packTextRT.anchorMin = new Vector2(0f, 0f);
            packTextRT.anchorMax = new Vector2(1f, 1f);
            packTextRT.offsetMin = new Vector2(30f, 0f);
            packTextRT.offsetMax = Vector2.zero;

            // Element indicators
            var elemGroup = new GameObject("ElementIndicators", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            elemGroup.transform.SetParent(bar.transform, false);
            var elemRT = elemGroup.GetComponent<RectTransform>();
            elemRT.anchorMin = elemRT.anchorMax = new Vector2(0.5f, 0.5f);
            elemRT.sizeDelta = new Vector2(80f, 16f);
            var elemHLG = elemGroup.GetComponent<HorizontalLayoutGroup>();
            elemHLG.spacing = 8f;
            elemHLG.childAlignment = TextAnchor.MiddleCenter;
            elemHLG.childControlWidth = false;
            elemHLG.childControlHeight = false;

            CreateDot("FireDot", elemGroup.transform, ElementColors.Fire, 12f);
            CreateDot("WaterDot", elemGroup.transform, ElementColors.Water, 12f);
            CreateDot("EarthDot", elemGroup.transform, ElementColors.Earth, 12f);
            CreateDot("WindDot", elemGroup.transform, ElementColors.Wind, 12f);

            // Zone name
            var zoneText = CreateTMP("ZoneName", bar.transform,
                GameState.CurrentZone, 18f, Color.white, TextAlignmentOptions.Right);
            var zoneRT = zoneText.GetComponent<RectTransform>();
            zoneRT.anchorMin = new Vector2(1f, 0f);
            zoneRT.anchorMax = new Vector2(1f, 1f);
            zoneRT.pivot = new Vector2(1f, 0.5f);
            zoneRT.anchoredPosition = new Vector2(-16f, 0f);
            zoneRT.sizeDelta = new Vector2(200f, 0f);

            // Wire HUD component
            var hud = root.AddComponent<HUD>();
            hud.goldText = goldText.GetComponent<TextMeshProUGUI>();
            hud.packText = packText.GetComponent<TextMeshProUGUI>();
            hud.zoneText = zoneText.GetComponent<TextMeshProUGUI>();

            return root;
        }

        // ═══════════════════════════════════════
        //  UI element helpers
        // ═══════════════════════════════════════

        private static GameObject CreatePanel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            StretchFill(go);
            return go;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static void CreateQuadrant(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = CreateImage(name, parent, color);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Creates a quadrant panel with RectMask2D clipping and a ParallaxBackgroundController.
        /// MainMenuUI.OnEnable calls LoadByBiomeName on each controller at runtime.
        /// </summary>
        private static ParallaxBackgroundController CreateParallaxQuadrant(
            string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.AddComponent<RectMask2D>();
            return go.AddComponent<ParallaxBackgroundController>();
        }

        private static GameObject CreateTMP(string name, Transform parent, string text, float fontSize,
            Color color, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            return go;
        }

        private static Button CreateButton(string name, Transform parent, string label, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;

            var img = go.GetComponent<Image>();
            img.color = Color.white;

            var outlineComp = go.AddComponent<Outline>();
            outlineComp.effectColor = ElementColors.Gold;
            outlineComp.effectDistance = new Vector2(2f, -2f);

            var outlineComp2 = go.AddComponent<Outline>();
            outlineComp2.effectColor = ElementColors.Gold;
            outlineComp2.effectDistance = new Vector2(-2f, 2f);

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = ElementColors.Dark;
            colors.highlightedColor = new Color(0.16f, 0.16f, 0.30f);
            colors.pressedColor = new Color(0.04f, 0.04f, 0.12f);
            colors.selectedColor = ElementColors.Dark;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            var text = CreateTMP("Text", go.transform, label, 24f, ElementColors.Gold,
                TextAlignmentOptions.Center);
            StretchFill(text);
            text.GetComponent<TextMeshProUGUI>().raycastTarget = false;

            return btn;
        }

        private static void CreateDot(string name, Transform parent, Color color, float size)
        {
            var go = CreateImage(name, parent, color);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            go.GetComponent<Image>().raycastTarget = false;
        }

        private static void StretchFill(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ═══════════════════════════════════════
        //  Utilities
        // ═══════════════════════════════════════

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var folderName = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static int CountChildren(Transform t)
        {
            int count = 1;
            foreach (Transform child in t)
                count += CountChildren(child);
            return count;
        }
    }
}
