using UnityEngine;

namespace TacticalZero
{
    public sealed class WeaponController : MonoBehaviour
    {
        [SerializeField]
        private Camera view;
        private WeaponDefinition[] definitions;
        private AmmoState[] ammunition;
        private GameObject[] models;
        private Transform mount;
        private PlayerLook look;
        private FPSPlayerController movement;
        private PlayerVisibilityState visibility;
        private AimAssistSystem assist;
        private int slot, shotMask;
        private float heat, switchUntil, emptyAt, mechanicalKick, turnSway, verticalSway;
        private bool classic, building;
        private int primary=-1;
        public bool ClassicInventory => classic;
        public bool SightEngaged => Current && GameManager.Instance && GameManager.Instance.IsPlaying && GameManager.Instance.Input.Aiming && Ammo != null && Ammo.ReloadRemaining <= 0;
        public bool ScopeActive => SightEngaged && WeaponSight.For(Current.Id).Magnified;
        public WeaponDefinition Current => definitions != null && definitions.Length > 0 ? definitions[slot] : null;
        public AmmoState Ammo => ammunition != null && ammunition.Length > 0 ? ammunition[slot] : null;
        public float Spread { get; private set; }
        public float HitUntil { get; private set; }
        public bool HeadHit { get; private set; }
        public string Message { get; private set; } = "";

        public void Configure(Camera camera)
        {
            view = camera;
        }

        private void Start()
        {
            var game = GameManager.Ensure();
            if (!game.Catalog)
            {
                Debug.LogError("Missing PrototypeCatalog. Run Tactical Zero setup before Play Mode.", this);
                enabled = false;
                return;
            }

            classic=ClassicRules.InMatch;
            building=BuildingCatalog.InScene;
            definitions = classic ? System.Array.ConvertAll(ClassicRules.Data.weapons,g=>ClassicRules.Weapon(g.id)) : game.Catalog.Weapons;
            if (definitions == null || definitions.Length < 3)
            {
                Debug.LogError("Three weapon definitions missing. Run Tactical Zero/Setup Complete 3D Prototype.", this);
                enabled = false;
                return;
            }

            if(BuildingCatalog.InScene) definitions=System.Array.ConvertAll(definitions,BuildingCatalog.Weapon);
            look = GetComponent<PlayerLook>();
            movement = GetComponent<FPSPlayerController>();
            visibility = GetComponent<PlayerVisibilityState>();
            assist = GetComponent<AimAssistSystem>();
            ammunition = new AmmoState[definitions.Length];
            models = new GameObject[definitions.Length];
            mount = new GameObject("Weapon mount").transform;
            mount.SetParent(view.transform, false);
            mount.localPosition = new Vector3(.21f, -.255f, .38f);
            for (int i = 0; i < definitions.Length; i++)
            {
                var d = definitions[i];
                if (!d || !d.Valid)
                {
                    Debug.LogError("Invalid weapon in PrototypeCatalog slot " + i, this);
                    enabled = false;
                    return;
                }

                ammunition[i] = new AmmoState(d.Magazine, d.Reserve);
                if (d.Model)
                {
                    if(!classic || i==0) {models[i] = Instantiate(d.Model, mount);models[i].SetActive(i == 0);TintSleeves(models[i]);}
                }
            }

            shotMask = LayerMask.GetMask("TZWorld", "TZEnemy", "TZInteractable");
        }

