using System.Collections;
using UnityEngine;

namespace TacticalZero
{
    public sealed class ClassicLoot : MonoBehaviour,IInteractable
    {
        string label,kind,weapon;bool taken,searching;float landedAt;Vector3 destination;
        public string Label=>searching?"搜索中…":kind=="equipment"?label+" · "+SpecialEquipment.Description(Equipment):label;
        public string WeaponId=>weapon;
        public string Kind=>kind;
        public Vector3 GroundPosition=>landedAt>0?destination-Vector3.up*.5f:transform.position-Vector3.up*.5f;
        public EquipmentKind Equipment=>System.Enum.TryParse<EquipmentKind>(weapon,out var value)?value:EquipmentKind.Armor;
        public void Configure(string name,string type,string gun){label=name;kind=type;weapon=gun;gameObject.AddComponent<SupplyPresentation>().Build(type,gun);}
        public bool CanInteract(PlayerHealth player,out string reason)
        {
            reason=taken?"已搜索":Time.time<landedAt?"空投尚未落地":searching?"正在搜索":!player||player.Health<=0?"无法互动":"";
            if(reason.Length>0)return false;
            if(kind=="heal"&&player.Health>=100)reason="生命已满";
            if(kind=="ammo"&&!player.GetComponent<WeaponController>().NeedsAmmo)reason="备弹已满";
            if(kind=="equipment"&&player.GetComponent<SpecialEquipment>().Count(Equipment)>=2)reason="此装备已满（2 个）· F 使用 / G 切换";
            if(kind!="equipment"&&kind!="heal"&&kind!="ammo"&&kind!="weapon"&&ClassicEncounter.Instance.Bag.Count>=8)reason="背包已满 · B 整理背包";
            return reason.Length==0;
        }
        public bool Interact(PlayerHealth player)
        {
            if(!CanInteract(player,out _))return false;
            if(kind=="weapon"){if(!player.GetComponent<WeaponController>().TakeWeapon(weapon))return false;Finish();}
            else if(kind=="equipment"){if(!player.GetComponent<SpecialEquipment>().Collect(Equipment))return false;ClassicEncounter.Instance.Notice=label+"已装入装备栏 · F 使用 / G 切换";Finish();}
            else if(kind=="heal"){player.Heal(40);Finish();}
            else if(kind=="ammo"){if(!player.GetComponent<WeaponController>().RefillAll())return false;Finish();}
            else StartCoroutine(Search(player));
            return true;
        }
        IEnumerator Search(PlayerHealth player)
        {
            searching=true;float elapsed=0,hurt=player.LastHurtAt;Vector3 start=player.transform.position;float duration=kind=="tech"?3.6f:1.2f;
            while(elapsed<duration)
            {
                yield return null;
                var game=GameManager.Instance;
                if(!game.IsPlaying||player.LastHurtAt!=hurt||Vector3.Distance(start,player.transform.position)>.15f||game.Input.FireHeld){searching=false;ClassicEncounter.Instance.SearchProgress=0;yield break;}
                elapsed+=Time.deltaTime;ClassicEncounter.Instance.SearchProgress=elapsed/duration;
            }
            searching=false;ClassicEncounter.Instance.SearchProgress=0;
            if(ClassicEncounter.Instance.AddContents(kind))
            {
                if(kind=="arms"){string[] types={"AK47","MP6","M4A1","DMR","RPK","Shotgun","VECTOR"};string id=types[Random.Range(0,types.Length)];ClassicEncounter.Instance.SpawnLoot(transform.position+Vector3.right*.8f-Vector3.up*.5f,id,"weapon",id);}
                Finish();
            }
        }
        void Finish(){taken=true;AudioService.Instance?.Play("pickup",transform.position,.8f,true);ClassicEncounter.Instance?.ReleaseSupply(this);gameObject.SetActive(false);Destroy(gameObject);}
        public void BeginDrop(){landedAt=Time.time+6;destination=transform.position;transform.position+=Vector3.up*16;}
        void Update(){if(landedAt>0)transform.position=destination+Vector3.up*Mathf.Max(0,(landedAt-Time.time)/6*16);}
    }
}
