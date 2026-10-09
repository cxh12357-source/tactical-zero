using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace TacticalZero.Editor
{
    public static class FieldWeapons14
    {
        const string Art="Assets/TacticalZero/Art/FieldWeapons14";
        [Serializable]class Document{public Surface[] materials;}
        [Serializable]class Surface{public string name;public float[] color;public float metallic,roughness;}
        public static void Apply()
        {
            if(File.Exists(Art+"/Imported-v3.txt"))return;
            var doc=JsonUtility.FromJson<Document>(File.ReadAllText(Art+"/materials.json"));
            foreach(string id in new[]{"P90","MG3","M700","M1014"})
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(Art+"/"+id+".fbx");importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
                foreach(var surface in doc.materials)
                {
                    string path=Art+"/"+surface.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
                    m.color=new Color(surface.color[0],surface.color[1],surface.color[2]).gamma;m.SetFloat("_Metallic",surface.metallic);m.SetFloat("_Smoothness",1-surface.roughness);
                    m.SetTexture("_BaseMap",null);
                    EditorUtility.SetDirty(m);importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),surface.name),m);
                }
                importer.SaveAndReimport();
                var root=new GameObject(id+" field model");var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/"+id+".fbx"),root.transform);model.name="Machined firearm";
                var donor=Object.Instantiate(Resources.Load<GameObject>("Classic/Models/M4A1"));
                foreach(var child in donor.GetComponentsInChildren<Transform>().Where(t=>t!=donor.transform).ToArray())
                {
                    string n=child.name.ToLowerInvariant();if(child.GetComponent<Renderer>()&&!n.Contains("handguard")&&(n.Contains("sleeve")||n.Contains("glove")||n.Contains("hand")||n.Contains("finger")))child.SetParent(root.transform,true);
                }
                Object.DestroyImmediate(donor);ConfigureReload(root,id);
                var muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(root.transform,false);muzzle.localPosition=new Vector3(0,id=="P90"?.047f:id=="MG3"?.009f:id=="M700"?.02f:.027f,id=="P90"?.235f:id=="MG3"?.71f:id=="M700"?.582f:.57f);
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,ClassicArt.Root+"/Models/"+id+".prefab");Object.DestroyImmediate(root);
                var definition=ClassicRules.Weapon(id);definition.ConfigureClassic(ClassicRules.Gun(id),prefab,Resources.Load<AudioClip>("Audio/"+(id=="M1014"?"Shotgun":"AK47")));EditorUtility.SetDirty(definition);
            }
            foreach(var gun in ClassicRules.Data.weapons)
            {
                if(new[]{"P90","MG3","M700","M1014"}.Contains(gun.id))continue;
                string path=ClassicArt.Root+"/Models/"+gun.id+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try{ConfigureReload(root,gun.id);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            File.WriteAllText(Art+"/Imported-v3.txt","Distinct P90, MG3, M700 and M1014 meshes, articulated reload groups.");AssetDatabase.SaveAssets();ClassicProjectSetup.RefreshWeaponPreviews();
        }
        static void ConfigureReload(GameObject root,string id)
        {
            var anim=root.GetComponent<WeaponReloadAnimation>()??root.AddComponent<WeaponReloadAnimation>();anim.WeaponId=id;
            Transform Group(string name,Func<Transform,bool> filter,Vector3 pivot)
            {
                var parts=root.GetComponentsInChildren<Transform>().Where(t=>t!=root.transform&&t.GetComponent<Renderer>()&&filter(t)).ToArray();if(parts.Length==0)return null;
                var parent=new GameObject(name).transform;parent.SetParent(root.transform,false);parent.localPosition=pivot;foreach(var part in parts)part.SetParent(parent,true);return parent;
            }
            anim.Magazine=Group("Reload magazine",t=>t.name.ToLowerInvariant().Contains("magazine"),Vector3.zero);
            anim.Bolt=Group("Reload bolt",t=>t.name=="Bolt"||t.name.Contains("Bolt handle")||t.name.Contains("Charging"),Vector3.zero);
            anim.Cover=Group("Reload feed cover",t=>t.name=="Cover",new Vector3(0,.067f,.085f));
            anim.Belt=Group("Reload linked belt",t=>t.name=="Belt",Vector3.zero);anim.Shell=Group("Reload shell",t=>t.name=="Shell",Vector3.zero);
            bool Hand(Transform t){string n=t.name.ToLowerInvariant();return !n.Contains("handguard")&&(n.Contains("sleeve")||n.Contains("glove")||n.Contains("hand")||n.Contains("finger"));}
            anim.LeftHand=Group("Reload left hand",t=>Hand(t)&&root.transform.InverseTransformPoint(t.position).x<0,new Vector3(-.02f,-.07f,.24f));
            anim.RightHand=Group("Reload right hand",t=>Hand(t)&&root.transform.InverseTransformPoint(t.position).x>=0,new Vector3(.032f,-.1f,-.075f));
            if(anim.Shell)anim.Shell.gameObject.SetActive(false);
        }
    }
}



