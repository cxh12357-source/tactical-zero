using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Object=UnityEngine.Object;

namespace TacticalZero.Editor
{
    public static class RefinedOperatorArt
    {
        const string Art="Assets/TacticalZero/Art/RefinedOperators";
        const string Generated=Art+"/Generated";
        const string Marker=Generated+"/import.txt";
        [Serializable]public class Document{public string role;public Surface[] materials;}
        [Serializable]public class Surface{public string name;public float[] color;public bool atlas,normal;public float roughness,metallic;}
        public static bool ApplyIfAvailable()
        {
            foreach(var role in ClassicRules.Data.roles)if(!File.Exists(Art+"/"+role.id+".fbx"))return false;
            string fingerprint="refined-13-v3";
            using(var hash=SHA256.Create())foreach(string path in Directory.GetFiles(Art).Where(p=>p.EndsWith(".fbx")||p.EndsWith(".json")||p.EndsWith(".png")).OrderBy(p=>p))fingerprint+=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path)));
            bool current=File.Exists(Marker)&&File.ReadAllText(Marker)==fingerprint;
            foreach(var role in ClassicRules.Data.roles){var p=Resources.Load<GameObject>("Classic/Operators/"+role.id);current&=p&&p.GetComponent<OperatorRig>()&&p.GetComponent<OperatorRig>().refined;}
            if(current)return true;
            Directory.CreateDirectory(Generated);AssetDatabase.Refresh();
            foreach(var role in ClassicRules.Data.roles)Import(role.id);
            File.WriteAllText(Marker,fingerprint);AssetDatabase.SaveAssets();AssetDatabase.Refresh();return true;
        }
        static void Import(string role)
        {
            string path=Art+"/"+role;
            var doc=JsonUtility.FromJson<Document>(File.ReadAllText(path+".json"));
            foreach(string suffix in new[]{"_BaseColor","_Normal","_Mask"})
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(path+suffix+".png");t.textureType=suffix=="_Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;t.sRGBTexture=suffix=="_BaseColor";t.alphaSource=TextureImporterAlphaSource.FromInput;t.maxTextureSize=2048;t.mipmapEnabled=true;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Trilinear;t.anisoLevel=4;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
            }
            var materials=new Dictionary<string,Material>();
            foreach(var surface in doc.materials)
            {
                string mp=Generated+"/"+surface.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(mp);
                if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=surface.name};AssetDatabase.CreateAsset(m,mp);}
                m.shader=Shader.Find(surface.atlas&&!surface.normal?"TacticalZero/Complexion":"Universal Render Pipeline/Lit");
                m.SetColor("_BaseColor",surface.atlas?Color.white:new Color(surface.color[0],surface.color[1],surface.color[2],1));
                m.SetFloat("_Smoothness",1-surface.roughness);m.SetFloat("_Metallic",surface.metallic);
                if(surface.atlas){m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path+"_BaseColor.png"));m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path+"_Mask.png"));m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",1);}
                if(surface.normal){m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path+"_Normal.png"));m.SetFloat("_BumpScale",.5f);m.EnableKeyword("_NORMALMAP");}
                EditorUtility.SetDirty(m);materials.Add(surface.name,m);
            }
            var importer=(ModelImporter)AssetImporter.GetAtPath(path+".fbx");importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=false;importer.importBlendShapes=true;importer.isReadable=true;importer.optimizeGameObjects=false;importer.importCameras=false;importer.importLights=false;importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var pair in materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
            importer.SaveAndReimport();
            var root=new GameObject("Refined operator "+role);var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path+".fbx"),root.transform);model.name="Skeleton and skin";
            try
            {
                var source=model.GetComponentInChildren<SkinnedMeshRenderer>();if(!source)throw new InvalidDataException(role+" missing skin");
                // Normalize to a human scale regardless of FBX unit conventions. FBX converts the Blender front to Unity +Z.
                float factor=1.92f/source.bounds.size.y;model.transform.localScale*=factor;model.transform.localRotation=Quaternion.identity;
                model.transform.localPosition-=new Vector3(source.bounds.center.x,source.bounds.min.y,source.bounds.center.z);
                var mesh=source.sharedMesh;var vertices=mesh.vertices;var renderers=new List<Renderer>();
                // Three skinned sections retain identical bind poses and shape keys. First person can show legs while hiding the torso.
                for(int section=0;section<3;section++)
                {
                    var part=Object.Instantiate(mesh);part.name=role+" refined "+section;
                    for(int sub=0;sub<mesh.subMeshCount;sub++)
                    {
                        var indices=mesh.GetTriangles(sub);var kept=new List<int>();
                        for(int i=0;i<indices.Length;i+=3)
                        {
                            Vector3 center=root.transform.InverseTransformPoint(source.transform.TransformPoint((vertices[indices[i]]+vertices[indices[i+1]]+vertices[indices[i+2]])/3));
                            int target=center.y<.86f?(center.x<0?1:2):0;if(target==section){kept.Add(indices[i]);kept.Add(indices[i+1]);kept.Add(indices[i+2]);}
                        }
                        part.SetTriangles(kept,sub,false);
                    }
                    var compact=Compact(part);Object.DestroyImmediate(part);part=compact;
                    string meshPath=Generated+"/"+role+"_skin_"+section+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(existing){EditorUtility.CopySerialized(part,existing);Object.DestroyImmediate(part);part=existing;EditorUtility.SetDirty(part);}else AssetDatabase.CreateAsset(part,meshPath);
                    var holder=new GameObject(section==0?"Body":section==1?"LeftLeg":"RightLeg");holder.transform.SetParent(root.transform,false);
                    var go=new GameObject("Skinned surface");go.transform.SetParent(holder.transform,false);go.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);go.transform.localScale=source.transform.lossyScale;
                    var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=part;skin.sharedMaterials=source.sharedMaterials;skin.bones=source.bones;skin.rootBone=source.rootBone;skin.localBounds=source.localBounds;skin.quality=SkinQuality.Bone4;skin.updateWhenOffscreen=true;renderers.Add(skin);
                }
                Object.DestroyImmediate(source);
                var rig=root.AddComponent<OperatorRig>();rig.refined=true;
                var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.012f,renderers.ToArray())});lod.RecalculateBounds();
                string prefab=ClassicArt.Root+"/Operators/"+role+".prefab";Directory.CreateDirectory("ArtSource/PreviousOperators");string backup="ArtSource/PreviousOperators/"+role+"-before-refined.prefab";if(!File.Exists(backup))File.Copy(prefab,backup);
                PrefabUtility.SaveAsPrefabAsset(root,prefab);
                Debug.Log("REFINED_IMPORT "+role+" bones="+renderers[0].GetComponent<SkinnedMeshRenderer>().bones.Length+" vertices="+mesh.vertexCount+" shapes="+mesh.blendShapeCount+" height="+(1.92f));
            }
            finally{Object.DestroyImmediate(root);}
        }
        public static void Review(){ApplyIfAvailable();ArtReview06.Render();}
        static Mesh Compact(Mesh source)
        {
            int[] used=source.triangles.Distinct().OrderBy(x=>x).ToArray();var remap=new int[source.vertexCount];for(int i=0;i<used.Length;i++)remap[used[i]]=i;
            var mesh=new Mesh{name=source.name,indexFormat=IndexFormat.UInt32};
            var vertices=source.vertices;var normals=source.normals;var tangents=source.tangents;var uv=source.uv;var colors=source.colors;var weights=source.boneWeights;
            mesh.vertices=used.Select(i=>vertices[i]).ToArray();mesh.normals=used.Select(i=>normals[i]).ToArray();mesh.uv=used.Select(i=>uv[i]).ToArray();
            if(tangents.Length==vertices.Length)mesh.tangents=used.Select(i=>tangents[i]).ToArray();if(colors.Length==vertices.Length)mesh.colors=used.Select(i=>colors[i]).ToArray();
            mesh.boneWeights=used.Select(i=>weights[i]).ToArray();mesh.bindposes=source.bindposes;mesh.subMeshCount=source.subMeshCount;
            for(int sub=0;sub<mesh.subMeshCount;sub++)mesh.SetTriangles(source.GetTriangles(sub).Select(i=>remap[i]).ToArray(),sub,false);
            for(int shape=0;shape<source.blendShapeCount;shape++)for(int frame=0;frame<source.GetBlendShapeFrameCount(shape);frame++)
            {
                var v=new Vector3[source.vertexCount];var n=new Vector3[source.vertexCount];var t=new Vector3[source.vertexCount];source.GetBlendShapeFrameVertices(shape,frame,v,n,t);
                mesh.AddBlendShapeFrame(source.GetBlendShapeName(shape),source.GetBlendShapeFrameWeight(shape,frame),used.Select(i=>v[i]).ToArray(),used.Select(i=>n[i]).ToArray(),used.Select(i=>t[i]).ToArray());
            }
            mesh.RecalculateBounds();return mesh;
        }
    }
}


