using UnityEngine;
using UnityEngine.UI;

namespace TacticalZero
{
    // Project markers after both PlayerLook and PlayerInteraction have resolved this frame.
    [DefaultExecutionOrder(200)]
    public sealed class SupplyHUD : MonoBehaviour
    {
        GameManager game;RectTransform root,pickup;SupplyIcon pickupIcon;Text title,description,key;
        readonly RectTransform[] markers=new RectTransform[20];readonly SupplyIcon[] markerIcons=new SupplyIcon[20];
        Canvas canvas;float nextInventory;int worldMask;
        public void Build(Transform parent)
        {
            game=GameManager.Ensure();canvas=parent.GetComponentInParent<Canvas>();root=UIFactory.Fill("Supply HUD",parent);
            worldMask=LayerMask.GetMask("TZWorld");
            pickup=UIFactory.Rect("Pickup card",root,new Vector2(.5f,.5f),new Vector2(270,-25),new Vector2(340,84));UIFactory.Panel(pickup,new Color(.025f,.055f,.08f,.96f));
            var accent=UIFactory.Rect("Pickup accent",pickup,new Vector2(0,.5f),new Vector2(2,0),new Vector2(3,84));UIFactory.Panel(accent,new Color(.9f,.69f,.26f));
            pickupIcon=Icon("Pickup icon",pickup,new Vector2(0,.5f),new Vector2(41,0),new Vector2(68,68));
            title=UIFactory.Text("Pickup name",pickup,"",new Vector2(0,1),new Vector2(206,-20),new Vector2(244,28),18);
            description=UIFactory.Text("Pickup description",pickup,"",new Vector2(0,1),new Vector2(206,-45),new Vector2(244,28),12);
            key=UIFactory.Text("Pickup key",pickup,"",new Vector2(0,0),new Vector2(206,12),new Vector2(244,20),12);key.color=new Color(1,.82f,.43f);
            for(int i=0;i<markers.Length;i++)
            {
                markers[i]=UIFactory.Rect("World supply marker "+i,root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(38,38));UIFactory.Panel(markers[i],new Color(.025f,.055f,.08f,.18f));
                markerIcons[i]=Icon("World supply icon",markers[i],new Vector2(.5f,.5f),Vector2.zero,new Vector2(34,34));markers[i].gameObject.SetActive(false);
            }
            pickup.SetAsLastSibling();pickup.gameObject.SetActive(false);
        }
        static SupplyIcon Icon(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var icon=UIFactory.Rect(name,parent,anchor,position,size).gameObject.AddComponent<SupplyIcon>();icon.raycastTarget=false;return icon;
        }
        public static Color Tint(ClassicLoot loot)=>loot.Kind=="weapon"?new Color(1,.77f,.35f):loot.Kind=="equipment"?SpecialEquipment.Tint(loot.Equipment):new Color(.4f,.87f,.74f);
        void LateUpdate()
        {
            if(!root||!game)return;root.gameObject.SetActive(game.IsPlaying);
            if(!game.IsPlaying||!game.Player)return;
            var player=game.Player;
            var interaction=player.GetComponent<PlayerInteraction>();var target=interaction.Target as ClassicLoot;
            bool show=target&&target.gameObject.activeInHierarchy;pickup.gameObject.SetActive(show);
            if(show)
            {
                pickupIcon.Set(target.Kind,target.WeaponId,Tint(target));
                title.text=target.Kind=="weapon"?ClassicRules.Gun(target.WeaponId).label:target.Kind=="equipment"?SpecialEquipment.Label(target.Equipment):target.Label;
                description.text=target.Kind=="weapon"?"主武器补给 · 拾取后替换当前主武器":target.Kind=="equipment"?SpecialEquipment.Description(target.Equipment):target.Kind=="ammo"?"补充普通枪械备弹":target.Kind=="heal"?"恢复 40 点生命":"搜索战利品 · 移动或受击会中断";
                key.text=target.CanInteract(player,out var reason)?"[ E ]  拾取 / 搜索":reason;
            }
            int used=0;var mission=ClassicEncounter.Instance;var view=player.GetComponent<PlayerLook>().View;
            if(mission)foreach(var loot in mission.Supplies)
            {
                if(!loot||!loot.gameObject.activeSelf||loot==target||used>=markers.Length)continue;
                Vector3 point=loot.GroundPosition+Vector3.up*1.6f;float distance=Vector3.Distance(view.transform.position,point);
                if(distance>14||distance<1.2f||Physics.Linecast(view.transform.position,point,worldMask,QueryTriggerInteraction.Ignore))continue;
                var screen=view.WorldToScreenPoint(point);if(screen.z<=0||!view.pixelRect.Contains(new Vector2(screen.x,screen.y)))continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local);
                var marker=markers[used];marker.anchoredPosition=local;marker.gameObject.SetActive(true);markerIcons[used].Set(loot.Kind,loot.WeaponId,Tint(loot));used++;
            }
            for(int i=used;i<markers.Length;i++)markers[i].gameObject.SetActive(false);
        }
    }
}