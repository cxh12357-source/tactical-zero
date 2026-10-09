using System.Collections.Generic;
using UnityEngine;

namespace TacticalZero
{
    public sealed class PlayerVisibilityState : MonoBehaviour
    {
        private readonly HashSet<GrassVolume> volumes = new HashSet<GrassVolume>();
        private FPSPlayerController movement;
        private float revealUntil;
        public bool HiddenInGrass => volumes.Count > 0;
        public bool ConcealedByGrass {get{if(!movement||!movement.Grounded)return false;foreach(var volume in volumes)if(volume&&(volume.ConcealsStanding||Crouched))return true;return false;}}
        public bool Crouched => movement && movement.IsCrouched;
        public bool Moving => movement && movement.Speed > .3f;
        public bool Sprinting => movement && movement.IsSprinting;
        public bool Revealed => Time.time < revealUntil;

        private void Awake()
        {
            movement = GetComponent<FPSPlayerController>();
        }
        void Update(){if(HiddenInGrass&&Sprinting)revealUntil=Mathf.Max(revealUntil,Time.time+.65f);}

        public void Enter(GrassVolume volume)
        {
            volumes.Add(volume);
        }

        public void Exit(GrassVolume volume)
        {
            volumes.Remove(volume);
        }

        public void OnShot()
        {
            revealUntil = Time.time + 2.5f;
        }

        public void ResetForRespawn() { volumes.Clear(); revealUntil=0; }

        private void OnDisable()
        {
            volumes.Clear();
        }
    }
}
