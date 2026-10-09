using UnityEngine;
namespace TacticalZero
{
    public sealed class BuildingStair:MonoBehaviour
    {
        public int Floor,Side;public bool Up;
        void OnTriggerEnter(Collider other)
        {
            var player=other.GetComponentInParent<PlayerHealth>();var mission=BuildingMission.Instance;
            if(player&&mission&&GameManager.Instance.IsPlaying&&!mission.Transitioning&&BuildingMission.FloorAt(player.transform.position)==Floor)
                mission.StartCoroutine(mission.ChangeFloor(Floor+(Up?1:-1),Side,Up));
        }
    }
}
