using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

// Builds the "Main Village" scene from the NinjaAdventure tilesets, plus the monster-filled
// wilds south of the village gate (y < 0).
// Menu: Tools > Main Village > Build. Safe to re-run: it rebuilds the village from scratch.
public static class MainVillageBuilder
{
    const string TilesetDir = "Assets/Sprites/NinjaAdventure/Backgrounds/Tilesets/";
    const string TileAssetDir = "Assets/Tiles/MainVillage";
    const string SourceScenePath = "Assets/Scenes/SampleScene.unity";
    const string ScenePath = "Assets/Scenes/MainVillage.unity";
    const string RootName = "MainVillage";
    const int Width = 50;
    const int Height = 40;
    const int Bottom = -32; // the wilds span y = Bottom..-1
    const int Seed = 1234;
    const string MonsterDir = "Assets/Sprites/NinjaAdventure/Actor/Monsters/";
    const string MobPrefabDir = "Assets/Prefabs/Mobs";

    enum Sheet { Floor, House, Nature, Element, Water, FloorDetail }

    static readonly Dictionary<Sheet, string> SheetFiles = new Dictionary<Sheet, string>
    {
        { Sheet.Floor, "TilesetFloor.png" },
        { Sheet.House, "TilesetHouse.png" },
        { Sheet.Nature, "Tileset_Nature.png" },
        { Sheet.Element, "TilesetElement.png" },
        { Sheet.Water, "Tileset_water.png" },
        { Sheet.FloorDetail, "TilesetFloorDetail.png" },
    };

    // A rectangular piece of a tileset: origin is the top-left tile (column, row from the top).
    struct Piece
    {
        public Sheet Sheet; public int Col, Row, W, H, Door;
        public Piece(Sheet sheet, int col, int row, int w, int h, int door = -1)
        { Sheet = sheet; Col = col; Row = row; W = w; H = h; Door = door; }
    }

    // Buildings (door = column offset of the doorway in the bottom row).
    static readonly Piece HouseOrange = new Piece(Sheet.House, 0, 0, 4, 3, 1);
    static readonly Piece HouseBeige = new Piece(Sheet.House, 4, 0, 4, 3, 1);
    static readonly Piece HouseOrange2 = new Piece(Sheet.House, 8, 0, 4, 3, 1);
    static readonly Piece HouseRed = new Piece(Sheet.House, 12, 0, 4, 3, 1);
    static readonly Piece Shop = new Piece(Sheet.House, 16, 0, 3, 3, 1);
    static readonly Piece HouseDark = new Piece(Sheet.House, 19, 0, 4, 3, 2);
    static readonly Piece HouseStone = new Piece(Sheet.House, 23, 0, 3, 3, 1);
    static readonly Piece HouseWood = new Piece(Sheet.House, 26, 0, 3, 3, 1);
    static readonly Piece Dojo = new Piece(Sheet.House, 29, 0, 4, 4, 1);
    static readonly Piece Hut = new Piece(Sheet.House, 0, 7, 3, 3, 1);
    static readonly Piece HutThatch = new Piece(Sheet.House, 3, 7, 3, 3, 1);
    static readonly Piece ToriiGate = new Piece(Sheet.House, 0, 5, 3, 2);

    // Nature.
    static readonly Piece[] SmallTrees =
    {
        new Piece(Sheet.Nature, 0, 0, 2, 2), new Piece(Sheet.Nature, 2, 0, 2, 2),
        new Piece(Sheet.Nature, 8, 0, 2, 2), new Piece(Sheet.Nature, 10, 0, 2, 2),
    };
    static readonly Piece BigPine = new Piece(Sheet.Nature, 0, 2, 4, 3);
    static readonly Piece BigTree = new Piece(Sheet.Nature, 4, 2, 4, 3);
    static readonly Piece BigTree2 = new Piece(Sheet.Nature, 4, 5, 4, 3);
    static readonly Piece Rocks = new Piece(Sheet.Nature, 12, 2, 4, 3);
    static readonly Piece Signpost = new Piece(Sheet.Nature, 17, 0, 1, 1);

