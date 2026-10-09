using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace TacticalZero.Tests
{
    public sealed class CombatRulesTests
    {
        [Test]
        public void GeneratedWeaponsAreValidAndDistinct()
        {
            var catalog = Resources.Load<PrototypeCatalog>("PrototypeCatalog");
            Assert.IsNotNull(catalog, "Run Tactical Zero/Setup Complete 3D Prototype first.");
            Assert.AreEqual(3, catalog.Weapons.Length);
            foreach (var gun in catalog.Weapons)
            {
                Assert.IsTrue(gun.Valid);
                Assert.IsNotNull(gun.Model);
                Assert.IsNotNull(gun.ShotAudio);
            }

            Assert.AreEqual(FireMode.SemiAutomatic, catalog.Weapons[0].Mode);
            Assert.AreEqual(FireMode.Automatic, catalog.Weapons[1].Mode);
            Assert.AreEqual(8, catalog.Weapons[2].Pellets);
        }

        [Test]
        public void AmmoRespectsRateAndNeverGoesNegative()
        {
            var a = new AmmoState(2, 4);
            Assert.IsTrue(a.TryFire(0, .2f));
            Assert.IsFalse(a.TryFire(.1f, .2f));
            Assert.IsTrue(a.TryFire(.2f, .2f));
            Assert.IsFalse(a.TryFire(1, .2f));
            Assert.AreEqual(0, a.Loaded);
        }

        [Test]
        public void ReloadWaitsAndTransfersOnlyAvailableRounds()
        {
            var a = new AmmoState(3, 1);
            a.TryFire(0, .1f);
            a.TryFire(.1f, .1f);
            Assert.IsTrue(a.StartReload(2));
            Assert.IsFalse(a.TryFire(5, .1f));
            a.Tick(1);
            Assert.AreEqual(1, a.Loaded);
            a.Tick(1);
            Assert.AreEqual(2, a.Loaded);
            Assert.AreEqual(0, a.Reserve);
        }

        [Test]
        public void CancelledReloadDoesNotCreateAmmo()
        {
            var a = new AmmoState(3, 3);
            a.TryFire(0, .1f);
            a.StartReload(1);
            a.CancelReload();
            a.Tick(5);
            Assert.AreEqual(2, a.Loaded);
            Assert.AreEqual(3, a.Reserve);
        }

        [Test]
        public void DamageAndHeadMultiplierUseDistanceFalloff()
        {
            Assert.AreEqual(60, DamageModel.Calculate(30, true, 2, 1, 50, false));
            Assert.AreEqual(15, DamageModel.Calculate(15, false, 2, 0, 20, true));
            Assert.AreEqual(3.75f, DamageModel.Calculate(15, false, 2, 30, 20, true));
        }

        [Test]
        public void ArmorAbsorbsOnlyItsAvailablePool()
        {
            float armor = 20;
            Assert.AreEqual(30, DamageModel.Absorb(50, ref armor));
            Assert.AreEqual(0, armor);
            Assert.AreEqual(50, DamageModel.Absorb(50, ref armor));
        }

        [Test]
        public void SettingsRoundTripAndCorruptFallback()
        {
            var d = new SettingsData
            {
                invertY = true,
                aimAssist = 0,
                frameLimit = 30,
                touchSensitivity = 3
            };
            var copy = SaveService.Decode(SaveService.Encode(d));
            Assert.IsTrue(copy.invertY);
            Assert.AreEqual(0, copy.aimAssist);
            Assert.AreEqual(30, copy.frameLimit);
            Assert.AreEqual(3, copy.touchSensitivity);
            Assert.IsNotNull(SaveService.Decode("broken"));
        }

        [Test]
        public void SaveSettingsCanPersistAndReload()
        {
            bool had = PlayerPrefs.HasKey(SaveService.SettingsKey);
            string old = PlayerPrefs.GetString(SaveService.SettingsKey);
            try
            {
                SaveService.Load();
                SaveService.Settings.touchSensitivity = 3.4f;
                Assert.IsTrue(SaveService.Save());
                SaveService.Load();
                Assert.AreEqual(3.4f, SaveService.Settings.touchSensitivity);
            }
            finally
            {
                if (had)
                    PlayerPrefs.SetString(SaveService.SettingsKey, old);
                else
                    PlayerPrefs.DeleteKey(SaveService.SettingsKey);
                PlayerPrefs.Save();
                SaveService.Load();
            }
        }

        [TestCase(false, false, 12, true)]
        [TestCase(true, false, 12, false)]
        [TestCase(true, false, 2, true)]
        [TestCase(true, true, 12, false)]
        public void GrassAndWallsGateVision(bool grass, bool blocked, float distance, bool expected)
        {
            Assert.AreEqual(expected, VisibilityRules.CanSee(distance, 0, blocked, 22, 50, grass, false, false, false, false));
        }

        [Test]
        public void MovingAndFiringReduceConcealment()
        {
            Assert.IsTrue(VisibilityRules.CanSee(10, 0, false, 22, 50, true, false, false, true, false));
            Assert.IsTrue(VisibilityRules.CanSee(15, 0, false, 22, 50, true, false, false, false, true));
        }

        [Test]
        public void HeardGunshotInvestigatesWithoutVisualLock()
        {
            var m = new EnemyStateMachine();
            m.Tick(false, false, 1);
            m.Hear();
            Assert.AreEqual(EnemyState.Suspicious, m.State);
            m.Tick(false, false, .5f);
            Assert.AreEqual(EnemyState.Investigate, m.State);
        }

        [Test]
        public void LostSightSearchesThenReturns()
        {
            var m = new EnemyStateMachine();
            m.Tick(true, false, .2f);
            m.Tick(false, false, .2f);
            Assert.AreEqual(EnemyState.Search, m.State);
            m.Tick(false, false, 8);
            Assert.AreEqual(EnemyState.Return, m.State);
            m.Tick(false, true, .1f);
            Assert.AreEqual(EnemyState.Patrol, m.State);
        }

        [Test]
        public void DeadAIHasNoTransitions()
        {
            var m = new EnemyStateMachine();
            m.Kill();
            m.Hear();
            m.Hit();
            m.Tick(true, true, 99);
            Assert.AreEqual(EnemyState.Dead, m.State);
        }

        [Test]
        public void ExtractionRequiresIntelAndContinuousPresence()
        {
            var o = new ObjectiveProgress();
            Assert.IsFalse(o.Tick(true, 6));
            o.Acquire();
            Assert.IsFalse(o.Tick(true, 3));
            o.Tick(false, .1f);
            Assert.AreEqual(0, o.ExtractProgress);
            Assert.IsTrue(o.Tick(true, 5));
        }

        [Test]
        public void RepeatedSetupPreservesUserAsset()
        {
            TacticalZero.Editor.TacticalZeroProjectSetup.Setup();
            var gun = Resources.Load<PrototypeCatalog>("PrototypeCatalog").Weapons[0];
            string path = AssetDatabase.GetAssetPath(gun), before = System.IO.File.ReadAllText(path);
            int sceneCount = EditorBuildSettings.scenes.Length;
            TacticalZero.Editor.TacticalZeroProjectSetup.Setup();
            Assert.AreEqual(before, System.IO.File.ReadAllText(path));
            Assert.AreEqual(sceneCount, EditorBuildSettings.scenes.Length);
        }
    }
}
