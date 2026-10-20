using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RoguePirates.V4.Config;
using RoguePirates.V4.Foundation.Data;
using RoguePirates.V4.Foundation.Pickups;
using RoguePirates.V4.Foundation.UI;
using RoguePirates.V4.Presentation.Effects;
using RoguePirates.V4.Presentation.Hud;
using RoguePirates.V4.Presentation.Pvp;
using RoguePirates.V4.Presentation.Stage;
using RoguePirates.V4.Presentation.Visuals;
using RoguePirates.V4.Presentation.Weapons;
using RoguePirates.V4.Presentation.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RoguePirates.V4.Editor
{
    /// <summary>
    /// V5 (owner: "every non-weapon visual from an editable prefab, like the weapons"): builds one prefab per visual under
    /// Assets/_Game/V4/Visuals/Prefabs/&lt;Category&gt;/&lt;id&gt;/&lt;id&gt;_&lt;Visual&gt;.prefab and links them in the visual catalog
    /// (Visuals/Resources/V4Visuals.asset), which the presenters read by id, pool and prewarm. Each prefab is a full copy
    /// (unpacked) of what the game drew before (<see cref="VisualSources"/>: the juice settings' and VFX profiles' shared
    /// prefabs), or, for what the code built at run time (the hostile shot, rings and lines, the floor powerups, the
    /// placeholder cube, damage numbers, HUD arrows, the prompt, minimap glyphs, the 1v1's pieces), the same object saved
    /// with its materials, meshes and sprites as assets; so the look is unchanged. The art team's prefabs that already were
    /// single clean prefabs (enemy art models, the Demo/POI models, the bounds wall) are listed in the catalog as they are.
    /// Each built prefab gets a <see cref="VisualTuning"/> for the numbers the code applies. Missing scripts are stripped, and
    /// V3 scripts (the game's old Assembly-CSharp code) too. A prefab already on disk is kept (the owner's edits stay);
    /// Rebuild remakes them all. A prefab the owner adds as &lt;Category&gt;/&lt;id&gt;/&lt;id&gt;_&lt;Visual&gt;.prefab is linked
    /// too (an enemy's own Death or Highlight, say). Presentation only: no config changes. The list is
    /// plan/v5/VISUAL-PREFABS.md (<see cref="ListMarkdown"/>).
    /// </summary>
    public static class VisualPrefabs
    {
        public const string Root = "Assets/_Game/V4/Visuals";
        public const string Folder = Root + "/Prefabs";
        public const string SpritesFolder = Root + "/Sprites";
        public const string CatalogPath = Root + "/Resources/" + VisualCatalog.ResourcePath + ".asset";
        const string SettingsPath = "Assets/_Game/Resources/DemoJuiceSettings.asset";
        const string ConfigSetPath = "Assets/_Game/V4/Data/ConfigSets/SaltwindDefault.asset";
        const string BoundsWallPath = "Assets/_Game/Resources/V3PlayBoundsWallSegment.prefab";
        static readonly Color PvpRed = new Color(0.9f, 0.18f, 0.14f, 1f);

        [MenuItem("Tools/Rogue Pirates/Visuals/Build Visual Prefabs (missing ones)")]
        public static void BuildMissing() => Build(false);

        [MenuItem("Tools/Rogue Pirates/Visuals/Rebuild All Visual Prefabs (discards edits)")]
        public static void RebuildAll()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Rebuild visual prefabs",
                    "Remake every prefab under " + Folder + " from the current shared look? Edits to them are lost.", "Rebuild", "Cancel")) return;
            Build(true);
        }

        public static string PrefabPath(VisualCategory category, string id, string visual) => Folder + "/" + category + "/" + id + "/" + id + "_" + visual + ".prefab";

        /// <summary>The sources the builder and the tests read (settings, config set, profiles, HUD settings, the ship model).</summary>
        public static List<VisualSources.Source> Sources()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DemoJuiceSettings>(SettingsPath);
            var set = AssetDatabase.LoadAssetAtPath<ConfigSet>(ConfigSetPath);
            if (settings == null || set == null) return new List<VisualSources.Source>();
            using var config = new ConfigSetRuntime(set);
            return VisualSources.All(settings, config, VfxProfileLibrary.Load(), SaltWindHudSettings.Load(),
                AssetDatabase.LoadAssetAtPath<GameObject>(VisualSources.PlayerShipModel), AssetDatabase.LoadAssetAtPath<GameObject>(BoundsWallPath));
        }

        /// <summary>Builds the prefabs (missing ones, or all with <paramref name="overwrite"/>) and links the catalog;
        /// returns a log line per prefab.</summary>
        public static List<string> Build(bool overwrite)
        {
            var log = new List<string>();
            var settings = AssetDatabase.LoadAssetAtPath<DemoJuiceSettings>(SettingsPath);
            if (settings == null) { Debug.LogError("[VisualPrefabs] no " + SettingsPath); return log; }
            EnsureFolder(Folder);
            EnsureFolder(SpritesFolder);
            EnsureFolder(Root + "/Resources");
            var catalog = AssetDatabase.LoadAssetAtPath<VisualCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<VisualCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var entries = new List<VisualEntry>();
            if (catalog.entries != null) entries.AddRange(catalog.entries.Where(e => e != null && !string.IsNullOrEmpty(e.id) && !string.IsNullOrEmpty(e.visual)));
            void Link(VisualCategory category, string id, string visual, GameObject prefab)
            {
                var e = entries.FirstOrDefault(x => x.id == id && x.visual == visual);
                if (e == null) entries.Add(e = new VisualEntry { id = id, visual = visual });
                e.category = category;
                e.prefab = prefab;
            }

            _sprites = null;
            foreach (var source in Sources())
            {
                if (source.How == VisualSources.Build.Existing) { Link(source.Category, source.Id, source.Visual, source.Prefab); log.Add("listed " + AssetDatabase.GetAssetPath(source.Prefab) + " for " + source.Id + " " + source.Visual); continue; }
                EnsureFolder(Folder + "/" + source.Category + "/" + source.Id);
                var s = source;
                Link(source.Category, source.Id, source.Visual, Make(PrefabPath(source.Category, source.Id, source.Visual), overwrite, log, path => BuildFrom(s, settings, path),
                    source.How == VisualSources.Build.ShipLook, source.How == VisualSources.Build.Copy || source.How == VisualSources.Build.Gem || source.How == VisualSources.Build.PvpShip));
            }
            // The owner's own additions: any <Category>/<id>/<id>_<Visual>.prefab not in the sources.
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var parts = path.Substring(Folder.Length + 1).Split('/');
                if (parts.Length != 3 || !Enum.TryParse<VisualCategory>(parts[0], out var category)) continue;
                var id = parts[1];
                var name = System.IO.Path.GetFileNameWithoutExtension(parts[2]);
                if (!name.StartsWith(id + "_")) continue;
                var visual = name.Substring(id.Length + 1);
                if (entries.Any(e => e.id == id && e.visual == visual && e.prefab != null)) continue;
                Link(category, id, visual, AssetDatabase.LoadAssetAtPath<GameObject>(path));
                log.Add("linked the owner's " + path);
            }
            catalog.entries = entries.OrderBy(e => e.category).ThenBy(e => e.id, StringComparer.Ordinal).ThenBy(e => e.visual, StringComparer.Ordinal).ToArray();
            catalog.Reindex();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            foreach (var line in log) Debug.Log("[VisualPrefabs] " + line);
            return log;
        }

        // ---- Making one prefab ----------------------------------------------------------------------------------------

        static GameObject Make(string path, bool overwrite, List<string> log, Func<string, GameObject> build, bool variant, bool copy)
        {
            if (!overwrite)
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null) { log.Add("kept " + path); return existing; }
            }
            var root = build(path);
            if (root == null) { log.Add("SKIPPED " + path + " (no source)"); return null; }
            root.name = System.IO.Path.GetFileNameWithoutExtension(path);
            root.SetActive(true);
            if (!variant)
            {
                Clean(root, log, path);
                if (!copy) Persist(root, path); // a copy's assets already are assets (a VFX Graph's own materials are not ours to save)
                if (root.GetComponent<VisualTuning>() == null) root.AddComponent<VisualTuning>();
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) AssetDatabase.DeleteAsset(path);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            log.Add("built " + path);
            return saved;
        }

        static GameObject BuildFrom(VisualSources.Source source, DemoJuiceSettings s, string path)
        {
            switch (source.How)
            {
                case VisualSources.Build.Copy: return CopyOf(source.Prefab, source.Child);
                case VisualSources.Build.PlaceholderCube: return EnemyViewPresenter.BuildPlaceholderCube();
                case VisualSources.Build.HostileShot: return HostileShotPresenter.BuildTemplate(null);
                case VisualSources.Build.TelegraphRing:
                case VisualSources.Build.ItemRing: return Line(HazardRingPresenter.ConfigureRingLine);
                case VisualSources.Build.AuraPulse: return Line(WeaponVfxPresenter.ConfigurePulseLine);
                case VisualSources.Build.StormRing: return Line(StormRingPresenter.ConfigureLine);
                case VisualSources.Build.ActiveRing: return Line(CaptainActiveEffectPresenter.ConfigureRing);
                case VisualSources.Build.PvpRing: return Line(PvpMatchPresenter.ConfigureRing);
                case VisualSources.Build.MarkedTelegraph: return BossTelegraphPresenter.BuildCircle(out _);
                case VisualSources.Build.Gem: return PickupPresenter.GemTemplate(source.Prefab);
                case VisualSources.Build.HealthPlus: return HealthPlus(s);
                case VisualSources.Build.Powerup: return PowerupPresenter.BuildTemplate((PowerupKind)Enum.Parse(typeof(PowerupKind), source.Arg));
                case VisualSources.Build.ChargeRing: return ChargeRing();
                case VisualSources.Build.DamageNumber: return DamageNumberPresenter.BuildTemplate();
                case VisualSources.Build.StormEdgeWarning: return BoundsWallPresenter.BuildWarning(out _);
                case VisualSources.Build.ThreatArrow: return ThreatArrowPresenter.BuildArrow(MinimapUiSprites.Triangle).gameObject;
                case VisualSources.Build.Prompt: return InteractionPromptPresenter.BuildPanel(null);
                case VisualSources.Build.MinimapGlyph: return SnapshotMinimapPresenter.BuildGlyph((MinimapMarkerGraphics.GlyphKind)Enum.Parse(typeof(MinimapMarkerGraphics.GlyphKind), source.Arg)).gameObject;
                case VisualSources.Build.PvpBarrel: return PvpMatchPresenter.BuildBarrel(source.Arg == "xp");
                case VisualSources.Build.PvpBall: return PvpMatchPresenter.BuildBall(new Material(Shader.Find("Sprites/Default")));
                case VisualSources.Build.PvpArrow: return PvpMatchPresenter.BuildArrow(MinimapUiSprites.Triangle).gameObject;
                case VisualSources.Build.PvpShip: return PvpShip(source.Prefab);
                case VisualSources.Build.ShipLook: return ShipLook(source.Prefab, source.Arg);
                default: return null;
            }
        }

        /// <summary>An unpacked copy of an asset (or of one of its children, at its world pose as the asset drew it).</summary>
        static GameObject CopyOf(GameObject asset, string childPath)
        {
            if (asset == null) return null;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            if (instance == null) instance = Object.Instantiate(asset);
            if (PrefabUtility.IsPartOfPrefabInstance(instance))
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            if (childPath == null) return instance;
            var child = instance.transform.Find(childPath);
            if (child == null) { Object.DestroyImmediate(instance); return null; }
            child.SetParent(null, false); // the code spawns the child itself at the pose and scale it sets
            child.gameObject.SetActive(true);
            Object.DestroyImmediate(instance);
            return child.gameObject;
        }

        /// <summary>A line renderer as the code builds it, with a Sprites/Default material (saved by <see cref="Persist"/>).</summary>
        static GameObject Line(Action<LineRenderer> configure)
        {
            var go = new GameObject();
            var line = go.AddComponent<LineRenderer>();
            configure(line);
            line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            return go;
        }

        /// <summary>The Signal Buoy's charge disc with its light blue colour in its own material (the code tints a copy).</summary>
        static GameObject ChargeRing()
        {
            var ring = PoiMarkerPresenter.BuildChargeRing();
            var r = ring.GetComponent<Renderer>();
            r.sharedMaterial = new Material(r.sharedMaterial) { color = new Color(0.2f, 0.75f, 1f, 0.45f) };
            ring.transform.localPosition = Vector3.zero; // the code places it 0.05 m up under the buoy
            return ring;
        }

        /// <summary>The flying health plus: the billboard, its quad and a copy of the health burst material (the code makes
        /// its own coloured copy of the prefab's material once per pooled plus).</summary>
        static GameObject HealthPlus(DemoJuiceSettings s)
        {
            var go = PickupPresenter.FlyingHealthTemplate();
            var quad = (Mesh)typeof(FlyingHealthBillboard).GetMethod("CreateQuadMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var r = go.AddComponent<MeshRenderer>();
            if (s.healthBurstMaterial != null) r.sharedMaterial = new Material(s.healthBurstMaterial);
            return go;
        }

        /// <summary>The 1v1 opponent's ship: the player ship's model without scripts, colliders and physics, its health bar and
        /// aim parts hidden (as the code's copy of the player's model was).</summary>
        static GameObject PvpShip(GameObject model)
        {
            var root = CopyOf(model, null);
            if (root == null) return null;
            foreach (var b in root.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b);
            foreach (var c in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in root.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(r);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "HealthBar" || t.name == "Aim") t.gameObject.SetActive(false);
            var tuning = root.AddComponent<VisualTuning>();
            tuning.overrideColor = true;
            tuning.color = PvpRed;
            tuning.tintStrength = 0.7f;
            return root;
        }

        /// <summary>A Captain's ship look: a prefab variant of the ship model with the Captain's sail and flag (the HUD settings'
        /// swap, applied to the variant).</summary>
        static GameObject ShipLook(GameObject model, string captainKey)
        {
            var set = AssetDatabase.LoadAssetAtPath<ConfigSet>(ConfigSetPath);
            using var config = new ConfigSetRuntime(set);
            var captain = config.All<CaptainConfig>().FirstOrDefault(c => c != null && c.captainKey == captainKey);
            var hud = SaltWindHudSettings.Load();
            if (captain == null || hud == null || model == null) return null;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            hud.ApplyShipLook(instance.transform, captain.legacyId);
            return instance;
        }

        // ---- Assets the code made at run time ---------------------------------------------------------------------------

        /// <summary>Saves what the code made at run time beside the prefab: materials (renderers, lines, trails, text), meshes
        /// and the generated UI sprites (shared, in Visuals/Sprites).</summary>
        static void Persist(GameObject root, string prefabPath)
        {
            var saved = new Dictionary<Material, Material>();
            var n = 0;
            Material Save(Material m)
            {
                if (m == null || EditorUtility.IsPersistent(m)) return m;
                if (!saved.TryGetValue(m, out var asset))
                {
                    asset = SaveMaterial(new Material(m), MaterialPath(prefabPath, n++), m);
                    saved[m] = asset;
                }
                return asset;
            }
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetType().Name == "VFXRenderer") continue; // a VFX Graph makes its own
                var mats = r.sharedMaterials;
                var changed = false;
                for (var i = 0; i < mats.Length; i++)
                {
                    var asset = Save(mats[i]);
                    if (asset != mats[i]) { mats[i] = asset; changed = true; }
                }
                if (changed) r.sharedMaterials = mats;
            }
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                var m = t.fontSharedMaterial;
                var asset = Save(m);
                if (asset != m) t.fontSharedMaterial = asset;
            }
            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            {
                if (g is TMP_Text) continue;
                if (g.material != null && g.material != Graphic.defaultGraphicMaterial && !EditorUtility.IsPersistent(g.material)) g.material = Save(g.material);
                if (g is Image img && img.sprite != null && !EditorUtility.IsPersistent(img.sprite)) img.sprite = SpriteAsset(img.sprite);
            }
            var meshes = 0;
            foreach (var f in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null || EditorUtility.IsPersistent(f.sharedMesh)) continue;
                var mesh = Object.Instantiate(f.sharedMesh);
                var path = prefabPath.Replace(".prefab", meshes++ == 0 ? "_Mesh.asset" : "_Mesh_" + meshes + ".asset");
                if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mesh, path);
                f.sharedMesh = mesh;
            }
        }

        static string MaterialPath(string prefabPath, int n) => prefabPath.Replace(".prefab", n == 0 ? ".mat" : "_" + n + ".mat");

        static Material SaveMaterial(Material material, string path, Material source)
        {
            material.hideFlags = HideFlags.None;
            material.name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (source != null)
            {
                material.shaderKeywords = source.shaderKeywords;
                material.globalIlluminationFlags = source.globalIlluminationFlags;
                material.renderQueue = source.renderQueue;
            }
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Dictionary<Sprite, Sprite> _sprites;

        /// <summary>The generated minimap sprite (White, Circle, Diamond, Triangle) as a PNG asset, pixel for pixel.</summary>
        static Sprite SpriteAsset(Sprite runtime)
        {
            if (_sprites == null)
            {
                _sprites = new Dictionary<Sprite, Sprite>();
                foreach (var (name, sprite) in new[] { ("White", MinimapUiSprites.White), ("Circle", MinimapUiSprites.Circle), ("Diamond", MinimapUiSprites.Diamond), ("Triangle", MinimapUiSprites.Triangle) })
                    _sprites[sprite] = SaveSprite(sprite, SpritesFolder + "/Minimap" + name + ".png");
            }
            return _sprites.TryGetValue(runtime, out var asset) ? asset : runtime;
        }

        static Sprite SaveSprite(Sprite sprite, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            System.IO.File.WriteAllBytes(path, sprite.texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = sprite.pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---- Cleaning ---------------------------------------------------------------------------------------------------

        /// <summary>V3 scripts: the old game's code (RoguePirates namespaces other than V4, V5, UI and Hackathon, or a V3-named
        /// type). The art packs' own runtime scripts (the tornado's driver, the treasure map's pulse, Cartoon FX) are kept:
        /// they play the art.</summary>
        public static bool IsV3Script(MonoBehaviour b)
        {
            if (b == null) return false;
            var type = b.GetType();
            var ns = type.Namespace ?? "";
            if (ns.StartsWith("RoguePirates.V4") || ns.StartsWith("RoguePirates.V5") || ns.StartsWith("RoguePirates.UI") || ns.StartsWith("RoguePirates.Hackathon")) return false;
            return ns.StartsWith("RoguePirates") || type.Name.StartsWith("V3") || ns.Contains("V3");
        }

        static void Clean(GameObject root, List<string> log, string path)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var missing = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                if (missing > 0) log.Add("  " + path + ": removed " + missing + " missing script(s) on " + t.name);
                foreach (var b in t.GetComponents<MonoBehaviour>())
                {
                    if (!IsV3Script(b)) continue;
                    log.Add("  " + path + ": removed " + b.GetType().FullName + " on " + t.name + " (V3)");
                    Object.DestroyImmediate(b);
                }
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        // ---- The list (plan/v5/VISUAL-PREFABS.md) ----------------------------------------------------------------------

        /// <summary>Every visual's prefab as Markdown tables per category and owner: visual, prefab, made from, what the
        /// code still sets.</summary>
        public static string ListMarkdown()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VisualCatalog>(CatalogPath);
            var sources = Sources();
            var sb = new StringBuilder();
            foreach (var group in sources.GroupBy(x => x.Category).OrderBy(g => g.Key))
            {
                sb.AppendLine("### " + group.Key).AppendLine();
                sb.AppendLine("| Owner | Visual | Prefab | Made from | Code sets at run time |").AppendLine("|---|---|---|---|---|");
                foreach (var x in group.OrderBy(x => x.Id, StringComparer.Ordinal))
                {
                    var prefab = catalog != null ? catalog.Get(x.Id, x.Visual) : null;
                    sb.AppendLine("| " + x.Id + " | " + x.Visual + " | `" + (prefab != null ? AssetDatabase.GetAssetPath(prefab) : "(none)") + "` | " + x.MadeFrom(AssetDatabase.GetAssetPath) + " | " + x.Code + " |");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
