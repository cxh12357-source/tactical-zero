using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TacticalZero.Editor
{
    public static class WeaponModelArt
    {
        const string Root=ClassicArt.Root;
        static Mesh ring;
        static string currentId;
        static GameObject P(string n,Transform t,Vector3 p,Vector3 s,Material m,int shape=0)=>ClassicArt.Part(n,t,p,s,m,shape);
        static void Rod(string n,Transform t,Vector3 a,Vector3 b,float w,Material m)=>ClassicArt.Rod(n,t,a,b,w,m);
        static void Remove(Transform t,params string[] names)
        {
            foreach(var name in names)foreach(var child in new List<Transform>(Children(t)))if(child.name==name)Object.DestroyImmediate(child.gameObject);
        }
        static IEnumerable<Transform> Children(Transform t){foreach(Transform child in t)yield return child;}
        public static void Apply()
        {
            ring=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/OpticAnnulus06.asset");
            if(!ring){ring=Tube();AssetDatabase.CreateAsset(ring,Root+"/Meshes/OpticAnnulus06.asset");}
            foreach(var data in ClassicRules.Data.weapons)
            {
                var model=ClassicArt.Gun(data);CombatPolishArt.GunDetails(model,data);Refine(model.transform,data);
                var prefab=ClassicArt.SavePrefab(model,Root+"/Models/"+data.id+".prefab");
                var definition=Resources.Load<WeaponDefinition>("Classic/Weapons/"+data.id);definition.ConfigureClassic(data,prefab,definition.ShotAudio);EditorUtility.SetDirty(definition);
            }
        }
        static void Annulus(string name,Transform t,Vector3 at,float diameter,float depth,Material mat)
        {
            var go=new GameObject(name);go.transform.SetParent(t,false);go.transform.localPosition=at;go.transform.localScale=new Vector3(diameter,diameter,depth);
            go.AddComponent<MeshFilter>().sharedMesh=ring;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        static Mesh Tube()
        {
            var v=new List<Vector3>();var tr=new List<int>();int sides=48;
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=v.Count;v.AddRange(new[]{a,b,c,d});tr.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 x=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0),y=new Vector3(Mathf.Cos(b),Mathf.Sin(b),0),f=Vector3.forward*.5f;
                Quad(x*.5f-f,y*.5f-f,y*.5f+f,x*.5f+f);Quad(x*.39f+f,y*.39f+f,y*.39f-f,x*.39f-f);
                Quad(x*.5f+f,y*.5f+f,y*.39f+f,x*.39f+f);Quad(x*.39f-f,y*.39f-f,y*.5f-f,x*.5f-f);
            }
            var m=new Mesh{name="Open machined optic housing"};m.SetVertices(v);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        static void Refine(Transform t,ClassicGun data)
        {
            string id=data.id;currentId=id;var steel=ClassicArt.Steel;var black=ClassicArt.Rubber;var optic=WeaponSight.For(id);
            var finish=ClassicArt.Mat("Weapon06_"+id,id=="AWP"?new Color(.26f,.34f,.22f):id=="DMR"?new Color(.38f,.34f,.24f):id=="NOVA"?new Color(.38f,.22f,.12f):new Color(.18f,.22f,.23f),.55f,.45f);
            finish.SetTexture("_BaseMap",Resources.Load<Texture2D>("Classic/Textures/Brushed steel"));finish.SetTextureScale("_BaseMap",new Vector2(3,3));
            foreach(var r in t.GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial.name.StartsWith("GunAccent"))r.sharedMaterial=finish;
            Remove(t,"Rear sight","Rear sight bridge","Rear sight aperture","Front sight","Reflex sight","Reflex lens","Scope tube","Scope objective","Scope glass","Scope mount","Scope clamp","Scope clamp screw","Elevation turret","Windage turret");
            Remove(t,"Receiver","Upper receiver","Lower receiver contour","Grip");
            if(id=="USP")
            {
                Profile("Beveled pistol slide",t,new[]{new Vector2(-.066f,-.015f),new Vector2(-.078f,.041f),new Vector2(-.056f,.064f),new Vector2(.193f,.064f),new Vector2(.211f,.044f),new Vector2(.201f,-.01f)},.035f,steel);
                Profile("Ergonomic pistol grip",t,new[]{new Vector2(-.097f,-.19f),new Vector2(-.12f,-.17f),new Vector2(-.058f,-.025f),new Vector2(.015f,-.027f),new Vector2(-.02f,-.195f)},.029f,black);
            }
            else
            {
                Profile("Machined receiver",t,new[]{new Vector2(-.096f,-.037f),new Vector2(-.105f,.018f),new Vector2(-.071f,.065f),new Vector2(.21f,.065f),new Vector2(.242f,.04f),new Vector2(.231f,-.024f),new Vector2(.10f,-.053f),new Vector2(-.055f,-.053f)},.045f,steel);
                Profile("Contoured pistol grip",t,new[]{new Vector2(-.088f,-.21f),new Vector2(-.121f,-.198f),new Vector2(-.058f,-.025f),new Vector2(.003f,-.035f),new Vector2(-.035f,-.21f)},.028f,black);
                if(id!="M4A1"&&id!="MP6"&&id!="VECTOR"&&id!="AWP"&&id!="DMR")
                {
                    Remove(t,"Buttstock");
                    Profile("Contoured shoulder stock",t,new[]{new Vector2(-.316f,-.086f),new Vector2(-.329f,.038f),new Vector2(-.293f,.058f),new Vector2(-.166f,.034f),new Vector2(-.095f,.015f),new Vector2(-.096f,-.035f),new Vector2(-.20f,-.055f)},.044f,id=="AK47"?ClassicArt.Wood:finish);
                }
            }
            if(id=="AK47")
            {
                Remove(t,"Magazine","Magazine rib");
                for(int i=0;i<6;i++)
                {
                    float f=i/5f;var part=P("Curved steel magazine segment",t,new Vector3(0,-.075f-f*.205f,.115f+f*f*.074f),new Vector3(.067f,.054f,.099f),black);part.transform.localRotation=Quaternion.Euler(-f*31,0,0);
                    foreach(int side in new[]{-1,1})P("Pressed magazine flute",t,new Vector3(side*.035f,-.075f-f*.205f,.111f+f*f*.074f),new Vector3(.006f,.04f,.043f),steel).transform.localRotation=part.transform.localRotation;
                }
                Rod("Gas tube",t,new Vector3(0,.067f,.24f),new Vector3(0,.067f,.44f),.023f,steel);
                P("AK dust cover",t,new Vector3(0,.063f,.065f),new Vector3(.073f,.065f,.26f),black,1);
            }
            if(id=="AWP"||id=="DMR")
            {
                Remove(t,"Buttstock","Handguard","Magazine","Magazine rib");
                P("Precision chassis",t,new Vector3(0,-.026f,.1f),new Vector3(.1f,.095f,.49f),finish,5).transform.localRotation=Quaternion.Euler(90,0,0);
                Profile("Adjustable precision stock",t,new[]{new Vector2(-.334f,-.082f),new Vector2(-.338f,.039f),new Vector2(-.28f,.054f),new Vector2(-.108f,.04f),new Vector2(-.087f,-.032f),new Vector2(-.19f,-.045f),new Vector2(-.263f,-.077f)},.045f,finish);
                P("Raised cheek riser",t,new Vector3(0,.067f,-.23f),new Vector3(.083f,.04f,.18f),black);
                Rod("Stock length adjustment",t,new Vector3(0,-.02f,-.32f),new Vector3(0,-.02f,-.36f),.034f,steel);
                P("Precision recoil pad",t,new Vector3(0,-.025f,-.369f),new Vector3(.11f,.17f,.025f),black);
                P("Short box magazine",t,new Vector3(0,-.109f,.058f),new Vector3(.075f,.102f,.1f),black);
                Rod("Fluted precision barrel",t,new Vector3(0,.035f,.3f),new Vector3(0,.035f,.77f),.031f,steel);
                for(int i=0;i<5;i++)Annulus("Barrel machining ring",t,new Vector3(0,.035f,.40f+i*.063f),.035f,.012f,black);
                Rod("Bolt handle stem",t,new Vector3(.035f,.024f,-.055f),new Vector3(.10f,-.035f,-.055f),.015f,steel);
                P("Bolt handle knob",t,new Vector3(.107f,-.038f,-.055f),Vector3.one*.036f,black,1);
                P("Bolt travel channel",t,new Vector3(.052f,.02f,-.004f),new Vector3(.008f,.02f,.13f),black);
            }
            if(id=="VECTOR")Remove(t,"Magazine","Magazine rib","Handguard");
            if(id=="MP6")
            {
                Remove(t,"Buttstock","Adjustable cheek rest");
                foreach(int side in new[]{-1,1})Rod("Retractable stock rail",t,new Vector3(side*.055f,.018f,-.05f),new Vector3(side*.055f,.018f,-.30f),.011f,steel);
                Annulus("Suppressor machined rim",t,new Vector3(0,.035f,.601f),.064f,.018f,steel);
            }
            if(id=="Shotgun")
            {
                Remove(t,"Magazine","Magazine rib");
                for(int i=0;i<10;i++)Annulus("Ribbed pump grip",t,new Vector3(0,-.027f,.20f+i*.015f),.095f,.012f,black);
                for(int i=0;i<4;i++){Rod("Side saddle shell",t,new Vector3(-.07f,-.037f,-.04f+i*.037f),new Vector3(-.07f,.035f,-.04f+i*.037f),.024f,finish);}
            }
            if(id=="RPK"||id=="CYCLONE")
            {
                Remove(t,"Magazine","Magazine rib","Drum magazine");
                P("Ammunition feed box",t,new Vector3(0,-.158f,.066f),new Vector3(id=="CYCLONE"?.27f:.21f,.21f,.21f),finish);
                for(int i=0;i<7;i++)Rod("Linked cartridge",t,new Vector3(-.13f-i*.016f,-.01f-i*.015f,.03f),new Vector3(-.13f-i*.016f,-.01f-i*.015f,.106f),.018f,ClassicArt.Gold);
                Rod("Carry handle uprights",t,new Vector3(.05f,.056f,.11f),new Vector3(.09f,.11f,.15f),.018f,steel);
                Rod("Carry handle grip",t,new Vector3(.09f,.11f,.15f),new Vector3(.09f,.11f,.25f),.028f,black);
            }
            if(id=="RAIL")
            {
                foreach(int side in new[]{-1,1})
                {
                    P("Electromagnetic rail",t,new Vector3(side*.047f,.035f,.54f),new Vector3(.032f,.065f,.5f),steel);
                    for(int i=0;i<5;i++)P("Rail induction cell",t,new Vector3(side*.072f,.033f,.34f+i*.083f),new Vector3(.012f,.04f,.044f),ClassicArt.Teal);
                }
            }
            if(id=="NOVA")
            {
                Remove(t,"Magazine","Magazine rib");
                for(int i=0;i<5;i++)Annulus("Plasma chamber heat ring",t,new Vector3(0,.03f,.12f+i*.066f),.20f,.027f,steel);
            }
            if(id=="CYCLONE")
            {
                for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;Annulus("Rotary barrel bore",t,new Vector3(Mathf.Cos(a)*.04f,.03f+Mathf.Sin(a)*.04f,.566f),.039f,.018f,steel);}
                Annulus("Rotary barrel shroud",t,new Vector3(0,.03f,.48f),.15f,.034f,steel);
            }
            // All muzzle tips are open rings instead of solid black caps.
            Annulus("Open muzzle crown",t,new Vector3(0,.035f,id=="USP"?.266f:id=="AWP"||id=="DMR"?.78f:.54f),id=="USP"?.032f:.045f,.018f,steel);
            if(optic.Magnified)
            {
                float y=optic.Height;
                foreach(float z in new[]{.012f,.19f}){P("Optic saddle",t,new Vector3(0,.106f,z),new Vector3(.05f,.055f,.037f),steel);Annulus("Split scope mounting ring",t,new Vector3(0,y,z),.098f,.025f,steel);}
                Annulus("Precision scope body",t,new Vector3(0,y,.09f),.074f,.29f,black);
                Annulus("Scope ocular bell",t,new Vector3(0,y,-.076f),.106f,.061f,black);
                Annulus("Scope objective bell",t,new Vector3(0,y,.276f),id=="AWP"?.134f:.114f,.096f,black);
                foreach(float z in new[]{-.107f,.298f})Annulus("Optical coating rim",t,new Vector3(0,y,z),z<0?.085f:.105f,.006f,ClassicArt.Glass);
                P("Elevation dial",t,new Vector3(0,y+.065f,.09f),new Vector3(.054f,.025f,.054f),black,2);
                Rod("Windage dial",t,new Vector3(.028f,y,.09f),new Vector3(.072f,y,.09f),.043f,black);
                for(int i=0;i<12;i++){float a=i*Mathf.PI/6;P("Turret knurl",t,new Vector3(Mathf.Cos(a)*.027f,y+.065f,.09f+Mathf.Sin(a)*.027f),new Vector3(.006f,.024f,.006f),steel);}
            }
            else if(optic.Style!=SightStyle.Iron)
            {
                // Open hood keeps the actual line of sight clear; the collimated reticle is HUD geometry.
                P("Reflex base",t,new Vector3(0,.093f,.055f),new Vector3(.09f,.026f,.096f),steel);
                foreach(int side in new[]{-1,1})P("Reflex open frame",t,new Vector3(side*.039f,.137f,.066f),new Vector3(.011f,.072f,.032f),steel);
                P("Reflex hood crown",t,new Vector3(0,.176f,.066f),new Vector3(.088f,.012f,.034f),steel);
                P("Reflex battery housing",t,new Vector3(.055f,.111f,.05f),new Vector3(.025f,.036f,.065f),black);
            }
            else if(id!="USP")
            {
                P("Iron sight base",t,new Vector3(0,.096f,-.012f),new Vector3(.07f,.018f,.025f),steel);
                foreach(int side in new[]{-1,1})P("Open rear notch",t,new Vector3(side*.025f,.12f,-.012f),new Vector3(.012f,.05f,.025f),steel);
                P("Front sight post",t,new Vector3(0,.107f,.43f),new Vector3(.009f,.046f,.015f),steel);
                P("Front sight luminous tip",t,new Vector3(0,.128f,.43f),new Vector3(.006f,.004f,.016f),ClassicArt.Teal);
            }
            // Visible identification, machined panels and fasteners; no text is placed in the aiming line.
            for(int i=0;i<4;i++)P("Receiver recess",t,new Vector3(.052f,.021f,.16f+i*.04f),new Vector3(.008f,.026f,.018f),black);

        }
        static void Profile(string name,Transform parent,Vector2[] outline,float halfWidth,Material material)
        {
            // Beveled extrusion of a purpose-drawn side silhouette, instead of scaled box primitives.
            Vector2 center=Vector2.zero;foreach(var p in outline)center+=p;center/=outline.Length;
            var vertices=new List<Vector3>();var triangles=new List<int>();
            void Face(Vector3 a,Vector3 b,Vector3 c){int n=vertices.Count;vertices.AddRange(new[]{a,b,c});triangles.AddRange(new[]{n,n+1,n+2});}
            for(int i=0;i<outline.Length;i++)
            {
                int j=(i+1)%outline.Length;var a=outline[i];var b=outline[j];var ia=Vector2.Lerp(a,center,.055f);var ib=Vector2.Lerp(b,center,.055f);
                foreach(int side in new[]{-1,1})
                {
                    Vector3 p=new Vector3(side*halfWidth,ia.y,ia.x),q=new Vector3(side*halfWidth,ib.y,ib.x),r=new Vector3(side*(halfWidth-.005f),a.y,a.x),s=new Vector3(side*(halfWidth-.005f),b.y,b.x),c=new Vector3(side*halfWidth,center.y,center.x);
                    if(side>0){Face(c,p,q);Face(p,r,s);Face(p,s,q);}else{Face(c,q,p);Face(p,s,r);Face(p,q,s);}
                }
                var x=new Vector3(halfWidth-.005f,a.y,a.x);var y=new Vector3(halfWidth-.005f,b.y,b.x);var z=new Vector3(-halfWidth+.005f,b.y,b.x);var w=new Vector3(-halfWidth+.005f,a.y,a.x);Face(x,w,z);Face(x,z,y);
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);var uv=new List<Vector2>();foreach(var p in vertices)uv.Add(new Vector2(p.z*3,p.y*3));mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Root+"/Meshes/Portrait06/"+currentId+"_"+name.Replace(' ','_')+".asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(prior){EditorUtility.CopySerialized(mesh,prior);Object.DestroyImmediate(mesh);mesh=prior;}else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
    }
}
