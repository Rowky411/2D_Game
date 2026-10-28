using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Populates the village with talking NPCs: makes a prefab per character sheet
// (Assets/Prefabs/Npcs), places the cast below with their dialogue, adds PlayerInteractor
// to the Player prefab and builds the dialogue box in the HUD.
// Menu: Tools > NPCs > Populate Village (with MainVillage open). Safe to re-run: the NPCs
// and dialogue box are rebuilt from scratch. Re-run it after Tools > Main Village > Build.
public static class NpcSetup
{
    const string CharacterDir = "Assets/Sprites/NinjaAdventure/Actor/Characters/";
    const string DialogDir = "Assets/Sprites/UI/Dialog/";
    const string NpcPrefabDir = "Assets/Prefabs/Npcs";
    const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

    class Cast
    {
        public string Sheet, Name;
        public Vector2 Position;
        public float Wander;
        public bool Restores;
        public string[] Lines, Repeat;
    }

    // Positions are world coordinates (tile centres) in the MainVillage layout.
    static readonly Cast[] Village =
    {
        new Cast
        {
            Sheet = "OldMan", Name = "Elder Bo", Position = new Vector2(22.5f, 20.5f), Wander = 1f,
            Lines = new[]
            {
                "Ah, a new face! Welcome to our village, young ninja.",
                "Monsters have been creeping out of the wilds south of the gate.",
                "Clear them out and you'll grow stronger - every level makes you tougher and deepens your mana.",
                "The slimes in the western meadow are the weakest. Start there.",
            },
            Repeat = new[]
            {
                "Slimes to the west, mushrooms in the central clearing, snakes up the eastern ridge.",
                "Mind the snakes. They bite harder than they look.",
            },
        },
        new Cast
        {
            Sheet = "Samurai", Name = "Sensei Kaito", Position = new Vector2(25.5f, 30.5f),
            Lines = new[]
            {
                "So you want to fight. Good.",
                "Space, J or a click swings your sword towards where you aim.",
                "Don't stand still. Strike, step back, strike again.",
            },
            Repeat = new[]
            {
                "Steel costs nothing. Spells cost mana. Remember that.",
                "A blade knocks enemies back. Use that space.",
            },
        },
        new Cast
        {
            Sheet = "BlackNinjaMage", Name = "Mage Ren", Position = new Vector2(19.5f, 25.5f), Wander = 0.5f,
            Lines = new[]
            {
                "I sense scrolls of fire, frost and thorn on you.",
                "Press 1, 2 or 3 to cast towards your cursor.",
                "Fireball is cheap and quick. Frost Orb pierces a whole line. Thorn Ring throws back anyone who crowds you.",
                "Your mana returns slowly on its own. Spend it wisely.",
            },
            Repeat = new[]
            {
                "1 for fire, 2 for frost, 3 for thorns.",
                "Out of mana? Fight with steel until it returns.",
            },
        },
        new Cast
        {
            Sheet = "Monk", Name = "Brother Tam", Position = new Vector2(29.5f, 25.5f), Restores = true,
            Lines = new[]
            {
                "You look weary, traveller. Sit a moment.",
                "...There. Body and spirit, restored.",
                "Come back whenever the wilds wear you down.",
            },
            Repeat = new[]
            {
                "Breathe in... and out. You are restored.",
            },
        },
        new Cast
        {
            Sheet = "Inspector", Name = "Inspector Hale", Position = new Vector2(26.5f, 4.5f),
            Lines = new[]
            {
                "Halt! ...Oh, you're heading out? Into the wilds?",
                "Fine. Hold Shift to run if things turn ugly.",
                "And if you fall out there, press R to get back up. Don't ask me how that works.",
            },
            Repeat = new[]
            {
                "Nothing to report. Except monsters. Lots of monsters.",
                "The gate stays open. For now.",
            },
        },
        new Cast
        {
            Sheet = "Woman", Name = "Mira", Position = new Vector2(8.5f, 22.5f), Wander = 1.5f,
            Lines = new[]
            {
                "Lovely day for laundry, if the slimes keep out of it.",
                "Have you met Brother Tam by the stone house? He'll patch you up.",
            },
            Repeat = new[] { "Brother Tam fixed my back in a minute. A minute!" },
        },
        new Cast
        {
            Sheet = "Boy", Name = "Tomo", Position = new Vector2(26.5f, 19.5f), Wander = 3f,
            Lines = new[]
            {
                "Are you a real ninja?! Show me a fireball!",
                "When I grow up I'm going to beat a snake. A big one.",
            },
            Repeat = new[] { "Fireball! Fwoosh! ...I can't do it yet.", "Did you beat a snake yet?" },
        },
        new Cast
        {
            Sheet = "Villager", Name = "Bun", Position = new Vector2(14.5f, 10.5f), Wander = 2f,
            Lines = new[]
            {
                "The shop's been quiet for weeks.",
                "Nobody wants to travel the road with monsters about.",
            },
            Repeat = new[] { "Maybe you could make the road safe again?" },
        },
        new Cast
        {
            Sheet = "Noble", Name = "Lord Ashby", Position = new Vector2(40.5f, 22.5f), Wander = 0.5f,
            Lines = new[] { "Hmph. Mind your boots on my road." },
            Repeat = new[] { "If you must know, I'm waiting for the snakes to apologise." },
        },
        new Cast
        {
            Sheet = "Caveman", Name = "Grug", Position = new Vector2(34.5f, 10.5f), Wander = 1.5f,
            Lines = new[] { "Grug hunt mushroom. Mushroom hunt Grug back.", "Grug not like mushroom." },
            Repeat = new[] { "Mushroom bad." },
        },
        new Cast
        {
            Sheet = "Cavegirl", Name = "Pebble", Position = new Vector2(37.5f, 10.5f), Wander = 1.5f,
            Lines = new[] { "Big brother Grug lost to a mushroom again. Don't tell him I told you." },
            Repeat = new[] { "Grug is strong. Just... not clever." },
        },
        new Cast
        {
            Sheet = "MaskFrog", Name = "Ribbit", Position = new Vector2(13.5f, 5.5f),
            Lines = new[]
            {
                "Ribbit. ...That's not a frog noise. It's my name.",
                "The mask? It keeps the rain out of my eyes.",
            },
            Repeat = new[] { "Ribbit." },
        },
    };

