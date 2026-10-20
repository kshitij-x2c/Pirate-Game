using System.Collections.Generic;
using UnityEngine;

namespace RoguePirates.V4.Presentation.Visuals
{
    /// <summary>
    /// V5 (owner: every visual tweakable in the Editor without code): the numbers the code applies to a visual, on the
    /// visual's prefab (root). Each value multiplies or offsets what the code computes (an enemy's drawn size, an effect's
    /// seconds, a pickup's spin, a ring's width or pulse), so 1 (and 0 for the offset) is the code's look. The presenters
    /// read it from the prefab asset when they use it, so most edits show during Play mode on the next use. A prefab
    /// without it uses the code's values. What each value does per visual is in plan/v5/VISUAL-PREFABS.md. Presentation only.
    /// Like <see cref="Weapons.WeaponVisualTuning"/> for the weapons, plus the pulse and tint the non-weapon visuals use.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VisualTuning : MonoBehaviour
    {
        [Tooltip("Times the scale the code gives it (an enemy's size, a pickup's scale, an effect's scale, a ring's radius).")]
        [Min(0f)] public float scale = 1f;
        [Tooltip("Times how long the code shows it (an effect's seconds, a number's rise, a banner's ring).")]
        [Min(0.05f)] public float lifetime = 1f;
        [Tooltip("Times its spin speed (pickups 180 deg/s, the 1v1 barrels 60 deg/s).")]
        public float spin = 1f;
        [Tooltip("Times the size of its pulse (the buoy's charge ring 4%, the Beacon's ring 8%, the Captain's ring 5%, the storm warning 8%).")]
        [Min(0f)] public float pulse = 1f;
        [Tooltip("Times the speed of its pulse.")]
        [Min(0f)] public float pulseSpeed = 1f;
        [Tooltip("Times its line width (rings, telegraphs, trails).")]
        [Min(0f)] public float width = 1f;
        [Tooltip("Metres added to its height above where the code puts it.")]
        public float heightOffset;

        [Header("Colour")]
        [Tooltip("Use this colour instead of the code's (a ring's colour, a telegraph's colour, the 1v1 opponent's tint). The code's alpha fades still apply.")]
        public bool overrideColor;
        public Color color = Color.white;
        [Tooltip("How far the code tints the model's own colours towards its colour (an enemy's hull 0.6, the 1v1 opponent 0.7). Below 0: the code's own.")]
        public float tintStrength = -1f;
        [Tooltip("Keep the prefab's own material colours: the code does not tint it (an enemy's type colour on sails and hull).")]
        public bool keepOwnColours;

        static readonly Dictionary<int, VisualTuning> Cache = new Dictionary<int, VisualTuning>();

        /// <summary>The tuning on a prefab's root (null: the code's values). Looked up once per prefab.</summary>
        public static VisualTuning Of(GameObject prefab)
        {
            if (prefab == null) return null;
            var id = prefab.GetInstanceID();
            if (!Cache.TryGetValue(id, out var tuning) || (tuning == null && !ReferenceEquals(tuning, null)))
            {
                tuning = prefab.GetComponent<VisualTuning>();
                Cache[id] = tuning;
            }
            return tuning;
        }

        public static float Scale(GameObject prefab) => Of(prefab) is { } t ? t.scale : 1f;
        public static float Lifetime(GameObject prefab) => Of(prefab) is { } t ? t.lifetime : 1f;
        public static float Spin(GameObject prefab) => Of(prefab) is { } t ? t.spin : 1f;
        public static float Pulse(GameObject prefab) => Of(prefab) is { } t ? t.pulse : 1f;
        public static float PulseSpeed(GameObject prefab) => Of(prefab) is { } t ? t.pulseSpeed : 1f;
        public static float Width(GameObject prefab) => Of(prefab) is { } t ? t.width : 1f;
        public static float Height(GameObject prefab) => Of(prefab) is { } t ? t.heightOffset : 0f;
        public static bool KeepsOwnColours(GameObject prefab) => Of(prefab) is { } t && t.keepOwnColours;

        /// <summary>The tint strength (the tuning's when set, else <paramref name="code"/>).</summary>
        public static float Strength(GameObject prefab, float code) => Of(prefab) is { } t && t.tintStrength >= 0f ? t.tintStrength : code;

        /// <summary>The tuning's colour when it overrides the code's (alpha times the code's alpha), else <paramref name="code"/>.</summary>
        public static Color Colour(GameObject prefab, Color code)
        {
            var t = Of(prefab);
            if (t == null || !t.overrideColor) return code;
            var c = t.color;
            c.a *= code.a;
            return c;
        }
    }