    // Props.
    static readonly Piece Lantern = new Piece(Sheet.Element, 6, 3, 1, 2);
    static readonly Piece Bench = new Piece(Sheet.Element, 13, 1, 3, 1);
    static readonly Piece Scarecrow = new Piece(Sheet.Element, 15, 0, 1, 1);
    static readonly Piece HayField = new Piece(Sheet.Element, 13, 4, 3, 3);
    static readonly Piece Cart = new Piece(Sheet.Element, 0, 3, 2, 2);

    // Wilds.
    static readonly Piece Pine = new Piece(Sheet.Nature, 2, 0, 2, 2);
    static readonly Piece DeadTree = new Piece(Sheet.Nature, 4, 0, 2, 2);
    static readonly Piece GnarledTree = new Piece(Sheet.Nature, 6, 0, 2, 2);
    static readonly Piece Stump = new Piece(Sheet.Nature, 12, 0, 2, 2);
    static readonly Piece Boulders = new Piece(Sheet.Nature, 8, 2, 4, 3);
    static readonly Piece BrownRock = new Piece(Sheet.Nature, 16, 3, 2, 2);
    static readonly Piece GreyRock = new Piece(Sheet.Nature, 19, 3, 2, 2);
    static readonly Piece[] Pebbles =
    {
        new Piece(Sheet.Nature, 18, 4, 1, 1), new Piece(Sheet.Nature, 18, 10, 1, 1),
        new Piece(Sheet.Nature, 11, 11, 1, 1), new Piece(Sheet.Nature, 15, 11, 1, 1),
    };
    static readonly Piece Tombstone = new Piece(Sheet.Element, 4, 2, 1, 1);
    static readonly Piece GraveCross = new Piece(Sheet.Element, 3, 2, 1, 1);
    static readonly Piece GravePost = new Piece(Sheet.Element, 5, 2, 1, 1);

    static readonly (Sheet, int, int)[] VillageDetails =
    {
        (Sheet.FloorDetail, 0, 2), (Sheet.FloorDetail, 1, 2), (Sheet.FloorDetail, 2, 2),
        (Sheet.FloorDetail, 3, 2), (Sheet.FloorDetail, 5, 2), (Sheet.FloorDetail, 6, 2),
        (Sheet.Nature, 1, 9), (Sheet.Nature, 2, 10), (Sheet.Nature, 8, 10),
        (Sheet.Nature, 3, 9), (Sheet.Nature, 6, 9), (Sheet.Nature, 7, 9),
    };
    // Grass tufts, fallen leaves, twigs, skulls and bones.
    static readonly (Sheet, int, int)[] WildDetails =
    {
        (Sheet.FloorDetail, 0, 2), (Sheet.FloorDetail, 2, 2), (Sheet.FloorDetail, 3, 2),
        (Sheet.FloorDetail, 0, 0), (Sheet.FloorDetail, 1, 0), (Sheet.FloorDetail, 5, 0),
        (Sheet.FloorDetail, 6, 0), (Sheet.FloorDetail, 7, 0), (Sheet.FloorDetail, 13, 0),
        (Sheet.FloorDetail, 14, 0), (Sheet.Nature, 0, 10), (Sheet.Nature, 1, 11),
        (Sheet.Nature, 3, 11), (Sheet.Nature, 6, 9), (Sheet.Nature, 0, 12),
    };

    // Monsters: a 4x4 sprite sheet (walk frames x directions) plus stats.
    struct MobType
    {
        public string Name, SheetPath; public int Health, Damage, Exp; public float ChaseSpeed, AggroRange;
        public MobType(string name, string sheetPath, int health, int damage, int exp, float chaseSpeed, float aggroRange)
        {
            Name = name; SheetPath = sheetPath; Health = health; Damage = damage; Exp = exp;
            ChaseSpeed = chaseSpeed; AggroRange = aggroRange;
        }
    }

    static readonly MobType SlimeMob = new MobType("Slime", "Slime/Slime.png", 2, 1, 10, 1.8f, 3.5f);
    static readonly MobType MushroomMob = new MobType("Mushroom", "Mushroom/mushroom.png", 3, 1, 15, 2f, 4f);
    static readonly MobType SnakeMob = new MobType("Snake", "Snake/Snake.png", 4, 2, 25, 2.6f, 5f);

