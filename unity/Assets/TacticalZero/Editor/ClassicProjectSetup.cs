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

namespace TacticalZero.Editor
{
    public static class ClassicProjectSetup
    {
        const string Root=ClassicArt.Root;
        [MenuItem("Tactical Zero/Restore Classic Campaign")]
        public static void Setup()
        {
            TacticalZeroProjectSetup.Setup();ClassicArt.Init();
            foreach(string dir in new[]{"Weapons","Models","Operators","Previews"}) Directory.CreateDirectory(Root+"/"+dir);
            AssetDatabase.Refresh();
            if(!File.Exists(Root+"/Models/FieldCache.prefab"))
            {
                var cache=new GameObject("Field cache");
                ClassicArt.Part("Body",cache.transform,Vector3.zero,new Vector3(.85f,.6f,.65f),ClassicArt.Steel);
                ClassicArt.Part("Lid",cache.transform,new Vector3(0,.32f,0),new Vector3(.91f,.09f,.71f),ClassicArt.White);
                foreach(int side in new[]{-1,1}){ClassicArt.Part("Latch",cache.transform,new Vector3(side*.25f,.15f,.34f),new Vector3(.09f,.16f,.05f),ClassicArt.Gold);ClassicArt.Part("Reinforcement",cache.transform,new Vector3(side*.43f,0,0),new Vector3(.06f,.62f,.7f),ClassicArt.Rubber);}
                cache.AddComponent<BoxCollider>().size=new Vector3(.9f,.7f,.7f);
                ClassicArt.SavePrefab(cache,Root+"/Models/FieldCache.prefab");
            }
            foreach(var gun in ClassicRules.Data.weapons)
            {
                string modelPath=Root+"/Models/"+gun.id+".prefab";
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if(!model)model=ClassicArt.SavePrefab(ClassicArt.Gun(gun),modelPath);
                GunPreview(gun,model);
                string path=Root+"/Weapons/"+gun.id+".asset";
                if(!File.Exists(path))
                {
                    var definition=ScriptableObject.CreateInstance<WeaponDefinition>();
                    definition.ConfigureClassic(gun,model,Resources.Load<AudioClip>("Audio/"+(gun.id=="USP"?"USP":gun.kind=="shotgun"||gun.kind=="launcher"?"Shotgun":"AK47")));
                    AssetDatabase.CreateAsset(definition,path);
                }
            }
            foreach(var role in ClassicRules.Data.roles)
            {
                string path=Root+"/Operators/"+role.id+".prefab";
                if(!File.Exists(path))ClassicArt.SavePrefab(ClassicArt.Operator(role),path);
            }
            if(!File.Exists(Root+"/Models/Details02.txt"))
            {
                foreach(string id in new[]{"MP6","VECTOR","AWP","RPK"})
                {
                    var gun=ClassicRules.Gun(id);var model=ClassicArt.SavePrefab(ClassicArt.Gun(gun),Root+"/Models/"+id+".prefab");GunPreview(gun,model,true);
                }
                File.WriteAllText(Root+"/Models/Details02.txt","0.2.0 original modular weapon details");
            }
            if(!File.Exists(Root+"/Operators/Details03.txt"))
            {
                foreach(var role in ClassicRules.Data.roles)ClassicArt.SavePrefab(ClassicArt.Operator(role),Root+"/Operators/"+role.id+".prefab");
                File.WriteAllText(Root+"/Operators/Details03.txt","0.2.0 contoured armor, facial equipment and preview weapons");
            }
            foreach(var map in ClassicRules.Data.maps) MapScene(map);
            RefineVisuals();
            bool polish=!File.Exists(Root+"/Polish04.txt")||!File.Exists(Root+"/WeaponSilhouettes04.txt")||!File.Exists(Root+"/ArtFinish04.txt");CombatPolishArt.Apply();CombatPolishArt.FinishArt();
            if(polish)
            {
                foreach(var gun in ClassicRules.Data.weapons)GunPreview(gun,Resources.Load<GameObject>("Classic/Models/"+gun.id),true);
                foreach(var map in ClassicRules.Data.maps)
                {
                    var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath(map),OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
                    try{Preview(map,true);}finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
                }
            }
            bool portraitUpgrade=!File.Exists(Root+"/PortraitsAndOptics06.txt");
            PortraitModelArt.Apply();
            BlenderOperatorArt.ApplyIfAvailable();
            if(portraitUpgrade)foreach(var gun in ClassicRules.Data.weapons)GunPreview(gun,Resources.Load<GameObject>("Classic/Models/"+gun.id),true);
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach(var map in ClassicRules.Data.maps)
            {
                string path=ScenePath(map);if(!scenes.Exists(s=>s.path==path))scenes.Add(new EditorBuildSettingsScene(path,true));
            }
            MapExpansion07.Apply();
            MapCollision08.Apply();
            MapLayout12.Apply();
            VisualPolish10.Apply();
            FieldWeapons14.Apply();
            FieldEnvironment14.Apply();
            BodycamArt15.Apply();
            ContactArt16.Apply();
            EditorBuildSettings.scenes=scenes.ToArray();PlayerSettings.bundleVersion="0.16.0";
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("Classic campaign generated: 8 professions / 4 maps / 4 difficulties / 12 weapons.");
        }
        static string ScenePath(ClassicMap map)=>"Assets/TacticalZero/Scenes/"+map.Scene+".unity";
        public static void RefreshMapPreview(ClassicMap map)=>Preview(map,true);
        public static void RefreshWeaponPreviews()
        {
            foreach(var gun in ClassicRules.Data.weapons)GunPreview(gun,Resources.Load<GameObject>("Classic/Models/"+gun.id),true);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        static void MapScene(ClassicMap map)
        {
            if(File.Exists(ScenePath(map)))return;
            var prior=SceneManager.GetActiveScene();
            if(prior.IsValid()&&string.IsNullOrEmpty(prior.path))EditorSceneManager.SaveScene(prior,AssetDatabase.GenerateUniqueAssetPath("Assets/TacticalZero/Scenes/PreservedWorkspace.unity"));
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                BuildMap(map);
                EditorSceneManager.SaveScene(scene,ScenePath(map));
                Preview(map);
            }
            finally {EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
        }
        static GameObject Solid(string name,Transform parent,Vector3 at,Vector3 size,Material mat,int shape=0)
        {
            var go=ClassicArt.Part(name,parent,at,size,mat,shape);go.layer=LayerMask.NameToLayer("TZWorld");
            var c=go.AddComponent<BoxCollider>();c.size=Vector3.one;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);return go;
        }
        static void BuildMap(ClassicMap map)
        {
            bool docks=map.id=="docks",ruins=map.id=="ruins",mountain=map.id=="outpost";
            Color groundColor=new Color(map.floor[0]/255f,map.floor[1]/255f,map.floor[2]/255f)*.8f;
            var groundMat=ClassicArt.Mat(map.id+"_Ground",groundColor,0,.18f);
            var wallMat=ClassicArt.Mat(map.id+"_Walls",ClassicRules.ColorOf(map.wall)*.85f,.1f,.35f);
            var trim=ClassicArt.Mat(map.id+"_Trim",ClassicRules.ColorOf(map.metal),.45f,.4f);
            var foliage=ClassicArt.Mat(map.id+"_Foliage",ClassicRules.ColorOf(map.grass));
            var stone=ClassicArt.Mat(map.id+"_Stone",groundColor*.65f);
            var geometry=new GameObject("Static architecture").transform;
            Solid("Foundation",geometry,new Vector3(0,-.2f,0),new Vector3(84,.4f,60),groundMat);
            // Thin floor slabs and lane markings follow the original 28 x 20 grid.
            for(int y=1;y<19;y++)for(int x=1;x<27;x++)
            {
                if(map.Blocked(x,y))continue;
                var p=ClassicMap.Position(x+.5f,y+.5f,.012f);
                if(!mountain&&((x+y)%4==0)) ClassicArt.Part("Paving joint",geometry,p,new Vector3(2.94f,.022f,2.94f),groundMat);
                if((docks||!ruins&&!mountain)&&(x==14||y==10)&&((x+y)%2==0))ClassicArt.Part("Route dash",geometry,p+Vector3.up*.02f,new Vector3(.14f,.025f,1.2f),ClassicArt.White);
                if(mountain&&(x==11||y==9))ClassicArt.Part("Dirt track",geometry,p,new Vector3(3,.03f,3),ClassicArt.Mat("Earth",new Color(.36f,.29f,.19f)));
            }
            foreach(var border in new[]{new Vector4(-42,0,1,60),new Vector4(42,0,1,60),new Vector4(0,-30,84,1),new Vector4(0,30,84,1)})
                Solid("Perimeter",geometry,new Vector3(border.x,1.6f,border.y),new Vector3(border.z,3.2f,border.w),wallMat);
            for(int index=0;index<map.walls.Length;index++)
            {
                var r=map.walls[index];Vector3 size=new Vector3(r.w*3,3.7f,r.h*3),center=ClassicMap.Position(r.x+r.w*.5f,r.y+r.h*.5f,1.85f);
                if(docks&&r.y==1)
                {
                    Solid("Seawall",geometry,center-Vector3.up*1.6f,new Vector3(size.x,.5f,size.z),stone);
                    ClassicArt.Part("Tidal water",geometry,center-Vector3.up*1.52f,new Vector3(size.x,.055f,size.z),ClassicArt.Mat("Harbor water",new Color(.075f,.36f,.46f),.55f,.88f));
                    continue;
                }
                if(mountain)
                {
                    Solid("Rock ridge",geometry,center,size,wallMat);
                    for(int k=0;k<r.w+r.h;k++)
                    {
                        float f=(k+.5f)/(r.w+r.h);var rock=ClassicArt.Part("Fractured rock crown",geometry,center+new Vector3((f-.5f)*size.x,1.8f,Mathf.Sin(k*3)*size.z*.2f),new Vector3(2.3f,1.5f,2.2f),k%2==0?stone:wallMat);
                        rock.transform.localRotation=Quaternion.Euler(12*k,22*k,13);
                    }
                    continue;
                }
                Solid(ruins?"Broken courtyard masonry":"Concrete partition",geometry,center,size,wallMat);
                ClassicArt.Part("Wall footing",geometry,center+Vector3.down*1.63f,new Vector3(size.x+.06f,.4f,size.z+.06f),stone);
                ClassicArt.Part("Coping",geometry,center+Vector3.up*1.8f,new Vector3(size.x+.15f,.17f,size.z+.15f),trim);
                int segments=Mathf.CeilToInt(Mathf.Max(size.x,size.z)/2.8f);
                for(int k=0;k<segments;k++)
                {
                    Vector3 post=center+(size.x>size.z?Vector3.right:Vector3.forward)*((k+.5f)/segments-.5f)*Mathf.Max(size.x,size.z);
                    ClassicArt.Part("Support pier",geometry,post,new Vector3(size.x>size.z?.21f:size.x+.13f,3.65f,size.x>size.z?size.z+.13f:.21f),trim);
                    if(ruins)ClassicArt.Part("Masonry seam",geometry,post+Vector3.up*.6f,new Vector3(size.x>size.z?2.5f:size.x+.2f,.06f,size.x>size.z?size.z+.2f:2.5f),stone);
                }
                if(!ruins)
                {
                    Vector3 start=center+new Vector3(-size.x*.45f,1.05f,size.z*.5f+.14f),end=center+new Vector3(size.x*.45f,1.05f,size.z*.5f+.14f);
                    ClassicArt.Rod("Service conduit",geometry,start,end,.09f,trim);
                    ClassicArt.Part("Amber status strip",geometry,center+new Vector3(0,.2f,size.z*.5f+.05f),new Vector3(.7f,.08f,.05f),ClassicArt.Gold);
                }
            }
            for(int index=0;index<map.crates.Length;index++)
            {
                var r=map.crates[index];float height=docks?3.4f:mountain?2.2f:ruins?1.4f:2;
                Vector3 center=ClassicMap.Position(r.x+r.w*.5f,r.y+r.h*.5f,height*.5f),size=new Vector3(r.w*3,height,r.h*3);
                Solid(docks?"Shipping container":ruins?"Vehicle barricade":mountain?"Supply shelter":"Cargo stack",geometry,center,size,trim);
                for(int k=0;k<Mathf.CeilToInt(size.z/.65f);k++)
                    foreach(int side in new[]{-1,1})ClassicArt.Part("Pressed steel rib",geometry,center+new Vector3(side*(size.x*.5f+.035f),0,-size.z*.5f+k*.65f),new Vector3(.08f,height*.92f,.07f),wallMat);
                foreach(int side in new[]{-1,1})
                {
                    ClassicArt.Part("Locking bar",geometry,center+new Vector3(side*size.x*.22f,0,size.z*.5f+.05f),new Vector3(.05f,height*.9f,.05f),ClassicArt.Steel);
                    ClassicArt.Part("Door hinge",geometry,center+new Vector3(side*size.x*.43f,height*.28f,size.z*.5f+.055f),new Vector3(.19f,.1f,.08f),ClassicArt.White);
                }
                ClassicArt.Label("TZ • "+(index+1).ToString("000"),geometry,center+new Vector3(0,.2f,size.z*.5f+.065f),.18f,Color.white);
                if(ruins)for(int side=-1;side<=1;side+=2)for(int k=-1;k<=1;k+=2)ClassicArt.Part("Vehicle tire",geometry,center+new Vector3(side*(size.x*.5f+.06f),-.35f,k*size.z*.3f),new Vector3(.38f,.8f,.8f),ClassicArt.Rubber,1);
            }
            // Grass volumes exactly cover the original brush rectangles; visible tufts are decorative.
            foreach(var r in map.brush)
            {
                var root=new GameObject("Concealment brush");root.transform.position=ClassicMap.Position(r.x+r.w*.5f,r.y+r.h*.5f);root.layer=LayerMask.NameToLayer("TZGrass");
                root.AddComponent<GrassVolume>();var trigger=root.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.up;trigger.size=new Vector3(r.w*3,2,r.h*3);
                for(int y=0;y<r.h*5;y++)for(int x=0;x<r.w*5;x++)
                {
                    float h=.65f+Mathf.Abs(Mathf.Sin(x*32+y*13))*.55f;
                    var tuft=ClassicArt.Part("Foliage blade",root.transform,new Vector3((x+.5f)/5*3-r.w*1.5f,h*.5f,(y+.5f)/5*3-r.h*1.5f),new Vector3(.16f,h,.28f),foliage);
                    tuft.transform.localRotation=Quaternion.Euler(0,x*71+y*29,Mathf.Sin(x+y)*16);
                }
                root.AddComponent<GrassVisualController>();
            }
            SetDressing(map,geometry,wallMat,trim,foliage);
            var surface=geometry.gameObject.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.layerMask=LayerMask.GetMask("TZWorld");
            Physics.SyncTransforms();surface.BuildNavMesh();
            if(!surface.navMeshData)throw new InvalidOperationException("Navigation generation failed: "+map.id);
            AssetDatabase.CreateAsset(surface.navMeshData,AssetDatabase.GenerateUniqueAssetPath(Root+"/"+map.id+"_Navigation.asset"));
            var catalog=Resources.Load<PrototypeCatalog>("PrototypeCatalog");
            var player=(GameObject)PrefabUtility.InstantiatePrefab(catalog.PlayerPrefab);player.transform.position=ClassicMap.Position(3.5f,17.5f,.1f);
            var encounter=new GameObject("Classic mission").AddComponent<ClassicEncounter>();encounter.MapId=map.id;
            new GameObject("Scene entry").AddComponent<SceneEntry>();new GameObject("Campaign HUD").AddComponent<GameUI>();
            // Three point environmental lighting, fog and map-specific palettes.
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=docks?new Color(.43f,.62f,.75f):ruins?new Color(.65f,.44f,.32f):new Color(.46f,.6f,.64f);
            RenderSettings.ambientEquatorColor=new Color(.22f,.28f,.3f);RenderSettings.ambientGroundColor=new Color(.12f,.14f,.16f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=35;RenderSettings.fogEndDistance=105;RenderSettings.fogColor=docks?new Color(.23f,.42f,.51f):ruins?new Color(.49f,.35f,.28f):new Color(.25f,.37f,.37f);
            var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=ruins?1.8f:1.4f;sun.color=ruins?new Color(1,.71f,.43f):new Color(1,.94f,.82f);sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(ruins?24:48,-35,0);RenderSettings.sun=sun;
            var rim=new GameObject("Sky fill").AddComponent<Light>();rim.type=LightType.Directional;rim.intensity=.35f;rim.color=new Color(.4f,.7f,1);rim.transform.rotation=Quaternion.Euler(35,150,0);
            foreach(var renderer in geometry.GetComponentsInChildren<MeshRenderer>())GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);
        }
        static void SetDressing(ClassicMap map,Transform parent,Material wall,Material trim,Material foliage)
        {
            if(map.id=="docks")
            {
                foreach(float x in new[]{-11f,17f})
                {
                    ClassicArt.Part("Crane leg",parent,new Vector3(x,6,24),new Vector3(.8f,12,.8f),trim);
                    ClassicArt.Part("Crane gantry",parent,new Vector3(x+4,11.7f,24),new Vector3(10,.6f,.8f),trim);
                    ClassicArt.Rod("Crane tension cable",parent,new Vector3(x+7,11.8f,24),new Vector3(x+7,4,24),.035f,ClassicArt.Rubber);
                    ClassicArt.Rod("Crane diagonal",parent,new Vector3(x,7,24),new Vector3(x+6,11.6f,24),.22f,trim);
                }
                for(int i=0;i<14;i++)ClassicArt.Part("Ocean horizon",parent,new Vector3(-39+i*6,-.1f,42),new Vector3(6,.1f,22),ClassicArt.Mat("Harbor water",new Color(.075f,.36f,.46f),.55f,.88f));
            }
            else if(map.id=="outpost")
            {
                for(int i=0;i<24;i++)
                {
                    float a=i*Mathf.PI*2/24;Vector3 p=new Vector3(Mathf.Cos(a)*47,0,Mathf.Sin(a)*35);
                    ClassicArt.Rod("Pine trunk",parent,p,p+Vector3.up*7,.45f,ClassicArt.Wood);
                    for(int tier=0;tier<3;tier++)ClassicArt.Part("Pine canopy",parent,p+Vector3.up*(4+tier*1.5f),new Vector3(4-tier,3,4-tier),foliage);
                    if(i%3==0)ClassicArt.Part("Distant ridge",parent,p*1.18f+Vector3.up*3,new Vector3(10,9,12),wall);
                }
                ClassicArt.Rod("Radar mast",parent,new Vector3(30,0,-20),new Vector3(30,10,-20),.28f,ClassicArt.Steel);
                ClassicArt.Part("Radar dish",parent,new Vector3(30,10,-20),new Vector3(3,2,.5f),ClassicArt.White,1);
            }
            else if(map.id=="ruins")
            {
                for(int i=0;i<12;i++)
                {
                    Vector3 p=new Vector3(-38+i*7,5,35);ClassicArt.Part("City silhouette",parent,p,new Vector3(6,10+(i%3)*3,8),wall);
                    for(int f=0;f<3;f++)for(int w=0;w<3;w++)ClassicArt.Part("Dark window",parent,p+new Vector3(-1.8f+w*1.7f,-2+f*2.2f,-4.03f),new Vector3(.8f,1.25f,.04f),ClassicArt.Rubber);
                }
            }
            else
            {
                for(int i=0;i<5;i++)
                {
                    float z=-23+i*11;ClassicArt.Part("Overhead truss",parent,new Vector3(0,7,z),new Vector3(81,.35f,.4f),ClassicArt.Steel);
                    for(int j=0;j<8;j++)ClassicArt.Rod("Truss diagonal",parent,new Vector3(-38+j*10,7,z),new Vector3(-33+j*10,8,z),.12f,trim);
                    foreach(float x in new[]{-37f,37f})ClassicArt.Part("Truss column",parent,new Vector3(x,3.5f,z),new Vector3(.42f,7,.42f),ClassicArt.Steel);
                    ClassicArt.Part("Suspended light",parent,new Vector3(0,6.8f,z),new Vector3(3,.07f,.3f),ClassicArt.Gold);
                }
            }
            ClassicArt.Label(map.code,parent,new Vector3(0,3.5f,29.35f),1.1f,new Color(.8f,.87f,.8f),Quaternion.Euler(0,180,0));
        }
        static void Preview(ClassicMap map,bool force=false)
        {
            string path=Root+"/Previews/"+map.id+".png";if(File.Exists(path)&&!force)return;
            var hidden=new List<Renderer>();foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var renderer in root.GetComponentsInChildren<Renderer>())if(renderer.enabled&&(renderer.name=="Roof panel"||renderer.name=="Room roof")){hidden.Add(renderer);renderer.enabled=false;}
            bool fog=RenderSettings.fog;RenderSettings.fog=false;
            var go=new GameObject("Preview camera");var camera=go.AddComponent<Camera>();camera.transform.position=new Vector3(47,68,-55);camera.transform.LookAt(Vector3.zero);
            camera.orthographic=true;camera.orthographicSize=43;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.085f,.11f);
            var rt=new RenderTexture(800,500,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(800,500,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,800,500),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
            RenderTexture.active=old;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);AssetDatabase.ImportAsset(path);RenderSettings.fog=fog;foreach(var renderer in hidden)renderer.enabled=true;
        }
        static void RefineVisuals()
        {
            var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");font.RequestCharactersInTexture("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .•/-",60);
            string materialPath=Root+"/Materials/WorldLettering.mat";var lettering=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!lettering)
            {
                lettering=new Material(Shader.Find("Universal Render Pipeline/Unlit"));lettering.SetTexture("_BaseMap",font.material.mainTexture);lettering.SetColor("_BaseColor",new Color(.86f,.91f,.85f));lettering.SetFloat("_AlphaClip",1);lettering.SetFloat("_Cutoff",.4f);lettering.EnableKeyword("_ALPHATEST_ON");lettering.renderQueue=2450;AssetDatabase.CreateAsset(lettering,materialPath);
            }
            foreach(var map in ClassicRules.Data.maps)
            {
                var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath(map),OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
                try
                {
                    bool refined=false;foreach(var root in scene.GetRootGameObjects())foreach(var node in root.GetComponentsInChildren<Transform>(true))if(node.name=="Art refinement 02")refined=true;
                    if(refined)continue;
                    var detail=new GameObject("Art refinement 02").transform;
                    foreach(var root in scene.GetRootGameObjects())
                    {
                        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                        {
                            if(filter.name=="Fractured rock crown"||filter.name=="Distant ridge")filter.sharedMesh=ClassicArt.Rock;
                            if(filter.name=="Pine canopy")filter.sharedMesh=ClassicArt.Pine;
                        }
                        foreach(var label in root.GetComponentsInChildren<TextMesh>())
                        {
                            label.font=font;label.GetComponent<MeshRenderer>().sharedMaterial=lettering;
                            if(label.text.StartsWith("SECTOR")){label.transform.localRotation=Quaternion.identity;label.characterSize=.45f;}
                        }
                    }
                    foreach(var r in map.walls)
                    {
                        if(map.id!="ruins")continue;
                        Vector3 face=ClassicMap.Position(r.x+r.w*.5f,r.y,1.9f);
                        for(int j=0;j<r.w;j++)
                        {
                            Vector3 at=face+new Vector3((j-(r.w-1)*.5f)*3,0,.04f);
                            ClassicArt.Part("Boarded window recess",detail,at,new Vector3(1.4f,1.65f,.04f),ClassicArt.Rubber);
                            foreach(int side in new[]{-1,1})ClassicArt.Part("Window jamb",detail,at+Vector3.right*side*.74f,new Vector3(.12f,1.8f,.12f),ClassicArt.White);
                            var board=ClassicArt.Part("Diagonal window plank",detail,at+Vector3.forward*.06f,new Vector3(1.8f,.18f,.06f),ClassicArt.Wood);board.transform.localRotation=Quaternion.Euler(0,0,23);
                        }
                    }
                    for(int i=0;i<45;i++)
                    {
                        int x=1+(i*13)%26,y=1+(i*7)%18;if(map.Blocked(x,y))continue;Vector3 at=ClassicMap.Position(x+.35f,y+.25f,.04f);
                        if(map.id=="ruins"||map.id=="outpost")ClassicArt.Part("Scattered stone",detail,at,new Vector3(.2f+(i%3)*.12f,.12f,.25f),ClassicArt.Mat("Rubble",new Color(.37f,.36f,.31f)),3);
                        else
                        {
                            ClassicArt.Part("Drain cover",detail,at,new Vector3(.8f,.04f,.5f),ClassicArt.Steel);
                            for(int k=0;k<6;k++)ClassicArt.Part("Drain slot",detail,at+new Vector3(-.3f+k*.12f,.025f,0),new Vector3(.03f,.012f,.4f),ClassicArt.Rubber);
                        }
                    }
                    RenderSettings.fogStartDistance=45;RenderSettings.fogEndDistance=145;
                    EditorSceneManager.SaveScene(scene);Preview(map,true);
                }
                finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
            }
        }
        static void GunPreview(ClassicGun gun,GameObject prefab,bool force=false)
        {
            string path=Root+"/Previews/gun_"+gun.id+".png";if(File.Exists(path)&&!force)return;
            var model=UnityEngine.Object.Instantiate(prefab);model.transform.position=new Vector3(1200,1200,1200);
            foreach(var part in model.GetComponentsInChildren<Transform>())if(part.name.Contains("sleeve")||part.name.Contains("glove")||part.name.Contains("hand")||part.name.Contains("Finger"))part.gameObject.SetActive(false);
            var go=new GameObject("Weapon photo camera");var camera=go.AddComponent<Camera>();camera.transform.position=model.transform.position+new Vector3(1.1f,.5f,.75f);camera.transform.LookAt(model.transform.position+Vector3.forward*.13f);camera.orthographic=true;camera.orthographicSize=.55f;camera.farClipPlane=5;camera.backgroundColor=new Color(.07f,.12f,.16f);camera.clearFlags=CameraClearFlags.SolidColor;
            var lamp=new GameObject("Weapon photo lamp");lamp.transform.position=model.transform.position+new Vector3(1,1.5f,.7f);var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=5;light.intensity=12;
            var rt=new RenderTexture(320,220,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(320,220,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,320,220),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(lamp);UnityEngine.Object.DestroyImmediate(model);AssetDatabase.ImportAsset(path);
        }
    }
}