        private void Update()
        {
            var game = GameManager.Instance;
            if (!game || !game.IsPlaying || !Current)
                return;
            var input = game.Input;
            Ammo.Tick(Time.deltaTime);
            heat = Mathf.MoveTowards(heat, 0, Time.deltaTime * 1.8f);
            int selected = input.WeaponSlot();
            if (selected != -1 && !BuildingCatalog.InScene)
                Switch(classic ? (selected==0?0:selected==-2?(slot==0?primary:0):primary) : selected == -2 ? (slot + 1) % definitions.Length : selected);
            if (input.ReloadPressed && Ammo.StartReload(Current.ReloadSeconds))
            {
                Message = "换弹中";
                AudioService.Instance?.Play("reload", transform.position, .6f, false);
                if(BuildingMission.Instance)NoiseBus.Emit(transform.position,8,NoiseKind.Reload);
            }

            bool held = input.FireHeld;
            bool pressed = input.FirePressed;
            if (Current.Mode == FireMode.Automatic ? held : pressed)
                TryShoot(input.Aiming);
            Spread = EffectiveSpread(input.Aiming);
            var sight=WeaponSight.For(Current.Id);
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, SightEngaged ? sight.Fov : 84, Time.deltaTime * 12);
            if(models[slot])models[slot].SetActive(!ScopeActive);
            float sway = game.Settings.weaponSway ? Mathf.Sin(Time.time * movement.Speed * 2) * .006f * Mathf.Min(1, movement.Speed) : 0;
            // Iron / reflex sight optical centers align with the camera; scopes use a full aperture view.
            Vector3 position = SightEngaged ? new Vector3(0, -sight.Height, .5f) : new Vector3(.21f, -.255f, .38f);
            float reloadAmount = Ammo.ReloadRemaining > 0 ? Mathf.Sin(Ammo.ReloadRemaining / Current.ReloadSeconds * Mathf.PI) : 0;
            Vector3 desiredMount=position+Vector3.up*(SightEngaged?0:sway)+new Vector3(-.045f,.12f,-.065f)*reloadAmount;
            mechanicalKick=Mathf.MoveTowards(mechanicalKick,0,Time.deltaTime*3.8f);
            float swayFactor=game.Settings.weaponSway&&!SightEngaged?1:0;
            turnSway=Mathf.Lerp(turnSway,Mathf.Clamp(-input.Look.x*.045f,-2.2f,2.2f)*swayFactor,Time.deltaTime*13);
            verticalSway=Mathf.Lerp(verticalSway,Mathf.Clamp(input.Look.y*.035f,-1.5f,1.5f)*swayFactor,Time.deltaTime*13);
            float wallRaise=0;
            if(!SightEngaged&&Physics.SphereCast(view.transform.position,.08f,view.transform.forward,out var wallHit,.95f,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore))wallRaise=Mathf.Clamp01((.80f-wallHit.distance)/.65f);
            desiredMount+=new Vector3(0,-.055f*wallRaise,-.08f*mechanicalKick-.15f*wallRaise);
            mount.localPosition=Vector3.Lerp(mount.localPosition,desiredMount,1-Mathf.Exp(-Time.deltaTime*20));
            mount.localRotation = Quaternion.Euler(-8*reloadAmount-7*mechanicalKick-28*wallRaise+verticalSway, -12*reloadAmount+turnSway, -24*reloadAmount+turnSway*.35f);
            if (Ammo.ReloadRemaining == 0)
                Message = Ammo.Loaded == 0 ? "空仓 · 按 R 换弹" : "";
        }

        public void Switch(int index)
        {
            if (ammunition == null || index < 0 || index >= ammunition.Length || slot == index || (classic&&index!=0&&index!=primary))
                return;
            Ammo.CancelReload();
            if (models[slot])
                models[slot].SetActive(false);
            slot = index;
            if(!models[slot]&&definitions[slot].Model) {models[slot]=Instantiate(definitions[slot].Model,mount);TintSleeves(models[slot]);}
            if (models[slot])
                models[slot].SetActive(true);
            heat = 0;
            switchUntil = Time.time + .25f;
        }

        public bool RefillAll()
        {
            if (ammunition == null)
                return false;
            bool changed = false;
            for (int i = 0; i < ammunition.Length; i++)
                if(!classic || ((i==0||i==primary)&&!ClassicRules.Gun(definitions[i].Id).limited)) changed |= ammunition[i].Refill(definitions[i].Magazine * 2);
            return changed;
        }

        public bool NeedsAmmo
        {
            get
            {
                if (ammunition == null)
                    return false;
                for(int i=0;i<ammunition.Length;i++)
                    if ((!classic||((i==0||i==primary)&&!ClassicRules.Gun(definitions[i].Id).limited)) && ammunition[i].Reserve < ammunition[i].MaximumReserve)
                        return true;
                return false;
            }
        }

