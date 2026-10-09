using UnityEngine;
namespace TacticalZero
{
    public sealed class BuildingMedkit:MonoBehaviour,IInteractable
    {
        public string Label=>"急救包 +35";
        public bool CanInteract(PlayerHealth player,out string reason){reason=player.Health>=100?"生命已满":"";return player.Health<100;}
        public bool Interact(PlayerHealth player){if(!CanInteract(player,out _))return false;player.Heal(35);GetComponent<Collider>().enabled=false;gameObject.SetActive(false);return true;}
    }
}
