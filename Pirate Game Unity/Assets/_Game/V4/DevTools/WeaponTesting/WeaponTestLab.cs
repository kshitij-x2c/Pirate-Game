using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoguePirates.V4.Config;
using RoguePirates.V4.DevTools.Scenarios;
using RoguePirates.V4.Enemies;
using RoguePirates.V4.Foundation.Core;
using RoguePirates.V4.Foundation.World;
using RoguePirates.V4.Spawning;
using RoguePirates.V4.Stage;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoguePirates.V4.DevTools.WeaponTesting
{
    /// <summary>
    /// V5 Weapon Lab (owner, 22 Oct): a real voyage (the Scenario Lab's load of the run scene, as a Set Sail) with the
    /// ship invulnerable, no cards, no timeline spawns and no boss, where the owner cycles every weapon in the config
    /// (I / O: Solo at level 1), levels it (U) and evolves it (P) while a constant stream of one enemy type (K / L) comes
    /// at the ship from the normal spawn distance. Every change goes through the dev hooks the Tuning Console uses
    /// (<see cref="WeaponLab"/>: Solo, RankUp, Evolve) and the enemy sink's spawn (the Module Lab's). Dev tools only:
    /// the scene that starts it (Scenes/V4/WeaponLab) is not in the build list.
    /// </summary>
    public sealed class WeaponTestLab : MonoBehaviour
    {
        /// <summary>The lab now running (null when none).</summary>
        public static WeaponTestLab Current { get; private set; }

        /// <summary>One enemy type the stream can send.</summary>
        public struct EnemyEntry
        {
            public string Id, Name, Kind;
            public int Definition;
            public bool Elite, Boss, Miniboss;
        }

        public const float DefaultRate = 3f;
        /// <summary>Metres from the ship a boss or miniboss of the stream rises at.</summary>
        public const float BossDistance = 12f;
        public const int OrdinaryCap = 30, EliteCap = 6, MinibossCap = 2, BossCap = 1;
        static readonly float[] Rates = { 0.25f, 0.5f, 1f, 2f, 3f, 5f, 8f, 12f };

        public V4Game Game { get; private set; }
        public IReadOnlyList<WeaponConfig> Weapons => _weapons;
        public IReadOnlyList<EnemyEntry> Enemies => _enemies;
        /// <summary>Enemy ids in the config that are not in the run database (no standalone spawn) and why.</summary>
        public readonly List<string> Skipped = new List<string>();
        public int WeaponIndex { get; private set; }
        public int EnemyIndex { get; private set; }
        public bool StreamOn { get; set; } = true;
        public bool HelpVisible { get; set; } = true;
        public float Rate => Rates[_rateIndex];
        /// <summary>Units the stream spawned (all types, since the lab started).</summary>
        public int StreamSpawned { get; private set; }
        /// <summary>The last action's note (max level, no evolution...).</summary>
        public string Message { get; private set; } = "";

        WeaponConfig CurrentWeapon => _weapons.Count > 0 ? _weapons[WeaponIndex] : null;
        public string CurrentWeaponId => CurrentWeapon?.weaponId;
        public string CurrentEnemyId => _enemies.Count > 0 ? _enemies[EnemyIndex].Id : null;
        public EnemyEntry CurrentEnemy => _enemies.Count > 0 ? _enemies[EnemyIndex] : default;

        readonly List<WeaponConfig> _weapons = new List<WeaponConfig>();
        readonly List<EnemyEntry> _enemies = new List<EnemyEntry>();
        IEnemySpawnSink _sink;
        IEnemyDevControl _dev;
        RoguePirates.V4.Foundation.Player.PlayerShipController _ship;
        int _rateIndex = System.Array.IndexOf(Rates, DefaultRate);
        float _spawnBudget, _spawnDistance = 24f;
        int _failedSpawns;
        readonly System.Random _rng = new System.Random(1);
        Console.TuningConsole _console;
        GUIStyle _style;
        Texture2D _back;

        // ---- Loading -----------------------------------------------------------------------------------------------

        /// <summary>The run the lab plays in: a regular voyage (seed 1, the saved Captain, playtest switches off) with
        /// the ship invulnerable, no cards, a quiet timeline (no ambient bands, hordes, focus phases, overtime or storm)
        /// and the first weapon in the config.</summary>
        public static ScenarioConfig CreateScenario()
        {
            var s = ScriptableObject.CreateInstance<ScenarioConfig>();
            s.name = "weapon_lab";
            s.description = "Weapon Lab: one weapon, a stream of one enemy type.";
            s.seed = 1;
            s.invulnerable = true;
            s.skipLevelUpCards = true;
            s.ambientSpawning = false;
            s.scriptedPhases = ScenarioConfig.ScriptedPhaseMode.SkipAll;
            s.boss = ScenarioConfig.BossSummon.None;
            return s;
        }

        /// <summary>Loads the lab's run and starts the lab in it (PlayMode tests and the WeaponLab scene's boot).</summary>
        public static IEnumerator Load()
        {
            yield return Scenario.Load(CreateScenario());
            var runner = Scenario.Current;
            if (runner == null || runner.Game == null) throw new System.InvalidOperationException("Weapon Lab: the run did not start");
            runner.gameObject.AddComponent<WeaponTestLab>().Begin(runner.Game);
        }

        public void Begin(V4Game game)
        {
            Game = game;
            Current = this;
            _ship = Scenario.Current != null ? Scenario.Current.Ship : FindFirstObjectByType<RoguePirates.V4.Foundation.Player.PlayerShipController>();
            game.Context.TryGet(out _sink);
            game.Context.TryGet(out _dev);
            var config = game.Config;
            _weapons.AddRange(config.All<WeaponConfig>().Where(w => w != null && !string.IsNullOrEmpty(w.weaponId)));
            var calibration = config.Single<BalanceTargetsConfig>()?.calibration;
            if (calibration != null && calibration.spawnDistance > 1f) _spawnDistance = calibration.spawnDistance;
            BuildEnemyList(config);
            QuietTheAimOverlayToggle();
            SelectWeapon(0);
            SelectEnemy(0);
            Debug.Log($"[V5 Weapon Lab] {_weapons.Count} weapons, {_enemies.Count} enemy types" +
                      (Skipped.Count > 0 ? $"; skipped: {string.Join(", ", Skipped)}" : ""));
        }

        void BuildEnemyList(ConfigSetRuntime config)
        {
            var seen = new HashSet<string>();
            void Add(string id, string name, string kind, bool registryElite)
            {
                if (string.IsNullOrEmpty(id) || !seen.Add(id)) return;
                var definition = _sink != null ? _sink.FindDefinition(id) : -1;
                if (definition < 0) { Skipped.Add($"{id} ({kind}: not in the run's enemy database)"); return; }
                var boss = _sink.DefinitionIsBoss(definition);
                var miniboss = !boss && _sink.DefinitionIsMiniboss(definition);
                _enemies.Add(new EnemyEntry
                {
                    Id = id, Name = string.IsNullOrEmpty(name) ? id : name, Definition = definition, Elite = registryElite,
                    Boss = boss, Miniboss = miniboss,
                    Kind = boss ? "boss" : miniboss ? "miniboss" : registryElite ? "elite" : kind,
                });
            }
            foreach (var a in config.All<EnemyArchetypeConfig>()) Add(a.enemyId, a.displayName, "enemy", false);
            foreach (var v in config.All<EnemyVariantConfig>().Where(v => v.baseArchetype != null))
                Add(v.variantId, v.displayName, v.kind == VariantKind.RegistryElite ? "elite" : "variant", v.kind == VariantKind.RegistryElite);
            // Ordinary first, then elites, minibosses and bosses (each in the config's order).
            var ordered = _enemies.OrderBy(e => e.Boss ? 3 : e.Miniboss ? 2 : e.Elite ? 1 : 0).ToList();
            _enemies.Clear();
            _enemies.AddRange(ordered);
        }

        /// <summary>O is the lab's next weapon; the weapon aim overlay's toggle keeps V and backslash.</summary>
        void QuietTheAimOverlayToggle()
        {
            var root = FindFirstObjectByType<RoguePirates.V4.Presentation.V4PresentationRoot>();
            if (root == null || root.AimOverlay == null) return;
            root.AimOverlay.TogglePressed = () =>
            {
                var k = Keyboard.current;
                return k != null && (k.vKey.wasPressedThisFrame || k.backslashKey.wasPressedThisFrame);
            };
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            if (_back != null) Destroy(_back);
        }

        // ---- Weapons -----------------------------------------------------------------------------------------------

        public void NextWeapon() => SelectWeapon(WeaponIndex + 1);
        public void PreviousWeapon() => SelectWeapon(WeaponIndex - 1);

        /// <summary>Solo the weapon at this index (wrapping) at level 1, not evolved.</summary>
        public void SelectWeapon(int index)
        {
            if (_weapons.Count == 0) return;
            WeaponIndex = ((index % _weapons.Count) + _weapons.Count) % _weapons.Count;
            var id = CurrentWeaponId;
            WeaponLab.SetLoadout(Game, new string[0]); // dropped first: it comes back fresh (level 1, no upgrades)
            WeaponLab.ClearEvolution(Game, id);
            WeaponLab.Solo(Game, id);
            Message = "";
        }

        public int Level => Game?.Build?.Progression?.WeaponLevel(CurrentWeaponId) ?? 0;
        public int MaxLevel => Mathf.Max(1, CurrentWeapon != null ? CurrentWeapon.maxLevel : 1);
        public bool AtMax => Level >= MaxLevel;

        public void LevelUp()
        {
            if (AtMax) { Message = "max level"; return; }
            WeaponLab.RankUp(Game, 0);
            Message = AtMax ? "max level" : "";
        }

        /// <summary>The evolution the config has for the current weapon, or null.</summary>
        public WeaponEvolutionConfig EvolutionConfig =>
            Game?.Config?.All<WeaponEvolutionConfig>().FirstOrDefault(e => e.baseWeapon != null && e.baseWeapon.weaponId == CurrentWeaponId);

        public string EvolutionId => Game?.Build?.Progression?.EvolutionOf(CurrentWeaponId);

        /// <summary>Evolves the current weapon as its evolution card does (the console's Evolve). The lab does not ask for
        /// the game's conditions (max level and the evolution's item): returns false with the reason when it cannot.</summary>
        public bool Evolve()
        {
            var evolution = EvolutionConfig;
            if (evolution == null) { Message = "no evolution for this weapon in the config"; return false; }
            if (EvolutionId != null) { Message = "already evolved"; return false; }
            WeaponLab.Evolve(Game, CurrentWeaponId, evolution.evolutionId);
            Message = EvolutionId != null ? $"evolved (in the game: max level + item {evolution.requiredItemId})" : "the evolution was refused";
            return EvolutionId != null;
        }

        /// <summary>R: clears the enemies and puts the current weapon back to level 1.</summary>
        public void ResetLab()
        {
            ClearEnemies();
            SelectWeapon(WeaponIndex);
            Message = "reset";
        }

        // ---- Enemy stream ------------------------------------------------------------------------------------------

        public void NextEnemy() => SelectEnemy(EnemyIndex + 1);
        public void PreviousEnemy() => SelectEnemy(EnemyIndex - 1);

        /// <summary>The stream sends this type from now on; the previous type's units are removed.</summary>
        public void SelectEnemy(int index)
        {
            if (_enemies.Count == 0) return;
            EnemyIndex = ((index % _enemies.Count) + _enemies.Count) % _enemies.Count;
            ClearEnemies();
            _spawnBudget = 1f; // the first one comes at once
            _failedSpawns = 0;
        }

        public void ClearEnemies()
        {
            if (_dev == null) return;
            _dev.DespawnAll();
            // A boss removed this way was never sunk: the stage forgets it (as the Module Lab's Clear all).
            if (Game.Context.TryGet<IStageContent>(out var stage)) { stage.Module.ForgetDespawnedBoss(); stage.Module.ForgetDespawnedPowderCaches(); }
        }

        public int Cap => CapOf(CurrentEnemy);
        static int CapOf(EnemyEntry e) => e.Boss ? BossCap : e.Miniboss ? MinibossCap : e.Elite ? EliteCap : OrdinaryCap;
        /// <summary>The stream's rate: bosses and minibosses at most one every 2 s.</summary>
        float EffectiveRate => CurrentEnemy.Boss || CurrentEnemy.Miniboss ? Mathf.Min(Rate, 0.5f) : Rate;
        public int Alive => _sink != null && _enemies.Count > 0 ? _sink.CountActive(CurrentEnemy.Definition) : 0;

        public void FasterStream() => _rateIndex = Mathf.Min(Rates.Length - 1, _rateIndex + 1);
        public void SlowerStream() => _rateIndex = Mathf.Max(0, _rateIndex - 1);

        /// <summary>One unit of the current type at the normal spawn distance (bosses and minibosses nearer), at a random bearing in open water.</summary>
        public bool SpawnOne()
        {
            if (_sink == null || _ship == null || _enemies.Count == 0) return false;
            var e = CurrentEnemy;
            var clearance = _sink.DefinitionRadius(e.Definition) + 0.35f;
            // Bosses and minibosses rise on screen (as from their whirlpool in the game); the rest come from off screen.
            var distance = e.Boss || e.Miniboss ? Mathf.Min(_spawnDistance, BossDistance) : _spawnDistance;
            for (var attempt = 0; attempt < 12; attempt++)
            {
                var bearing = (float)_rng.NextDouble() * 360f; // its own stream: the run's seeded Unity Random is left alone
                var p = _ship.Position + Quaternion.AngleAxis(bearing, Vector3.up) * Vector3.forward * distance;
                p.y = 0f;
                if (PlayBounds.IsConfigured) p = PlayBounds.Clamp(p, 2f);
                if (PlayBounds.IsConfigured && !PlayBounds.IsOpenWater(p, clearance)) continue;
                var slot = _sink.Spawn(e.Definition, p, e.Elite ? ConfigEnums.EliteAffix.Rich : ConfigEnums.EliteAffix.None, 1f, 1f, 1f);
                if (slot < 0) continue;
                StreamSpawned++;
                return true;
            }
            _failedSpawns++;
            return false;
        }

        // ---- Frame -------------------------------------------------------------------------------------------------

        void Update()
        {
            var run = RunManager.Instance;
            if (Game == null || run == null || run.IsRunEnded) return;
            HandleKeys();
            SkipChestChoice(run);
            if (run.IsPaused || !StreamOn || _enemies.Count == 0) return;
            _spawnBudget = Mathf.Min(_spawnBudget + EffectiveRate * Time.deltaTime, 3f);
            while (_spawnBudget >= 1f && Alive < Cap)
            {
                _spawnBudget -= 1f;
                if (!SpawnOne()) break;
            }
        }

        /// <summary>A salvage chest's choice pauses the game (items would change the weapon under test): the lab skips
        /// it as the trace pilot does (R in the game).</summary>
        void SkipChestChoice(RunManager run)
        {
            if (run.PauseReason != PauseReason.POIChoice || !Game.Context.TryGet<IStageContent>(out var stage) || stage.PromptTarget < 0) return;
            stage.SkipChest();
            ChestsSkipped++;
        }

        /// <summary>Salvage chest choices the lab skipped.</summary>
        public int ChestsSkipped { get; private set; }

        void HandleKeys()
        {
            var k = Keyboard.current;
            if (k == null) return;
            var console = TuningConsoleOpen();
            if (console) return; // typing in the console's search box
            if (k.iKey.wasPressedThisFrame) PreviousWeapon();
            if (k.oKey.wasPressedThisFrame) NextWeapon();
            if (k.uKey.wasPressedThisFrame) LevelUp();
            if (k.pKey.wasPressedThisFrame) Evolve();
            if (k.kKey.wasPressedThisFrame) PreviousEnemy();
            if (k.lKey.wasPressedThisFrame) NextEnemy();
            if (k.leftBracketKey.wasPressedThisFrame) SlowerStream();
            if (k.rightBracketKey.wasPressedThisFrame) FasterStream();
            if (k.rKey.wasPressedThisFrame) ResetLab();
            if (k.hKey.wasPressedThisFrame) HelpVisible = !HelpVisible;
        }

        bool TuningConsoleOpen()
        {
            if (_console == null) _console = FindFirstObjectByType<Console.TuningConsole>();
            return _console != null && _console.IsOpen;
        }

        // ---- Overlay -----------------------------------------------------------------------------------------------

        /// <summary>The overlay's text (also read by tests).</summary>
        public string OverlayText()
        {
            var sb = new System.Text.StringBuilder();
            var w = CurrentWeapon;
            sb.Append("<b>WEAPON LAB</b>\n");
            if (w != null)
            {
                sb.Append($"Weapon {WeaponIndex + 1}/{_weapons.Count}: <b>{(string.IsNullOrEmpty(w.displayName) ? w.weaponId : w.displayName)}</b> ({w.weaponId}, {w.behavior})\n");
                sb.Append($"Level {Level}/{MaxLevel}{(AtMax ? "  <b>max</b>" : "")}\n");
                var evo = EvolutionConfig;
                var evolved = EvolutionId;
                if (evolved != null) sb.Append($"Evolved: <b>{(evo != null && !string.IsNullOrEmpty(evo.displayName) ? evo.displayName : evolved)}</b>\n");
                else sb.Append(evo != null ? $"Evolution: {evo.displayName} (P)\n" : "Evolution: none in the config\n");
            }
            if (_enemies.Count > 0)
            {
                var e = CurrentEnemy;
                sb.Append($"Enemy {EnemyIndex + 1}/{_enemies.Count}: <b>{e.Name}</b> ({e.Id}, {e.Kind})\n");
                sb.Append($"Alive {Alive}/{Cap} · {EffectiveRate:0.##}/s{(StreamOn ? "" : " (off)")}{(_failedSpawns > 0 ? $" · {_failedSpawns} spawns found no water" : "")}\n");
            }
            if (!string.IsNullOrEmpty(Message)) sb.Append($"<color=#ffd75e>{Message}</color>\n");
            if (HelpVisible)
                sb.Append("<size=13>I / O weapon  ·  U level up  ·  P evolve\nK / L enemy  ·  [ / ] stream rate  ·  R reset  ·  H help</size>");
            else sb.Append("<size=13>H help</size>");
            return sb.ToString();
        }

        /// <summary>Below the HUD's Captain portrait, health bar and pause button (top-left at 1920x1080).</summary>
        const float OverlayTop = 330f;

        void OnGUI()
        {
            if (Game == null) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true, wordWrap = false };
                _style.normal.textColor = Color.white;
                _back = new Texture2D(1, 1);
                _back.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
                _back.Apply();
            }
            var text = OverlayText();
            var content = new GUIContent(text);
            var size = _style.CalcSize(content);
            var rect = new Rect(12f, OverlayTop * Screen.height / 1080f, size.x + 20f, size.y + 12f);
            GUI.DrawTexture(rect, _back);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 6f, size.x, size.y), content, _style);
        }
    }
}