    // Per-cell flags for the whole map; y may be negative (the wilds).
    class Mask
    {
        readonly bool[,] cells = new bool[Width, Height - Bottom];
        public bool this[int x, int y] { get => cells[x, y - Bottom]; set => cells[x, y - Bottom] = value; }
    }

    static Dictionary<(Sheet, int, int), Tile> tileCache;
    static Dictionary<Sheet, Dictionary<string, Sprite>> spriteCache;

    static Tilemap ground, path, water, decor, solid, overhead;
    static Mask dirt, occupied, pathContinues;
    static System.Random rng, wildRng;

    [MenuItem("Tools/Main Village/Build")]
    public static void Build()
    {
        EditorSceneManager.SaveOpenScenes();
        foreach (var file in SheetFiles.Values) EnsureSliced(TilesetDir + file);
        LoadSprites();
        OpenOrCreateScene();

        var oldRoot = GameObject.Find(RootName);
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        var root = new GameObject(RootName, typeof(Grid));
        ground = MakeLayer(root.transform, "Ground", -10, false);
        path = MakeLayer(root.transform, "Path", -9, false);
        water = MakeLayer(root.transform, "Water", -9, true);
        decor = MakeLayer(root.transform, "Decor", -8, false);
        solid = MakeLayer(root.transform, "Solid", -5, true);
        overhead = MakeLayer(root.transform, "Overhead", 10, false);
        AddMapBounds(root);

        dirt = new Mask();
        occupied = new Mask();
        pathContinues = new Mask();
        rng = new System.Random(Seed);
        wildRng = new System.Random(Seed + 1);

        LayOut();
        LayOutWilds();
        PaintGrass(0, Height, rng, 0.75);
        PaintGrass(Bottom, 0, wildRng, 0.55);
        PaintPaths();
        ScatterDecor(4, Height - 4, rng, VillageDetails, 0.08);
        ScatterDecor(Bottom + 4, 0, wildRng, WildDetails, 0.12);
        AddSpawners(root);

        PlacePlayerAndCamera();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        AddSceneToBuildSettings();
        Debug.Log("Main Village built.");
    }

    // ---------------------------------------------------------------- Layout

    static void LayOut()
    {
        // Roads and plaza (x0, y0, x1, y1 inclusive, y up).
        FillDirt(23, 0, 25, 31);   // main road: south entrance -> dojo
        FillDirt(5, 21, 44, 23);   // upper road
        FillDirt(5, 9, 44, 11);    // lower road
        FillDirt(18, 17, 30, 26);  // plaza

        // Houses along the upper road (bottom row at y = 25, doors face the road).
        House(HouseOrange, 6, 25);
        House(HouseRed, 12, 25);
        House(HouseBeige, 33, 25);
        House(HouseDark, 39, 25);

        // Plaza shops and the dojo at the north end of the main road.
        House(Shop, 19, 27);
        House(HouseStone, 27, 27);
        House(Dojo, 22, 32);

        // Houses along the lower road.
        House(HouseOrange2, 6, 13);
        House(HouseWood, 12, 13);
        House(Hut, 33, 13);
        House(HutThatch, 39, 13);

        // Village entrance.
        Gate(ToriiGate, 23, 5);
        Place(Signpost, 21, 6, overheadRows: 0);

        // Plaza props.
        Place(Lantern, 17, 17, overheadRows: 1);
        Place(Lantern, 31, 17, overheadRows: 1);
        Place(Lantern, 17, 28, overheadRows: 1);
        Place(Lantern, 31, 28, overheadRows: 1);
        Place(Bench, 19, 18, overheadRows: 0);
        Place(Bench, 27, 18, overheadRows: 0);

        // Gardens between the two rows of houses.
        Place(BigTree, 6, 19, overheadRows: 2);
        Place(BigTree2, 39, 19, overheadRows: 2);
        Place(SmallTrees[0], 13, 19, overheadRows: 1);
        Place(SmallTrees[2], 34, 19, overheadRows: 1);
        Place(Cart, 15, 16, overheadRows: 1);

        // South side: pond, farm and rocks.
        Pond(6, 4, 12, 7);
        Place(HayField, 31, 7, overheadRows: 0, layer: decor);
        Place(HayField, 34, 7, overheadRows: 0, layer: decor);
        Place(Scarecrow, 37, 6, overheadRows: 0);
        Place(Rocks, 40, 8, overheadRows: 1);
        Place(BigPine, 15, 7, overheadRows: 2);

        // Forest around the whole village, with a gap for the entrance road.
        for (int x = 0; x < Width; x += 2)
        for (int y = 0; y < Height; y += 2)
        {
            bool border = x < 4 || x >= Width - 4 || y < 4 || y >= Height - 4;
            bool entrance = y < 4 && x >= 22 && x <= 26;
            if (!border || entrance) continue;
            var tree = SmallTrees[rng.Next(SmallTrees.Length)];
            Place(tree, x, y + 1, overheadRows: 1);
        }
    }

