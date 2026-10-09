using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace TacticalZero.Tests
{
    public sealed class OutpostPlayTests
    {
        private GameManager game;
        [UnitySetUp]
        public IEnumerator OpenOutpost()
        {
            Assert.IsNotNull(Resources.Load<PrototypeCatalog>("PrototypeCatalog"), "Run setup before tests.");
            yield return SceneManager.LoadSceneAsync("Outpost01");
            yield return null;
            game = GameManager.Instance;
            Assert.IsNotNull(game.Player);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (game)
            {
                game.MainMenu();
                while (game.Phase == GamePhase.Loading)
                    yield return null;
            }

            yield return null;
        }

        private void Warp(Vector3 position)
        {
            var controller = game.Player.GetComponent<CharacterController>();
            controller.enabled = false;
            game.Player.transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator PlayerMovesAndCannotCrossWall()
        {
            var controller = game.Player.GetComponent<FPSPlayerController>();
            float start = game.Player.transform.position.z;
            controller.Simulate(Vector2.up, false, false, .1f);
            Assert.Greater(game.Player.transform.position.z, start);
            Warp(new Vector3(0, .05f, -30));
            for (int i = 0; i < 100; i++)
                controller.Simulate(Vector2.down, false, false, .05f);
            Assert.Greater(game.Player.transform.position.z, -31.5f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WeaponRayDamagesEnemyAndEnemyDies()
        {
            var enemy = Object.FindFirstObjectByType<EnemyHealth>();
            enemy.GetComponent<EnemyBrain>().enabled = false;
            var nav = enemy.GetComponent<NavMeshAgent>();
            nav.enabled = false;
            enemy.transform.position = new Vector3(0, 0, -18);
            Warp(new Vector3(0, .05f, -23));
            game.Player.transform.rotation = Quaternion.identity;
            var view = game.Player.GetComponent<PlayerLook>().View;
            view.transform.LookAt(enemy.AimPoint);
            Physics.SyncTransforms();
            float before = enemy.Health;
            Assert.IsTrue(game.Player.GetComponent<WeaponController>().TryShoot(true));
            Assert.Less(enemy.Health, before);
            enemy.Apply(new DamageInfo(1000, game.Player.transform.position, "测试", "USP"));
            Assert.IsFalse(enemy.Alive);
            Assert.AreEqual(EnemyState.Dead, enemy.GetComponent<EnemyBrain>().Machine.State);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WallsBlockVisionAndMuzzleShots()
        {
            var enemy = Object.FindFirstObjectByType<EnemyHealth>();
            enemy.GetComponent<EnemyBrain>().enabled = false;
            enemy.GetComponent<NavMeshAgent>().enabled = false;
            enemy.transform.position = new Vector3(-12, 0, 16);
            enemy.transform.rotation = Quaternion.Euler(0, 90, 0);
            Warp(new Vector3(-8, .05f, 16));
            Physics.SyncTransforms();
            var data = ScriptableObject.CreateInstance<EnemyDefinition>();
            Assert.IsFalse(enemy.GetComponent<EnemyPerception>().Sees(game.Player, game.Player.GetComponent<PlayerVisibilityState>(), data));
            Assert.IsTrue(HitscanSolver.Trace(new Vector3(-12, 1, 16), new Vector3(-9, 1, 16), Vector3.right, 30, LayerMask.GetMask("TZWorld", "TZEnemy"), out var hit));
            Assert.AreEqual(LayerMask.NameToLayer("TZWorld"), hit.collider.gameObject.layer);
            Object.Destroy(data);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PatrolEntersCombatThenSearchWhenPlayerConceals()
        {
            var brain = Object.FindFirstObjectByType<EnemyBrain>();
            brain.transform.position = new Vector3(0, 0, -14);
            brain.GetComponent<NavMeshAgent>().Warp(brain.transform.position);
            brain.transform.rotation = Quaternion.Euler(0, 180, 0);
            Warp(new Vector3(0, .05f, -20));
            yield return new WaitForSeconds(.65f);
            Assert.AreEqual(EnemyState.Combat, brain.Machine.State);
            var grass = Object.FindFirstObjectByType<GrassVolume>();
            game.Player.GetComponent<PlayerVisibilityState>().Enter(grass);
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(EnemyState.Search, brain.Machine.State);
        }

        [UnityTest]
        public IEnumerator GrassTriggerAndGunNoiseWork()
        {
            var volume = Object.FindFirstObjectByType<GrassVolume>();
            Warp(volume.transform.position);
            game.Player.GetComponent<FPSPlayerController>().Simulate(Vector2.zero, false, true, .05f);
            yield return new WaitForFixedUpdate();
            yield return null;
            var state = game.Player.GetComponent<PlayerVisibilityState>();
            Assert.IsTrue(state.HiddenInGrass);
            var brain = Object.FindFirstObjectByType<EnemyBrain>();
            brain.GetComponent<NavMeshAgent>().Warp(volume.transform.position + Vector3.right * 10);
            brain.transform.rotation = Quaternion.Euler(0, 90, 0);
            yield return null;
            game.Player.GetComponent<WeaponController>().TryShoot();
            Assert.IsTrue(state.Revealed);
            Assert.AreNotEqual(EnemyState.Patrol, brain.Machine.State);
        }

        [UnityTest]
        public IEnumerator DeathShowsDefeatAndRestartRecovers()
        {
            game.Player.Apply(new DamageInfo(1000, Vector3.zero, "步枪兵", "步枪"));
            Assert.AreEqual(GamePhase.Defeat, game.Phase);
            Assert.AreEqual(0, Time.timeScale);
            game.StartMatch();
            while (game.Phase == GamePhase.Loading)
                yield return null;
            Assert.AreEqual(GamePhase.Playing, game.Phase);
            Assert.AreEqual(100, game.Player.Health);
        }

        [UnityTest]
        public IEnumerator IntelAndPhysicalExtractionProduceVictory()
        {
            var terminal = Object.FindFirstObjectByType<ObjectiveTerminal>();
            Assert.IsTrue(terminal.Interact(game.Player));
            var zone = Object.FindFirstObjectByType<ExtractionZone>();
            Warp(zone.Position + Vector3.up * .1f);
            float deadline = Time.realtimeSinceStartup + 7;
            while (game.IsPlaying && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.AreEqual(GamePhase.Victory, game.Phase);
            Assert.IsTrue(game.Stats.won);
        }

        [UnityTest]
        public IEnumerator PauseStopsClockThenResumesAndCanReturnMenu()
        {
            game.Pause();
            float seconds = game.Stats.seconds;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(seconds, game.Stats.seconds);
            Assert.AreEqual(0, Time.timeScale);
            game.Resume();
            yield return null;
            Assert.AreEqual(1, Time.timeScale);
            game.MainMenu();
            while (game.Phase == GamePhase.Loading)
                yield return null;
            Assert.AreEqual(GamePhase.MainMenu, game.Phase);
        }

        [UnityTest]
        public IEnumerator SeparateTouchesMoveLookAndFireWithoutDoubleOwnership()
        {
            game.Input.ForceTouch = true;
            game.Pause();
            game.Resume();
            yield return null;
            var controls = Object.FindObjectsByType<TouchControl>(FindObjectsSortMode.None);
            TouchControl move = null, look = null, fire = null;
            foreach (var control in controls)
            {
                if (control.name == "Move")
                    move = control;
                if (control.name == "Look Area")
                    look = control;
                if (control.name == "开火")
                    fire = control;
            }

            Assert.IsNotNull(move);
            Assert.IsNotNull(look);
            Assert.IsNotNull(fire);
            var a = new PointerEventData(EventSystem.current)
            {
                pointerId = 11,
                position = new Vector2(100, 100)
            };
            move.OnPointerDown(a);
            a.position = new Vector2(170, 100);
            move.OnDrag(a);
            var b = new PointerEventData(EventSystem.current)
            {
                pointerId = 12,
                position = new Vector2(700, 300),
                delta = new Vector2(15, 0)
            };
            look.OnPointerDown(b);
            look.OnDrag(b);
            var c = new PointerEventData(EventSystem.current)
            {
                pointerId = 13
            };
            fire.OnPointerDown(c);
            look.OnPointerDown(c);
            Assert.Greater(game.Input.Mobile.Move.x, 0);
            Assert.Greater(game.Input.Mobile.LookDelta.x, 0);
            Assert.IsTrue(game.Input.Mobile.Fire);
            game.Pause();
            Assert.AreEqual(Vector2.zero, game.Input.Mobile.Move);
            Assert.IsFalse(game.Input.Mobile.Fire);
            game.Input.ForceTouch = false;
        }
    }
}
