using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Object=UnityEngine.Object;

namespace TacticalZero.Editor
{
    // Evaluated Blender meshes retain split normals, atlas UVs and game-space limb pivots.
    // Source .blend and standard FBX files are kept outside Assets to avoid a Blender install dependency.
    public static class BlenderOperatorArt
    {
        const string Art="Assets/TacticalZero/Art/BlenderOperators";
        const string Meshes=ClassicArt.Root+"/Meshes/Blender11";
        const string Marker=ClassicArt.Root+"/BlenderOperators11.txt";
        [Serializable]public class Document{public string role,source;public Part[] groups;}
        [Serializable]public class Part{public string name;public int lod;public float[] pivot,positions,normals,uvs;public int[] indices;}
        public static void ApplyIfAvailable()
        {
            if(RefinedOperatorArt.ApplyIfAvailable())return;
            foreach(var role in ClassicRules.Data.roles)if(!File.Exists(Art+"/"+role.id+".mesh.json"))return;
            string fingerprint=Fingerprint();bool current=File.Exists(Marker)&&File.ReadAllText(Marker)==fingerprint;
            foreach(var role in ClassicRules.Data.roles){var prefab=Resources.Load<GameObject>("Classic/Operators/"+role.id);var mesh=prefab?prefab.GetComponentInChildren<MeshFilter>():null;current&=prefab&&prefab.GetComponent<LODGroup>()&&mesh&&mesh.sharedMesh&&mesh.sharedMesh.name.StartsWith("Blender ");}
            if(current)return;
            Apply();
        }
        static string Fingerprint()
        {
            using(var hash=SHA256.Create())
            {
                string all="blender-operator-import-v1\n";
                foreach(var role in ClassicRules.Data.roles)foreach(string suffix in new[]{".mesh.json","_albedo.png","_normal.png","_mask.png"})all+=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(Art+"/"+role.id+suffix)));
                return all;
            }
        }
        [MenuItem("Tactical Zero/Import Blender Portrait Models")]
        public static void Apply()
        {
            Directory.CreateDirectory(Meshes);Directory.CreateDirectory("ArtSource/PreviousOperators");AssetDatabase.Refresh();
            foreach(var role in ClassicRules.Data.roles)
            {
                string path=Art+"/"+role.id;var doc=JsonUtility.FromJson<Document>(File.ReadAllText(path+".mesh.json"));
                if(doc.role!=role.id||doc.groups.Length!=10)throw new InvalidDataException("Incomplete Blender export: "+role.id);
                Texture(path+"_albedo.png",false);Texture(path+"_normal.png",true);Texture(path+"_mask.png",false,false);
                string materialPath=path+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=role.id+" tailored atlas",enableInstancing=true};AssetDatabase.CreateAsset(material,materialPath);}
                material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path+"_albedo.png"));
                material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path+"_normal.png"));material.SetFloat("_BumpScale",.4f);material.EnableKeyword("_NORMALMAP");material.SetFloat("_Smoothness",.26f);material.SetFloat("_Metallic",.08f);EditorUtility.SetDirty(material);
                material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path+"_mask.png"));material.EnableKeyword("_METALLICSPECGLOSSMAP");material.SetFloat("_Smoothness",1);
                var root=new GameObject("Blender portrait operator "+role.id);var groups=new Dictionary<string,Transform>();var high=new List<Renderer>();var low=new List<Renderer>();int triangles=0;
                try
                {
                    foreach(var part in doc.groups)
                    {
                        if(!groups.TryGetValue(part.name,out var parent)){parent=new GameObject(part.name).transform;parent.SetParent(root.transform,false);parent.localPosition=new Vector3(part.pivot[0],part.pivot[1],part.pivot[2]);groups.Add(part.name,parent);}
                        var mesh=BuildMesh(part);string meshPath=Meshes+"/"+role.id+"_"+part.name+"_LOD"+part.lod+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                        if(existing){EditorUtility.CopySerialized(mesh,existing);EditorUtility.SetDirty(existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,meshPath);
                        var go=new GameObject(part.name+" LOD"+part.lod);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
                        if(part.lod==0){high.Add(renderer);triangles+=part.indices.Length/3;}else low.Add(renderer);
                    }
                    var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.18f,high.ToArray()),new LOD(.015f,low.ToArray())});lod.RecalculateBounds();
                    root.AddComponent<OperatorRig>();new GameObject("Microphone").transform.SetParent(root.transform,false);
                    string prefabPath=ClassicArt.Root+"/Operators/"+role.id+".prefab",backup="ArtSource/PreviousOperators/"+role.id+"-before-blender.prefab";
                    if(!File.Exists(backup))File.Copy(prefabPath,backup);
                    PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
                    Debug.Log("BLENDER_OPERATOR "+role.id+" triangles="+triangles+" highRenderers="+high.Count+" LOD1="+low.Count);
                }
                finally{Object.DestroyImmediate(root);}
            }
            File.WriteAllText(Marker,Fingerprint());AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        static Mesh BuildMesh(Part part)
        {
            int count=part.positions.Length/3;if(count<3||part.normals.Length!=part.positions.Length||part.uvs.Length!=count*2||part.indices.Length%3!=0)throw new InvalidDataException("Invalid "+part.name);
            var vertices=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];
            for(int i=0;i<count;i++){vertices[i]=new Vector3(part.positions[i*3],part.positions[i*3+1],part.positions[i*3+2]);normals[i]=new Vector3(part.normals[i*3],part.normals[i*3+1],part.normals[i*3+2]);uv[i]=new Vector2(part.uvs[i*2],part.uvs[i*2+1]);}
            foreach(int i in part.indices)if(i<0||i>=count)throw new InvalidDataException("Index outside "+part.name);
            var mesh=new Mesh{name="Blender "+part.name+" LOD"+part.lod,indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=part.indices;mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
        }
        static void Texture(string path,bool normal,bool srgb=true)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=!normal&&srgb;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        }
        [MenuItem("Tactical Zero/Review Blender Models")]
        public static void Review(){Apply();ArtReview06.Render();}
    }
}
