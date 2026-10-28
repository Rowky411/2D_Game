using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Gives the player their starter spells: imports the projectile and scroll sprites, creates the
// Spell assets, adds PlayerSpellcaster to the Player prefab and builds the spell bar in the
// open scene's HUD. Menu: Tools > Player > Setup Spells. Safe to re-run; it resets spell stats.
public static class PlayerSpellSetup
{
    const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    const string SpellDir = "Assets/Spells";
    const string ProjectileDir = "Assets/Sprites/NinjaAdventure/FX/Projectiles/";
    const string ScrollDir = "Assets/Sprites/NinjaAdventure/Items/Scroll/";
    const string PanelSprite = "Assets/Sprites/UI/BlankPixel.png";

    [MenuItem("Tools/Player/Setup Spells")]
    public static void Setup()
    {
        // Bread-and-butter ranged attack: cheap, fast, stops at the first mob.
        var fireball = MakeSpell("Fireball", s =>
        {
            s.displayName = "Fireball";
            s.icon = ImportIcon("ScrollFire");
            s.manaCost = 6;
            s.cooldown = 0.45f;
            s.count = 1;
            s.spawnOffset = 0.6f;
            s.speed = 9f;
            s.range = 7f;
            s.hitRadius = 0.35f;
            s.pierce = false;
            s.damage = 2;
            s.knockback = 5f;
            s.frames = ImportStrip("Fireball", 4, 16);
            s.framesPerSecond = 12f;
            s.spriteAngle = 90f;
        });

        // Line clear: slow, big, pierces everything in its path and shoves mobs back hard.
        var frostOrb = MakeSpell("FrostOrb", s =>
        {
            s.displayName = "Frost Orb";
            s.icon = ImportIcon("ScrollIce");
            s.manaCost = 18;
            s.cooldown = 4f;
            s.count = 1;
            s.spawnOffset = 0.7f;
            s.speed = 4.5f;
            s.range = 8f;
            s.hitRadius = 0.6f;
            s.pierce = true;
            s.damage = 3;
            s.knockback = 9f;
            s.frames = ImportStrip("BigEnergyBall", 4, 20);
            s.framesPerSecond = 8f;
            s.spriteAngle = 90f;
        });

        // Panic button when surrounded: eight thorns burst outward and knock everything away.
        var thornRing = MakeSpell("ThornRing", s =>
        {
            s.displayName = "Thorn Ring";
            s.icon = ImportIcon("ScrollPlant");
            s.manaCost = 14;
            s.cooldown = 6f;
            s.count = 8;
            s.ring = true;
            s.spawnOffset = 0.4f;
            s.speed = 7f;
            s.range = 2.6f;
            s.hitRadius = 0.4f;
            s.pierce = true;
            s.damage = 2;
            s.knockback = 10f;
            s.frames = ImportStrip("PlantSpike", 4, 20);
            s.framesPerSecond = 12f;
            s.spriteAngle = 0f;
        });
        AssetDatabase.SaveAssets();

        var spells = new[] { fireball, frostOrb, thornRing };
        SetupPrefab(spells);
        BuildSpellBar(spells.Length);
        Debug.Log("Spells set up: 1 = Fireball, 2 = Frost Orb, 3 = Thorn Ring.");
    }

    static Spell MakeSpell(string fileName, System.Action<Spell> configure)
    {
        Directory.CreateDirectory(SpellDir);
        string path = $"{SpellDir}/{fileName}.asset";
        var spell = AssetDatabase.LoadAssetAtPath<Spell>(path);
        if (spell == null)
        {
            spell = ScriptableObject.CreateInstance<Spell>();
            AssetDatabase.CreateAsset(spell, path);
        }
        configure(spell);
        EditorUtility.SetDirty(spell);
        return spell;
    }

