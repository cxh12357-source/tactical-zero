using UnityEngine;

namespace TacticalZero
{
    public sealed class AimAssistSystem : MonoBehaviour
    {
        [SerializeField]
        private float coneDegrees = 5, magnetDegrees = .3f, scanInterval = .12f, quickDragThreshold = 24;
        private EnemyHealth target;
        private Camera view;
        private float scanAt, acquiredAt;
        private int obstruction;
        private void Start()
        {
            view = GetComponent<PlayerLook>().View;
            obstruction = LayerMask.GetMask("TZWorld", "TZInteractable");
        }

        private bool ClassicAuto => ClassicRules.InMatch && ClassicRules.AutoAim;
        private bool Allowed => GameManager.Instance && view && !GetComponent<FPSPlayerController>().IsAirborne && (ClassicAuto || (GameManager.Instance.Input.TouchMode && GameManager.Instance.Settings.aimAssist > 0));

        private bool Valid(EnemyHealth enemy)
        {
            if (!enemy || !enemy.Alive || !BuildingMission.SameFloor(transform.position,enemy.transform.position))
                return false;
            var line = enemy.AimPoint - view.transform.position;
            if(BuildingMission.Instance)
            {
                Vector3 screen=view.WorldToViewportPoint(enemy.AimPoint);
                if(screen.z<=0||screen.x<0||screen.x>1||screen.y<0||screen.y>1)return false;
            }
            if(ClassicAuto&&ClassicEncounter.Instance&&ClassicEncounter.Instance.InBrush(enemy.transform.position)&&line.magnitude>2.5f&&Time.time-enemy.GetComponent<EnemyCombat>().ShotAt> .9f)return false;
            return line.magnitude < (ClassicAuto?31:55) && Vector3.Angle(view.transform.forward, line) < (ClassicAuto?32:coneDegrees) && !Physics.Linecast(view.transform.position, enemy.AimPoint, obstruction, QueryTriggerInteraction.Ignore);
        }

        private void Acquire()
        {
            if (Valid(target))
                return;
            target = null;
            if (Time.time < scanAt)
                return;
            scanAt = Time.time + scanInterval;
            float angle = BuildingMission.Instance?float.MaxValue:ClassicAuto?32:coneDegrees;
            foreach (var enemy in EnemyHealth.Active)
            {
                if (!Valid(enemy))
                    continue;
                float a = Vector3.Angle(view.transform.forward, enemy.AimPoint - view.transform.position);
                if(BuildingMission.Instance)
                {
                    var combat=enemy.GetComponent<EnemyCombat>();
                    a=Vector3.Distance(view.transform.position,enemy.AimPoint)+a*.12f-(combat&&Time.time-combat.ShotAt<1.5f?5:0);
                }
                if (a < angle)
                {
                    angle = a;
                    target = enemy; acquiredAt=Time.time;
                }
            }
        }

        public float Friction(float drag)
        {
            if (!Allowed || drag > quickDragThreshold)
                return 1;
            Acquire();
            return target ? 1 - .25f * GameManager.Instance.Settings.aimAssist : 1;
        }

        public Vector3 Magnet(Vector3 direction, float range)
        {
            if (!Allowed || (!ClassicAuto&&GameManager.Instance.Input.Mobile.LookDelta.magnitude > quickDragThreshold))
                return direction;
            Acquire();
            if (!target || Vector3.Distance(view.transform.position, target.AimPoint) > range || (BuildingMission.Instance && BuildingCatalog.Sniper(GetComponent<WeaponController>().Current.Id) && Time.time-acquiredAt<.38f))
                return direction;
            // Body center only, bounded angular correction; never rotates the camera or seeks a head.
            return ClassicAuto?(target.AimPoint-view.transform.position).normalized:Vector3.RotateTowards(direction, (target.AimPoint - view.transform.position).normalized, magnetDegrees * Mathf.Deg2Rad * GameManager.Instance.Settings.aimAssist, 0);
        }
    }
}