    /// <summary>The visual names the catalog uses (see plan/v5/VISUAL-PREFABS.md for what each one is).</summary>
    public static class Visual
    {
        // Models
        public const string Model = "Model";
        public const string Placeholder = "Placeholder";
        public const string ShipLook = "ShipLook";
        public const string Ship = "Ship";
        // Enemy moments (an enemy's own entry wins over the shared one)
        public const string Spawn = "Spawn";
        public const string Death = "Death";
        public const string DeathExtra = "DeathExtra";
        public const string EliteDeath = "EliteDeath";
        public const string BossDeath = "BossDeath";
        public const string Highlight = "Highlight";
        public const string HealthBar = "HealthBar";
        public const string Arrival = "Arrival";
        // Enemy attacks
        public const string Shot = "Shot";
        public const string TelegraphRing = "TelegraphRing";
        public const string MarkedTelegraph = "MarkedTelegraph";
        // Shared effects
        public const string Hit = "Hit";
        public const string CritHit = "CritHit";
        public const string CannonImpact = "CannonImpact";
        public const string Blast = "Blast";
        public const string Splash = "Splash";
        public const string Strike = "Strike";
        public const string Impact = "Impact";
        public const string LandingRing = "LandingRing";
        public const string PulseRing = "PulseRing";
        public const string Patch = "Patch";
        public const string Reticle = "Reticle";
        public const string Number = "Number";
        public const string LevelUp = "LevelUp";
        // Rings, world and HUD
        public const string Ring = "Ring";
        public const string ChargeRing = "ChargeRing";
        public const string ActiveRing = "ActiveRing";
        public const string Wall = "Wall";
        public const string Warning = "Warning";
        public const string Arrow = "Arrow";
        public const string Icon = "Icon";
        public const string Prompt = "Prompt";
        public const string Barrel = "Barrel";
        public const string Ball = "Ball";
    }

    /// <summary>The ids of the shared owners (one prefab every enemy, item or HUD element uses unless it has its own).</summary>
    public static class VisualIds
    {
        public const string Enemy = "enemy";
        public const string Boss = "boss";
        public const string PlayerShip = "player_ship";
        public const string HitSpark = "hit";
        public const string Kill = "kill";
        public const string DamageHighlight = "damage_highlight";
        public const string Splash = "splash";
        public const string EnemyBlast = "enemy_blast";
        public const string ItemBlast = "item_blast";
        public const string ItemStrike = "item_strike";
        public const string ItemRing = "item_ring";
        public const string ItemImpact = "item_impact";
        public const string BurningPatch = "burning_patch";
        public const string MortarReticle = "mortar_reticle";
        public const string AuraPulse = "aura_pulse";
        public const string DamageNumber = "damage_number";
        public const string VfxFallback = "vfx_fallback";
        public const string StormRing = "storm_ring";
        public const string BoundsWall = "bounds_wall";
        public const string StormEdge = "storm_edge";
        public const string ThreatArrow = "threat_arrow";
        public const string InteractionPrompt = "interaction_prompt";
        public const string PvpOpponent = "pvp_opponent";
        public const string PvpStormRing = "pvp_storm_ring";
        public const string PvpXpBarrel = "pvp_xp_barrel";
        public const string PvpHealthBarrel = "pvp_health_barrel";
        public const string PvpShot = "pvp_shot";
        public const string PvpArrow = "pvp_arrow";

        /// <summary>The minimap icon owner for a glyph ("minimap_boss", "minimap_salvage_chest"...).</summary>
        public static string MinimapIcon(string glyph) => "minimap_" + Snake(glyph);

        /// <summary>A pickup type's owner id.</summary>
        public static string Pickup(RoguePirates.V4.Foundation.Core.PickupType type) => type switch
        {
            RoguePirates.V4.Foundation.Core.PickupType.GoldBarrel => "gold_barrel",
            RoguePirates.V4.Foundation.Core.PickupType.XpBarrel => "xp_barrel",
            RoguePirates.V4.Foundation.Core.PickupType.Gold or RoguePirates.V4.Foundation.Core.PickupType.FlyingGold => "gold_coin",
            RoguePirates.V4.Foundation.Core.PickupType.Powerup => "health_barrel",
            RoguePirates.V4.Foundation.Core.PickupType.FlyingHealth => "health_plus",
            _ => "xp_gem",
        };

        /// <summary>A floor powerup kind's owner id ("repair_supplies", "full_sail"...).</summary>
        public static string Powerup(RoguePirates.V4.Config.PowerupKind kind) => Snake(kind.ToString());

        /// <summary>A point of interest's owner id (its player-facing name: "salvage_chest", "repair_wreck", "powder_cache"...).</summary>
        public static string Poi(RoguePirates.V4.Foundation.Core.POIType type) => type switch
        {
            RoguePirates.V4.Foundation.Core.POIType.SalvageChest => "salvage_chest",
            RoguePirates.V4.Foundation.Core.POIType.LighthouseOfFortune => "lighthouse_of_fortune",
            RoguePirates.V4.Foundation.Core.POIType.TrialBuoy => "signal_buoy",
            RoguePirates.V4.Foundation.Core.POIType.BossBeacon => "treasury_beacon",
            RoguePirates.V4.Foundation.Core.POIType.GildedWreck => "repair_wreck",
            RoguePirates.V4.Foundation.Core.POIType.MerchantSkiff => "powder_cache",
            RoguePirates.V4.Foundation.Core.POIType.CursedIdol => "treasure_chart",
            _ => Snake(type.ToString()),
        };

        /// <summary>"MagneticTide" to "magnetic_tide".</summary>
        public static string Snake(string pascal)
        {
            var sb = new System.Text.StringBuilder(pascal.Length + 4);
            for (var i = 0; i < pascal.Length; i++)
            {
                var c = pascal[i];
                if (char.IsUpper(c) && i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
    }
}
