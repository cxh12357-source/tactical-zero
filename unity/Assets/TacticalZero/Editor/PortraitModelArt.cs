using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TacticalZero.Editor
{
    // Portrait-led clothing volumes and silhouettes; all geometry is editable and persisted as assets.
    public static class PortraitModelArt
    {
        const string Root=ClassicArt.Root; static string owner;static int serial;
        static Material cloth,pants,armor,trim,skin,hair,leather,metal,light,cream;
        static GameObject P(string n,Transform t,Vector3 p,Vector3 s,Material m,int shape=0)=>shape==5?ArmorPanel(n,t,p,s,m):ClassicArt.Part(n,t,p,s,m,shape);
        static GameObject ArmorPanel(string name,Transform parent,Vector3 at,Vector3 size,Material mat)
        {
            const int columns=8,rows=8;var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int face=0;face<2;face++)for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                float u=x/(float)columns*2-1,v=y/(float)rows*2-1;
                float taper=1-Mathf.Max(0,Mathf.Abs(v)-.55f)*.55f;
                float z=face==0?size.z*(.48f-.25f*u*u-.13f*v*v):-size.z*.45f;
                vertices.Add(new Vector3(u*taper*size.x*.5f,v*size.y*.5f,z));
            }
            int offset=(columns+1)*(rows+1);
            for(int face=0;face<2;face++)for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
            {
                int a=face*offset+y*(columns+1)+x,b=a+columns+1;
                if(face==0)triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});else triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});
            }
            var perimeter=new List<int>();for(int x=0;x<=columns;x++)perimeter.Add(x);for(int y=1;y<=rows;y++)perimeter.Add(y*(columns+1)+columns);for(int x=columns-1;x>=0;x--)perimeter.Add(rows*(columns+1)+x);for(int y=rows-1;y>0;y--)perimeter.Add(y*(columns+1));
            for(int i=0;i<perimeter.Count;i++){int a=perimeter[i],b=perimeter[(i+1)%perimeter.Count];triangles.AddRange(new[]{a,a+offset,b,b,a+offset,b+offset});}
            var panel=MeshPart(name,parent,vertices,triangles,mat);panel.transform.localPosition=at;return panel;
        }
        static void Rod(string n,Transform t,Vector3 a,Vector3 b,float w,Material m)=>ClassicArt.Rod(n,t,a,b,w,m);
        static Material M(string name,string hex,float metallic=0)
        {
            ColorUtility.TryParseHtmlString("#"+hex,out var c);var m=ClassicArt.Mat("Portrait06_"+name,c,metallic,.28f);
            var texture=Resources.Load<Texture2D>("Classic/Textures/"+(metallic>.3f?"Brushed steel":"Woven textile"));
            if(texture&&!name.Contains("skin")&&!name.Contains("hair")){m.SetTexture("_BaseMap",texture);m.SetTextureScale("_BaseMap",Vector2.one*3);}
            return m;
        }
        public static void Apply()
        {
            EnsureSupportAssets();
            if(File.Exists(Root+"/PortraitsAndOptics06.txt"))return;
            Directory.CreateDirectory(Root+"/Meshes/Portrait06");AssetDatabase.Refresh();
            WeaponModelArt.Apply();
            foreach(var role in ClassicRules.Data.roles)ClassicArt.SavePrefab(Build(role),Root+"/Operators/"+role.id+".prefab");
            ClassicArt.SavePrefab(new GameObject("Holographic supply anchor"),Root+"/Models/FieldCache.prefab");
            File.WriteAllText(Root+"/PortraitsAndOptics06.txt","Portrait-led eight operator silhouettes, twelve weapons and collider-free supplies.");
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        public static void Refine()
        {
            ClassicArt.Init();EnsureSupportAssets();WeaponModelArt.Apply();
            foreach(var role in ClassicRules.Data.roles)ClassicArt.SavePrefab(Build(role),Root+"/Operators/"+role.id+".prefab");
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();ClassicProjectSetup.RefreshWeaponPreviews();ArtReview06.Render();
        }
        static void EnsureSupportAssets()
        {
            const string path=Root+"/Materials/SupplyHologram.mat";
            if(!AssetDatabase.LoadAssetAtPath<Material>(path))
            {
                var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="SupplyHologram",enableInstancing=true};
                material.SetColor("_BaseColor",Color.white);AssetDatabase.CreateAsset(material,path);
            }
        }
        public static GameObject MeshPart(string name,Transform parent,List<Vector3> vertices,List<int> indices,Material material)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);
            var uv=new List<Vector2>();foreach(var p in vertices)uv.Add(new Vector2(p.x*2+p.z,p.y*2));mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Root+"/Meshes/Portrait06/"+owner+"_"+(serial++)+".asset";
            var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(prior){EditorUtility.CopySerialized(mesh,prior);Object.DestroyImmediate(mesh);mesh=prior;}else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        // Sections are y, half-width, half-depth, z offset. Subtle folds are built into the cloth surface.
        static GameObject Loft(string name,Transform parent,Vector4[] rows,Material mat,float folds=0,int sides=20)
        {
            var v=new List<Vector3>();var tri=new List<int>();
            for(int j=0;j<rows.Length;j++)for(int i=0;i<=sides;i++)
            {
                float a=i*Mathf.PI*2/sides,f=1+folds*Mathf.Sin(i*2.7f+j*3.9f);var row=rows[j];v.Add(new Vector3(Mathf.Sin(a)*row.y*f,row.x,Mathf.Cos(a)*row.z*f+row.w));
            }
            for(int j=0;j<rows.Length-1;j++)for(int i=0;i<sides;i++){int a=j*(sides+1)+i,b=a+sides+1;tri.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            foreach(int row in new[]{0,rows.Length-1}){int n=v.Count;v.Add(new Vector3(0,rows[row].x,rows[row].w));for(int i=0;i<sides;i++){int a=row*(sides+1)+i;if(row==0)tri.AddRange(new[]{n,a+1,a});else tri.AddRange(new[]{n,a,a+1});}}
            return MeshPart(name,parent,v,tri,mat);
        }
        static Vector4 S(float y,float w,float d,float z=0)=>new Vector4(y,w,d,z);
        static void Ribbon(string name,Transform parent,Vector3[] path,float width,Material mat)
        {
            var v=new List<Vector3>();var tr=new List<int>();
            for(int i=0;i<path.Length;i++){v.Add(path[i]+Vector3.left*width*.5f);v.Add(path[i]+Vector3.right*width*.5f);if(i>0){int a=(i-1)*2;tr.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}}
            var m=MeshPart(name,parent,v,tr,mat);m.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        }
        static void Leaf(string name,Transform t,Vector3 a,Vector3 b,float width,Material mat)
        {
            Vector3 cross=Vector3.Cross((b-a).normalized,Vector3.forward);if(cross.sqrMagnitude<.1f)cross=Vector3.right;
            Vector3 middle=Vector3.Lerp(a,b,.45f);var v=new List<Vector3>{a,middle+cross*width*.5f,middle+Vector3.forward*.028f,middle-cross*width*.5f,b};
            MeshPart(name,t,v,new List<int>{0,1,2,0,2,3,1,4,2,2,4,3},mat);
        }
        static void Pouch(string name,Transform t,Vector3 at,Vector3 scale,Material mat)
        {
            P(name,t,at,scale,mat);P(name+" flap",t,at+new Vector3(0,scale.y*.31f,scale.z*.52f),new Vector3(scale.x*.93f,scale.y*.3f,.018f),mat,5);
            P(name+" snap",t,at+new Vector3(0,scale.y*.2f,scale.z*.57f),new Vector3(.019f,.022f,.012f),metal,1);
        }
        public static GameObject Build(ClassicRole role)
        {
            owner=role.id;serial=0;string id=role.id;bool heavy=id=="guardian",female=id=="medic"||id=="marksman",rolled=id=="assault"||id=="scout"||id=="engineer"||id=="supplier";
            string c=id=="medic"?"D5D1B5":id=="scout"?"37606A":id=="engineer"?"BA7929":id=="supplier"?"9B8A65":id=="infiltrator"?"414B32":"303238";
            string a=heavy?"394651":id=="assault"?"BB5622":id=="medic"?"6C8065":id=="marksman"?"64465E":id=="infiltrator"?"596044":id=="supplier"?"796042":id=="engineer"?"A77027":"425D5D";
            cloth=M(id+" fabric",c);pants=M(id+" trousers",id=="medic"||id=="infiltrator"?"444D3A":id=="supplier"?"74674E":"3A3A39");armor=M(id+" armor",a,heavy?.65f:.1f);trim=M(id+" webbing",id=="medic"?"68785C":"655844");
            skin=M(id+" skin",id=="marksman"?"CDA98D":id=="assault"||id=="medic"?"BD9373":"AF8158");hair=M(id+" hair",id=="assault"?"743D21":id=="marksman"?"B4B8BA":"282723");
            leather=M("leather","35302A");metal=M("buckles","7B817E",.7f);light=ClassicArt.Teal;cream=M("ivory","CFCEB8");
            var root=new GameObject("Portrait operator "+id);var t=root.transform;float width=heavy?.32f:female?.217f:id=="supplier"||id=="engineer"?.27f:.245f;
            Loft("Tailored jacket",t,new[]{S(.89f,width*.8f,.13f),S(.96f,width*.85f,.14f),S(1.05f,width*.77f,.135f),S(1.18f,width*.9f,.153f),S(1.36f,width,.165f),S(1.46f,width*.93f,.14f),S(1.51f,width*.5f,.10f)},cloth,.025f);
            if(id=="engineer"||id=="medic"||id=="scout")
            {
                foreach(int side in new[]{-1,1})
                {
                    var collar=P("Turned jacket collar",t,new Vector3(side*.075f,1.505f,.096f),new Vector3(.105f,.155f,.035f),cloth,5);collar.transform.localRotation=Quaternion.Euler(-18,0,side*22);
                    Ribbon("Tailored front seam",t,new[]{new Vector3(side*.026f,.99f,.15f),new Vector3(side*.025f,1.18f,.166f),new Vector3(side*.027f,1.35f,.177f),new Vector3(side*.065f,1.46f,.138f)},.008f,trim);
                    Pouch("Tailored chest pocket",t,new Vector3(side*.12f,1.34f,.163f),new Vector3(.113f,.115f,.028f),cloth);
                }
            }
            if(id=="medic")Loft("Field coat skirt",t,new[]{S(.77f,.265f,.17f),S(.82f,.25f,.16f),S(.94f,.21f,.14f),S(1.01f,.2f,.14f)},cloth,.055f);
            P("Utility belt",t,new Vector3(0,.96f,0),new Vector3(width*1.8f,.08f,.32f),leather);
            P("Belt clasp",t,new Vector3(0,.96f,.176f),new Vector3(.085f,.062f,.018f),metal);P("Belt clasp inset",t,new Vector3(0,.96f,.187f),new Vector3(.055f,.032f,.008f),leather);
            if(id!="engineer"&&id!="scout"&&id!="medic"&&id!="infiltrator")
            {
                P("Anatomical chest carrier",t,new Vector3(0,1.28f,.137f),new Vector3(width*1.6f,.39f,.095f),heavy?armor:leather,5);
                for(int i=0;i<(heavy?4:3);i++)Pouch("Chest magazine",t,new Vector3((i-(heavy?1.5f:1))*.105f,1.13f,.23f),new Vector3(.089f,.16f,.055f),trim);
                for(int i=0;i<3;i++)P("Chest stitched channel",t,new Vector3(0,1.32f+i*.043f,.204f),new Vector3(width*1.4f,.012f,.01f),trim);
            }
            foreach(int side in new[]{-1,1})
            {
                Ribbon("Contoured harness",t,new[]{new Vector3(side*width*.76f,1.02f,.16f),new Vector3(side*width*.72f,1.3f,.185f),new Vector3(side*width*.65f,1.48f,.1f),new Vector3(side*width*.65f,1.5f,-.12f),new Vector3(side*width*.7f,1.1f,-.2f)},.055f,trim);
                P("Harness buckle",t,new Vector3(side*width*.74f,1.39f,.198f),new Vector3(.065f,.043f,.022f),metal);
                Pouch("Belt utility",t,new Vector3(side*(width+.006f),.945f,.04f),new Vector3(.12f,.17f,.12f),trim);
                var leg=new GameObject(side<0?"LeftLeg":"RightLeg").transform;leg.SetParent(t,false);leg.localPosition=new Vector3(side*(heavy?.17f:.135f),.9f,0);
                Loft("Articulated trouser",leg,new[]{S(-.76f,.077f,.085f),S(-.68f,.086f,.09f),S(-.57f,.089f,.095f),S(-.47f,.079f,.088f,.015f),S(-.38f,.097f,.101f,.02f),S(-.28f,.12f,.118f),S(-.15f,.123f,.125f),S(.015f,.115f,.13f)},pants,.045f);
                Pouch("Thigh cargo",leg,new Vector3(side*.097f,-.21f,.09f),new Vector3(.115f,.19f,.07f),id=="medic"?armor:trim);
                P("Knee cup",leg,new Vector3(0,-.41f,.105f),new Vector3(.167f,.19f,.075f),heavy?armor:leather,5);
                foreach(float y in new[]{-.34f,-.47f})P("Knee fastening",leg,new Vector3(0,y,.01f),new Vector3(.195f,.026f,.208f),leather);
                if(heavy)P("Segmented shin shell",leg,new Vector3(0,-.625f,.083f),new Vector3(.18f,.27f,.092f),armor,5);
                Loft("Laced boot shaft",leg,new[]{S(-.88f,.095f,.105f,.028f),S(-.79f,.093f,.1f,.01f),S(-.66f,.085f,.087f),S(-.63f,.09f,.089f)},id=="medic"?cream:leather,.02f);
                P("Sculpted boot toe",leg,new Vector3(0,-.845f,.09f),new Vector3(.195f,.14f,.29f),id=="medic"?cream:leather,1);
                P("Boot outsole",leg,new Vector3(0,-.9f,.057f),new Vector3(.206f,.052f,.32f),leather);
                for(int k=0;k<5;k++){float y=-.82f+k*.034f;Rod("Crossed boot lace",leg,new Vector3(-.049f,y,.104f),new Vector3(.049f,y+.02f,.105f),.008f,trim);Rod("Crossed boot lace",leg,new Vector3(.049f,y,.105f),new Vector3(-.049f,y+.02f,.106f),.008f,trim);}
                for(int k=0;k<4;k++)P("Sole tread",leg,new Vector3(side*.104f,-.909f,-.07f+k*.07f),new Vector3(.025f,.032f,.039f),leather);
                var arm=new GameObject(side<0?"LeftArm":"RightArm").transform;arm.SetParent(t,false);arm.localPosition=new Vector3(side*(width+.036f),1.45f,0);
                Loft("Shaped jacket sleeve",arm,new[]{S(-.31f,.081f,.088f,.02f),S(-.27f,.09f,.097f,.012f),S(-.18f,.102f,.107f),S(-.06f,.106f,.111f),S(.025f,.075f,.083f),S(.055f,.028f,.035f)},cloth,.035f);
                if(rolled)Loft("Rolled sleeve cuff",arm,new[]{S(-.30f,.09f,.095f,.018f),S(-.27f,.101f,.105f,.018f),S(-.245f,.096f,.103f,.018f)},cloth,.03f);
                if(heavy||id=="assault")P("Layered shoulder armor",arm,new Vector3(side*.025f,-.065f,.055f),new Vector3(heavy?.3f:.245f,.27f,.19f),armor,5);
                var forearm=Loft("Tapered forearm",arm,new[]{S(0,.06f,.066f),S(.09f,.075f,.075f),S(.23f,.085f,.083f)},rolled?skin:cloth,.02f);
                forearm.transform.localPosition=new Vector3(-side*.065f,-.535f,.14f);forearm.transform.localRotation=Quaternion.Euler(-26,0,-side*13);
                if(heavy)P("Forearm gauntlet",arm,new Vector3(-side*.045f,-.41f,.18f),new Vector3(.16f,.24f,.11f),armor,5);
                P("Wrist strap",arm,new Vector3(-side*.09f,-.535f,.15f),new Vector3(.13f,.048f,.145f),leather);
                P("Anatomical glove",arm,new Vector3(-side*.11f,-.595f,.177f),new Vector3(.125f,.16f,.095f),leather,1);
                for(int f=0;f<4;f++){P("Glove finger",arm,new Vector3(-side*.11f-.043f+f*.028f,-.655f,.185f),new Vector3(.025f,.087f,.045f),leather,1);P("Knuckle pad",arm,new Vector3(-side*.11f-.043f+f*.028f,-.6f,.227f),new Vector3(.022f,.032f,.012f),trim,1);}
                P("Glove thumb",arm,new Vector3(-side*.11f-side*.065f,-.592f,.19f),new Vector3(.04f,.085f,.05f),leather,1);
            }
            Head(t,id,female);
            Accessories(t,id,width);
            // A lowered slung rifle preserves the portrait silhouette and avoids rigid floating hands.
            var gun=Object.Instantiate(Resources.Load<GameObject>("Classic/Models/"+(id=="marksman"?"AWP":heavy?"RPK":id=="scout"?"MP6":"M4A1")));
            if(gun){gun.name="Slung field weapon";gun.transform.SetParent(t,false);gun.transform.localPosition=new Vector3(.18f,.8f,.24f);gun.transform.localRotation=Quaternion.Euler(67,0,-14);gun.transform.localScale=Vector3.one*.75f;
                foreach(var child in gun.GetComponentsInChildren<Transform>())if(child.name.ToLowerInvariant().Contains("glove")||child.name.ToLowerInvariant().Contains("sleeve")||child.name.ToLowerInvariant().Contains("hand")||child.name.Contains("Finger"))child.gameObject.SetActive(false);}
            root.AddComponent<OperatorRig>();BakeRigidGroups(root);return root;
        }
        static void BakeRigidGroups(GameObject root)
        {
            var groups=new Dictionary<(Transform,Material),List<CombineInstance>>();
            var renderers=root.GetComponentsInChildren<MeshRenderer>();
            foreach(var renderer in renderers)
            {
                if(!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
                var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
                Transform bone=root.transform;
                for(var parent=renderer.transform;parent&&parent!=root.transform;parent=parent.parent)
                    if(parent.name=="LeftLeg"||parent.name=="RightLeg"||parent.name=="LeftArm"||parent.name=="RightArm"){bone=parent;break;}
                var key=(bone,renderer.sharedMaterial);if(!groups.TryGetValue(key,out var parts))groups[key]=parts=new List<CombineInstance>();
                parts.Add(new CombineInstance{mesh=filter.sharedMesh,transform=bone.worldToLocalMatrix*renderer.transform.localToWorldMatrix});
            }
            int batch=0;
            foreach(var group in groups)
            {
                var mesh=new Mesh{name=owner+" tailored surface "+batch};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(group.Value.ToArray(),true,true);
                string path=Root+"/Meshes/Portrait06/"+owner+"_batch_"+(batch++)+".asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(prior){EditorUtility.CopySerialized(mesh,prior);Object.DestroyImmediate(mesh);mesh=prior;}else AssetDatabase.CreateAsset(mesh,path);
                var go=new GameObject("Batched "+group.Key.Item2.name);go.transform.SetParent(group.Key.Item1,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=group.Key.Item2;
            }
            foreach(var renderer in renderers)
            {
                if(!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
                var filter=renderer.GetComponent<MeshFilter>();if(filter)Object.DestroyImmediate(filter);Object.DestroyImmediate(renderer);
            }
        }
        static void Head(Transform t,string id,bool female)
        {
            Loft("Neck anatomy",t,new[]{S(1.48f,.075f,.073f),S(1.61f,.062f,.07f)},skin);
            Loft("Sculpted face",t,new[]{S(1.595f,.05f,.07f,.023f),S(1.63f,.087f,.086f,.013f),S(1.685f,female?.103f:.113f,.106f),S(1.74f,.114f,.107f),S(1.805f,.108f,.103f,-.004f),S(1.855f,.087f,.087f,-.006f),S(1.877f,.038f,.04f)},skin,0,32);
            foreach(int side in new[]{-1,1})
            {
                P("Ear",t,new Vector3(side*.113f,1.726f,-.002f),new Vector3(.035f,.074f,.034f),skin,1);
                P("Eye socket",t,new Vector3(side*.047f,1.739f,.099f),new Vector3(.049f,.019f,.013f),M("eyelid shadow","765F4E"),1);
                P("Eye white",t,new Vector3(side*.047f,1.74f,.108f),new Vector3(.038f,.012f,.011f),cream,1);
                P("Iris",t,new Vector3(side*.047f,1.74f,.114f),new Vector3(.012f,.013f,.006f),hair,1);
                P("Upper eyelid",t,new Vector3(side*.047f,1.748f,.11f),new Vector3(.043f,.01f,.012f),skin,1);
                var brow=P("Eyebrow",t,new Vector3(side*.047f,1.764f,.105f),new Vector3(.052f,.011f,.017f),hair,1);brow.transform.localRotation=Quaternion.Euler(0,0,-side*8);
            }
            MeshPart("Faceted nose bridge",t,new List<Vector3>{new Vector3(-.015f,1.752f,.104f),new Vector3(.015f,1.752f,.104f),new Vector3(.022f,1.694f,.113f),new Vector3(0,1.704f,.143f),new Vector3(-.022f,1.694f,.113f)},new List<int>{0,3,1,1,3,2,2,3,4,4,3,0},skin);
            P("Mouth line",t,new Vector3(0,1.664f,.108f),new Vector3(.055f,.008f,.01f),M("lips","765044"),1);
            P("Lower lip",t,new Vector3(0,1.658f,.11f),new Vector3(.046f,.009f,.012f),skin,1);
            if(id=="guardian")
            {
                Loft("Sealed helmet",t,new[]{S(1.61f,.085f,.09f,.015f),S(1.65f,.123f,.123f),S(1.75f,.148f,.135f),S(1.86f,.13f,.122f),S(1.92f,.075f,.075f)},armor,0,12);
                P("Armored faceplate",t,new Vector3(0,1.704f,.13f),new Vector3(.21f,.18f,.068f),armor,5);
                P("Visor recess",t,new Vector3(0,1.773f,.135f),new Vector3(.226f,.034f,.035f),leather);
                P("Sealed cyan visor",t,new Vector3(0,1.775f,.158f),new Vector3(.18f,.009f,.009f),light);
                P("Helmet crown ridge",t,new Vector3(0,1.897f,-.009f),new Vector3(.08f,.035f,.18f),armor,5);
            }
            else
            {
                // Layered swept locks, rather than a spherical helmet used as hair.
                Loft("Contoured hair cap",t,new[]{S(1.793f,.118f,.113f,-.008f),S(1.84f,.111f,.105f,-.012f),S(1.879f,.083f,.076f,-.015f),S(1.903f,.018f,.02f,-.017f)},hair,.035f,24);
                for(int i=0;i<14;i++)
                {
                    float a=i*2.4f;var from=new Vector3(Mathf.Sin(a)*.086f,1.85f+(i%3)*.011f,Mathf.Cos(a)*.067f-.018f);
                    var to=new Vector3(from.x+.05f,1.80f-(i%4)*.026f,from.z+.07f);Leaf("Swept hair lock",t,from,to,.052f,hair);
                }
                if(id=="marksman")
                {
                    var beret=P("Purple beret",t,new Vector3(-.024f,1.897f,-.01f),new Vector3(.28f,.095f,.241f),armor,1);beret.transform.localRotation=Quaternion.Euler(0,0,12);
                    P("Beret leather band",t,new Vector3(0,1.863f,0),new Vector3(.237f,.024f,.213f),leather,1);
                    Leaf("Beret silver insignia",t,new Vector3(.028f,1.9f,.109f),new Vector3(.032f,1.874f,.122f),.03f,metal);
                    for(int i=0;i<5;i++)Leaf("Silver side fringe",t,new Vector3(-.067f+i*.018f,1.85f,.097f),new Vector3(-.116f+i*.01f,1.655f+i*.018f,.10f),.043f,hair);
                }
                if(id=="medic"||id=="engineer"||id=="supplier")
                {
                    var hatMat=id=="medic"?cream:id=="engineer"?armor:trim;
                    Loft(id=="engineer"?"Safety hardhat":id=="medic"?"Medical helmet":"Field cap",t,new[]{S(1.82f,.13f,.12f),S(1.86f,.139f,.126f),S(1.93f,.085f,.092f),S(1.945f,.025f,.03f)},hatMat,0,24);
                    P("Hat brim",t,new Vector3(0,1.828f,id=="supplier"?.112f:.01f),new Vector3(.29f,.018f,id=="supplier"?.19f:.27f),hatMat,1);
                    if(id=="medic")P("Medical helmet green stripe",t,new Vector3(0,1.942f,.015f),new Vector3(.067f,.013f,.115f),armor);
                    if(id=="engineer")foreach(int side in new[]{-1,1}){P("Goggle frame",t,new Vector3(side*.065f,1.887f,.11f),new Vector3(.112f,.073f,.04f),leather,1);P("Goggle glass",t,new Vector3(side*.065f,1.887f,.134f),new Vector3(.079f,.051f,.01f),ClassicArt.Glass,1);}
                }
                if(id=="engineer"||id=="supplier")
                {
                    for(int i=0;i<11;i++){float x=-.086f+i*.017f;Leaf("Sculpted beard lock",t,new Vector3(x,1.685f,.09f),new Vector3(x*.75f,1.597f+Mathf.Abs(x)*.4f,.103f),.04f,hair);}
                    P("Moustache",t,new Vector3(0,1.682f,.119f),new Vector3(.073f,.017f,.023f),hair,1);
                }
            }
            if(id=="scout")
            {
                P("Recon monocular rim",t,new Vector3(-.065f,1.744f,.132f),new Vector3(.071f,.076f,.073f),leather,1);
                P("Recon cyan monocular",t,new Vector3(-.065f,1.744f,.172f),new Vector3(.049f,.053f,.015f),light,1);
            }
            Rod("Headset boom",t,new Vector3(.122f,1.73f,0),new Vector3(.075f,1.67f,.132f),.008f,metal);
            P("Microphone",t,new Vector3(.07f,1.67f,.143f),new Vector3(.03f,.015f,.02f),leather);
        }
        static void Accessories(Transform t,string id,float width)
        {
            if(id=="assault"||id=="supplier"||id=="scout")
            {
                Loft("Wrapped scarf",t,new[]{S(1.455f,.105f,.10f,.024f),S(1.50f,.16f,.138f,.004f),S(1.555f,.145f,.127f),S(1.594f,.104f,.10f)},id=="scout"?cloth:trim,.08f);
                Leaf("Scarf drape",t,new Vector3(-.085f,1.53f,.13f),new Vector3(.04f,1.35f,.19f),.18f,trim);
            }
            if(id=="marksman"||id=="infiltrator")
            {
                var cloak=id=="marksman"?armor:cloth;cloak.SetFloat("_Cull",0);
                var v=new List<Vector3>();var indices=new List<int>();
                for(int j=0;j<7;j++)for(int i=0;i<13;i++)
                {
                    float u=i/12f,f=j/6f,a=(id=="marksman"?.05f:-.8f)+u*(id=="marksman"?1.85f:3.7f);
                    float radius=.20f+f*.21f,y=1.52f-f*(id=="marksman"?.83f:1.05f)+((j==6)?Mathf.Sin(i*5)*.095f:0);
                    v.Add(new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*radius*-1+.014f*Mathf.Sin(i*3+f*5)));
                    if(j<6&&i<12){int k=j*13+i;indices.AddRange(new[]{k,k+13,k+1,k+1,k+13,k+14});}
                }
                MeshPart(id=="marksman"?"Draped purple half cape":"Tattered camouflage mantle",t,v,indices,cloak);
                if(id=="infiltrator")
                {
                    var hood=new List<Vector3>();var tr=new List<int>();
                    for(int j=0;j<6;j++)for(int i=0;i<19;i++)
                    {
                        float f=j/5f,a=.7f+i/18f*(Mathf.PI*2-1.4f),rad=.17f*(1-f*.75f);
                        hood.Add(new Vector3(Mathf.Sin(a)*rad,1.59f+f*.39f,Mathf.Cos(a)*rad-.025f));
                        if(j<5&&i<18){int k=j*19+i;tr.AddRange(new[]{k,k+19,k+1,k+1,k+19,k+20});}
                    }
                    MeshPart("Pointed open face hood",t,hood,tr,cloak);
                    P("Olive face wrap",t,new Vector3(0,1.68f,.124f),new Vector3(.206f,.11f,.062f),cloth,5);
                    for(int j=0;j<5;j++)for(int i=0;i<5;i++)Leaf("Camouflage fabric leaf",t,new Vector3(-.26f+i*.11f,1.49f-j*.15f,.175f),new Vector3(-.28f+i*.12f,1.29f-j*.15f,.205f),.12f,(i+j)%2==0?armor:trim);
                    Rod("Sheathed knife",t,new Vector3(-.15f,.86f,.23f),new Vector3(.15f,1.16f,.24f),.03f,leather);
                }
                else Leaf("Cape silver emblem",t,new Vector3(.245f,1.415f,.09f),new Vector3(.29f,1.33f,.075f),.065f,metal);
            }
            float bagH=id=="supplier"?.96f:id=="medic"?.62f:.43f,bagW=id=="supplier"?.55f:.36f;
            Pouch("Field backpack",t,new Vector3(0,id=="supplier"?1.38f:1.25f,-.285f),new Vector3(bagW,bagH,.22f),id=="medic"?cream:trim);
            foreach(int side in new[]{-1,1})P("Pack compression strap",t,new Vector3(side*bagW*.32f,1.3f,-.409f),new Vector3(.028f,bagH*.9f,.018f),leather);
            if(id=="guardian")
            {
                P("Back mounted ballistic shield",t,new Vector3(0,1.27f,-.43f),new Vector3(.66f,1.2f,.10f),armor,5);
                for(int i=0;i<5;i++)P("Shield stiffener",t,new Vector3(0,.82f+i*.22f,-.497f),new Vector3(.6f,.035f,.029f),metal);
                Loft("Armored gorget",t,new[]{S(1.42f,.26f,.18f),S(1.5f,.2f,.17f),S(1.59f,.17f,.13f)},armor,0,12);
            }
            if(id=="supplier")
            {
                var roll=P("Rolled sleeping mat",t,new Vector3(0,1.93f,-.28f),new Vector3(.55f,.21f,.21f),trim,2);roll.transform.localRotation=Quaternion.Euler(0,0,90);
                for(int i=0;i<3;i++)Pouch("External ammunition case",t,new Vector3(.34f,1.07f+i*.26f,-.28f),new Vector3(.2f,.22f,.23f),armor);
                foreach(float x in new[]{-.18f,.18f})P("Bedroll strap",t,new Vector3(x,1.943f,-.28f),new Vector3(.036f,.22f,.225f),leather);
            }
            if(id=="scout")
            {
                foreach(int side in new[]{-1,1})
                {
                    Rod("Recon antenna",t,new Vector3(side*.13f,1.42f,-.31f),new Vector3(side*.15f,side<0?2.04f:1.97f,-.31f),.013f,metal);
                    Rod("Chest binocular tube",t,new Vector3(side*.047f,1.25f,.2f),new Vector3(side*.047f,1.11f,.22f),.066f,leather);
                }
            }
            if(id=="medic")
            {
                foreach(int side in new[]{-1,1})for(int i=0;i<3;i++){Rod("Medical vial",t,new Vector3(side*.105f+(i-1)*.025f,1.17f,.196f),new Vector3(side*.105f+(i-1)*.025f,1.27f,.196f),.017f,cream);}
                // Original pulse emblem, not a text label or protected red-cross emblem.
                var line=new[]{new Vector3(-.08f,1.36f,.178f),new Vector3(-.035f,1.36f,.184f),new Vector3(-.018f,1.40f,.186f),new Vector3(.015f,1.32f,.186f),new Vector3(.042f,1.36f,.184f),new Vector3(.08f,1.36f,.178f)};
                for(int i=0;i<line.Length-1;i++)Rod("Medical pulse emblem",t,line[i],line[i+1],.013f,armor);
            }
            if(id=="engineer")
            {
                Rod("Pack wrench shaft",t,new Vector3(.24f,1.2f,-.31f),new Vector3(.31f,1.93f,-.31f),.042f,metal);
                foreach(int side in new[]{-1,1})Rod("Open wrench jaw",t,new Vector3(.31f,1.92f,-.31f),new Vector3(.31f+side*.056f,2.015f,-.31f),.035f,metal);
                for(int i=0;i<3;i++)Pouch("Tool belt pocket",t,new Vector3(-.15f+i*.15f,.91f,.197f),new Vector3(.12f,.2f,.065f),trim);
                Rod("Belt pliers",t,new Vector3(.18f,1.01f,.24f),new Vector3(.22f,.82f,.24f),.025f,metal);
            }
        }
    }
}
