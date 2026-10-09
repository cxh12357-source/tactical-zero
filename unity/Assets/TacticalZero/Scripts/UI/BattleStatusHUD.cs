using UnityEngine;
using UnityEngine.UI;

namespace TacticalZero
{
    public sealed class BattleStatusHUD : MonoBehaviour
    {
        GameManager game;RectTransform root,healthFill,damageFill,armorFill,reloadFill;
        Text health,weapon,loaded,reserve,fireMode,armor,heading,stats;SupplyIcon glyph;
        readonly RectTransform[] ticks=new RectTransform[9];readonly Text[] bearings=new Text[9];
        float trailing=100,lastHealth=100,trailAt,nextText;int priorSector=int.MinValue;
        static readonly Color White=new Color(.94f,.95f,.93f),Muted=new Color(.65f,.7f,.72f),Amber=new Color(1,.75f,.30f);
        static readonly string[] Directions={"N","NE","E","SE","S","SW","W","NW"};
        public void Build(Transform parent)
        {
            game=GameManager.Ensure();root=UIFactory.Fill("Battle status",parent);
            var panel=UIFactory.Rect("Vitals",root,new Vector2(.5f,0),new Vector2(0,42),new Vector2(400,46));UIFactory.Panel(panel,new Color(.025f,.035f,.04f,.62f));
            var track=UIFactory.Rect("Health track",panel,new Vector2(.5f,.5f),new Vector2(0,-4),new Vector2(376,12));UIFactory.Panel(track,new Color(.1f,.13f,.14f,.9f));
            damageFill=Bar("Damage trail",track,376,12,new Color(.94f,.65f,.25f));healthFill=Bar("Health fill",track,376,12,White);
            for(int i=1;i<4;i++)UIFactory.Panel(UIFactory.Rect("Health division",track,new Vector2(0,.5f),new Vector2(i*94,0),new Vector2(2,12)),new Color(.035f,.05f,.06f,.85f));
            armorFill=Bar("Armor fill",UIFactory.Rect("Armor track",panel,new Vector2(.5f,.5f),new Vector2(0,7),new Vector2(376,3)),376,3,new Color(.45f,.73f,.83f));
            health=Label("Health",panel,"100",new Vector2(164,21),new Vector2(50,20),14,TextAnchor.MiddleRight);
            armor=Label("Armor",panel,"护甲 0",new Vector2(-134,21),new Vector2(110,20),12);armor.color=Muted;
            var ammo=UIFactory.Rect("Ammunition",root,new Vector2(.5f,0),new Vector2(0,106),new Vector2(400,64));
            glyph=UIFactory.Rect("Equipped weapon icon",ammo,new Vector2(.5f,.5f),new Vector2(-138,0),new Vector2(108,44)).gameObject.AddComponent<SupplyIcon>();glyph.raycastTarget=false;
            weapon=Label("Weapon",ammo,"",new Vector2(-25,14),new Vector2(110,25),14);
            fireMode=Label("Weapon state",ammo,"",new Vector2(-20,-13),new Vector2(120,20),12);fireMode.color=Muted;
            loaded=Label("Loaded ammo",ammo,"",new Vector2(70,0),new Vector2(85,52),38,TextAnchor.MiddleRight);loaded.fontStyle=FontStyle.Bold;loaded.verticalOverflow=VerticalWrapMode.Overflow;
            reserve=Label("Reserve ammo",ammo,"",new Vector2(155,-7),new Vector2(80,32),21);reserve.color=Muted;
            var rail=UIFactory.Rect("Reload track",ammo,new Vector2(.5f,.5f),new Vector2(0,-33),new Vector2(376,2));reloadFill=Bar("Reload fill",rail,0,2,Amber);
            Label("Weapon keys",ammo,BuildingCatalog.InScene?"单武器出战 · R 换弹":"1  副武器     2  主武器",new Vector2(0,39),new Vector2(260,20),11,TextAnchor.MiddleCenter).color=Muted;
            var compass=UIFactory.Rect("Compass",root,new Vector2(.5f,1),new Vector2(0,-26),new Vector2(550,50));
            for(int i=0;i<9;i++)
            {
                ticks[i]=UIFactory.Rect("Bearing tick",compass,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1,6));UIFactory.Panel(ticks[i],Muted);
                bearings[i]=Label("Bearing",compass,"",Vector2.zero,new Vector2(42,20),12,TextAnchor.MiddleCenter);
            }
            heading=Label("Heading",compass,"",new Vector2(0,-22),new Vector2(74,20),12,TextAnchor.MiddleCenter);heading.color=Amber;
            Label("Heading pointer",compass,"▴",new Vector2(0,-5),new Vector2(15,18),14,TextAnchor.MiddleCenter).color=Amber;
            var score=UIFactory.Rect("Match counters",root,new Vector2(1,1),new Vector2(-123,-31),new Vector2(204,34));UIFactory.Panel(score,new Color(.025f,.035f,.04f,.55f));
            stats=Label("Match stats",score,"",Vector2.zero,new Vector2(192,28),15,TextAnchor.MiddleCenter);
        }
        static Text Label(string name,Transform parent,string value,Vector2 position,Vector2 size,int font,TextAnchor align=TextAnchor.MiddleLeft)
        {var t=UIFactory.Text(name,parent,value,new Vector2(.5f,.5f),position,size,font,align);t.color=White;var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(1,-1);return t;}
        static RectTransform Bar(string name,RectTransform parent,float width,float height,Color color)
        {var r=UIFactory.Rect(name,parent,new Vector2(0,.5f),Vector2.zero,new Vector2(width,height));r.pivot=new Vector2(0,.5f);UIFactory.Panel(r,color);return r;}
        void Update()
        {
            if(!game||!root)return;root.gameObject.SetActive(game.IsPlaying);if(!game.IsPlaying||!game.Player)return;
            var player=game.Player;float hp=Mathf.Clamp(player.Health,0,100);
            if(hp<lastHealth)trailAt=Time.unscaledTime+.2f;
            if(hp>lastHealth)trailing=hp;else if(Time.unscaledTime>trailAt)trailing=Mathf.MoveTowards(trailing,hp,Time.unscaledDeltaTime*85);
            lastHealth=hp;healthFill.sizeDelta=new Vector2(hp*3.76f,12);damageFill.sizeDelta=new Vector2(trailing*3.76f,12);armorFill.sizeDelta=new Vector2(Mathf.Clamp(player.Armor,0,100)*3.76f,3);
            healthFill.GetComponent<Image>().color=hp<=30?new Color(1,.34f,.28f):White;
            float yaw=player.transform.eulerAngles.y;int sector=Mathf.RoundToInt(yaw/15);
            for(int i=0;i<9;i++)
            {
                int angle=((sector+i-4)*15%360+360)%360;float x=Mathf.DeltaAngle(yaw,angle)*4.2f;
                ticks[i].anchoredPosition=new Vector2(x,0);bearings[i].rectTransform.anchoredPosition=new Vector2(x,15);
                if(sector!=priorSector)bearings[i].text=angle%45==0?Directions[angle/45]:angle.ToString();
            }
            priorSector=sector;
            var guns=player.GetComponent<WeaponController>();
            if(guns.Current&&guns.Ammo!=null)reloadFill.sizeDelta=new Vector2(guns.Ammo.ReloadRemaining>0?376*(1-guns.Ammo.ReloadRemaining/guns.Current.ReloadSeconds):0,2);
            if(Time.unscaledTime<nextText)return;nextText=Time.unscaledTime+.1f;
            health.text=hp.ToString("0");armor.text="护甲 "+player.Armor.ToString("0");heading.text=Mathf.RoundToInt(yaw).ToString("000")+"°";
            stats.text="守卫 "+EnemyHealth.Active.Count+"    击杀 "+game.Stats.kills;
            if(!guns.Current||guns.Ammo==null)return;
            glyph.Set("weapon",guns.Current.Id,White);weapon.text=guns.Current.DisplayName;loaded.text=guns.Ammo.Loaded.ToString();reserve.text="/ "+guns.Ammo.Reserve;
            loaded.color=guns.Ammo.Loaded==0?new Color(1,.34f,.28f):guns.Ammo.Loaded<=Mathf.Max(3,guns.Current.Magazine*.2f)?Amber:White;
            fireMode.text=guns.Ammo.ReloadRemaining>0?"装填 "+guns.Ammo.ReloadRemaining.ToString("0.0")+"s":guns.Ammo.Loaded==0?"R  换弹":guns.Current.Mode==FireMode.Automatic?"自动":"单发";
        }
    }
}
