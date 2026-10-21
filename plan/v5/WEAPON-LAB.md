# Weapon Lab (22 Oct)

A scene for trying every weapon against a steady stream of one enemy type. It is a real voyage: the same run scene,
stage, ship, Captain and presenters as Set Sail, so weapons, evolutions and the weapon and visual prefabs look and
behave exactly as they do in the game.

## How to open it

1. In the Unity Editor open `Assets/_Game/Scenes/V4/WeaponLab.unity`.
2. Press Play. After a few seconds the voyage starts with the first weapon (Broadside Battery) and the first enemy
   type (Boarding Barge) coming at the ship.

The scene is Editor-only. It is not in the build list, and its code is in the DevTools assembly (Editor and
development builds only).

What the lab changes in the voyage:

- The ship takes no damage from anything: contact, shots, rams, bombs, mines and hazards, boss attacks, the storm or
  edge, and island bumps. The hull stays full and no damage number or hit flash shows on the ship. Every hull loss
  goes through the ship's damage receiver, and the lab keeps that receiver invulnerable every frame. Bumping an island
  still bounces the ship, and nets still slow it. Neither is damage, and taking them out would change gameplay code.
- Kills drop nothing (22 Oct, owner): no XP barrels, gold or coin barrels, health drops, powerup drops (elites and
  boss), or Cursed Doubloons' extra gold. For this run only, the lab clears the kill fan-out's drop and powerup hooks
  (`EnemyWorldModule.KillTargets.SpawnKillDrops`, `RollPowerup` and `WeaponKillGold`). Kills still count. Gold stays
  0, and the Captain never levels up.
- Level-ups give no cards and do not pause.
- No timeline spawns: no ambient bands, hordes, focus phases, overtime or storm. No boss summon either.
- Salvage chest prompts are skipped as they come up, because an item would change the weapon you are testing.
- Everything else is the normal game. The run clock counts, and you sail with WASD or the arrows, Shift to boost and
  Space for all guns.

## Keys

