using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Unity.AI.Navigation;
using Object=UnityEngine.Object;

namespace TacticalZero.Editor
{
    // Structural detailing is gameplay geometry, even where an older wall was removed.
    public static class MapCollision08
    {
        static readonly HashSet<string> Solids=new HashSet<string>{"Perimeter","Concrete partition","Broken courtyard masonry","Rock ridge","Support pier","Coping","Wall footing","Fractured rock crown","Weathered ridge face","Doorway wall","Window sill wall","Window header wall","Window pier","Interior partition","Room roof","Door lintel","Interior door header","Shipping container","Cargo stack","Supply shelter","Vehicle barricade","Roof panel","Truss column","Truss beam"};
        static readonly HashSet<string> WallAttachments=new HashSet<string>{"Boarded window recess","Window jamb","Diagonal window plank","Recessed access panel","Vent slat","Maintenance label"};
        public static void Apply()
        {
            foreach(var map in ClassicRules.Data.maps)
            {
                var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene("Assets/TacticalZero/Scenes/"+map.Scene+".unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
                try
                {
                    Transform geometry=null;foreach(var root in scene.GetRootGameObjects())if(root.name=="Static architecture")geometry=root.transform;
                    if(!geometry)throw new System.InvalidOperationException("Missing architecture: "+map.id);
                    bool refinementFound=false;
                    foreach(var root in scene.GetRootGameObjects())foreach(var node in root.GetComponentsInChildren<Transform>(true))if(node&&node.name=="Art refinement 02")
                    {if(refinementFound)Object.DestroyImmediate(node.gameObject);else refinementFound=true;}
                    foreach(var root in scene.GetRootGameObjects())if(root.name=="Industrial finish 04"||root.name=="Surface detail 04"||root.name=="Art refinement 02")root.transform.SetParent(geometry,true);
                    int repaired=0;
                    foreach(var r in geometry.GetComponentsInChildren<MeshRenderer>())
                    {
                        var go=r.gameObject;
                        GameObjectUtility.SetStaticEditorFlags(go,GameObjectUtility.GetStaticEditorFlags(go)|StaticEditorFlags.BatchingStatic);
                        if(!Solids.Contains(go.name))continue;
                        bool changed=go.layer!=LayerMask.NameToLayer("TZWorld");go.layer=LayerMask.NameToLayer("TZWorld");
                        var collider=go.GetComponent<Collider>();
                        if(!collider)
                        {
                            // Jagged rock silhouettes need their actual shape, not a large invisible box.
                            if(go.name=="Weathered ridge face"||go.name=="Fractured rock crown")
                            {var mesh=go.AddComponent<MeshCollider>();mesh.sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;collider=mesh;}
                            else collider=go.AddComponent<BoxCollider>();
                            changed=true;
                        }
                        if(collider.isTrigger||!collider.enabled)changed=true;
                        // The decorative cliff mesh has an open underside. A convex hull closes it
                        // so rotated crowns block movement and bullets from both sides.
                        if(collider is MeshCollider rock&&!rock.convex){rock.convex=true;changed=true;}
                        collider.enabled=true;collider.isTrigger=false;if(changed)repaired++;
                    }
                    int patch=0;foreach(var root in scene.GetRootGameObjects())foreach(var grass in root.GetComponentsInChildren<GrassVolume>())CombineGrass(grass,map.id,patch++);
                    Physics.SyncTransforms();int removed=0;
                    foreach(var root in scene.GetRootGameObjects())foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
                        if(WallAttachments.Contains(r.name)&&!Physics.CheckSphere(r.bounds.center,.28f,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore))
                        {Object.DestroyImmediate(r.gameObject);removed++;}
                    if(repaired>0)
                    {
                        Physics.SyncTransforms();var surface=geometry.GetComponent<NavMeshSurface>();surface.BuildNavMesh();
                        if(!surface.navMeshData)throw new System.InvalidOperationException("Collision navigation bake failed: "+map.id);
                        AssetDatabase.CreateAsset(surface.navMeshData,AssetDatabase.GenerateUniqueAssetPath(ClassicArt.Root+"/"+map.id+"_Navigation08.asset"));
                    }
                    EditorSceneManager.SaveScene(scene);ClassicProjectSetup.RefreshMapPreview(map);
                    Debug.Log("Collision08 "+map.id+": repaired structural surfaces="+repaired+", removed floating wall attachments="+removed);
                }
                finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        static void CombineGrass(GrassVolume grass,string map,int patch)
        {
            var tufts=new List<MeshFilter>();foreach(var filter in grass.GetComponentsInChildren<MeshFilter>())if(filter.name=="Foliage blade")tufts.Add(filter);
            if(tufts.Count==0)return;
            var blade=Resources.Load<Mesh>("Classic/Meshes/GrassBlade");var material=tufts[0].GetComponent<Renderer>().sharedMaterial;
            // Two interleaved meshes preserve the existing low-quality half-density option.
            for(int group=0;group<2;group++)
            {
                var instances=new List<CombineInstance>();
                for(int i=group;i<tufts.Count;i+=2)
                {
                    var t=tufts[i].transform;
                    instances.Add(new CombineInstance{mesh=blade,transform=Matrix4x4.TRS(t.localPosition,t.localRotation,new Vector3(.65f,t.localScale.y,.65f))});
                }
                if(instances.Count==0)continue;
                var mesh=new Mesh{name="Combined grass "+map+" "+patch+" "+group,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(instances.ToArray(),true,true);mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(ClassicArt.Root+"/Meshes/Grass08_"+map+"_"+patch+"_"+group+".asset"));
                var go=new GameObject("Combined grass density "+group);go.transform.SetParent(grass.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            foreach(var tuft in tufts)Object.DestroyImmediate(tuft.gameObject);
        }
    }
}
