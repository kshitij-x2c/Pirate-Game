using System.Collections.Generic;
using System.Linq;
using RoguePirates.V4.Config;
using RoguePirates.V4.Enemies;
using RoguePirates.V4.Foundation.Core;
using RoguePirates.V4.Foundation.Data;
using RoguePirates.V4.Foundation.UI;
using UnityEngine;

namespace RoguePirates.V4.Presentation.Visuals
{
    /// <summary>
    /// V5 (owner: every non-weapon visual as an editable prefab): every visual the game draws outside the weapons, with
    /// what the presenters drew it from before the visual catalog (the juice settings' and the VFX profiles' shared prefabs,
    /// the art team's own prefabs, the objects the code built), resolved exactly as the presenters resolve them with no
    /// catalog. The prefab builder (Editor: VisualPrefabs) copies or builds these into
    /// Assets/_Game/V4/Visuals/Prefabs/&lt;Category&gt;/&lt;id&gt;/, so the look is unchanged; the tests and
    /// plan/v5/VISUAL-PREFABS.md list them. Read only. Like <see cref="Weapons.WeaponVisualSources"/> for the weapons.
    /// </summary>
    public static class VisualSources
    {
        /// <summary>How a visual's prefab is made.</summary>
        public enum Build
        {
            /// <summary>A full, unpacked copy of <see cref="Source.Prefab"/> (or of its <see cref="Source.Child"/>).</summary>
            Copy,
            /// <summary>Already a single clean prefab the game instantiates (the art team's): the catalog points at it.</summary>
            Existing,
            PlaceholderCube, HostileShot, TelegraphRing, MarkedTelegraph, Gem, HealthPlus, Powerup, ChargeRing, ItemRing, AuraPulse,
            DamageNumber, StormRing, StormEdgeWarning, ThreatArrow, Prompt, MinimapGlyph, ShipLook, ActiveRing,
            PvpRing, PvpBarrel, PvpBall, PvpArrow, PvpShip,
        }

        public readonly struct Source
        {
            public readonly VisualCategory Category;
            public readonly string Id, Visual;
            /// <summary>The asset copied (Copy), the existing prefab (Existing), or the base model (ShipLook, PvpShip, Gem).</summary>
            public readonly GameObject Prefab;
            public readonly Build How;
            /// <summary>Copy only: the child path copied instead of the whole prefab.</summary>
            public readonly string Child;
            /// <summary>The builder's argument: a powerup kind, a glyph, a captain key, "xp" for the XP barrel.</summary>
            public readonly string Arg;
            /// <summary>What the code still sets on it at run time.</summary>
            public readonly string Code;

            public Source(VisualCategory category, string id, string visual, GameObject prefab, Build how, string code, string child = null, string arg = null)
            {
                Category = category;
                Id = id;
                Visual = visual;
                Prefab = prefab;
                How = how;
                Code = code;
                Child = child;
                Arg = arg;
            }

            /// <summary>"Copy of ..." / "built as the code built it" / "already a prefab" (the doc's Made from column).</summary>
            public string MadeFrom(System.Func<Object, string> pathOf)
            {
                switch (How)
                {
                    case Build.Copy: return "copy of `" + pathOf(Prefab) + "`" + (Child != null ? " (its " + Child + " child)" : "");
                    case Build.Existing: return "already a prefab (listed, not copied)";
                    case Build.Gem: return "copy of `" + pathOf(Prefab) + "` without its colliders (as the code's gem template)";
                    case Build.ShipLook: return "prefab variant of `" + pathOf(Prefab) + "` with the Captain's sail and flag (V4SaltWindHudSettings)";
                    case Build.PvpShip: return "copy of `" + pathOf(Prefab) + "` without scripts, colliders and physics, health bar and aim hidden (as the code's copy of the ship)";
                    default: return "built as the code built it (" + How + ")";
                }
            }
        }

        public const string PlayerShipModel = "Assets/_Game/Prefabs/Player/CutlassSloop.prefab";

        // ---- What the code still sets (the doc's last column) ---------------------------------------------------------