| Key | What it does |
|---|---|
| **I / O** | Previous / next weapon. It cycles through all 19 in the config's order and wraps. Each press gives the ship only that weapon, at level 1 and not evolved. |
| **U** | Level the weapon up by one rank (the console's Rank up), up to its max (12). At max the overlay shows **max**. |
| **P** | Evolve the weapon (the console's Evolve, the same as taking its evolution card). It works at any level. The overlay shows the evolution's name and what the game asks for (max level and the item). A second press says "already evolved". |
| **K / L** | Previous / next enemy type for the stream. The enemies on the sea are removed and the new type starts coming. |
| **[ / ]** | Stream rate down / up (0.25 to 12 per second, 3 to start). |
| **R** | Reset: clears the enemies and puts the weapon back to level 1, not evolved. |
| **H** | Hide or show the key list. |
| F1 | The Tuning Console, as in any voyage. The lab's keys are off while it is open. |

O also toggled the weapon aim overlay in the game. In the lab that toggle is on **V** (or backslash) only.

The overlay at the top left, under the Captain portrait, shows:

- the weapon (name, id, behaviour), its level and max, and its evolution (or the evolution P would give)
- the enemy type (name, id, kind), how many are alive against the cap, and the stream rate
- "Ship takes no damage · kills drop nothing"
- the last action's note and the keys

## The enemy stream

- Enemies come at a random bearing from the normal spawn distance (24 m, `BalanceTargetsConfig.calibration.spawnDistance`),
  just off screen and in open water.
- Up to 30 ordinary enemies alive, 6 elites, 2 minibosses and 1 boss. When they die, more come at the stream rate.
- Bosses and minibosses come at most one every 2 s, and they rise 12 m from the ship so they are on screen.
- Elites spawn with their elite profile, as the Module Lab spawns them.

The 26 types are in this order: the 14 ordinary enemies, the 8 variants, then the 2 elites, the miniboss and the boss.

- **Ordinary:** boarding_barge, bomb_boat, cannonboat, cutthroat_skiff, deckhand_dinghy, drowned_harrier,
  drowned_pursuer, gunner_cog, hammerhead, kraken_spawn, manta_ray, rammer_raft, shark, swordfish
- **Variants:** bomb_ketch, needle_sloop, net_cutter, powder_barge, ram_raider, repair_tender, shield_frigate,
  treasure_raider
- **Elites:** elite_hammerhead, elite_kraken_spawn
- **Miniboss:** reef_crab_miniboss
- **Boss:** barnacle_king

**Skipped: none.** Every archetype and variant id in the config is in the run's enemy database, and each one spawns on
its own. The lab would skip an id that is not in the database, and it would list it in the Editor console line
`[V5 Weapon Lab] ... skipped: ...`.

A Barnacle King from the stream is not the stage's boss encounter. Sinking it does not secure the objective or end the
voyage, and the next one rises.

## Evolutions

All 19 weapons have an evolution in the config, and P evolves each of them. These were checked at max level in play
mode:

broadside_battery > kings_broadside, deck_cannons > stormshot_cannon, flamethrower > hellfire_nozzle,
flying_cutlasses > bladed_tide, harpoon_gun > whalers_volley, lightning_mast > tempest_mast,
long_nine > ship_of_the_line, mortar > siege_bombard, powder_kegs > devils_wake, swivel_guns > swivel_battery,
anchor_flail > kraken_chain, chainshot > bar_shot_barrage, siren_bell > drowned_choir,
squid_ink_pots > black_tide, parrot_squadron > parrot_armada, monkey_gunners > monkey_battery,
maelstrom_charge > abyssal_vortex, lighthouse_lantern > beacon_of_the_deep, cursed_doubloons > pirate_kings_hoard.

The lab does not hand over the evolution's item. That item would also add its own stats and stay with the ship for
the next weapons. If a weapon had no evolution, P would say "no evolution for this weapon in the config".

## Notes

- The first screenshots (cutlasses, parrots, mortar, tempest mast, ink pots, monkeys, anchor flail) were taken before
  drops were switched off, so they show XP barrels and coins piled around the ship. `weapon-lab-no-drops-*.png` show
  the lab as it is now.
- The level shown is the weapon's level. The Captain stays at level 1, because kills drop no XP.
- The voyage uses seed 1 and the saved Captain. The ship starts where the stage puts it.

## For agents

- Code: `Assets/_Game/V4/DevTools/WeaponTesting/WeaponTestLab.cs` (the controller and overlay) and
  `WeaponTestLabBoot.cs` (the scene's one object). The voyage is a Scenario Lab scenario made in code
  (`WeaponTestLab.CreateScenario`), loaded by `Scenario.Load`. Weapons change through `WeaponLab.Solo/RankUp/Evolve`,
  and enemies spawn through `IEnemySpawnSink.Spawn`. The one runtime addition is `ProgressionModule.DevClearEvolution`,
  a dev hook beside `DevEvolve`.
- `yield return WeaponTestLab.Load();` starts the lab from a PlayMode test without the scene.
  `WeaponTestLab.Current` is the running lab. Its methods match the keys (`NextWeapon`, `LevelUp`, `Evolve`,
  `NextEnemy`...).
- Test: PlayMode `V5WeaponLabPlayModeTests` (3 tests). It loads the scene and drives the controller.
  `NoDrops_NoDamage_WhileEnemiesAttack` runs gunner cogs, rammer rafts, bomb boats, elite kraken spawn and the Barnacle
  King against a maxed, evolved flamethrower. It checks that enemy shots were in flight and enemies reached the ship;
  that the hull never changed and no Damaged event fired; and that kills dropped no pickups or powerups and gave no
  gold or XP.
- Screenshots: `plan/v5/screens/weapon-lab-*.png`.
