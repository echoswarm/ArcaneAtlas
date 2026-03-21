using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ArcaneAtlas.Data;

namespace ArcaneAtlas.Editor
{
    /// <summary>
    /// Visual editor for designing card styles. Assign frame, element, creature sprites
    /// and preview the assembled card in real-time.
    /// Arcane Atlas > Card Designer
    /// </summary>
    public class CardDesignerWindow : EditorWindow
    {
        private CardStyleDef editingStyle;
        private string styleName = "Default";

        // Preview state
        private ElementType previewElement = ElementType.Fire;
        private CardRarity previewRarity = CardRarity.Common;
        private int previewSpriteIndex = 1;
        private Vector2 scrollPos;
        private float previewScale = 1.5f;

        // Sprite browser
        private string spriteFolder = "Assets/Art/Sprites/cards";
        private List<Sprite> browseSprites = new List<Sprite>();
        private Vector2 browseScrollPos;
        private float browseSize = 48f;
        private string browseFilter = "";
        private string activeSlot = "";

        // Tab state
        private int activeTab = 0;
        private static readonly string[] TAB_NAMES = { "Style Editor", "Card Gallery" };

        // Gallery state
        private Vector2 galleryScrollPos;
        private ElementType galleryFilter = ElementType.Fire;
        private float galleryCardScale = 1.0f;
        private bool galleryShowAll = true;
        private CardRarity galleryRarityFilter = CardRarity.Common;