        const string ModelBase = "position and facing (the simulation's), scale = the enemy's size x viewSize (x tuning scale), V3's spawn variation and swordfish spring; physics and NavMesh removed; its Wake/Foam parts and trails switched with its speed; burn flash";
        const string ModelArt = ModelBase + "; keeps its own colours (art team model)";
        const string ModelTinted = ModelBase + "; sails and flags in the enemy's type colour and the hull 60% towards it (tuning: overrideColor, tintStrength; keepOwnColours turns the tint off)";
        const string FxCode = "position, scale (the juice settings' scale x tuning scale), particles replayed; back to the pool after its particles' own length (x tuning lifetime)";

        /// <summary>Every non-weapon visual and its pre-catalog source (empty without settings).</summary>
        public static List<Source> All(DemoJuiceSettings s, ConfigSetRuntime config, VfxProfileLibrary profiles, Hud.SaltWindHudSettings hud, GameObject playerShip, GameObject boundsWall)
        {
            var list = new List<Source>();
            if (s == null) return list;
            void Copy(VisualCategory c, string id, string visual, GameObject prefab, string code, string child = null)
            {
                if (prefab != null) list.Add(new Source(c, id, visual, prefab, Build.Copy, code, child));
            }
            void Made(VisualCategory c, string id, string visual, Build how, string code, GameObject basis = null, string arg = null) =>
                list.Add(new Source(c, id, visual, basis, how, code, null, arg));

            // ---- Enemies and bosses: their models and their profile moments ------------------------------------------
            if (config != null)
            {
                foreach (var (id, model, category, baseId) in Enemies(config, s))
                {
                    var art = s.EnemyArtModel(id);
                    if (art != null) list.Add(new Source(category, id, Visual.Model, art, Build.Existing, ModelArt));
                    else Copy(category, id, Visual.Model, model, ModelTinted);
                    var spawn = ProfileSlot(profiles, id, baseId, VfxSlotKind.Spawn);
                    if (spawn != null)
                        Copy(category, id, Visual.Spawn, spawn.prefab, "where the model appears (on the water), scale = the profile's " + spawn.scale + " (x tuning scale); the VFX profile still decides whether it plays, its offset, tint and seconds; WebGL plays the vfx_fallback splash instead of a VFX Graph");
                    var death = ProfileSlot(profiles, id, baseId, VfxSlotKind.Death);
                    if (death != null)
                        Copy(category, id, Visual.DeathExtra, death.prefab, "where it dies, on top of the kill burst, scale = the profile's " + death.scale + " (x tuning scale); the VFX profile still decides whether it plays");
                }
            }
            Copy(VisualCategory.Enemies, VisualIds.Enemy, Visual.HealthBar, s.enemyHealthBarPrefab, "over elites, minibosses and the boss: above the model's top (at least enemyHealthBarHeight; + tuning heightOffset), its fill = hull / max hull");
            Made(VisualCategory.Enemies, VisualIds.Enemy, Visual.Placeholder, Build.PlaceholderCube, "an enemy with no model: the enemy's size (radius x 2, x tuning scale), tinted its placeholder colour (magenta when elite)");
            Made(VisualCategory.Enemies, VisualIds.Enemy, Visual.Shot, Build.HostileShot, "position (the shot's), size = shot radius x 2 x projectileSizeMultiplier x 1.6 (min 0.45; x tuning scale); trail width 0.8 x size (x tuning width); trail cleared on each new shot; its splash is Effects/splash");
            Made(VisualCategory.Enemies, VisualIds.Enemy, Visual.TelegraphRing, Build.TelegraphRing, "every enemy telegraph: the 40 points of trap, mine, net and warning circles (outline 0.3, filling ring 0.45, x tuning width) and the lanes of charges; colours per kind (trap red, mine amber, net blue, warning red; tuning overrideColor replaces them); alpha from the fuse");
            Copy(VisualCategory.Bosses, VisualIds.Boss, Visual.Arrival, s.bossArrivalPrefab, "where the boss is about to rise, scale bossArrivalScale (x tuning), for bossArrivalSeconds + 2.5 s (x tuning lifetime); WebGL plays the vfx_fallback splash");
            Made(VisualCategory.Bosses, VisualIds.Boss, Visual.MarkedTelegraph, Build.MarkedTelegraph, "the boss's marked bombardment or slam: at the marked point (BossConfig telegraphHeight + tuning heightOffset), radius x 2 wide (x tuning scale), telegraphThickness tall (x tuning width), colour BossConfig telegraphColor (tuning overrideColor replaces it)");

            // ---- The player ship and the Captains -------------------------------------------------------------------
            Copy(VisualCategory.Player, VisualIds.PlayerShip, Visual.HealthBar, s.enemyHealthBarPrefab, "over the ship's model at playerHealthBarHeight (+ tuning heightOffset), its fill = hull / max hull");
            Copy(VisualCategory.Player, VisualIds.PlayerShip, Visual.LevelUp, s.levelUpBurstPrefab, "at the ship (0.5 m up) on each level-up, scale levelUpBurstScale; " + FxCode);
            Copy(VisualCategory.Player, VisualIds.PlayerShip, Visual.Highlight, s.damageHighlightPrefab, "on the ship (0.5 m up, parented) when it is hit, scale damageHighlightScale (x tuning), for damageHighlightDuration (x tuning lifetime)");
            var shipDeath = profiles != null ? profiles.Slot(VfxSubject.PlayerShip, VfxProfileLibrary.PlayerShipId, VfxSlotKind.Death) : null;
            if (shipDeath != null && shipDeath.HasPrefab)
                Copy(VisualCategory.Player, VisualIds.PlayerShip, Visual.DeathExtra, shipDeath.prefab, "at the ship when it sinks, scale = the profile's " + shipDeath.scale + " (x tuning scale); the VFX profile still decides whether it plays");
            if (config != null && playerShip != null)
                foreach (var c in config.All<CaptainConfig>().Where(c => c != null && !string.IsNullOrEmpty(c.captainKey)).OrderBy(c => c.legacyId))
                {
                    if (hud != null && hud.EntryOf(c.legacyId) != null)
                        Made(VisualCategory.Captains, c.captainKey, Visual.ShipLook, Build.ShipLook, "at the run's start its renderers' materials go on the player ship's matching renderers (the ship's model and collider stay the run scene's)", playerShip, c.captainKey);
                    Made(VisualCategory.Captains, c.captainKey, Visual.ActiveRing, Build.ActiveRing, "while the Captain's active lasts: a circle of 48 points round the ship, radius 3.6 m (x tuning scale) pulsing 5% (x tuning pulse) at 8 rad/s (x pulseSpeed), 0.25 m up (+ heightOffset), the Captain's theme colour lifted halfway to white (tuning overrideColor replaces it), fading", null, c.captainKey);
                }

            // ---- Pickups and floor powerups -------------------------------------------------------------------------
            const string Barrel = "position (floats with barrelSubmergedFraction of its height under water), scale x the juice settings' barrel scale (x tuning scale), squashed to barrelVerticalScale tall; colliders removed; no shadows; sparkles switched off (barrelSparkles)";
            Copy(VisualCategory.Pickups, VisualIds.Pickup(PickupType.GoldBarrel), Visual.Model, s.goldBarrelPickupPrefab != null ? s.goldBarrelPickupPrefab : s.goldPickupPrefab, Barrel);
            Copy(VisualCategory.Pickups, VisualIds.Pickup(PickupType.XpBarrel), Visual.Model, s.xpBarrelPickupPrefab != null ? s.xpBarrelPickupPrefab : s.infamyPickupPrefab, Barrel);
            Copy(VisualCategory.Pickups, VisualIds.Pickup(PickupType.Powerup), Visual.Model, s.healthPickupPrefab, Barrel);
            Copy(VisualCategory.Pickups, VisualIds.Pickup(PickupType.Gold), Visual.Model, s.goldPickupPrefab, "position (bobbing, then flying to the ship), scale goldFlyingCoinScale (x tuning scale), spinning 180 deg/s (x tuning spin); its CollectSparkle replayed; colliders removed; no shadows");
            var gem = s.xpFlyingGemPrefab != null ? s.xpFlyingGemPrefab : s.infamyPickupPrefab;
            if (gem != null) Made(VisualCategory.Pickups, VisualIds.Pickup(PickupType.FlyingXp), Visual.Model, Build.Gem, "position (bobbing, then flying to the ship), scale xpFlyingGemScale (x tuning scale), spinning 180 deg/s (x tuning spin); its particles replayed; no shadows", gem);
            Made(VisualCategory.Pickups, VisualIds.Pickup(PickupType.FlyingHealth), Visual.Model, Build.HealthPlus, "flies to the ship from a health barrel: scale healthFlyingPickupScale (x tuning scale), faces the camera, fades in and out over 1.2 s; its material copied once, coloured healthBurstColor (tuning overrideColor replaces it)");
            foreach (PowerupKind kind in System.Enum.GetValues(typeof(PowerupKind)))
                Made(VisualCategory.Powerups, VisualIds.Powerup(kind), Visual.Model, Build.Powerup, "position (bobbing on the water, flying to the ship within the magnet radius); its name label faces the camera (WorldBillboardLabel); its collider is kept (as the pooled copies always had)", null, kind.ToString());

            // ---- Points of interest ---------------------------------------------------------------------------------
            void Poi(POIType type, GameObject prefab, string scale)
            {
                if (prefab == null) return;
                var code = "position (0.15 m above the POI, + tuning heightOffset), scale x " + scale + " (x tuning scale); colliders removed; shown, hidden and retired from the stage";
                if (type == POIType.SalvageChest) code += "; floats on the water and gets the golden glow light (SalvageChestWaterFloat, SalvageChestTreasureGlow); the Powder Cache's Rare Treasure Chest look is this chest";
                var path = PathOf(prefab);
                if (path != null && path.StartsWith("Assets/_Game/V4/Demo/POI/")) list.Add(new Source(VisualCategory.POIs, VisualIds.Poi(type), Visual.Model, prefab, Build.Existing, code));
                else Copy(VisualCategory.POIs, VisualIds.Poi(type), Visual.Model, prefab, code);
            }
            Poi(POIType.SalvageChest, s.salvageChestPrefab, "salvageChestScale");
            Poi(POIType.LighthouseOfFortune, s.lighthousePrefab, "lighthouseScale");
            Poi(POIType.TrialBuoy, s.trialBuoyPrefab, "trialBuoyScale");
            Poi(POIType.BossBeacon, s.bossBeaconPrefab, "bossBeaconScale");
            Poi(POIType.GildedWreck, s.gildedWreckPrefab, "gildedWreckScale");
            Poi(POIType.CursedIdol, s.cursedIdolPrefab, "cursedIdolScale");
            Poi(POIType.MerchantSkiff, s.merchantSkiffPrefab, "merchantSkiffScale");
            Made(VisualCategory.POIs, VisualIds.Poi(POIType.TrialBuoy), Visual.ChargeRing, Build.ChargeRing, "while the buoy charges: twice the charge radius wide (x tuning scale), 0.04 tall, pulsing 4% (x tuning pulse) at 6 rad/s (x pulseSpeed); light blue, brighter as it charges (tuning overrideColor replaces it)");
            Copy(VisualCategory.POIs, VisualIds.Poi(POIType.BossBeacon), Visual.TelegraphRing, s.bossTelegraphRingPrefab, "while the Beacon is telegraphed: bossTelegraphScale wide (x tuning scale), 0.05 tall, pulsing 8% (x tuning pulse) at 24 rad/s (x pulseSpeed)");

            // ---- Shared combat effects --------------------------------------------------------------------------------
            Copy(VisualCategory.Effects, VisualIds.HitSpark, Visual.Hit, s.enemyHitPrefab, "hits with no weapon prefab (items, grouped burn ticks): 0.35 m up, scale enemyHitScale; " + FxCode);
            Copy(VisualCategory.Effects, VisualIds.HitSpark, Visual.CritHit, s.critHitPrefab, "critical hits with no weapon prefab: 0.35 m up, scale critHitScale; " + FxCode);
            Copy(VisualCategory.Effects, VisualIds.HitSpark, Visual.CannonImpact, s.cannonImpactPrefab != null ? s.cannonImpactPrefab : s.weaponExplosionPrefab, "cannon hits with no weapon prefab: 0.45 m up, scale cannonImpactScale (x1.12 on a crit); 0.55 s (x tuning lifetime)");
            Copy(VisualCategory.Effects, VisualIds.Kill, Visual.Death, s.enemyKillPrefab, "every ordinary enemy's death (0.25 m up), scale enemyKillScale x the killing weapon's profile scale; " + FxCode + "; an enemy's own <id>_Death prefab wins");
            Copy(VisualCategory.Effects, VisualIds.Kill, Visual.EliteDeath, s.eliteKillExplosionPrefab, "an elite's death (an elite variant or an affix), scale eliteKillExplosionScale; " + FxCode);
            Copy(VisualCategory.Effects, VisualIds.Kill, Visual.BossDeath, s.bossKillExplosionPrefab != null ? s.bossKillExplosionPrefab : s.enemyKillPrefab, "the boss's death: bossDeathBurstCount bursts around it (random offsets), scale bossKillExplosionScale x 0.85, 1.0, 1.15...; " + FxCode);
            Copy(VisualCategory.Effects, VisualIds.DamageHighlight, Visual.Highlight, s.damageHighlightPrefab, "on a hit enemy (0.5 m up, parented to it), scale damageHighlightScale (x tuning), for damageHighlightDuration (x tuning lifetime); an enemy's own <id>_Highlight wins");
            Copy(VisualCategory.Effects, VisualIds.Splash, Visual.Splash, s.waterImpactSplashPrefab, "a splash with no weapon (enemy shots): 0.35 m up, scale waterImpactSplashScale; 0.55 s; merged with any splash within 1.35 m and 0.14 s");
            var kegBlast = s.powderKegExplosionPrefab != null ? s.powderKegExplosionPrefab : s.weaponExplosionPrefab;
            const string BlastCode = "0.45 m up, only its 'Impact small', 'Ring' and 'Smoke' children; scale max(1.15, powderKegExplosionScale x 2.35 x blast radius / 2.8 (0.9-1.35)) (x tuning scale); particles restarted; 1.5 s (x tuning lifetime)";
            Copy(VisualCategory.Effects, VisualIds.EnemyBlast, Visual.Blast, kegBlast, "the enemies' blasts (bomb boats, mines, traps, the boss's slams): " + BlastCode);
            Copy(VisualCategory.Effects, VisualIds.ItemBlast, Visual.Blast, kegBlast, "blasts with no keg weapon (Devil's Match, Queen Anne's Fuse, the Leviathan Heart's strike, the 1v1 bot's kegs): " + BlastCode);
            Copy(VisualCategory.Effects, VisualIds.ItemStrike, Visual.Strike, s.lightningStrikePrefab, "lightning with no strike weapon equipped (Storm in a Bottle): on the water, scale lightningStrikeScale x radius / 3.5 (0.85-1.35); 1.15 s (x tuning lifetime)");
            Made(VisualCategory.Effects, VisualIds.ItemRing, Visual.LandingRing, Build.ItemRing, "a timed ring with no weapon (the Leviathan Heart's): outline and a ring filling over its seconds; orange (tuning overrideColor replaces it), widths 0.3 and 0.45 (x tuning width)");
            var mortarBurst = s.mortarExplosionPrefab != null ? s.mortarExplosionPrefab : s.weaponExplosionPrefab;
            Copy(VisualCategory.Effects, VisualIds.ItemImpact, Visual.Impact, mortarBurst, "an area impact with no shell weapon (the Hammerhead Helm's bow spark): 4 bursts spread inside the radius; scale weaponExplosionScale x mortarExplosionScaleMult x radius / 11 (0.18-0.34); 1.05 s");
            Copy(VisualCategory.Effects, VisualIds.BurningPatch, Visual.Patch, s.burningTrailPrefab, "a burning oil patch: scale burningTrailScale x radius / 2.8 (0.75-1.6); 1.6 s (x tuning lifetime)");
            var telegraph = s.mortarTelegraphPrefab != null ? s.mortarTelegraphPrefab : s.lightningStrikePrefab;
            if (telegraph != null && telegraph.transform.Find("Visuals/WaterReticle") != null)
                Copy(VisualCategory.Effects, VisualIds.MortarReticle, Visual.Reticle, telegraph, "a mortar telegraph with no landing ring (the 1v1 bot's shells): 0.15 m up, scale mortarTelegraphScale x radius x 2 / 12.5; tinted towards mortarTelegraphColor; 1.22 s", "Visuals/WaterReticle");
            Made(VisualCategory.Effects, VisualIds.AuraPulse, Visual.PulseRing, Build.AuraPulse, "an aura pulse with no weapon (the 1v1 bot's): 48 points sweeping out to the radius (x tuning scale), gold (tuning overrideColor replaces it), width 0.45 shrinking (x tuning width), 0.5 s (x tuning lifetime)");
            Made(VisualCategory.Effects, VisualIds.DamageNumber, Visual.Number, Build.DamageNumber, "each number: its text, font size and character size per kind (hit 60/0.22, crit 72/0.26 gold, the ship's 64/0.24 red, grouped 54/0.19 orange; character size x tuning scale), rising 2.8 m/s, fading over 0.95 s (the ship's 1.05 s; x tuning lifetime), facing the camera");
            Copy(VisualCategory.Effects, VisualIds.VfxFallback, Visual.Splash, s.vfxGraphFallbackPrefab, "on devices without compute shaders (WebGL), in place of a VFX Graph effect (the spawn whirlpools, the boss arrival): scale vfxGraphFallbackScale (x tuning)");

            // ---- World and HUD --------------------------------------------------------------------------------------
            Made(VisualCategory.World, VisualIds.StormRing, Visual.Ring, Build.StormRing, "its 4 corners (the storm's safe rectangle), from the storm's start; colour, width and material are the prefab's");
            if (boundsWall != null) list.Add(new Source(VisualCategory.World, VisualIds.BoundsWall, Visual.Wall, boundsWall, Build.Existing, "one per edge of the play area, faded by distance (hidden since the hackathon: the storm edge warning shows instead)"));
            Made(VisualCategory.World, VisualIds.StormEdge, Visual.Warning, Build.StormEdgeWarning, "shown within 10 m of the play area's edge; its text's alpha 0.55-1 and size 1-1.08 (x tuning pulse) pulse at 8 rad/s (x pulseSpeed)");
            Made(VisualCategory.World, VisualIds.ThreatArrow, Visual.Arrow, Build.ThreatArrow, "one per off-screen boss, miniboss or scheduled elite: on the safe area's edge, turned towards it, pulsing 8% (x tuning pulse) at 6 rad/s (x pulseSpeed; x tuning scale); red-orange for bosses, gold for elites; its label (name and distance) beside it");
            Made(VisualCategory.World, VisualIds.InteractionPrompt, Visual.Prompt, Build.Prompt, "its place and size per layout (bottom bar, centred box, the chest's popup), its text and font size per layout, the USE and SKIP buttons' places and listeners");
            foreach (MinimapMarkerGraphics.GlyphKind glyph in System.Enum.GetValues(typeof(MinimapMarkerGraphics.GlyphKind)))
                Made(VisualCategory.World, VisualIds.MinimapIcon(glyph.ToString()), Visual.Icon, Build.MinimapGlyph, "the minimap icon's place (10 times a second), its icon box size (ship 14, POIs 12, boss 20, miniboss 15), the boss's 2.2x scale, the ship's heading; tuning scale multiplies the glyph", null, glyph.ToString());

            // ---- The 1v1 ----------------------------------------------------------------------------------------------
            if (playerShip != null)
                Made(VisualCategory.Pvp, VisualIds.PvpOpponent, Visual.Ship, Build.PvpShip, "the opponent's pose (the relay's); dressed as the player's ship is when the match starts (its Captain's materials, parts shown or hidden); every material tinted 70% (tuning tintStrength) towards red (tuning overrideColor)", playerShip);
            Copy(VisualCategory.Pvp, VisualIds.PvpOpponent, Visual.HealthBar, s.enemyHealthBarPrefab, "over the opponent's ship at enemyHealthBarHeight (+ tuning heightOffset), its fill = its hull");
            Made(VisualCategory.Pvp, VisualIds.PvpStormRing, Visual.Ring, Build.PvpRing, "its 128 points: the storm's circle round the arena centre, 0.4 m up (+ tuning heightOffset); colour, width and material are the prefab's");
            Made(VisualCategory.Pvp, VisualIds.PvpXpBarrel, Visual.Barrel, Build.PvpBarrel, "position (0.6 m up + heightOffset, bobbing 0.25 m x tuning pulse at 2 rad/s x pulseSpeed), turning 60 deg/s (x tuning spin); made when the barrel appears (as before)", null, "xp");
            Made(VisualCategory.Pvp, VisualIds.PvpHealthBarrel, Visual.Barrel, Build.PvpBarrel, "position (0.6 m up + heightOffset, bobbing 0.25 m x tuning pulse at 2 rad/s x pulseSpeed), turning 60 deg/s (x tuning spin); made when the barrel appears (as before)", null, "health");
            Made(VisualCategory.Pvp, VisualIds.PvpShot, Visual.Ball, Build.PvpBall, "each bot shot: its flight (an arc 1.5 m up + its arc height), its colour on the ball and trail (pale smoke behind a dark ball)");
            Made(VisualCategory.Pvp, VisualIds.PvpArrow, Visual.Arrow, Build.PvpArrow, "one per off-screen opponent or barrel: on the screen's edge, turned towards it, coloured (red foe, gold XP, green health), its label");
            return list;
        }

