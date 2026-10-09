using UnityEngine;
using UnityEngine.UI;
namespace TacticalZero
{
    public sealed class BuildingDeploymentUI:MonoBehaviour
    {
        RectTransform root,content;GameManager game;System.Action settings;int step;
        static readonly Color Selected=new Color(.15f,.46f,.43f),Dark=new Color(.055f,.085f,.1f);
        public void Build(Transform parent,System.Action openSettings)
        {
            game=GameManager.Ensure();settings=openSettings;
            root=UIFactory.Fill("Building deployment",parent);UIFactory.Panel(root,new Color(.016f,.025f,.03f,.99f),true);
            content=UIFactory.Rect("Action preparation",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1180,650));Draw();
        }
        public void SetVisible(bool value){if(root)root.gameObject.SetActive(value);}
        void Draw()
        {
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            UIFactory.Text("Title",content,"TACTICAL ZERO / BUILDING SWEEP   ·   UNITY 3D 0.18",new Vector2(.5f,.5f),new Vector2(0,295),new Vector2(1100,36),22).color=new Color(.55f,.83f,.76f);
            UIFactory.Button(step==0?"设置":"← 返回",content,new Vector2(.5f,.5f),new Vector2(-465,-285),new Vector2(190,48),()=>{if(step==0)settings();else{step--;Draw();}});
            if(step==0)
            {
                UIFactory.Text("Mission",content,"战术禁区\n建筑清剿",new Vector2(.5f,.5f),new Vector2(-245,95),new Vector2(560,210),58);
                UIFactory.Text("Brief",content,"第一人称战术行动\n逐层排查 / 听声辨位 / 全部清除后撤离",new Vector2(.5f,.5f),new Vector2(-235,-83),new Vector2(570,100),21);
                var portrait=UIFactory.Rect("干员",content,new Vector2(.5f,.5f),new Vector2(300,20),new Vector2(260,390)).gameObject.AddComponent<RawImage>();portrait.texture=Resources.Load<Texture2D>("Classic/Portraits/assault");portrait.raycastTarget=false;
                UIFactory.Button("开始行动 →",content,new Vector2(.5f,.5f),new Vector2(385,-270),new Vector2(340,64),()=>{BuildingCatalog.Enabled=true;step=1;Draw();});
                return;
            }
            if(step==1)
            {
                Label("01  选择本局武器 · 每局携带一把",new Vector2(0,245));
                for(int i=0;i<BuildingCatalog.Weapons.Length;i++)
                {
                    string id=BuildingCatalog.Weapons[i];var gun=ClassicRules.Weapon(id);int n=i;
                    var b=UIFactory.Button("",content,new Vector2(.5f,.5f),new Vector2(-420+i%4*280,150-i/4*119),new Vector2(265,108),()=>{BuildingCatalog.SelectedWeapon=id;Draw();});b.name="武器 "+id;b.image.color=BuildingCatalog.SelectedWeapon==id?Selected:Dark;
                    var icon=UIFactory.Rect("Gun artwork",b.transform,new Vector2(.5f,.5f),new Vector2(-76,3),new Vector2(104,65)).gameObject.AddComponent<RawImage>();icon.texture=Resources.Load<Texture2D>("Classic/Previews/gun_"+id);icon.raycastTarget=false;
                    var specs=UIFactory.Text("Specs",b.transform,id+"  "+(BuildingCatalog.SelectedWeapon==id?"✓":"")+"\n伤害 "+(BuildingCatalog.Sniper(id)?115:gun.Damage)+" · 弹匣 "+(BuildingCatalog.Sniper(id)?5:id=="MG3"||id=="RPK"?60:gun.Magazine)+"\n射速 "+Mathf.RoundToInt(60/(BuildingCatalog.Sniper(id)?1.2f:gun.Interval))+" 发/分\n精度 "+Mathf.RoundToInt(100/(1+gun.HipSpread*.1f))+" / 100\n机动 "+(BuildingCatalog.Mobility(id)*100).ToString("0")+"%",new Vector2(.5f,.5f),new Vector2(56,0),new Vector2(150,104),14);specs.lineSpacing=.85f;
                }
                Next("选择建筑 →",System.Array.IndexOf(BuildingCatalog.Weapons,BuildingCatalog.SelectedWeapon)>=0);return;
            }
            if(step==2)
            {
                Label("02  选择建筑",new Vector2(0,235));
                for(int i=0;i<4;i++)
                {
                    int index=i;var map=BuildingCatalog.Maps[i];
                    var b=UIFactory.Button("",content,new Vector2(.5f,.5f),new Vector2(-420+i*280,60),new Vector2(268,304),()=>{BuildingCatalog.SelectedMap=index;Draw();});
                    b.name="Map card "+map.Id;b.image.color=i==BuildingCatalog.SelectedMap?Selected:Dark;
                    var image=UIFactory.Rect("Scene cover",b.transform,new Vector2(.5f,.5f),new Vector2(0,73),new Vector2(256,144)).gameObject.AddComponent<RawImage>();
                    image.texture=Resources.Load<Texture2D>(map.previewImage);image.raycastTarget=false;
                    if(!image.texture){image.color=new Color(.18f,.23f,.24f);Debug.LogWarning("Missing map cover: "+map.previewImage);}
                    UIFactory.Text("Map description",b.transform,(i==BuildingCatalog.SelectedMap?"✓  ":"")+map.Name+"\n"+string.Join(" / ",map.Floors)+"\n\n"+map.Feature,new Vector2(.5f,.5f),new Vector2(0,-71),new Vector2(240,126),19);

                }
                UIFactory.Text("Selected briefing",content,BuildingCatalog.Maps[BuildingCatalog.SelectedMap].Briefing,new Vector2(.5f,.5f),new Vector2(0,-171),new Vector2(1090,75),20);Next("确认建筑 →",true);return;
            }
            if(step==3)
            {
                Label("03  选择难度",new Vector2(0,225));
                for(int i=0;i<4;i++){int index=i;var b=UIFactory.Button(ClassicRules.Data.difficulties[i].label+"\n每层 "+new[]{5,8,11,14}[i]+" 名敌人",content,new Vector2(.5f,.5f),new Vector2(-414+i*276,139),new Vector2(259,88),()=>{ClassicRules.DifficultyIndex=index;Draw();});b.image.color=i==ClassicRules.DifficultyIndex?Selected:Dark;}
                Label("普通作战人员 · 没有职业能力 · 依靠装备与观察完成清剿",new Vector2(0,-30));Next("查看任务简报 →",true);return;
            }
            var selected=BuildingCatalog.Maps[BuildingCatalog.SelectedMap];
            Label("04  任务简报 / "+selected.Name,new Vector2(0,208));
            UIFactory.Text("Briefing",content,selected.Briefing+"\n\n出战武器："+BuildingCatalog.SelectedWeapon+"  ·  "+ClassicRules.Difficulty.label+"难度\n敌人总数："+(selected.Floors.Length*new[]{5,8,11,14}[ClassicRules.DifficultyIndex])+" · 无增援\n\n全部敌人死亡 → 撤离区开启 → 连续坚守 3 秒\n血量归零即任务失败\n\nWASD 移动 / 左键射击 / 右键开镜 / R 换弹 / E 开门\n走入标记楼梯换层；使用耳机听取脚步；声音不会显示在画面中。",new Vector2(.5f,.5f),new Vector2(0,0),new Vector2(990,345),23);
            UIFactory.Button("进入建筑 →",content,new Vector2(.5f,.5f),new Vector2(380,-280),new Vector2(350,54),()=>{BuildingCatalog.Enabled=true;game.StartMatch();});
        }
        void Label(string value,Vector2 p){UIFactory.Text("Section",content,value,new Vector2(.5f,.5f),p,new Vector2(1090,58),24);}
        void Next(string label,bool allowed){var b=UIFactory.Button(label,content,new Vector2(.5f,.5f),new Vector2(380,-285),new Vector2(350,48),()=>{step++;Draw();});b.interactable=allowed;}
    }
}
