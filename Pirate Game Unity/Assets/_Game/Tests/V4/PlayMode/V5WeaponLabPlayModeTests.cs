using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RoguePirates.V4.Config;
using RoguePirates.V4.DevTools.Scenarios;
using RoguePirates.V4.DevTools.WeaponTesting;
using RoguePirates.V4.Foundation.Balance;
using RoguePirates.V4.Foundation.Core;
using RoguePirates.V4.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RoguePirates.V4.Tests
{
    /// <summary>
    /// V5 Weapon Lab (owner, 22 Oct): the WeaponLab scene starts the lab's voyage, and its controller (driven here as the
    /// keys drive it) cycles every weapon, levels and evolves it, and streams the chosen enemy type at the ship.
    /// </summary>
    [Category("V5")]
    public sealed class V5WeaponLabPlayModeTests
    {
        const string LabScene = "Assets/_Game/Scenes/V4/WeaponLab.unity";
        V4TestSaveIsolation _isolation;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _isolation = new V4TestSaveIsolation();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return SceneManager.LoadSceneAsync(V4SceneNames.Boot, LoadSceneMode.Single);
            yield return null;
            Assert.IsNull(WeaponTestLab.Current, "the lab ended with its scene");
            StageBalanceProfileContext.ResetForTests();
            _isolation?.Dispose();
            _isolation = null;
        }

        static IEnumerator LoadLab()
        {
#if UNITY_EDITOR
            // The lab's scene is not in the build list (dev tools only): the Editor loads it by path.
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(LabScene, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("the Weapon Lab scene loads in the Editor only");
#endif
            var deadline = Time.realtimeSinceStartup + 40f;
            while (WeaponTestLab.Current == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsNotNull(WeaponTestLab.Current, "the WeaponLab scene started the lab");
            CollectionAssert.IsEmpty(Scenario.Current.Errors, "the lab's voyage set up without problems");
            yield return null;
        }

        static WeaponTestLab Lab => WeaponTestLab.Current;

        static IReadOnlyList<WeaponSlot> Slots()
        {
            Assert.IsTrue(Lab.Game.Context.TryGet<IWeaponManager>(out var weapons), "the weapon manager");
            return weapons.Slots;
        }

        [UnityTest]
        public IEnumerator Weapons_CycleAllWrap_LevelToMax_Evolve()
        {
            yield return LoadLab();
            var ids = Lab.Game.Config.All<WeaponConfig>().Select(w => w.weaponId).ToList();
            Assert.AreEqual(19, ids.Count, "the config has 19 weapons");
            CollectionAssert.AreEqual(ids, Lab.Weapons.Select(w => w.weaponId).ToList(), "the lab cycles them in the config's order");
            Assert.IsTrue(Lab.Game.Build.Progression.AutoLevelUp != null, "level-ups apply without cards");
            Assert.IsTrue(Scenario.Current.Ship.Damage != null, "the ship");

            // O: every weapon in turn, soloed at level 1; the 20th press wraps to the first.
            Assert.AreEqual(ids[0], Lab.CurrentWeaponId);
            for (var i = 1; i <= ids.Count; i++)
            {
                Lab.NextWeapon();
                yield return null;
                Assert.AreEqual(ids[i % ids.Count], Lab.CurrentWeaponId, $"press {i}");
                Assert.AreEqual(1, Slots().Count, "only that weapon is carried");
                Assert.AreEqual(Lab.CurrentWeaponId, Slots()[0].Config.weaponId, "the weapon manager fires it");
                Assert.AreEqual(1, Lab.Level, "at level 1");
            }
            Lab.PreviousWeapon();
            Assert.AreEqual(ids[ids.Count - 1], Lab.CurrentWeaponId, "I wraps back to the last");

            // U: up to its max, then it stays.
            Lab.SelectWeapon(0);
            for (var guard = 0; !Lab.AtMax && guard < 64; guard++) Lab.LevelUp();
            Assert.AreEqual(Lab.MaxLevel, Lab.Level, "levelled to max");
            Lab.LevelUp();
            Assert.AreEqual(Lab.MaxLevel, Lab.Level, "no level past max");
            StringAssert.Contains("max", Lab.OverlayText(), "the overlay says max");
            yield return null;
            Assert.AreEqual(Lab.MaxLevel, Slots()[0].Level, "the weapon manager has the level");

            // P: the first weapon with an evolution evolves; cycling away and back brings it fresh.
            var evolvable = Lab.Game.Config.All<WeaponEvolutionConfig>().Where(e => e.baseWeapon != null).Select(e => e.baseWeapon.weaponId).ToList();
            Assert.IsNotEmpty(evolvable, "some weapon has an evolution");
            var index = ids.FindIndex(id => evolvable.Contains(id));
            Lab.SelectWeapon(index);
            Assert.IsTrue(Lab.Evolve(), Lab.Message);
            yield return null;
            Assert.IsNotNull(Lab.EvolutionId, "evolved");
            Assert.IsNotNull(Slots()[0].Evolution, "the weapon manager fires the evolution");
            Assert.IsFalse(Lab.Evolve(), "a second evolve is refused");
            Lab.NextWeapon();
            Lab.PreviousWeapon();
            yield return null;
            Assert.IsNull(Lab.EvolutionId, "back to the weapon: not evolved");
            Assert.IsNull(Slots()[0].Evolution);
            var plain = ids.FindIndex(id => !evolvable.Contains(id));
            if (plain >= 0)
            {
                Lab.SelectWeapon(plain);
                Assert.IsFalse(Lab.Evolve(), "a weapon without an evolution");
                StringAssert.Contains("no evolution", Lab.Message);
            }

            // Every weapon fires for a moment with the stream on: no exceptions.
            for (var i = 0; i < ids.Count; i++)
            {
                Lab.SelectWeapon(i);
                for (var f = 0; f < 10; f++) yield return null;
            }
            Assert.IsFalse(RunManager.Instance.IsRunEnded, "the voyage is still on");
        }

        /// <summary>Owner (22 Oct): kills drop nothing and the ship takes no damage from anything, a boss included.</summary>
        [UnityTest]
        public IEnumerator NoDrops_NoDamage_WhileEnemiesAttack()
        {
            yield return LoadLab();
            Assert.IsTrue(Lab.KillRewardsOff, "the lab switched the kill rewards off");
            var game = Lab.Game;
            var ship = Scenario.Current.Ship;
            var run = RunManager.Instance;
            var hull = ship.Damage.CurrentHP;
            Assert.AreEqual(ship.Damage.MaxHP, hull, "the hull starts full");
            var damaged = 0;
            System.Action<float, bool> onDamaged = (a, c) => damaged++;
            ship.Damage.Damaged += onDamaged;
            var pickups = game.Build.Pickups.ActiveCount;
            var powerups = game.Build.Powerups.ActiveCount;
            var gold = run.Gold;
            var level = game.Build.Progression.Level;
            try
            {
                // A strong weapon among shooters, rammers, bombers and an elite, then the boss: kills, and every attack lands on the ship.
                var flamethrower = Lab.Weapons.ToList().FindIndex(w => w.weaponId == "flamethrower");
                Lab.SelectWeapon(flamethrower);
                for (var guard = 0; !Lab.AtMax && guard < 64; guard++) Lab.LevelUp();
                Lab.Evolve();
                var kills = run.KillCount;
                var sim = V4TestEnemies.Simulation();
                var shotsSeen = 0;
                var closest = float.MaxValue;
                foreach (var id in new[] { "gunner_cog", "rammer_raft", "bomb_boat", "elite_kraken_spawn", "barnacle_king" })
                {
                    var index = Lab.Enemies.ToList().FindIndex(e => e.Id == id);
                    Assert.GreaterOrEqual(index, 0, id + " is in the stream");
                    Lab.SelectEnemy(index);
                    var until = Time.time + (id == "barnacle_king" ? 10f : 5f);
                    while (Time.time < until)
                    {
                        Assert.AreEqual(hull, ship.Damage.CurrentHP, $"{id}: the hull never changes");
                        shotsSeen = Mathf.Max(shotsSeen, game.Shell.Enemies.Services.Shots.ActiveCount);
                        for (var i = 0; i < sim.Agents.Length; i++)
                            if (sim.Agents[i].active)
                            {
                                var d = sim.Agents[i].position - ship.Position;
                                d.y = 0f;
                                closest = Mathf.Min(closest, d.magnitude - sim.Agents[i].radius);
                            }
                        yield return null;
                    }
                }
                Assert.Greater(run.KillCount, kills, "the weapon sank some");
                Assert.Greater(shotsSeen, 0, "enemy shots were in flight at the ship");
                Assert.Less(closest, 3f, "enemies reached the ship (contact and rams)");
                Assert.AreEqual(0, damaged, "no Damaged event (no damage number or hit flash on the ship)");
                Assert.LessOrEqual(game.Build.Pickups.ActiveCount, pickups, "no XP, gold or health dropped");
                Assert.LessOrEqual(game.Build.Powerups.ActiveCount, powerups, "no powerup dropped");
                Assert.AreEqual(gold, run.Gold, "no gold");
                Assert.AreEqual(level, game.Build.Progression.Level, "no XP");
                Assert.IsTrue(ship.Damage.IsAlive && !run.IsRunEnded, "the voyage goes on");
            }
            finally { ship.Damage.Damaged -= onDamaged; }
        }

        [UnityTest]
        public IEnumerator Enemies_CycleTypes_StreamKeepsComing()
        {
            yield return LoadLab();
            Assert.Greater(Lab.Enemies.Count, 3, "the stream has the config's enemy types");
            Assert.IsTrue(Lab.Enemies.Any(e => e.Boss), "a boss is in the list");
            var sim = V4TestEnemies.Simulation();
            Assert.IsTrue(Lab.Game.Context.TryGet<RoguePirates.V4.Spawning.IEnemySpawnSink>(out var sink));

            // The stream sends the chosen type.
            var first = Lab.CurrentEnemyId;
            var deadline = Time.time + 4f;
            while (Lab.Alive < 3 && Time.time < deadline) yield return null;
            Assert.GreaterOrEqual(Lab.Alive, 3, $"{first} keeps coming");

            Lab.NextEnemy();
            var second = Lab.CurrentEnemyId;
            Assert.AreNotEqual(first, second, "L changes the type");
            yield return null;
            deadline = Time.time + 4f;
            while (Lab.Alive < 2 && Time.time < deadline) yield return null;
            var active = Enumerable.Range(0, sim.Agents.Length).Where(i => sim.Agents[i].active)
                .Select(i => sink.DefinitionId(sim.Agents[i].definitionIndex)).Distinct().ToList();
            CollectionAssert.Contains(active, sink.DefinitionId(Lab.CurrentEnemy.Definition), "the new type is spawned");
            CollectionAssert.DoesNotContain(active, sink.DefinitionId(Lab.Enemies.First(e => e.Id == first).Definition), "the old type is gone");

            // It keeps coming while the weapon sinks them.
            var spawned = Lab.StreamSpawned;
            yield return new WaitForSeconds(4f);
            Assert.Greater(Lab.StreamSpawned, spawned, "more came");

            // A boss: one at a time.
            var boss = Lab.Enemies.ToList().FindIndex(e => e.Boss);
            Lab.SelectEnemy(boss);
            deadline = Time.time + 6f;
            while (Lab.Alive < 1 && Time.time < deadline) yield return null;
            Assert.AreEqual(1, Lab.Alive, $"{Lab.CurrentEnemyId}: one boss");
            yield return new WaitForSeconds(2.5f);
            Assert.LessOrEqual(Lab.Alive, WeaponTestLab.BossCap);
            Lab.PreviousEnemy();
            Assert.AreNotEqual(boss, Lab.EnemyIndex, "K changes the type");
            Assert.IsFalse(RunManager.Instance.IsRunEnded, "the voyage is still on");
        }
    }
}
