using System;
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
    public static class FieldEnvironment14
    {
        public static void Apply()
        {
            ClassicArt.Init();
            CreateReflection();
            foreach(var map in ClassicRules.Data.maps)
            {
                var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene("Assets/TacticalZero/Scenes/"+map.Scene+".unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
                try
                {
                    var geo=Array.Find(scene.GetRootGameObjects(),g=>g.name=="Static architecture");if(!geo||geo.transform.Find("Field environment 14"))continue;
                    var root=new GameObject("Field environment 14").transform;root.SetParent(geo.transform,false);
                    var wall=Resources.Load<Material>("Classic/Materials/"+map.id+"_Walls");var trim=Resources.Load<Material>("Classic/Materials/"+map.id+"_Trim");
                    GameObject Part(string n,Transform parent,Vector3 at,Vector3 size,Material mat,bool solid=false,int shape=0)
                    {
                        var obj=ClassicArt.Part(n,parent,at,size,mat,shape);obj.layer=LayerMask.NameToLayer("TZWorld");
                        GameObjectUtility.SetStaticEditorFlags(obj,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.OccluderStatic);
                        if(solid)obj.AddComponent<BoxCollider>();else obj.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                        return obj;
                    }
                    foreach(var area in geo.GetComponentsInChildren<MapArea>())if(area.Interior&&area.Size.x>=6&&area.Size.z>=6)
                    {
                        var room=new GameObject("Interior flanking cover").transform;room.SetParent(root,false);room.SetPositionAndRotation(area.transform.position,area.transform.rotation);
                        // Staggered waist-high barriers create two flanking choices, with continuous passage on both sides.
                        Part("Offset concrete barricade",room,new Vector3(-area.Size.x*.20f,.65f,-.7f),new Vector3(1.6f,1.3f,.45f),wall,true);
                        Part("Offset equipment barricade",room,new Vector3(area.Size.x*.20f,.50f,.9f),new Vector3(1.25f,1,.7f),trim,true);
                        Part("Cable tray",room,new Vector3(0,2.95f,0),new Vector3(.35f,.08f,area.Size.z-.8f),ClassicArt.Steel);
                        for(int side=-1;side<=1;side+=2)
                        {
                            Part("Overhead practical light",room,new Vector3(side*area.Size.x*.25f,2.82f,0),new Vector3(.6f,.07f,.18f),ClassicArt.White);
                            var light=new GameObject("Interior practical fill").AddComponent<Light>();light.transform.SetParent(room,false);light.transform.localPosition=new Vector3(side*area.Size.x*.25f,2.5f,0);light.type=LightType.Point;light.range=5;light.intensity=.75f;light.color=new Color(1,.86f,.69f);light.shadows=LightShadows.None;
                        }
                    }
                    var backdrop=ClassicArt.Mat(map.id+"_Distant14",map.id=="outpost"?new Color(.18f,.23f,.19f):new Color(.19f,.24f,.27f),0,.12f);
                    for(int i=0;i<24;i++)
                    {
                        float a=i*Mathf.PI*2/24;Vector3 at=new Vector3(Mathf.Cos(a)*82,0,Mathf.Sin(a)*66);float h=7+(i*7%13);
                        if(map.id=="outpost"||map.id=="ruins")Part("Layered distant ridge",root,at+Vector3.up*(h*.25f-3),new Vector3(19,h,16),backdrop,false,3);
                        else
                        {
                            Part("Distant industrial silhouette",root,at+Vector3.up*h*.5f,new Vector3(9,h,8),backdrop);
                            if(i%4==0){Part("Distant crane mast",root,at+Vector3.up*(h+7),new Vector3(.65f,18,.65f),backdrop);Part("Distant crane jib",root,at+new Vector3(7,h+14,0),new Vector3(21,.7f,.7f),backdrop);}
                        }
                    }
                    // Rebuild collision-based navigation and verify every authored route remains connected.
                    Physics.SyncTransforms();var surface=geo.GetComponent<NavMeshSurface>();surface.BuildNavMesh();var nav=surface.navMeshData;
                    string path=ClassicArt.Root+"/Meshes/Layout12/"+map.id+"_Navigation14.asset";AssetDatabase.CreateAsset(nav,path);
                    var routes=geo.GetComponentsInChildren<MapRoutePoint>();var pathCheck=new NavMeshPath();Vector3 origin=Vector3.zero;bool found=false;
                    foreach(var route in routes)
                    {
                        if(!NavMesh.SamplePosition(route.transform.position,out var hit,1.2f,NavMesh.AllAreas))throw new InvalidOperationException("Blocked route "+map.id+" / "+route.name);
                        if(!found){origin=hit.position;found=true;}else if(!NavMesh.CalculatePath(origin,hit.position,NavMesh.AllAreas,pathCheck)||pathCheck.status!=NavMeshPathStatus.PathComplete)throw new InvalidOperationException("Disconnected route "+route.name);
                    }
                    foreach(var light in geo.GetComponentsInChildren<Light>())if(light.type==LightType.Point)light.shadows=LightShadows.None;
                    EditorSceneManager.SaveScene(scene);Debug.Log("FIELD_ENVIRONMENT "+map.id+" verified routes="+routes.Length);
                }
                finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
            }
            AssetDatabase.SaveAssets();
        }
        static void CreateReflection()
        {
            string path=ClassicArt.Root+"/Textures/FieldReflection14.asset";if(File.Exists(path))return;
            const int size=64;var cube=new Cubemap(size,TextureFormat.RGBAHalf,true){name="FieldReflection14",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};
            for(int face=0;face<6;face++)
            {
                var pixels=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1;
                    Vector3 dir=face==0?new Vector3(1,-v,-u):face==1?new Vector3(-1,-v,u):face==2?new Vector3(u,1,v):face==3?new Vector3(u,-1,-v):face==4?new Vector3(u,-v,1):new Vector3(-u,-v,-1);dir.Normalize();
                    var c=Color.Lerp(new Color(.12f,.14f,.14f),new Color(.5f,.62f,.74f),Mathf.Clamp01(dir.y*.5f+.5f));float sun=Mathf.Pow(Mathf.Max(0,Vector3.Dot(dir,new Vector3(-.4f,.7f,.5f).normalized)),80);pixels[x+y*size]=c+new Color(1,.91f,.72f)*sun*2;
                }
                cube.SetPixels(pixels,(CubemapFace)face);
            }
            cube.Apply(true,false);AssetDatabase.CreateAsset(cube,path);
        }
    }
}
