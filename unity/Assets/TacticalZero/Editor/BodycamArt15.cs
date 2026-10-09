using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace TacticalZero.Editor
{
    public static class BodycamArt15
    {
        const string Art="Assets/TacticalZero/Art/Bodycam15";
        [Serializable]class Document {public Surface[] materials;}
        [Serializable]class Surface {public string name;public float[] color;}
        static Material floor,wall,rust,mud,painted;
        public static void Apply()
        {
            AssetDatabase.Refresh();Import();BodycamWeapons15.Apply();
            floor=Scanned("Concrete floor","concrete_floor_worn_001",Color.white,.35f);
            wall=Scanned("Aged concrete","concrete_wall_007",new Color(.8f,.82f,.80f),.45f);
            rust=Scanned("Oxidized steel","rusty_metal_02",new Color(.66f,.70f,.69f),.7f,.55f);
            mud=Scanned("Damp earth","brown_mud_03",new Color(.65f,.69f,.61f),.35f);
            painted=Scanned("Worn painted steel","concrete_floor_worn_001",new Color(1.8f,1.95f,2.0f),1.8f,.08f);painted.SetFloat("_BumpScale",.10f);
            Arms();Operators();
            foreach(var map in ClassicRules.Data.maps)Map(map);
            ClassicProjectSetup.RefreshWeaponPreviews();
            AssetDatabase.SaveAssets();Debug.Log("BODYCAM15 scanned surfaces, HDR lighting, skeletal arms and detailed environment applied");
        }
        static void Import()
        {
            foreach(var file in Directory.GetFiles(Art,"*.jpg"))
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(file);bool normal=file.Contains("nor_gl"),linear=normal||file.Contains("Rough");
                t.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;t.sRGBTexture=!linear;t.maxTextureSize=2048;t.anisoLevel=8;t.mipmapEnabled=true;t.wrapMode=TextureWrapMode.Repeat;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
            }
            var hdr=(TextureImporter)AssetImporter.GetAtPath(Art+"/overcast_soil_puresky_hdri.hdr");hdr.textureShape=TextureImporterShape.TextureCube;hdr.generateCubemap=TextureImporterGenerateCubemap.AutoCubemap;hdr.maxTextureSize=1024;hdr.sRGBTexture=false;
            var settings=new TextureImporterSettings();hdr.ReadTextureSettings(settings);settings.cubemapConvolution=TextureImporterCubemapConvolution.Specular;hdr.SetTextureSettings(settings);hdr.SaveAndReimport();
            var doc=JsonUtility.FromJson<Document>(File.ReadAllText(Art+"/materials.json"));
            foreach(var surface in doc.materials)
            {
                var mat=Material(surface.name,"Universal Render Pipeline/Lit");mat.color=new Color(surface.color[0],surface.color[1],surface.color[2]).gamma;
                mat.SetFloat("_Metallic",surface.name.Contains("steel")?.7f:surface.name.Contains("paint")?.25f:0);mat.SetFloat("_Smoothness",surface.name.Contains("steel")?.48f:.23f);
                Microdetail(mat,surface.name.Contains("cloth")||surface.name.Contains("leather"));
            }
            foreach(var file in Directory.GetFiles(Art,"*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(file);importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                foreach(var surface in doc.materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),surface.name),AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+surface.name+".mat"));
                importer.SaveAndReimport();
            }
        }
        static Material Material(string name,string shader)
        {
            string path=Art+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find(shader)){name=name,enableInstancing=true};AssetDatabase.CreateAsset(mat,path);}EditorUtility.SetDirty(mat);return mat;
        }
        static Material Scanned(string name,string scan,Color color,float repeats,float metal=0)
        {
            var m=Material(name,"TacticalZero/Scanned Surface");m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+scan+"_Diffuse.jpg"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+scan+"_nor_gl.jpg"));m.SetTexture("_RoughMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+scan+"_Rough.jpg"));m.SetColor("_BaseColor",color);m.SetFloat("_Scale",repeats);m.SetFloat("_BumpScale",.6f);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",metal>0?.85f:.6f);return m;
        }
        static void Microdetail(Material mat,bool woven)
        {
            string path=Art+(woven?"/WovenNormal.asset":"/MetalNormal.asset");var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!tex)
            {
                tex=new Texture2D(128,128,TextureFormat.RGBA32,true,true){name=woven?"Woven fabric relief":"Machined surface relief",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};var colors=new Color[128*128];
                for(int y=0;y<128;y++)for(int x=0;x<128;x++)
                {
                    float dx=woven?Mathf.Sin(x*Mathf.PI*.5f)*.16f:(Mathf.PerlinNoise(x*.17f,y*.5f)-.5f)*.14f;
                    float dy=woven?Mathf.Sin(y*Mathf.PI*.5f)*.16f:(Mathf.PerlinNoise(y*.24f,x*.8f)-.5f)*.14f;
                    colors[x+y*128]=new Color(.5f+dx,.5f+dy,1,1);
                }
                tex.SetPixels(colors);tex.Apply();AssetDatabase.CreateAsset(tex,path);
            }
            mat.SetTexture("_BumpMap",tex);mat.SetFloat("_BumpScale",woven?.4f:.12f);mat.EnableKeyword("_NORMALMAP");mat.SetTextureScale("_BaseMap",Vector2.one*(woven?3:2));EditorUtility.SetDirty(mat);
        }
        static Transform Find(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        static void Arms()
        {
            foreach(var gun in ClassicRules.Data.weapons)
            {
                var path=ClassicArt.Root+"/Models/"+gun.id+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var anim=root.GetComponent<WeaponReloadAnimation>();if(!anim)continue;
                    foreach(var old in root.GetComponentsInChildren<FirstPersonArm>(true))Object.DestroyImmediate(old.gameObject);
                    foreach(var r in root.GetComponentsInChildren<Renderer>(true))
                    {
                        var n=r.name.ToLowerInvariant();if(!n.Contains("handguard")&&(n.Contains("sleeve")||n.Contains("glove")||n.Contains("hand")||n.Contains("finger")))r.enabled=false;
                        foreach(var m in r.sharedMaterials)if(m&&m.shader.name=="Universal Render Pipeline/Lit"&&!n.Contains("lens")&&!n.Contains("glass"))Microdetail(m,n.Contains("sleeve")||n.Contains("glove"));
                    }
                    foreach(var side in new[]{"L","R"})
                    {
                        var target=side=="L"?anim.LeftHand:anim.RightHand;if(!target)continue;
                        var arm=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Arm_"+side+".fbx"),root.transform);arm.name="Anatomical arm "+side;
                        var solver=arm.AddComponent<FirstPersonArm>();solver.Left=side=="L";solver.Upper=Find(arm,"upper."+side);solver.Lower=Find(arm,"lower."+side);solver.Hand=Find(arm,"hand."+side);solver.Target=target;
                        foreach(var skin in arm.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;skin.shadowCastingMode=ShadowCastingMode.Off;skin.receiveShadows=false;}
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        static T Component<T>(VolumeProfile p)where T:VolumeComponent
        {if(!p.TryGet<T>(out var c)){c=p.Add<T>(true);AssetDatabase.AddObjectToAsset(c,p);}EditorUtility.SetDirty(c);return c;}
        static void Operators()
        {
            foreach(var role in ClassicRules.Data.roles)
            {
                string path=ClassicArt.Root+"/Operators/"+role.id+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    root.transform.localScale=new Vector3(1.10f,1,1.02f);
                    foreach(var old in root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Field kit ")).ToArray())Object.DestroyImmediate(old.gameObject);
                    foreach(var tuple in new[]{("Helmet","head"),("PlateCarrier","chest")})
                    {
                        var bone=Find(root,tuple.Item2);if(!bone)continue;
                        var kit=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/"+tuple.Item1+".fbx"),root.transform);kit.name="Field kit "+tuple.Item1;kit.transform.SetParent(bone,true);
                    }
                    foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        skin.updateWhenOffscreen=false;int weaponBone=Array.FindIndex(skin.bones,b=>b&&b.name=="weapon");if(weaponBone<0)continue;
                        string mp=Art+"/"+role.id+"_"+skin.transform.parent.name+"_field.asset";
                        var clean=AssetDatabase.LoadAssetAtPath<Mesh>(mp);
                        if(!clean)
                        {
                            clean=Object.Instantiate(skin.sharedMesh);clean.name=role.id+" clean field skin";var weights=clean.boneWeights;
                            bool Slung(int v){var w=weights[v];return(w.boneIndex0==weaponBone?w.weight0:0)+(w.boneIndex1==weaponBone?w.weight1:0)+(w.boneIndex2==weaponBone?w.weight2:0)+(w.boneIndex3==weaponBone?w.weight3:0)>.5f;}
                            for(int sub=0;sub<clean.subMeshCount;sub++)
                            {var tri=clean.GetTriangles(sub);var keep=new System.Collections.Generic.List<int>();for(int i=0;i<tri.Length;i+=3)if(!Slung(tri[i])&&!Slung(tri[i+1])&&!Slung(tri[i+2])){keep.Add(tri[i]);keep.Add(tri[i+1]);keep.Add(tri[i+2]);}clean.SetTriangles(keep,sub);}
                            AssetDatabase.CreateAsset(clean,mp);
                        }
                        skin.sharedMesh=clean;
                    }
                    var document=JsonUtility.FromJson<RefinedOperatorArt.Document>(File.ReadAllText("Assets/TacticalZero/Art/RefinedOperators/"+role.id+".json"));
                    foreach(var surface in document.materials)if(surface.atlas&&surface.normal)
                    {
                        var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/TacticalZero/Art/RefinedOperators/Generated/"+surface.name+".mat");if(!mat)continue;
                        mat.SetTexture("_BaseMap",MutedAtlas(role.id));mat.SetColor("_BaseColor",new Color(.85f,.85f,.77f));
                        EditorUtility.SetDirty(mat);
                    }
                    var lod=root.GetComponent<LODGroup>();if(lod){lod.SetLODs(new[]{new LOD(.012f,root.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();}
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        static Texture2D MutedAtlas(string role)
        {
            string path=Art+"/"+role+"_MutedCloth.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture)return texture;
            var source=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TacticalZero/Art/RefinedOperators/"+role+"_BaseColor.png");var rt=RenderTexture.GetTemporary(2048,2048,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;
            try
            {
                Graphics.Blit(source,rt);RenderTexture.active=rt;texture=new Texture2D(2048,2048,TextureFormat.RGBA32,true,false){name=role+" muted field fabric",filterMode=FilterMode.Trilinear,anisoLevel=4};texture.ReadPixels(new Rect(0,0,2048,2048),0,0);var pixels=texture.GetPixels32();
                for(int i=0;i<pixels.Length;i++){var c=pixels[i];float gray=c.r*.25f+c.g*.60f+c.b*.15f;pixels[i]=new Color32((byte)Mathf.Lerp(c.r,gray,.8f),(byte)Mathf.Lerp(c.g,gray,.8f),(byte)Mathf.Lerp(c.b,gray,.8f),c.a);}
                texture.SetPixels32(pixels);texture.Apply(true,false);EditorUtility.CompressTexture(texture,TextureFormat.DXT5,TextureCompressionQuality.Best);texture.Apply(false,true);AssetDatabase.CreateAsset(texture,path);return texture;
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
        }
        static void Map(ClassicMap map)
        {
            var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene("Assets/TacticalZero/Scenes/"+map.Scene+".unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var geo=Array.Find(scene.GetRootGameObjects(),g=>g.name=="Static architecture");
                var signFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                signFont.RequestCharactersInTexture("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .•/-",60);
                var lettering=Material("WorldLettering","TacticalZero/World Lettering");
                lettering.shader=Shader.Find("TacticalZero/World Lettering");
                lettering.SetTexture("_MainTex",signFont.material.mainTexture);
                foreach(var label in geo.GetComponentsInChildren<TextMesh>(true))
                {
                    label.font=signFont;label.GetComponent<MeshRenderer>().sharedMaterial=lettering;
                }
                var old=geo.transform.Find("Bodycam environment 15");if(old)Object.DestroyImmediate(old.gameObject);
                foreach(var renderer in geo.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if(renderer.GetComponent<TextMesh>())continue;
                    string n=renderer.name.ToLowerInvariant(),m=renderer.sharedMaterial?renderer.sharedMaterial.name.ToLowerInvariant():"";
                    if(n.Contains("distant")||n.Contains("sky")||n.Contains("light")||n.Contains("water")||n.Contains("sign"))continue;
                    if(n.Contains("pine canopy")||n.Contains("pine crown")){renderer.enabled=false;continue;}
                    if(m.Contains("wall")||n.Contains("concrete")||n.Contains("masonry")||n.Contains("stone")||n.Contains("rock"))renderer.sharedMaterial=wall;
                    else if(m.Contains("ground")||m.Contains("route12")||n.Contains("floor")||n.Contains("pavement")||n.Contains("asphalt"))renderer.sharedMaterial=map.id=="outpost"?mud:floor;
                    else if(m.Contains("trim")||m.Contains("steel")||n.Contains("container")||n.Contains("pipe")||n.Contains("tank")||m.Contains("oxidized"))renderer.sharedMaterial=n.Contains("rust")?rust:painted;
                }
                var extra=new GameObject("Bodycam environment 15").transform;extra.SetParent(geo.transform,false);
                BodycamArchitecture15.Build(extra,map.id,wall,painted);
                GameObject Prop(string id,Transform parent,Vector3 at,Quaternion rotation,float scale=1)
                {
                    var o=new GameObject("Field detail "+id);o.transform.SetParent(parent,false);o.transform.localPosition=at;o.transform.localRotation=rotation;o.transform.localScale=Vector3.one*scale;o.layer=LayerMask.NameToLayer("TZWorld");
                    Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/"+id+".fbx"),o.transform);
                    if(id=="Drum"){var collider=o.AddComponent<CapsuleCollider>();collider.center=new Vector3(0,.44f,0);collider.height=.88f;collider.radius=.29f;collider.direction=1;}
                    else if(id=="TimberCrate"||id=="ElectricalBox"){var collider=o.AddComponent<BoxCollider>();collider.center=new Vector3(0,id=="TimberCrate"?.43f:.48f,0);collider.size=id=="TimberCrate"?new Vector3(.85f,.86f,.69f):new Vector3(.64f,.90f,.27f);}
                    foreach(var r in o.GetComponentsInChildren<Renderer>()){r.gameObject.layer=LayerMask.NameToLayer("TZWorld");GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic);}
                    return o;
                }
                foreach(var area in geo.GetComponentsInChildren<MapArea>())if(area.Interior)
                {
                    var room=new GameObject("Lived-in room detail").transform;room.SetParent(extra,false);room.SetPositionAndRotation(area.transform.position,area.transform.rotation);
                    Prop("ElectricalBox",room,new Vector3(-area.Size.x*.5f+.17f,1.0f,1),Quaternion.Euler(0,90,0));
                    Prop("Drum",room,new Vector3(area.Size.x*.5f-.5f,.03f,-area.Size.z*.3f),Quaternion.identity);
                    Prop("TimberCrate",room,new Vector3(area.Size.x*.5f-.55f,.03f,-area.Size.z*.3f+1),Quaternion.Euler(0,9,0));
                    for(int k=0;k<4;k++)Prop("Rubble",room,new Vector3((k%2==0?-1:1)*(area.Size.x*.5f-.55f),.015f,(k-1.5f)*1.2f),Quaternion.Euler(0,k*79,0),.65f);
                }
                var random=new System.Random(map.id.GetHashCode());
                for(int i=0;i<48;i++)
                {
                    var pos=new Vector3((float)random.NextDouble()*75-37.5f,12,(float)random.NextDouble()*51-25.5f);
                    if(Physics.Raycast(pos,Vector3.down,out var hit,15,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore)&&hit.normal.y>.8f&&hit.point.y<.3f)Prop("Rubble",extra,hit.point+Vector3.up*.012f,Quaternion.Euler(0,i*137,0),.45f+(float)random.NextDouble()*.65f);
                }
                for(int i=0;i<(map.id=="outpost"?120:35);i++)
                {
                    var p=new Vector3((float)random.NextDouble()*74-37,10,(float)random.NextDouble()*50-25);
                    if(Physics.Raycast(p,Vector3.down,out var hit,12,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore)&&hit.normal.y>.85f&&hit.point.y<.2f)Prop("Grass",extra,hit.point,Quaternion.Euler(0,i*111,0),map.id=="outpost"?.8f:.4f);
                }
                if(map.id=="outpost"||map.id=="ruins")
                {
                    foreach(var r in geo.GetComponentsInChildren<MeshRenderer>())if(r.name.ToLowerInvariant().Contains("pine")&&!r.name.Contains("Field"))r.enabled=false;
                    for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;Prop("Pine",extra,new Vector3(Mathf.Cos(a)*(43+i%3*3),0,Mathf.Sin(a)*(33+i%4*3)),Quaternion.Euler(0,i*137,0),.85f+(i%5)*.13f);}
                }
                var sky=Material("Overcast photographic sky","Skybox/Cubemap");var hdri=AssetDatabase.LoadAssetAtPath<Cubemap>(Art+"/overcast_soil_puresky_hdri.hdr");sky.SetTexture("_Tex",hdri);sky.SetFloat("_Exposure",.75f);sky.SetFloat("_Rotation",110);RenderSettings.skybox=sky;
                RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=hdri;RenderSettings.reflectionIntensity=.6f;
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.53f,.56f,.58f);
                var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(new Color(.14f,.16f,.18f));ambient.AddDirectionalLight(Vector3.up,new Color(.8f,.86f,1),.3f);RenderSettings.ambientProbe=ambient;
                if(RenderSettings.sun){RenderSettings.sun.intensity=1.05f;RenderSettings.sun.color=new Color(.92f,.95f,1);RenderSettings.sun.transform.rotation=Quaternion.Euler(48,-35,0);RenderSettings.sun.shadowNormalBias=.12f;RenderSettings.sun.shadowBias=.025f;RenderSettings.sun.shadowStrength=.78f;}
                foreach(var root in scene.GetRootGameObjects())if(root.name=="Sky fill"&&root.TryGetComponent<Light>(out var fill))fill.intensity=.12f;
                foreach(var light in geo.GetComponentsInChildren<Light>())if(!light.name.StartsWith("Hall")){light.intensity=1.5f;light.range=6;light.color=new Color(1,.81f,.59f);}
                RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.49f,.53f,.55f);RenderSettings.fogStartDistance=32;RenderSettings.fogEndDistance=map.id=="outpost"?130:105;
                var style=scene.GetRootGameObjects().Select(g=>g.GetComponent<SceneVisualStyle>()).FirstOrDefault(g=>g);
                string profilePath=Art+"/"+map.id+" Body camera.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
                Component<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
                var color=Component<ColorAdjustments>(profile);color.contrast.Override(10);color.saturation.Override(-13);color.postExposure.Override(.24f);
                var wb=Component<WhiteBalance>(profile);wb.temperature.Override(-5);wb.tint.Override(-2);
                var lens=Component<LensDistortion>(profile);lens.intensity.Override(-.19f);lens.scale.Override(1.02f);
                var grain=Component<FilmGrain>(profile);grain.type.Override(FilmGrainLookup.Thin1);grain.intensity.Override(.16f);grain.response.Override(.65f);
                Component<ChromaticAberration>(profile).intensity.Override(.035f);
                var vignette=Component<Vignette>(profile);vignette.intensity.Override(.19f);vignette.smoothness.Override(.5f);vignette.rounded.Override(true);
                var blur=Component<MotionBlur>(profile);blur.intensity.Override(.10f);blur.clamp.Override(.025f);
                var bloom=Component<Bloom>(profile);bloom.threshold.Override(1.35f);bloom.intensity.Override(.09f);bloom.scatter.Override(.35f);
                if(style){style.Grading.sharedProfile=profile;style.BodyCamera=true;if(!style.GetComponent<BodyCameraStyle>())style.gameObject.AddComponent<BodyCameraStyle>();}
                Foliage(scene,map.id);
                Physics.SyncTransforms();var surface=geo.GetComponent<Unity.AI.Navigation.NavMeshSurface>();surface.BuildNavMesh();string navPath=Art+"/"+map.id+"_Navigation.asset";
                if(File.Exists(navPath))AssetDatabase.DeleteAsset(navPath);AssetDatabase.CreateAsset(surface.navMeshData,navPath);
                var routes=geo.GetComponentsInChildren<MapRoutePoint>();Vector3 origin=Vector3.zero;bool first=true;var routePath=new UnityEngine.AI.NavMeshPath();
                foreach(var route in routes)
                {
                    if(!UnityEngine.AI.NavMesh.SamplePosition(route.transform.position,out var hit,1.2f,UnityEngine.AI.NavMesh.AllAreas))throw new InvalidOperationException("Bodycam blocked route "+route.name);
                    if(first){origin=hit.position;first=false;}else if(!UnityEngine.AI.NavMesh.CalculatePath(origin,hit.position,UnityEngine.AI.NavMesh.AllAreas,routePath)||routePath.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)throw new InvalidOperationException("Bodycam disconnected route "+route.name);
                }
                Debug.Log("BODYCAM_ROUTES "+map.id+" connected="+routes.Length);
                EditorSceneManager.SaveScene(scene);ClassicProjectSetup.RefreshMapPreview(map);
            }
            finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
        }
        static void Foliage(Scene scene,string id)
        {
            var material=Material("Natural reed foliage","Universal Render Pipeline/Lit");material.SetColor("_BaseColor",new Color(.26f,.29f,.15f));material.SetFloat("_Smoothness",.1f);material.SetFloat("_Cull",0);
            int index=0;var random=new System.Random(1515);
            foreach(var root in scene.GetRootGameObjects())foreach(var volume in root.GetComponentsInChildren<GrassVolume>())
            {
                var bounds=volume.GetComponent<BoxCollider>();var vertices=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>();
                int count=Mathf.Clamp(Mathf.RoundToInt(bounds.size.x*bounds.size.z*20),200,2200);
                for(int i=0;i<count;i++)
                {
                    float x=((float)random.NextDouble()-.5f)*bounds.size.x,z=((float)random.NextDouble()-.5f)*bounds.size.z,h=(volume.ConcealsStanding?.7f:.35f)*(1+(float)random.NextDouble());
                    float angle=(float)random.NextDouble()*Mathf.PI*2,width=.022f+(float)random.NextDouble()*.027f;var sideways=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var bend=new Vector3(-sideways.z,0,sideways.x)*(.07f+(float)random.NextDouble()*.22f);int start=vertices.Count;
                    for(int j=0;j<4;j++)
                    {
                        float t=j/3f;var center=new Vector3(x,h*t,z)+bend*(t*t);float w=width*(1-t*.98f);
                        vertices.Add(center-sideways*w);vertices.Add(center+sideways*w);uv.Add(new Vector2(0,t));uv.Add(new Vector2(1,t));
                        if(j>0){int a=start+(j-1)*2;triangles.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
                    }
                }
                var mesh=new Mesh{name=id+" bent reed field",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                string path=Art+"/"+id+"_Reeds_"+index+++".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
                var filters=volume.GetComponentsInChildren<MeshFilter>();if(filters.Length>0)
                {
                    filters[0].sharedMesh=mesh;filters[0].transform.localPosition=Vector3.zero;filters[0].transform.localRotation=Quaternion.identity;filters[0].transform.localScale=Vector3.one;filters[0].GetComponent<Renderer>().sharedMaterial=material;
                    for(int k=1;k<filters.Length;k++)filters[k].gameObject.SetActive(false);
                }
            }
        }
    }
}
