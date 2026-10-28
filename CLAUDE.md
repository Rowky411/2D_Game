# 2D_Game

Top-down 2D action RPG in Unity using the NinjaAdventure pixel-art pack. The player (Green Ninja)
explores a village with talkable NPCs, then fights monsters in the wilds south of the gate with a
sword and mana-based spells.

- **Unity:** 6000.6.7f1, URP 2D renderer, new Input System (`com.unity.inputsystem`), legacy uGUI `Text` for HUD/UI
- **Main scene:** `Assets/Scenes/MainVillage.unity`
- **Art:** `Assets/Sprites/NinjaAdventure/` (16 px tiles/sprites, PPU 16, Point filter, uncompressed)

## Project layout

| Path | Contents |
|---|---|
| `Assets/Scripts/` | Runtime scripts (flat, one class per file) |
| `Assets/Editor/` | Menu-driven setup/builder scripts (see below) |
| `Assets/Prefabs/` | `Player.prefab`, `Mobs/`, `Npcs/` |
| `Assets/Spells/` | `Spell` ScriptableObject assets (Fireball, FrostOrb, ThornRing) |
| `Assets/Animations/GreenNinja/` | Player animator (8-direction Idle/Walk/Sprint blend trees) |

### Gameplay systems

- **Player:** `PlayerController` (WASD, Shift sprint, faces the mouse, snaps to the nearest of 8 directions),
  `PlayerHealth` (R to revive), `PlayerMana`, `PlayerExperience`, `PlayerAttack` (Space/J/left click
  sword swing toward the aim), `PlayerSpellcaster` (keys 1-3), `PlayerInteractor` (E to talk), `PlayerDebugKeys`.
- **Spells:** data-driven `Spell` assets fired as `SpellProjectile`s; HUD `SpellBar` shows cost/cooldown.
- **Mobs:** `Mob` (wander, chase, contact damage, EXP reward) spawned by `MobSpawner`.
- **NPCs:** `Npc` (wander, talk prompt, dialogue lines) + `DialogueBox` (portrait, typewriter text;
  pauses player movement/attack/spells while open).
- **HUD:** `PlayerHUD` + `StatBar` on the `HUD` canvas in the scene.

### Editor setup tools (menu items, all safe to re-run)

| Menu | Script | Does |
|---|---|---|
| Tools > Main Village > Build | `MainVillageBuilder` | Rebuilds the whole village/wilds scene from tilesets |
| Tools > Player > Setup Sword Attack | `PlayerAttackSetup` | Sword sprite + attack wiring on the Player prefab |
| Tools > Player > Setup Spells | `PlayerSpellSetup` | Spell assets, caster on prefab, HUD spell bar (resets spell stats) |
| Tools > Player > Setup 8-Direction Animation | `PlayerEightDirectionSetup` | Diagonal clips + 8-way blend trees |
| Tools > NPCs > Populate Village | `NpcSetup` | NPC prefabs, cast placement + dialogue, HUD dialogue box |

**Order matters:** `Main Village > Build` recreates the scene, so afterwards re-run `Setup Spells`
(rebuilds the spell bar and the Water layer) and `Populate Village` (re-adds the NPCs).

## Conventions

- Match the existing style: `[SerializeField] private` fields, short header comment on each class
  explaining behaviour, `Tooltip`s on non-obvious fields, events as `Action<...>`.
- Input uses the Input System with manual held/pressed tracking (`isPressed` + a `held` flag) instead of
  `wasPressedThisFrame`, so simulated input is caught too. Keep that pattern.
- Scene/prefab wiring is done by editor scripts using `SerializedObject`, not by hand-editing YAML.
- Sprite sheets are sliced on a 16 px grid by the setup scripts (deprecated `importer.spritesheet` is used deliberately).
- Images (`*.png` etc.) are stored in **Git LFS** (see `.gitattributes`).
- When committing, stage only the files you changed; the user often has unrelated work in progress.

## Tooling available

### Unity CLI

Installed at `C:\Users\Rowky\AppData\Local\Unity\bin\unity` (version 1.0.0-beta.13). Use it to manage
editors, projects, builds, tests, logs and the MCP server; see the `unity:unity-cli` skill.

