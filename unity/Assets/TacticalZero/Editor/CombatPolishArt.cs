using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TacticalZero.Editor
{
    public static class CombatPolishArt
    {
        const string Root=ClassicArt.Root;
        public static void FinishArt()
        {
            if(File.Exists(Root+"/ArtFinish04.txt"))return;
            foreach(var gun in ClassicRules.Data.weapons)
            {
                var material=Resources.Load<Material>("Classic/Materials/GunAccent_"+gun.id);
                material.SetColor("_BaseColor",Color.Lerp(material.GetColor("_BaseColor"),new Color(.16f,.2f,.21f),.5f));EditorUtility.SetDirty(material);
                string path=Root+"/Models/"+gun.id+".prefab";var model=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var sight=model.transform.Find("Rear sight");
                    if(sight)
                    {
                        UnityEngine.Object.DestroyImmediate(sight.gameObject);
                        ClassicArt.Part("Rear sight bridge",model.transform,new Vector3(0,.096f,-.005f),new Vector3(.06f,.012f,.024f),ClassicArt.Steel);
                        foreach(int side in new[]{-1,1})ClassicArt.Part("Rear sight aperture",model.transform,new Vector3(side*.024f,.121f,-.005f),new Vector3(.012f,.052f,.024f),ClassicArt.Steel);
                    }
                    PrefabUtility.SaveAsPrefabAsset(model,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(model);}
            }
            var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene("Assets/TacticalZero/Scenes/Classic_sector07.unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var detail=new GameObject("Industrial finish 04").transform;
                var roofing=ClassicArt.Mat("Roof charcoal",new Color(.11f,.15f,.18f),.15f,.2f);
                foreach(var root in scene.GetRootGameObjects())foreach(var renderer in root.GetComponentsInChildren<Renderer>())if(renderer.name=="Truss diagonal")renderer.sharedMaterial=ClassicArt.Steel;
                for(int row=0;row<5;row++)
                {
                    float z=-23+row*11;
                    ClassicArt.Part("Truss upper chord",detail,new Vector3(0,8,z),new Vector3(81,.18f,.25f),ClassicArt.Steel);
                    for(int column=0;column<9;column++)ClassicArt.Rod("Truss vertical",detail,new Vector3(-38+column*9.5f,7,z),new Vector3(-38+column*9.5f,8,z),.08f,ClassicArt.Steel);
                    ClassicArt.Part("Roof panel",detail,new Vector3(0,8.3f,z),new Vector3(82,.1f,7.5f),roofing);
                    for(int column=0;column<6;column++)ClassicArt.Part("Roof seam",detail,new Vector3(-35+column*14,8.18f,z),new Vector3(.09f,.13f,7.5f),ClassicArt.Steel);
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
            File.WriteAllText(Root+"/ArtFinish04.txt","Open sights and finished skylit warehouse roof.");AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        public static void Apply()
        {
            if(File.Exists(Root+"/Polish04.txt"))
            {
                if(!File.Exists(Root+"/WeaponSilhouettes04.txt"))
                {
                    foreach(var gun in ClassicRules.Data.weapons){var model=ClassicArt.Gun(gun);GunDetails(model,gun);ClassicArt.SavePrefab(model,Root+"/Models/"+gun.id+".prefab");}
                    File.WriteAllText(Root+"/WeaponSilhouettes04.txt","Skeleton stocks, optic controls, receiver contours.");AssetDatabase.SaveAssets();
                }
                return;
            }
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/Bevel.asset");
            var replacement=Chamfer();EditorUtility.CopySerialized(replacement,mesh);EditorUtility.SetDirty(mesh);UnityEngine.Object.DestroyImmediate(replacement);
            Directory.CreateDirectory(Root+"/Textures");AssetDatabase.Refresh();
            Texture2D metal=Surface("Brushed steel",0),cloth=Surface("Woven textile",1),concrete=Surface("Weathered concrete",2);
            foreach(var path in Directory.GetFiles(Root+"/Materials","*.mat"))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);string name=material.name;
                if(name.StartsWith("Glow")||name=="WorldLettering"||name=="OpticGlass"||name.EndsWith("Foliage"))continue;
                bool fabric=name.StartsWith("Armor_")||name=="Undersuit"||name=="Rubber";
                bool wall=name.EndsWith("Walls")||name.EndsWith("Ground")||name.EndsWith("Stone");
                material.SetTexture("_BaseMap",fabric?cloth:wall?concrete:metal);
                material.SetTextureScale("_BaseMap",fabric?new Vector2(5,5):wall?new Vector2(12,8):new Vector2(3,3));
                material.SetFloat("_Smoothness",fabric?.16f:wall?.15f:.36f);
                if(wall){material.SetFloat("_Metallic",0);material.SetColor("_BaseColor",material.GetColor("_BaseColor")*.85f);}
                EditorUtility.SetDirty(material);
            }
            foreach(var gun in ClassicRules.Data.weapons)
            {
                var model=ClassicArt.Gun(gun);GunDetails(model,gun);var prefab=ClassicArt.SavePrefab(model,Root+"/Models/"+gun.id+".prefab");
                var definition=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(Root+"/Weapons/"+gun.id+".asset");
                definition.ConfigureClassic(gun,prefab,definition.ShotAudio);EditorUtility.SetDirty(definition);
            }
            foreach(var role in ClassicRules.Data.roles)
            {
                var model=ClassicArt.Operator(role);CharacterDetails(model,role);ClassicArt.SavePrefab(model,Root+"/Operators/"+role.id+".prefab");
            }
            foreach(var map in ClassicRules.Data.maps)
            {
                var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene("Assets/TacticalZero/Scenes/"+map.Scene+".unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
                try
                {
                    foreach(var root in scene.GetRootGameObjects())foreach(var renderer in root.GetComponentsInChildren<Renderer>())
                    {
                        if(renderer.name=="Coping"||renderer.name=="Support pier")renderer.sharedMaterial=ClassicArt.Steel;
                    }
                    // Warmer key and cool fill retain each zone's palette without glossy toy surfaces.
                    if(RenderSettings.sun){RenderSettings.sun.intensity=map.id=="ruins"?1.45f:1.25f;RenderSettings.sun.shadowBias=.03f;RenderSettings.sun.shadowNormalBias=.22f;}
                    var detail=new GameObject("Surface detail 04").transform;
                    foreach(var wall in map.walls)
                    {
                        if(map.id=="outpost"||(map.id=="docks"&&wall.y==1))continue;
                        var at=ClassicMap.Position(wall.x+wall.w*.5f,wall.y,1.4f)+Vector3.forward*.04f;
                        var mat=ClassicArt.Mat(map.id+"_AccessPanel",map.id=="ruins"?new Color(.25f,.2f,.17f):new Color(.14f,.19f,.2f),.2f,.2f);
                        ClassicArt.Part("Recessed access panel",detail,at,new Vector3(.8f,.9f,.045f),mat);
                        for(int i=0;i<4;i++)ClassicArt.Part("Vent slat",detail,at+new Vector3(0,.22f-i*.13f,.034f),new Vector3(.62f,.025f,.028f),ClassicArt.Steel);
                        ClassicArt.Part("Maintenance label",detail,at+new Vector3(.24f,-.31f,.03f),new Vector3(.19f,.07f,.01f),ClassicArt.White);
                    }
                    EditorSceneManager.SaveScene(scene);
                }
                finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
            }
            File.WriteAllText(Root+"/Polish04.txt","Precise chamfers, textile/metal/concrete materials, model details and FPS accuracy.");
            File.WriteAllText(Root+"/WeaponSilhouettes04.txt","Skeleton stocks, optic controls, receiver contours.");AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        static Mesh Chamfer()
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();float[] grid={-.5f,-.48f,0,.48f,.5f};
            foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
            {
                Vector3 a=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up,b=Vector3.Cross(normal,a);int first=vertices.Count;
                for(int y=0;y<5;y++)for(int x=0;x<5;x++)
                {
                    var p=normal*.5f+a*grid[x]+b*grid[y];var core=new Vector3(Mathf.Clamp(p.x,-.48f,.48f),Mathf.Clamp(p.y,-.48f,.48f),Mathf.Clamp(p.z,-.48f,.48f));
                    var n=(p-core).normalized;vertices.Add(core+n*.02f);normals.Add(n);uv.Add(new Vector2(grid[x]+.5f,grid[y]+.5f));
                }
                for(int y=0;y<4;y++)for(int x=0;x<4;x++){int i=first+y*5+x;indices.AddRange(new[]{i,i+1,i+6,i,i+6,i+5});}
            }
            var mesh=new Mesh{name="Precision hard surface chamfer"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
        static Texture2D Surface(string name,int style)
        {
            var texture=new Texture2D(256,256,TextureFormat.RGB24,true){name=name,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                float noise=Mathf.PerlinNoise(x*.31f,y*.31f),broad=Mathf.PerlinNoise(x*.047f,y*.047f);
                float value=style==1?.78f+(((x/2+y/2)%2==0)?.12f:0)+noise*.07f:style==0?.84f+Mathf.Sin(y*2.2f)*.025f+noise*.08f:.69f+broad*.2f+noise*.09f;
                texture.SetPixel(x,y,new Color(value,value,value));
            }
            texture.Apply(true,false);AssetDatabase.CreateAsset(texture,Root+"/Textures/"+name+".asset");return texture;
        }
        public static void GunDetails(GameObject model,ClassicGun gun)
        {
            var t=model.transform;
            bool skeletal=gun.id=="M4A1"||gun.id=="MP6"||gun.id=="VECTOR";
            if(skeletal)
            {
                var stock=t.Find("Buttstock");if(stock)UnityEngine.Object.DestroyImmediate(stock.gameObject);
                ClassicArt.Rod("Buffer tube",t,new Vector3(0,.015f,-.1f),new Vector3(0,.015f,-.29f),.034f,ClassicArt.Steel);
                ClassicArt.Part("Adjustable cheek rest",t,new Vector3(0,.035f,-.22f),new Vector3(.075f,.038f,.16f),ClassicArt.Rubber);
                foreach(int side in new[]{-1,1})ClassicArt.Rod("Skeleton stock brace",t,new Vector3(side*.027f,-.055f,-.12f),new Vector3(side*.027f,-.065f,-.29f),.015f,ClassicArt.Steel);
                ClassicArt.Part("Stock adjustment lever",t,new Vector3(0,-.046f,-.22f),new Vector3(.04f,.016f,.07f),ClassicArt.Rubber);
            }
            if(gun.id!="USP")
            {
                ClassicArt.Part("Lower receiver contour",t,new Vector3(0,-.037f,.015f),new Vector3(.077f,.055f,.17f),ClassicArt.Steel,5).transform.localRotation=Quaternion.Euler(90,0,0);
                ClassicArt.Rod("Trigger guard front",t,new Vector3(0,-.012f,.087f),new Vector3(0,-.058f,.08f),.009f,ClassicArt.Steel);
                ClassicArt.Part("Bolt release paddle",t,new Vector3(-.055f,-.025f,.025f),new Vector3(.009f,.035f,.027f),ClassicArt.Rubber);
            }
            if(gun.kind=="dmr"||gun.kind=="rail")
            {
                foreach(float z in new[]{.015f,.16f})
                {
                    ClassicArt.Part("Scope clamp",t,new Vector3(0,.135f,z),new Vector3(.09f,.085f,.022f),ClassicArt.Steel);
                    foreach(int side in new[]{-1,1})ClassicArt.Part("Scope clamp screw",t,new Vector3(side*.051f,.12f,z),new Vector3(.018f,.018f,.018f),ClassicArt.White,1);
                }
                ClassicArt.Part("Elevation turret",t,new Vector3(0,.198f,.095f),new Vector3(.043f,.023f,.043f),ClassicArt.Rubber,2);
                ClassicArt.Rod("Windage turret",t,new Vector3(.029f,.145f,.095f),new Vector3(.06f,.145f,.095f),.035f,ClassicArt.Rubber);
            }
            foreach(int side in new[]{-1,1})
            {
                for(int i=0;i<3;i++)ClassicArt.Part("Receiver pin",t,new Vector3(side*.05f,-.005f,-.04f+i*.065f),new Vector3(.008f,.013f,.013f),ClassicArt.White,1);
                for(int i=0;i<5;i++)ClassicArt.Part("Grip checkering",t,new Vector3(side*.032f,-.09f-i*.025f,-.045f),new Vector3(.006f,.006f,.07f),ClassicArt.Steel);
            }
            ClassicArt.Part("Safety selector",t,new Vector3(.057f,-.03f,-.034f),new Vector3(.015f,.022f,.05f),ClassicArt.Steel).transform.localRotation=Quaternion.Euler(25,0,0);
            ClassicArt.Part("Charging handle",t,new Vector3(.065f,.045f,-.06f),new Vector3(.06f,.018f,.027f),ClassicArt.Steel);
            if(gun.id=="USP")
            {
                for(int i=0;i<6;i++)ClassicArt.Part("Slide serration",t,new Vector3(.035f,.022f,-.025f+i*.009f),new Vector3(.007f,.032f,.004f),ClassicArt.Rubber);
                foreach(int side in new[]{-1,1})ClassicArt.Part("Rear sight post",t,new Vector3(side*.022f,.083f,-.055f),new Vector3(.01f,.018f,.014f),ClassicArt.Steel);
                ClassicArt.Part("Front sight bead",t,new Vector3(0,.076f,.192f),new Vector3(.007f,.013f,.01f),ClassicArt.Teal);
            }
            foreach(int side in new[]{-1,1})
            {
                var at=side>0?new Vector3(.035f,-.11f,-.085f):new Vector3(-.025f,-.075f,gun.id=="USP"?.08f:.24f);
                for(int i=0;i<4;i++)
                {
                    ClassicArt.Part("Detail glove knuckle",t,at+new Vector3(side*.05f,.025f,i*.019f-.03f),new Vector3(.035f,.033f,.019f),ClassicArt.Cloth,1);
                    ClassicArt.Part("Detail glove seam",t,at+new Vector3(side*.062f,-.025f,i*.019f-.03f),new Vector3(.008f,.035f,.009f),ClassicArt.Steel);
                }
                ClassicArt.Part("Detail glove thumb",t,at+new Vector3(-side*.04f,.01f,.045f),new Vector3(.035f,.045f,.085f),ClassicArt.Rubber,1).transform.localRotation=Quaternion.Euler(0,side*30,0);
            }
        }
        static void CharacterDetails(GameObject model,ClassicRole role)
        {
            var t=model.transform;var armor=Resources.Load<Material>("Classic/Materials/Armor_"+role.id);
            foreach(int side in new[]{-1,1})
            {
                var leg=t.Find(side<0?"LeftLeg":"RightLeg");
                for(int row=0;row<3;row++)ClassicArt.Part("Trouser articulated fold",leg,new Vector3(0,-.29f-row*.18f,-.015f),new Vector3(.21f,.025f,.21f),ClassicArt.Cloth);
                ClassicArt.Part("Boot sole",leg,new Vector3(0,-.925f,.065f),new Vector3(.235f,.035f,.36f),ClassicArt.Rubber);
                var arm=t.Find(side<0?"LeftArm":"RightArm");
                ClassicArt.Part("Shoulder outer shell",arm,new Vector3(side*.06f,-.03f,-.01f),new Vector3(.23f,.23f,.31f),armor,5);
                ClassicArt.Part("Collar pad",t,new Vector3(side*.115f,1.535f,-.025f),new Vector3(.075f,.14f,.27f),armor);
                ClassicArt.Part("Eye",t,new Vector3(side*.064f,1.75f,.153f),new Vector3(.04f,.021f,.016f),ClassicArt.Rubber,1);
                ClassicArt.Part("Backpack cinch",t,new Vector3(side*.115f,1.2f,-.437f),new Vector3(.025f,.36f,.015f),ClassicArt.Rubber);
            }
            ClassicArt.Part("Belt clasp",t,new Vector3(0,.95f,.2f),new Vector3(.11f,.07f,.027f),ClassicArt.White);
            ClassicArt.Part("Plate maker badge",t,new Vector3(.125f,1.38f,.243f),new Vector3(.075f,.035f,.012f),ClassicArt.White);
            ClassicArt.Rod("Headset boom",t,new Vector3(.17f,1.72f,.03f),new Vector3(.09f,1.66f,.19f),.012f,ClassicArt.Steel);
            ClassicArt.Part("Microphone",t,new Vector3(.075f,1.66f,.19f),new Vector3(.045f,.023f,.026f),ClassicArt.Rubber);
        }
    }
}