    // The wilds south of the gate: a trail into a central clearing, with side trails to a meadow
    // in the west and a rocky ridge in the east. Uses its own RNG so the village layout is unchanged.
    static void LayOutWilds()
    {
        FillDirt(23, -9, 25, -1);    // trail from the gate
        FillDirt(17, -20, 31, -10);  // central clearing
        FillDirt(20, -23, 28, -8);
        FillDirt(8, -16, 16, -14);   // west trail and meadow clearing
        FillDirt(5, -19, 11, -11);
        FillDirt(32, -16, 40, -14);  // east trail and ridge clearing
        FillDirt(37, -20, 43, -12);

        Place(Signpost, 22, -6, overheadRows: 0);

        // Landmarks.
        Place(BigPine, 6, -5, overheadRows: 2);
        Place(BigTree2, 13, -24, overheadRows: 2);
        Place(Boulders, 36, -5, overheadRows: 1);
        Place(Rocks, 40, -22, overheadRows: 1);
        Pond(5, -27, 10, -24);

        // Graveyard south of the clearing.
        var graves = new[] { Tombstone, GraveCross, Tombstone, GravePost, GraveCross };
        for (int x = 20, i = 0; x <= 28; x += 2, i++)
        {
            Place(graves[i % graves.Length], x, -25, overheadRows: 0);
            Place(graves[(i + 2) % graves.Length], x + 1, -27, overheadRows: 0);
        }

        // Forest wall between the village and the wilds (gap for the trail) and around the edges.
        var borderTrees = new[] { Pine, Pine, DeadTree, GnarledTree, SmallTrees[0] };
        for (int x = 0; x < Width; x += 2)
        for (int y = Bottom; y < 0; y += 2)
        {
            bool border = x < 4 || x >= Width - 4 || y < Bottom + 4 || y >= -4;
            bool trail = y >= -4 && x >= 22 && x <= 26;
            if (!border || trail) continue;
            Place(borderTrees[wildRng.Next(borderTrees.Length)], x, y + 1, overheadRows: 1);
        }

        // Scattered trees and rocks, kept off the trails so every clearing stays reachable.
        ScatterObstacles(new[] { Pine, DeadTree, GnarledTree, SmallTrees[0], Stump }, 22, overheadRows: 1);
        ScatterObstacles(new[] { BrownRock, GreyRock }, 8, overheadRows: 1);
        ScatterObstacles(Pebbles, 14, overheadRows: 0);
    }

    // Places `count` random pieces on free grass in the wilds, with a one-tile gap around each.
    static void ScatterObstacles(Piece[] pieces, int count, int overheadRows)
    {
        for (int placed = 0, attempt = 0; placed < count && attempt < count * 50; attempt++)
        {
            var piece = pieces[wildRng.Next(pieces.Length)];
            int x = wildRng.Next(4, Width - 4 - piece.W + 1);
            int topY = wildRng.Next(Bottom + 4 + piece.H - 1, -4);
            if (!IsFree(x - 1, topY + 1, piece.W + 2, piece.H + 2)) continue;
            Place(piece, x, topY, overheadRows);
            placed++;
        }
    }

