using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace TacticalZero.Editor
{
    public static class ContactArt16
    {
        const string Art="Assets/TacticalZero/Art/Contact16";
        const string Prior="Assets/TacticalZero/Art/Bodycam15";
        [Serializable]class Document{public Surface[] materials;}
        [Serializable]class Surface{public string name;public float[] color;}
        static Material brick,wood,aggregate,steel,paint,glass,plaster;
        public static void Apply()
        {
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles(Art,"*.jpg"))
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(path);bool normal=path.Contains("nor_gl");
                t.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;t.sRGBTexture=!normal&&!path.Contains("rough");t.maxTextureSize=2048;t.anisoLevel=8;t.wrapMode=TextureWrapMode.Repeat;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
            }
            brick=Scan("Exposed brick plaster","wall_bricks_plaster",.72f,new Color(.85f,.84f,.78f));
            wood=Scan("Desaturated timber grain","wood_table_001",.68f,new Color(.67f,.69f,.62f));
            aggregate=Scan("Broken aggregate","concrete_debris",.55f,new Color(.9f,.9f,.85f));
            steel=Lit("Satin oxide steel",new Color(.18f,.20f,.20f),.62f,.38f);
            paint=Lit("Faded machinery enamel",new Color(.24f,.29f,.25f),.18f,.3f);
            glass=Lit("Smoked optical glass",new Color(.14f,.20f,.19f,.22f),.05f,.86f);
            glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;
            plaster=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Two tone service plaster.mat");
            if(!plaster){plaster=new Material(AssetDatabase.LoadAssetAtPath<Material>(Prior+"/Aged concrete.mat"));AssetDatabase.CreateAsset(plaster,Art+"/Two tone service plaster.mat");}
            plaster.SetFloat("_Weathering",.38f);plaster.SetFloat("_PaintBand",1);plaster.SetColor("_BandColor",new Color(.45f,.54f,.49f));EditorUtility.SetDirty(plaster);
            var doc=JsonUtility.FromJson<Document>(File.ReadAllText(Art+"/materials.json"));
            foreach(var file in Directory.GetFiles(Art,"*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(file);importer.importAnimation=importer.importCameras=importer.importLights=false;importer.isReadable=true;importer.importBlendShapes=true;
                foreach(var s in doc.materials)
                {
                    Material m=s.name.Contains("timber")?wood:s.name.Contains("steel")?steel:s.name.Contains("paint")?paint:s.name.Contains("concrete")?aggregate:s.name.Contains("glass")?glass:AssetDatabase.LoadAssetAtPath<Material>(Prior+"/"+s.name+".mat");
                    if(m)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),s.name),m);
                }
                importer.SaveAndReimport();
            }
            Weapons();
            foreach(var role in ClassicRules.Data.roles)
            {
                string path=ClassicArt.Root+"/Operators/"+role.id+".prefab";
                if(!File.Exists(path))continue;
                var root=PrefabUtility.LoadPrefabContents(path);try{foreach(var rig in root.GetComponentsInChildren<OperatorRig>())if(rig.refined&&!rig.GetComponent<FootContact16>())rig.gameObject.AddComponent<FootContact16>();PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(var map in ClassicRules.Data.maps)Map(map);
            ClassicProjectSetup.RefreshWeaponPreviews();AssetDatabase.SaveAssets();Debug.Log("CONTACT16: contact hands, planted locomotion, furnished architecture applied");
        }
        static Material Lit(string name,Color c,float metal,float smooth)
        {
            string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=c;m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        static Material Scan(string name,string source,float scale,Color tint)
        {
            string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("TacticalZero/Scanned Surface"));AssetDatabase.CreateAsset(m,path);}
            m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+source+"_diff.jpg"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+source+"_nor_gl.jpg"));m.SetTexture("_RoughMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+source+"_rough.jpg"));m.SetColor("_BaseColor",tint);m.SetFloat("_Scale",scale);m.SetFloat("_BumpScale",.45f);m.SetFloat("_Smoothness",.55f);m.SetFloat("_Weathering",.20f);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
        }
        static Transform Find(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        static Vector3 Center(Transform part,Transform root)
        {
            if(!part)return Vector3.zero;var renderers=part.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return part.localPosition;
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);return root.InverseTransformPoint(bounds.center);
        }
        static void Weapons()
        {
            // These four models predate the current importer. Recreate their transforms from
            // the corrected FBX rather than retaining the old serialized import transforms.
            foreach(string id in new[]{"P90","MG3","M700","M1014"})
            {
                var root=new GameObject(id+" contact firearm");Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TacticalZero/Art/FieldWeapons14/"+id+".fbx"),root.transform);
                var anim=root.AddComponent<WeaponReloadAnimation>();anim.WeaponId=id;anim.Magazine=Find(root,"Magazine");anim.Bolt=Find(root,"Bolt");anim.Belt=Find(root,"Belt");anim.Shell=Find(root,"Shell");
                var cover=Find(root,"Cover");if(cover){var hinge=new GameObject("Feed cover hinge").transform;hinge.SetParent(root.transform,false);hinge.localPosition=new Vector3(0,.067f,.085f);cover.SetParent(hinge,true);anim.Cover=hinge;}
                Transform Target(string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(root.transform,false);t.localPosition=p;return t;}
                anim.LeftHand=Target("Reload left hand",Vector3.zero);anim.RightHand=Target("Reload right hand",Vector3.zero);if(anim.Shell)anim.Shell.gameObject.SetActive(false);
                Target("Muzzle",new Vector3(0,id=="P90"?.047f:id=="MG3"?.009f:id=="M700"?.02f:.027f,id=="P90"?.235f:id=="MG3"?.71f:id=="M700"?.582f:.57f));
                PrefabUtility.SaveAsPrefabAsset(root,ClassicArt.Root+"/Models/"+id+".prefab");Object.DestroyImmediate(root);
            }
            foreach(var gun in ClassicRules.Data.weapons)
            {
                string path=ClassicArt.Root+"/Models/"+gun.id+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var anim=root.GetComponent<WeaponReloadAnimation>();if(!anim)continue;
                    foreach(var old in root.GetComponentsInChildren<FirstPersonArm>(true))Object.DestroyImmediate(old.gameObject);
                    foreach(var old in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Contact optical lens").ToArray())Object.DestroyImmediate(old.gameObject);
                    Vector3 left=new Vector3(-.047f,-.043f,.165f),right=new Vector3(.045f,-.077f,-.175f);
                    if(gun.id=="USP"){left=new Vector3(-.057f,-.097f,-.155f);right.z=-.179f;}
                    if(gun.id=="P90"){left=new Vector3(-.050f,-.061f,.052f);right=new Vector3(.047f,-.073f,-.115f);}
                    if(gun.id=="MP6"||gun.id=="VECTOR"){left.z=.105f;right.z=gun.id=="MP6"?-.19f:-.13f;}
                    if(gun.id=="MG3"){left.z=.235f;right.z=-.13f;}
                    if(gun.id=="M700"||gun.id=="AWP"){left.z=.15f;right.z=-.20f;}
                    if(gun.id=="AK47"||gun.id=="RPK")right.z=-.20f;
                    if(anim.LeftHand)anim.LeftHand.localPosition=left;if(anim.RightHand)anim.RightHand.localPosition=right;
                    anim.ContactGrip=true;anim.MagazineGrip=Center(anim.Magazine,root.transform)+new Vector3(-.05f,.035f,-.045f);anim.BoltGrip=Center(anim.Bolt,root.transform)+new Vector3(-.05f,-.035f,-.025f);
                    anim.BeltGrip=Center(anim.Belt,root.transform)+new Vector3(-.05f,-.035f,-.075f);
                    anim.ShellGrip=Center(anim.Shell,root.transform)+new Vector3(-.05f,-.035f,-.075f);
                    if(anim.Cover)anim.CoverGrip=anim.Cover.InverseTransformPoint(root.transform.TransformPoint(Center(anim.Cover,root.transform)+new Vector3(-.05f,-.035f,-.075f)));
                    foreach(string side in new[]{"L","R"})
                    {
                        var target=side=="L"?anim.LeftHand:anim.RightHand;if(!target)continue;
                        var arm=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Arm_"+side+".fbx"),root.transform);arm.name="Contact skeletal arm "+side;
                        var solver=arm.AddComponent<FirstPersonArm>();solver.Left=side=="L";solver.Upper=Find(arm,"upper."+side);solver.Lower=Find(arm,"lower."+side);solver.Hand=Find(arm,"hand."+side);solver.Target=target;
                        foreach(var r in arm.GetComponentsInChildren<SkinnedMeshRenderer>()){r.updateWhenOffscreen=true;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
                    }
                    foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())foreach(var m in renderer.sharedMaterials)
                    {
                        if(!m||m.shader.name!="Universal Render Pipeline/Lit")continue;
                        if(m.name.Contains("Gunmetal")||m.name.Contains("Parker")||m.name.Contains("black")||m.name.Contains("Black")){m.SetFloat("_BumpScale",.035f);m.SetFloat("_Smoothness",.32f);EditorUtility.SetDirty(m);}
                        if(m.name=="Blued steel"){m.color=new Color(.24f,.265f,.275f);m.SetFloat("_BumpScale",.025f);m.SetFloat("_Metallic",.62f);m.SetFloat("_Smoothness",.36f);EditorUtility.SetDirty(m);}
                        if(m.name=="Machined edges"){m.color=new Color(.25f,.275f,.28f);m.SetFloat("_BumpScale",.018f);m.SetFloat("_Smoothness",.42f);EditorUtility.SetDirty(m);}
                    }
                    if(gun.id=="M4A1"||gun.id=="VECTOR")
                    {
                        var lens=ClassicArt.Part("Contact optical lens",root.transform,new Vector3(0,.14f,-.03f),new Vector3(.042f,.065f,.002f),glass);lens.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        static GameObject Prop(string id,Transform root,Vector3 position,Vector3 rotation,Vector3? scale=null)
        {
            var o=new GameObject("Contact detail "+id);o.transform.SetParent(root,false);o.transform.localPosition=position;o.transform.localRotation=Quaternion.Euler(rotation);o.transform.localScale=scale??Vector3.one;
            Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/"+id+".fbx"),o.transform);
            foreach(var r in o.GetComponentsInChildren<Renderer>()){r.gameObject.layer=LayerMask.NameToLayer("TZWorld");GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic);}
            return o;
        }
        static GameObject Part(string n,Transform root,Vector3 p,Vector3 size,Material m)
        {
            var o=ClassicArt.Part(n,root,p,size,m);o.layer=LayerMask.NameToLayer("TZWorld");GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.BatchingStatic);return o;
        }
        static void Pipe(Transform root,Vector3 a,Vector3 b,float radius,Material m)
        {
            var o=Part("Galvanized conduit",root,(a+b)*.5f,new Vector3(radius*2,(a-b).magnitude*.5f,radius*2),m);o.GetComponent<MeshFilter>().sharedMesh=Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        }
        static void Map(ClassicMap map)
        {
            var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene("Assets/TacticalZero/Scenes/"+map.Scene+".unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var geo=scene.GetRootGameObjects().First(g=>g.name=="Static architecture");var old=geo.transform.Find("Contact architecture 16");if(old)Object.DestroyImmediate(old.gameObject);
                var detail=new GameObject("Contact architecture 16").transform;detail.SetParent(geo.transform,false);int roomIndex=0;
                foreach(var area in geo.GetComponentsInChildren<MapArea>().Where(a=>a.Interior))
                {
                    var room=new GameObject("Furnished "+area.Label).transform;room.SetParent(detail,false);room.SetPositionAndRotation(area.transform.position,area.transform.rotation);
                    float w=area.Size.x,d=area.Size.z;int index=roomIndex++;
                    foreach(var r in area.GetComponentsInChildren<MeshRenderer>())
                    {
                        if(r.GetComponent<TextMesh>())continue;
                        if(r.name.Contains("wall")||r.name.Contains("partition"))r.sharedMaterial=(index%3==0||map.id=="ruins")?brick:plaster;
                        if(r.name=="Desk cover")
                        {
                            r.enabled=false;var pos=r.transform.localPosition;Prop("Desk",room,new Vector3(pos.x,0,pos.z),Vector3.zero,new Vector3(1.2f,1.12f,1.3f));
                            Prop("Chair",room,new Vector3(pos.x+.14f,0,pos.z-.72f),new Vector3(0,170+index*3,0));
                        }
                        if(r.name=="Wall cabinet"){r.enabled=false;var p=r.transform.localPosition;Prop("Lockers",room,new Vector3(p.x,0,p.z),Vector3.zero,new Vector3(.66f,1.06f,1.15f));}
                        if(r.name=="Door threshold stripe")r.sharedMaterial=paint;
                    }
                    foreach(int side in new[]{-1,1})
                    {
                        float x=side*(w*.5f-.13f);
                        for(int k=0;k<2;k++)
                        {
                            float z=(k==0?-1:1)*d*.19f,length=d*.38f-.8f;
                            foreach(float y in new[]{1.09f,2.34f})Part("Recessed steel window frame",room,new Vector3(x,y,z),new Vector3(.07f,.055f,length),steel);
                            foreach(int end in new[]{-1,1})Part("Window reveal",room,new Vector3(x,1.72f,z+end*length*.5f),new Vector3(.07f,1.28f,.055f),steel);
                            Part("Window sill drip edge",room,new Vector3(x,1.07f,z),new Vector3(.38f,.035f,length+.12f),paint);
                            // Broken edge panes leave the existing traversable opening unobstructed.
                            Part("Remaining dusty window pane",room,new Vector3(x,2.10f,z-length*.43f),new Vector3(.008f,.44f,.21f),glass);
                        }
                        Pipe(room,new Vector3(x,2.72f,-d*.47f),new Vector3(x,2.72f,d*.47f),.016f,steel);
                        Pipe(room,new Vector3(x,.3f,d*.43f),new Vector3(x,2.72f,d*.43f),.016f,steel);
                        for(int k=0;k<5;k++)Part("Conduit clamp",room,new Vector3(x-side*.016f,2.72f,(k/4f-.5f)*d*.85f),new Vector3(.028f,.062f,.045f),steel);
                        Part("Wall skirting",room,new Vector3(x,.10f,0),new Vector3(.028f,.19f,d-.1f),paint);
                        Prop("Vent",room,new Vector3(x-side*.1f,2.6f,d*.30f),new Vector3(0,side*90,0));
                        var door=Prop("Door",room,new Vector3(side*1.63f,0,side*d*.5f+side*.3f),new Vector3(0,side*88,0));
                        var dc=door.AddComponent<BoxCollider>();dc.center=new Vector3(0,1.14f,0);dc.size=new Vector3(.92f,2.28f,.06f);door.layer=LayerMask.NameToLayer("TZWorld");
                        Prop("Lamp",room,new Vector3(w*.22f,3.3f,side*d*.26f),Vector3.zero);
                        Prop("Debris",room,new Vector3(side*(w*.5f-.64f),.02f,-d*.36f),new Vector3(0,index*51,0),Vector3.one*.65f);
                    }
                    // Suspended cable tray occupies the ceiling, away from navigation and sight lines.
                    for(int side=-1;side<=1;side+=2)Part("Overhead cable tray rail",room,new Vector3(w*.22f+side*.12f,3.15f,0),new Vector3(.023f,.06f,d*.90f),steel);
                    for(int k=0;k<18;k++)Part("Cable tray rung",room,new Vector3(w*.22f,3.13f,(k/17f-.5f)*d*.9f),new Vector3(.25f,.014f,.024f),steel);
                    for(int cable=0;cable<3;cable++)Pipe(room,new Vector3(w*.22f+(cable-1)*.045f,3.17f,-d*.45f),new Vector3(w*.22f+(cable-1)*.045f,3.17f,d*.45f),.008f,paint);
                }
                if(map.id!="outpost")for(int k=0;k<7;k++)
                {
                    var block=new GameObject("Distant industrial facade").transform;block.SetParent(detail,false);float angle=k*Mathf.PI*2/7;
                    block.localPosition=new Vector3(Mathf.Cos(angle)*70,-1,Mathf.Sin(angle)*58);block.localRotation=Quaternion.Euler(0,-k*360/7,0);float height=8+k%3*3;
                    Part("Brick factory mass",block,new Vector3(0,height*.5f,0),new Vector3(14,height,8),brick);
                    for(int row=0;row<(int)(height/2.4f);row++)for(int col=0;col<5;col++)Part("Factory window recess",block,new Vector3(col*2.4f-4.8f,1.5f+row*2.4f,-4.025f),new Vector3(1.35f,1.2f,.025f),steel);
                    Part("Roof cornice",block,new Vector3(0,height,0),new Vector3(14.3f,.16f,8.3f),paint);
                    Pipe(block,new Vector3(5,0,-4.15f),new Vector3(5,height,-4.15f),.085f,steel);
                }
                Physics.SyncTransforms();var surface=geo.GetComponent<Unity.AI.Navigation.NavMeshSurface>();surface.BuildNavMesh();string nav=Art+"/"+map.id+"_Navigation.asset";if(File.Exists(nav))AssetDatabase.DeleteAsset(nav);AssetDatabase.CreateAsset(surface.navMeshData,nav);
                var routes=geo.GetComponentsInChildren<MapRoutePoint>();var path=new NavMeshPath();Vector3 origin=Vector3.zero;bool first=true;
                foreach(var point in routes)
                {
                    if(!NavMesh.SamplePosition(point.transform.position,out var hit,1.2f,NavMesh.AllAreas))throw new InvalidOperationException("Contact16 missing route "+point.name);
                    if(first){origin=hit.position;first=false;}else if(!NavMesh.CalculatePath(origin,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new InvalidOperationException("Contact16 blocked route "+point.name);
                }
                Debug.Log("CONTACT16_ROUTES "+map.id+" connected="+routes.Length+" furnished="+roomIndex);EditorSceneManager.SaveScene(scene);ClassicProjectSetup.RefreshMapPreview(map);
            }
            finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
        }
    }
}
