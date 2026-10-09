using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace TacticalZero.Editor
{
    public static class EnvironmentDetail12
    {
        public static void Apply(Transform root,string id)
        {
            var steel=ClassicArt.Steel;var wall=Resources.Load<Material>("Classic/Materials/"+id+"_Walls");var trim=Resources.Load<Material>("Classic/Materials/"+id+"_Trim");
            foreach(var area in root.GetComponentsInChildren<MapArea>())if(area.Interior)
            {
                var t=area.transform;float w=area.Size.x,d=area.Size.z;
                for(int side=-1;side<=1;side+=2)
                {
                    // A continuous low plinth grounds the facade, windows stay completely open.
                    Part("Weathered facade plinth",t,new Vector3(side*w*.5f,.14f,0),new Vector3(.32f,.28f,d),steel);
                    for(int k=0;k<6;k++)Part("Facade panel joint",t,new Vector3(side*(w*.5f+.013f),.55f,(k/5f-.5f)*d*.85f),new Vector3(.02f,.85f,.035f),steel);
                    if(id=="outpost")
                    {
                        for(int k=0;k<5;k++)Part("Timber bunker facing",t,new Vector3(side*(w*.5f+.07f),.17f+k*.17f,0),new Vector3(.14f,.12f,d),ClassicArt.Wood);
                        var eave=Part("Sloping timber roof",t,new Vector3(side*w*.25f,3.7f,0),new Vector3(w*.51f,.12f,d+.8f),wall,true);eave.transform.localRotation=Quaternion.Euler(0,0,-side*12);
                    }
                    else if(id=="docks")
                    {
                        for(int k=0;k<14;k++)Part("Corrugated siding",t,new Vector3(side*(w*.5f+.04f),.51f,(k/13f-.5f)*d*.95f),new Vector3(.075f,.93f,.065f),trim);
                        for(int k=0;k<3;k++)Part("Canopy steel outrigger",t,new Vector3(side*(w*.5f+.4f),3.4f,(k-1)*d*.38f),new Vector3(.9f,.08f,.1f),steel);
                    }
                    else if(id=="ruins")
                    {
                        for(int k=0;k<4;k++)Part("Exposed masonry course",t,new Vector3(side*(w*.5f+.026f),.15f+k*.22f,0),new Vector3(.035f,.023f,d-.4f),steel);
                        Part("Broken upper corner",t,new Vector3(side*(w*.5f-.25f),4.0f,d*.42f),new Vector3(.7f,1.0f,1.3f),wall,true);
                    }
                    else
                    {
                        ClassicArt.Rod("Exterior insulated conduit",t,new Vector3(side*(w*.5f+.17f),2.9f,-d*.46f),new Vector3(side*(w*.5f+.17f),2.9f,d*.46f),.065f,steel);
                        Part("Exterior junction box",t,new Vector3(side*(w*.5f+.18f),.69f,d*.3f),new Vector3(.25f,.40f,.40f),steel);
                    }
                    for(int k=0;k<3;k++)
                    {
                        var shelf=Part("Interior storage shelf",t,new Vector3(side*(w*.5f-.6f),.45f+k*.43f,d*.30f),new Vector3(.8f,.045f,1.5f),steel);
                        Part("Stored equipment cases",t,new Vector3(side*(w*.5f-.6f),.61f+k*.43f,d*.30f),new Vector3(.48f,.28f,.66f),k%2==0?trim:ClassicArt.Wood);
                    }
                }
            }
            // Scatter authored cover groups in free areas, leaving room approaches clear.
            var random=new System.Random(id=="sector07"?12:id=="docks"?27:id=="ruins"?41:63);
            for(int i=0;i<26;i++)
            {
                var p=new Vector3((float)random.NextDouble()*64-32,0,(float)random.NextDouble()*40-20);
                bool clear=true;foreach(var area in root.GetComponentsInChildren<MapArea>())
                {var local=area.transform.InverseTransformPoint(p);if(Mathf.Abs(local.x)<area.Size.x*.5f+3&&Mathf.Abs(local.z)<area.Size.z*.5f+4){clear=false;break;}}
                foreach(var point in root.GetComponentsInChildren<MapRoutePoint>())if(Vector3.Distance(p,point.transform.position)<3)clear=false;
                if(!clear)continue;
                if(!Physics.Raycast(p+Vector3.up*15,Vector3.down,out var hit,16,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore)||hit.point.y>.1f)continue;
                var cluster=new GameObject("Cover island "+i).transform;cluster.SetParent(root,false);cluster.localPosition=p;cluster.localRotation=Quaternion.Euler(0,i*137.5f,0);
                if(id=="outpost"||id=="ruins")
                {
                    var rock=Part("Fractured natural cover",cluster,new Vector3(0,.6f,0),new Vector3(2.1f,1.2f,1.4f),wall);
                    rock.GetComponent<MeshFilter>().sharedMesh=Rock();var collider=rock.AddComponent<MeshCollider>();collider.sharedMesh=Rock();collider.convex=true;rock.layer=LayerMask.NameToLayer("TZWorld");
                    for(int k=0;k<4;k++)Part("Loose stone fragments",cluster,new Vector3(Mathf.Sin(k*2)*1.2f,.1f,Mathf.Cos(k*2)*.8f),new Vector3(.45f,.2f,.32f),wall);
                }
                else
                {
                    Part("Stacked industrial cover",cluster,new Vector3(0,.5f,0),new Vector3(1.5f,1,1.1f),trim,true);
                    Part("Cross tie steel band",cluster,new Vector3(0,.51f,0),new Vector3(.10f,1.03f,1.14f),steel);
                    for(int side=-1;side<=1;side+=2)Part("Wood pallet foot",cluster,new Vector3(side*.55f,.07f,0),new Vector3(.12f,.14f,1.3f),ClassicArt.Wood);
                }
            }
            // Reuse a closed, irregular rock mesh; visible edges and physical hulls coincide.
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                if(filter.name=="Boundary fractured cliff"||filter.name=="Interior granite ridge")
                {filter.sharedMesh=Rock();var collider=filter.GetComponent<MeshCollider>();collider.sharedMesh=Rock();collider.convex=true;}
        }
        static Mesh rockMesh;
        static Mesh Rock()
        {
            if(rockMesh)return rockMesh;string path=ClassicArt.Root+"/Meshes/Layout12/SculptedRock.asset";rockMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(rockMesh)return rockMesh;
            var v=new List<Vector3>();var tris=new List<int>();const int sides=14,rows=9;
            for(int j=0;j<rows;j++)for(int k=0;k<sides;k++)
            {float a=k*Mathf.PI*2/sides,latitude=-Mathf.PI*.5f+.03f+j/(rows-1f)*(Mathf.PI-.06f);float r=1+.12f*Mathf.Sin(k*3.17f+j*1.71f)+.06f*Mathf.Cos(k*1.4f-j*2.8f);v.Add(new Vector3(Mathf.Cos(a)*Mathf.Cos(latitude),Mathf.Sin(latitude),Mathf.Sin(a)*Mathf.Cos(latitude))*.5f*r);}
            for(int j=0;j<rows-1;j++)for(int k=0;k<sides;k++){int a=j*sides+k,b=j*sides+(k+1)%sides;tris.AddRange(new[]{a,a+sides,b,b,a+sides,b+sides});}
            for(int k=1;k<sides-1;k++){tris.AddRange(new[]{0,k,k+1});int n=(rows-1)*sides;tris.AddRange(new[]{n,n+k+1,n+k});}
            rockMesh=new Mesh{name="Sculpted fractured granite"};rockMesh.SetVertices(v);rockMesh.SetTriangles(tris,0);rockMesh.RecalculateNormals();rockMesh.RecalculateBounds();AssetDatabase.CreateAsset(rockMesh,path);return rockMesh;
        }
        static GameObject Part(string name,Transform parent,Vector3 p,Vector3 size,Material mat,bool solid=false)
        {
            var go=ClassicArt.Part(name,parent,p,size,mat);if(solid){go.layer=LayerMask.NameToLayer("TZWorld");go.AddComponent<BoxCollider>();}GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);return go;
        }
    }
}
