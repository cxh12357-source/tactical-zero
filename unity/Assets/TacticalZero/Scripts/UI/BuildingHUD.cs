using UnityEngine;
using UnityEngine.UI;
namespace TacticalZero
{
    public sealed class BuildingHUD:MonoBehaviour
    {
        Text notice,guide;Image fade;RawImage map;Texture2D mapTexture;RectTransform root;
        float next;Color32[] pixels=new Color32[160*112];
        Bounds[][] floorPlan;
        public void Build(Transform parent)
        {
            root=UIFactory.Fill("Building HUD",parent);
            notice=UIFactory.Text("Clear announcement",root,"",new Vector2(.5f,1),new Vector2(0,-105),new Vector2(820,55),25,TextAnchor.MiddleCenter);notice.color=new Color(.95f,.83f,.45f);
            guide=UIFactory.Text("Extraction guidance",root,"",new Vector2(.5f,1),new Vector2(0,-152),new Vector2(600,35),18,TextAnchor.MiddleCenter);
            var r=UIFactory.Rect("Current floor minimap",root,new Vector2(0,1),new Vector2(103,-112),new Vector2(160,112));map=r.gameObject.AddComponent<RawImage>();map.raycastTarget=false;
            mapTexture=new Texture2D(160,112,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};map.texture=mapTexture;
            fade=UIFactory.Fill("Stair fade",root).gameObject.AddComponent<Image>();fade.color=Color.clear;fade.raycastTarget=false;
        }
        void Update()
        {
            var m=BuildingMission.Instance;var g=GameManager.Instance;if(!m||!g||!g.Player)return;
            root.gameObject.SetActive(g.IsPlaying);if(!g.IsPlaying)return;
            fade.color=new Color(0,0,0,m.Fade);notice.text=m.Notice;
            if(Time.time<next||m.CurrentFloor<0)return;next=Time.time+.25f;
            guide.text="";
            if(m.Remaining==0)
            {
                Vector3 target=m.Definition.Exit;
                bool upstairs=m.CurrentFloor<m.Definition.ExitFloor;
                if(m.CurrentFloor!=m.Definition.ExitFloor)
                {var left=BuildingMission.Stair(m.CurrentFloor,-1,upstairs);var right=BuildingMission.Stair(m.CurrentFloor,1,upstairs);target=(left-g.Player.transform.position).sqrMagnitude<(right-g.Player.transform.position).sqrMagnitude?left:right;}
                Vector3 local=g.Player.transform.InverseTransformDirection(target-g.Player.transform.position);
                string direction=Mathf.Abs(local.x)>Mathf.Abs(local.z)?local.x<0?"← 左侧":"右侧 →":local.z>0?"↑ 前方":"↓ 后方";
                guide.text=(m.CurrentFloor==m.Definition.ExitFloor?"撤离点":upstairs?"上楼前往撤离点":"下楼返回撤离点")+" · "+direction+" · "+Vector3.Distance(target,g.Player.transform.position).ToString("0")+"m";
            }
            for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(15,24,27,210);
            if(floorPlan==null)
            {
                floorPlan=new Bounds[m.FloorRoots.Length][];
                for(int f=0;f<floorPlan.Length;f++)
                {
                    var boxes=m.FloorRoots[f].GetComponentsInChildren<BoxCollider>();var shapes=new System.Collections.Generic.List<Bounds>();
                    foreach(var box in boxes)if(!box.isTrigger&&box.gameObject.layer==LayerMask.NameToLayer("TZWorld")&&box.bounds.min.y<f*5+2.5f&&box.bounds.max.y>f*5+.3f)shapes.Add(box.bounds);
                    floorPlan[f]=shapes.ToArray();
                }
            }
            for(int i=m.FloorStart(m.CurrentFloor);i<m.FloorStart(m.CurrentFloor)+m.FloorCount(m.CurrentFloor);i++)Rectangle(m.RoomBounds[i],m.Explored.Contains(i)?new Color32(68,90,94,255):new Color32(29,39,43,255));
            foreach(var bounds in floorPlan[m.CurrentFloor])Rectangle(bounds,new Color32(109,122,123,255));
            for(int side=-1;side<=1;side+=2){if(m.CurrentFloor<m.Definition.Floors.Length-1)Dot(BuildingMission.Stair(m.CurrentFloor,side,true),new Color32(235,185,68,255),2);if(m.CurrentFloor>0)Dot(BuildingMission.Stair(m.CurrentFloor,side,false),new Color32(235,185,68,255),2);}
            if(m.Remaining==0&&m.CurrentFloor==m.Definition.ExitFloor)Dot(m.Definition.Exit,new Color32(50,250,115,255),4);
            Dot(g.Player.transform.position,new Color32(245,245,225,255),2);Dot(g.Player.transform.position+g.Player.transform.forward*1.5f,new Color32(245,245,225,255),1);
            mapTexture.SetPixels32(pixels);mapTexture.Apply(false);
        }
        void Dot(Vector3 world,Color32 color,int r)
        {
            int cx=Mathf.RoundToInt((world.x+24)/48*159),cy=Mathf.RoundToInt((world.z+17)/34*111);
            for(int y=Mathf.Max(0,cy-r);y<=Mathf.Min(111,cy+r);y++)for(int x=Mathf.Max(0,cx-r);x<=Mathf.Min(159,cx+r);x++)pixels[y*160+x]=color;
        }
        void Rectangle(Bounds bounds,Color32 color)
        {
            int x0=Mathf.Clamp(Mathf.FloorToInt((bounds.min.x+24)/48*159),0,159),x1=Mathf.Clamp(Mathf.CeilToInt((bounds.max.x+24)/48*159),0,159);
            int y0=Mathf.Clamp(Mathf.FloorToInt((bounds.min.z+17)/34*111),0,111),y1=Mathf.Clamp(Mathf.CeilToInt((bounds.max.z+17)/34*111),0,111);
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)pixels[y*160+x]=color;
        }
        void OnDestroy(){if(mapTexture)Destroy(mapTexture);}
    }
}
