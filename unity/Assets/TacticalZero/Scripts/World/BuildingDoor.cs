using UnityEngine;
namespace TacticalZero
{
    public sealed class BuildingDoor : MonoBehaviour,IInteractable
    {
        public int Floor; public Vector3 SlideDirection=Vector3.right; public static int Revision {get;private set;}
        public bool Open {get;private set;}
        Vector3 shut;Collider barrier;float nextProbe;
        public string Label=>Open?"关门":"开门";
        void Awake(){shut=transform.localPosition;barrier=GetComponent<Collider>();}
        public bool CanInteract(PlayerHealth player,out string reason){reason="";return BuildingMission.SameFloor(transform.position-Vector3.up*1.2f,player.transform.position);}
        public bool Interact(PlayerHealth player){SetOpen(!Open);return true;}
        public void SetOpen(bool value)
        {
            if(value==Open)return;
            // Retracting leaf leaves the full 1.9m doorway clear, including its return path.
            Vector3 doorway=transform.parent?transform.parent.TransformPoint(shut):shut;
            if(!value&&Physics.CheckBox(doorway,SlideDirection.x>.5f?new Vector3(1.2f,1.2f,.6f):new Vector3(.6f,1.2f,1.2f),Quaternion.identity,LayerMask.GetMask("TZPlayer","TZEnemy"),QueryTriggerInteraction.Ignore))return;
            Open=value;Revision++;barrier.enabled=!Open;transform.localPosition=shut+(Open?SlideDirection*2.2f:Vector3.zero);
            BuildingAcoustics.Emit(transform.position-Vector3.up*1.2f,13,NoiseKind.Door);
        }
        void Update()
        {
            if(Open||Time.time<nextProbe||!BuildingMission.Instance||!GameManager.Instance.IsPlaying)return;nextProbe=Time.time+.25f;
            foreach(var e in EnemyHealth.Active)if(BuildingMission.FloorAt(e.transform.position)==Floor&&(e.transform.position-(transform.position-Vector3.up*1.2f)).sqrMagnitude<5){SetOpen(true);break;}
        }
    }
}