    // True when no cell of the w x h block with top-left (x, topY) is occupied or dirt.
    static bool IsFree(int x, int topY, int w, int h)
    {
        for (int j = 0; j < h; j++)
        for (int i = 0; i < w; i++)
        {
            int cx = x + i, cy = topY - j;
            if (!InBounds(cx, cy) || occupied[cx, cy] || dirt[cx, cy]) return false;
        }
        return true;
    }

    // Places a building with its bottom-left tile at (x, bottomY) and a short path from its door to the road.
    static void House(Piece house, int x, int bottomY)
    {
        Place(house, x, bottomY + house.H - 1, overheadRows: 1);
        int doorX = x + house.Door;
        // The path runs into the doorway, so auto-tiling treats the door tiles as path.
        pathContinues[doorX, bottomY] = true;
        pathContinues[doorX + 1, bottomY] = true;
        for (int y = bottomY - 1; y >= Bottom && !dirt[doorX, y]; y--)
        {
            dirt[doorX, y] = true;
            dirt[doorX + 1, y] = true;
        }
    }

    // A gate with its top-left tile at (x, topY): the beam is drawn over the player, the posts
    // (bottom row, outer columns) are solid, and the opening between them stays walkable with
    // its threshold drawn under the player.
    static void Gate(Piece gate, int x, int topY)
    {
        Place(gate, x, topY, overheadRows: gate.H - 1);
        int bottomY = topY - gate.H + 1;
        for (int i = 1; i < gate.W - 1; i++)
        {
            var cell = new Vector3Int(x + i, bottomY, 0);
            decor.SetTile(cell, solid.GetTile(cell));
            solid.SetTile(cell, null);
        }
    }

    // Places a piece with its top-left tile at (x, topY). The top rows go on the overhead layer
    // (drawn above the player, no collision); the rest on `layer` (solid by default).
    static void Place(Piece piece, int x, int topY, int overheadRows, Tilemap layer = null)
    {
        layer = layer ?? solid;
        for (int j = 0; j < piece.H; j++)
        for (int i = 0; i < piece.W; i++)
        {
            int cx = x + i, cy = topY - j;
            if (!InBounds(cx, cy)) continue;
            var tile = GetTile(piece.Sheet, piece.Col + i, piece.Row + j);
            if (tile == null) continue;
            (j < overheadRows ? overhead : layer).SetTile(new Vector3Int(cx, cy, 0), tile);
            occupied[cx, cy] = true;
        }
    }

    static void Pond(int x0, int y0, int x1, int y1)
    {
        for (int x = x0; x <= x1; x++)
        for (int y = y0; y <= y1; y++)
        {
            int col = x == x0 ? 0 : x == x1 ? 2 : 1;
            int row = y == y1 ? 0 : y == y0 ? 2 : 1;
            water.SetTile(new Vector3Int(x, y, 0), GetTile(Sheet.Water, col, row));
            occupied[x, y] = true;
        }
        decor.SetTile(new Vector3Int(x0 + 2, y0 + 2, 0), GetTile(Sheet.Water, 11, 3)); // lily pad
    }

    static void FillDirt(int x0, int y0, int x1, int y1)
    {
        for (int x = x0; x <= x1; x++)
        for (int y = y0; y <= y1; y++)
            dirt[x, y] = true;
    }

    // ---------------------------------------------------------------- Painting

    // Paints rows y0..y1-1; plainChance is the share of plain (undecorated) grass tiles.
    static void PaintGrass(int y0, int y1, System.Random random, double plainChance)
    {
        var variants = new[] { (3, 11), (1, 12), (2, 12), (3, 12), (4, 12), (2, 11) };
        for (int x = 0; x < Width; x++)
        for (int y = y0; y < y1; y++)
        {
            var (c, r) = random.NextDouble() < plainChance ? (0, 12) : variants[random.Next(variants.Length)];
            ground.SetTile(new Vector3Int(x, y, 0), GetTile(Sheet.Floor, c, r));
        }
    }