    [MenuItem("Tools/NPCs/Populate Village")]
    public static void Populate()
    {
        var hud = Object.FindAnyObjectByType<PlayerHUD>(FindObjectsInactive.Include);
        var player = Object.FindAnyObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        if (hud == null || player == null)
        {
            Debug.LogWarning("Open the MainVillage scene (with the HUD and Player) and re-run.");
            return;
        }

        var arrow = ImportUiSprite("Arrow", pixelsPerUnit: 16);
        AddInteractorToPlayer();

        var root = GameObject.Find("NPCs");
        if (root != null) Object.DestroyImmediate(root);
        root = new GameObject("NPCs");

        var playerRenderer = player.GetComponent<SpriteRenderer>();
        foreach (var cast in Village)
        {
            var prefab = MakeNpcPrefab(cast.Sheet, arrow, playerRenderer);
            var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            npc.name = cast.Name;
            npc.transform.position = cast.Position;

            var so = new SerializedObject(npc.GetComponent<Npc>());
            so.FindProperty("displayName").stringValue = cast.Name;
            so.FindProperty("wanderRadius").floatValue = cast.Wander;
            so.FindProperty("restoresPlayer").boolValue = cast.Restores;
            SetStrings(so.FindProperty("lines"), cast.Lines);
            SetStrings(so.FindProperty("repeatLines"), cast.Repeat ?? new string[0]);
            so.ApplyModifiedPropertiesWithoutUndo();

            Physics2D.SyncTransforms();
            if (Physics2D.OverlapCircleAll(cast.Position, 0.3f).Any(c => !c.isTrigger && c.gameObject != npc))
                Debug.LogWarning($"{cast.Name} at {cast.Position} overlaps a collider; move them.");
        }

        BuildDialogueBox(hud, player);

        var scene = hud.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Placed {Village.Length} NPCs. Walk up to one and press E to talk.");
    }

