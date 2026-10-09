using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TacticalZero
{
    [DefaultExecutionOrder(150)]
    public sealed class SupplyPresentation : MonoBehaviour
    {
        static Mesh ringMesh; static Material hologram;
        Transform ring; ClassicLoot loot;
        public void Build(string kind,string id)
        {
            // Compatibility with old prefabs: no physical or trigger collider survives.
            foreach(var collider in GetComponentsInChildren<Collider>()){collider.enabled=false;Destroy(collider);}
            foreach(var renderer in GetComponentsInChildren<Renderer>())renderer.enabled=false;
            loot=GetComponent<ClassicLoot>();
            if(!ringMesh)ringMesh=Ring();
            if(!hologram)hologram=Resources.Load<Material>("Classic/Materials/SupplyHologram");
            Color tint=SupplyHUD.Tint(loot);
            ring=new GameObject("Supply ground halo").transform;ring.SetParent(transform,false);
            ring.gameObject.AddComponent<MeshFilter>().sharedMesh=ringMesh;
            var r=ring.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=hologram;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            var properties=new MaterialPropertyBlock();properties.SetColor("_BaseColor",tint);r.SetPropertyBlock(properties);
            // SupplyHUD projects the category glyph after the camera moves; avoid a second overlapping icon.
            UpdatePose();
        }
        void LateUpdate(){if(loot)UpdatePose();}
        void UpdatePose()
        {
            float pulse=1+Mathf.Sin(Time.time*2.5f+transform.position.x)*.035f;
            ring.position=loot.GroundPosition+Vector3.up*.035f;ring.localScale=Vector3.one*(pulse*.45f);ring.rotation=Quaternion.identity;
            var player=GameManager.Instance?GameManager.Instance.Player:null;
            ring.gameObject.SetActive(!player||(player.transform.position-loot.GroundPosition).sqrMagnitude<36);
        }
        static Mesh Ring()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            void Arc(float inner,float outer,int start,int end)
            {
                for(int i=start;i<end;i++)
                {
                    float a=i*Mathf.PI/48,b=(i+1)*Mathf.PI/48;int n=vertices.Count;
                    vertices.Add(new Vector3(Mathf.Cos(a)*inner,0,Mathf.Sin(a)*inner));vertices.Add(new Vector3(Mathf.Cos(a)*outer,0,Mathf.Sin(a)*outer));
                    vertices.Add(new Vector3(Mathf.Cos(b)*outer,0,Mathf.Sin(b)*outer));vertices.Add(new Vector3(Mathf.Cos(b)*inner,0,Mathf.Sin(b)*inner));
                    triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
                }
            }
            Arc(.43f,.455f,0,96);for(int i=0;i<4;i++)Arc(.51f,.56f,i*24,i*24+17);Arc(.15f,.165f,0,96);
            var mesh=new Mesh{name="Supply concentric segmented halo"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
