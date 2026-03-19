namespace ArcaneAtlas.Data
{
    public static class IntroData
    {
        public struct IntroScene
        {
            public string ImageDescription; // For placeholder rendering
            public string Text;
        }

        public static readonly IntroScene[] Scenes = new IntroScene[]
        {
            new IntroScene
            {
                ImageDescription = "An old man at a desk, candlelight, surrounded by maps and cards. The Atlas open before him, nearly complete.",
                Text = "For fifty years, the cartographer traveled the four regions of the Arcane Realm.\nEvery creature catalogued. Every path mapped. Every card collected."
            },
            new IntroScene
            {
                ImageDescription = "The cartographer walking up a path toward a small house, smiling, Atlas under his arm.",
                Text = "At last, his life's work was complete.\nHe was coming home."
            },
            new IntroScene
            {
                ImageDescription = "A woman alone at a window, years of loneliness on her face. A child plays in the background.",
                Text = "His wife had waited. And waited.\nThe joy she once felt had long since turned to something else entirely."
            },
            new IntroScene
            {
                ImageDescription = "The woman kneeling by the stream, tearing pages, letting them fall into the water.",
                Text = "She carried the Atlas to the stream behind their home.\nPage by page, she tore his life's work apart and let the current take it."
            },
            new IntroScene
            {
                ImageDescription = "The cartographer standing at the stream's edge, hand on his chest, pages floating away.",
                Text = "When the cartographer saw what she had done,\nhis heart gave out. He collapsed at the water's edge\nand did not rise again."
            },
            new IntroScene
            {
                ImageDescription = "The woman clutching a small boy, walking toward a town in the distance, the old house behind them.",
                Text = "Consumed by guilt, his wife gathered their son and fled\nto her parents' home in town.\nThe cartographer's house stood empty. Years passed."
            },
            new IntroScene
            {
                ImageDescription = "A young man at a door, a banker in formal clothes holding a document. Mother asleep on a couch.",
                Text = "The boy grew up. Attended the academy.\nKnew nothing of his father's work.\nUntil the morning a banker arrived with a deed\nto a house he didn't remember."
            },
            new IntroScene
            {
                ImageDescription = "The young man in a dusty room, sunlight through a window, holding a leather-bound book.",
                Text = "In the back room of his father's abandoned home, he found it.\nNot the completed Atlas, but something perhaps even more valuable.\nHis father's draft. Annotated. Nearly complete.\nFull of notes only a master cartographer could have written."
            },
            new IntroScene
            {
                ImageDescription = "The Atlas open on a table, glowing faintly, empty spaces where cards should be.",
                Text = "The Arcane Atlas was incomplete. Its pages held maps and notes,\nbut the cards \u2014 the proof that every creature had been faced,\nevery challenge met \u2014 were scattered across the four regions."
            },
            new IntroScene
            {
                ImageDescription = "The young man stepping through a glowing doorway, Atlas in hand, the world opening up before him.",
                Text = "To complete his father's work, he would need to enter\nthe Arcane Realm himself. Collect every card.\nFace every challenge. Verify every page.\n\nAnd perhaps, in doing so, become something\nhis father never quite achieved \u2014 an Ascendant."
            },
        };

        public static readonly string[] TutorialLines = new string[]
        {
            "Ah, the Atlas has found a new keeper.\nI am the Scholar \u2014 the last student of the man who completed the original.",
            "Let me teach you how to survive out there.\nThe creatures of this world settle disputes through card combat.",
            "Cards are placed on a board \u2014 3 in the front row, 2 in the back.\nFront row units fight first.",
            "Each round, you draft cards from a shop.\nSpend your gold wisely \u2014 you only get so much per round.",
            "Buy three copies of the same card to merge them.\nBronze becomes Silver, Silver becomes Gold.\nEach rank makes the card stronger.",
            "Higher tier cards cost more gold, but they deal more HP damage\nto your opponent when they survive combat.\nEvery card is a strategic decision.",
            "Curate your collection carefully.\nOnly the cards you've checked as active will appear in your shop.\nFewer cards means more focused rolls and easier triples.",
            "The Atlas will guide you. Fill its pages,\nand the path forward will reveal itself.\nGood luck, keeper."
        };
    }
}
