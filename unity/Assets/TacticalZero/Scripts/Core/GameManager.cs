using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalZero
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public PrototypeCatalog Catalog { get; private set; }
        public PlayerHealth Player { get; private set; }
        public InputRouter Input { get; private set; }
        public SettingsData Settings => SaveService.Settings;
        public GamePhase Phase { get; private set; } = GamePhase.Boot;
        public bool IsPlaying => Phase == GamePhase.Playing;
        public bool PauseMenuRequested {get;private set;}
        public MatchStats Stats { get; private set; } = new MatchStats();
        public ObjectiveProgress Objective { get; private set; } = new ObjectiveProgress();

        public event Action<GamePhase> Changed;
        public static GameManager Ensure()
        {
            if (Instance)
                return Instance;
            return new GameObject("Tactical Zero Session").AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SaveService.Load();
            Catalog = Resources.Load<PrototypeCatalog>("PrototypeCatalog");
            if (!Catalog)
                Debug.LogError("Missing PrototypeCatalog. Stop Play Mode and run Tactical Zero/Setup Complete 3D Prototype.");
            Input = gameObject.AddComponent<InputRouter>();
            gameObject.AddComponent<AudioService>();
            gameObject.AddComponent<EffectPool>();
            gameObject.AddComponent<CombatFeedback>();
            SaveService.Apply();
        }

        public void EnterScene(string name)
        {
            Player = FindFirstObjectByType<PlayerHealth>();
            if (name == "Outpost01" || name.StartsWith("Classic_"))
            {
                Stats = new MatchStats();
                Objective = new ObjectiveProgress();
                SetPhase(GamePhase.Playing);
            }
            else
                SetPhase(GamePhase.MainMenu);
        }

        private void Update()
        {
            if (IsPlaying)
                Stats.seconds += Time.deltaTime;
        }

        public void StartMatch()
        {
            bool building=BuildingCatalog.InScene||(!ClassicRules.InMatch&&BuildingCatalog.Enabled);
            LoadScene(building ? BuildingMission.Instance?BuildingMission.Instance.Definition.Scene:BuildingCatalog.Maps[BuildingCatalog.SelectedMap].Scene : ClassicRules.Map.Scene);
        }

        public void MainMenu()
        {
            LoadScene("MainMenu");
        }

        public void LoadScene(string scene)
        {
            if (Phase == GamePhase.Loading)
                return;
            StartCoroutine(LoadRoutine(scene));
        }

        private IEnumerator LoadRoutine(string scene)
        {
            SetPhase(GamePhase.Loading);
            var operation = SceneManager.LoadSceneAsync(scene);
            if (operation == null)
            {
                Debug.LogError("Missing build scene " + scene + ". Run Tactical Zero setup.");
                SetPhase(GamePhase.MainMenu);
                yield break;
            }

            while (!operation.isDone)
                yield return null;
        }

        private void SetPhase(GamePhase phase)
        {
            if(phase!=GamePhase.Paused)PauseMenuRequested=false;
            Phase = phase;
            BuildingWeb18.Phase(phase);
            Time.maximumDeltaTime=.067f;
            Time.timeScale = phase == GamePhase.Paused || phase == GamePhase.Victory || phase == GamePhase.Defeat ? 0 : 1;
            bool lockCursor = phase == GamePhase.Playing && !Input.TouchMode;
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
            Input.Clear();
            Changed?.Invoke(phase);
        }

        public void Pause(bool showMenu=true)
        {
            if (IsPlaying)
            {
                PauseMenuRequested=showMenu;
                SetPhase(GamePhase.Paused);
            }
        }

        public void Resume()
        {
            if (Phase == GamePhase.Paused)
                SetPhase(GamePhase.Playing);
            else if(Phase==GamePhase.Defeat||Phase==GamePhase.Victory)
            { PauseMenuRequested=false;Changed?.Invoke(Phase); }
        }

        public bool Respawn()
        {
            if (Phase!=GamePhase.Defeat || !ClassicEncounter.Instance || !ClassicEncounter.Instance.RespawnPlayer()) return false;
            Stats=new MatchStats();
            SetPhase(GamePhase.Playing);
            return true;
        }

        public void TogglePause()
        {
            if (IsPlaying)
                Pause();
            else if (Phase == GamePhase.Paused)
                Resume();
            else if(Phase==GamePhase.Defeat||Phase==GamePhase.Victory)
            { PauseMenuRequested=!PauseMenuRequested;Changed?.Invoke(Phase); }
        }

        public void EndMatch(bool won)
        {
            if (!IsPlaying)
                return;
            if(BuildingMission.Instance && !won) BuildingMission.Instance.TickExtraction(0,Player.transform.position,0);
            Stats.won = won;
            SaveService.Record(Stats);
            AudioService.Instance?.Play(won ? "victory" : "defeat", Vector3.zero, .8f, false);
            SetPhase(won ? GamePhase.Victory : GamePhase.Defeat);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Pause(false);
                SaveService.Save();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Time.timeScale = 1;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
