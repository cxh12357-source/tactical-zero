using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using Object=UnityEngine.Object;

namespace TacticalZero.Editor
{
    // World-space authored layouts. The classic grid is retained only as a legacy data format.
    public static class MapLayout12
    {
        static Transform root;static string id;static Material ground,wall,trim,road,leaf;
        static readonly string Folder=ClassicArt.Root+"/Meshes/Layout12";
        [MenuItem("Tactical Zero/Rebuild Freeform Maps")]
        public static void Rebuild(){Apply(true);}
        public static void Apply(bool force=false)
        {
            ClassicArt.Init();Directory.CreateDirectory(Folder);Directory.CreateDirectory("ArtSource/Maps11");AssetDatabase.Refresh();
            foreach(var map in ClassicRules.Data.maps)
            {
                var path="Assets/TacticalZero/Scenes/"+map.Scene+".unity";
                var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
                try
                {
                    var old=Array.Find(scene.GetRootGameObjects(),x=>x.name=="Static architecture");
                    if(!force&&old&&old.transform.Find("Freeform layout 12"))continue;
                    string backup="ArtSource/Maps11/"+map.Scene+".unity";if(!File.Exists(backup))File.Copy(path,backup);
                    foreach(var obj in scene.GetRootGameObjects())
                        if(obj==old||obj.GetComponent<GrassVolume>()||obj.name=="Industrial finish 04"||obj.name=="Surface detail 04"||obj.name=="Art refinement 02")Object.DestroyImmediate(obj);
                    id=map.id;ground=Resources.Load<Material>("Classic/Materials/"+id+"_Ground");wall=Resources.Load<Material>("Classic/Materials/"+id+"_Walls");trim=Resources.Load<Material>("Classic/Materials/"+id+"_Trim");leaf=Resources.Load<Material>("Classic/Materials/"+id+"_Foliage");
                    road=ClassicArt.Mat(id+"_Route12",id=="outpost"?new Color(.32f,.26f,.16f):id=="ruins"?new Color(.29f,.25f,.22f):new Color(.105f,.14f,.15f),0,.2f);
                    var geo=new GameObject("Static architecture").transform;root=new GameObject("Freeform layout 12").transform;root.SetParent(geo,false);root.gameObject.AddComponent<MapExpansionStamp>();
                    new GameObject("Art refinement 02").transform.SetParent(geo,false);
                    MapExpansion07.UseLayout12(root,map);
                    var outline=Outline(id);Polygon("Irregular terrain foundation",outline,0,ground,true);
                    // Distant surroundings are below the playable polygon, never navigable.
                    var outside=ClassicArt.Mat(id+"_Backdrop12",id=="docks"?new Color(.075f,.27f,.32f):new Color(.12f,.17f,.16f),0,id=="docks"?.85f:.2f);
                    Visual(id=="docks"?"Harbor water":"Surrounding valley",new Vector3(0,-2.8f,0),new Vector3(260,.1f,220),outside);
                    Boundary(outline);
                    if(id=="sector07")Industrial();else if(id=="docks")Harbor();else if(id=="ruins")Ruins();else Valley();
                    foreach(var area in root.GetComponentsInChildren<MapArea>())if(area.Interior)
                    {
                        var light=new GameObject("Warm interior lamp").AddComponent<Light>();light.transform.SetParent(area.transform,false);light.transform.localPosition=new Vector3(0,2.9f,0);light.type=LightType.Point;light.intensity=1.8f;light.range=9;light.color=new Color(1,.79f,.53f);
                    }
                    Physics.SyncTransforms();EnvironmentDetail12.Apply(root,id);
                    var surface=geo.gameObject.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.layerMask=LayerMask.GetMask("TZWorld");surface.overrideVoxelSize=true;surface.voxelSize=.12f;
                    Physics.SyncTransforms();surface.BuildNavMesh();if(!surface.navMeshData)throw new InvalidOperationException("No navigation: "+id);
                    string nav=Folder+"/"+id+"_Navigation.asset";var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(nav);
                    if(existing){EditorUtility.CopySerialized(surface.navMeshData,existing);EditorUtility.SetDirty(existing);surface.RemoveData();surface.navMeshData=existing;surface.AddData();}else AssetDatabase.CreateAsset(surface.navMeshData,nav);
                    ValidateRoutes();EditorSceneManager.SaveScene(scene);ClassicProjectSetup.RefreshMapPreview(map);
                    Debug.Log("Layout12 complete: "+id+", boundary="+outline.Length+", routes="+root.GetComponentsInChildren<MapRoutePoint>().Length);
                }
                finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        static Vector2[] Outline(string map)
        {
            if(map=="docks")return Points(-40,-20,-29,-28,-8,-27,15,-24,38,-17,40,-5,29,1,37,16,29,26,18,24,13,8,4,5,-3,23,-14,26,-20,9,-26,5,-32,21,-40,17,-38,1);
            if(map=="sector07")return Points(-40,-15,-32,-27,-6,-28,11,-22,32,-26,41,-13,36,5,39,19,27,28,3,27,-13,21,-32,25,-40,13,-35,0);
            var result=new Vector2[40];
            for(int k=0;k<result.Length;k++){float a=k*Mathf.PI*2/result.Length;float r=map=="outpost"? .86f+.13f*Mathf.Sin(3*a+.4f)+.045f*Mathf.Cos(7*a):.93f+.045f*Mathf.Sin(5*a)+.025f*Mathf.Cos(9*a);result[k]=new Vector2(Mathf.Cos(a)*41*r,Mathf.Sin(a)*29*r);}
            return result;
        }
        static Vector2[] Points(params float[] values){var p=new Vector2[values.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Vector2(values[i*2],values[i*2+1]);return p;}
        static Vector3 At(float x,float z,float y=0)=>new Vector3(x,y,z);
        static GameObject Visual(string name,Vector3 p,Vector3 size,Material m,int shape=0)=>ClassicArt.Part(name,root,p,size,m,shape);
        static GameObject Solid(string name,Vector3 p,Vector3 size,Material m,int shape=0)
        {
            var go=Visual(name,p,size,m,shape);go.layer=LayerMask.NameToLayer("TZWorld");
            if(shape==0)go.AddComponent<BoxCollider>();else {var c=go.AddComponent<MeshCollider>();c.sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;c.convex=true;}
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);return go;
        }
        static GameObject Segment(string name,Vector3 a,Vector3 b,float width,float height,Material mat,bool solid=true)
        {
            var mid=(a+b)*.5f+Vector3.up*height*.5f;var size=new Vector3(width,height,Vector3.Distance(a,b)+.06f);var go=solid?Solid(name,mid,size,mat):Visual(name,mid,size,mat);go.transform.localRotation=Quaternion.LookRotation(b-a);return go;
        }
        static void Boundary(Vector2[] outline)
        {
            for(int i=0;i<outline.Length;i++)
            {
                var a=At(outline[i].x,outline[i].y);var b=At(outline[(i+1)%outline.Length].x,outline[(i+1)%outline.Length].y);
                float h=id=="outpost"?5.5f:id=="docks"?3.0f:3.8f;
                Segment("Perimeter",a,b,.7f,h,wall);
                if(id=="outpost"||id=="ruins")for(int k=0;k<3;k++)
                {var p=Vector3.Lerp(a,b,(k+.5f)/3);var rock=Solid("Boundary fractured cliff",p+Vector3.up*(h*.43f),new Vector3(2.6f,h*1.1f,3.5f),wall,3);rock.transform.localRotation=Quaternion.Euler(0,i*41+k*17,0);}
                else Segment("Perimeter rim",a+Vector3.up*h,b+Vector3.up*h,.8f,.14f,trim,false);
            }
        }
        static void Route(string name,float width,params Vector2[] knots)
        {
            // Catmull-Rom ribbons create continuous bends, independently of the old grid.
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();int steps=(knots.Length-1)*12;
            for(int k=0;k<=steps;k++)
            {
                float f=(float)k/12;int i=Mathf.Min(knots.Length-2,(int)f);float t=f-i;
                Vector2 a=knots[Mathf.Max(0,i-1)],b=knots[i],c=knots[i+1],d=knots[Mathf.Min(knots.Length-1,i+2)];
                Vector2 p=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
                Vector2 tangent=.5f*((-a+c)+2*(2*a-5*b+4*c-d)*t+3*(-a+3*b-3*c+d)*t*t);var side=new Vector2(-tangent.y,tangent.x).normalized*width*.5f;
                vertices.Add(At(p.x+side.x,p.y+side.y,.025f));vertices.Add(At(p.x-side.x,p.y-side.y,.025f));uv.Add(new Vector2(0,k*.25f));uv.Add(new Vector2(1,k*.25f));
                if(k>0){int n=k*2;indices.AddRange(new[]{n-2,n,n-1,n-1,n,n+1});}
            }
            MakeMesh(name,vertices,indices,uv,road,false);
        }
        static void Building(string name,float x,float z,float w,float d,float angle,bool partition=true)
        {
            MapExpansion07.Room(name,At(x,z),w,d,partition);var room=root.Find(name);room.localRotation=Quaternion.Euler(0,angle,0);
            // Entrance canopy and external buttresses clarify the building silhouette.
            var sign=ClassicArt.Part("Entrance canopy",room,new Vector3(0,2.7f,-d*.5f-.6f),new Vector3(3.2f,.12f,1.3f),trim);
            sign.gameObject.layer=LayerMask.NameToLayer("TZWorld");sign.AddComponent<BoxCollider>();
            ClassicArt.Label(name,room,new Vector3(0,2.95f,-d*.5f-.15f),.15f,new Color(.86f,.89f,.8f));
            for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)
            {var pier=ClassicArt.Part("Support pier",room,new Vector3(side*(w*.5f+.12f),1.7f,end*(d*.5f-.2f)),new Vector3(.35f,3.4f,.48f),trim);pier.layer=LayerMask.NameToLayer("TZWorld");pier.AddComponent<BoxCollider>();}
        }
        static void Patch(float x,float z,float w,float d,bool tall=true){MapExpansion07.Brush(At(x,z),new Vector2(w,d),tall);}
        static void Cover(float x,float z,float angle=0)
        {var obj=Solid("Low tactical barricade",At(x,z,.55f),new Vector3(3,1.1f,.65f),trim);obj.transform.localRotation=Quaternion.Euler(0,angle,0);}
        static void Industrial()
        {
            Route("Sweeping service ring",5,Points(-30,-14,-12,-19,12,-14,29,-4,24,14,5,19,-19,14,-30,-2,-30,-14));
            Route("Diagonal maintenance passage",3.5f,Points(-29,-13,-10,-2,8,4,25,13));
            Building("SECURITY OFFICE",-25,13,10,10,-24);Building("POWER CONTROL",17,15,11,10,28);Building("MAINTENANCE",26,-12,10,9,-32);Building("PUMP ROOM",-23,-15,8,7,25);
            MapExpansion07.Platform("REACTOR CATWALK",At(0,0),8,8,2.4f,7);
            for(int i=0;i<3;i++)
            {float x=-13+i*6;Solid("Reactor pressure vessel",At(x,8,2.4f),new Vector3(3.5f,2.4f,3.5f),trim,2);for(float y=1;y<5;y+=1.4f)Visual("Vessel reinforcing ring",At(x,8,y),new Vector3(3.6f,.09f,3.6f),ClassicArt.Steel,2);ClassicArt.Rod("Overhead transfer line",root,At(x,8,5),At(x,2,5),.13f,ClassicArt.Steel);}
            Cover(-12,-5,-35);Cover(12,-4,40);Cover(10,8,65);Patch(-30,0,5,7);Patch(28,4,4,5,false);Patch(10,-18,5,4);
            for(int k=0;k<7;k++){float a=k*.7f;var p=At(Mathf.Cos(a)*16,Mathf.Sin(a)*12);Visual("Amber guide bollard",p+Vector3.up*.7f,new Vector3(.18f,1.4f,.18f),ClassicArt.Gold);}
        }
        static void Harbor()
        {
            Route("Crescent quay route",5,Points(-33,13,-29,-3,-22,-18,1,-18,24,-13,30,-5));
            Route("Pier access curve",3,Points(-24,-9,-12,-1,-10,17));Route("East finger pier route",3,Points(4,-15,12,-2,21,16,28,21));
            Building("CUSTOMS HOUSE",-28,-13,9,9,-28);Building("HARBOR CONTROL",18,-12,10,9,26);Building("PIER WAREHOUSE",-11,12,6,12,15,false);Building("EAST TRANSIT",26,15,6,10,-25,false);
            MapExpansion07.Platform("QUAYSIDE LOOKOUT",At(1,-3),7,7,2.2f,6);
            for(int i=0;i<4;i++)
            {var go=Solid("Shipping container",At(-15+i*7,-22,1.45f),new Vector3(5.2f,2.9f,2.4f),i%2==0?trim:wall);go.transform.localRotation=Quaternion.Euler(0,12+i*3,0);}
            for(int i=0;i<3;i++){float x=-31+i*27;Solid("Crane foot",At(x,-3,3.8f),new Vector3(.65f,7.6f,.65f),trim);Segment("Crane diagonal boom",At(x,-3,7.6f),At(x+5,0,10.5f),.35f,.4f,trim);ClassicArt.Rod("Hanging crane cable",root,At(x+5,0,10.5f),At(x+5,0,3.5f),.035f,ClassicArt.Rubber);}
            MapExpansion07.Wetland("TIDAL SHALLOWS",At(-34,1),new Vector2(5,7),new Color(.15f,.35f,.35f),.72f);Patch(-34,1,4,6);Patch(30,-10,4,4,false);Cover(-17,-6,35);Cover(12,-5,-30);
        }
        static void Ruins()
        {
            Route("Broken circular boulevard",4.6f,Points(-26,0,-19,-16,0,-21,22,-13,28,4,15,19,-9,21,-25,8,-26,0));
            Route("Diagonal old tram avenue",4,Points(-28,-18,-10,-8,6,6,26,19));
            Building("ABANDONED CLINIC",-21,10,10,11,-28);Building("RESIDENTIAL BLOCK",18,11,12,10,32);Building("OLD WORKSHOP",-16,-15,10,10,28);Building("ARCHIVE ANNEX",24,-10,9,10,-18);
            MapExpansion07.Platform("COLLAPSED UPPER FLOOR",At(2,2),8,7,2.1f,6);
            for(int i=0;i<10;i++)
            {float a=i*Mathf.PI*2/10;float x=Mathf.Cos(a)*10,z=Mathf.Sin(a)*9; if(z<-3&&Mathf.Abs(x)<5)continue;var go=Solid("Broken courtyard masonry",At(x,z,.65f),new Vector3(2.2f,1.3f,.65f),wall);go.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}
            for(int i=0;i<14;i++){float x=-30+(i*17%59),z=-22+(i*13%43);if(Mathf.Abs(x)<7)continue;Solid("Scattered collapsed stone",At(x,z,.3f),new Vector3(1.3f,.6f,1),wall,3);}
            Patch(-29,-5,6,5);Patch(12,-18,5,5,false);Patch(0,19,7,4);Cover(-7,9,55);Cover(12,-5,-25);
        }
        static void Valley()
        {
            Route("Winding canyon trail",4.5f,Points(-27,-14,-18,-9,-13,3,-3,11,10,9,19,-1,26,-11));
            Route("Ridge flanking trail",3,Points(-24,-9,-9,-18,8,-17,23,-8));
            Building("FOREST BUNKER",-24,5,8,9,-35);Building("FIELD QUARTERS",18,2,8,9,24);Building("RADAR STATION",4,14,9,7,-15);Building("RANGER SHELTER",-12,-15,7,7,35);
            MapExpansion07.Platform("RIDGE LOOKOUT",At(3,-2),8,7,2.6f,7);
            for(int i=0;i<8;i++)
            {float x=-12+i*3.3f,z=2+Mathf.Sin(i*.8f)*2; if(x>-3&&x<9)continue;var rock=Solid("Interior granite ridge",At(x,z,1.8f),new Vector3(3.8f,3.6f,3.2f),wall,3);rock.transform.localRotation=Quaternion.Euler(0,i*27,0);}
            foreach(var p in Points(-28,-6,-21,15,-9,13,11,16,26,1,21,-14,-25,-14,-4,-20))
            {Solid("Pine trunk",At(p.x,p.y,2),new Vector3(.48f,2,.48f),ClassicArt.Wood,2);for(int j=0;j<3;j++)Visual("Layered pine canopy",At(p.x,p.y,3+j*.9f),new Vector3(3.6f-j*.7f,2.5f,3.6f-j*.7f),leaf,4);}
            Patch(-18,-4,6,7);Patch(21,-7,5,7);Patch(-6,13,5,4,false);Patch(12,-15,6,4);MapExpansion07.Wetland("MUDDY STREAM BED",At(-19,-5),new Vector2(7,5),new Color(.22f,.23f,.15f),.82f);
            Cover(-7,-6,-35);Cover(13,7,20);
        }
        static void Polygon(string name,Vector2[] p,float y,Material mat,bool collider)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();foreach(var point in p){v.Add(At(point.x,point.y,y));uv.Add(point*.12f);}
            var available=new List<int>();for(int i=0;i<p.Length;i++)available.Add(i);var tris=new List<int>();int guard=0;
            while(available.Count>2&&guard++<p.Length*p.Length)
            {
                bool cut=false;for(int k=0;k<available.Count;k++)
                {int a=available[(k+available.Count-1)%available.Count],b=available[k],c=available[(k+1)%available.Count];if(Cross(p[b]-p[a],p[c]-p[b])<=0)continue;bool contains=false;foreach(int j in available)if(j!=a&&j!=b&&j!=c&&Cross(p[b]-p[a],p[j]-p[a])>=0&&Cross(p[c]-p[b],p[j]-p[b])>=0&&Cross(p[a]-p[c],p[j]-p[c])>=0){contains=true;break;}if(contains)continue;tris.AddRange(new[]{a,c,b});available.RemoveAt(k);cut=true;break;}
                if(!cut)throw new InvalidOperationException("Invalid terrain polygon "+id);
            }
            MakeMesh(name,v,tris,uv,mat,collider);
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        static void MakeMesh(string name,List<Vector3> vertices,List<int> indices,List<Vector2> uv,Material mat,bool collider)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();var path=Folder+"/"+id+"_"+name.Replace(" ","_")+".asset";
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
            if(collider){go.layer=LayerMask.NameToLayer("TZWorld");go.AddComponent<MeshCollider>().sharedMesh=mesh;}GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
        }
        static void ValidateRoutes()
        {
            var points=root.GetComponentsInChildren<MapRoutePoint>();if(points.Length<12)throw new InvalidOperationException("Missing route markers "+id);
            if(!NavMesh.SamplePosition(points[0].transform.position,out var origin,.8f,NavMesh.AllAreas))throw new InvalidOperationException("Invalid first route "+id);
            foreach(var p in points)
            {var path=new NavMeshPath();if(!NavMesh.SamplePosition(p.transform.position,out var hit,.8f,NavMesh.AllAreas)||!NavMesh.CalculatePath(origin.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new InvalidOperationException("Disconnected route "+id+" "+p.transform.parent.name+" / "+p.name+" "+p.transform.position);}
        }
    }
}
