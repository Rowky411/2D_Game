using System.Linq;
using UnityEditor;
using UnityEngine;

// Adds the sword attack to the Player prefab: imports the big sword and slash FX sprites,
// creates the Sword child and wires up PlayerAttack.
// Menu: Tools > Player > Setup Sword Attack. Safe to re-run.
public static class PlayerAttackSetup
{
    const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    const string SwordPath = "Assets/Sprites/NinjaAdventure/Items/Weapons/BigSwordSprite.png";
    const string SlashPath = "Assets/Sprites/NinjaAdventure/FX/SlashFX/SpriteSheet.png";
    const string NinjaSheetPath = "Assets/Sprites/NinjaAdventure/Actor/Characters/GreenNinja/SpriteSheet.png";
    const int AttackPoseRow = 4; // row of the ninja sheet holding the attack poses (down, up, left, right)

    [MenuItem("Tools/Player/Setup Sword Attack")]
    public static void Setup()
    {
        var swordSprite = ImportSword();
        var slashFrames = ImportSlash();
        var attackPoses = LoadCells(NinjaSheetPath, 16, AttackPoseRow, 4);

        var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            var playerRenderer = root.GetComponent<SpriteRenderer>();
            var swordTransform = root.transform.Find("Sword");
            if (swordTransform == null)
            {
                swordTransform = new GameObject("Sword", typeof(SpriteRenderer)).transform;
                swordTransform.SetParent(root.transform, false);
            }
            swordTransform.localPosition = new Vector3(0f, -0.1f, 0f);
            var swordRenderer = swordTransform.GetComponent<SpriteRenderer>();
            swordRenderer.sprite = swordSprite;
            swordRenderer.sharedMaterial = playerRenderer.sharedMaterial;
            swordRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            swordRenderer.enabled = false;

            var attack = root.GetComponent<PlayerAttack>();
            if (attack == null) attack = root.AddComponent<PlayerAttack>();
            var so = new SerializedObject(attack);
            so.FindProperty("sword").objectReferenceValue = swordRenderer;
            SetArray(so.FindProperty("attackPoses"), attackPoses);
            // Frame 0 is a thin slash streak; 1-3 are the cross flash and sparks.
            SetArray(so.FindProperty("hitEffectFrames"), slashFrames.Skip(1).ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log("Sword attack set up on the Player prefab.");
    }

    // Pixel-art sprite with the pivot at the hilt (bottom centre) so it swings around the hand.
    static Sprite ImportSword()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(SwordPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(SwordPath);
    }

    // 4 frames of 32x32, shown at 24 px per unit (slightly bigger than a tile).
    static Sprite[] ImportSlash()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(SlashPath);
        if (importer.spriteImportMode != SpriteImportMode.Multiple || !Mathf.Approximately(importer.spritePixelsPerUnit, 24))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 24;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
#pragma warning disable 618 // spritesheet is the simplest way to slice a fixed grid
            importer.spritesheet = Enumerable.Range(0, 4).Select(i => new SpriteMetaData
            {
                name = $"SlashFX_{i}",
                rect = new Rect(i * 32, 0, 32, 32),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
            }).ToArray();
#pragma warning restore 618
            importer.SaveAndReimport();
        }
        return LoadCells(SlashPath, 32, 0, 4);
    }

    // Sprites of one row of a sliced sheet, by position (row 0 = top), left to right.
    static Sprite[] LoadCells(string path, int cellSize, int row, int count)
    {
        var cells = new Sprite[count];
        foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
        {
            int col = Mathf.RoundToInt(sprite.rect.x / cellSize);
            int r = Mathf.RoundToInt((sprite.texture.height - sprite.rect.yMax) / cellSize);
            if (r == row && col < count) cells[col] = sprite;
        }
        return cells;
    }

    static void SetArray(SerializedProperty property, Sprite[] sprites)
    {
        property.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
