using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TacticalZero.Editor
{
    public static class TacticalZeroValidation
    {
        [MenuItem("Tactical Zero/Validate Generated Project")]
        public static void Validate()
        {
            var catalog = Resources.Load<PrototypeCatalog>("PrototypeCatalog");
            if (!catalog || catalog.Weapons == null || catalog.Weapons.Length != 3)
                throw new InvalidOperationException("Missing catalog or weapons; run setup.");
            if (!catalog.PlayerPrefab || !catalog.PlayerPrefab.GetComponent<CharacterController>() || !catalog.PlayerPrefab.GetComponent<PlayerLook>() || !catalog.PlayerPrefab.GetComponent<PlayerLook>().View)
                throw new InvalidOperationException("Player prefab, CharacterController or camera reference missing; run setup or repair FPSPlayer prefab.");
            if (catalog.EnemyPrefabs == null || catalog.EnemyPrefabs.Length != 2)
                throw new InvalidOperationException("Two enemy prefabs are required in PrototypeCatalog.");
            foreach (var enemy in catalog.EnemyPrefabs)
                if (!enemy || !enemy.GetComponent<EnemyBrain>() || !enemy.GetComponent<EnemyHealth>() || !enemy.GetComponent<UnityEngine.AI.NavMeshAgent>())
                    throw new InvalidOperationException("Enemy prefab has missing AI/health/navigation components.");
            if (!catalog.GrassPrefab || !catalog.GrassPrefab.GetComponent<GrassVolume>() || catalog.PickupPrefabs == null || catalog.PickupPrefabs.Length != 5)
                throw new InvalidOperationException("Grass and five pickup prefabs are required in PrototypeCatalog.");
            foreach (var pickup in catalog.PickupPrefabs)
                if (!pickup || !pickup.GetComponent<PickupInteractable>())
                    throw new InvalidOperationException("A pickup prefab or interactable component is missing.");
            if (!Resources.Load<Material>("EffectsMaterial"))
                throw new InvalidOperationException("EffectsMaterial missing; rerun setup.");
            foreach (var weapon in catalog.Weapons)
                if (!weapon || !weapon.Valid || !weapon.Model)
                    throw new InvalidOperationException("Invalid weapon definition or missing model.");
            foreach (string name in new[]
            {
                "Boot",
                "MainMenu",
                "Outpost01"
            }

            )
            {
                string path = TacticalZeroProjectSetup.Root + "/Scenes/" + name + ".unity";
                if (!File.Exists(path))
                    throw new InvalidOperationException("Missing " + path + "; run setup.");
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened)
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                                throw new InvalidOperationException("Missing script: " + name + "/" + transform.name);
                }
                finally
                {
                    if (opened)
                        EditorSceneManager.CloseScene(scene, true);
                }
            }

            Debug.Log("Tactical Zero scene scripts and weapon references validated.");
        }
    }
}