        [MenuItem("Arcane Atlas/Card Designer", false, 40)]
        public static void ShowWindow()
        {
            var window = GetWindow<CardDesignerWindow>("Card Designer");
            window.minSize = new Vector2(800, 600);
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawToolbar();
            EditorGUILayout.EndHorizontal();

            // Tab bar
            activeTab = GUILayout.Toolbar(activeTab, TAB_NAMES, GUILayout.Height(24));

            if (activeTab == 1)
            {
                DrawGallery();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawPreviewPanel();
            DrawSpritePanel();
            EditorGUILayout.EndHorizontal();
        }

        // ════════════════════════════════════════════
        //  TOOLBAR
        // ════════════════════════════════════════════

        private void DrawToolbar()
        {
            EditorGUILayout.LabelField("Style:", GUILayout.Width(40));
            styleName = EditorGUILayout.TextField(styleName, GUILayout.Width(120));

            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                editingStyle = null;
                styleName = "NewStyle";
            }

            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(40)))
                LoadStyle();

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(40)))
                SaveStyle();

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Browse:", GUILayout.Width(50));
            spriteFolder = EditorGUILayout.TextField(spriteFolder, GUILayout.Width(200));
            if (GUILayout.Button("Scan", EditorStyles.toolbarButton, GUILayout.Width(40)))
                ScanSprites();

            GUILayout.FlexibleSpace();
        }

        // ════════════════════════════════════════════
        //  LEFT PANEL — Sprite Slots
        // ════════════════════════════════════════════

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(280));
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            if (editingStyle == null)
            {
                EditorGUILayout.HelpBox("No style loaded. Click 'Save' to create a new one, or 'Load' an existing one.", MessageType.Info);
                EnsureStyle();
            }

            GUILayout.Label("Card Frames", EditorStyles.boldLabel);
            DrawSpriteSlot("FrameCommon", "Common Frame", ref editingStyle.FrameCommon);
            DrawSpriteSlot("FrameUncommon", "Uncommon Frame", ref editingStyle.FrameUncommon);
            DrawSpriteSlot("FrameRare", "Rare Frame", ref editingStyle.FrameRare);
            DrawSpriteSlot("FrameLegendary", "Legendary Frame", ref editingStyle.FrameLegendary);

            GUILayout.Space(6);
            GUILayout.Label("Frame Details", EditorStyles.boldLabel);
            DrawSpriteSlot("DetailCommon", "Common Detail", ref editingStyle.DetailCommon);
            DrawSpriteSlot("DetailUncommon", "Uncommon Detail", ref editingStyle.DetailUncommon);
            DrawSpriteSlot("DetailRare", "Rare Detail", ref editingStyle.DetailRare);
            DrawSpriteSlot("DetailLegendary", "Legendary Detail", ref editingStyle.DetailLegendary);

            GUILayout.Space(6);
            GUILayout.Label("Element Icons", EditorStyles.boldLabel);
            DrawSpriteSlot("IconFire", "Fire Icon", ref editingStyle.IconFire);
            DrawSpriteSlot("IconWater", "Water Icon", ref editingStyle.IconWater);
            DrawSpriteSlot("IconEarth", "Earth Icon", ref editingStyle.IconEarth);
            DrawSpriteSlot("IconWind", "Wind Icon", ref editingStyle.IconWind);

            GUILayout.Space(6);
            GUILayout.Label("Element Tints", EditorStyles.boldLabel);
            editingStyle.TintFire = EditorGUILayout.ColorField("Fire", editingStyle.TintFire);
            editingStyle.TintWater = EditorGUILayout.ColorField("Water", editingStyle.TintWater);
            editingStyle.TintEarth = EditorGUILayout.ColorField("Earth", editingStyle.TintEarth);
            editingStyle.TintWind = EditorGUILayout.ColorField("Wind", editingStyle.TintWind);

            GUILayout.Space(6);
            GUILayout.Label("Card Backs", EditorStyles.boldLabel);
            DrawSpriteSlot("CardBack", "Card Back", ref editingStyle.CardBack);
            DrawSpriteSlot("CardBackShop", "Shop Card Back", ref editingStyle.CardBackShop);

            GUILayout.Space(6);
            GUILayout.Label("Tier Badges", EditorStyles.boldLabel);
            DrawSpriteSlot("BadgeBronze", "Bronze Badge", ref editingStyle.BadgeBronze);
            DrawSpriteSlot("BadgeSilver", "Silver Badge", ref editingStyle.BadgeSilver);
            DrawSpriteSlot("BadgeGold", "Gold Badge", ref editingStyle.BadgeGold);

            GUILayout.Space(6);
            GUILayout.Label("Shop Frames", EditorStyles.boldLabel);
            DrawSpriteSlot("ShopCommon", "Shop Common", ref editingStyle.ShopFrameCommon);
            DrawSpriteSlot("ShopUncommon", "Shop Uncommon", ref editingStyle.ShopFrameUncommon);
            DrawSpriteSlot("ShopRare", "Shop Rare", ref editingStyle.ShopFrameRare);
            DrawSpriteSlot("ShopLegendary", "Shop Legendary", ref editingStyle.ShopFrameLegendary);

            GUILayout.Space(6);
            GUILayout.Label("Creature Portrait", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Current: {previewElement} #{previewSpriteIndex}\nClick a card in Gallery to select, then assign art from the sprite browser.",
                MessageType.Info);

            // Show current creature art for the previewed card
            Sprite currentCreature = LoadCreatureInEditor(GetPreviewCard());
            DrawSpriteSlot($"Creature_{previewElement}_{previewSpriteIndex}",
                $"{previewElement} #{previewSpriteIndex}", ref currentCreature);

            // Quick navigation: cycle through all cards in current element
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("◀ Prev", GUILayout.Width(60)))
            {
                previewSpriteIndex = Mathf.Max(1, previewSpriteIndex - 1);
            }
            if (GUILayout.Button("Next ▶", GUILayout.Width(60)))
            {
                previewSpriteIndex = Mathf.Min(25, previewSpriteIndex + 1);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSpriteSlot(string slotId, string label, ref Sprite sprite)
        {
            bool isActive = activeSlot == slotId;
            Color prevBg = GUI.backgroundColor;
            if (isActive) GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Preview
            Rect previewRect = GUILayoutUtility.GetRect(28, 28, GUILayout.Width(28), GUILayout.Height(28));
            if (sprite != null)
            {
                Rect uv = new Rect(
                    sprite.rect.x / sprite.texture.width, sprite.rect.y / sprite.texture.height,
                    sprite.rect.width / sprite.texture.width, sprite.rect.height / sprite.texture.height);
                GUI.DrawTextureWithTexCoords(previewRect, sprite.texture, uv);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f));
            }

            GUILayout.Space(4);
            EditorGUILayout.LabelField(label, GUILayout.Width(120), GUILayout.Height(28));

            // Select button
            if (GUILayout.Button(isActive ? "●" : "○", GUILayout.Width(22), GUILayout.Height(28)))
                activeSlot = isActive ? "" : slotId;

            // Clear
            if (sprite != null && GUILayout.Button("×", GUILayout.Width(18), GUILayout.Height(28)))
                sprite = null;

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = prevBg;
        }

        // ════════════════════════════════════════════
        //  PREVIEW PANEL — Live Card Preview
        // ════════════════════════════════════════════

        private void DrawPreviewPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            GUILayout.Label("Card Preview", EditorStyles.boldLabel);

            previewElement = (ElementType)EditorGUILayout.EnumPopup("Element", previewElement);
            previewRarity = (CardRarity)EditorGUILayout.EnumPopup("Rarity", previewRarity);
            previewSpriteIndex = EditorGUILayout.IntSlider("Creature #", previewSpriteIndex, 1, 25);
            previewScale = EditorGUILayout.Slider("Scale", previewScale, 0.5f, 3f);

            GUILayout.Space(8);

            EnsureStyle();

            // Draw the card preview
            float cardW = 160 * previewScale;
            float cardH = 210 * previewScale;
            Rect cardRect = GUILayoutUtility.GetRect(cardW, cardH, GUILayout.Width(cardW), GUILayout.Height(cardH));

            // Background tint
            Color tint = editingStyle.GetElementTint(previewElement) * 0.3f + new Color(0, 0, 0, 0.7f);
            EditorGUI.DrawRect(cardRect, tint);

            // Creature art
            var tempCard = ScriptableObject.CreateInstance<CardData>();
            tempCard.Element = previewElement;
            tempCard.SpriteIndex = previewSpriteIndex;
            tempCard.Rarity = previewRarity;
            tempCard.CardName = $"{previewElement} Creature";
            tempCard.Attack = 3;
            tempCard.Health = 4;
            tempCard.Cost = 2;

            // Draw creature art
            var creatureSprite = LoadCreatureInEditor(tempCard);
            if (creatureSprite != null)
            {
                Rect creatureRect = new Rect(
                    cardRect.x + cardW * 0.10f, cardRect.y + cardH * 0.15f,
                    cardW * 0.80f, cardH * 0.55f);
                DrawSprite(creatureRect, creatureSprite);
            }
            else
            {
                // Try texture fallback
                var creatureTex = LoadCreatureTexture(tempCard);
                if (creatureTex != null)
                {
                    Rect creatureRect = new Rect(
                        cardRect.x + cardW * 0.10f, cardRect.y + cardH * 0.15f,
                        cardW * 0.80f, cardH * 0.55f);
                    GUI.DrawTexture(creatureRect, creatureTex, ScaleMode.ScaleToFit);
                }
            }

            // Frame
            Sprite frame = editingStyle.GetFrame(previewRarity);
            if (frame != null)
            {
                Rect uv = new Rect(
                    frame.rect.x / frame.texture.width, frame.rect.y / frame.texture.height,
                    frame.rect.width / frame.texture.width, frame.rect.height / frame.texture.height);
                GUI.DrawTextureWithTexCoords(cardRect, frame.texture, uv);
            }

            // Detail overlay
            Sprite detail = editingStyle.GetDetail(previewRarity);
            if (detail != null)
            {
                Rect uv = new Rect(
                    detail.rect.x / detail.texture.width, detail.rect.y / detail.texture.height,
                    detail.rect.width / detail.texture.width, detail.rect.height / detail.texture.height);
                GUI.DrawTextureWithTexCoords(cardRect, detail.texture, uv);
            }

            // Element icon
            Sprite icon = editingStyle.GetElementIcon(previewElement);
            if (icon != null)
            {
                Rect iconRect = new Rect(cardRect.x + 4, cardRect.y + 4, 24 * previewScale, 24 * previewScale);
                Rect uv = new Rect(
                    icon.rect.x / icon.texture.width, icon.rect.y / icon.texture.height,
                    icon.rect.width / icon.texture.width, icon.rect.height / icon.texture.height);
                GUI.DrawTextureWithTexCoords(iconRect, icon.texture, uv);
            }

            // Text overlays
            GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = (int)(10 * previewScale) };
            nameStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(cardRect.x, cardRect.y + cardH * 0.72f, cardW, cardH * 0.12f), tempCard.CardName, nameStyle);

            GUIStyle statsStyle = new GUIStyle(EditorStyles.label) { fontSize = (int)(12 * previewScale) };
            statsStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(cardRect.x + 6, cardRect.y + cardH * 0.85f, cardW / 2, cardH * 0.12f),
                $"{tempCard.Attack}/{tempCard.Health}", statsStyle);

            GUIStyle costStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = (int)(14 * previewScale) };
            costStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(cardRect.x + 2, cardRect.y + 2, 24 * previewScale, 24 * previewScale),
                tempCard.Cost.ToString(), costStyle);

            DestroyImmediate(tempCard);

            EditorGUILayout.EndVertical();
        }

        // ════════════════════════════════════════════
        //  SPRITE BROWSER — Pick sprites for slots
        // ════════════════════════════════════════════

        private void DrawSpritePanel()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Sprite Browser", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            browseFilter = EditorGUILayout.TextField(browseFilter, EditorStyles.toolbarSearchField);
            browseSize = EditorGUILayout.Slider(browseSize, 24f, 96f, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            if (browseSprites.Count == 0)
            {
                EditorGUILayout.HelpBox("Click 'Scan' in the toolbar to load sprites from the cards folder.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            var filtered = browseSprites;
            if (!string.IsNullOrEmpty(browseFilter))
                filtered = browseSprites.Where(s => s.name.ToLower().Contains(browseFilter.ToLower())).ToList();

            EditorGUILayout.LabelField($"{filtered.Count} sprites", EditorStyles.miniLabel);

            browseScrollPos = EditorGUILayout.BeginScrollView(browseScrollPos);

            float availWidth = position.width - 500f;
            int cols = Mathf.Max(1, Mathf.FloorToInt(availWidth / (browseSize + 4)));
            int col = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var sprite in filtered)
            {
                if (col >= cols)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    col = 0;
                }

                Rect btnRect = GUILayoutUtility.GetRect(browseSize + 2, browseSize + 2,
                    GUILayout.Width(browseSize + 2), GUILayout.Height(browseSize + 2));

                Rect uv = new Rect(
                    sprite.rect.x / sprite.texture.width, sprite.rect.y / sprite.texture.height,
                    sprite.rect.width / sprite.texture.width, sprite.rect.height / sprite.texture.height);
                GUI.DrawTextureWithTexCoords(new Rect(btnRect.x + 1, btnRect.y + 1, browseSize, browseSize),
                    sprite.texture, uv);

                // Click to assign to active slot
                if (Event.current.type == EventType.MouseDown && btnRect.Contains(Event.current.mousePosition))
                {
                    if (!string.IsNullOrEmpty(activeSlot))
                    {
                        AssignToSlot(activeSlot, sprite);
                        Event.current.Use();
                        Repaint();
                    }
                }

                col++;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ════════════════════════════════════════════
        //  CARD GALLERY — Preview all cards
        // ════════════════════════════════════════════

        private void DrawGallery()
        {
            EnsureStyle();

            // Filter bar
            EditorGUILayout.BeginHorizontal();
            galleryShowAll = EditorGUILayout.Toggle("Show All", galleryShowAll, GUILayout.Width(80));
            if (!galleryShowAll)
            {
                galleryFilter = (ElementType)EditorGUILayout.EnumPopup("Element", galleryFilter, GUILayout.Width(200));
                galleryRarityFilter = (CardRarity)EditorGUILayout.EnumPopup("Rarity", galleryRarityFilter, GUILayout.Width(200));
            }
            galleryCardScale = EditorGUILayout.Slider("Card Size", galleryCardScale, 0.4f, 2f, GUILayout.Width(250));
            EditorGUILayout.EndHorizontal();

            var allCards = CardDatabase.GetAllCards();
            var filtered = allCards;
            if (!galleryShowAll)
            {
                filtered = allCards.Where(c =>
                    c.Element == galleryFilter &&
                    c.Rarity == galleryRarityFilter).ToList();
            }

            EditorGUILayout.LabelField($"Showing {filtered.Count} of {allCards.Count} cards", EditorStyles.miniLabel);

            float cardW = 120 * galleryCardScale;
            float cardH = 158 * galleryCardScale;
            float availWidth = position.width - 20f;
            int cols = Mathf.Max(1, Mathf.FloorToInt(availWidth / (cardW + 8)));

            galleryScrollPos = EditorGUILayout.BeginScrollView(galleryScrollPos);

            int col = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var card in filtered)
            {
                if (col >= cols)
                {
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(4);
                    EditorGUILayout.BeginHorizontal();
                    col = 0;
                }

                if (DrawGalleryCard(card, cardW, cardH))
                {
                    // Card clicked — load into preview and switch to editor
                    previewElement = card.Element;
                    previewRarity = card.Rarity;
                    previewSpriteIndex = card.SpriteIndex;
                    activeTab = 0; // Switch to Style Editor
                    Repaint();
                }
                GUILayout.Space(4);
                col++;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private bool DrawGalleryCard(CardData card, float cardW, float cardH)
        {
            Rect cardRect = GUILayoutUtility.GetRect(cardW, cardH, GUILayout.Width(cardW), GUILayout.Height(cardH));
            bool clicked = false;

            // Click detection
            if (Event.current.type == EventType.MouseDown && cardRect.Contains(Event.current.mousePosition))
            {
                clicked = true;
                Event.current.Use();
            }

            // Hover highlight
            if (cardRect.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(cardRect, new Color(1f, 1f, 1f, 0.08f));

            // Background tint
            Color tint = editingStyle.GetElementTint(card.Element) * 0.3f + new Color(0, 0, 0, 0.7f);
            EditorGUI.DrawRect(cardRect, tint);

            // Creature art
            var galleryCreature = LoadCreatureInEditor(card);
            if (galleryCreature != null)
            {
                Rect creatureRect = new Rect(
                    cardRect.x + cardW * 0.08f, cardRect.y + cardH * 0.15f,
                    cardW * 0.84f, cardH * 0.52f);
                DrawSprite(creatureRect, galleryCreature);
            }

            // Frame
            Sprite frame = editingStyle.GetFrame(card.Rarity);
            if (frame != null)
                DrawSprite(cardRect, frame);

            // Detail
            Sprite detail = editingStyle.GetDetail(card.Rarity);
            if (detail != null)
                DrawSprite(cardRect, detail);

            // Element icon
            Sprite icon = editingStyle.GetElementIcon(card.Element);
            if (icon != null)
            {
                float iconSize = 16 * galleryCardScale;
                DrawSprite(new Rect(cardRect.x + 3, cardRect.y + 3, iconSize, iconSize), icon);
            }

            // Cost
            float fontSize = Mathf.Max(8f, 11f * galleryCardScale);
            GUIStyle costStyle = new GUIStyle(EditorStyles.boldLabel) {
                alignment = TextAnchor.MiddleCenter, fontSize = (int)fontSize };
            costStyle.normal.textColor = Color.yellow;
            float costSize = 16 * galleryCardScale;
            GUI.Label(new Rect(cardRect.x + 2, cardRect.y + 2, costSize, costSize), card.Cost.ToString(), costStyle);

            // Name
            float nameSize = Mathf.Max(7f, 9f * galleryCardScale);
            GUIStyle nameStyle = new GUIStyle(EditorStyles.label) {
                alignment = TextAnchor.MiddleCenter, fontSize = (int)nameSize,
                wordWrap = true };
            nameStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(cardRect.x + 2, cardRect.y + cardH * 0.68f, cardW - 4, cardH * 0.14f),
                card.CardName, nameStyle);

            // Stats (ATK / HP)
            GUIStyle statsStyle = new GUIStyle(EditorStyles.label) { fontSize = (int)fontSize };
            statsStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);
            GUI.Label(new Rect(cardRect.x + 4, cardRect.y + cardH * 0.84f, cardW * 0.3f, cardH * 0.14f),
                card.Attack.ToString(), statsStyle);

            GUIStyle hpStyle = new GUIStyle(EditorStyles.label) {
                alignment = TextAnchor.MiddleRight, fontSize = (int)fontSize };
            hpStyle.normal.textColor = new Color(0.3f, 1f, 0.3f);
            GUI.Label(new Rect(cardRect.x + cardW * 0.6f, cardRect.y + cardH * 0.84f, cardW * 0.36f, cardH * 0.14f),
                card.Health.ToString(), hpStyle);

            // Rarity label
            GUIStyle rarityStyle = new GUIStyle(EditorStyles.miniLabel) {
                alignment = TextAnchor.MiddleCenter, fontSize = (int)Mathf.Max(6f, 7f * galleryCardScale) };
            rarityStyle.normal.textColor = GetRarityColor(card.Rarity);
            GUI.Label(new Rect(cardRect.x, cardRect.y + cardH * 0.12f, cardW, cardH * 0.06f),
                card.Rarity.ToString().ToUpper(), rarityStyle);

            // Tier indicator
            GUIStyle tierStyle = new GUIStyle(EditorStyles.miniLabel) {
                alignment = TextAnchor.MiddleRight, fontSize = (int)Mathf.Max(6f, 7f * galleryCardScale) };
            tierStyle.normal.textColor = new Color(0.8f, 0.7f, 0.5f);
            GUI.Label(new Rect(cardRect.x, cardRect.y + 2, cardW - 4, 12 * galleryCardScale),
                $"T{card.Tier}", tierStyle);

            return clicked;
        }

        private void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;
            Rect uv = new Rect(
                sprite.rect.x / sprite.texture.width, sprite.rect.y / sprite.texture.height,
                sprite.rect.width / sprite.texture.width, sprite.rect.height / sprite.texture.height);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv);
        }

        private Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return new Color(0.7f, 0.7f, 0.7f);
                case CardRarity.Uncommon: return new Color(0.3f, 0.8f, 0.3f);
                case CardRarity.Rare: return new Color(0.3f, 0.5f, 1f);
                case CardRarity.Legendary: return new Color(1f, 0.7f, 0.2f);
                default: return Color.white;
            }
        }

        /// <summary>
        /// Loads creature art in the editor using AssetDatabase (Resources.Load doesn't work outside Play).
        /// Checks style overrides first, then falls back to the CardArt Resources path.
        /// </summary>
        // Cache loaded creature textures to avoid reloading every frame
        private static Dictionary<string, Texture2D> creatureTexCache = new Dictionary<string, Texture2D>();

        private Texture2D LoadCreatureTexture(CardData card)
        {
            if (card == null || card.SpriteIndex <= 0) return null;

            string elem = card.Element.ToString().ToLower();
            string assetPath = $"Assets/Resources/CardArt/{elem}/creature_{elem}_{card.SpriteIndex:D2}.png";

            if (creatureTexCache.TryGetValue(assetPath, out var cached) && cached != null)
                return cached;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex != null)
                creatureTexCache[assetPath] = tex;

            return tex;
        }

        /// <summary>
        /// Draws creature art directly as a Texture2D (bypasses Sprite loading issues).
        /// </summary>
        private void DrawCreatureArt(Rect rect, CardData card)
        {
            // Check style overrides first
            if (editingStyle != null && editingStyle.CreatureOverrides != null)
            {
                foreach (var ov in editingStyle.CreatureOverrides)
                {
                    if (ov.Element == card.Element && ov.SpriteIndex == card.SpriteIndex && ov.Sprite != null)
                    {
                        DrawSprite(rect, ov.Sprite);
                        return;
                    }
                }
            }

            var tex = LoadCreatureTexture(card);
            if (tex != null)
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
        }

        // Keep LoadCreatureInEditor for the sprite slot preview (returns Sprite or null)
        private Sprite LoadCreatureInEditor(CardData card)
        {
            if (card == null || card.SpriteIndex <= 0) return null;

            if (editingStyle != null && editingStyle.CreatureOverrides != null)
            {
                foreach (var ov in editingStyle.CreatureOverrides)
                {
                    if (ov.Element == card.Element && ov.SpriteIndex == card.SpriteIndex && ov.Sprite != null)
                        return ov.Sprite;
                }
            }

            string elem = card.Element.ToString().ToLower();
            string path = $"Assets/Resources/CardArt/{elem}/creature_{elem}_{card.SpriteIndex:D2}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private CardData GetPreviewCard()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.Element = previewElement;
            card.SpriteIndex = previewSpriteIndex;
            card.Rarity = previewRarity;
            card.CardName = $"{previewElement} #{previewSpriteIndex}";
            return card;
        }

        // ════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════

        private void EnsureStyle()
        {
            if (editingStyle == null)
                editingStyle = ScriptableObject.CreateInstance<CardStyleDef>();
        }

        private void AssignToSlot(string slotId, Sprite sprite)
        {
            EnsureStyle();
            switch (slotId)
            {
                case "FrameCommon": editingStyle.FrameCommon = sprite; break;
                case "FrameUncommon": editingStyle.FrameUncommon = sprite; break;
                case "FrameRare": editingStyle.FrameRare = sprite; break;
                case "FrameLegendary": editingStyle.FrameLegendary = sprite; break;
                case "DetailCommon": editingStyle.DetailCommon = sprite; break;
                case "DetailUncommon": editingStyle.DetailUncommon = sprite; break;
                case "DetailRare": editingStyle.DetailRare = sprite; break;
                case "DetailLegendary": editingStyle.DetailLegendary = sprite; break;
                case "IconFire": editingStyle.IconFire = sprite; break;
                case "IconWater": editingStyle.IconWater = sprite; break;
                case "IconEarth": editingStyle.IconEarth = sprite; break;
                case "IconWind": editingStyle.IconWind = sprite; break;
                case "CardBack": editingStyle.CardBack = sprite; break;
                case "CardBackShop": editingStyle.CardBackShop = sprite; break;
                case "BadgeBronze": editingStyle.BadgeBronze = sprite; break;
                case "BadgeSilver": editingStyle.BadgeSilver = sprite; break;
                case "BadgeGold": editingStyle.BadgeGold = sprite; break;
                case "ShopCommon": editingStyle.ShopFrameCommon = sprite; break;
                case "ShopUncommon": editingStyle.ShopFrameUncommon = sprite; break;
                case "ShopRare": editingStyle.ShopFrameRare = sprite; break;
                case "ShopLegendary": editingStyle.ShopFrameLegendary = sprite; break;
                default:
                    // Handle creature art assignment: Creature_{Element}_{Index}
                    if (slotId.StartsWith("Creature_"))
                    {
                        AssignCreatureArt(slotId, sprite);
                    }
                    break;
            }
        }

        /// <summary>
        /// Assigns a sprite as creature art for the given element+index.
        /// Copies the sprite's texture to Resources/CardArt/ so it loads at runtime,
        /// and adds it as a CreatureOverride on the style.
        /// </summary>
        private void AssignCreatureArt(string slotId, Sprite sprite)
        {
            // Parse "Creature_Fire_1" -> element=Fire, index=1
            var parts = slotId.Split('_');
            if (parts.Length < 3) return;

            if (!System.Enum.TryParse<ElementType>(parts[1], out var element)) return;
            if (!int.TryParse(parts[2], out int index)) return;

            string elem = element.ToString().ToLower();

            // Copy the sprite's source texture to Resources/CardArt/{element}/
            string srcPath = AssetDatabase.GetAssetPath(sprite.texture);
            string destFolder = $"Assets/Resources/CardArt/{elem}";
            if (!AssetDatabase.IsValidFolder(destFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources/CardArt"))
                    AssetDatabase.CreateFolder("Assets/Resources", "CardArt");
                AssetDatabase.CreateFolder("Assets/Resources/CardArt", elem);
            }

            string destPath = $"{destFolder}/creature_{elem}_{index:D2}.png";
            string fullSrc = System.IO.Path.Combine(Application.dataPath, "..", srcPath).Replace('\\', '/');
            string fullDest = System.IO.Path.Combine(Application.dataPath, "..", destPath).Replace('\\', '/');

            // If the source is a sub-sprite from a larger texture, we need to extract it
            if (sprite.texture != null)
            {
                var tex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);

                // Make source readable
                var srcImporter = AssetImporter.GetAtPath(srcPath) as TextureImporter;
                bool wasReadable = srcImporter != null && srcImporter.isReadable;
                if (srcImporter != null && !wasReadable)
                {
                    srcImporter.isReadable = true;
                    srcImporter.SaveAndReimport();
                }

                var pixels = sprite.texture.GetPixels(
                    (int)sprite.rect.x, (int)sprite.rect.y,
                    (int)sprite.rect.width, (int)sprite.rect.height);
                tex.SetPixels(pixels);
                tex.Apply();

                System.IO.File.WriteAllBytes(fullDest, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);

                // Restore readability
                if (srcImporter != null && !wasReadable)
                {
                    srcImporter.isReadable = false;
                    srcImporter.SaveAndReimport();
                }

                AssetDatabase.ImportAsset(destPath, ImportAssetOptions.ForceSynchronousImport);

                // Configure as single sprite
                var destImporter = AssetImporter.GetAtPath(destPath) as TextureImporter;
                if (destImporter != null)
                {
                    destImporter.textureType = TextureImporterType.Sprite;
                    destImporter.spriteImportMode = SpriteImportMode.Single;
                    destImporter.spritePixelsPerUnit = 32;
                    destImporter.filterMode = FilterMode.Point;
                    destImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    destImporter.SaveAndReimport();
                }
            }

            var savedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(destPath);
            Debug.Log($"[CardDesigner] Assigned creature art: {element} #{index} -> {destPath} (sprite={savedSprite != null})");
        }

        private void ScanSprites()
        {
            browseSprites.Clear();
            if (!AssetDatabase.IsValidFolder(spriteFolder)) return;

            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { spriteFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) browseSprites.Add(sprite);
            }

            // Also load sub-sprites from multi-sprite textures
            var texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { spriteFolder });
            foreach (string guid in texGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                foreach (var asset in assets)
                {
                    if (asset is Sprite s && !browseSprites.Contains(s))
                        browseSprites.Add(s);
                }
            }

            browseSprites.Sort((a, b) => string.Compare(a.name, b.name));
        }

        private void SaveStyle()
        {
            EnsureStyle();
            string folder = "Assets/Resources/CardStyles";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateFolder("Assets/Resources", "CardStyles");
            }

            string path = $"{folder}/{styleName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CardStyleDef>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(editingStyle, existing);
                existing.StyleName = styleName;
                EditorUtility.SetDirty(existing);
                editingStyle = existing;
            }
            else
            {
                editingStyle.StyleName = styleName;
                AssetDatabase.CreateAsset(editingStyle, path);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CardDesigner] Saved card style '{styleName}' to {path}");
        }

        private void LoadStyle()
        {
            string path = EditorUtility.OpenFilePanel("Load Card Style", "Assets/Resources/CardStyles", "asset");
            if (string.IsNullOrEmpty(path)) return;
            if (path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);

            var loaded = AssetDatabase.LoadAssetAtPath<CardStyleDef>(path);
            if (loaded != null)
            {
                editingStyle = loaded;
                styleName = loaded.StyleName;
                Debug.Log($"[CardDesigner] Loaded card style '{styleName}'");
            }
        }
    }
}
