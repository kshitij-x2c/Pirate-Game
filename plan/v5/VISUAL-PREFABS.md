# Visual prefabs (V5, 20 Oct)

Every visual the game draws outside the weapons now comes from a prefab you can edit in the Unity Editor without code,
the way the weapons' visuals do since 13 Oct (plan/hackathon/WEAPON-PREFABS.md). That covers the enemies and bosses
(models, spawn and death effects, health bars, their shots and telegraphs), the player ship (its look per Captain, health
bar, level-up burst, hit highlight, sinking), each Captain's active ring, the pickups and the eight floor powerups, the
points of interest, the shared combat effects (hit sparks, kill bursts, blasts, splashes, damage numbers, item procs), the
storm ring and edge warning, threat arrows, minimap icons, the interaction prompt, and the 1v1's pieces.

118 visuals in 10 categories: 96 new prefabs, and 22 that already were single clean prefabs the game instantiates (the
art team's 15 enemy models, 6 Demo/POI models and the bounds wall). Those 22 are listed in the catalog as they are, not
copied, so you edit them where they are.

| Category | Visuals | Already a prefab (listed) | New: copied from a shared prefab | New: built from what the code built |
|---|---|---|---|---|
| Enemies | 30 | 15 (art team models) | 12 | 3 |
| Bosses | 8 | 0 | 7 | 1 |
| Player | 4 | 0 | 4 | 0 |
| Captains | 12 | 0 | 0 | 12 (6 ship looks, 6 active rings) |
| Pickups | 6 | 0 | 4 | 2 |
| Powerups | 8 | 0 | 0 | 8 |
| POIs | 9 | 6 (Demo/POI) | 2 | 1 |
| Effects | 18 | 0 | 15 | 3 |
| World | 16 | 1 (bounds wall) | 0 | 15 |
| Pvp | 7 | 0 | 1 | 6 |
| **Total** | **118** | **22** | **45** | **51** |

## Where things are

| What | Path |
|---|---|
| Prefabs, one folder per owner | `Assets/_Game/V4/Visuals/Prefabs/<Category>/<id>/<id>_<Visual>.prefab` (plus the materials, meshes the code used to make at run time, beside them) |
| UI sprites the code used to generate (minimap glyphs, arrows) | `Assets/_Game/V4/Visuals/Sprites/Minimap{White,Circle,Diamond,Triangle}.png` |
| The catalog (owner id + visual to prefab) | `Assets/_Game/V4/Visuals/Resources/V4Visuals.asset` (`VisualCatalog`; the game loads it from Resources). One row per visual: category, id, visual, prefab |
| Tuning on each prefab | `VisualTuning` component on the prefab root |
| Builder | **Tools > Rogue Pirates > Visuals > Build Visual Prefabs (missing ones)**. It keeps prefabs that are already on disk, so your edits are safe, and it links any prefab you add as `<Category>/<id>/<id>_<Visual>.prefab`. **Rebuild All Visual Prefabs (discards edits)** remakes every prefab from the current shared look. Code: `Assets/_Game/V4/Editor/VisualPrefabs.cs` |
| What each visual was made from | `VisualSources` (`Assets/_Game/V4/Presentation/Visuals/VisualSources.cs`) |
| Test scenarios | `Assets/_Game/V4/DevTools/Scenarios/Scenarios/visuals_*.asset` |
| Automated test | EditMode `VisualPrefabsTests` |

Each new prefab is a full, unpacked **copy** of what the game drew before: the juice settings' or the VFX profiles' shared
prefab, or, for what the code built at run time (the enemy shot, rings and lines, the floor powerups, the placeholder
cube, damage numbers, HUD arrows, the prompt, minimap glyphs, the 1v1's barrels, ball and opponent ship), the same object
saved with its materials, meshes and sprites as assets. The look is unchanged (see Screenshots), and an edit to one
prefab changes only that visual. The other side: an art change to a shared source prefab (say
`Assets/Prefabs/VFX/02_FishEnemy_DamageVFX.prefab`) no longer reaches the copies. To pick it up, delete the copy and run
Build Visual Prefabs (missing ones).

An empty catalog row, or a prefab you clear, falls back to the old look (the juice settings' prefab or the code-built
object).

### Shared owners and your own per-enemy copies

What the game shared across every enemy or item has one owner prefab, not a copy per enemy: the kill bursts
(`Effects/kill`), hit sparks (`Effects/hit`), the hit highlight (`Effects/damage_highlight`), health bars
(`Enemies/enemy/enemy_HealthBar`), the enemy shot (`Enemies/enemy/enemy_Shot`), the telegraph rings
(`Enemies/enemy/enemy_TelegraphRing`), blasts and splashes. An enemy's own row wins over the shared one for its **Death**
(the kill burst for any tier) and **Highlight**. To give one enemy its own: duplicate the shared prefab into
`Enemies/<id>/<id>_Death.prefab` (or `_Highlight`), edit it, and run Build Visual Prefabs (missing ones), which links it.
It is pooled and prewarmed at the next run.

## How to tweak a visual

1. Open `Assets/_Game/V4/Visuals/Prefabs/<Category>/<id>/` and double-click the prefab, for example
   `Effects/kill/kill_EliteDeath.prefab`.
2. Change what you like: particle systems, materials, meshes, child offsets, line widths and colours, text fonts, or add
   children. The root's pose is set by code (except where the table says not). Child poses are yours.
3. For the numbers the code applies, use **VisualTuning** on the root:

| Field | Default | What it does |
|---|---|---|
| `scale` | 1 | Multiplies the scale the code gives it (an enemy's drawn size, a pickup's scale, an effect's scale, a ring's radius, a number's character size) |
| `lifetime` | 1 | Multiplies how long the code shows it (an effect's seconds or its particles' own length, a number's fade) |
| `spin` | 1 | Multiplies its spin (pickups 180 deg/s, the 1v1 barrels 60 deg/s) |
| `pulse` | 1 | Multiplies the size of its pulse (the buoy's charge ring 4%, the Beacon's ring 8%, the Captain's ring 5%, the storm edge warning 8%, threat arrows 8%, the 1v1 barrels' bob 0.25 m) |
| `pulseSpeed` | 1 | Multiplies the speed of that pulse |
| `width` | 1 | Multiplies line widths (telegraph rings and lanes, the items' ring, the aura pulse, the enemy shot's trail, the boss's marked circle's thickness) |
| `heightOffset` | 0 | Metres added above where the code puts it |
| `overrideColor` / `color` | off | Use this colour instead of the code's (ring and telegraph colours, the boss's marked circle, the buoy's ring, the Captain's ring, an enemy's type colour, the health plus, the 1v1 opponent's tint). The code's alpha fades still apply |
| `tintStrength` | -1 (code's) | How far the code tints the model towards its colour: an enemy's hull 0.6, the 1v1 opponent 0.7 |
| `keepOwnColours` | off | Enemy models: the code does not tint the model with the enemy's type colour |

4. Press Play, or reload a scenario (see below). The presenters read the tuning each time they use it, so a tuning change
   during Play shows on the next use. A prefab change needs the pools to rebuild: reload the scenario or start a new voyage.

## What the code still sets (and why)

The simulation and the run decide these, so they stay in code. The table for each visual lists them in full.

- **Pose**: position, facing and parenting come from the simulation (the enemy, the pickup, the shot, the POI, the
  ship). Enemy models keep V3's spawn variation and the swordfish spring.
- **Sizes from data**: an enemy's size and viewSize, pickup and POI scales from the juice settings, effect scales from
  the juice settings and the VFX profiles, blast radii, the storm's rectangle, telegraph radii and lanes. Tuning `scale`
  multiplies on top.
- **Colours from data**: an enemy's type colour on config models (sails, flags, hull 60%), each telegraph kind's colour,
  the Captain's theme colour, the boss config's marked circle colour, the damage numbers' colours per kind, the 1v1 shot
  colours, the threat arrows' boss and elite colours. Tuning `overrideColor` replaces most of them (see the table).
- **When things show**: the VFX profiles (`Resources/V4VfxProfiles.asset`) still decide whether an enemy's spawn or
  death-extra effect plays, its offset, tint and seconds; the catalog only gives the art. Health bars show over elites,
  minibosses and the boss only.
- **The player ship's model**: it stays the run scene's `CutlassSloop` (its hull carries the ship's collider, which is
  gameplay). Each Captain's `<captain>_ShipLook` is a prefab variant of
  `Assets/_Game/Prefabs/Player/CutlassSloop.prefab` with that Captain's sail and flag; at the run's start its materials go
  on the ship's matching renderers. Edit the variant's materials to restyle a Captain's ship; edit
  `CutlassSloop.prefab` itself to change the ship's shape for everyone.
- **Pools**: every catalog prefab is pooled and prewarmed when the run binds, with the prewarm counts the shared prefabs
  had (health bars 80, hit sparks 40, kill bursts 16, gold barrels 80, XP barrels 112, gems 64, enemy shots 24, blasts 12
  for enemies and 8 for items, splashes 12...). Shared prefabs that were one pool before (the hit, crit, kill and level-up
  bursts were all `02_FishEnemy_DamageVFX`) are separate copies now, so each has its own prewarmed pool. Nothing is made
  mid-run beyond what was before (the 1v1 barrels and balls, the POI markers at layout time, the Captain ring at bind).

### Which owner an effect belongs to

Several effect calls from the simulation carry no id, and the simulation contract was not changed. So:

| Effect | Owner |
|---|---|
| A blast with no keg weapon | `item_blast`: Devil's Match, Queen Anne's Fuse, the Leviathan Heart's strike, the 1v1 bot's kegs |
| An enemy blast (bomb boats, mines, traps, the boss's slam) | `enemy_blast` |
| A splash with no weapon (enemy shots) | `splash` |
| Lightning with no strike weapon equipped (Storm in a Bottle) | `item_strike` (with a Lightning Mast equipped it uses the mast's strike, as before) |
| A timed ring with no weapon (the Leviathan Heart) | `item_ring` |
| An area impact with no shell weapon (the Hammerhead Helm's bow spark) | `item_impact` (with a shell weapon equipped, its impact, as before) |
| An enemy's death | its own `<id>_Death` if you add one, else `kill` (Death, EliteDeath for an elite or an affixed enemy, BossDeath) |

## What stays code-only (and why)

| Visual | Why |
|---|---|
| Enemy burn flash (`EnemyBurnFlash`) and the player's damage flash | Not objects: a timed colour change on the model's own materials. Colours and timing are in code and the juice settings (`playerDamageFlashColor`, `playerDamageFlashDuration`) |
| Elite affix looks, status looks (slow, shock, ink, tar) | The game draws none beyond the elite health bar and the elite kill burst (both prefabs). Ink and net slows are the weapons' pulse rings (weapon prefabs) and the net telegraph ring (`enemy_TelegraphRing`) |
| Enemy name plates | None in the voyage (the threat arrows carry names). The 1v1 opponent's plate is HUD text, part of the match HUD |
| Wakes and trails | Part of each model (its "Wake" and "Foam" children), so they are in the model prefab; the code only switches them with speed |
| The salvage chest's float and glow light | Components the code adds to the chest model (`SalvageChestWaterFloat`, `SalvageChestTreasureGlow`: a point light pulsing). The chest model itself is the Demo/POI prefab |
| Boost and heal effects on the ship, Captain passives | The game draws none (boost is the camera zoom and HUD meter; heals are the health plus pickup) |
| The 1v1 HUD, end screen, name plate and chips; the voyage HUD; the minimap's frame and island snapshot | HUD layout, not world visuals (the art team's HUD prefab and the parchment UI kit already style them) |
| Weapons | Their own catalog since 13 Oct (plan/hackathon/WEAPON-PREFABS.md) |

## No VFX Graph on WebGL

No new prefab uses VFX Graph. Four copies keep the VFX Graph their source already had: the whirlpool spawns of the
Kraken Spawn, the Elite Kraken Spawn and the Reef Crab, and the boss arrival (all `PF_Whirlpool`). As before, devices
without compute shaders (desktop WebGL, phones) play the stand-in instead: now `Effects/vfx_fallback/vfx_fallback_Splash`.
The builder strips missing scripts and V3 scripts; none were found. The art packs' own runtime scripts are kept because
they play the art (the tornado's `TornadoVFX` on the Barnacle King's spawn).

## How to test

- **In a voyage**: V4Boot > Set Sail. Edit a prefab, then start a new voyage or reload a scenario from the Tuning Console
  (F1 > Scenarios).
- **Scenarios** (Tuning Console > Scenarios; each also says which prefabs it shows):
  - `visuals_enemies`: the 14 ordinary and specialist enemies around the ship, no weapons: models, wakes, telegraphs, shots
  - `visuals_elites`: the 9 elite and specialist variants and a shark with each of the 6 elite affixes: models, health bars
  - `visuals_bosses`: the Reef Crab, a Kraken Spawn, an Elite Kraken Spawn and the Barnacle King: spawn whirlpools and tornado, health bars
  - `visuals_pickups`: XP gems, gold coins (they fly to the ship at once), the gold, XP and health barrels, four powerups
  - `visuals_powerups`: the other four powerups (a run holds six at once)
  - `visuals_pois`: all seven points of interest, the chest's float and glow, the buoy's ring, the Beacon's whirlpool
  - `visuals_effects`: weapons and the five proc items among weak sharks, cannonboats, bomb boats and a kraken: hits,
    crits, kill bursts, the elite kill, blasts, splashes, numbers, burn flashes, item procs
  - `visuals_player`: Ember's ship look, level 2 (its level-up burst), health bar and wake
  - `visuals_world`: the ship near the east edge at 14:00: the storm edge warning, the storm ring when on, a threat arrow, minimap icons
  - `visuals_pvp`: a 1v1 against the practice bot: the opponent's ship, its bar, the storm ring, barrels, the bot's
    cannonballs, edge arrows
  - The Captain's active ring shows only with the Captain active HUD on (`hideCaptainActive` off in
    `Assets/_Game/V4/Resources/V4SaltWindHudSettings.asset`; it is on in the demo, so the ring is not drawn today), when
    you press Space; the boss arrival whirlpool when the boss is summoned (Tuning Console > Boss > Summon).
  - Scenarios gained `pickups`, `powerups` and a `NearEdge` ship start for these (dev tools only).
- **Automated**: EditMode `VisualPrefabsTests`. It checks that every visual has its prefab in its owner's folder (or the
  listed art prefab), linked in the catalog the game loads, with a VisualTuning, no missing or V3 script, no new VFX Graph
  and only asset materials; that the copies match their source (renderers, materials, meshes, scales, shown state,
  particle systems); that the code-built ones keep the code's settings (lines, powerups, the enemy shot, the damage number,
  sprites, the boss glyph's halo); that each Captain's ship look gives the ship the same materials as the old sail and flag
  swap; and that the presenters draw the catalog's prefabs (enemy models, pickups, gems, powerups, enemy shots) from pools
  prewarmed before the run, with the tuning applied.
- **Suites** (Editor, 16:9 game view): EditMode 848/848 (841 before plus the 7 new tests), PlayMode 132/134 (the two
  load probes skip without RP_PROBE_SECONDS, as before). Three older tests were adjusted: the two that check the juice
  settings' shared effects run without the visual catalog, and the loot barrel test finds the barrels by their new names.

## Screenshots

`plan/v5/screens/visuals-<group>-before.png` (before this change) and `visuals-<group>-after.png` (after), 1920x1080 in
256 colours, for the ten scenarios, each frozen at the same moment after the load (time scale 0). Pixel check (share of
pixels whose colour changed by more than 24 of 255): before against after is within the noise of two runs of the
**same** build (before against a second before), so nothing changed except animated things (the water, foam, moving
ships and shots, particles, the pulsing storm warning):

| Group | before vs after | noise: before vs before (two runs, old build) |
|---|---|---|
| enemies | 1.53% | 0.96% |
| elites | 1.18% | 1.08% |
| bosses | 2.46% | 2.80% |
| pickups | 0.93% | 1.40% |
| powerups | 0.24% | 1.07% |
| pois | 0.73% | 1.62% |
| effects | 4.00% | 4.77% |
| player | 0.27% | 1.09% |
| world | 1.22% | 1.70% |
| pvp | 1.72% | 2.73% |

The changed pixels sit on the edges of moving ships and shots, the water and foam, particles and the storm warning's
pulse (checked in red-overlay diff images); no model, colour or effect differs.

## Each visual's prefab

Generated by `VisualPrefabs.ListMarkdown()`. "Made from" is what the game drew before this change.

### Enemies

| Owner | Visual | Prefab | Made from | Code sets at run time |
|---|---|---|---|---|
| boarding_barge | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_BoardingBarge.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| bomb_boat | Model | `Assets/_Game/V4/Visuals/Prefabs/Enemies/bomb_boat/bomb_boat_Model.prefab` | copy of `Assets/GameObject/FireBoat.prefab` | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; sails and flags in the enemy's type colour and the hull 60% towards it (tuning: overrideColor, tintStrength; keepOwnColours turns the tint off) |
| bomb_ketch | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_BombKetch.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| cannonboat | Model | `Assets/_Game/V4/Visuals/Prefabs/Enemies/cannonboat/cannonboat_Model.prefab` | copy of `Assets/GameObject/Cannonboat.prefab` | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; sails and flags in the enemy's type colour and the hull 60% towards it (tuning: overrideColor, tintStrength; keepOwnColours turns the tint off) |
| cutthroat_skiff | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_CutthroatSkiff.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| deckhand_dinghy | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_DeckhandDinghy.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| drowned_harrier | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_DrownedHarrier.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| drowned_pursuer | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_DrownedPursuer.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| elite_hammerhead | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_EliteHammerhead.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| elite_kraken_spawn | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_EliteKrakenSpawn.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| elite_kraken_spawn | Spawn | `Assets/_Game/V4/Visuals/Prefabs/Enemies/elite_kraken_spawn/elite_kraken_spawn_Spawn.prefab` | copy of `Assets/_Game/VFX/Whirlpool/PF_Whirlpool.prefab` | where the model appears (on the water), scale = the profile's 0.6 (x tuning scale); the VFX profile still decides whether it plays, its offset, tint and seconds; WebGL plays the vfx_fallback splash instead of a VFX Graph |
| enemy | HealthBar | `Assets/_Game/V4/Visuals/Prefabs/Enemies/enemy/enemy_HealthBar.prefab` | copy of `Assets/Prefabs/VFX/05_EnemyHealthIndicator.prefab` | over elites, minibosses and the boss: above the model's top (at least enemyHealthBarHeight; + tuning heightOffset), its fill = hull / max hull |
| enemy | Placeholder | `Assets/_Game/V4/Visuals/Prefabs/Enemies/enemy/enemy_Placeholder.prefab` | built as the code built it (PlaceholderCube) | an enemy with no model: the enemy's size (radius x 2, x tuning scale), tinted its placeholder colour (magenta when elite) |
| enemy | Shot | `Assets/_Game/V4/Visuals/Prefabs/Enemies/enemy/enemy_Shot.prefab` | built as the code built it (HostileShot) | position (the shot's), size = shot radius x 2 x projectileSizeMultiplier x 1.6 (min 0.45; x tuning scale); trail width 0.8 x size (x tuning width); trail cleared on each new shot; its splash is Effects/splash |
| enemy | TelegraphRing | `Assets/_Game/V4/Visuals/Prefabs/Enemies/enemy/enemy_TelegraphRing.prefab` | built as the code built it (TelegraphRing) | every enemy telegraph: the 40 points of trap, mine, net and warning circles (outline 0.3, filling ring 0.45, x tuning width) and the lanes of charges; colours per kind (trap red, mine amber, net blue, warning red; tuning overrideColor replaces them); alpha from the fuse |
| gunner_cog | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_GunnerCog.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| hammerhead | Model | `Assets/_Game/V4/Visuals/Prefabs/Enemies/hammerhead/hammerhead_Model.prefab` | copy of `Assets/GameObject/HammerHead.prefab` | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; sails and flags in the enemy's type colour and the hull 60% towards it (tuning: overrideColor, tintStrength; keepOwnColours turns the tint off) |
| kraken_spawn | Model | `Assets/_Game/V4/Visuals/Prefabs/Enemies/kraken_spawn/kraken_spawn_Model.prefab` | copy of `Assets/GameObject/Kraken.prefab` | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; sails and flags in the enemy's type colour and the hull 60% towards it (tuning: overrideColor, tintStrength; keepOwnColours turns the tint off) |
| kraken_spawn | Spawn | `Assets/_Game/V4/Visuals/Prefabs/Enemies/kraken_spawn/kraken_spawn_Spawn.prefab` | copy of `Assets/_Game/VFX/Whirlpool/PF_Whirlpool.prefab` | where the model appears (on the water), scale = the profile's 0.6 (x tuning scale); the VFX profile still decides whether it plays, its offset, tint and seconds; WebGL plays the vfx_fallback splash instead of a VFX Graph |
| manta_ray | Model | `Assets/_Game/V4/Visuals/Prefabs/Enemies/manta_ray/manta_ray_Model.prefab` | copy of `Assets/GameObject/MantaRay.prefab` | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; sails and flags in the enemy's type colour and the hull 60% towards it (tuning: overrideColor, tintStrength; keepOwnColours turns the tint off) |
| needle_sloop | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_NeedleSloop.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| net_cutter | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_NetCutter.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| powder_barge | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_PowderBarge.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| ram_raider | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_RamRaider.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| rammer_raft | Model | `Assets/_Game/V4/Visuals/Prefabs/Enemies/rammer_raft/rammer_raft_Model.prefab` | copy of `Assets/GameObject/RamShip.prefab` | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; sails and flags in the enemy's type colour and the hull 60% towards it (tuning: overrideColor, tintStrength; keepOwnColours turns the tint off) |
| repair_tender | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_RepairTender.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| shark | Model | `Assets/_Game/V4/Visuals/Prefabs/Enemies/shark/shark_Model.prefab` | copy of `Assets/GameObject/Shark.prefab` | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; sails and flags in the enemy's type colour and the hull 60% towards it (tuning: overrideColor, tintStrength; keepOwnColours turns the tint off) |
| shield_frigate | Model | `Assets/_Game/V4/Demo/Art/Enemies/Art_ShieldFrigate.prefab` | already a prefab (listed, not copied) | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; keeps its own colours (art team model) |
| swordfish | Model | `Assets/_Game/V4/Visuals/Prefabs/Enemies/swordfish/swordfish_Model.prefab` | copy of `Assets/GameObject/SwordFish.prefab` | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; sails and flags in the enemy's type colour and the hull 60% towards it (tuning: overrideColor, tintStrength; keepOwnColours turns the tint off) |
| treasure_raider | Model | `Assets/_Game/V4/Visuals/Prefabs/Enemies/treasure_raider/treasure_raider_Model.prefab` | copy of `Assets/GameObject/SmuggleBoat.prefab` | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; sails and flags in the enemy's type colour and the hull 60% towards it (tuning: overrideColor, tintStrength; keepOwnColours turns the tint off) |

### Bosses

| Owner | Visual | Prefab | Made from | Code sets at run time |
|---|---|---|---|---|
| barnacle_king | Model | `Assets/_Game/V4/Visuals/Prefabs/Bosses/barnacle_king/barnacle_king_Model.prefab` | copy of `Assets/GameObject/CrabElite.prefab` | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; sails and flags in the enemy's type colour and the hull 60% towards it (tuning: overrideColor, tintStrength; keepOwnColours turns the tint off) |
| barnacle_king | Spawn | `Assets/_Game/V4/Visuals/Prefabs/Bosses/barnacle_king/barnacle_king_Spawn.prefab` | copy of `Assets/TornadoVFX/Tornado_VFX.prefab` | where the model appears (on the water), scale = the profile's 1.5 (x tuning scale); the VFX profile still decides whether it plays, its offset, tint and seconds; WebGL plays the vfx_fallback splash instead of a VFX Graph |
| barnacle_king | DeathExtra | `Assets/_Game/V4/Visuals/Prefabs/Bosses/barnacle_king/barnacle_king_DeathExtra.prefab` | copy of `Assets/Hun0FX/FX/FireFX_vol1/Prefabs/FX_Fire_05.prefab` | where it dies, on top of the kill burst, scale = the profile's 2.5 (x tuning scale); the VFX profile still decides whether it plays |
| boss | Arrival | `Assets/_Game/V4/Visuals/Prefabs/Bosses/boss/boss_Arrival.prefab` | copy of `Assets/_Game/VFX/Whirlpool/PF_Whirlpool.prefab` | where the boss is about to rise, scale bossArrivalScale (x tuning), for bossArrivalSeconds + 2.5 s (x tuning lifetime); WebGL plays the vfx_fallback splash |
| boss | MarkedTelegraph | `Assets/_Game/V4/Visuals/Prefabs/Bosses/boss/boss_MarkedTelegraph.prefab` | built as the code built it (MarkedTelegraph) | the boss's marked bombardment or slam: at the marked point (BossConfig telegraphHeight + tuning heightOffset), radius x 2 wide (x tuning scale), telegraphThickness tall (x tuning width), colour BossConfig telegraphColor (tuning overrideColor replaces it) |
| reef_crab_miniboss | Model | `Assets/_Game/V4/Visuals/Prefabs/Bosses/reef_crab_miniboss/reef_crab_miniboss_Model.prefab` | copy of `Assets/GameObject/Crab.prefab` | position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash; sails and flags in the enemy's type colour and the hull 60% towards it (tuning: overrideColor, tintStrength; keepOwnColours turns the tint off) |
| reef_crab_miniboss | Spawn | `Assets/_Game/V4/Visuals/Prefabs/Bosses/reef_crab_miniboss/reef_crab_miniboss_Spawn.prefab` | copy of `Assets/_Game/VFX/Whirlpool/PF_Whirlpool.prefab` | where the model appears (on the water), scale = the profile's 0.8 (x tuning scale); the VFX profile still decides whether it plays, its offset, tint and seconds; WebGL plays the vfx_fallback splash instead of a VFX Graph |
| reef_crab_miniboss | DeathExtra | `Assets/_Game/V4/Visuals/Prefabs/Bosses/reef_crab_miniboss/reef_crab_miniboss_DeathExtra.prefab` | copy of `Assets/GameObject/FX_Fire_04_2_GREEN.prefab` | where it dies, on top of the kill burst, scale = the profile's 1.5 (x tuning scale); the VFX profile still decides whether it plays |

### Player

| Owner | Visual | Prefab | Made from | Code sets at run time |
|---|---|---|---|---|
| player_ship | HealthBar | `Assets/_Game/V4/Visuals/Prefabs/Player/player_ship/player_ship_HealthBar.prefab` | copy of `Assets/Prefabs/VFX/05_EnemyHealthIndicator.prefab` | over the ship's model at playerHealthBarHeight (+ tuning heightOffset), its fill = hull / max hull |
| player_ship | LevelUp | `Assets/_Game/V4/Visuals/Prefabs/Player/player_ship/player_ship_LevelUp.prefab` | copy of `Assets/Prefabs/VFX/02_FishEnemy_DamageVFX.prefab` | at the ship (0.5 m up) on each level-up, scale levelUpBurstScale; position, scale (the juice settings' scale x tuning scale), particles replayed; back to the pool after its particles' own length (x tuning lifetime) |
| player_ship | Highlight | `Assets/_Game/V4/Visuals/Prefabs/Player/player_ship/player_ship_Highlight.prefab` | copy of `Assets/Prefabs/VFX/06_DamageHighlightShader.prefab` | on the ship (0.5 m up, parented) when it is hit, scale damageHighlightScale (x tuning), for damageHighlightDuration (x tuning lifetime) |
| player_ship | DeathExtra | `Assets/_Game/V4/Visuals/Prefabs/Player/player_ship/player_ship_DeathExtra.prefab` | copy of `Assets/Hun0FX/FX/FireFX_vol1/Prefabs/FX_Fire_05.prefab` | at the ship when it sinks, scale = the profile's 2 (x tuning scale); the VFX profile still decides whether it plays |

### Captains

| Owner | Visual | Prefab | Made from | Code sets at run time |
|---|---|---|---|---|
| brine | ShipLook | `Assets/_Game/V4/Visuals/Prefabs/Captains/brine/brine_ShipLook.prefab` | prefab variant of `Assets/_Game/Prefabs/Player/CutlassSloop.prefab` with the Captain's sail and flag (V4SaltWindHudSettings) | at the run's start its renderers' materials go on the player ship's matching renderers (the ship's model and collider stay the run scene's) |
| brine | ActiveRing | `Assets/_Game/V4/Visuals/Prefabs/Captains/brine/brine_ActiveRing.prefab` | built as the code built it (ActiveRing) | while the Captain's active lasts: a circle of 48 points round the ship, radius 3.6 m (x tuning scale) pulsing 5% (x tuning pulse) at 8 rad/s (x pulseSpeed), 0.25 m up (+ heightOffset), the Captain's theme colour lifted halfway to white (tuning overrideColor replaces it), fading |
| ember | ShipLook | `Assets/_Game/V4/Visuals/Prefabs/Captains/ember/ember_ShipLook.prefab` | prefab variant of `Assets/_Game/Prefabs/Player/CutlassSloop.prefab` with the Captain's sail and flag (V4SaltWindHudSettings) | at the run's start its renderers' materials go on the player ship's matching renderers (the ship's model and collider stay the run scene's) |
| ember | ActiveRing | `Assets/_Game/V4/Visuals/Prefabs/Captains/ember/ember_ActiveRing.prefab` | built as the code built it (ActiveRing) | while the Captain's active lasts: a circle of 48 points round the ship, radius 3.6 m (x tuning scale) pulsing 5% (x tuning pulse) at 8 rad/s (x pulseSpeed), 0.25 m up (+ heightOffset), the Captain's theme colour lifted halfway to white (tuning overrideColor replaces it), fading |
| flint | ShipLook | `Assets/_Game/V4/Visuals/Prefabs/Captains/flint/flint_ShipLook.prefab` | prefab variant of `Assets/_Game/Prefabs/Player/CutlassSloop.prefab` with the Captain's sail and flag (V4SaltWindHudSettings) | at the run's start its renderers' materials go on the player ship's matching renderers (the ship's model and collider stay the run scene's) |
| flint | ActiveRing | `Assets/_Game/V4/Visuals/Prefabs/Captains/flint/flint_ActiveRing.prefab` | built as the code built it (ActiveRing) | while the Captain's active lasts: a circle of 48 points round the ship, radius 3.6 m (x tuning scale) pulsing 5% (x tuning pulse) at 8 rad/s (x pulseSpeed), 0.25 m up (+ heightOffset), the Captain's theme colour lifted halfway to white (tuning overrideColor replaces it), fading |
| richie | ShipLook | `Assets/_Game/V4/Visuals/Prefabs/Captains/richie/richie_ShipLook.prefab` | prefab variant of `Assets/_Game/Prefabs/Player/CutlassSloop.prefab` with the Captain's sail and flag (V4SaltWindHudSettings) | at the run's start its renderers' materials go on the player ship's matching renderers (the ship's model and collider stay the run scene's) |
| richie | ActiveRing | `Assets/_Game/V4/Visuals/Prefabs/Captains/richie/richie_ActiveRing.prefab` | built as the code built it (ActiveRing) | while the Captain's active lasts: a circle of 48 points round the ship, radius 3.6 m (x tuning scale) pulsing 5% (x tuning pulse) at 8 rad/s (x pulseSpeed), 0.25 m up (+ heightOffset), the Captain's theme colour lifted halfway to white (tuning overrideColor replaces it), fading |
| rook | ShipLook | `Assets/_Game/V4/Visuals/Prefabs/Captains/rook/rook_ShipLook.prefab` | prefab variant of `Assets/_Game/Prefabs/Player/CutlassSloop.prefab` with the Captain's sail and flag (V4SaltWindHudSettings) | at the run's start its renderers' materials go on the player ship's matching renderers (the ship's model and collider stay the run scene's) |
| rook | ActiveRing | `Assets/_Game/V4/Visuals/Prefabs/Captains/rook/rook_ActiveRing.prefab` | built as the code built it (ActiveRing) | while the Captain's active lasts: a circle of 48 points round the ship, radius 3.6 m (x tuning scale) pulsing 5% (x tuning pulse) at 8 rad/s (x pulseSpeed), 0.25 m up (+ heightOffset), the Captain's theme colour lifted halfway to white (tuning overrideColor replaces it), fading |
| roxie | ShipLook | `Assets/_Game/V4/Visuals/Prefabs/Captains/roxie/roxie_ShipLook.prefab` | prefab variant of `Assets/_Game/Prefabs/Player/CutlassSloop.prefab` with the Captain's sail and flag (V4SaltWindHudSettings) | at the run's start its renderers' materials go on the player ship's matching renderers (the ship's model and collider stay the run scene's) |
| roxie | ActiveRing | `Assets/_Game/V4/Visuals/Prefabs/Captains/roxie/roxie_ActiveRing.prefab` | built as the code built it (ActiveRing) | while the Captain's active lasts: a circle of 48 points round the ship, radius 3.6 m (x tuning scale) pulsing 5% (x tuning pulse) at 8 rad/s (x pulseSpeed), 0.25 m up (+ heightOffset), the Captain's theme colour lifted halfway to white (tuning overrideColor replaces it), fading |

### Pickups

| Owner | Visual | Prefab | Made from | Code sets at run time |
|---|---|---|---|---|
| gold_barrel | Model | `Assets/_Game/V4/Visuals/Prefabs/Pickups/gold_barrel/gold_barrel_Model.prefab` | copy of `Assets/GameObject/LootBarrel.prefab` | position (floats with barrelSubmergedFraction of its height under water), scale x the juice settings' barrel scale (x tuning scale), squashed to barrelVerticalScale tall; colliders removed; no shadows; sparkles switched off (barrelSparkles) |
| gold_coin | Model | `Assets/_Game/V4/Visuals/Prefabs/Pickups/gold_coin/gold_coin_Model.prefab` | copy of `Assets/Prefabs/VFX/03_GoldCoin.prefab` | position (bobbing, then flying to the ship), scale goldFlyingCoinScale (x tuning scale), spinning 180 deg/s (x tuning spin); its CollectSparkle replayed; colliders removed; no shadows |
| health_barrel | Model | `Assets/_Game/V4/Visuals/Prefabs/Pickups/health_barrel/health_barrel_Model.prefab` | copy of `Assets/GameObject/HealthBarrel.prefab` | position (floats with barrelSubmergedFraction of its height under water), scale x the juice settings' barrel scale (x tuning scale), squashed to barrelVerticalScale tall; colliders removed; no shadows; sparkles switched off (barrelSparkles) |
| health_plus | Model | `Assets/_Game/V4/Visuals/Prefabs/Pickups/health_plus/health_plus_Model.prefab` | built as the code built it (HealthPlus) | flies to the ship from a health barrel: scale healthFlyingPickupScale (x tuning scale), faces the camera, fades in and out over 1.2 s; its material copied once, coloured healthBurstColor (tuning overrideColor replaces it) |
| xp_barrel | Model | `Assets/_Game/V4/Visuals/Prefabs/Pickups/xp_barrel/xp_barrel_Model.prefab` | copy of `Assets/GameObject/GemBarrel.prefab` | position (floats with barrelSubmergedFraction of its height under water), scale x the juice settings' barrel scale (x tuning scale), squashed to barrelVerticalScale tall; colliders removed; no shadows; sparkles switched off (barrelSparkles) |
| xp_gem | Model | `Assets/_Game/V4/Visuals/Prefabs/Pickups/xp_gem/xp_gem_Model.prefab` | copy of `Assets/Prefabs/VFX/04_XPCrystal.prefab` without its colliders (as the code's gem template) | position (bobbing, then flying to the ship), scale xpFlyingGemScale (x tuning scale), spinning 180 deg/s (x tuning spin); its particles replayed; no shadows |

### Powerups

| Owner | Visual | Prefab | Made from | Code sets at run time |
|---|---|---|---|---|
| battle_signal | Model | `Assets/_Game/V4/Visuals/Prefabs/Powerups/battle_signal/battle_signal_Model.prefab` | built as the code built it (Powerup) | position (bobbing on the water, flying to the ship within the magnet radius); its name label faces the camera (WorldBillboardLabel); its collider is kept (as the pooled copies always had) |
| dead_calm | Model | `Assets/_Game/V4/Visuals/Prefabs/Powerups/dead_calm/dead_calm_Model.prefab` | built as the code built it (Powerup) | position (bobbing on the water, flying to the ship within the magnet radius); its name label faces the camera (WorldBillboardLabel); its collider is kept (as the pooled copies always had) |
| full_sail | Model | `Assets/_Game/V4/Visuals/Prefabs/Powerups/full_sail/full_sail_Model.prefab` | built as the code built it (Powerup) | position (bobbing on the water, flying to the ship within the magnet radius); its name label faces the camera (WorldBillboardLabel); its collider is kept (as the pooled copies always had) |
| magnetic_tide | Model | `Assets/_Game/V4/Visuals/Prefabs/Powerups/magnetic_tide/magnetic_tide_Model.prefab` | built as the code built it (Powerup) | position (bobbing on the water, flying to the ship within the magnet radius); its name label faces the camera (WorldBillboardLabel); its collider is kept (as the pooled copies always had) |
| powder_rush | Model | `Assets/_Game/V4/Visuals/Prefabs/Powerups/powder_rush/powder_rush_Model.prefab` | built as the code built it (Powerup) | position (bobbing on the water, flying to the ship within the magnet radius); its name label faces the camera (WorldBillboardLabel); its collider is kept (as the pooled copies always had) |
| repair_supplies | Model | `Assets/_Game/V4/Visuals/Prefabs/Powerups/repair_supplies/repair_supplies_Model.prefab` | built as the code built it (Powerup) | position (bobbing on the water, flying to the ship within the magnet radius); its name label faces the camera (WorldBillboardLabel); its collider is kept (as the pooled copies always had) |
| salvage_chime | Model | `Assets/_Game/V4/Visuals/Prefabs/Powerups/salvage_chime/salvage_chime_Model.prefab` | built as the code built it (Powerup) | position (bobbing on the water, flying to the ship within the magnet radius); its name label faces the camera (WorldBillboardLabel); its collider is kept (as the pooled copies always had) |
| tailwind | Model | `Assets/_Game/V4/Visuals/Prefabs/Powerups/tailwind/tailwind_Model.prefab` | built as the code built it (Powerup) | position (bobbing on the water, flying to the ship within the magnet radius); its name label faces the camera (WorldBillboardLabel); its collider is kept (as the pooled copies always had) |

### POIs

| Owner | Visual | Prefab | Made from | Code sets at run time |
|---|---|---|---|---|
| lighthouse_of_fortune | Model | `Assets/_Game/V4/Demo/POI/POI_LighthouseOfFortune.prefab` | already a prefab (listed, not copied) | position (0.15 m above the POI, + tuning heightOffset), scale x lighthouseScale (x tuning scale); colliders removed; shown, hidden and retired from the stage |
| powder_cache | Model | `Assets/_Game/V4/Demo/POI/POI_PowderCache.prefab` | already a prefab (listed, not copied) | position (0.15 m above the POI, + tuning heightOffset), scale x merchantSkiffScale (x tuning scale); colliders removed; shown, hidden and retired from the stage |
| repair_wreck | Model | `Assets/_Game/V4/Demo/POI/POI_RepairWreck.prefab` | already a prefab (listed, not copied) | position (0.15 m above the POI, + tuning heightOffset), scale x gildedWreckScale (x tuning scale); colliders removed; shown, hidden and retired from the stage |
| salvage_chest | Model | `Assets/_Game/V4/Demo/POI/POI_SalvageChest.prefab` | already a prefab (listed, not copied) | position (0.15 m above the POI, + tuning heightOffset), scale x salvageChestScale (x tuning scale); colliders removed; shown, hidden and retired from the stage; floats on the water and gets the golden glow light (SalvageChestWaterFloat, SalvageChestTreasureGlow); the Powder Cache's Rare Treasure Chest look is this chest |
| signal_buoy | Model | `Assets/_Game/V4/Demo/POI/POI_SignalBuoy.prefab` | already a prefab (listed, not copied) | position (0.15 m above the POI, + tuning heightOffset), scale x trialBuoyScale (x tuning scale); colliders removed; shown, hidden and retired from the stage |
| signal_buoy | ChargeRing | `Assets/_Game/V4/Visuals/Prefabs/POIs/signal_buoy/signal_buoy_ChargeRing.prefab` | built as the code built it (ChargeRing) | while the buoy charges: twice the charge radius wide (x tuning scale), 0.04 tall, pulsing 4% (x tuning pulse) at 6 rad/s (x pulseSpeed); light blue, brighter as it charges (tuning overrideColor replaces it) |
| treasure_chart | Model | `Assets/_Game/V4/Demo/POI/POI_TreasureChart.prefab` | already a prefab (listed, not copied) | position (0.15 m above the POI, + tuning heightOffset), scale x cursedIdolScale (x tuning scale); colliders removed; shown, hidden and retired from the stage |
| treasury_beacon | Model | `Assets/_Game/V4/Visuals/Prefabs/POIs/treasury_beacon/treasury_beacon_Model.prefab` | copy of `Assets/Prefabs/VFX/whirlpool.prefab` | position (0.15 m above the POI, + tuning heightOffset), scale x bossBeaconScale (x tuning scale); colliders removed; shown, hidden and retired from the stage |
| treasury_beacon | TelegraphRing | `Assets/_Game/V4/Visuals/Prefabs/POIs/treasury_beacon/treasury_beacon_TelegraphRing.prefab` | copy of `Assets/GameObject/FireBarrel.prefab` | while the Beacon is telegraphed: bossTelegraphScale wide (x tuning scale), 0.05 tall, pulsing 8% (x tuning pulse) at 24 rad/s (x pulseSpeed) |

### Effects

| Owner | Visual | Prefab | Made from | Code sets at run time |
|---|---|---|---|---|
| aura_pulse | PulseRing | `Assets/_Game/V4/Visuals/Prefabs/Effects/aura_pulse/aura_pulse_PulseRing.prefab` | built as the code built it (AuraPulse) | an aura pulse with no weapon (the 1v1 bot's): 48 points sweeping out to the radius (x tuning scale), gold (tuning overrideColor replaces it), width 0.45 shrinking (x tuning width), 0.5 s (x tuning lifetime) |
| burning_patch | Patch | `Assets/_Game/V4/Visuals/Prefabs/Effects/burning_patch/burning_patch_Patch.prefab` | copy of `Assets/Prefabs/VFX/14_BurningTrail_VFX.prefab` | a burning oil patch: scale burningTrailScale x radius / 2.8 (0.75-1.6); 1.6 s (x tuning lifetime) |
| damage_highlight | Highlight | `Assets/_Game/V4/Visuals/Prefabs/Effects/damage_highlight/damage_highlight_Highlight.prefab` | copy of `Assets/Prefabs/VFX/06_DamageHighlightShader.prefab` | on a hit enemy (0.5 m up, parented to it), scale damageHighlightScale (x tuning), for damageHighlightDuration (x tuning lifetime); an enemy's own <id>_Highlight wins |
| damage_number | Number | `Assets/_Game/V4/Visuals/Prefabs/Effects/damage_number/damage_number_Number.prefab` | built as the code built it (DamageNumber) | each number: its text, font size and character size per kind (hit 60/0.22, crit 72/0.26 gold, the ship's 64/0.24 red, grouped 54/0.19 orange; character size x tuning scale), rising 2.8 m/s, fading over 0.95 s (the ship's 1.05 s; x tuning lifetime), facing the camera |
| enemy_blast | Blast | `Assets/_Game/V4/Visuals/Prefabs/Effects/enemy_blast/enemy_blast_Blast.prefab` | copy of `Assets/GameObject/Guns/VFX/PowderKeg_Explosion_VFX.prefab` | the enemies' blasts (bomb boats, mines, traps, the boss's slams): 0.45 m up, only its 'Impact small', 'Ring' and 'Smoke' children; scale max(1.15, powderKegExplosionScale x 2.35 x blast radius / 2.8 (0.9-1.35)) (x tuning scale); particles restarted; 1.5 s (x tuning lifetime) |
| hit | Hit | `Assets/_Game/V4/Visuals/Prefabs/Effects/hit/hit_Hit.prefab` | copy of `Assets/Prefabs/VFX/02_FishEnemy_DamageVFX.prefab` | hits with no weapon prefab (items, grouped burn ticks): 0.35 m up, scale enemyHitScale; position, scale (the juice settings' scale x tuning scale), particles replayed; back to the pool after its particles' own length (x tuning lifetime) |
| hit | CritHit | `Assets/_Game/V4/Visuals/Prefabs/Effects/hit/hit_CritHit.prefab` | copy of `Assets/Prefabs/VFX/02_FishEnemy_DamageVFX.prefab` | critical hits with no weapon prefab: 0.35 m up, scale critHitScale; position, scale (the juice settings' scale x tuning scale), particles replayed; back to the pool after its particles' own length (x tuning lifetime) |
| hit | CannonImpact | `Assets/_Game/V4/Visuals/Prefabs/Effects/hit/hit_CannonImpact.prefab` | copy of `Assets/GameObject/Guns/VFX/PowderKeg_Explosion_VFX.prefab` | cannon hits with no weapon prefab: 0.45 m up, scale cannonImpactScale (x1.12 on a crit); 0.55 s (x tuning lifetime) |
| item_blast | Blast | `Assets/_Game/V4/Visuals/Prefabs/Effects/item_blast/item_blast_Blast.prefab` | copy of `Assets/GameObject/Guns/VFX/PowderKeg_Explosion_VFX.prefab` | blasts with no keg weapon (Devil's Match, Queen Anne's Fuse, the Leviathan Heart's strike, the 1v1 bot's kegs): 0.45 m up, only its 'Impact small', 'Ring' and 'Smoke' children; scale max(1.15, powderKegExplosionScale x 2.35 x blast radius / 2.8 (0.9-1.35)) (x tuning scale); particles restarted; 1.5 s (x tuning lifetime) |
| item_impact | Impact | `Assets/_Game/V4/Visuals/Prefabs/Effects/item_impact/item_impact_Impact.prefab` | copy of `Assets/GameObject/Guns/VFX/Mortar_Explosion_VFX.prefab` | an area impact with no shell weapon (the Hammerhead Helm's bow spark): 4 bursts spread inside the radius; scale weaponExplosionScale x mortarExplosionScaleMult x radius / 11 (0.18-0.34); 1.05 s |
| item_ring | LandingRing | `Assets/_Game/V4/Visuals/Prefabs/Effects/item_ring/item_ring_LandingRing.prefab` | built as the code built it (ItemRing) | a timed ring with no weapon (the Leviathan Heart's): outline and a ring filling over its seconds; orange (tuning overrideColor replaces it), widths 0.3 and 0.45 (x tuning width) |
| item_strike | Strike | `Assets/_Game/V4/Visuals/Prefabs/Effects/item_strike/item_strike_Strike.prefab` | copy of `Assets/GameObject/Guns/Lightning Mast.prefab` | lightning with no strike weapon equipped (Storm in a Bottle): on the water, scale lightningStrikeScale x radius / 3.5 (0.85-1.35); 1.15 s (x tuning lifetime) |
| kill | Death | `Assets/_Game/V4/Visuals/Prefabs/Effects/kill/kill_Death.prefab` | copy of `Assets/Prefabs/VFX/02_FishEnemy_DamageVFX.prefab` | every ordinary enemy's death (0.25 m up), scale enemyKillScale x the killing weapon's profile scale; position, scale (the juice settings' scale x tuning scale), particles replayed; back to the pool after its particles' own length (x tuning lifetime); an enemy's own <id>_Death prefab wins |
| kill | EliteDeath | `Assets/_Game/V4/Visuals/Prefabs/Effects/kill/kill_EliteDeath.prefab` | copy of `Assets/GameObject/Explosion_Large.prefab` | an elite's death (an elite variant or an affix), scale eliteKillExplosionScale; position, scale (the juice settings' scale x tuning scale), particles replayed; back to the pool after its particles' own length (x tuning lifetime) |
| kill | BossDeath | `Assets/_Game/V4/Visuals/Prefabs/Effects/kill/kill_BossDeath.prefab` | copy of `Assets/GameObject/Explosion_Large.prefab` | the boss's death: bossDeathBurstCount bursts around it (random offsets), scale bossKillExplosionScale x 0.85, 1.0, 1.15...; position, scale (the juice settings' scale x tuning scale), particles replayed; back to the pool after its particles' own length (x tuning lifetime) |
| mortar_reticle | Reticle | `Assets/_Game/V4/Visuals/Prefabs/Effects/mortar_reticle/mortar_reticle_Reticle.prefab` | copy of `Assets/GameObject/Guns/Lightning Mast.prefab` (its Visuals/WaterReticle child) | a mortar telegraph with no landing ring (the 1v1 bot's shells): 0.15 m up, scale mortarTelegraphScale x radius x 2 / 12.5; tinted towards mortarTelegraphColor; 1.22 s |
| splash | Splash | `Assets/_Game/V4/Visuals/Prefabs/Effects/splash/splash_Splash.prefab` | copy of `Assets/Prefabs/VFX/19_WaterImpactSplash.prefab` | a splash with no weapon (enemy shots): 0.35 m up, scale waterImpactSplashScale; 0.55 s; merged with any splash within 1.35 m and 0.14 s |
| vfx_fallback | Splash | `Assets/_Game/V4/Visuals/Prefabs/Effects/vfx_fallback/vfx_fallback_Splash.prefab` | copy of `Assets/Prefabs/VFX/19_WaterImpactSplash.prefab` | on devices without compute shaders (WebGL), in place of a VFX Graph effect (the spawn whirlpools, the boss arrival): scale vfxGraphFallbackScale (x tuning) |

### World

| Owner | Visual | Prefab | Made from | Code sets at run time |
|---|---|---|---|---|
| bounds_wall | Wall | `Assets/_Game/Resources/V3PlayBoundsWallSegment.prefab` | already a prefab (listed, not copied) | one per edge of the play area, faded by distance (hidden since the hackathon: the storm edge warning shows instead) |
| interaction_prompt | Prompt | `Assets/_Game/V4/Visuals/Prefabs/World/interaction_prompt/interaction_prompt_Prompt.prefab` | built as the code built it (Prompt) | its place and size per layout (bottom bar, centred box, the chest's popup), its text and font size per layout, the USE and SKIP buttons' places and listeners |
| minimap_boss | Icon | `Assets/_Game/V4/Visuals/Prefabs/World/minimap_boss/minimap_boss_Icon.prefab` | built as the code built it (MinimapGlyph) | the minimap icon's place (10 times a second), its icon box size (ship 14, POIs 12, boss 20, miniboss 15), the boss's 2.2x scale, the ship's heading; tuning scale multiplies the glyph |
| minimap_boss_beacon | Icon | `Assets/_Game/V4/Visuals/Prefabs/World/minimap_boss_beacon/minimap_boss_beacon_Icon.prefab` | built as the code built it (MinimapGlyph) | the minimap icon's place (10 times a second), its icon box size (ship 14, POIs 12, boss 20, miniboss 15), the boss's 2.2x scale, the ship's heading; tuning scale multiplies the glyph |
| minimap_cursed_idol | Icon | `Assets/_Game/V4/Visuals/Prefabs/World/minimap_cursed_idol/minimap_cursed_idol_Icon.prefab` | built as the code built it (MinimapGlyph) | the minimap icon's place (10 times a second), its icon box size (ship 14, POIs 12, boss 20, miniboss 15), the boss's 2.2x scale, the ship's heading; tuning scale multiplies the glyph |
| minimap_generic_poi | Icon | `Assets/_Game/V4/Visuals/Prefabs/World/minimap_generic_poi/minimap_generic_poi_Icon.prefab` | built as the code built it (MinimapGlyph) | the minimap icon's place (10 times a second), its icon box size (ship 14, POIs 12, boss 20, miniboss 15), the boss's 2.2x scale, the ship's heading; tuning scale multiplies the glyph |
| minimap_gilded_wreck | Icon | `Assets/_Game/V4/Visuals/Prefabs/World/minimap_gilded_wreck/minimap_gilded_wreck_Icon.prefab` | built as the code built it (MinimapGlyph) | the minimap icon's place (10 times a second), its icon box size (ship 14, POIs 12, boss 20, miniboss 15), the boss's 2.2x scale, the ship's heading; tuning scale multiplies the glyph |
| minimap_lighthouse_of_fortune | Icon | `Assets/_Game/V4/Visuals/Prefabs/World/minimap_lighthouse_of_fortune/minimap_lighthouse_of_fortune_Icon.prefab` | built as the code built it (MinimapGlyph) | the minimap icon's place (10 times a second), its icon box size (ship 14, POIs 12, boss 20, miniboss 15), the boss's 2.2x scale, the ship's heading; tuning scale multiplies the glyph |
| minimap_merchant_skiff | Icon | `Assets/_Game/V4/Visuals/Prefabs/World/minimap_merchant_skiff/minimap_merchant_skiff_Icon.prefab` | built as the code built it (MinimapGlyph) | the minimap icon's place (10 times a second), its icon box size (ship 14, POIs 12, boss 20, miniboss 15), the boss's 2.2x scale, the ship's heading; tuning scale multiplies the glyph |
| minimap_miniboss | Icon | `Assets/_Game/V4/Visuals/Prefabs/World/minimap_miniboss/minimap_miniboss_Icon.prefab` | built as the code built it (MinimapGlyph) | the minimap icon's place (10 times a second), its icon box size (ship 14, POIs 12, boss 20, miniboss 15), the boss's 2.2x scale, the ship's heading; tuning scale multiplies the glyph |
| minimap_player | Icon | `Assets/_Game/V4/Visuals/Prefabs/World/minimap_player/minimap_player_Icon.prefab` | built as the code built it (MinimapGlyph) | the minimap icon's place (10 times a second), its icon box size (ship 14, POIs 12, boss 20, miniboss 15), the boss's 2.2x scale, the ship's heading; tuning scale multiplies the glyph |
| minimap_salvage_chest | Icon | `Assets/_Game/V4/Visuals/Prefabs/World/minimap_salvage_chest/minimap_salvage_chest_Icon.prefab` | built as the code built it (MinimapGlyph) | the minimap icon's place (10 times a second), its icon box size (ship 14, POIs 12, boss 20, miniboss 15), the boss's 2.2x scale, the ship's heading; tuning scale multiplies the glyph |
| minimap_trial_buoy | Icon | `Assets/_Game/V4/Visuals/Prefabs/World/minimap_trial_buoy/minimap_trial_buoy_Icon.prefab` | built as the code built it (MinimapGlyph) | the minimap icon's place (10 times a second), its icon box size (ship 14, POIs 12, boss 20, miniboss 15), the boss's 2.2x scale, the ship's heading; tuning scale multiplies the glyph |
| storm_edge | Warning | `Assets/_Game/V4/Visuals/Prefabs/World/storm_edge/storm_edge_Warning.prefab` | built as the code built it (StormEdgeWarning) | shown within 10 m of the play area's edge; its text's alpha 0.55-1 and size 1-1.08 (x tuning pulse) pulse at 8 rad/s (x pulseSpeed) |
| storm_ring | Ring | `Assets/_Game/V4/Visuals/Prefabs/World/storm_ring/storm_ring_Ring.prefab` | built as the code built it (StormRing) | its 4 corners (the storm's safe rectangle), from the storm's start; colour, width and material are the prefab's |
| threat_arrow | Arrow | `Assets/_Game/V4/Visuals/Prefabs/World/threat_arrow/threat_arrow_Arrow.prefab` | built as the code built it (ThreatArrow) | one per off-screen boss, miniboss or scheduled elite: on the safe area's edge, turned towards it, pulsing 8% (x tuning pulse) at 6 rad/s (x pulseSpeed; x tuning scale); red-orange for bosses, gold for elites; its label (name and distance) beside it |

### Pvp

| Owner | Visual | Prefab | Made from | Code sets at run time |
|---|---|---|---|---|
| pvp_arrow | Arrow | `Assets/_Game/V4/Visuals/Prefabs/Pvp/pvp_arrow/pvp_arrow_Arrow.prefab` | built as the code built it (PvpArrow) | one per off-screen opponent or barrel: on the screen's edge, turned towards it, coloured (red foe, gold XP, green health), its label |
| pvp_health_barrel | Barrel | `Assets/_Game/V4/Visuals/Prefabs/Pvp/pvp_health_barrel/pvp_health_barrel_Barrel.prefab` | built as the code built it (PvpBarrel) | position (0.6 m up + heightOffset, bobbing 0.25 m x tuning pulse at 2 rad/s x pulseSpeed), turning 60 deg/s (x tuning spin); made when the barrel appears (as before) |
| pvp_opponent | Ship | `Assets/_Game/V4/Visuals/Prefabs/Pvp/pvp_opponent/pvp_opponent_Ship.prefab` | copy of `Assets/_Game/Prefabs/Player/CutlassSloop.prefab` without scripts, colliders and physics, health bar and aim hidden (as the code's copy of the ship) | the opponent's pose (the relay's); dressed as the player's ship is when the match starts (its Captain's materials, parts shown or hidden); every material tinted 70% (tuning tintStrength) towards red (tuning overrideColor) |
| pvp_opponent | HealthBar | `Assets/_Game/V4/Visuals/Prefabs/Pvp/pvp_opponent/pvp_opponent_HealthBar.prefab` | copy of `Assets/Prefabs/VFX/05_EnemyHealthIndicator.prefab` | over the opponent's ship at enemyHealthBarHeight (+ tuning heightOffset), its fill = its hull |
| pvp_shot | Ball | `Assets/_Game/V4/Visuals/Prefabs/Pvp/pvp_shot/pvp_shot_Ball.prefab` | built as the code built it (PvpBall) | each bot shot: its flight (an arc 1.5 m up + its arc height), its colour on the ball and trail (pale smoke behind a dark ball) |
| pvp_storm_ring | Ring | `Assets/_Game/V4/Visuals/Prefabs/Pvp/pvp_storm_ring/pvp_storm_ring_Ring.prefab` | built as the code built it (PvpRing) | its 128 points: the storm's circle round the arena centre, 0.4 m up (+ tuning heightOffset); colour, width and material are the prefab's |
| pvp_xp_barrel | Barrel | `Assets/_Game/V4/Visuals/Prefabs/Pvp/pvp_xp_barrel/pvp_xp_barrel_Barrel.prefab` | built as the code built it (PvpBarrel) | position (0.6 m up + heightOffset, bobbing 0.25 m x tuning pulse at 2 rad/s x pulseSpeed), turning 60 deg/s (x tuning spin); made when the barrel appears (as before) |