        public bool TryShoot(bool aiming = false)
        {
            if (!Current || !GameManager.Instance.IsPlaying || Time.time < switchUntil || (BuildingMission.Instance && BuildingMission.Instance.Transitioning))
                return false;
            if (!Ammo.TryFire(Time.time, Current.Interval))
            {
                if (Ammo.Loaded == 0 && Time.time >= emptyAt)
                {
                    emptyAt = Time.time + .4f;
                    AudioService.Instance?.Play("empty", transform.position, .5f, false);
                }

                return false;
            }

            if(models[slot]&&models[slot].TryGetComponent<WeaponReloadAnimation>(out var reloadAnimation))reloadAnimation.Shot();
            mechanicalKick=Mathf.Min(1,mechanicalKick+(Current.Kind==WeaponKind.Shotgun?.85f:.38f));
            visibility?.OnShot();
            NoiseBus.Emit(transform.position, BuildingMission.Instance ? (BuildingCatalog.Sniper(Current.Id)||Current.Id=="MG3"?70:BuildingCatalog.Mobility(Current.Id)>1?28:45) : Current.NoiseRadius, NoiseKind.Gunshot);
            AudioService.Instance?.PlayClip(Current.ShotAudio, transform.position, .8f, false);
            look.Recoil(Current.Recoil, Current.RecoilRecovery);
            heat = Mathf.Min(.45f, heat + (Current.Kind == WeaponKind.Rifle ? .11f : .045f));
            float spread = EffectiveSpread(aiming);
            Vector3 muzzle = view.transform.TransformPoint(new Vector3(.18f, -.15f, .7f));
            var muzzleSocket=models[slot]?models[slot].transform.Find("Muzzle"):null;if(muzzleSocket)muzzle=muzzleSocket.position;
            EffectPool.Instance?.MuzzleFlash(muzzle,view.transform.forward);
            EffectPool.Instance?.Shell(view.transform.TransformPoint(new Vector3(.3f, -.18f, .5f)), view.transform.right);
            var stats = GameManager.Instance.Stats;
            stats.shots++;
            var classicGun=classic?ClassicRules.Gun(Current.Id):null;
            if(classicGun!=null&&classicGun.blast>0)
            {
                Vector3 launch=ShotDirection(spread);
                if(Physics.Linecast(view.transform.position,muzzle,out var blocked,LayerMask.GetMask("TZWorld","TZInteractable"),QueryTriggerInteraction.Ignore)) muzzle=blocked.point-launch*.12f;
                NovaProjectile.Launch(muzzle,launch,classicGun,gameObject);
                return true;
            }
            bool hitAny = false, headAny = false;
            for (int i = 0; i < Current.Pellets; i++)
            {
                float fan=classicGun!=null?classicGun.fan*Mathf.Rad2Deg*(i-(Current.Pellets-1)*.5f):0;
                Vector3 direction = ShotDirection(spread,fan);
                if (!HitscanSolver.Trace(view.transform.position, muzzle, direction, Current.Range, shotMask, out var hit))
                {
                    EffectPool.Instance?.Tracer(muzzle,view.transform.position+direction*Current.Range,Current.PenetrationTargets>0);
                    continue;
                }
                EffectPool.Instance?.Tracer(muzzle,hit.point,Current.PenetrationTargets>0);
                var box = hit.collider.GetComponent<Hitbox>();
                bool hostile = box && box.Health && box.Health.Alive && BuildingMission.SameFloor(transform.position,box.Health.transform.position);
                EffectPool.Instance?.Impact(hit.point,hit.normal,hostile);
                if(BuildingMission.Instance)BuildingAcoustics.Emit(hit.point-Vector3.up*.5f,8,NoiseKind.Impact);
                if (!hostile)
                    continue;
                if(Current.PenetrationTargets>0)
                {
                    // Keep the penetrating beam on the sight line after the muzzle obstruction check.
                    // Converging on the first body from an offset muzzle would drift away from later targets.
                    var hits=Physics.RaycastAll(view.transform.position,direction,Current.Range,shotMask,QueryTriggerInteraction.Ignore);
                    System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
                    var struck=new System.Collections.Generic.HashSet<EnemyHealth>();
                    foreach(var railHit in hits)
                    {
                        var railBox=railHit.collider.GetComponent<Hitbox>();
                        if(!railBox) break;
                        if(!railBox.Health||!railBox.Health.Alive||!BuildingMission.SameFloor(transform.position,railBox.Health.transform.position)||!struck.Add(railBox.Health))continue;
                        float power=DamageModel.Calculate(Current.Damage*Mathf.Pow(Current.PenetrationFactor,struck.Count-1),railBox.Head,Current.HeadshotMultiplier,railHit.distance,Current.Range,false);
                        railBox.Health.Apply(new DamageInfo(power,transform.position,"玩家",Current.DisplayName,railBox.Head));
                        EffectPool.Instance?.Number(railHit.point,Mathf.RoundToInt(power),railBox.Head);
                        hitAny=true;headAny|=railBox.Head;if(struck.Count>=Current.PenetrationTargets)break;
                    }
                    continue;
                }
                float damage = DamageModel.Calculate(Current.Damage, box.Head, Current.HeadshotMultiplier, Vector3.Distance(muzzle, hit.point), Current.Range, Current.Kind == WeaponKind.Shotgun);
                if(BuildingMission.Instance && BuildingCatalog.Mobility(Current.Id)==1.1f)damage*=Mathf.Lerp(1,.25f,Mathf.InverseLerp(12,45,Vector3.Distance(muzzle,hit.point)));
                box.Health.Apply(new DamageInfo(damage, transform.position, "玩家", Current.DisplayName, box.Head));
                EffectPool.Instance?.Number(hit.point, Mathf.RoundToInt(damage), box.Head);
                hitAny = true;
                headAny |= box.Head;
            }

            if (hitAny)
            {
                stats.hits++;
                if (headAny)
                    stats.headshots++;
                HitUntil = Time.time + .18f;
                HeadHit = headAny;
                // Audio and HUD confirmation come from actual damage, including explosive kills.
            }

            return true;
        }