    // Auto-tiles the dirt mask with the 3x3 dirt-on-grass set (cols 0-2, rows 7-9) plus inner corners.
    static void PaintPaths()
    {
        for (int x = 0; x < Width; x++)
        for (int y = Bottom; y < Height; y++)
        {
            if (!dirt[x, y]) continue;
            bool n = IsDirt(x, y + 1), s = IsDirt(x, y - 1), w = IsDirt(x - 1, y), e = IsDirt(x + 1, y);
            (int, int) t;
            if (!n && !w) t = (0, 7);
            else if (!n && !e) t = (2, 7);
            else if (!s && !w) t = (0, 9);
            else if (!s && !e) t = (2, 9);
            else if (!n) t = (1, 7);
            else if (!s) t = (1, 9);
            else if (!w) t = (0, 8);
            else if (!e) t = (2, 8);
            else if (!IsDirt(x - 1, y + 1)) t = (6, 9);
            else if (!IsDirt(x + 1, y + 1)) t = (5, 9);
            else if (!IsDirt(x - 1, y - 1)) t = (6, 8);
            else if (!IsDirt(x + 1, y - 1)) t = (5, 8);
            else t = (1, 8);
            path.SetTile(new Vector3Int(x, y, 0), GetTile(Sheet.Floor, t.Item1, t.Item2));
        }
    }

    static void ScatterDecor(int y0, int y1, System.Random random, (Sheet, int, int)[] details, double chance)
    {
        for (int x = 4; x < Width - 4; x++)
        for (int y = y0; y < y1; y++)
        {
            if (dirt[x, y] || occupied[x, y] || random.NextDouble() > chance) continue;
            var (sheet, c, r) = details[random.Next(details.Length)];
            decor.SetTile(new Vector3Int(x, y, 0), GetTile(sheet, c, r));
        }
    }

    // Roads continue past the map edge and into doorways.
    static bool IsDirt(int x, int y) => !InBounds(x, y) || dirt[x, y] || pathContinues[x, y];
    static bool InBounds(int x, int y) => x >= 0 && y >= Bottom && x < Width && y < Height;

    // ---------------------------------------------------------------- Scene setup

    static void OpenOrCreateScene()
    {
        if (!File.Exists(ScenePath)) AssetDatabase.CopyAsset(SourceScenePath, ScenePath);
        EditorSceneManager.OpenScene(ScenePath);
    }

