using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TacticalZero
{
    public sealed class GameUI : MonoBehaviour
    {
        private GameManager game;
        private RectTransform safe, hud, menu, settings, mobile;
        private Text objective, prompt, conceal, hit, result, direction, heading, passivePause;
        private RectTransform[] crosshair;
        private WeaponController weapons;
        private PlayerVisibilityState visibility;
        private PlayerInteraction interaction;
        private ObjectiveTerminal terminal;
        private ExtractionZone extraction;
        private float nextHud;
        private bool settingsOpen;
        private Button start, resume, restart, home, exit, respawn, options;
        private Button fullscreenButton;
        private BuildingDeploymentUI deployment;
        private void Start()
        {
            game = GameManager.Ensure();
            Build();
            deployment=gameObject.AddComponent<BuildingDeploymentUI>();deployment.Build(safe,OpenSettings);
            if(BuildingCatalog.InScene)gameObject.AddComponent<BuildingHUD>().Build(hud);
            if(ClassicRules.InMatch&&!BuildingCatalog.InScene){gameObject.AddComponent<ClassicCombatUI>().Build(safe);gameObject.AddComponent<OperationHUD>().Build(safe);gameObject.AddComponent<SupplyHUD>().Build(safe);}
            gameObject.AddComponent<CombatFeedbackUI>().Build(safe);
            gameObject.AddComponent<WeaponOpticHUD>().Build(hud);
            hud.Find("Battle status").SetAsLastSibling();
            game.Changed += Refresh;
            BindPlayer();
            Refresh(game.Phase);
        }

        private void OnDestroy()
        {
            if (game)
                game.Changed -= Refresh;
        }

        private void BindPlayer()
        {
            if (game.Player)
            {
                weapons = game.Player.GetComponent<WeaponController>();
                visibility = game.Player.GetComponent<PlayerVisibilityState>();
                interaction = game.Player.GetComponent<PlayerInteraction>();
            }

            terminal = FindFirstObjectByType<ObjectiveTerminal>();
            extraction = FindFirstObjectByType<ExtractionZone>();
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            if (!EventSystem.current)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            safe = UIFactory.Fill("Safe Area", transform);
            safe.gameObject.AddComponent<SafeAreaController>();
            hud = UIFactory.Fill("HUD", safe);
            gameObject.AddComponent<BattleStatusHUD>().Build(hud);
            objective = UIFactory.Text("Objective", hud, "", new Vector2(0, 1), new Vector2(184, -58), new Vector2(324, 48), 16, TextAnchor.MiddleLeft);
            prompt = UIFactory.Text("Interact", hud, "", new Vector2(.5f, .5f), new Vector2(0, -85), new Vector2(650, 64), 21, TextAnchor.MiddleCenter);
            conceal = UIFactory.Text("Concealment", hud, "", new Vector2(.5f, 0), new Vector2(0, 192), new Vector2(520, 24), 13, TextAnchor.MiddleCenter);
            conceal.color = new Color(.59f, .94f, .60f);
            hit = UIFactory.Text("Hit marker", hud, "", new Vector2(.5f, .5f), Vector2.zero, new Vector2(55, 55), 35, TextAnchor.MiddleCenter);
            direction = UIFactory.Text("Damage direction", hud, "", new Vector2(.5f, .5f), new Vector2(0, 72), new Vector2(430, 45), 23, TextAnchor.MiddleCenter);
            direction.color = new Color(1, .53f, .38f);
            crosshair = new RectTransform[4];
            for (int i = 0; i < 4; i++)
            {
                crosshair[i] = UIFactory.Rect("Reticle", hud, new Vector2(.5f, .5f), Vector2.zero, i % 2 == 0 ? new Vector2(2, 9) : new Vector2(9, 2));
                UIFactory.Panel(crosshair[i], new Color(.92f, .95f, .83f));
            }

            passivePause=UIFactory.Text("Passive pause",safe,"已暂停 · Esc 继续",new Vector2(.5f,.5f),Vector2.zero,new Vector2(550,90),26,TextAnchor.MiddleCenter);
            var touchResume=passivePause.gameObject.AddComponent<Button>();touchResume.targetGraphic=passivePause;touchResume.onClick.AddListener(()=>game.Resume());
            passivePause.gameObject.SetActive(false);
            BuildMobile();
            menu = UIFactory.Rect("Menu", safe, new Vector2(.5f, .5f), Vector2.zero, new Vector2(620, 650));
            UIFactory.Panel(menu, new Color(.035f, .07f, .085f, .98f), true);
            heading = UIFactory.Text("Title", menu, "TACTICAL ZERO\n战术禁区", new Vector2(.5f, 1), new Vector2(0, -74), new Vector2(550, 120), 40, TextAnchor.MiddleCenter);
            result = UIFactory.Text("Briefing", menu, "", new Vector2(.5f, 1), new Vector2(0, -225), new Vector2(550, 190), 21, TextAnchor.MiddleCenter);
            start = UIFactory.Button("开始行动", menu, new Vector2(.5f, 0), new Vector2(0, 255), new Vector2(430, 52), () => game.StartMatch());
            resume = UIFactory.Button("继续行动", menu, new Vector2(.5f, 0), new Vector2(0, 255), new Vector2(430, 52), () => game.Resume());
            respawn = UIFactory.Button("随机复活 · 丢失背包与装备", menu, new Vector2(.5f,0),new Vector2(0,255),new Vector2(430,52),()=>game.Respawn());
            restart = UIFactory.Button("重新开始", menu, new Vector2(.5f, 0), new Vector2(0, 195), new Vector2(430, 52), () => game.StartMatch());
            options=UIFactory.Button("设置", menu, new Vector2(.5f, 0), new Vector2(0, 195), new Vector2(430, 52), OpenSettings);
            home = UIFactory.Button("返回主菜单", menu, new Vector2(.5f, 0), new Vector2(0, 75), new Vector2(430, 52), () => game.MainMenu());
            exit = UIFactory.Button("退出游戏", menu, new Vector2(.5f, 0), new Vector2(0, 75), new Vector2(430, 52), () => Application.Quit());
            UIFactory.Text("Version", menu, "3D 0.17.0 · 建筑清剿 · 单人离线", new Vector2(.5f, 0), new Vector2(0, 23), new Vector2(550, 35), 16, TextAnchor.MiddleCenter);
            BuildSettings();
        }

        private void BuildMobile()
        {
            mobile = UIFactory.Fill("Mobile Controls", hud);
            // Look background is earlier in draw order; buttons above it own their touches exclusively.
            var look = UIFactory.Fill("Look Area", mobile);
            look.anchorMin = new Vector2(.43f, 0);
            look.anchorMax = new Vector2(1, .85f);
            UIFactory.Panel(look, new Color(0, 0, 0, 0), true);
            look.gameObject.AddComponent<TouchControl>().Configure(MobileAction.Look);
            // Desktop has no menu button; touch devices retain an Escape equivalent.
            TouchButton("Esc",MobileAction.Pause,new Vector2(-72,365),new Vector2(70,46));
            var stick = UIFactory.Rect("Move", mobile, new Vector2(0, 0), new Vector2(130, 140), new Vector2(160, 160));
            UIFactory.Panel(stick, new Color(.13f, .23f, .25f, .55f), true);
            var knob = UIFactory.Rect("Knob", stick, new Vector2(.5f, .5f), Vector2.zero, new Vector2(55, 55));
            UIFactory.Panel(knob, new Color(.63f, .73f, .63f, .7f));
            stick.gameObject.AddComponent<TouchControl>().Configure(MobileAction.Move, knob);
            TouchButton("开火", MobileAction.Fire, new Vector2(-100, 170), new Vector2(115, 100));
            TouchButton("瞄准", MobileAction.Aim, new Vector2(-225, 170), new Vector2(94, 72));
            TouchButton("换弹", MobileAction.Reload, new Vector2(-95, 282), new Vector2(90, 68));
            TouchButton("↗ 互动", MobileAction.Interact, new Vector2(-215, 282), new Vector2(94, 68));
            if(!BuildingCatalog.InScene)TouchButton("切枪", MobileAction.Switch, new Vector2(-95, 60), new Vector2(90, 65));
            TouchButton("跳跃", MobileAction.Jump, new Vector2(-335,170),new Vector2(94,72));
            TouchButton("蹲下", MobileAction.Crouch, new Vector2(-215, 60), new Vector2(94, 65));
            var run = UIFactory.Rect("Sprint", mobile, new Vector2(0, 0), new Vector2(290, 115), new Vector2(90, 75));
            UIFactory.Panel(run, new Color(.15f, .25f, .28f, .8f), true);
            UIFactory.Text("Label", run, "冲刺", new Vector2(.5f, .5f), Vector2.zero, new Vector2(80, 60), 22, TextAnchor.MiddleCenter);
            run.gameObject.AddComponent<TouchControl>().Configure(MobileAction.Sprint);
        }

        private void TouchButton(string label, MobileAction action, Vector2 pos, Vector2 size)
        {
            var rect = UIFactory.Rect(label, mobile, new Vector2(1, 0), pos, size);
            UIFactory.Panel(rect, new Color(.13f, .23f, .27f, .82f), true);
            UIFactory.Text("Label", rect, label, new Vector2(.5f, .5f), Vector2.zero, size, 22, TextAnchor.MiddleCenter);
            rect.gameObject.AddComponent<TouchControl>().Configure(action);
        }

        private void BuildSettings()
        {
            settings = UIFactory.Rect("Settings", safe, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1020, 640));
            UIFactory.Panel(settings, new Color(.04f, .08f, .10f, .99f), true);
            UIFactory.Text("Title", settings, "行动设置", new Vector2(.5f, 1), new Vector2(0, -36), new Vector2(800, 50), 30, TextAnchor.MiddleCenter);
            var data = game.Settings;
            UIFactory.Slider("主音量", settings, new Vector2(-245, 195), data.masterVolume, 0, 1, x => data.masterVolume = x);
            UIFactory.Slider("音效音量", settings, new Vector2(-245, 115), data.sfxVolume, 0, 1, x => data.sfxVolume = x);
            UIFactory.Slider("鼠标灵敏度", settings, new Vector2(-245, 35), data.mouseSensitivity, .2f, 5, x => data.mouseSensitivity = x);
            UIFactory.Slider("手机视角灵敏度", settings, new Vector2(-245, -45), data.touchSensitivity, .2f, 5, x => data.touchSensitivity = x);
            UIFactory.Slider("开镜灵敏度", settings, new Vector2(-245, -125), data.adsSensitivity, .2f, 1.5f, x => data.adsSensitivity = x);
            UIFactory.Slider("手机瞄准辅助", settings, new Vector2(-245, -205), data.aimAssist, 0, 1, x => data.aimAssist = x);
            Toggle("镜头震动", new Vector2(245, 195), () => data.cameraShake, () => data.cameraShake = !data.cameraShake);
            Toggle("武器摆动", new Vector2(245, 135), () => data.weaponSway, () => data.weaponSway = !data.weaponSway);
            Toggle("反转 Y 轴", new Vector2(245, 75), () => data.invertY, () => data.invertY = !data.invertY);
            if(!Application.isMobilePlatform)
            {
                fullscreenButton=Toggle("全屏 F11",new Vector2(245,-105),()=>data.fullscreen,()=>DisplayMode.Toggle());
                Button aimMode=null;
                System.Func<string> label=()=>"开镜方式："+(data.toggleAim?"点击右键切换":"按住右键");
                aimMode=UIFactory.Button(label(),settings,new Vector2(.5f,.5f),new Vector2(245,-165),new Vector2(400,52),()=>
                {data.toggleAim=!data.toggleAim;game.Input.Clear();SaveService.Save();aimMode.GetComponentInChildren<Text>().text=label();});
                aimMode.name="Aim mode";
            }
            Button qualityButton = null;
            qualityButton = UIFactory.Button("画质：" + QualityName(data.quality), settings, new Vector2(.5f, .5f), new Vector2(245, 15), new Vector2(400, 52), () =>
            {
                data.quality = (data.quality + 1) % 3;
                qualityButton.GetComponentInChildren<Text>().text = "画质：" + QualityName(data.quality);
            });
            Button fpsButton = null;
            fpsButton = UIFactory.Button("帧率：" + data.frameLimit, settings, new Vector2(.5f, .5f), new Vector2(245, -45), new Vector2(400, 52), () =>
            {
                data.frameLimit = data.frameLimit == 30 ? 60 : data.frameLimit == 60 ? 120 : data.frameLimit == 120 ? 144 : 30;
                fpsButton.GetComponentInChildren<Text>().text = "帧率：" + data.frameLimit;
            });
            UIFactory.Slider("环境音量", settings, new Vector2(245,-215), data.ambientVolume, 0, 1, x=>data.ambientVolume=x);
            Button soundTest=null; soundTest=UIFactory.Button("耳机声场测试",settings,new Vector2(.5f,0),new Vector2(-350,35),new Vector2(190,42),()=>AudioService.Instance?.TestHeadphones(soundTest.GetComponentInChildren<Text>()));
            UIFactory.Button("调整触屏布局",settings,new Vector2(.5f,0),new Vector2(-140,35),new Vector2(200,42),EditTouchLayout);
            UIFactory.Button("保存并返回", settings, new Vector2(.5f, 0), new Vector2(245, 35), new Vector2(400, 44), () =>
            {
                SaveService.Save();
                SaveService.Apply();
                settingsOpen = false;
                Refresh(game.Phase);
            });
            settings.gameObject.SetActive(false);
        }

        private static string QualityName(int q) => q == 0 ? "低 / Low" : q == 1 ? "中 / Medium" : "高 / High";
        private void EditTouchLayout()
        {
            settings.gameObject.SetActive(false);deployment.SetVisible(false);menu.gameObject.SetActive(false);hud.gameObject.SetActive(true);mobile.gameObject.SetActive(true);TouchLayout.Begin();
            var editing=UIFactory.Rect("Touch layout editor",safe,new Vector2(.5f,1),new Vector2(0,-64),new Vector2(650,92));
            UIFactory.Panel(editing,new Color(.025f,.05f,.06f,.96f),true);
            UIFactory.Text("Hint",editing,"拖动摇杆和按钮 · 位置自动保存",new Vector2(.5f,.5f),new Vector2(0,24),new Vector2(610,30),22,TextAnchor.MiddleCenter);
            UIFactory.Button("恢复默认位置",editing,new Vector2(.5f,.5f),new Vector2(-156,-19),new Vector2(290,38),TouchLayout.ResetPositions);
            UIFactory.Button("完成调整",editing,new Vector2(.5f,.5f),new Vector2(156,-19),new Vector2(290,38),()=>{TouchLayout.End();Destroy(editing.gameObject);settingsOpen=true;Refresh(game.Phase);});
        }
        private Button Toggle(string label, Vector2 position, System.Func<bool> read, System.Action change)
        {
            Button button = null;
            button = UIFactory.Button(label + "：" + (read() ? "开" : "关"), settings, new Vector2(.5f, .5f), position, new Vector2(400, 52), () =>
            {
                change();
                button.GetComponentInChildren<Text>().text = label + "：" + (read() ? "开" : "关");
            });
            return button;
        }

        private void OpenSettings()
        {
            game.Pause();
            settingsOpen = true;
            Refresh(game.Phase);
        }

        private void Refresh(GamePhase phase)
        {
            if (!hud)
                return;
            if (phase == GamePhase.Playing || phase == GamePhase.Loading)
                settingsOpen = false;
            if(!game.PauseMenuRequested&&phase!=GamePhase.MainMenu)settingsOpen=false;
            hud.gameObject.SetActive(phase == GamePhase.Playing);
            mobile.gameObject.SetActive(game.Input.TouchMode);
            menu.gameObject.SetActive((phase==GamePhase.MainMenu||phase==GamePhase.Defeat||phase==GamePhase.Victory||(phase==GamePhase.Paused&&game.PauseMenuRequested))&&!settingsOpen);
            passivePause.gameObject.SetActive(phase==GamePhase.Paused&&!game.PauseMenuRequested);
            passivePause.raycastTarget=game.Input.TouchMode;passivePause.GetComponent<Button>().enabled=game.Input.TouchMode;
            passivePause.text=game.Input.TouchMode?"已暂停 · 点击继续":"已暂停 · Esc 继续";
            settings.gameObject.SetActive(settingsOpen);
            bool main = phase == GamePhase.MainMenu, paused = phase == GamePhase.Paused, finished = phase == GamePhase.Victory || phase == GamePhase.Defeat;
            if(deployment){deployment.SetVisible(main&&!settingsOpen);if(main)menu.gameObject.SetActive(false);}
            bool menuOptions=game.PauseMenuRequested;
            bool dead=phase==GamePhase.Defeat&&!menuOptions&&!BuildingCatalog.InScene;
            menu.sizeDelta=dead?new Vector2(440,340):new Vector2(620,650);
            menu.anchoredPosition=dead?new Vector2(390,-170):Vector2.zero;
            heading.gameObject.SetActive(!dead);
            result.rectTransform.anchoredPosition=new Vector2(0,dead?-80:menuOptions?-190:-225);
            result.rectTransform.sizeDelta=dead?new Vector2(400,155):menuOptions?new Vector2(550,100):new Vector2(550,190);
            result.fontSize=dead?19:21;
            foreach(var button in menu.GetComponentsInChildren<Button>(true))((RectTransform)button.transform).sizeDelta=new Vector2(dead?380:430,52);
            start.gameObject.SetActive(main);
            resume.gameObject.SetActive(menuOptions);
            resume.GetComponentInChildren<Text>().text=finished?"返回结算":"继续行动";
            ((RectTransform)resume.transform).anchoredPosition=new Vector2(0,315);
            respawn.gameObject.SetActive(dead&&ClassicEncounter.Instance);
            ((RectTransform)respawn.transform).anchoredPosition=new Vector2(0,135);
            restart.gameObject.SetActive(menuOptions || finished);
            ((RectTransform)restart.transform).anchoredPosition=new Vector2(0,dead?75:255);
            options.gameObject.SetActive(main||menuOptions);
            home.gameObject.SetActive(menuOptions || finished);
            ((RectTransform)home.transform).anchoredPosition=new Vector2(0,135);
            exit.gameObject.SetActive((main||menuOptions) && !Application.isMobilePlatform);
            heading.text = main ? "TACTICAL ZERO\n战术禁区" : menuOptions ? "战术菜单" : phase == GamePhase.Victory ? "成功撤离" : phase == GamePhase.Defeat ? "行动失败" : "正在载入前哨基地";
            if (main)
                result.text = "OUTPOST 01 / 前哨基地\n穿过道路或沿东侧草丛潜入仓库。\n获取情报后，前往东南撤离区。\n三把武器就绪 · 选择你的接近路线";
            else if (menuOptions)
                result.text = "单人行动已暂停。\nWASD 移动 · 空格跳跃 · E 互动\nR 换弹 · 右键瞄准\n手机：左侧移动，右侧滑动观察";
            else if (finished)
            {
                var s = game.Stats;
                string death = phase == GamePhase.Defeat && game.Player ? "击杀者：" + game.Player.LastDamage.Attacker + " · " + game.Player.LastDamage.Weapon + "\n" : "";
                result.text = death + $"击杀 {s.kills}   命中率 {s.Accuracy:0}%   爆头 {s.headshots}\n承受伤害 {s.damageTaken:0}   用时 {s.seconds:0} 秒\n物资 {s.supplies}   分数 {s.Score}\n" + SaveService.LastError;
                if(BuildingMission.Instance){var m=BuildingMission.Instance; result.text=m.Definition.Name+" · "+ClassicRules.Difficulty.label+" · "+BuildingCatalog.SelectedWeapon+"\n"+result.text+"\n"+(s.damageTaken==0?"无伤行动":"")+"  最佳 "+(PlayerPrefs.HasKey(m.RecordKey)?PlayerPrefs.GetFloat(m.RecordKey).ToString("0.0")+"秒":"暂无");result.fontSize=18;}
            }
            else
                result.text = "正在准备行动区域…";
        }

        private void Update()
        {
            if(fullscreenButton&&settings.gameObject.activeInHierarchy)fullscreenButton.GetComponentInChildren<Text>().text="全屏 F11："+(game.Settings.fullscreen?"开":"关");
            if (!game || !game.IsPlaying || !game.Player || !weapons)
                return;
            float gap = 6 + weapons.Spread * 2;
            for (int i = 0; i < 4; i++)
            {
                crosshair[i].gameObject.SetActive(!weapons.SightEngaged);
                crosshair[i].anchoredPosition = (i == 0 ? Vector2.up : i == 1 ? Vector2.right : i == 2 ? Vector2.down : Vector2.left) * gap;
            }
            hit.text = ""; // Dedicated feedback reticle also handles NOVA and lethal hits.
            hit.color = weapons.HeadHit ? new Color(1, .76f, .38f) : Color.white;
            if (Time.unscaledTime < nextHud)
                return;
            nextHud = Time.unscaledTime + .1f;
            var player = game.Player;
            Vector3 target = game.Objective.HasIntel ? (extraction ? extraction.Position : Vector3.zero) : (terminal ? terminal.transform.position : Vector3.zero);
            Vector3 local = player.transform.InverseTransformDirection(target - player.transform.position);
            string turn = Mathf.Abs(local.x) < 2 && local.z > 0 ? "前方" : local.z < 0 ? "后方" : local.x < 0 ? "左侧" : "右侧";
            objective.text = (game.Objective.HasIntel ? "前往撤离点" : "获取仓库情报") + $" · {Vector3.Distance(player.transform.position, target):0} m · {turn}";
            if(ClassicEncounter.Instance)objective.text=ClassicEncounter.Instance.ObjectiveText;
            if(BuildingMission.Instance){objective.rectTransform.anchoredPosition=new Vector2(390,-79);objective.text=BuildingMission.Instance.ObjectiveText;}
            if (game.Objective.ExtractProgress > 0)
                objective.text += $"\n正在撤离 {game.Objective.Duration - game.Objective.ExtractProgress:0.0}s";
            prompt.text = interaction.Target is ClassicLoot?"":interaction.Prompt;
            conceal.text = visibility.HiddenInGrass ? (visibility.Revealed ? "行踪暴露 · 停止奔跑或开火后恢复" : !visibility.ConcealedByGrass ? "低矮草丛 · 蹲伏隐蔽" : visibility.Moving ? "草丛中移动 · 小心近处敌人" : "已隐蔽 · 近距离仍会被发现") : "";
            if(player.GetComponent<FPSPlayerController>().IsAirborne)conceal.text="腾空中 · 弹道随机 / 自动瞄准暂停";
            if(player.SpawnProtectionSeconds>0)conceal.text="复活保护 "+player.SpawnProtectionSeconds.ToString("0.0")+" 秒 · 开火解除";
            if (Time.time - player.LastHurtAt < .8f)
            {
                Vector3 incoming = player.transform.InverseTransformDirection(player.LastDamage.Origin - player.transform.position);
                direction.text = "受击：" + (Mathf.Abs(incoming.x) > Mathf.Abs(incoming.z) ? incoming.x > 0 ? "右侧 →" : "← 左侧" : incoming.z > 0 ? "↑ 前方" : "↓ 后方");
            }
            else
                direction.text = "";
        }
    }
}