    // Assets/Prefabs/Npcs/<Sheet>.prefab: walk frames, portrait, collider and talk prompt.
    // Dialogue is set on the scene instances, so one prefab can play many villagers.
    static GameObject MakeNpcPrefab(string sheet, Sprite arrow, SpriteRenderer playerRenderer)
    {
        string sheetPath = CharacterDir + sheet + "/SpriteSheet.png";
        var frames = SliceWalkFrames(sheetPath);
        var portrait = ImportPortrait(CharacterDir + sheet + "/Faceset.png");

        var go = new GameObject(sheet, typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D));
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = frames[0];
        renderer.sharedMaterial = playerRenderer.sharedMaterial;
        renderer.sortingLayerID = playerRenderer.sortingLayerID;
        renderer.sortingOrder = playerRenderer.sortingOrder;

        var body = go.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        // Heavy, so the player bumps into villagers instead of shoving them around.
        body.mass = 50f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var circle = go.GetComponent<CircleCollider2D>();
        circle.radius = 0.35f;
        circle.offset = new Vector2(0f, -0.1f);

        var prompt = new GameObject("TalkPrompt", typeof(SpriteRenderer));
        prompt.transform.SetParent(go.transform, false);
        prompt.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        prompt.transform.localScale = Vector3.one * 0.6f;
        var promptRenderer = prompt.GetComponent<SpriteRenderer>();
        promptRenderer.sprite = arrow;
        promptRenderer.sharedMaterial = playerRenderer.sharedMaterial;
        promptRenderer.sortingLayerID = playerRenderer.sortingLayerID;
        promptRenderer.sortingOrder = playerRenderer.sortingOrder + 20;
        prompt.SetActive(false);

        var so = new SerializedObject(go.AddComponent<Npc>());
        so.FindProperty("displayName").stringValue = sheet;
        so.FindProperty("portrait").objectReferenceValue = portrait;
        so.FindProperty("talkPrompt").objectReferenceValue = prompt.transform;
        var framesProperty = so.FindProperty("frames");
        framesProperty.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            framesProperty.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(NpcPrefabDir);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{NpcPrefabDir}/{sheet}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // Walk frames from the top 4 rows of a character sheet, row-major (frame x direction).
    // Re-slices the sheet as a 16px grid if any walk cell is missing.
    static Sprite[] SliceWalkFrames(string path)
    {
        var frames = LoadWalkFrames(path);
        if (frames.All(f => f != null)) return frames;

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int columns = texture.width / 16, rows = texture.height / 16;
        string baseName = Path.GetFileNameWithoutExtension(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
#pragma warning disable 618 // spritesheet is the simplest way to slice a fixed grid
        importer.spritesheet = Enumerable.Range(0, rows * columns).Select(i => new SpriteMetaData
        {
            name = $"{baseName}_{i}",
            rect = new Rect(i % columns * 16, texture.height - (i / columns + 1) * 16, 16, 16),
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f),
        }).ToArray();
#pragma warning restore 618
        importer.SaveAndReimport();
        return LoadWalkFrames(path);
    }

    static Sprite[] LoadWalkFrames(string path)
    {
        var frames = new Sprite[16];
        foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
        {
            if (sprite.rect.width != 16 || sprite.rect.height != 16 || !Mathf.Approximately(sprite.pixelsPerUnit, 16f)) continue;
            int column = Mathf.RoundToInt(sprite.rect.x / 16);
            int row = Mathf.RoundToInt((sprite.texture.height - sprite.rect.yMax) / 16);
            if (column < 4 && row < 4) frames[row * 4 + column] = sprite;
        }
        return frames;
    }

