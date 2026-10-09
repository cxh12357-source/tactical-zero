using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
namespace TacticalZero
{
    public static class BuildingArchitecture18
    {
        public static Vector3 Stair(int map,int floor,int side,bool up)
        {
            float direction=map==1&&side>0||map==2&&side<0?-1:1;
            return new Vector3(side*22,floor*5,(up?-7.6f:7.6f)*direction);
        }
        public static void Generate(BuildingMission m)
        {
            var layout=BuildingLayout18.Create(m.MapIndex);var d=m.Definition;m.FloorRoots=new Transform[d.Floors.Length];m.SpawnZones=new Vector3[layout.Rooms.Count];m.RoomBounds=new Bounds[layout.Rooms.Count];
            for(int f=0;f<d.Floors.Length;f++)
            {
                var root=new GameObject(d.Floors[f]+" 建筑楼层").transform;root.SetParent(m.transform);root.position=Vector3.up*f*5;m.FloorRoots[f]=root;
                var holes=new List<Rect>();if(f>0){holes.Add(new Rect(-24,-6.9f,4,13.8f));holes.Add(new Rect(20,-6.9f,4,13.8f));}
                if(m.MapIndex==0&&f>0)holes.Add(new Rect(-5,-5,10,10));
                if(m.MapIndex==3&&f==1)holes.Add(new Rect(-13,-6,24,12));
                Slab(m,root,f,"承重楼板",-.18f,.36f,holes,m.FloorMaterial,SurfaceUse18.Floor);
                var ceilingHoles=new List<Rect>{new Rect(-24,-8,4,16),new Rect(20,-8,4,16)};
                if(m.MapIndex==0)ceilingHoles.Add(new Rect(-5,-5,10,10));if(m.MapIndex==3&&f==0)ceilingHoles.Add(new Rect(-13,-6,24,12));
                if(!(m.MapIndex==0&&f==3))Slab(m,root,f,"吊顶",3.7f,.16f,ceilingHoles,m.WallMaterial,SurfaceUse18.Ceiling);
                foreach(int side in new[]{-1,1})
                {
                    Part(m,"外围承重墙",root,new Vector3(side*24,1.85f,0),new Vector3(.3f,3.7f,34),m.WallMaterial);
                    Part(m,"窗下墙",root,new Vector3(0,.5f,side*17),new Vector3(48,1,.3f),m.WallMaterial);
                    Part(m,"窗梁",root,new Vector3(0,3.35f,side*17),new Vector3(48,.7f,.3f),m.WallMaterial);
                    Part(m,"窗玻璃",root,new Vector3(0,2,side*17),new Vector3(48,2,.06f),m.GlassMaterial);
                    for(int n=0;n<12;n++)Part(m,"窗框",root,new Vector3(-22+n*4,2,side*17),new Vector3(.08f,2,.14f),m.MetalMaterial);
                    if(f<d.Floors.Length-1)Ramp(m,root,f,side);
                    if(f<d.Floors.Length-1)Trigger(m,root,f,side,true);
                    if(f>0)Trigger(m,root,f,side,false);
                }
                if(m.MapIndex==0&&f>0)RailRect(m,root,new Rect(-5,-5,10,10));
                if(m.MapIndex==3&&f==1)RailRect(m,root,new Rect(-13,-6,24,12));
                if(m.MapIndex==0&&f==0)
                {
                    Part(m,"内庭花池",root,new Vector3(-1,.25f,0),new Vector3(3,.5f,2),m.MetalMaterial);
                    Part(m,"内庭长椅",root,new Vector3(3,.45f,2),new Vector3(.6f,.25f,2.5f),m.WoodMaterial);
                }
                for(int i=0;i<layout.Rooms.Count;i++)
                {
                    var room=layout.Rooms[i];if(room.Floor!=f)continue;var r=new GameObject(room.Name).transform;r.SetParent(root,false);
                    Vector3 center=new Vector3(room.Area.center.x,0,room.Area.center.y);m.SpawnZones[i]=center+root.position;m.RoomBounds[i]=new Bounds(m.SpawnZones[i]+Vector3.up*1.5f,new Vector3(room.Area.width,3,room.Area.height));
                    var floor=Part(m,room.Name+" 地面",r,center+Vector3.up*.012f,new Vector3(room.Area.width,.024f,room.Area.height),m.MapIndex==0?m.WoodMaterial:m.FloorMaterial);
                    Tag(floor,f,SurfaceUse18.Floor,m.MapIndex==0?"wood":m.MapIndex==2?"tile":m.MapIndex==1?"carpet":"concrete");
                    if(!room.Open)for(int side=0;side<4;side++)Wall(m,r,room,side,layout.Portals);
                    if(room.Area.height<4)
                    {
                        var station=new Vector3(room.Area.xMin+1,0,room.Area.center.y);
                        Part(m,room.Name+" 工作台",r,station+Vector3.up*.7f,new Vector3(.75f,1.4f,1.25f),m.MetalMaterial);
                        Part(m,"控制面板",r,station+new Vector3(.39f,1.15f,0),new Vector3(.035f,.3f,.65f),m.AccentMaterial,false);
                    }
                    else m.Furniture(r,room.Name,center+new Vector3(-Mathf.Min(1.5f,room.Area.width*.2f),0,Mathf.Min(1.8f,room.Area.height*.5f-3)),m.MapIndex,i);
                    if(room.Open&&room.Area.width>14)
                    {
                        for(int n=0;n<3;n++)m.Furniture(r,m.MapIndex==3?"生产机器":"办公桌",center+new Vector3(-5+n*5,0,-3+(n%2)*4),m.MapIndex,n);
                        if(m.MapIndex==1)Part(m,"接待前台",r,center+new Vector3(4,.6f,3),new Vector3(4,1.2f,.9f),m.WoodMaterial);
                    }
                    Sign(r,room.Name,center+new Vector3(0,2.7f,-room.Area.height*.5f+.18f),180);
                    Part(m,"灯具",r,center+Vector3.up*3.5f,new Vector3(1.8f,.06f,.25f),m.AccentMaterial,false);
                }
                var usedDoors=new HashSet<string>();
                foreach(var p in layout.Portals)if(p.Floor==f&&usedDoors.Add(p.Point+"/"+p.AlongX))
                {
                    var door=Part(m,"门户 "+p.Id,root,new Vector3(p.Point.x,1.25f,p.Point.y),p.AlongX?new Vector3(2.15f,2.5f,.14f):new Vector3(.14f,2.5f,2.15f),m.MapIndex==1?m.GlassMaterial:m.WoodMaterial);
                    door.layer=LayerMask.NameToLayer("TZInteractable");door.AddComponent<NavMeshModifier>().ignoreFromBuild=true;var action=door.AddComponent<BuildingDoor>();action.Floor=f;action.SlideDirection=p.AlongX?Vector3.right:Vector3.forward;
                }
                for(int i=0;i<2;i++){var med=Part(m,"急救包",root,new Vector3(i==0?-19:19,.3f,i==0?15:-15),new Vector3(.6f,.5f,.4f),m.AccentMaterial);med.layer=LayerMask.NameToLayer("TZInteractable");med.AddComponent<BuildingMedkit>();}
                Sign(root,d.Name+" / "+d.Floors[f],new Vector3(0,2.7f,-16.7f),180);
            }
        }
        static GameObject Part(BuildingMission m,string n,Transform p,Vector3 at,Vector3 size,Material material,bool solid=true)=>BuildingMission.Part(n,p,at,size,material,solid);
        public static void Tag(GameObject go,int floor,SurfaceUse18 use,string material)
        {var s=go.AddComponent<BuildingSurface18>();s.FloorId=floor;s.SurfaceId="F"+floor+"/"+go.name+"/"+go.transform.localPosition;s.Use=use;s.FootMaterial=material;}
        static void Slab(BuildingMission m,Transform root,int f,string name,float y,float depth,List<Rect> holes,Material mat,SurfaceUse18 use)
        {
            var xs=new SortedSet<float>{-24,24};var zs=new SortedSet<float>{-17,17};foreach(var h in holes){xs.Add(h.xMin);xs.Add(h.xMax);zs.Add(h.yMin);zs.Add(h.yMax);}var x=new List<float>(xs);var z=new List<float>(zs);
            for(int i=0;i<x.Count-1;i++)for(int j=0;j<z.Count-1;j++)
            {
                Vector2 mid=new Vector2((x[i]+x[i+1])*.5f,(z[j]+z[j+1])*.5f);bool hole=false;foreach(var h in holes)if(h.Contains(mid))hole=true;if(hole)continue;
                var part=Part(m,name,root,new Vector3(mid.x,y,mid.y),new Vector3(x[i+1]-x[i],depth,z[j+1]-z[j]),mat);Tag(part,f,use,m.MapIndex==2?"tile":"concrete");
                if(use==SurfaceUse18.Ceiling)part.AddComponent<NavMeshModifier>().ignoreFromBuild=true;
            }
        }
        static void Wall(BuildingMission m,Transform root,BuildingRoom18 room,int side,List<BuildingPortal18> portals)
        {
            bool x=side%2==0;float fixedAt=side==0?room.Area.yMax:side==2?room.Area.yMin:side==1?room.Area.xMax:room.Area.xMin;
            float lo=x?room.Area.xMin:room.Area.yMin,hi=x?room.Area.xMax:room.Area.yMax;var gaps=new List<Vector2>();
            foreach(var p in portals)if(p.Floor==room.Floor&&p.AlongX==x&&Mathf.Abs((x?p.Point.y:p.Point.x)-fixedAt)<.1f)
            {float c=x?p.Point.x:p.Point.y;if(c>lo+.5f&&c<hi-.5f)gaps.Add(new Vector2(Mathf.Max(lo,c-1.15f),Mathf.Min(hi,c+1.15f)));}
            gaps.Sort((a,b)=>a.x.CompareTo(b.x));float cursor=lo;
            foreach(var gap in gaps){Segment(cursor,gap.x,1.8f,3.6f);Segment(gap.x,gap.y,3.05f,1.1f);cursor=Mathf.Max(cursor,gap.y);}Segment(cursor,hi,1.8f,3.6f);
            void Segment(float a,float b,float y,float height){if(b-a<.04f)return;Part(m,"房间墙",root,x?new Vector3((a+b)*.5f,y,fixedAt):new Vector3(fixedAt,y,(a+b)*.5f),x?new Vector3(b-a,height,.2f):new Vector3(.2f,height,b-a),m.WallMaterial);}
        }
        static void Ramp(BuildingMission m,Transform root,int f,int side)
        {
            float direction=Stair(m.MapIndex,0,side,true).z<0?1:-1;float angle=Mathf.Atan2(5,14)*Mathf.Rad2Deg;
            var ramp=Part(m,"实体楼梯坡面",root,new Vector3(side*22,2.5f,0),new Vector3(3.2f,.18f,Mathf.Sqrt(221)),m.MetalMaterial);ramp.transform.localRotation=Quaternion.Euler(-direction*angle,0,0);Tag(ramp,f,SurfaceUse18.Stair,m.MapIndex==3?"metal":"concrete");
            for(int step=0;step<28;step++)
            {float t=(step+.5f)/28;Part(m,"楼梯踏面",root,new Vector3(side*22,t*5+.10f,direction*Mathf.Lerp(-7,7,t)),new Vector3(3.15f,.06f,.5f),m.MetalMaterial,false);}
            for(int s=-1;s<=1;s+=2)
            {var rail=Part(m,"楼梯扶手",root,new Vector3(side*22+s*1.7f,3.45f,0),new Vector3(.08f,.08f,Mathf.Sqrt(221)),m.MetalMaterial,false);rail.transform.localRotation=Quaternion.Euler(-direction*angle,0,0);}
        }
        static void Trigger(BuildingMission m,Transform root,int f,int side,bool up)
        {
            var p=Stair(m.MapIndex,0,side,up);var go=new GameObject(up?"上楼触发":"下楼触发");go.transform.SetParent(root,false);go.transform.localPosition=p;
            var c=go.AddComponent<BoxCollider>();c.isTrigger=true;c.center=Vector3.up;c.size=new Vector3(2.4f,2,.55f);var s=go.AddComponent<BuildingStair>();s.Floor=f;s.Side=side;s.Up=up;
            Sign(root,(side<0?"主楼梯 ":"安全楼梯 ")+(up?"↑":"↓"),p+new Vector3(0,2.6f,0),p.z<0?0:180);
        }
        static void RailRect(BuildingMission m,Transform root,Rect area)
        {
            foreach(int s in new[]{-1,1})
            {
                Part(m,"中庭护栏",root,new Vector3(area.center.x,1.05f,s<0?area.yMin:area.yMax),new Vector3(area.width, .08f,.1f),m.MetalMaterial);
                Part(m,"中庭护栏",root,new Vector3(s<0?area.xMin:area.xMax,1.05f,area.center.y),new Vector3(.1f,.08f,area.height),m.MetalMaterial);
                for(float x=area.xMin;x<=area.xMax;x+=1.4f)Part(m,"栏杆竖杆",root,new Vector3(x,.55f,s<0?area.yMin:area.yMax),new Vector3(.055f,1.1f,.055f),m.MetalMaterial);
            }
        }
        static void Sign(Transform root,string name,Vector3 at,float yaw)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=at;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var text=go.AddComponent<TextMesh>();text.font=UIFactory.Font;text.text=name;text.fontSize=36;text.characterSize=.037f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.7f,.86f,.78f);go.AddComponent<BuildingSign>();go.GetComponent<MeshRenderer>().sharedMaterial=BuildingSign.WorldMaterial;
        }
    }
}
