using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

namespace TacticalZero.Editor
{
    // Create-only: rerunning repairs missing assets, never replaces an artist's existing work.
    public static class TacticalZeroProjectSetup
    {
        public const string Root = "Assets/TacticalZero";
        static Material ground, wall, steel, amber, grass, pale;
        static PrototypeCatalog catalog;
        [MenuItem("Tactical Zero/Setup Complete 3D Prototype")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before running Tactical Zero setup.");
            Folders();
            Layers();
            Pipeline();
            ground = Material("Ground", new Color(.24f, .32f, .27f));
            wall = Material("Concrete", new Color(.43f, .50f, .53f));
            steel = Material("Steel", new Color(.12f, .20f, .25f));
            amber = Material("SignalAmber", new Color(.95f, .60f, .20f));
            grass = Material("Grass", new Color(.26f, .43f, .24f));
            pale = Material("SignalTeal", new Color(.30f, .83f, .71f));
            catalog = Asset<PrototypeCatalog>(Root + "/Resources/PrototypeCatalog.asset", x =>
            {
            });
            var pistol = Weapon("USP", "USP 手枪", WeaponKind.Pistol, FireMode.SemiAutomatic, 30, .26f, 12, 60, 1.4f, 48, 1, 1.1f, .7f);
            var rifle = Weapon("AK47", "AK-47 突击步枪", WeaponKind.Rifle, FireMode.Automatic, 36, .105f, 30, 120, 2.15f, 65, 1, 1.6f, 1.1f);
            var shotgun = Weapon("Shotgun", "泵动霰弹枪", WeaponKind.Shotgun, FireMode.SemiAutomatic, 15, .9f, 6, 30, 2.4f, 22, 8, 5.5f, 2.4f);
            var soldier = Asset<EnemyDefinition>(Root + "/Data/Enemies/Rifleman.asset", x => x.Configure("步枪兵", 100, 2.8f, 22, 95, 13, 7, 3, .18f));
            var raider = Asset<EnemyDefinition>(Root + "/Data/Enemies/Rusher.asset", x => x.Configure("突击兵", 75, 4.1f, 19, 105, 6, 5, 4, .28f));
            var player = Prefab("Player/FPSPlayer", CreatePlayer);
            var enemyA = Prefab("Enemies/Rifleman", () => CreateEnemy(soldier, steel));
            var enemyB = Prefab("Enemies/Rusher", () => CreateEnemy(raider, amber));
            var foliage = Prefab("World/Grass", CreateGrass);
            var pickups = new GameObject[5];
            for (int i = 0; i < pickups.Length; i++)
            {
                int index = i;
                pickups[i] = Prefab("Pickups/" + ((PickupKind)i), () => CreatePickup((PickupKind)index));
            }

            Prefab("UI/TacticalHUD", () =>
            {
                var g = new GameObject("TacticalHUD");
                g.AddComponent<GameUI>();
                return g;
            });
            // Only fill unassigned references so a designer's replacements survive setup.
            catalog.FillMissing(new[] { pistol, rifle, shotgun }, player, new[] { enemyA, enemyB }, foliage, pickups);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Scene("Boot", s => new GameObject("Bootstrap").AddComponent<GameBootstrap>());
            Scene("MainMenu", s =>
            {
                MenuBackdrop();
                new GameObject("Menu UI").AddComponent<GameUI>();
                new GameObject("Scene Entry").AddComponent<SceneEntry>();
            });
            Scene("Outpost01", BuildOutpost);
            var order = new List<EditorBuildSettingsScene>();
            foreach (string name in new[]
            {
                "Boot",
                "MainMenu",
                "Outpost01"
            }

            )
                order.Add(new EditorBuildSettingsScene(Root + "/Scenes/" + name + ".unity", true));
            foreach (var previous in EditorBuildSettings.scenes)
                if (!order.Exists(x => x.path == previous.path))
                    order.Add(previous);
            EditorBuildSettings.scenes = order.ToArray();
            PlayerSettings.companyName = "Tactical Zero";
            PlayerSettings.productName = "战术禁区 3D";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.tacticalzero.prototype");
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.tacticalzero.prototype");
            // Input System UI module and gameplay both use the new input backend.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input != null)
            {
                input.intValue = 1;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tactical Zero setup complete. Open Assets/TacticalZero/Scenes/Boot.unity and press Play. Existing assets were preserved.");
        }

        static void Folders()
        {
            foreach (string path in new[]
            {
                "Resources",
                "Art/Materials",
                "Art/Models",
                "Art/Particles",
                "Audio",
                "Data/Weapons",
                "Data/Enemies",
                "Prefabs/Player",
                "Prefabs/Weapons",
                "Prefabs/Enemies",
                "Prefabs/World",
                "Prefabs/Pickups",
                "Prefabs/UI",
                "Scenes",
                "Settings"
            }

            )
                Directory.CreateDirectory(Root + "/" + path);
            AssetDatabase.Refresh();
        }

        static void Layers()
        {
            var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = manager.FindProperty("layers");
            foreach (string name in new[]
            {
                "TZPlayer",
                "TZEnemy",
                "TZWorld",
                "TZInteractable",
                "TZGrass",
                "TZViewModel",
                "TZEffects"
            }

            )
            {
                if (LayerMask.NameToLayer(name) >= 0)
                    continue;
                bool found = false;
                for (int i = 8; i < 32; i++)
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                    {
                        layers.GetArrayElementAtIndex(i).stringValue = name;
                        found = true;
                        break;
                    }

                if (!found)
                    throw new InvalidOperationException("No free layer for " + name + ". Reserve a user layer and rerun setup.");
                manager.ApplyModifiedPropertiesWithoutUndo();
            }

            var tags = manager.FindProperty("tags");
            bool exists = false;
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == "TacticalObjective")
                    exists = true;
            if (!exists)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "TacticalObjective";
            }

