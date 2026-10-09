using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TacticalZero.Editor
{
    // Original modular meshes. Shared geometry/materials; bevels catch light without textures.
    public static class ClassicArt
    {
        public const string Root="Assets/TacticalZero/Resources/Classic";
        static Mesh bevel, sphere, cylinder, rock, cone,plate;
        public static Material Steel, Rubber, Cloth, Skin, Wood, White, Glass, Gold, Teal;
        public static Material Mat(string name,Color color,float metallic=0,float smooth=.3f,bool emission=false)
        {
            string path=Root+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);
            if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*2);}
            AssetDatabase.CreateAsset(m,path);return m;
        }
        public static void Init()
        {
            System.IO.Directory.CreateDirectory(Root+"/Materials");System.IO.Directory.CreateDirectory(Root+"/Meshes");
            AssetDatabase.Refresh();
            Steel=Mat("Gunmetal",new Color(.14f,.19f,.23f),.75f,.48f);Rubber=Mat("Rubber",new Color(.045f,.06f,.075f),.05f,.16f);
            Cloth=Mat("Undersuit",new Color(.13f,.17f,.18f));Skin=Mat("Skin",new Color(.64f,.43f,.3f),0,.25f);
            Wood=Mat("Walnut",new Color(.38f,.16f,.07f));White=Mat("Ceramic",new Color(.78f,.83f,.8f),.3f,.4f);
            Glass=Mat("OpticGlass",new Color(.06f,.5f,.6f),.8f,.9f);Gold=Mat("GlowAmber",new Color(1,.48f,.11f),.3f,.6f,true);Teal=Mat("GlowTeal",new Color(.12f,.82f,.75f),.3f,.6f,true);
            bevel=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/Bevel.asset");
            if(!bevel){bevel=BevelMesh();AssetDatabase.CreateAsset(bevel,Root+"/Meshes/Bevel.asset");}
            var temp=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
            temp=GameObject.CreatePrimitive(PrimitiveType.Cylinder);cylinder=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
            rock=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/Cliff.asset");if(!rock){rock=RadialMesh(false);AssetDatabase.CreateAsset(rock,Root+"/Meshes/Cliff.asset");}
            cone=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/Pine.asset");if(!cone){cone=RadialMesh(true);AssetDatabase.CreateAsset(cone,Root+"/Meshes/Pine.asset");}
            plate=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/ArmorPlate.asset");if(!plate){plate=PlateMesh();AssetDatabase.CreateAsset(plate,Root+"/Meshes/ArmorPlate.asset");}
            if(!File.Exists(Root+"/Meshes/GrassBlade.asset"))AssetDatabase.CreateAsset(GrassMesh(),Root+"/Meshes/GrassBlade.asset");
        }
        static Mesh BevelMesh()
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            Vector3[] normals={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            const int n=6;
            foreach(var normal in normals)
            {
                Vector3 a=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up,b=Vector3.Cross(normal,a);int start=v.Count;
                for(int y=0;y<=n;y++)for(int x=0;x<=n;x++)
                {
                    Vector3 p=normal*.5f+a*((float)x/n-.5f)+b*((float)y/n-.5f);
                    Vector3 core=new Vector3(Mathf.Clamp(p.x,-.40f,.40f),Mathf.Clamp(p.y,-.40f,.40f),Mathf.Clamp(p.z,-.40f,.40f));
                    v.Add(core+(p-core).normalized*.1f);uv.Add(new Vector2((float)x/n,(float)y/n));
                }
                for(int y=0;y<n;y++)for(int x=0;x<n;x++){int i=start+y*(n+1)+x;triangles.AddRange(new[]{i,i+1,i+n+2,i,i+n+2,i+n+1});}
            }
            var mesh=new Mesh{name="Soft chamfer box"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Mesh RadialMesh(bool tree)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();int sides=tree?9:10;
            var rings=new List<Vector3[]>();
            for(int row=0;row<4;row++)
            {
                var ring=new Vector3[sides];float y=-.5f+row/3f;
                for(int i=0;i<sides;i++){float angle=i*Mathf.PI*2/sides;float radius=tree?(row==3?.025f:.5f*(1-row/3f)):(row==0?.48f:row==3?.27f:.5f)*(1+Mathf.Sin(i*31+row*7)*.15f);ring[i]=new Vector3(Mathf.Cos(angle)*radius,y+(tree||row==0?0:Mathf.Sin(i*13+row)*.07f),Mathf.Sin(angle)*radius);}
                rings.Add(ring);
            }
            for(int r=0;r<3;r++)for(int i=0;i<sides;i++)
            {
                int next=(i+1)%sides;Vector3[] face={rings[r][i],rings[r+1][i],rings[r+1][next],rings[r][i],rings[r+1][next],rings[r][next]};
                foreach(var p in face){triangles.Add(vertices.Count);vertices.Add(p);}
            }
            for(int i=0;i<sides;i++){foreach(var p in new[]{Vector3.up*.5f,rings[3][(i+1)%sides],rings[3][i]}){triangles.Add(vertices.Count);vertices.Add(p);}}
            var mesh=new Mesh{name=tree?"Pine layered cone":"Fractured cliff mesh"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Rock=>rock;
        public static Mesh Pine=>cone;
        static Mesh GrassMesh()
        {
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int blade=0;blade<6;blade++)
            {
                float angle=blade*Mathf.PI/3;Vector3 side=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));Vector3 lean=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*.3f;
                Vector3 a=-Vector3.up*.5f-side*.17f,b=-Vector3.up*.5f+side*.17f,c=Vector3.up*(.24f+(blade%3)*.13f)+lean;
                foreach(var p in new[]{a,b,c,c,b,a}){indices.Add(vertices.Count);vertices.Add(p);}
            }
            var mesh=new Mesh{name="Six tapered grass blades"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Mesh PlateMesh()
        {
            Vector2[] outline={new Vector2(-.32f,.5f),new Vector2(.32f,.5f),new Vector2(.5f,.26f),new Vector2(.45f,-.28f),new Vector2(.27f,-.5f),new Vector2(-.27f,-.5f),new Vector2(-.45f,-.28f),new Vector2(-.5f,.26f)};
            var vertices=new List<Vector3>();var indices=new List<int>();
            void Triangle(Vector3 a,Vector3 b,Vector3 c){int first=vertices.Count;vertices.AddRange(new[]{a,b,c});indices.AddRange(new[]{first,first+1,first+2});}
            for(int i=0;i<8;i++)
            {
                int j=(i+1)%8;Vector3 a=new Vector3(outline[i].x,outline[i].y,.27f),b=new Vector3(outline[j].x,outline[j].y,.27f),backA=new Vector3(outline[i].x*.91f,outline[i].y*.91f,-.5f),backB=new Vector3(outline[j].x*.91f,outline[j].y*.91f,-.5f);
                Triangle(new Vector3(0,0,.5f),b,a);Triangle(a,b,backB);Triangle(a,backB,backA);Triangle(new Vector3(0,0,-.5f),backA,backB);
            }
            var mesh=new Mesh{name="Contoured ballistic plate"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static GameObject Part(string name,Transform parent,Vector3 at,Vector3 scale,Material material,int shape=0)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=shape==1?sphere:shape==2?cylinder:shape==3?rock:shape==4?cone:shape==5?plate:bevel;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;
            return go;
        }
        public static void Rod(string name,Transform parent,Vector3 from,Vector3 to,float width,Material mat)
        {
            var go=Part(name,parent,(from+to)*.5f,new Vector3(width,(to-from).magnitude*.5f,width),mat,2);go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,to-from);
        }
        public static void Label(string text,Transform parent,Vector3 at,float size,Color color,Quaternion? rotation=null)
        {
            var go=new GameObject("Sign "+text);go.transform.SetParent(parent,false);go.transform.localPosition=at;
            go.transform.localRotation=rotation??Quaternion.identity;
            var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=60;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.color=color;
        }
        public static GameObject Operator(ClassicRole role)
        {
            var root=new GameObject("Operator "+role.id);var t=root.transform;bool heavy=role.id=="guardian";
            Material armor=Mat("Armor_"+role.id,ClassicRules.ColorOf(role.color)*.6f,.35f,.35f);
            Material accent=Mat("Accent_"+role.id,ClassicRules.ColorOf(role.color),.3f,.5f);
            float w=heavy?.64f:.47f;
            Part("Torso fabric",t,new Vector3(0,1.23f,0),new Vector3(w,.62f,.3f),Cloth,1);
            Part("Plate carrier",t,new Vector3(0,1.25f,.16f),new Vector3(w*.91f,.4f,.105f),armor,5);
            Part("Sternum plate",t,new Vector3(0,1.42f,.16f),new Vector3(w*.7f,.14f,.12f),accent,5);
            Part("Back plate",t,new Vector3(0,1.25f,-.16f),new Vector3(w*.9f,.48f,.13f),armor);
            Part("Utility belt",t,new Vector3(0,.95f,0),new Vector3(w,.1f,.37f),Rubber);
            Part("Pelvis",t,new Vector3(0,.87f,0),new Vector3(w*.8f,.21f,.3f),Cloth);
            for(int side=-1;side<=1;side+=2)
            {
                Part("Carrier strap",t,new Vector3(side*w*.36f,1.44f,0),new Vector3(.075f,.17f,.39f),armor);
                Part("Buckle",t,new Vector3(side*w*.36f,1.37f,.225f),new Vector3(.052f,.065f,.025f),White);
                for(int k=0;k<3;k++)Part("MOLLE webbing",t,new Vector3(side*.12f,1.3f-k*.07f,.229f),new Vector3(.17f,.022f,.019f),Rubber);
                Part("Magazine pouch",t,new Vector3(side*.115f,1.03f,.22f),new Vector3(.1f,.17f,.09f),armor);
                var leg=new GameObject(side<0?"LeftLeg":"RightLeg").transform;leg.SetParent(t,false);leg.localPosition=new Vector3(side*.14f,.91f,0);
                Part("Combat trouser",leg,new Vector3(0,-.22f,0),new Vector3(.225f,.47f,.26f),Cloth,1);
                Part("Thigh pocket",leg,new Vector3(side*.1f,-.2f,.04f),new Vector3(.1f,.18f,.16f),armor);
                Part("Shin",leg,new Vector3(0,-.57f,.02f),new Vector3(.17f,.36f,.2f),Cloth,1);
                Part("Knee pad",leg,new Vector3(0,-.4f,.13f),new Vector3(.18f,.2f,.095f),armor,5);
                for(int k=0;k<2;k++)Part("Knee strap",leg,new Vector3(0,-.35f-k*.1f,.02f),new Vector3(.195f,.035f,.23f),Rubber);
                Part("Boot",leg,new Vector3(0,-.82f,.06f),new Vector3(.22f,.19f,.35f),Rubber);
                for(int k=0;k<3;k++)Part("Boot lace",leg,new Vector3(0,-.76f,.12f+k*.04f),new Vector3(.12f,.025f,.015f),White);
                var arm=new GameObject(side<0?"LeftArm":"RightArm").transform;arm.SetParent(t,false);arm.localPosition=new Vector3(side*(w*.5f+.055f),1.46f,0);
                Part("Upper sleeve",arm,new Vector3(0,-.16f,.035f),new Vector3(.2f,.38f,.24f),armor,1);
                Part("Shoulder armor",arm,new Vector3(side*.04f,-.06f,0),new Vector3(heavy?.3f:.23f,.22f,.28f),armor,1);
                Part("Shoulder insignia",arm,new Vector3(side*.04f,-.075f,.135f),new Vector3(.12f,.10f,.025f),accent,5);
                Part("Elbow",arm,new Vector3(0,-.3f,.07f),new Vector3(.18f,.16f,.18f),Rubber,1);
                Rod("Forearm",arm,new Vector3(0,-.3f,.08f),new Vector3(-side*.18f,-.28f,.38f),.13f,armor);
                Part("Glove",arm,new Vector3(-side*.18f,-.28f,.39f),new Vector3(.13f,.14f,.16f),Rubber);
                for(int finger=0;finger<4;finger++)Part("Glove knuckle",arm,new Vector3(-side*.18f-.043f+finger*.029f,-.245f,.46f),new Vector3(.023f,.04f,.025f),armor,1);
                Part("Wrist cuff",arm,new Vector3(-side*.14f,-.285f,.30f),new Vector3(.145f,.13f,.055f),Cloth);
                Part("Radio pouch",t,new Vector3(side*.25f,1.0f,-.06f),new Vector3(.1f,.17f,.15f),armor);
            }
            Part("Neck",t,new Vector3(0,1.56f,0),new Vector3(.14f,.16f,.14f),Skin,1);
            Part("Head",t,new Vector3(0,1.72f,.015f),new Vector3(.27f,.34f,.28f),Skin,1);
            Part("Jaw",t,new Vector3(0,1.62f,.05f),new Vector3(.2f,.13f,.21f),Skin);
            Part("Nose",t,new Vector3(0,1.72f,.155f),new Vector3(.06f,.085f,.07f),Skin);
            foreach(int side in new[]{-1,1})
            {
                Part("Cheek",t,new Vector3(side*.073f,1.69f,.105f),new Vector3(.09f,.075f,.09f),Skin,1);
                Part("Temple strap",t,new Vector3(side*.132f,1.755f,.04f),new Vector3(.025f,.045f,.23f),Rubber);
                Part("Helmet retention",t,new Vector3(side*.123f,1.655f,.035f),new Vector3(.025f,.12f,.1f),Cloth);
            }
            Part("Lip",t,new Vector3(0,1.647f,.164f),new Vector3(.09f,.018f,.018f),Wood);
            Part("Neck gaiter",t,new Vector3(0,1.56f,0),new Vector3(.29f,.15f,.3f),Cloth);
            if(role.id=="infiltrator"||role.id=="marksman")
            {
                Part("Hood",t,new Vector3(0,1.77f,-.05f),new Vector3(.34f,.36f,.26f),armor,1);
                Part("Face mask",t,new Vector3(0,1.66f,.14f),new Vector3(.24f,.16f,.08f),Rubber);
                Part("Cloak",t,new Vector3(-.15f,1.05f,-.26f),new Vector3(.5f,.9f,.1f),armor);
            }
            else if(role.id=="scout")
            {
                Part("Hair",t,new Vector3(0,1.88f,-.025f),new Vector3(.29f,.14f,.28f),Rubber,1);
                Part("Monocular",t,new Vector3(.105f,1.76f,.17f),new Vector3(.085f,.085f,.06f),Glass,1);
            }
            else
            {
                Part("Helmet shell",t,new Vector3(0,1.84f,-.015f),new Vector3(.34f,.23f,.36f),armor,1);
                Part("Helmet rim",t,new Vector3(0,1.79f,.01f),new Vector3(.36f,.045f,.37f),Rubber);
                Part("Night visor",t,new Vector3(0,1.755f,.167f),new Vector3(.25f,.068f,.035f),Glass);
            }
            foreach(int side in new[]{-1,1})Part("Headset",t,new Vector3(side*.16f,1.74f,0),new Vector3(.06f,.16f,.13f),Rubber);
            Part("Backpack",t,new Vector3(0,1.2f,-.31f),new Vector3(role.id=="supplier"?.6f:.32f,role.id=="supplier"?.6f:.42f,.22f),armor);
            if(role.id=="scout"||role.id=="engineer")
            {
                Rod("Radio antenna",t,new Vector3(.12f,1.4f,-.35f),new Vector3(.16f,2.2f,-.35f),.019f,Steel);
                Part("Sensor",t,new Vector3(-.26f,1.49f,.03f),new Vector3(.12f,.12f,.1f),Teal,1);
            }
            if(role.id=="medic")
            {
                Part("Medic cross horizontal",t,new Vector3(0,1.29f,.24f),new Vector3(.19f,.05f,.02f),White);
                Part("Medic cross vertical",t,new Vector3(0,1.29f,.25f),new Vector3(.05f,.19f,.02f),White);
            }
            if(heavy)Part("Ballistic shoulder shield",t,new Vector3(-.43f,1.13f,.1f),new Vector3(.1f,.55f,.4f),armor);
            if(role.id=="engineer")for(int i=0;i<3;i++)Rod("Tool",t,new Vector3(.3f,.9f,-.1f+i*.07f),new Vector3(.3f,1.16f,-.1f+i*.07f),.035f,White);
            var rifle=Gun(ClassicRules.Gun(role.id=="marksman"?"DMR":role.id=="guardian"?"RPK":role.id=="scout"?"MP6":"M4A1"),false);
            rifle.name="Holstered preview weapon";rifle.transform.SetParent(t,false);rifle.transform.localPosition=new Vector3(.09f,1.18f,.3f);rifle.transform.localRotation=Quaternion.Euler(0,-12,0);rifle.transform.localScale=Vector3.one*.8f;
            root.AddComponent<OperatorRig>();return root;
        }
        public static GameObject Gun(ClassicGun gun,bool hands=true)
        {
            var root=new GameObject(gun.label);var t=root.transform;bool pistol=gun.id=="USP",longGun=gun.kind=="dmr"||gun.kind=="rail",shotgun=gun.kind=="shotgun";
            var accent=Mat("GunAccent_"+gun.id,ClassicRules.ColorOf(gun.color)*.65f,.55f,.5f);
            float length=pistol?.3f:longGun?.8f:.61f;
            Part("Receiver",t,new Vector3(0,0,.08f),new Vector3(pistol?.065f:.095f,.09f,length*.47f),Steel);
            Part("Upper receiver",t,new Vector3(0,.05f,.1f),new Vector3(.068f,.035f,length*.58f),accent);
            Part("Grip",t,new Vector3(0,-.105f,-.045f),new Vector3(.06f,.19f,.08f),Rubber).transform.localRotation=Quaternion.Euler(-14,0,0);
            Rod("Barrel",t,new Vector3(0,.035f,.15f),new Vector3(0,.035f,length*.8f),pistol?.027f:.04f,Steel);
            Rod("Muzzle brake",t,new Vector3(0,.035f,length*.76f),new Vector3(0,.035f,length*.86f),.054f,Rubber);
            Part("Muzzle bore",t,new Vector3(0,.035f,length*.87f),new Vector3(.024f,.024f,.01f),Rubber,1);
            Part("Trigger guard",t,new Vector3(0,-.055f,.04f),new Vector3(.025f,.01f,.1f),Steel);
            Part("Trigger",t,new Vector3(0,-.029f,.04f),new Vector3(.014f,.05f,.012f),Gold);
            Part("Ejection port",t,new Vector3(.049f,.019f,.06f),new Vector3(.012f,.035f,.08f),Rubber);
            if(!pistol)
            {
                Part("Buttstock",t,new Vector3(0,-.018f,-.19f),new Vector3(.09f,.14f,.23f),gun.id=="AK47"?Wood:accent);
                Part("Shoulder pad",t,new Vector3(0,-.02f,-.31f),new Vector3(.1f,.16f,.025f),Rubber);
                Part("Handguard",t,new Vector3(0,.009f,.25f),new Vector3(.092f,.12f,.23f),gun.id=="AK47"?Wood:accent);
                for(int i=0;i<6;i++) {Part("Cooling vent",t,new Vector3(.049f,.035f,.16f+i*.033f),new Vector3(.008f,.034f,.014f),Rubber);Part("Top rail tooth",t,new Vector3(0,.079f,.1f+i*.029f),new Vector3(.063f,.016f,.012f),Steel);}
                Part("Magazine",t,new Vector3(0,-.14f,.11f),new Vector3(.068f,gun.kind=="lmg"?.23f:.21f,.105f),Rubber).transform.localRotation=Quaternion.Euler(gun.id=="AK47"?-17:0,0,0);
                for(int i=0;i<3;i++)Part("Magazine rib",t,new Vector3(.036f,-.065f-i*.056f,.11f),new Vector3(.008f,.011f,.088f),Steel);
                if(longGun)
                {
                    Rod("Scope tube",t,new Vector3(0,.14f,-.04f),new Vector3(0,.14f,.25f),.07f,Steel);
                    Rod("Scope objective",t,new Vector3(0,.14f,.2f),new Vector3(0,.14f,.28f),.105f,Rubber);
                    Part("Scope glass",t,new Vector3(0,.14f,.283f),new Vector3(.08f,.08f,.01f),Glass,1);
                    Part("Scope mount",t,new Vector3(0,.1f,.07f),new Vector3(.035f,.08f,.13f),Steel);
                }
                else {Part("Rear sight",t,new Vector3(0,.12f,-.005f),new Vector3(.062f,.063f,.022f),Steel);Part("Front sight",t,new Vector3(0,.102f,.4f),new Vector3(.025f,.085f,.02f),Steel);}
            }
            if(shotgun)Rod("Pump tube",t,new Vector3(0,-.045f,.15f),new Vector3(0,-.045f,.45f),.04f,Steel);
            if(gun.kind=="lmg")Part("Drum magazine",t,new Vector3(0,-.14f,.1f),new Vector3(.21f,.18f,.17f),accent,1);
            if(gun.id=="AWP"||gun.id=="RPK")foreach(int side in new[]{-1,1})Rod("Folded bipod",t,new Vector3(side*.03f,-.015f,.38f),new Vector3(side*.06f,-.09f,.12f),.017f,Steel);
            if(gun.id=="MP6")
            {
                Part("Compact stamped receiver",t,new Vector3(0,.015f,.02f),new Vector3(.092f,.13f,.22f),accent);
                Rod("Integral suppressor",t,new Vector3(0,.035f,.4f),new Vector3(0,.035f,.6f),.061f,Rubber);
                foreach(int side in new[]{-1,1})Rod("Folding stock strut",t,new Vector3(side*.05f,.04f,-.08f),new Vector3(side*.05f,.025f,-.34f),.018f,Steel);
            }
            if(gun.id=="VECTOR")
            {
                Part("Recoil channel",t,new Vector3(0,-.115f,.025f),new Vector3(.14f,.23f,.2f),accent);
                Part("Rear-mounted magazine",t,new Vector3(0,-.26f,-.045f),new Vector3(.065f,.21f,.095f),Rubber);
                Part("Reflex sight",t,new Vector3(0,.13f,.06f),new Vector3(.065f,.08f,.07f),Steel);
                Part("Reflex lens",t,new Vector3(0,.14f,.1f),new Vector3(.044f,.046f,.008f),Glass);
            }
            if(gun.id=="RAIL")for(int i=0;i<4;i++)Part("Capacitor",t,new Vector3(.06f,.012f,.17f+i*.09f),new Vector3(.025f,.09f,.03f),Teal);
            if(gun.id=="NOVA") {Rod("Launcher chamber",t,new Vector3(0,.03f,.02f),new Vector3(0,.03f,.5f),.18f,accent);Part("Plasma core",t,new Vector3(0,.03f,.51f),new Vector3(.14f,.14f,.014f),Gold,1);}
            if(gun.id=="CYCLONE")for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;Rod("Rotary barrel",t,new Vector3(Mathf.Cos(a)*.04f,.03f+Mathf.Sin(a)*.04f,.21f),new Vector3(Mathf.Cos(a)*.04f,.03f+Mathf.Sin(a)*.04f,.56f),.035f,Steel);}
            if(hands)
            {
                Part("Trigger hand",t,new Vector3(.032f,-.1f,-.075f),new Vector3(.10f,.115f,.11f),Rubber);
                Rod("Right sleeve",t,new Vector3(.047f,-.12f,-.08f),new Vector3(.23f,-.25f,-.29f),.15f,Cloth);
                Part("Support glove",t,new Vector3(-.02f,-.07f,pistol?.08f:.24f),new Vector3(.12f,.08f,.115f),Rubber);
                Rod("Left sleeve",t,new Vector3(-.03f,-.1f,pistol?.08f:.24f),new Vector3(-.3f,-.25f,-.23f),.135f,Cloth);
                for(int i=0;i<4;i++)Part("Finger armor",t,new Vector3(.075f,-.085f-i*.021f,-.07f),new Vector3(.023f,.016f,.065f),Steel);
            }
            return root;
        }
        public static GameObject SavePrefab(GameObject go,string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));var result=PrefabUtility.SaveAsPrefabAsset(go,path);Object.DestroyImmediate(go);return result;
        }
    }
}