    static Tilemap MakeLayer(Transform parent, string name, int sortingOrder, bool collide)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
        if (collide) go.AddComponent<TilemapCollider2D>();
        return go.GetComponent<Tilemap>();
    }

    // Invisible wall around the map so the player cannot walk out through the entrance gap.
    static void AddMapBounds(GameObject root)
    {
        var edge = root.AddComponent<EdgeCollider2D>();
        edge.points = new[]
        {
            new Vector2(0, Bottom), new Vector2(Width, Bottom), new Vector2(Width, Height),
            new Vector2(0, Height), new Vector2(0, Bottom),
        };
    }

    static void PlacePlayerAndCamera()
    {
        var player = GameObject.Find("Player");
        if (player != null) player.transform.position = new Vector3(24.5f, 20.5f, 0);

        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(24.5f, 20.5f, cam.transform.position.z);
        cam.orthographicSize = 5;
        cam.backgroundColor = new Color(0.27f, 0.42f, 0.2f);
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) return;
        var so = new SerializedObject(follow);
        so.FindProperty("clampToBounds").boolValue = true;
        so.FindProperty("bounds").rectValue = new Rect(0, Bottom, Width, Height - Bottom);
        so.ApplyModifiedProperties();
    }

    // One spawner per clearing in the wilds, each with its own kind of monster.
    static void AddSpawners(GameObject root)
    {
        var parent = new GameObject("MobSpawners").transform;
        parent.SetParent(root.transform, false);
        AddSpawner(parent, "SlimeMeadow", new Vector2(8.5f, -14.5f), new Vector2(9, 10), 5, MakeMobPrefab(SlimeMob));
        AddSpawner(parent, "MushroomClearing", new Vector2(24.5f, -14.5f), new Vector2(14, 11), 5, MakeMobPrefab(MushroomMob));
        AddSpawner(parent, "SnakeRidge", new Vector2(40.5f, -15.5f), new Vector2(8, 9), 4, MakeMobPrefab(SnakeMob));
    }

    static void AddSpawner(Transform parent, string name, Vector2 center, Vector2 size, int maxAlive, Mob prefab)
    {
        var go = new GameObject(name, typeof(MobSpawner));
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var so = new SerializedObject(go.GetComponent<MobSpawner>());
        var prefabs = so.FindProperty("mobPrefabs");
        prefabs.arraySize = 1;
        prefabs.GetArrayElementAtIndex(0).objectReferenceValue = prefab;
        so.FindProperty("areaSize").vector2Value = size;
        so.FindProperty("maxAlive").intValue = maxAlive;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Creates (or overwrites) Assets/Prefabs/Mobs/<name>.prefab from a monster sprite sheet.
    static Mob MakeMobPrefab(MobType type)
    {
        string sheetPath = MonsterDir + type.SheetPath;
        EnsureSliced(sheetPath);

        // Index cells by position: some sheets were sliced by hand with different sprite names.
        var frames = new Sprite[16];
        foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>())
        {
            int col = Mathf.RoundToInt(sprite.rect.x / 16);
            int row = Mathf.RoundToInt((sprite.texture.height - sprite.rect.yMax) / 16);
            if (col < 4 && row < 4) frames[row * 4 + col] = sprite;
        }

        var go = new GameObject(type.Name, typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D));
        go.GetComponent<SpriteRenderer>().sprite = frames[0];
        var body = go.GetComponent<Rigidbody2D>();
        body.gravityScale = 0;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var circle = go.GetComponent<CircleCollider2D>();
        circle.radius = 0.35f;
        circle.offset = new Vector2(0, -0.1f);

        var so = new SerializedObject(go.AddComponent<Mob>());
        var framesProp = so.FindProperty("frames");
        framesProp.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        so.FindProperty("maxHealth").intValue = type.Health;
        so.FindProperty("contactDamage").intValue = type.Damage;
        so.FindProperty("expReward").intValue = type.Exp;
        so.FindProperty("chaseSpeed").floatValue = type.ChaseSpeed;
        so.FindProperty("aggroRange").floatValue = type.AggroRange;
        so.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(MobPrefabDir);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{MobPrefabDir}/{type.Name}.prefab");
        Object.DestroyImmediate(go);
        return prefab.GetComponent<Mob>();
    }

    static void AddSceneToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ---------------------------------------------------------------- Tiles

    // Imports a tileset as pixel art and slices it into a 16x16 grid named "<file>_<col>_<row>" (row 0 = top).
    static void EnsureSliced(string assetPath)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer.spriteImportMode == SpriteImportMode.Multiple && Mathf.Approximately(importer.spritePixelsPerUnit, 16))
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        string baseName = Path.GetFileNameWithoutExtension(assetPath);
        int cols = texture.width / 16, rows = texture.height / 16;
        var rects = new List<SpriteRect>();
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            rects.Add(new SpriteRect
            {
                name = $"{baseName}_{c}_{r}",
                rect = new Rect(c * 16, texture.height - (r + 1) * 16, 16, 16),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate(),
            });
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        provider.SetSpriteRects(rects.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
    }

    static void LoadSprites()
    {
        tileCache = new Dictionary<(Sheet, int, int), Tile>();
        spriteCache = new Dictionary<Sheet, Dictionary<string, Sprite>>();
        foreach (var pair in SheetFiles)
        {
            spriteCache[pair.Key] = AssetDatabase.LoadAllAssetsAtPath(TilesetDir + pair.Value)
                .OfType<Sprite>().ToDictionary(s => s.name);
        }
    }

    // Returns (creating on first use) a Tile asset for one tileset cell.
    static Tile GetTile(Sheet sheet, int col, int row)
    {
        if (tileCache.TryGetValue((sheet, col, row), out var cached)) return cached;

        string spriteName = $"{Path.GetFileNameWithoutExtension(SheetFiles[sheet])}_{col}_{row}";
        if (!spriteCache[sheet].TryGetValue(spriteName, out var sprite))
        {
            Debug.LogWarning($"Missing sprite {spriteName}");
            return null;
        }

        Directory.CreateDirectory(TileAssetDir);
        string tilePath = $"{TileAssetDir}/{spriteName}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, tilePath);
        }
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        tileCache[(sheet, col, row)] = tile;
        return tile;
    }
}