            manager.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Pipeline()
        {
            string effectPath = Root + "/Resources/EffectsMaterial.mat";
            if (!File.Exists(effectPath))
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (!shader) throw new InvalidOperationException("URP Unlit shader missing; resolve Universal RP in Package Manager.");
                AssetDatabase.CreateAsset(new Material(shader) { enableInstancing = true }, effectPath);
            }
            var renderer = Asset<UniversalRendererData>(Root + "/Settings/ForwardRenderer.asset", x =>
            {
            });
            string path = Root + "/Settings/TacticalURP.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (!pipeline)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.renderScale = 1;
                pipeline.msaaSampleCount = 2;
                pipeline.shadowDistance = 28;
                pipeline.supportsHDR = false;
                AssetDatabase.CreateAsset(pipeline, path);
            }

            if (!GraphicsSettings.defaultRenderPipeline)
                GraphicsSettings.defaultRenderPipeline = pipeline;
            if (!QualitySettings.renderPipeline)
                QualitySettings.renderPipeline = pipeline;
        }

        static T Asset<T>(string path, Action<T> initialize)
            where T : ScriptableObject
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value)
                return value;
            if (File.Exists(path))
                throw new InvalidOperationException("Asset exists with unexpected type: " + path);
            value = ScriptableObject.CreateInstance<T>();
            initialize(value);
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        static Material Material(string name, Color color)
        {
            string path = Root + "/Art/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat)
                return mat;
            if (File.Exists(path))
                throw new InvalidOperationException("Existing material could not be loaded; preserving it: " + path);
            var shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (!shader)
                throw new InvalidOperationException("URP Simple Lit shader missing. Resolve Universal RP in Package Manager, then rerun setup.");
            mat = new Material(shader)
            {
                name = name,
                color = color,
                enableInstancing = true
            };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static GameObject Prefab(string name, Func<GameObject> factory)
        {
            string path = Root + "/Prefabs/" + name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab)
                return prefab;
            if (File.Exists(path))
                throw new InvalidOperationException("Existing prefab could not be loaded; preserving it: " + path);
            var instance = factory();
            try
            {
                return PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        static WeaponDefinition Weapon(string id, string name, WeaponKind kind, FireMode mode, float damage, float interval, int mag, int reserve, float reload, float range, int pellets, float spread, float recoil)
        {
            var model = Prefab("Weapons/" + id, () =>
            {
                var root = new GameObject(id);
                var body = Cube("Receiver", root.transform, new Vector3(0, 0, .16f), new Vector3(.12f, .13f, kind == WeaponKind.Pistol ? .25f : .5f), steel, "TZViewModel");
                Cube("Barrel", root.transform, new Vector3(0, .02f, .42f), new Vector3(.045f, .045f, .3f), wall, "TZViewModel");
                Cube("Grip", root.transform, new Vector3(0, -.13f, .04f), new Vector3(.09f, .18f, .09f), amber, "TZViewModel");
                Cube("Support hand", root.transform, new Vector3(-.02f, -.08f, .27f), new Vector3(.16f, .09f, .10f), ground, "TZViewModel");
                foreach (var c in root.GetComponentsInChildren<Collider>())
                    UnityEngine.Object.DestroyImmediate(c);
                return root;
            });
            return Asset<WeaponDefinition>(Root + "/Data/Weapons/" + id + ".asset", x => x.Configure(id, name, kind, mode, damage, interval, mag, reserve, reload, range, pellets, spread, recoil, model, AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Resources/Audio/" + id + ".wav")));
        }

        static GameObject CreatePlayer()
        {
            var root = new GameObject("FPS Player");
            root.tag = "Player";
            root.layer = LayerMask.NameToLayer("TZPlayer");
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = new Vector3(0, .9f, 0);
            controller.radius = .32f;
            controller.stepOffset = .25f;
            controller.skinWidth = .04f;
            var view = new GameObject("View");
            view.transform.SetParent(root.transform, false);
            view.transform.localPosition = new Vector3(0, 1.6f, 0);
            var camera = view.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.nearClipPlane = .04f;
            camera.farClipPlane = 120;
            camera.fieldOfView = 75;
            view.AddComponent<AudioListener>();
            root.AddComponent<PlayerHealth>();
            root.AddComponent<PlayerVisibilityState>();
            root.AddComponent<FPSPlayerController>();
            root.AddComponent<PlayerLook>().Configure(camera);
            root.AddComponent<AimAssistSystem>();
            root.AddComponent<WeaponController>().Configure(camera);
            root.AddComponent<PlayerInteraction>().Configure(camera);
            root.AddComponent<PlayerFootsteps>();
            return root;
        }

        static GameObject CreateEnemy(EnemyDefinition data, Material cloth)
        {
            var root = new GameObject(data.DisplayName);
            root.layer = LayerMask.NameToLayer("TZEnemy");
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, .8f, 0);
            body.transform.localScale = new Vector3(.65f, .65f, .65f);
            body.layer = root.layer;
            body.GetComponent<Renderer>().sharedMaterial = cloth;
            body.AddComponent<Hitbox>().Configure(false);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0, 1.55f, 0);
            head.transform.localScale = Vector3.one * .36f;
            head.layer = root.layer;
            head.GetComponent<Renderer>().sharedMaterial = wall;
            head.AddComponent<Hitbox>().Configure(true);
            Cube("Visor", root.transform, new Vector3(0, 1.56f, .17f), new Vector3(.29f, .09f, .06f), amber, "TZEffects").GetComponent<Collider>().enabled = false;
            var gun = Cube("Weapon", root.transform, new Vector3(.3f, 1.15f, .27f), new Vector3(.13f, .15f, .55f), steel, "TZEffects");
            gun.GetComponent<Collider>().enabled = false;
            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = .38f;
            agent.height = 1.8f;
            agent.speed = data.Speed;
            agent.angularSpeed = 240;
            agent.acceleration = 12;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            root.AddComponent<EnemyHealth>().Configure(data);
            root.AddComponent<EnemyPerception>();
            root.AddComponent<EnemyCombat>();
            root.AddComponent<EnemyBrain>().Configure(data);
            root.AddComponent<EnemyAnimatorBridge>();
            return root;
        }

        static GameObject CreateGrass()
        {
            var root = new GameObject("Grass Volume");
            root.layer = LayerMask.NameToLayer("TZGrass");
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(7, 2, 7);
            box.center = Vector3.up;
            root.AddComponent<GrassVolume>();
            var visual = new GameObject("Foliage");
            visual.transform.SetParent(root.transform, false);
            for (int i = 0; i < 28; i++)
            {
                float x = (i % 7 - 3) * .93f, z = (i / 7 - 1.5f) * 1.6f;
                var g = Cube("Grass tuft", visual.transform, new Vector3(x, .58f, z), new Vector3(.16f, 1.15f, .55f), grass, "TZGrass");
                g.transform.localRotation = Quaternion.Euler(0, i * 137.5f, 8);
                UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
            }

            root.AddComponent<GrassVisualController>();
            return root;
        }

        static GameObject CreatePickup(PickupKind kind)
        {
            var root = Cube(kind.ToString(), null, Vector3.zero, new Vector3(.65f, .55f, .65f), kind == PickupKind.Medical ? pale : amber, "TZInteractable");
            root.AddComponent<PickupInteractable>().Configure(kind);
            return root;
        }

        static void Scene(string name, Action<Scene> build)
        {
            string path = Root + "/Scenes/" + name + ".unity";
            if (File.Exists(path))
                return;
            var prior = SceneManager.GetActiveScene();
            // Unity cannot add a scene alongside an untitled scene. Preserve its contents first.
            if (prior.IsValid() && string.IsNullOrEmpty(prior.path))
            {
                string preservedPath = AssetDatabase.GenerateUniqueAssetPath(Root + "/Scenes/PreservedWorkspace.unity");
                if (!EditorSceneManager.SaveScene(prior, preservedPath))
                    throw new InvalidOperationException("Could not preserve the untitled scene before setup.");
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                build(scene);
                EditorSceneManager.SaveScene(scene, path);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (prior.IsValid())
                    SceneManager.SetActiveScene(prior);
            }
        }

        static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material mat, string layer = "TZWorld")
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.layer = LayerMask.NameToLayer(layer);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = position;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            return g;
        }

        static GameObject Place(GameObject prefab, Vector3 position, Transform parent = null)
        {
            if (!prefab)
                throw new InvalidOperationException("PrototypeCatalog contains a missing prefab. Rerun setup.");
            var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            g.transform.SetParent(parent, false);
            g.transform.position = position;
            return g;
        }

        static void Lighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.color = new Color(1, .9f, .74f);
            sun.shadows = LightShadows.Hard;
            sun.transform.rotation = Quaternion.Euler(48, -32, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.48f, .56f, .61f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 45;
            RenderSettings.fogEndDistance = 110;
            RenderSettings.fogColor = new Color(.31f, .41f, .45f);
        }

        static void MenuBackdrop()
        {
            Lighting();
            Cube("Backdrop", null, new Vector3(0, -.5f, 0), new Vector3(24, 1, 24), ground);
            for (int i = 0; i < 5; i++)
                Cube("Supply crate", null, new Vector3(-4 + i * 2, 1, 5), new Vector3(1.5f, 2, 2), i % 2 == 0 ? steel : amber);
            var c = new GameObject("Menu Camera").AddComponent<Camera>();
            c.tag = "MainCamera";
            c.transform.position = new Vector3(8, 5, -8);
            c.transform.LookAt(new Vector3(0, 1, 3));
            c.gameObject.AddComponent<AudioListener>();
        }

        static void BuildOutpost(Scene scene)
        {
            Lighting();
            var geometry = new GameObject("Static Geometry");
            Cube("Ground", geometry.transform, new Vector3(0, -.5f, 0), new Vector3(64, 1, 64), ground);
            Cube("Road", geometry.transform, new Vector3(0, .015f, 0), new Vector3(7, .025f, 55), wall).GetComponent<Collider>().enabled = false;
            foreach (float edge in new[]
            {
                -32f,
                32f
            }

            )
            {
                Cube("Perimeter", geometry.transform, new Vector3(edge, 2, 0), new Vector3(1, 4, 65), steel);
                Cube("Perimeter", geometry.transform, new Vector3(0, 2, edge), new Vector3(65, 4, 1), steel);
            }

            // Warehouse with a 4 m front opening and an independent 3 m side entrance.
            Cube("Warehouse west", geometry.transform, new Vector3(-10, 2.5f, 15), new Vector3(.5f, 5, 18), wall);
            Cube("Warehouse rear", geometry.transform, new Vector3(0, 2.5f, 24), new Vector3(20, 5, .5f), wall);
            Cube("Warehouse front L", geometry.transform, new Vector3(-6, 2.5f, 6), new Vector3(8, 5, .5f), wall);
            Cube("Warehouse front R", geometry.transform, new Vector3(6, 2.5f, 6), new Vector3(8, 5, .5f), wall);
            Cube("East lower", geometry.transform, new Vector3(10, 2.5f, 9), new Vector3(.5f, 5, 6), wall);
            Cube("East upper", geometry.transform, new Vector3(10, 2.5f, 19.5f), new Vector3(.5f, 5, 9), wall);
            Cube("Roof", geometry.transform, new Vector3(0, 5.2f, 15), new Vector3(21, .4f, 19), steel);
            Cube("Door lintel", geometry.transform, new Vector3(0, 4.5f, 6), new Vector3(4, 1, .5f), amber);
            var interior = new GameObject("Warehouse fill").AddComponent<Light>();
            interior.type = LightType.Point;
            interior.range = 18;
            interior.intensity = 3;
            interior.color = new Color(.6f, .78f, 1);
            interior.shadows = LightShadows.None;
            interior.transform.position = new Vector3(0, 4, 16);
            foreach (var p in new[]
            {
                new Vector3(-6, 1, 12),
                new Vector3(5, 1, 17),
                new Vector3(-8, 1, -8),
                new Vector3(7, 1, -3),
                new Vector3(18, 1, 0),
                new Vector3(-18, 1, 8)
            }

            )
                Cube("Cover", geometry.transform, p, new Vector3(3, 2, 3), steel);
            for (int i = 0; i < 7; i++)
                Cube("Road marking", geometry.transform, new Vector3(0, .035f, -22 + i * 4), new Vector3(.2f, .02f, 1.7f), amber).GetComponent<Collider>().enabled = false;
            foreach (var r in geometry.GetComponentsInChildren<Renderer>())
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            Physics.SyncTransforms();
            var surface = geometry.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = LayerMask.GetMask("TZWorld");
            surface.BuildNavMesh();
            if (!surface.navMeshData)
                throw new InvalidOperationException("Outpost NavMesh bake failed. Check AI Navigation and default agent settings.");
            string nav = AssetDatabase.GenerateUniqueAssetPath(Root + "/Scenes/Outpost01NavMesh.asset");
            AssetDatabase.CreateAsset(surface.navMeshData, nav);
            var player = Place(catalog.PlayerPrefab, new Vector3(0, .05f, -26));
            foreach (var p in new[]
            {
                new Vector3(14, 0, -18),
                new Vector3(18, 0, -10),
                new Vector3(22, 0, 0),
                new Vector3(19, 0, 10)
            }

            )
                Place(catalog.GrassPrefab, p);
            var positions = new[]
            {
                new Vector3(-6, 0, -2),
                new Vector3(6, 0, 3),
                new Vector3(-5, 0, 18),
                new Vector3(5, 0, 21),
                new Vector3(18, 0, 18),
                new Vector3(-18, 0, 16)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                var enemy = Place(catalog.EnemyPrefabs[i % 2], positions[i]);
                var points = new Vector3[]
                {
                    positions[i],
                    positions[i] + new Vector3(i % 2 == 0 ? -3 : 3, 0, -3),
                    positions[i] + new Vector3(0, 0, 3)
                };
                for (int j = 0; j < points.Length; j++)
                {
                    if (!NavMesh.SamplePosition(points[j], out var hit, 3, NavMesh.AllAreas))
                        throw new InvalidOperationException("Unreachable patrol point " + i + ":" + j);
                    points[j] = hit.position;
                }

                enemy.GetComponent<EnemyBrain>().SetPatrol(points);
                var route = new NavMeshPath();
                foreach (var point in points)
                    if (!NavMesh.CalculatePath(points[0], point, NavMesh.AllAreas, route) || route.status != NavMeshPathStatus.PathComplete)
                        throw new InvalidOperationException("Disconnected enemy patrol route: " + i);
            }

            for (int i = 0; i < 5; i++)
                Place(catalog.PickupPrefabs[i], new Vector3(i % 2 == 0 ? -3 : 3, .4f, -21 + i * 5));
            var terminal = Cube("Intel terminal", null, new Vector3(0, .75f, 21), new Vector3(1.4f, 1.5f, .8f), amber, "TZInteractable");
            terminal.tag = "TacticalObjective";
            terminal.AddComponent<ObjectiveTerminal>();
            var terminalObstacle = terminal.AddComponent<NavMeshObstacle>();
            terminalObstacle.shape = NavMeshObstacleShape.Box;
            terminalObstacle.size = Vector3.one;
            terminalObstacle.carving = true;
            var extract = Cube("Extraction", null, new Vector3(24, .04f, -25), new Vector3(6, .08f, 6), pale, "TZEffects");
            extract.GetComponent<Collider>().enabled = false;
            extract.AddComponent<ExtractionZone>();
            new GameObject("Scene Entry").AddComponent<SceneEntry>();
            new GameObject("HUD").AddComponent<GameUI>();
            Physics.SyncTransforms();
            var navigationPath = new NavMeshPath();
            foreach (var point in new[]
            {
                player.transform.position,
                terminal.transform.position + Vector3.back * 2,
                extract.transform.position
            }

            )
            {
                if (!NavMesh.SamplePosition(point, out var hit, 2, NavMesh.AllAreas))
                    throw new InvalidOperationException("Gameplay point is outside NavMesh: " + point);
                if (!NavMesh.CalculatePath(player.transform.position, hit.position, NavMesh.AllAreas, navigationPath) || navigationPath.status != NavMeshPathStatus.PathComplete)
                    throw new InvalidOperationException("No complete route from spawn to " + point);
            }
        }
    }
}