        /// <summary>Every enemy and variant in the config (sorted by id): its id, its model before the catalog (the config's
        /// model, else the fallback; the art model is <see cref="DemoJuiceSettings.EnemyArtModel"/>), its folder (Bosses for
        /// the boss and minibosses) and, for an elite variant, its base enemy (its VFX profile's fallback).</summary>
        public static List<(string id, GameObject model, VisualCategory category, string baseId)> Enemies(ConfigSetRuntime config, DemoJuiceSettings s)
        {
            var list = new List<(string, GameObject, VisualCategory, string)>();
            if (config == null) return list;
            foreach (var a in config.All<EnemyArchetypeConfig>())
            {
                if (a == null || string.IsNullOrEmpty(a.enemyId)) continue;
                var r = EnemyStatResolver.Resolve(a);
                list.Add((a.enemyId, r.visualPrefab, a.isBoss || a.isMiniboss ? VisualCategory.Bosses : VisualCategory.Enemies, null));
            }
            foreach (var v in config.All<EnemyVariantConfig>())
            {
                if (v == null || v.baseArchetype == null || string.IsNullOrEmpty(v.variantId)) continue;
                var r = EnemyStatResolver.Resolve(v);
                var boss = v.baseArchetype.isBoss || v.baseArchetype.isMiniboss;
                list.Add((v.variantId, r.visualPrefab, boss ? VisualCategory.Bosses : VisualCategory.Enemies, v.isElite ? v.baseArchetype.enemyId : null));
            }
            list.Sort((x, y) => string.CompareOrdinal(x.Item1, y.Item1));
            return list;
        }

        /// <summary>An enemy's VFX profile moment with a prefab (its own, else its base enemy's for an elite), else null.</summary>
        static VfxSlot ProfileSlot(VfxProfileLibrary profiles, string id, string baseId, VfxSlotKind kind)
        {
            if (profiles == null) return null;
            var slot = profiles.Slot(VfxSubject.Enemy, id, kind);
            if ((slot == null || !slot.HasPrefab) && baseId != null) slot = profiles.Slot(VfxSubject.Enemy, baseId, kind);
            return slot != null && slot.HasPrefab ? slot : null;
        }

        static string PathOf(Object o)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetAssetPath(o);
#else
            return null;
#endif
        }
    }
}
