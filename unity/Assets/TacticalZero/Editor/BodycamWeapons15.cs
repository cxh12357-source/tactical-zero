using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace TacticalZero.Editor
{
    public static class BodycamWeapons15
    {
        const string Art="Assets/TacticalZero/Art/BodycamFirearms15";
        [Serializable]class Document{public Surface[] materials;}
        [Serializable]class Surface{public string name;public float[] color;public float metallic,roughness;}
        public static void Apply()
        {
            var doc=JsonUtility.FromJson<Document>(File.ReadAllText(Art+"/materials.json"));
            string[] names={"USP","M4A1","AK47","MP6","VECTOR","DMR","AWP","RPK","Shotgun"};float[] ends={.063f,.52f,.59f,.23f,.17f,.65f,.72f,.76f,.65f};float[] heights={.052f,.023f,.021f,.023f,.016f,.023f,.027f,.021f,.028f};
            for(int i=0;i<names.Length;i++)
            {
                string id=names[i];var importer=(ModelImporter)AssetImporter.GetAtPath(Art+"/"+id+".fbx");importer.isReadable=true;importer.importCameras=importer.importLights=importer.importAnimation=false;
                foreach(var surface in doc.materials)
                {
                    string path=Art+"/"+surface.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
                    m.color=new Color(surface.color[0],surface.color[1],surface.color[2]).gamma;m.SetFloat("_Metallic",surface.metallic);m.SetFloat("_Smoothness",1-surface.roughness);EditorUtility.SetDirty(m);importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),surface.name),m);
                }
                importer.SaveAndReimport();var root=new GameObject(id+" detailed firearm");Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/"+id+".fbx"),root.transform);
                Transform Find(string name)=>root.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name==name);
                Transform Target(string name,Vector3 pos){var t=new GameObject(name).transform;t.SetParent(root.transform,false);t.localPosition=pos;return t;}
                var anim=root.AddComponent<WeaponReloadAnimation>();anim.WeaponId=id;anim.Magazine=Find("Magazine");anim.Bolt=Find("Bolt");anim.Shell=Find("Shell");
                anim.LeftHand=Target("Reload left hand",id=="USP"?new Vector3(-.045f,-.1f,-.08f):new Vector3(-.024f,-.065f,.20f));anim.RightHand=Target("Reload right hand",new Vector3(.034f,-.09f,-.075f));if(anim.Shell)anim.Shell.gameObject.SetActive(false);
                Target("Muzzle",new Vector3(0,heights[i],ends[i]));var prefab=PrefabUtility.SaveAsPrefabAsset(root,ClassicArt.Root+"/Models/"+id+".prefab");Object.DestroyImmediate(root);
                var definition=ClassicRules.Weapon(id);definition.ConfigureClassic(ClassicRules.Gun(id),prefab,Resources.Load<AudioClip>("Audio/"+(id=="USP"?"USP":id=="Shotgun"?"Shotgun":"AK47")));EditorUtility.SetDirty(definition);
            }
        }
    }
}