    static Sprite ImportPortrait(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.filterMode != FilterMode.Point || importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Sprite ImportUiSprite(string name, float pixelsPerUnit = 100f)
    {
        string path = DialogDir + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void AddInteractorToPlayer()
    {
        var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            if (root.GetComponent<PlayerInteractor>() == null) root.AddComponent<PlayerInteractor>();
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // HUD/Dialogue: the NinjaAdventure dialogue box (x3.5 scale) with a portrait frame on the left.
    static void BuildDialogueBox(PlayerHUD hud, PlayerHealth player)
    {
        var old = hud.transform.Find("Dialogue");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        const float scale = 3.5f;
        var boxSprite = ImportUiSprite("DialogueBoxSimple");
        var frameSprite = ImportUiSprite("FacesetBox");
        var arrowSprite = ImportUiSprite("Arrow", pixelsPerUnit: 16);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var root = NewUI("Dialogue", hud.transform);
        Stretch(root, 0f);

        var panel = NewUI("Panel", root);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 150f);
        panel.sizeDelta = new Vector2(316f, 60f) * scale;
        var box = panel.gameObject.AddComponent<Image>();
        box.sprite = boxSprite;

        float frameSize = 48f * 3f;
        var frame = NewUI("PortraitFrame", panel);
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0f, 0.5f);
        frame.anchoredPosition = new Vector2(36f, 0f);
        frame.sizeDelta = new Vector2(frameSize, frameSize);
        frame.gameObject.AddComponent<Image>().sprite = frameSprite;

        var face = NewUI("Portrait", frame);
        Stretch(face, 5f * 3f);
        var portrait = face.gameObject.AddComponent<Image>();
        portrait.preserveAspect = true;

        float textLeft = 36f + frameSize + 28f;
        var nameLabel = NewText("Name", panel, font, 34, new Color(0.78f, 0.38f, 0.16f), FontStyle.Bold);
        Place(nameLabel.rectTransform, textLeft, 24f, 40f, 60f * scale - 70f);

        var body = NewText("Body", panel, font, 30, new Color(0.16f, 0.12f, 0.16f), FontStyle.Normal);
        Place(body.rectTransform, textLeft, 76f, 56f, 28f);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Truncate;
        body.lineSpacing = 1.1f;

        var arrow = NewUI("Continue", panel);
        arrow.anchorMin = arrow.anchorMax = arrow.pivot = new Vector2(1f, 0f);
        arrow.anchoredPosition = new Vector2(-34f, 26f);
        arrow.sizeDelta = new Vector2(13f, 13f) * 3f;
        arrow.gameObject.AddComponent<Image>().sprite = arrowSprite;

        var dialogue = root.gameObject.AddComponent<DialogueBox>();
        var so = new SerializedObject(dialogue);
        so.FindProperty("panel").objectReferenceValue = panel.gameObject;
        so.FindProperty("portraitFrame").objectReferenceValue = frame.gameObject;
        so.FindProperty("portrait").objectReferenceValue = portrait;
        so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
        so.FindProperty("body").objectReferenceValue = body;
        so.FindProperty("continueArrow").objectReferenceValue = arrow;
        so.FindProperty("playerHealth").objectReferenceValue = player;
        var paused = new Behaviour[]
        {
            player.GetComponent<PlayerController>(),
            player.GetComponent<PlayerAttack>(),
            player.GetComponent<PlayerSpellcaster>(),
        }.Where(b => b != null).ToArray();
        var pausedProperty = so.FindProperty("pausedWhileOpen");
        pausedProperty.arraySize = paused.Length;
        for (int i = 0; i < paused.Length; i++)
            pausedProperty.GetArrayElementAtIndex(i).objectReferenceValue = paused[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
    }

    static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    // Stretches to the parent with insets from each edge.
    static void Place(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    static Text NewText(string name, RectTransform parent, Font font, int size, Color color, FontStyle style)
    {
        var text = NewUI(name, parent).gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
        text.raycastTarget = false;
        return text;
    }

    static void SetStrings(SerializedProperty property, string[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).stringValue = values[i];
    }
}
