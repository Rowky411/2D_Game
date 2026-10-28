using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Upgrades the Green Ninja animator from 4 to 8 facing directions: slices the diagonal sheets
// (WalkDiagonal.png / IdleDiagonal.png, columns: down-left, down-right, up-left, up-right),
// creates diagonal Idle/Walk clips from the Down clips and adds them to the Idle, Walk and
// Sprint blend trees. Menu: Tools > Player > Setup 8-Direction Animation. Safe to re-run.
public static class PlayerEightDirectionSetup
{
    const string AnimDir = "Assets/Animations/GreenNinja";
    const string SpriteDir = "Assets/Sprites/NinjaAdventure/Actor/Characters/GreenNinja/SeparateAnim";
    const string ControllerPath = AnimDir + "/GreenNinja.controller";

    static readonly (string name, int column, Vector2 position)[] Diagonals =
    {
        ("DownLeft", 0, new Vector2(-1f, -1f).normalized),
        ("DownRight", 1, new Vector2(1f, -1f).normalized),
        ("UpLeft", 2, new Vector2(-1f, 1f).normalized),
        ("UpRight", 3, new Vector2(1f, 1f).normalized),
    };

    [MenuItem("Tools/Player/Setup 8-Direction Animation")]
    public static void Setup()
    {
        var walkSheet = SliceLike($"{SpriteDir}/WalkDiagonal.png", $"{SpriteDir}/Walk.png");
        var idleSheet = SliceLike($"{SpriteDir}/IdleDiagonal.png", $"{SpriteDir}/Idle.png");

        var idleClips = Diagonals.Select(d => MakeDiagonalClip("Idle", d.name, d.column, idleSheet)).ToArray();
        var walkClips = Diagonals.Select(d => MakeDiagonalClip("Walk", d.name, d.column, walkSheet)).ToArray();

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        foreach (var state in controller.layers[0].stateMachine.states)
        {
            if (!(state.state.motion is BlendTree tree)) continue;
            // Idle uses the idle clips; Walk and Sprint share the walk clips.
            var down = tree.children.FirstOrDefault(c => c.position == Vector2.down).motion;
            if (down == null) continue;
            AddDiagonals(tree, down.name.StartsWith("Idle") ? idleClips : walkClips);
        }
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Green Ninja animator now has 8 directions.");
    }

    // Slices a 16px grid with the same import settings as the original 4-direction sheet.
    static Sprite[,] SliceLike(string path, string templatePath)
    {
        var template = (TextureImporter)AssetImporter.GetAtPath(templatePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int columns = texture.width / 16, rows = texture.height / 16;
        string baseName = System.IO.Path.GetFileNameWithoutExtension(path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = template.spritePixelsPerUnit;
        importer.filterMode = template.filterMode;
        importer.textureCompression = template.textureCompression;
        importer.mipmapEnabled = false;
#pragma warning disable 618 // spritesheet is the simplest way to slice a fixed grid
        importer.spritesheet = Enumerable.Range(0, rows * columns).Select(i => new SpriteMetaData
        {
            name = $"{baseName}_{i}",
            // Row 0 is the top of the image.
            rect = new Rect(i % columns * 16, texture.height - (i / columns + 1) * 16, 16, 16),
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f),
        }).ToArray();
#pragma warning restore 618
        importer.SaveAndReimport();

        var grid = new Sprite[rows, columns];
        foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
        {
            int column = Mathf.RoundToInt(sprite.rect.x / 16);
            int row = Mathf.RoundToInt((texture.height - sprite.rect.yMax) / 16);
            grid[row, column] = sprite;
        }
        return grid;
    }

    // Copies <kind>Down.anim, swapping every frame for the same row of the diagonal sheet.
    static AnimationClip MakeDiagonalClip(string kind, string direction, int column, Sprite[,] sheet)
    {
        var source = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimDir}/{kind}Down.anim");
        string path = $"{AnimDir}/{kind}{direction}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        clip.frameRate = source.frameRate;
        AnimationUtility.SetAnimationClipSettings(clip, AnimationUtility.GetAnimationClipSettings(source));

        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(source, binding).Select(key =>
            {
                var frame = (Sprite)key.value;
                int row = Mathf.RoundToInt((frame.texture.height - frame.rect.yMax) / 16);
                return new ObjectReferenceKeyframe { time = key.time, value = sheet[row, column] };
            }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        }
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static void AddDiagonals(BlendTree tree, AnimationClip[] clips)
    {
        var straight = tree.children.Where(c => c.position.x == 0f || c.position.y == 0f);
        var diagonal = Diagonals.Select((d, i) => new ChildMotion { motion = clips[i], position = d.position, timeScale = 1f });
        tree.children = straight.Concat(diagonal).ToArray();
        EditorUtility.SetDirty(tree);
    }
}