### MCP servers

- **unity-editor-mcp** — drives the open Unity Editor: `eval` (run C# in the editor), `recompile`,
  `invoke_menu`, `start`/`stop` play mode, `game_view`/`screenshot`, `simulate_key`/`simulate_pointer`,
  scene/hierarchy/asset/prefab/animator tools, `wait`, build tools.
- claude.ai connectors are also attached at the account level (Claude Docs, Gmail, Google Calendar,
  Google Drive, Atlassian, Excalidraw, Hugging Face); they are not used by this project.

**Testing tips with unity-editor-mcp:**
- When the editor isn't focused, play mode doesn't tick: enable `set_run_in_background` (turn it off
  again afterwards) and `set_auto_tick`.
- Simulated keyboard/mouse input does not reach the Input System while the Game view is unfocused
  (`PointersAndKeyboardsRespectGameViewFocus`). Test by calling methods via `eval`
  (e.g. `PlayerSpellcaster.TryCast`, `Npc.Talk`) instead.
- Recompiling during play mode leaves a stale session; stop and restart play mode.
- `game_view` with a `save_path` writes under `Assets/` — delete those test images (and their `.meta`) afterwards.
- Long editor operations (reimports) can exceed `eval`'s default timeout; pass a larger `timeout`.

### Plugins

- **unity** (`unity@anthropic-plugin-directory`) — Unity skills: `unity:unity-cli`, `unity:unity-package-management`,
  `unity:2d-pixel-perfect`, `unity:sprite-editor`, `unity:manage-sprite-atlas`, `unity:sprite-segment-3x3grid`,
  `unity:tilemap-palette-create`, `unity:tilemap-ruletile-createempty`, `unity:ui-ugui`, `unity:ui-uitk`,
  `unity:optimize-text-mesh-pro`, `unity:localization`, `unity:initialize-ai-navigation`,
  `unity:audio-setup-mixers`, `unity:optimize-audio`, `unity:optimize-web`, `unity:urp-postprocessing`,
  `unity:migrate-birp-to-urp`, `unity:shader-graph-create-custom-node`,
  `unity:validate-urp-render-graph-renderer-feature`, `unity:generate-editor-search-query`,
  `unity:new-unity-project`, `unity:build-live-game`, `unity:setup-multiplayer-services`,
  `unity:setup-vivox-voice-chat`, `unity:implement-in-app-purchases`, `unity:levelplay-unity-integration`.
- **caveman** — terse communication mode and helpers: `caveman:caveman`, `caveman:caveman-commit`,
  `caveman:caveman-review`, `caveman:caveman-help`, `caveman:caveman-stats`, `caveman:compress`,
  `caveman:cavecrew` (plus agents `cavecrew-investigator`, `cavecrew-builder`, `cavecrew-reviewer`).
- **frontend-design** — `frontend-design:frontend-design`.
- **andrej-karpathy-skills** — `andrej-karpathy-skills:karpathy-guidelines` (avoid common LLM coding mistakes).

### Project skills (`.claude/skills/`)

Game-design guidance, committed with the repo:

| Skill | Use for |
|---|---|
| `game-design-philosophy` | Core design principles |
| `game-mechanics` | Designing mechanics (combat, abilities, spells) |
| `system-design` | Interconnected systems, crafting, skill trees, combat frameworks |
| `game-balancing` | Numbers, tuning, difficulty curves |
| `game-economy-design` | Currencies, sinks/faucets, shops |
| `level-design` | Layout, pacing, encounter placement |
| `world-building` | Setting, lore, places |
| `narrative-design` | Story structure, quests |
| `dialogue-system` | NPC dialogue, branching, barks, dialogue UI |
| `game-ui-ux` | HUD, menus, UX |
| `game-accessibility` | Colorblind modes, remapping, subtitles, assists |
| `player-psychology` | Motivation, reward loops |
| `playtesting` | Test plans, feedback collection |
| `pixel-art-sprite-animation` | Pixel art and sprite animation |
| `procedural-generation` | Procedural content |
| `game-audio-design` | Sound design and implementation |
| `game-analytics-liveops` | Analytics, live operations |
| `unity-development` | Unity engineering practice |
