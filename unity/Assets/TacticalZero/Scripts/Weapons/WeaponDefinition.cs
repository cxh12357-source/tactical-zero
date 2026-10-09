using UnityEngine;

namespace TacticalZero
{
    public enum WeaponKind
    {
        Pistol,
        Rifle,
        Shotgun
    }

    public enum FireMode
    {
        SemiAutomatic,
        Automatic
    }

    [CreateAssetMenu(menuName = "Tactical Zero/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField]
        private string weaponId, displayName;
        [SerializeField]
        private WeaponKind kind;
        [SerializeField]
        private FireMode fireMode;
        [SerializeField]
        private float damage = 30, fireInterval = .26f, reloadSeconds = 1.4f, range = 48;
        [SerializeField]
        private int magazineCapacity = 12, reserveCapacity = 60, pellets = 1;
        [SerializeField]
        private float hipSpread = 1.1f, adsSpread = .25f, recoil = .7f, recoilRecovery = 12, moveSpreadMultiplier = 1.5f, headshotMultiplier = 2, noiseRadius = 38, adsFov = 54;
        [SerializeField]
        private int penetrationTargets = 0;
        [SerializeField]
        private float penetrationDamageFactor = .65f;
        [SerializeField]
        private GameObject muzzleEffect, impactEffect, firstPersonModel;
        [SerializeField]
        private AudioClip shotAudio;
        [SerializeField]
        private Sprite icon;
        public string Id => weaponId;
        public string DisplayName => displayName;
        public WeaponKind Kind => kind;
        public FireMode Mode => fireMode;
        public float Damage => damage;
        public float Interval => fireInterval;
        public float ReloadSeconds => reloadSeconds;
        public float Range => range;
        public int Magazine => magazineCapacity;
        public int Reserve => reserveCapacity;
        public int Pellets => pellets;
        public float HipSpread => hipSpread;
        public float AdsSpread => adsSpread;
        public float Recoil => recoil;
        public float RecoilRecovery => recoilRecovery;
        public float MoveSpreadMultiplier => moveSpreadMultiplier;
        public float HeadshotMultiplier => headshotMultiplier;
        public float NoiseRadius => noiseRadius;
        public float AdsFov => adsFov;
        public GameObject Model => firstPersonModel;
        public AudioClip ShotAudio => shotAudio;
        public int PenetrationTargets => penetrationTargets;
        public float PenetrationFactor => penetrationDamageFactor;
        public void ConfigureClassic(ClassicGun gun, GameObject model, AudioClip clip)
        {
            Configure(gun.id,gun.label,gun.id=="USP"?WeaponKind.Pistol:gun.kind=="shotgun"?WeaponKind.Shotgun:WeaponKind.Rifle,
                gun.kind=="pistol"||gun.kind=="dmr"||gun.kind=="shotgun"||gun.kind=="rail"||gun.kind=="launcher"?FireMode.SemiAutomatic:FireMode.Automatic,
                gun.damage,gun.interval,gun.mag,gun.reserve,Mathf.Max(.1f,gun.reload),gun.range*3/64,gun.pellets,gun.spread*Mathf.Rad2Deg,gun.recoil*Mathf.Rad2Deg,model,clip);
            headshotMultiplier=gun.head;adsSpread=hipSpread*.25f;penetrationTargets=gun.pierce;penetrationDamageFactor=.75f;
            // Explicit FPS tuning: 2D spread/recoil values do not map to usable ADS angles.
            bool precision=gun.kind=="dmr"||gun.kind=="rail";
            hipSpread=gun.id=="USP"?.6f:precision?.7f:gun.kind=="shotgun"?4.5f:gun.kind=="smg"?1.1f:.8f;
            adsSpread=precision?.025f:gun.kind=="shotgun"?3.2f:gun.kind=="smg"?.12f:gun.kind=="launcher"?.12f:.065f;
            recoil=gun.id=="AWP"?1.5f:precision?.7f:gun.kind=="shotgun"?1.2f:gun.kind=="smg"?.2f:.32f;
            recoilRecovery=15;moveSpreadMultiplier=1.2f;
            range=precision?130:gun.kind=="shotgun"?38:gun.kind=="smg"?60:gun.kind=="launcher"?55:gun.id=="USP"?65:90;
            adsFov=gun.kind=="dmr"?35:54;
        }
        public bool Valid => !string.IsNullOrWhiteSpace(weaponId) && damage > 0 && fireInterval > 0 && magazineCapacity > 0 && reserveCapacity >= 0 && reloadSeconds > 0 && range > 0 && pellets >= 1 && pellets <= 16 && hipSpread >= 0 && adsSpread >= 0 && headshotMultiplier >= 1;

        public void Configure(string id, string label, WeaponKind type, FireMode mode, float hit, float interval, int mag, int reserve, float reload, float reach, int count, float spread, float kick, GameObject model, AudioClip clip)
        {
            weaponId = id;
            displayName = label;
            kind = type;
            fireMode = mode;
            damage = hit;
            fireInterval = interval;
            magazineCapacity = mag;
            reserveCapacity = reserve;
            reloadSeconds = reload;
            range = reach;
            pellets = count;
            hipSpread = spread;
            adsSpread = type == WeaponKind.Shotgun ? spread * .75f : .25f;
            recoil = kick;
            firstPersonModel = model;
            shotAudio = clip;
        }

        private void OnValidate()
        {
            if (!Valid)
                Debug.LogWarning("Invalid weapon definition '" + name + "'. Set ID, positive damage/interval/capacity/reload/range and 1–16 pellets.", this);
        }
    }
}
