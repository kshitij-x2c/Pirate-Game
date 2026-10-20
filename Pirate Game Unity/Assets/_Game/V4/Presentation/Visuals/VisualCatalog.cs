using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoguePirates.V4.Presentation.Visuals
{
    /// <summary>The folder a visual's prefabs live in (Assets/_Game/V4/Visuals/Prefabs/&lt;Category&gt;/&lt;id&gt;/).</summary>
    public enum VisualCategory { Enemies, Bosses, Player, Captains, Pickups, Powerups, POIs, Effects, World, Pvp }

    /// <summary>One visual: an owner id (an enemy, a pickup, a shared effect...), what it is (<see cref="Visual"/>), its prefab.</summary>
    [Serializable]
    public sealed class VisualEntry
    {
        public VisualCategory category;
        [Tooltip("The owner: an enemy or variant id, a captain key, a pickup or powerup id, a POI id, or a shared effect's id (see plan/v5/VISUAL-PREFABS.md).")]
        public string id;
        [Tooltip("Which of its visuals (Model, Death, HealthBar, Ring... see Visual).")]
        public string visual;
        public GameObject prefab;
    }

    /// <summary>
    /// V5 (20 Oct, owner: "every non-weapon visual from an editable prefab, like the weapons"): which prefab each visual the
    /// game draws comes from, by owner id and visual name: enemy and boss models, their spawn and death effects, the shared
    /// hit, kill, blast and splash effects, health bars, enemy shots and telegraph rings, pickups and the floor powerups,
    /// points of interest, the player ship's look per Captain and its Captain's active ring, damage numbers, the storm ring
    /// and edge warning, threat arrows, minimap icons, the interaction prompt and the 1v1's pieces. The prefabs live under
    /// Assets/_Game/V4/Visuals/Prefabs/&lt;Category&gt;/&lt;id&gt;/&lt;id&gt;_&lt;Visual&gt;.prefab (built by Tools/Rogue Pirates/Visuals/Build
    /// Visual Prefabs as copies of what the game drew before, so the look is unchanged), or are the art team's own prefabs
    /// where those already were single clean prefabs. The presenters read this catalog by id, pool and prewarm what it
    /// lists before the run, and apply each prefab's <see cref="VisualTuning"/> to the numbers the code still sets. An empty
    /// entry (or a missing one) keeps the code's or the juice settings' look. Weapons have their own catalog
    /// (<see cref="Weapons.WeaponVisualCatalog"/>). Presentation only: the simulation never reads this.
    /// The full list is plan/v5/VISUAL-PREFABS.md.
    /// </summary>
    [CreateAssetMenu(fileName = "V4Visuals", menuName = "RoguePirates/Visual Catalog")]
    public sealed class VisualCatalog : ScriptableObject
    {
        /// <summary>Resources path (the asset is Assets/_Game/V4/Visuals/Resources/V4Visuals.asset).</summary>
        public const string ResourcePath = "V4Visuals";

        [Tooltip("One row per visual: category, owner id, visual name, prefab. Rows with the same id and visual: the first wins.")]
        public VisualEntry[] entries = Array.Empty<VisualEntry>();

        static VisualCatalog _loaded;
        Dictionary<string, GameObject> _index;

        /// <summary>The catalog in Resources (null when there is none: every visual keeps the code's look).</summary>
        public static VisualCatalog Load()
        {
            if (_loaded == null) _loaded = Resources.Load<VisualCatalog>(ResourcePath);
            return _loaded;
        }

        void OnEnable() => _index = null;
        void OnValidate() => _index = null;

        static string Key(string id, string visual) => id + "/" + visual;

        /// <summary>The prefab for an owner's visual (null: none set).</summary>
        public GameObject Get(string id, string visual)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(visual)) return null;
            if (_index == null)
            {
                _index = new Dictionary<string, GameObject>();
                if (entries != null)
                    foreach (var e in entries)
                        if (e != null && !string.IsNullOrEmpty(e.id) && !string.IsNullOrEmpty(e.visual) && e.prefab != null && !_index.ContainsKey(Key(e.id, e.visual)))
                            _index[Key(e.id, e.visual)] = e.prefab;
            }
            return _index.TryGetValue(Key(id, visual), out var prefab) ? prefab : null;
        }

        /// <summary>The prefab for an owner's visual, else <paramref name="fallback"/>.</summary>
        public GameObject Pick(string id, string visual, GameObject fallback)
        {
            var prefab = Get(id, visual);
            return prefab != null ? prefab : fallback;
        }

        /// <summary>The entry for an owner's visual (null: none).</summary>
        public VisualEntry Find(string id, string visual)
        {
            if (entries == null) return null;
            foreach (var e in entries)
                if (e != null && e.id == id && e.visual == visual) return e;
            return null;
        }

        /// <summary>Every prefab of one visual name, across owners (prewarming per-owner overrides).</summary>
        public IEnumerable<(string id, GameObject prefab)> All(string visual)
        {
            if (entries == null) yield break;
            foreach (var e in entries)
                if (e != null && e.visual == visual && e.prefab != null) yield return (e.id, e.prefab);
        }

        /// <summary>Editor and tests: the index is rebuilt after the rows change.</summary>
        public void Reindex() => _index = null;
    }

    /// <summary>
    /// Shorthand for presenters: the run's catalog (<see cref="VisualCatalog.Load"/>, settable for tests) and its picks.
    /// </summary>
    public static class VisualPick
    {
        static VisualCatalog _override;
        static bool _overridden;

        /// <summary>The catalog presenters read (tests may set another, or none).</summary>
        public static VisualCatalog Catalog
        {
            get => _overridden ? _override : VisualCatalog.Load();
            set { _override = value; _overridden = true; }
        }

        /// <summary>Back to the Resources catalog (tests).</summary>
        public static void ResetCatalog() { _override = null; _overridden = false; }

        public static GameObject Get(string id, string visual) => Catalog != null ? Catalog.Get(id, visual) : null;

        public static GameObject Pick(string id, string visual, GameObject fallback) => Catalog != null ? Catalog.Pick(id, visual, fallback) : fallback;
    }
}