        public float EffectiveSpread(bool aiming)
        {
            float spread=(aiming?Current.AdsSpread:Current.HipSpread)+heat*(aiming?.18f:1);
            if(movement.Speed>.3f)spread*=Current.MoveSpreadMultiplier;
            // Airborne instability applies after ADS and profession bonuses.
            return movement.IsAirborne?Mathf.Max(2.2f,spread*1.8f):spread;
        }
        public Vector3 ShotDirection(float spread, float fan=0)
        {
            var jitter=Random.insideUnitCircle*spread;
            var forward=assist&&!movement.IsAirborne?assist.Magnet(view.transform.forward,Current.Range):view.transform.forward;
            return Quaternion.LookRotation(forward)*Quaternion.Euler(jitter.y,jitter.x+fan,0)*Vector3.forward;
        }
        public void ResetLoadout()
        {
            if(!classic||ammunition==null)return;
            Switch(0);
            if(primary>0&&models[primary])Destroy(models[primary]);
            primary=-1;
            ammunition[0]=new AmmoState(definitions[0].Magazine,definitions[0].Reserve);
            heat=HitUntil=emptyAt=0; switchUntil=Time.time+.25f; Message="";
        }

        public bool SupplyCurrent() => Current && (BuildingCatalog.InScene||!classic||!ClassicRules.Gun(Current.Id).limited) && Ammo.Refill(Current.Magazine);
        void OnDestroy(){if(building&&definitions!=null)foreach(var definition in definitions)if(definition)Destroy(definition);}
        void TintSleeves(GameObject model)
        {
            if(!classic)return;var cloth=Resources.Load<Material>("Classic/Materials/Portrait06_"+"assault"+" fabric")??Resources.Load<Material>("Classic/Materials/Armor_"+"assault");
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())if(renderer.name.Contains("sleeve"))renderer.sharedMaterial=cloth;
        }
        public bool TakeWeapon(string id)
        {
            if(!classic||ammunition==null) return false;
            int index=System.Array.FindIndex(definitions,d=>d.Id==id);if(index<1)return false;
            if(primary>=0&&primary!=index) {Switch(0); if(models[primary]) Destroy(models[primary]);}
            primary=index;ammunition[index]=new AmmoState(definitions[index].Magazine,definitions[index].Reserve);Switch(index);return true;
        }
    }
}
