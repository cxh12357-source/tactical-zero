using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace TacticalZero
{
    public sealed class ClassicCombatUI : MonoBehaviour
    {
        GameManager game;RectTransform hud,bag,marker,extractMarker;Text notice,inventory,areaLabel;Texture2D mapTexture;float next;bool bagOpen;
        ClassicEncounter mappedMission;readonly Color32[] terrainPixels=new Color32[168*120],mapPixels=new Color32[168*120];
        public void Build(Transform parent)
        {
            game=GameManager.Ensure();hud=UIFactory.Fill("Classic HUD",parent);
            areaLabel=UIFactory.Text("Current map area",hud,"",new Vector2(.5f,1),new Vector2(0,-76),new Vector2(480,24),12,TextAnchor.MiddleCenter);areaLabel.color=new Color(.62f,.78f,.75f);
            notice=UIFactory.Text("Mission notice",hud,"",new Vector2(.5f,0),new Vector2(0,166),new Vector2(540,24),13,TextAnchor.MiddleCenter);
            inventory=UIFactory.Text("Bag summary",hud,"",new Vector2(1,0),new Vector2(-120,20),new Vector2(204,24),12,TextAnchor.MiddleRight);
            var frame=UIFactory.Rect("Minimap frame",hud,new Vector2(1,0),new Vector2(-120,126),new Vector2(212,184));UIFactory.Panel(frame,new Color(.025f,.035f,.04f,.76f));
            UIFactory.Text("Map label",frame,ClassicRules.Map.name,new Vector2(.5f,1),new Vector2(0,-14),new Vector2(196,24),13,TextAnchor.MiddleLeft);
            var map=UIFactory.Rect("Minimap",hud,new Vector2(1,0),new Vector2(-120,116),new Vector2(196,140));var image=map.gameObject.AddComponent<RawImage>();image.raycastTarget=false;
            mapTexture=new Texture2D(168,120,TextureFormat.RGBA32,false);mapTexture.filterMode=FilterMode.Point;image.texture=mapTexture;
            marker=UIFactory.Rect("Player map marker",map,new Vector2(0,0),Vector2.zero,new Vector2(7,7));UIFactory.Panel(marker,Color.white);
            extractMarker=UIFactory.Rect("Extraction map marker",map,new Vector2(0,0),Vector2.zero,new Vector2(9,9));UIFactory.Panel(extractMarker,new Color(.3f,1,.7f));
            bag=UIFactory.Rect("Inventory overlay",parent,new Vector2(.5f,.5f),Vector2.zero,new Vector2(740,580));UIFactory.Panel(bag,new Color(.025f,.055f,.08f,.99f),true);bag.gameObject.SetActive(false);
        }
        void Update()
        {
            var mission=ClassicEncounter.Instance;if(!mission||!game.Player)return;
            if(Keyboard.current!=null&&Keyboard.current.bKey.wasPressedThisFrame){if(bagOpen)CloseBag();else if(game.IsPlaying)OpenBag();}
            if(bagOpen&&game.Phase!=GamePhase.Paused){bagOpen=false;bag.gameObject.SetActive(false);}
            hud.gameObject.SetActive(game.IsPlaying);
            if(!game.IsPlaying||Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;
            var visibility=game.Player.GetComponent<PlayerVisibilityState>();
            areaLabel.text=MapArea.NameAt(game.Player.transform.position)+(visibility.HiddenInGrass?(visibility.Revealed?" · 行踪暴露":visibility.ConcealedByGrass?" · 草丛掩护":" · 低草：蹲伏隐蔽"):"");
            notice.text=mission.SearchProgress>0?"搜索进度 "+(mission.SearchProgress*100).ToString("0")+"% · 移动或受击将中断":mission.Notice.StartsWith(mission.Map.name+" / ")?"":mission.Notice;
            inventory.text="B  背包 "+mission.Bag.Count+" / 8  ·  "+mission.BagValue+" ◈";
            // Terrain and brush volumes are static for a match; only supply markers change.
            if(mappedMission!=mission)
            {
                mappedMission=mission;
                for(int y=0;y<120;y++)for(int x=0;x<168;x++)
                {
                    var at=new Vector3((x+.5f)*.5f-42,0,(y+.5f)*.5f-30);
                    bool navigable=NavMesh.SamplePosition(at,out var sample,.3f,NavMesh.AllAreas);
                    bool solid=Physics.Raycast(at+Vector3.up*1.7f,Vector3.down,3,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore);
                    terrainPixels[x+y*168]=navigable?(mission.InBrush(at)?new Color(.12f,.3f,.2f):new Color(.13f,.20f,.23f)):solid?new Color(.38f,.42f,.42f):new Color(.025f,.065f,.08f);
                }
            }
            System.Array.Copy(terrainPixels,mapPixels,terrainPixels.Length);
            foreach(var loot in mission.Supplies)
            {
                if(!loot||!loot.gameObject.activeSelf||(loot.Kind!="weapon"&&loot.Kind!="equipment"))continue;
                var p=loot.GroundPosition;int x=Mathf.Clamp(Mathf.FloorToInt((p.x+42)*2),1,166),y=Mathf.Clamp(Mathf.FloorToInt((p.z+30)*2),1,118);
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)mapPixels[x+dx+(y+dy)*168]=loot.Kind=="weapon"?new Color(1,.74f,.22f):new Color(.75f,.45f,1);
            }
            mapTexture.SetPixels32(mapPixels);mapTexture.Apply(false);
            marker.anchoredPosition=new Vector2((game.Player.transform.position.x/3+14)*7,(game.Player.transform.position.z/3+10)*7);
            extractMarker.anchoredPosition=new Vector2((mission.Extraction.x/3+14)*7,(mission.Extraction.z/3+10)*7);
        }
        void OpenBag()
        {
            var mission=ClassicEncounter.Instance;if(!mission||!game.IsPlaying)return;game.Pause(false);bagOpen=true;RebuildBag();bag.SetAsLastSibling();bag.gameObject.SetActive(true);
        }
        void RebuildBag()
        {
            foreach(Transform child in bag)Destroy(child.gameObject);
            var mission=ClassicEncounter.Instance;
            UIFactory.Text("Title",bag,"行动背包   "+mission.Bag.Count+" / 8",new Vector2(.5f,1),new Vector2(0,-55),new Vector2(650,55),30,TextAnchor.MiddleCenter);
            UIFactory.Text("Value",bag,"战利品 "+mission.BagValue+" ◈  ·  成功撤离后存入仓库",new Vector2(.5f,1),new Vector2(0,-111),new Vector2(650,42),20,TextAnchor.MiddleCenter);
            for(int i=0;i<8;i++)
            {
                int index=i;string label=i<mission.Bag.Count?ClassicEncounter.ItemName(mission.Bag[i])+"  /  "+ClassicEncounter.Value(mission.Bag[i])+" ◈\n点击丢弃":"空格位";
                var button=UIFactory.Button(label,bag,new Vector2(.5f,.5f),new Vector2(-160+(i%2)*320,95-(i/2)*73),new Vector2(295,63),()=>{if(index<mission.Bag.Count){mission.Bag.RemoveAt(index);game.Stats.supplies=mission.BagValue;RebuildBag();}});button.interactable=i<mission.Bag.Count;
            }
            UIFactory.Button("继续行动",bag,new Vector2(.5f,0),new Vector2(0,48),new Vector2(610,55),CloseBag);
        }
        void CloseBag(){bagOpen=false;bag.gameObject.SetActive(false);game.Resume();}
        void OnDestroy(){if(mapTexture)Destroy(mapTexture);}
    }
}
