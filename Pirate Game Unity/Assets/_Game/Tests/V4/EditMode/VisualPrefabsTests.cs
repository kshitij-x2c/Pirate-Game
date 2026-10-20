using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RoguePirates.V4.Config;
using RoguePirates.V4.Editor;
using RoguePirates.V4.Foundation.Core;
using RoguePirates.V4.Foundation.Data;
using RoguePirates.V4.Presentation.Effects;
using RoguePirates.V4.Presentation.Hud;
using RoguePirates.V4.Presentation.Pooling;
using RoguePirates.V4.Presentation.Visuals;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoguePirates.V4.Tests
{
    /// <summary>
    /// V5 (owner: every non-weapon visual as an editable prefab, like the weapons): every visual the game draws outside the
    /// weapons has a prefab in the visual catalog the game loads, in its owner's folder (or the art team's own prefab), with
    /// no missing or V3 script and no new VFX Graph; the copies look like what the game drew before (same meshes, materials,
    /// particles, scales); the code-built ones keep the code's settings; and the presenters draw the catalog's prefabs from
    /// pools prewarmed before the run.
    /// </summary>
    [Category("V4")]
    public sealed class VisualPrefabsTests
    {
        const string SetPath = "Assets/_Game/V4/Data/ConfigSets/SaltwindDefault.asset";
        const string SettingsPath = "Assets/_Game/Resources/DemoJuiceSettings.asset";

        static VisualCatalog Catalog => AssetDatabase.LoadAssetAtPath<VisualCatalog>(VisualPrefabs.CatalogPath);
        static DemoJuiceSettings Settings => AssetDatabase.LoadAssetAtPath<DemoJuiceSettings>(SettingsPath);
        static List<VisualSources.Source> _sources;
        static List<VisualSources.Source> Sources => _sources ??= VisualPrefabs.Sources();

        [TearDown]
        public void ResetPick() => VisualPick.ResetCatalog();

        [Test]
        public void TheCatalog_IsTheOneTheGameLoads_AndCoversEveryCategory()
        {
            Assert.IsNotNull(Catalog, VisualPrefabs.CatalogPath);
            Assert.AreSame(Catalog, VisualCatalog.Load(), "loaded from Resources at run time");
            Assert.Greater(Sources.Count, 100, "every non-weapon visual");
            foreach (VisualCategory c in System.Enum.GetValues(typeof(VisualCategory)))
                Assert.IsTrue(Sources.Any(s => s.Category == c), c + " has visuals");
            // Every enemy and variant of the run's config has its model listed.
            var set = AssetDatabase.LoadAssetAtPath<ConfigSet>(SetPath);
            using var config = new ConfigSetRuntime(set);
            foreach (var (id, _, _, _) in VisualSources.Enemies(config, Settings))
                Assert.IsNotNull(Catalog.Get(id, Visual.Model), id + ": its model");
            foreach (PowerupKind kind in System.Enum.GetValues(typeof(PowerupKind)))
                Assert.IsNotNull(Catalog.Get(VisualIds.Powerup(kind), Visual.Model), kind + ": its model");
        }

        [Test]
        public void EveryVisual_HasItsPrefab_InItsFolder_WithNoMissingOrV3Scripts_AndNoNewVfxGraph()
        {
            var problems = new List<string>();
            foreach (var s in Sources)
            {
                var prefab = Catalog.Get(s.Id, s.Visual);
                var what = s.Category + "/" + s.Id + " " + s.Visual;
                if (prefab == null) { problems.Add(what + ": not in the catalog"); continue; }
                var path = AssetDatabase.GetAssetPath(prefab);
                if (s.How == VisualSources.Build.Existing)
                {
                    if (prefab != s.Prefab) problems.Add(what + ": should list " + AssetDatabase.GetAssetPath(s.Prefab));
                    continue;
                }
                if (path != VisualPrefabs.PrefabPath(s.Category, s.Id, s.Visual)) problems.Add(what + ": is " + path);
                if (s.How != VisualSources.Build.ShipLook && prefab.GetComponent<VisualTuning>() == null) problems.Add(what + ": no VisualTuning");
                var sourceHasGraph = s.Prefab != null && s.Prefab.GetComponentsInChildren<Component>(true).Any(c => c != null && c.GetType().Name == "VisualEffect");
                foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) problems.Add(path + ": missing script on " + t.name);
                    foreach (var b in t.GetComponents<MonoBehaviour>())
                        if (VisualPrefabs.IsV3Script(b)) problems.Add(path + ": V3 script " + b.GetType().FullName);
                    if (!sourceHasGraph && t.GetComponents<Component>().Any(c => c != null && c.GetType().Name == "VisualEffect")) problems.Add(path + ": a new VFX Graph");
                }
                foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                        if (m != null && !EditorUtility.IsPersistent(m)) problems.Add(path + ": a material that is not an asset on " + r.name);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void TheVfxGraphCopies_HaveTheWebGlStandIn()
        {
            var graphs = Sources.Where(s => s.How == VisualSources.Build.Copy && Catalog.Get(s.Id, s.Visual).GetComponentInChildren<UnityEngine.VFX.VisualEffect>(true) != null).ToList();
            Assert.IsNotEmpty(graphs, "the spawn whirlpools and the boss arrival keep their VFX Graph (as before)");
            Assert.IsNotNull(Catalog.Get(VisualIds.VfxFallback, Visual.Splash), "devices without compute shaders (WebGL) play the stand-in, as before");
            foreach (var s in graphs) Assert.IsNotNull(s.Prefab.GetComponentInChildren<UnityEngine.VFX.VisualEffect>(true), s.Id + ": its source had one");
        }

        [Test]
        public void TheCopies_LookLikeWhatTheGameDrewBefore()
        {
            var problems = new List<string>();
            foreach (var s in Sources.Where(s => s.How == VisualSources.Build.Copy || s.How == VisualSources.Build.Gem || s.How == VisualSources.Build.PvpShip))
            {
                var copy = Catalog.Get(s.Id, s.Visual);
                var source = s.Child != null ? s.Prefab.transform.Find(s.Child).gameObject : s.Prefab;
                var what = s.Id + " " + s.Visual + " (" + source.name + ")";
                if (s.Child == null && (copy.transform.localScale - source.transform.localScale).sqrMagnitude > 1e-8f) problems.Add(what + ": root scale");
                var a = source.GetComponentsInChildren<Renderer>(true);
                var b = copy.GetComponentsInChildren<Renderer>(true);
                if (a.Length != b.Length) { problems.Add(what + ": " + b.Length + " renderers, was " + a.Length); continue; }
                for (var i = 0; i < a.Length; i++)
                {
                    if (a[i].GetType().Name != "VFXRenderer" && !a[i].sharedMaterials.SequenceEqual(b[i].sharedMaterials)) problems.Add(what + ": materials of " + b[i].name);
                    var ma = a[i].GetComponent<MeshFilter>();
                    var mb = b[i].GetComponent<MeshFilter>();
                    if (ma != null && (mb == null || ma.sharedMesh != mb.sharedMesh)) problems.Add(what + ": mesh of " + b[i].name);
                    if ((a[i].transform.localScale - b[i].transform.localScale).sqrMagnitude > 1e-8f) problems.Add(what + ": scale of " + b[i].name);
                    if (s.How != VisualSources.Build.PvpShip && a[i].gameObject.activeSelf != b[i].gameObject.activeSelf) problems.Add(what + ": shown state of " + b[i].name);
                }
                if (source.GetComponentsInChildren<ParticleSystem>(true).Length != copy.GetComponentsInChildren<ParticleSystem>(true).Length) problems.Add(what + ": particle systems");
                if (s.How == VisualSources.Build.Gem && copy.GetComponentsInChildren<Collider>(true).Length != 0) problems.Add(what + ": the gem has no colliders (as the code's template)");
                if (s.How == VisualSources.Build.PvpShip && copy.GetComponentsInChildren<MonoBehaviour>(true).Any(m => !(m is VisualTuning))) problems.Add(what + ": scripts left on the opponent");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void TheCodeBuiltVisuals_ArePrefabsWithTheCodeBuiltSettings()
        {
            var host = new GameObject("CodeLines");
            try
            {
                void SameLine(System.Action<LineRenderer> configure, string id, string visual)
                {
                    var line = host.AddComponent<LineRenderer>();
                    configure(line);
                    var prefab = Catalog.Get(id, visual);
                    var own = prefab.GetComponent<LineRenderer>();
                    Assert.IsNotNull(own, id + ": a LineRenderer at its root");
                    Assert.AreEqual(line.positionCount, own.positionCount, id);
                    Assert.AreEqual(line.loop, own.loop, id);
                    Assert.AreEqual(line.widthMultiplier, own.widthMultiplier, 1e-5f, id);
                    Assert.AreEqual(line.startColor, own.startColor, id);
                    Assert.AreEqual(line.shadowCastingMode, own.shadowCastingMode, id);
                    Assert.AreEqual("Sprites/Default", own.sharedMaterial.shader.name, id);
                    Object.DestroyImmediate(line);
                }
                SameLine(HazardRingPresenter.ConfigureRingLine, VisualIds.Enemy, Visual.TelegraphRing);
                SameLine(HazardRingPresenter.ConfigureRingLine, VisualIds.ItemRing, Visual.LandingRing);
                SameLine(RoguePirates.V4.Presentation.Weapons.WeaponVfxPresenter.ConfigurePulseLine, VisualIds.AuraPulse, Visual.PulseRing);
                SameLine(RoguePirates.V4.Presentation.World.StormRingPresenter.ConfigureLine, VisualIds.StormRing, Visual.Ring);
                SameLine(RoguePirates.V4.Presentation.Pvp.PvpMatchPresenter.ConfigureRing, VisualIds.PvpStormRing, Visual.Ring);
                foreach (var key in new[] { "rook", "brine", "flint", "richie", "ember", "roxie" })
                    SameLine(CaptainActiveEffectPresenter.ConfigureRing, key, Visual.ActiveRing);
            }
            finally { Object.DestroyImmediate(host); }

            // The floor powerups: the placeholder's primitive mesh, scale and colour, its collider kept (as the pooled copies had).
            foreach (PowerupKind kind in System.Enum.GetValues(typeof(PowerupKind)))
            {
                var code = PowerupPresenter.BuildTemplate(kind);
                try
                {
                    var prefab = Catalog.Get(VisualIds.Powerup(kind), Visual.Model);
                    Assert.AreEqual(code.GetComponent<MeshFilter>().sharedMesh, prefab.GetComponent<MeshFilter>().sharedMesh, kind + " mesh");
                    Assert.AreEqual(code.transform.localScale, prefab.transform.localScale, kind + " scale");
                    Assert.AreEqual(code.GetComponent<Renderer>().sharedMaterial.color, prefab.GetComponent<Renderer>().sharedMaterial.color, kind + " colour");
                    Assert.IsNotNull(prefab.GetComponent<Collider>(), kind + " keeps its collider");
                    Assert.IsNotNull(prefab.GetComponent<RoguePirates.V4.Foundation.World.WorldBillboardLabel>(), kind + " keeps its label");
                }
                finally { Object.DestroyImmediate(code); }
            }

            // The enemy shot: an iron ball (lit, dark) and its trail, materials as assets with the code's colours.
            var owned = new List<Material>();
            var shot = HostileShotPresenter.BuildTemplate(owned);
            try
            {
                var prefab = Catalog.Get(VisualIds.Enemy, Visual.Shot);
                var ball = prefab.GetComponentInChildren<MeshRenderer>(true);
                Assert.AreEqual(shot.GetComponentInChildren<MeshRenderer>(true).sharedMaterial.color, ball.sharedMaterial.color, "iron colour");
                var trail = prefab.GetComponent<TrailRenderer>();
                Assert.AreEqual(shot.GetComponent<TrailRenderer>().time, trail.time);
                Assert.AreEqual(shot.GetComponent<TrailRenderer>().colorGradient.colorKeys.Length, trail.colorGradient.colorKeys.Length);
            }
            finally
            {
                Object.DestroyImmediate(shot);
                foreach (var m in owned) Object.DestroyImmediate(m);
            }

            // The damage number keeps the built-in font; the minimap glyphs and arrows use the saved sprites.
            var number = Catalog.Get(VisualIds.DamageNumber, Visual.Number).GetComponent<TextMesh>();
            Assert.AreEqual(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), number.font);
            foreach (var image in Catalog.Get(VisualIds.ThreatArrow, Visual.Arrow).GetComponentsInChildren<UnityEngine.UI.Image>(true))
                Assert.IsTrue(EditorUtility.IsPersistent(image.sprite), "the arrow's sprite is an asset");
            Assert.AreEqual(3, Catalog.Get(VisualIds.MinimapIcon("Boss"), Visual.Icon).transform.childCount, "the boss glyph has its halo");
        }

        [Test]
        public void ACaptainsShipLook_PutsTheSameMaterialsOnTheShip_AsTheSailAndFlagSwap()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(VisualSources.PlayerShipModel);
            var hud = SaltWindHudSettings.Load();
            var set = AssetDatabase.LoadAssetAtPath<ConfigSet>(SetPath);
            using var config = new ConfigSetRuntime(set);
            foreach (var captain in config.All<CaptainConfig>())
            {
                var a = (GameObject)PrefabUtility.InstantiatePrefab(model);
                var b = (GameObject)PrefabUtility.InstantiatePrefab(model);
                try
                {
                    hud.ApplyShipLook(a.transform, captain.legacyId);
                    var look = Catalog.Get(captain.captainKey, Visual.ShipLook);
                    Assert.IsNotNull(look, captain.captainKey);
                    Assert.Greater(ShipLook.Apply(b.transform, look), 0, captain.captainKey + ": the sail and flag changed");
                    var ra = a.GetComponentsInChildren<Renderer>(true);
                    var rb = b.GetComponentsInChildren<Renderer>(true);
                    for (var i = 0; i < ra.Length; i++)
                        Assert.IsTrue(ra[i].sharedMaterials.SequenceEqual(rb[i].sharedMaterials), captain.captainKey + ": " + ra[i].name);
                }
                finally
                {
                    Object.DestroyImmediate(a);
                    Object.DestroyImmediate(b);
                }
            }
        }

        [Test]
        public void ThePresenters_DrawTheCatalogsPrefabs_FromPoolsPrewarmedBeforeTheRun()
        {
            var root = new GameObject("VisualTestPools");
            var pools = new PoolService(root.transform);
            var set = AssetDatabase.LoadAssetAtPath<ConfigSet>(SetPath);
            using var config = new ConfigSetRuntime(set);
            var shark = ScriptableObject.CreateInstance<EnemyDefinition>();
            shark.id = "shark";
            shark.radius = 1f;
            try
            {
                // The run's enemy pools are the catalog's models.
                var defs = PoolCatalog.Build(config, moduleLab: false);
                foreach (var id in new[] { "shark", "bomb_ketch", "barnacle_king" })
                {
                    var model = Catalog.Get(id, Visual.Model);
                    var def = defs.FirstOrDefault(d => d.Key == PoolCatalog.EnemyKeyPrefix + model.name);
                    Assert.AreSame(model, def.Prefab, id + ": its pool is its catalog model");
                    Assert.Greater(def.Prewarm, 0, id + ": prewarmed");
                }
                foreach (var d in defs.Where(d => d.Key.StartsWith(PoolCatalog.EnemyKeyPrefix))) pools.Register(d);
                pools.PrewarmAll();

                var enemies = new EnemyViewPresenter(pools, Settings);
                var pickups = new PickupPresenter(pools, Settings);
                var powerups = new PowerupPresenter(pools);
                var shots = new HostileShotPresenter(pools, Settings, null);
                var made = root.GetComponentsInChildren<Transform>(true).Length;
                try
                {
                    var view = enemies.CreateView(shark, new Vector3(4f, 0f, 4f), 1f, false, 1);
                    Assert.AreEqual(PoolCatalog.EnemyKeyPrefix + Catalog.Get("shark", Visual.Model).name, view.name, "the shark is its own prefab");
                    var barrel = (Transform)pickups.Acquire(PickupType.GoldBarrel, new Vector3(2f, 0f, 0f), 5);
                    Assert.AreEqual(PickupPresenter.KeyPrefix + Catalog.Get("gold_barrel", Visual.Model).name, barrel.name);
                    var gem = (Transform)pickups.Acquire(PickupType.FlyingXp, new Vector3(3f, 0f, 0f), 1);
                    Assert.AreEqual(PickupPresenter.KeyPrefix + Catalog.Get("xp_gem", Visual.Model).name, gem.name);
                    var powerup = (Transform)powerups.Create(PowerupKind.Tailwind, new Vector3(-2f, 0f, 0f));
                    Assert.AreEqual(Catalog.Get("tailwind", Visual.Model).GetComponent<Renderer>().sharedMaterial, powerup.GetComponent<Renderer>().sharedMaterial, "the powerup is its own prefab");
                    var shot = shots.Acquire(new Vector3(0f, 1f, 0f), Color.red, 0.3f);
                    Assert.AreEqual(HostileShotPresenter.KeyPrefix + Catalog.Get(VisualIds.Enemy, Visual.Shot).name, shot.name);
                    Assert.AreEqual(made, root.GetComponentsInChildren<Transform>(true).Length, "every instance came from the prewarm");
                    Assert.AreEqual(0, pools.TotalCreatedAfterPrewarm, "no pool grew past its prewarm");
                    shots.Release(shot);
                    powerups.Destroy(powerup);
                    pickups.Release(gem, PickupType.FlyingXp, 1);
                    pickups.Release(barrel, PickupType.GoldBarrel, 5);
                    enemies.Release(shark, view, null);
                }
                finally
                {
                    enemies.Dispose();
                    pickups.Dispose();
                    powerups.Dispose();
                    shots.Dispose();
                }

                // The tuning applies: a pickup's scale doubles with its prefab's VisualTuning.scale.
                var tuning = Catalog.Get("gold_barrel", Visual.Model).GetComponent<VisualTuning>();
                var old = tuning.scale;
                try
                {
                    var p = new PickupPresenter(pools, Settings);
                    var a = (Transform)p.Acquire(PickupType.GoldBarrel, Vector3.zero, 1);
                    var before = a.localScale.x;
                    p.Release(a, PickupType.GoldBarrel, 1);
                    tuning.scale = old * 2f;
                    var b = (Transform)p.Acquire(PickupType.GoldBarrel, Vector3.zero, 1);
                    Assert.AreEqual(before * 2f, b.localScale.x, 1e-4f, "tuning scale");
                    p.Release(b, PickupType.GoldBarrel, 1);
                }
                finally { tuning.scale = old; }
            }
            finally
            {
                Object.DestroyImmediate(shark);
                pools.Dispose();
                Object.DestroyImmediate(root);
            }
        }
    }
}