    // Slices a horizontal strip of `frames` equal cells, left to right.
    static Sprite[] ImportStrip(string name, int frames, float pixelsPerUnit)
    {
        string path = ProjectileDir + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int cellWidth = texture.width / frames;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
#pragma warning disable 618 // spritesheet is the simplest way to slice a fixed grid
        importer.spritesheet = Enumerable.Range(0, frames).Select(i => new SpriteMetaData
        {
            name = $"{name}_{i}",
            rect = new Rect(i * cellWidth, 0, cellWidth, texture.height),
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f),
        }).ToArray();
#pragma warning restore 618
        importer.SaveAndReimport();

        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
    }

    static Sprite ImportIcon(string name)
    {
        string path = ScrollDir + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void SetupPrefab(Spell[] spells)
    {
        var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            var caster = root.GetComponent<PlayerSpellcaster>();
            if (caster == null) caster = root.AddComponent<PlayerSpellcaster>();
            var so = new SerializedObject(caster);
            var property = so.FindProperty("spells");
            property.arraySize = spells.Length;
            for (int i = 0; i < spells.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = spells[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Rebuilds HUD/SpellBar (bottom centre) in the open scene and hooks it to the scene's player.
    static void BuildSpellBar(int slotCount)
    {
        var hud = Object.FindAnyObjectByType<PlayerHUD>(FindObjectsInactive.Include);
        var caster = Object.FindAnyObjectByType<PlayerSpellcaster>(FindObjectsInactive.Include);
        if (hud == null || caster == null)
        {
            Debug.LogWarning("Spell bar not built: open a scene with the HUD and the Player prefab, then re-run.");
            return;
        }

        var old = hud.transform.Find("SpellBar");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        const float slotSize = 104f, gap = 14f;
        var panel = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSprite);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var bar = NewUI("SpellBar", hud.transform);
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, 24f);
        bar.sizeDelta = new Vector2(slotCount * slotSize + (slotCount - 1) * gap, slotSize);

        var slots = new SpellBar.Slot[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            var frame = NewUI($"Slot{i + 1}", bar);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0f, 0.5f);
            frame.anchoredPosition = new Vector2(i * (slotSize + gap), 0f);
            frame.sizeDelta = new Vector2(slotSize, slotSize);
            var slot = new SpellBar.Slot { frame = frame.gameObject.AddComponent<Image>() };
            slot.frame.sprite = panel;
            slot.frame.color = new Color(0.1f, 0.08f, 0.12f, 0.9f);

            var icon = NewUI("Icon", frame);
            Stretch(icon, 12f);
            slot.icon = icon.gameObject.AddComponent<Image>();
            slot.icon.preserveAspect = true;

            var cooldown = NewUI("Cooldown", frame);
            Stretch(cooldown, 3f);
            slot.cooldown = cooldown.gameObject.AddComponent<Image>();
            slot.cooldown.sprite = panel;
            slot.cooldown.color = new Color(0f, 0f, 0f, 0.65f);
            slot.cooldown.type = Image.Type.Filled;
            slot.cooldown.fillMethod = Image.FillMethod.Radial360;
            slot.cooldown.fillOrigin = (int)Image.Origin360.Top;
            slot.cooldown.fillClockwise = false;
            slot.cooldown.fillAmount = 0f;

            slot.key = NewLabel("Key", frame, font, TextAnchor.UpperLeft, Color.white);
            slot.cost = NewLabel("Cost", frame, font, TextAnchor.LowerRight, new Color(0.55f, 0.75f, 1f));
            slots[i] = slot;
        }

        var spellBar = bar.gameObject.AddComponent<SpellBar>();
        var so = new SerializedObject(spellBar);
        so.FindProperty("caster").objectReferenceValue = caster;
        var slotsProperty = so.FindProperty("slots");
        slotsProperty.arraySize = slotCount;
        for (int i = 0; i < slotCount; i++)
        {
            var element = slotsProperty.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("frame").objectReferenceValue = slots[i].frame;
            element.FindPropertyRelative("icon").objectReferenceValue = slots[i].icon;
            element.FindPropertyRelative("cooldown").objectReferenceValue = slots[i].cooldown;
            element.FindPropertyRelative("key").objectReferenceValue = slots[i].key;
            element.FindPropertyRelative("cost").objectReferenceValue = slots[i].cost;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        // Spells fly over water, so the water tilemap needs its own layer.
        var water = GameObject.Find("Water");
        if (water != null && water.GetComponent<UnityEngine.Tilemaps.Tilemap>() != null)
            water.layer = LayerMask.NameToLayer("Water");

        var scene = hud.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
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

    static Text NewLabel(string name, RectTransform parent, Font font, TextAnchor alignment, Color color)
    {
        var rect = NewUI(name, parent);
        Stretch(rect, 5f);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = 22;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);
        return text;
    }
}
