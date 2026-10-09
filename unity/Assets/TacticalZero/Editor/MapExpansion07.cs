using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine.AI;
using Object=UnityEngine.Object;

namespace TacticalZero.Editor
{
    public static class MapExpansion07
    {
        const string Root=ClassicArt.Root;static Material wall,trim,floor,foliage;static Transform geometry,detail;static string mapId;
        public static void Apply()
        {
            ClassicArt.Init();
            foreach(var map in ClassicRules.Data.maps)
            {
                var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene("Assets/TacticalZero/Scenes/"+map.Scene+".unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
                try
                {
                    geometry=null;bool exists=false;
                    foreach(var root in scene.GetRootGameObjects()){if(root.name=="Static architecture")geometry=root.transform;if(root.GetComponentInChildren<MapExpansionStamp>())exists=true;}
                    if(geometry && geometry.Find("Freeform layout 12"))continue;
                    if(exists){Finish(scene,map);EditorSceneManager.SaveScene(scene);ClassicProjectSetup.RefreshMapPreview(map);continue;}
                    if(!geometry)throw new InvalidOperationException("Missing map architecture: "+map.id);
                    mapId=map.id;wall=Resources.Load<Material>("Classic/Materials/"+map.id+"_Walls");trim=Resources.Load<Material>("Classic/Materials/"+map.id+"_Trim");floor=Resources.Load<Material>("Classic/Materials/"+map.id+"_Ground");foliage=Resources.Load<Material>("Classic/Materials/"+map.id+"_Foliage");
                    detail=new GameObject("Expanded rooms and terrain 07").transform;detail.SetParent(geometry,false);detail.gameObject.AddComponent<MapExpansionStamp>();
                    foreach(var root in scene.GetRootGameObjects())foreach(var grass in root.GetComponentsInChildren<GrassVolume>())
                    {
                        var colliders=grass.GetComponents<BoxCollider>();BoxCollider keep=colliders[0];foreach(var c in colliders)if(c.size.sqrMagnitude>keep.size.sqrMagnitude)keep=c;
                        foreach(var collider in colliders)if(collider!=keep)Object.DestroyImmediate(collider);keep.isTrigger=true;
                    }
                    if(map.id=="sector07")
                    {
                        Room("SECURITY OFFICE",new Vector3(-31,0,18),12,13,true);
                        Room("POWER CONTROL",new Vector3(8,0,19),12,11,true);
                        Room("MAINTENANCE",new Vector3(28,0,-5),12,10,true);
                        Platform("WAREHOUSE CATWALK",new Vector3(-8,0,-4),8,8,2.4f,7);
                        Brush(new Vector3(-32,0,-12),new Vector2(7,8),true);
                        Brush(new Vector3(31,0,10),new Vector2(6,6),false);
                        Cover(new Vector3(6,0,3),6,ClassicArt.Steel);
                    }
                    else if(map.id=="docks")
                    {
                        Room("CONTAINER A / TRANSIT",new Vector3(-24,0,7.5f),5.8f,14.8f,false);
                        Room("CONTAINER B / SUPPLY",new Vector3(-6,0,4.5f),5.8f,14.8f,false);
                        Room("HARBOR CONTROL",new Vector3(27,0,17),11,10,true);
                        Platform("LOADING DECK",new Vector3(3,0,-14),9,8,2.2f,6);
                        Wetland("TIDAL SHALLOWS",new Vector3(-33,0,-13),new Vector2(8,13),new Color(.16f,.38f,.4f),.72f);
                        Brush(new Vector3(-32,0,-11),new Vector2(7,10),true);
                        Brush(new Vector3(29,0,-14),new Vector2(7,5),false);
                    }
                    else if(map.id=="ruins")
                    {
                        Room("ABANDONED CLINIC",new Vector3(-21,0,14),13,11,true);
                        Room("RESIDENTIAL BLOCK",new Vector3(21,0,13),13,11,true);
                        Room("OLD WORKSHOP",new Vector3(-21,0,-13),13,12,true);
                        Platform("COLLAPSED UPPER FLOOR",new Vector3(15,0,-12),9,7,2.1f,6);
                        Brush(new Vector3(0,0,5),new Vector2(8,10),true);
                        Brush(new Vector3(-33,0,0),new Vector2(7,8),false);
                        Cover(new Vector3(25,0,-1),5,wall);
                    }
                    else
                    {
                        Room("FOREST BUNKER",new Vector3(-30,0,15),9,11,true);
                        Room("FIELD QUARTERS",new Vector3(15,0,0),9,9,true);
                        Room("RADAR STATION",new Vector3(30,0,-18),10,9,true);
                        Platform("RIDGE LOOKOUT",new Vector3(-1,0,5),10,8,2.6f,8);
                        Wetland("MUDDY FOREST BED",new Vector3(-22,0,-8),new Vector2(10,8),new Color(.22f,.23f,.15f),.82f);
                        Brush(new Vector3(-22,0,-8),new Vector2(10,8),true);
                        Brush(new Vector3(16,0,18),new Vector2(9,5),false);
                        Cover(new Vector3(9,0,-17),6,wall);
                    }
                    Physics.SyncTransforms();var surface=geometry.GetComponent<NavMeshSurface>();surface.overrideVoxelSize=true;surface.voxelSize=.12f;surface.BuildNavMesh();
                    if(!surface.navMeshData)throw new InvalidOperationException("Navigation bake failed: "+map.id);
                    string navPath=Root+"/"+map.id+"_Navigation07.asset";AssetDatabase.CreateAsset(surface.navMeshData,navPath);
                    Finish(scene,map);EditorSceneManager.SaveScene(scene);ClassicProjectSetup.RefreshMapPreview(map);
                }
                finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        internal static void UseLayout12(Transform target, ClassicMap map)
        {
            geometry=target.parent;detail=target;mapId=map.id;
            wall=Resources.Load<Material>("Classic/Materials/"+map.id+"_Walls");trim=Resources.Load<Material>("Classic/Materials/"+map.id+"_Trim");floor=Resources.Load<Material>("Classic/Materials/"+map.id+"_Ground");foliage=Resources.Load<Material>("Classic/Materials/"+map.id+"_Foliage");
        }
        static void Finish(Scene scene,ClassicMap map)
        {
            var stamp=geometry.GetComponentInChildren<MapExpansionStamp>();if(!stamp)return;
            foreach(var t in stamp.GetComponentsInChildren<Transform>())
            {
                if(t.name=="Room entrance"||t.name=="Room navigation center"||t.name=="Ramp entry"||t.name=="Lookout route"||t.name.EndsWith("search position"))
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                    var wrongArea=t.GetComponent<MapArea>();if(wrongArea)Object.DestroyImmediate(wrongArea);
                    if(!t.GetComponent<MapRoutePoint>())t.gameObject.AddComponent<MapRoutePoint>().Supply=t.name.EndsWith("search position")||t.name=="Lookout route";
                }
            }
            if(stamp.transform.Find("Terrain finish 07"))return;
            var finish=new GameObject("Terrain finish 07").transform;finish.SetParent(stamp.transform,false);
            foreach(var area in stamp.GetComponentsInChildren<MapArea>())if(area.Interior)
            {
                foreach(int side in new[]{-1,1})
                {
                    Visual("Interior strip light",area.transform,new Vector3(0,3.18f,side*area.Size.z*.25f),new Vector3(1.6f,.05f,.15f),ClassicArt.Gold);
                    var light=new GameObject("Interior local light").AddComponent<Light>();light.transform.SetParent(area.transform,false);light.transform.localPosition=new Vector3(0,2.85f,side*area.Size.z*.25f);light.type=LightType.Point;light.color=new Color(1,.85f,.65f);light.intensity=1.6f;light.range=7;light.shadows=LightShadows.None;
                }
            }
            if(map.id=="outpost")
            {
                var rock=Resources.Load<Material>("Classic/Materials/outpost_Walls");
                Solid("Natural ridge foundation",finish,new Vector3(-1,1.14f,5),new Vector3(9.8f,2.28f,7.8f),rock);
                foreach(int side in new[]{-1,1})for(int i=0;i<4;i++)Visual("Weathered ridge face",finish,new Vector3(-1+side*4.8f,1.1f,2+i*1.9f),new Vector3(1.8f,2.4f,2.3f),rock,3);
                Physics.SyncTransforms();var surface=geometry.GetComponent<NavMeshSurface>();surface.BuildNavMesh();AssetDatabase.CreateAsset(surface.navMeshData,AssetDatabase.GenerateUniqueAssetPath(Root+"/outpost_Navigation07finish.asset"));
            }
            if(map.id=="docks")
            {
                var wood=ClassicArt.Mat("Weathered dock planks",new Color(.3f,.29f,.23f));
                for(int i=0;i<18;i++)Visual("Loading deck plank",finish,new Vector3(3,2.215f,-17.8f+i*.44f),new Vector3(8.8f,.03f,.40f),wood);
            }
        }
        static GameObject Visual(string n,Transform parent,Vector3 at,Vector3 size,Material material,int shape=0)=>ClassicArt.Part(n,parent,at,size,material,shape);
        static GameObject Solid(string name,Transform parent,Vector3 at,Vector3 size,Material material)
        {
            var go=Visual(name,parent,at,size,material);go.layer=LayerMask.NameToLayer("TZWorld");go.AddComponent<BoxCollider>();return go;
        }
        static void ClearZone(Vector3 at,float width,float depth)
        {
            if(detail && detail.name=="Freeform layout 12")return;
            var bounds=new Bounds(at+Vector3.up*1.6f,new Vector3(width+.3f,3.2f,depth+.3f));
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var grass in root.GetComponentsInChildren<GrassVolume>())if(grass&&bounds.Intersects(grass.GetComponent<BoxCollider>().bounds))Object.DestroyImmediate(grass.gameObject);
            foreach(var child in new List<Transform>(Children(geometry)))
            {
                if(child==detail||child.name=="Foundation"||child.name=="Perimeter"||child.name=="Paving joint"||child.name.Contains("Truss")||child.name.Contains("truss"))continue;
                var renderer=child.GetComponent<Renderer>();if(renderer&&bounds.Intersects(renderer.bounds))Object.DestroyImmediate(child.gameObject);
            }
        }
        static IEnumerable<Transform> Children(Transform t){foreach(Transform child in t)yield return child;}
        static void Point(string name,Transform parent,Vector3 at,bool supply=false)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=at;go.AddComponent<MapRoutePoint>().Supply=supply;
        }
        internal static void Room(string name,Vector3 at,float width,float depth,bool partition)
        {
            ClearZone(at,width,depth);
            var root=new GameObject(name).transform;root.SetParent(detail,false);root.localPosition=at;
            var area=root.gameObject.AddComponent<MapArea>();area.Label=name;area.Size=new Vector3(width,3.6f,depth);area.Interior=true;
            float h=3.4f,t=.22f,door=2.3f;
            Visual("Interior floor",root,new Vector3(0,.018f,0),new Vector3(width,.036f,depth),floor);
            // Two end doors make every room a route; wall above each opening keeps sight lines readable.
            foreach(int side in new[]{-1,1})
            {
                float z=side*depth*.5f;
                foreach(int flank in new[]{-1,1})Solid("Doorway wall",root,new Vector3(flank*(width+door)*.25f,h*.5f,z),new Vector3((width-door)*.5f,h,t),wall);
                Solid("Door lintel",root,new Vector3(0,2.94f,z),new Vector3(door,.92f,t),trim);
                foreach(int flank in new[]{-1,1})Visual("Door jamb",root,new Vector3(flank*(door*.5f+.04f),1.2f,z),new Vector3(.09f,2.4f,.3f),trim);
                Visual("Door threshold stripe",root,new Vector3(0,.027f,z),new Vector3(door,.025f,.18f),ClassicArt.Gold);
                Point("Room entrance",root,new Vector3(0,0,z+side*1.2f));
            }
            // Side windows: low sill supports cover and firing; no invisible full-height collider.
            foreach(int side in new[]{-1,1})
            {
                float x=side*width*.5f;
                Solid("Window sill wall",root,new Vector3(x,.53f,0),new Vector3(t,1.06f,depth),wall);
                Solid("Window header wall",root,new Vector3(x,2.88f,0),new Vector3(t,1.04f,depth),wall);
                for(int i=-1;i<=1;i++)Solid("Window pier",root,new Vector3(x,1.73f,i*depth*.38f),new Vector3(t,1.35f,.8f),trim);
            }
            // Partial roof leaves a skylight, while keeping an enclosed ceiling over most of the room.
            foreach(int side in new[]{-1,1})Solid("Room roof",root,new Vector3(side*(width*.25f+.4f),3.48f,0),new Vector3(width*.5f-.8f,.16f,depth+.3f),trim);
            if(partition)
            {
                float gap=2.2f;
                foreach(int side in new[]{-1,1})Solid("Interior partition",root,new Vector3(side*(width+gap)*.25f,1.5f,0),new Vector3((width-gap)*.5f,3,.18f),wall);
                Solid("Interior door header",root,new Vector3(0,2.8f,0),new Vector3(gap,.4f,.18f),wall);
                foreach(int side in new[]{-1,1})
                {
                    Solid("Desk cover",root,new Vector3(-width*.32f,.45f,side*depth*.27f),new Vector3(1.8f,.9f,1),ClassicArt.Wood);
                    Visual("Desk console",root,new Vector3(-width*.32f,1.12f,side*depth*.27f),new Vector3(.6f,.42f,.07f),ClassicArt.Rubber);
                    Visual("Console display",root,new Vector3(-width*.32f,1.12f,side*depth*.27f+.042f),new Vector3(.48f,.3f,.015f),ClassicArt.Teal);
                    Solid("Wall cabinet",root,new Vector3(width*.35f,1,side*depth*.34f),new Vector3(1.2f,2,.55f),trim);
                    Point("Interior search position",root,new Vector3(1.3f,0,side*depth*.25f),true);
                }
            }
            else {Point("Container search position",root,new Vector3(1.2f,0,depth*.22f),true);for(int i=0;i<8;i++)Visual("Container structural rib",root,new Vector3(-width*.5f+.16f,1.6f,-depth*.4f+i*depth*.11f),new Vector3(.08f,3.1f,.1f),trim);}
            Visual("Door identification light",root,new Vector3(0,2.5f,-depth*.5f-.14f),new Vector3(1.2f,.045f,.04f),ClassicArt.Teal);
            Point("Room navigation center",root,new Vector3(0,0,-depth*.22f));
        }
        internal static void Platform(string name,Vector3 at,float width,float depth,float height,float run)
        {
            ClearZone(at-Vector3.forward*run*.5f,width+1,depth+run);
            var root=new GameObject(name).transform;root.SetParent(detail,false);root.localPosition=at;
            var area=root.gameObject.AddComponent<MapArea>();area.Label=name;area.Size=new Vector3(width,height+2,depth);
            Solid("Elevated traversable deck",root,new Vector3(0,height-.12f,0),new Vector3(width,.24f,depth),mapId=="outpost"?wall:trim);
            foreach(int side in new[]{-1,1})
            {
                Solid("Deck supporting pier",root,new Vector3(side*(width*.5f-.3f),height*.5f,depth*.33f),new Vector3(.45f,height,.45f),wall);
                foreach(float z in new[]{-depth*.5f,0,depth*.5f})Solid("Railing pillar",root,new Vector3(side*width*.5f,height+.55f,z),new Vector3(.09f,1.1f,.09f),trim);
                Solid("Railing top",root,new Vector3(side*width*.5f,height+1.1f,0),new Vector3(.08f,.08f,depth),trim);
            }
            // A continuous collision ramp under decorative stair treads supports players and NavMesh agents.
            float length=Mathf.Sqrt(run*run+height*height),angle=Mathf.Atan2(height,run)*Mathf.Rad2Deg;
            var ramp=Solid("Walkable ascent ramp",root,new Vector3(0,height*.5f-.08f,-depth*.5f-run*.5f),new Vector3(3,.16f,length),trim);ramp.transform.localRotation=Quaternion.Euler(-angle,0,0);
            for(int i=0;i<14;i++){float f=(i+.5f)/14;Visual("Ramp grip tread",root,new Vector3(0,height*f+.012f,-depth*.5f-run+run*f),new Vector3(3,.018f,.05f),ClassicArt.White);}
            Point("Ramp entry",root,new Vector3(0,0,-depth*.5f-run-.6f));Point("Lookout route",root,new Vector3(0,height,0),true);
            Solid("Lookout low cover",root,new Vector3(0,height+.45f,depth*.5f-.4f),new Vector3(width*.6f,.9f,.45f),wall);
        }
        internal static void Brush(Vector3 at,Vector2 size,bool tall)
        {
            var root=new GameObject(tall?"Tall concealment grass":"Low crouch brush").transform;root.SetParent(detail,false);root.localPosition=at;root.gameObject.layer=LayerMask.NameToLayer("TZGrass");
            var volume=root.gameObject.AddComponent<GrassVolume>();volume.ConcealsStanding=tall;var collider=root.GetComponent<BoxCollider>();collider.isTrigger=true;collider.center=Vector3.up*.8f;collider.size=new Vector3(size.x,1.6f,size.y);
            var blade=Resources.Load<Mesh>("Classic/Meshes/GrassBlade");var combines=new List<CombineInstance>();
            for(int z=0;z<size.y*2;z++)for(int x=0;x<size.x*2;x++)
            {
                float height=(tall?1.2f:.56f)+Mathf.Abs(Mathf.Sin(x*3.7f+z*5.1f))*.28f;
                var p=new Vector3((x+.5f)*.5f-size.x*.5f,height*.5f,(z+.5f)*.5f-size.y*.5f);
                combines.Add(new CombineInstance{mesh=blade,transform=Matrix4x4.TRS(p,Quaternion.Euler(0,x*73+z*47,0),new Vector3(.6f,height,.6f))});
            }
            var mesh=new Mesh{name=root.name};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(combines.ToArray());AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Root+"/Meshes/"+mapId+"_brush07.asset"));
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=root.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=foliage;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        internal static void Wetland(string name,Vector3 at,Vector2 size,Color tint,float slow)
        {
            var root=new GameObject(name).transform;root.SetParent(detail,false);root.localPosition=at;var area=root.gameObject.AddComponent<MapArea>();area.Label=name;area.Size=new Vector3(size.x,.5f,size.y);area.Movement=slow;
            Visual("Wet ground",root,Vector3.up*.025f,new Vector3(size.x,.035f,size.y),ClassicArt.Mat(mapId+" wet ground",tint,.25f,.8f));
            for(int i=0;i<8;i++)Visual("Shallow stepping stone",root,new Vector3(Mathf.Sin(i*5)*size.x*.3f,.08f,(i/7f-.5f)*size.y),new Vector3(1.1f,.13f,.8f),wall,3);
        }
        static void Cover(Vector3 at,int count,Material material)
        {
            for(int i=0;i<count;i++)
            {
                Vector3 p=at+new Vector3((i%3-1)*2.5f,0,(i/3)*3);
                Solid("Staggered low cover",detail,p+Vector3.up*.45f,new Vector3(1.7f,.9f,.7f),material);
            }
        }
    }
}
